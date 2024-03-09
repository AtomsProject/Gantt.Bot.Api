using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Gantt.Bot.DataModel;
using Gantt.Bot.DataModel.Utilities;
using Gantt.Bot.Scheduler.Helpers;
using Gantt.Bot.Scheduler.Logger;
using Gantt.Bot.Scheduler.Model;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using QuikGraph;
using QuikGraph.Algorithms;
using MatrixExtensions = Gantt.Bot.Scheduler.Helpers.MatrixExtensions;

namespace Gantt.Bot.Scheduler;

/// <summary>
/// Build out a graph of tasks and their dependencies, part of Critical Path Method (CPM) algorithm.
/// </summary>
public sealed class TaskGraph
{
    private readonly DebugLoggerFactor _log = new();
    private const string MdSpace = "   ";
    private readonly IReadOnlyDictionary<string, SchedulingTask> _allTasksLookup;
    private readonly BidirectionalGraph<SchedulingTask, Edge<SchedulingTask>> _graph = new();
    private readonly GlobalSettings _settings;
    private readonly IReadOnlyDictionary<string, Resource> _resourcesLookup;
    private readonly ImmutableList<Resource> _resources;
    
    private TaskGraph(IReadOnlyDictionary<string, SchedulingTask> allTasksLookup, GlobalSettings settings,
        ImmutableList<Resource> resources)
    {
        _allTasksLookup = allTasksLookup;
        _settings = settings;
        _resources = resources;
        _resourcesLookup = resources.ToDictionary(r => r.Id, r => r);
    }

    public IReadOnlyDictionary<string, SchedulingTask> AllTasksLookup => _allTasksLookup;
    public float TargetProbability { get; init; }
    public  BidirectionalGraph<SchedulingTask, Edge<SchedulingTask>> Graph => _graph;
    public GlobalSettings Settings => _settings;
    public ImmutableList<Resource> Resources => _resources;
    public IReadOnlyDictionary<string, Resource> ResourcesLookup => _resourcesLookup;

    public static TaskGraph Create(
        ImmutableList<TaskItem> tasks,
        float targetProbability,
        ImmutableList<Resource> resources,
        GlobalSettings globalSettings,
        IDurationSimulation? durationSimulator = null)
    {
        var (allTasksLookup, descendantsLookup) =
            SchedulingTask.CreateAllTasksLookup(tasks, targetProbability, globalSettings, durationSimulator);

        var graph = new TaskGraph(allTasksLookup, globalSettings, resources) { TargetProbability = targetProbability };

        foreach (var task in allTasksLookup)
        {
            graph._graph.AddVertex(task.Value);
        }

        foreach (var task in allTasksLookup.Values)
        {
            if (task.Task.Dependencies is not null)
            {
                foreach (var dependency in task.Task.Dependencies)
                {
                    if (allTasksLookup.TryGetValue(dependency, out var dependencyTask))
                    {
                        graph.AddDependency(dependencyTask, task);
                    }
                }
            }

            if (task.Task.ParentTaskId is not null)
            {
                if (allTasksLookup.TryGetValue(task.Task.ParentTaskId, out var parentTask))
                {
                    graph.AddDependency(task, parentTask);
                }
            }
        }

        // Now set the sibling dependencies
        foreach (var taskSiblingGroup in descendantsLookup)
        {
            if (taskSiblingGroup.Key == SchedulingTask.RootTaskId) continue;

            SchedulingTask? priorTask = null;
            foreach (var sibling in taskSiblingGroup.Value.Where(t => !t.AllowParallelScheduling)
                         .OrderBy(t => t.ordinal))
            {
                var task = allTasksLookup[sibling.id];
                if (priorTask is not null)
                {
                    graph.AddDependency(priorTask, task);
                }

                priorTask = task;
            }
        }

        if (!graph._graph.IsDirectedAcyclicGraph())
            throw new InvalidOperationException(
                "The graph is not a Directed Acyclic Graph (DAG), circular dependency detected in the tasks.");

        graph.PropagatePriority();
        return graph;
    }

    /// <summary>
    /// Build a Markdown representation of the graph, comparable with mermaid.
    /// </summary>
    public string GenerateGraphMarkdown()
    {
        return _graph.GenerateGraphMarkdown();
    }

    public string GenerateGraphMarkdownDepthFirst()
    {
        return _graph.GenerateGraphMarkdownDepthFirst();
    }

    public string DumpGantt(GraphGroupBy groupBy = GraphGroupBy.Project)
    {
        var title = "Project Schedule";

        switch (groupBy)
        {
            case GraphGroupBy.Resource:
                title = "Resource Schedule";
                break;
            case GraphGroupBy.ResourceAndProject:
                title = "Resource and Project Schedule";
                break;
            case GraphGroupBy.Milestone:
                title = "Project Milestones";
                break;
            case GraphGroupBy.Project:
            default:
                break;
        }

        title = $"{title} {TargetProbability * 100}%";
        var sb = new StringBuilder();
        sb.AppendLine("gantt");
        sb.AppendLine($"{MdSpace}dateFormat YYYY-MM-DD");
        sb.AppendLine($"{MdSpace}title {title}");
        sb.AppendLine($"{MdSpace}excludes weekends");
        sb.AppendLine();
        var allTasks = _graph.Vertices.Where(v => v.CanBeScheduled || v.Task.IsMilestone);
        // Group tasks by ProjectName for sectioning
        var tasksByProject = groupBy == GraphGroupBy.Milestone
            ? allTasks.Where(t => t.Task.IsMilestone).OrderBy(t => t.EarliestFinish).GroupBy(GroupBy)
            : allTasks.OrderBy(t => t.EarliestFinish).GroupBy(GroupBy);

        string GroupBy(SchedulingTask st)
        {
            switch (groupBy)
            {
                case GraphGroupBy.Resource when st.AssignedResourceId is not null:
                    return string.IsNullOrWhiteSpace(st.AssignedResourceId)
                        ? "Unassigned"
                        : _resourcesLookup.TryGetValue(st.AssignedResourceId, out var r1Name)
                            ? r1Name.Name
                            : "Unknown";
                case GraphGroupBy.ResourceAndProject:
                    var resourceName = string.IsNullOrWhiteSpace(st.AssignedResourceId)
                        ? "Unassigned"
                        : _resourcesLookup.TryGetValue(st.AssignedResourceId, out var resource)
                            ? resource.Name
                            : "Unknown";
                    return $"{st.ProjectName} {resourceName}";
                case GraphGroupBy.Project:
                default:
                    return st.ProjectName;
            }
        }

        foreach (var projectGroup in tasksByProject)
        {
            sb.AppendLine($"{MdSpace}section {projectGroup.Key}");
            foreach (var task in projectGroup.OrderBy(t => t.EarliestStart))
            {
                var taskEarliestStart = task.EarliestStart;
                var taskEarliestFinish = task.EarliestFinish;

                if (taskEarliestStart < 0)
                    taskEarliestStart = 0;

                if (taskEarliestFinish < 0)
                    taskEarliestFinish = 0;

                var taskStart = _settings.ConvertWorkingDaysToDate(taskEarliestStart).ToString("yyyy-MM-dd");
                var taskEnd = _settings.ConvertWorkingDaysToDate(taskEarliestFinish + 1).ToString("yyyy-MM-dd");

                var critical = task.Slack == 0 ? "crit, " : "";

                if (task.Task.IsMilestone)
                {
                    sb.AppendLine($"{MdSpace}{MdSpace}{task.Name}{MdSpace}:{critical}milestone, {taskStart},{taskEnd}");
                }

                if (task.IsParentTask || task.Task.IsMilestone)
                {
                    // TODO: Would be nice to see the overall project start and end date for the child tasks.
                    //sb.AppendLine($"{MdSpace}{MdSpace}{task.Name}{MdSpace}:{critical}milestone, {taskStart},{taskEnd}");
                }
                else
                {
                    sb.AppendLine($"{MdSpace}{MdSpace}{task.Name}{MdSpace}:{critical}{taskStart},{taskEnd}");
                }
            }
        }

        return sb.ToString();
    }

    private void AddDependency(SchedulingTask fromSchedulingTask, SchedulingTask toSchedulingTask)
    {
        _graph.AddEdge(new Edge<SchedulingTask>(fromSchedulingTask, toSchedulingTask));
    }

    private void PropagatePriority()
    {
        using var log = _log.OpenLog("priority");
        log.WriteLine(DateTime.Now.ToString("O"));

        uint startingPriority = 0;
        foreach (var vertex in _graph.Vertices.Where(v => v.Task.Priority is not null)
                     .OrderByDescending(v => v.Task.Priority))
        {
            // While looking through, someone has already put a higher priority on this Vertices
            if (vertex.HasRanking)
            {
                log.WriteLine($"Priority already set for {vertex.Name} ({vertex.Id}), skipping");
                continue;
            }

            startingPriority = PropagatePriorityChild(vertex, startingPriority, _graph, log);
            log.WriteLine($"Priority set for {vertex.Name} ({vertex.Id}) to {vertex.Ranking}");
        }

        // Ok, now look for any task that didn't get a priority.
        // These are going to be child task that are set for independent scheduling, but didn't get a priority.
        foreach (var vertex in _graph.Vertices.Where(v => v.HasRanking is false)
                     // We are ordering by priority just in case, but they should all be null.
                     .OrderByDescending(v => v.Task.Priority))
        {
            // While looking through, someone has already put a higher priority on this Vertices
            if (vertex.HasRanking)
            {
                log.WriteLine($"Priority set for root - {vertex.Name} ({vertex.Id}) to {vertex.Ranking}");
                continue;
            }

            // Tasks that don't have a priority, could be given the same priority order as each other
            // But we need to run down the tree to make sure we schedule dependent tasks first.
            PropagatePriorityChild(vertex, startingPriority, _graph, log);
            log.WriteLine($"Priority set for root - {vertex.Name} ({vertex.Id}) to {vertex.Ranking}");
        }

        return;

        static uint PropagatePriorityChild(
            SchedulingTask task,
            uint currentRank,
            IBidirectionalIncidenceGraph<SchedulingTask, Edge<SchedulingTask>> graph,
            IDebugWriter log)
        {
            // Take all the edges to tasks that we depend on to complete
            // we first to do a deep first search to get to the highest priority task we depend on
            // If we don't have any dependencies, that have a priority
            // then it doesn't really matter what order we do them in.
            // Do we can then go deep first search for the children and start numbering them.

            // our direct dependents who don't have a priority, could be given the same priority order as each other
            // They will need a priority order 1 more then us, or the heap will not pick them before us.
            log.WriteLine($"PropagatePriorityChild for {task.Name} ({task.Id}) entry rank {currentRank}");

            // Not sure if the check for a lower rank is right, but it seems to work.
            if (task.HasRanking && task.Ranking <= currentRank)
            {
                log.WriteLine(
                    $"PropagatePriorityChild for {task.Name} ({task.Id}) - Already set to {task.Ranking} - EXIT");
                return task.Ranking.Value;
            }

            //  Going to try having the children of a task with a priority, scheduled fully independently
            // var dependedTasksWithPriority = graph
            //     .InEdges(task)
            //     .Select(e => e.Source)
            //     .Where(t => t.HasUserPriority)
            //     .OrderByDescending(e => e.UserPriority ?? 0)
            //     .ToList();
            // if (dependedTasksWithPriority.Count > 0)
            // {
            //     foreach (var dependedTask in dependedTasksWithPriority.Where(t => t.HasUserPriority == false))
            //     {
            //         currentRank = PropagatePriorityChild(dependedTask, currentRank, graph, log);
            //     }
            //
            //     log.WriteLine(
            //         $"PropagatePriorityChild for {task.Name} ({task.Id}) - dependedTasksWithPriority EXIT rank {currentRank:N0}");
            // }


            var dependedTasksWithOutPriority = graph
                .InEdges(task)
                .Select(e => e.Source)
                .Where(t => !t.HasUserPriority)
                .ToList();
            if (dependedTasksWithOutPriority.Count > 0)
            {
                var highestDependentRank = currentRank;
                foreach (var dependedTask in dependedTasksWithOutPriority)
                {
                    // Lock in the current rank for all direct dependents
                    var r = PropagatePriorityChild(dependedTask, currentRank, graph, log);
                    log.WriteLine(
                        $"PropagatePriorityChild for {task.Name} ({task.Id}) - dependedTasksWithOutPriority {dependedTask.Name}, high rank is {highestDependentRank:N0}");

                    if (dependedTask.Ranking.HasValue)
                        highestDependentRank = Math.Max(highestDependentRank, r);
                }

                log.WriteLine(
                    $"PropagatePriorityChild for {task.Name} ({task.Id}) - dependedTasksWithOutPriority EXIT rank {highestDependentRank:N0} - Updating Current Rank");

                currentRank = highestDependentRank;
            }

            currentRank++;
            task.Ranking = currentRank;

            log.WriteLine($"PropagatePriorityChild for {task.Name} ({task.Id}) EXIT rank {currentRank:N0}");
            return currentRank;
        }
    }

    public IReadOnlyList<SchedulingTask> FindCriticalPath()
    {
        // Reset the last critical path and slack
        foreach (var task in _graph.Vertices)
        {
            task.ResetCriticalPathValues();
        }

        CalculateEarliestStartAndFinish();
        CalculateLatestStartAndFinish();

        foreach (var task in _graph.Vertices)
        {
            task.Slack = task.LatestStart - task.EarliestStart;
        }

        return _graph.Vertices
            .Where(task => Math.Abs(task.Slack) < double.Epsilon
                           && task is { CanBeScheduled: true, IsScheduled: false })
            .OrderBy(t => t.Ranking ?? uint.MaxValue)
            .ToList();
    }

    private void CalculateEarliestStartAndFinish()
    {
        var sortedTasks =
            _graph.TopologicalSort().Where(r => !r.IsScheduled); //.Where(t => t.CanBeScheduled && !t.IsScheduled);
        foreach (var task in sortedTasks)
        {
            var maxEarliestFinishOfPredecessors = 0;
            foreach (var edge in _graph.InEdges(task))
            {
                maxEarliestFinishOfPredecessors =
                    Math.Max(maxEarliestFinishOfPredecessors, edge.Source.EarliestFinish + 1);
            }

            task.EarliestStart = maxEarliestFinishOfPredecessors;
        }
    }

    private void CalculateLatestStartAndFinish()
    {
        var sortedTasks =
            _graph.TopologicalSort().Reverse()
                .Where(r => !r.IsScheduled); //.Where(t => t.CanBeScheduled && !t.IsScheduled);
        foreach (var task in sortedTasks)
        {
            var minLatestFinishOfPredecessors = task.LatestFinish;
            foreach (var edge in _graph.OutEdges(task))
            {
                minLatestFinishOfPredecessors = Math.Min(edge.Target.LatestFinish, minLatestFinishOfPredecessors);
            }

            task.LatestFinish = minLatestFinishOfPredecessors;
        }
    }

    public void ScheduleResources()
    {
        // Create a logger of what we are doing.
        using var log = _log.OpenLog("schedule");
        log.WriteLine(DateTime.Now.ToString("O"));
        log.DumpList("Resources", _resources);

        var kindByResource = CreateKindByResourceMatrix(_resources);
        log.Dump("kindByResource", kindByResource, true);

        var taskByResourceIndividualPriority = CreateTaskByResourceIndividualPriority(_resources);
        log.Dump("Manually Assigned individual per Task - Transposed", taskByResourceIndividualPriority.Transpose(),
            true);

        var availableResources = ResourceAvailability.Create(_settings, _resources);
        log.DumpTranspose("AvailableResources - Transposed", availableResources.GetAvailabilityMatrix(), true);

        try
        {
            while (true)
            {
                // This will update the min/max start/end times for each task
                // and return any task that is on the critical path.
                // The important thing here is to try an honor any task that has a start after or must be done by.
                var criticalPath = FindCriticalPath();
                //
                // log.WriteLine(MatrixExtensions.Hr);
                // log.WriteLine(DumpGraph());
                // log.WriteLine(MatrixExtensions.Hr);
                // log.WriteLine(DumpGantt());
                // log.WriteLine(MatrixExtensions.Hr);
                //
                if (criticalPath.Count > 0)
                {
                    log.WriteLine("Scheduling Critical Path");
                    ScheduleTasks(criticalPath[0]);
                    continue;
                }

                // Find the task with the lowest rank.
                var taskByRanking = _graph.Vertices
                    .Where(v => v is { CanBeScheduled: true, IsScheduled: false })
                    .MinBy(v => v.Ranking ?? uint.MaxValue);

                if (taskByRanking is null) return; // We have scheduled all the tasks.

                ScheduleTasks(taskByRanking);
            }

            void ScheduleTasks(SchedulingTask t)
            {
                log.WriteLine();
                log.WriteLine(new string('*', 80));
                log.WriteLine($"*  Scheduling: {t.Name} ({t.Id})".PadRight(79) + "*");
                log.WriteLine(
                    $"*  Duration: {t.Duration:N0} ({t.DurationResourceAdjusted:N0}) WorkType: {t.Task.WorkTypeId}, Slack: {(t.Slack > 4000 ? "N/A" : t.Slack)}, After: {t.StartAfter:N0}"
                        .PadRight(79) + "*");
                log.WriteLine(
                    $"*  Earliest Start: {t.EarliestStart:N0} Latest Finish: {t.LatestFinish:N0}, {t.Task.Duration} "
                        .PadRight(79) + "*");
                log.WriteLine(new string('*', 80));

                log.Dump("AvailableResources", availableResources.GetAvailabilityMatrix(), true);

                if (t.Task.WorkTypeId is null)
                    throw new InvalidOperationException($"Task {t.Task.Id} has no WorkTypeId")
                    {
                        Data = { { "TaskId", t.Task.Id } }
                    };

                // Going task by task, until we find the first one we can schedule.
                var resourceSelector = (Vector<double>)kindByResource[t.Task.WorkTypeId];
                var individualPriority = taskByResourceIndividualPriority.Row(t.Index);

                var orgResourceSelector = resourceSelector;

                // Check if we have IP for this task
                if (individualPriority.Sum() > double.Epsilon)
                {
                    resourceSelector = resourceSelector.PointwiseMultiply(individualPriority);
                }

                log.Dump("Resource Selector\n       \tKind   \tIndiv  \tResult",
                    [orgResourceSelector, individualPriority, resourceSelector]);

                if (resourceSelector.Sum() < double.Epsilon)
                {
                    // No resources available for this task
                    throw new InvalidOperationException(
                        "We have tried to schedule the task, but we couldn't find a resource for this kind of task.");
                }

                var selectedResources = resourceSelector
                    .EnumerateIndexed()
                    .Where(r => r.Item2 > double.Epsilon)

                    // availableResources.GetAvailability is very slow right now, so we may not want to run it in the linq.
                    .Select(r => availableResources.GetAvailability(
                            r.Item1,
                            t.Duration,
                            t.EarliestStart,
                            t.Task.WorkTypeId)
                        with
                        {
                            ScoreOrg = r.Item2
                        }
                    )
                    .ToList();

                var maxDelay = 0;
                var minDelay = int.MaxValue;
                var maxDuration = 0;

                foreach (var selected in selectedResources)
                {
                    maxDelay = Math.Max(maxDelay, selected.StartDay - t.EarliestStart);
                    minDelay = Math.Min(minDelay, selected.StartDay - t.EarliestStart);
                    maxDuration = Math.Max(maxDuration, selected.Duration);
                }

                var delayDelta = (double)maxDelay - minDelay;
                var durationDelta = (double)maxDuration - t.Duration;
                // now include the max delay and max duration in the score
                selectedResources = selectedResources.Select(r =>
                    {
                        var delay = (r.StartDay - t.EarliestStart) - minDelay;
                        var duration = r.Duration - t.Duration;
                        // now make delay relative to the max delay
                        var delayFactor = Math.Min(1, -Math.Log(delayDelta <= 0d ? 0 : delay / delayDelta));
                        var durationFactor = Math.Min(1, -Math.Log(durationDelta <= 0d ? 0 : duration / durationDelta));

                        // Delay and duration factor should be logarithmic

                        return r with
                        {
                            DelayFactor = delayFactor,
                            DurationFactor = durationFactor,
                            Score = ((r.ScoreOrg * 2) + delayFactor + durationFactor) / 4
                        };
                    }).GroupBy(r => (int)(r.Score * 100))
                    .OrderByDescending(r => r.Key)
                    .SelectMany(r => r.OrderBy(r => r.StartDay)).ToList();

                log.DumpList("Resource Consideration", selectedResources);

                // TODO: Need to build out the system that makes task assignments to a project and dependencies sticky,
                // meaning we want to keep task, particularly ones of the same kind, on the same resource.
                //  - This will help with the learning curve of the resource, and also help with the project
                //  - Also any delays in schedule will be easier to manage.
                // TODO: need to compute the duration factor for the resource and this task kind before we can check availability
                // TODO: Need to think about how to keep from adding gaps in the schedule, right now there is no care taken for this.
                //  - For now, we are just going to schedule new tasks around the existing ones.
                // TODO: We may need to think about adding a system to move an already scheduled task

                // Steps:
                // 1. See if our first picks availability is good compared to everyone else.
                //   - Think about applying a factor to the score based on the availability.
                // 2. If we are good, then go ahead and schedule the work.
                // 3. If we think another resource would be better, then go ahead an schedule there.

                var earliestStart = selectedResources.Min(r => r.StartDay);

                // How much longer do we allow the task to take compared to planned.
                var maxSchedulingDuration = 8d;
                if (t.Task.Duration is not null)
                {
                    // Thoughts:
                    //  - We need to be computing and recording the work factor for each resource and task kind.
                    // TODO: Need to model this to see if we are happy with this or not.
                    // maxSchedulingDuration = t.Duration * (
                    //     (t.Task.Duration.Pessimistic - t.Task.Duration.Optimistic) /
                    //     t.Task.Duration.MostLikely * 1.75d);
                    // maxSchedulingDuration = Math.Max(5d, maxSchedulingDuration);

                    maxSchedulingDuration = t.Task.Duration.Pessimistic +
                                            Math.Max(10d,
                                                ((t.Task.Duration.Pessimistic - t.Task.Duration.Optimistic) * 2));
                }

                log.WriteLine($"Earliest Start: {earliestStart} Max Scheduling Duration: {maxSchedulingDuration}");

                // Indicates that we have tried to fit it into the schedule
                // But we couldn't find anything, so just schedule it this time.
                var forceSchedule = false;
                while (true)
                {
                    foreach (var selected in selectedResources)
                    {
                        var resource = _resources[selected.ResourceIndex];
                        var startDelay = selected.StartDay - earliestStart;

                        // How much longer do we think this take will take then we had planned.
                        var taskExtension = selected.Duration - t.Duration;

                        // if a task longer then the duration we had planned, that can be ok to a point.
                        switch (forceSchedule)
                        {
                            // We are going to have to move this task to another resource.
                            // However, if we can't find a resource that has a better start day,
                            // then we will just have to schedule it here.
                            case false when (startDelay + taskExtension) > t.Slack:
                                log.WriteLine(
                                    $"  Skipping {resource.Name}: Can't complete in time, {startDelay} + {taskExtension} < {t.Slack}");
                                continue;
                            case false when selected.Duration > maxSchedulingDuration:
                                log.WriteLine(
                                    $"  Skipping {resource.Name}: Too much delay, {selected.Duration} > {maxSchedulingDuration}");
                                continue;
                        }

                        // We have found a resource that we can schedule this task on.
                        // We are going to schedule it here.
                        availableResources.BlockSchedulingAvailability(selected);

                        t.DurationResourceAdjusted = selected.Duration;
                        t.StartAfter = selected.StartDay;
                        t.AssignedResourceId = resource.Id;

                        log.WriteLine(
                            $"   Scheduling {t.Id} on {resource.Name} at {t.StartAfter} for {t.DurationResourceAdjusted} ({t.Duration:N1}) days");

                        break;
                    }

                    // All Good, we have scheduled the task.
                    if (t.IsScheduled) return;
                    if (forceSchedule)
                        throw new InvalidOperationException(
                            "We have tried to schedule the task, but we couldn't find a resource to schedule it on.");

                    DumpTaskState("FORCE SCHEDULE");
                    forceSchedule = true;
                }
            }
        }
        catch (Exception e)
        {
            log.WriteLine(e);
            throw;
        }
        finally
        {
            DumpTaskState("EXISTING SCHEDULE");
        }

        // TODO: Once we have schedule all the task, we need to go update the dates for milestones and projects.
        void DumpTaskState(string? title)
        {
            log.WriteLine();
            log.WriteLine(title);
            log.WriteLine(MatrixExtensions.Hr);
            log.WriteLine(GenerateGraphMarkdown());
            log.WriteLine(MatrixExtensions.Hr);
            log.WriteLine("Project Schedule");
            log.WriteLine(DumpGantt());
            log.WriteLine(MatrixExtensions.Hr);
            log.WriteLine("Resource Schedule");
            log.WriteLine(DumpGantt(GraphGroupBy.Resource));
            log.WriteLine(MatrixExtensions.Hr);
            log.DumpTranspose("Resource availability - Transposed", availableResources.GetAvailabilityMatrix(), true);

            log.WriteLine(" -- Settings ---");
            log.WriteLine(JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private SparseMatrix CreateTaskByResourceIndividualPriority(ImmutableList<Resource> resources)
    {
        var taskByResourceIndividualPriority = new SparseMatrix(_allTasksLookup.Count, resources.Count);
        foreach (var task in _allTasksLookup.Values)
        {
            var resourceIndex = 0;
            foreach (var resource in resources)
            {
                // Compute the resource preference for each task
                if (task.Task.IndividualPriorities?.Find(i => i.ResourceId == resource.Id) is { } individualPriority)
                {
                    taskByResourceIndividualPriority[task.Index, resourceIndex] = individualPriority.Priority;
                }

                resourceIndex++;
            }
        }

        return taskByResourceIndividualPriority;
    }

    private Dictionary<string, Vector> CreateKindByResourceMatrix(IReadOnlyList<Resource> resources)
    {
        var kindByResourceMatrix = new Dictionary<string, Vector>();

        foreach (var workType in _settings.WorkTypes)
        {
            var workTypeMatrix = new SparseVector(resources.Count);
            kindByResourceMatrix.Add(workType.Id, workTypeMatrix);
        }

        for (var i = 0; i < resources.Count; i++)
        {
            foreach (var r in resources[i].WorkTypeAssignments)
            {
                if (!kindByResourceMatrix.TryGetValue(r.WorkTypeId, out var workTypeMatrix))
                {
                    // need to add this work type to the matrix
                    workTypeMatrix = new SparseVector(resources.Count);
                    kindByResourceMatrix.Add(r.WorkTypeId, workTypeMatrix);
                }

                workTypeMatrix[i] = r.PreferenceFactor;
            }
        }

        return kindByResourceMatrix;
    }
}

public enum GraphGroupBy
{
    Milestone,
    Project,
    Resource,
    ResourceAndProject
}