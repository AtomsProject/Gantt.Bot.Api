using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;
using Gantt.Bot.DataModel;
using Gantt.Bot.DataModel.Utilities;
using Gantt.Bot.Scheduler.Helpers;
using MathNet.Numerics.Statistics;

namespace Gantt.Bot.Scheduler.Model;

/// <summary>
/// Hold a pointer to the task and its associated data. And then used to hold data for the scheduler.
/// </summary>
public class SchedulingTask(
    TaskItem task,
    bool isRootTask,
    string projectName,
    int duration,
    int index,
    bool isParentTask)
{
    public TaskItem Task { get; } = task;

    /// <summary>
    /// Earliest possible start time for this task, based on the completion of its prerequisites.
    /// This value is calculated during the forward pass of the CPM and represents the earliest
    /// point in time the task can begin without delaying the project, assuming all preceding
    /// tasks start as early as possible.
    /// </summary>
    public int EarliestStart
    {
        get => Math.Max(_earliestStart ?? 0, StartAfter ?? 0);
        set => _earliestStart = value;
    }

    /// <summary>
    /// Earliest possible finish time for this task, derived by adding the task's duration
    /// to its EarliestStart. It indicates the soonest the task can be completed, assuming
    /// it starts at the EarliestStart time and there are no delays in preceding tasks.
    /// </summary>
    public int EarliestFinish => EarliestStart + DurationResourceAdjusted;

    /// <summary>
    /// Latest possible start time for this task without delaying the project's completion.
    /// Calculated during the backward pass of the CPM, it reflects the latest the task can
    /// begin without pushing the end date of the project. It takes into account the task's
    /// dependencies and its criticality in the project timeline.
    /// </summary>
    public int LatestStart => LatestFinish - DurationResourceAdjusted;

    /// <summary>
    /// Latest possible finish time for this task, ensuring the project's completion date
    /// is not delayed. This is the LatestStart plus the task's duration. It represents the
    /// latest the task can finish without affecting subsequent tasks and ultimately the
    /// project's deadline.
    /// </summary>
    public int LatestFinish
    {
        get => Math.Min(_latestFinish ?? int.MaxValue, DueBefore ?? int.MaxValue);
        set => _latestFinish = value;
    }

    public void ResetCriticalPathValues()
    {
        // Can't reset once it's been scheduled.
        if (IsScheduled) return;
        _earliestStart = null;
        _latestFinish = null;
    }

    public int? DueBefore { get; set; }
    public int? StartAfter { get; set; }

    /// <summary>
    /// Slack (or float) represents the amount of time that this task can be delayed without
    /// causing a delay to the project's completion date. It is calculated as the difference
    /// between the Latest Start and Earliest Start times (or, equivalently, the difference
    /// between Latest Finish and Earliest Finish times). A task with zero slack is considered
    /// critical, meaning any delay in this task will directly impact the project's deadline.
    /// Tasks with positive slack have some flexibility in their scheduling without affecting
    /// the overall project timeline.
    /// </summary>
    public double Slack { get; set; } = double.PositiveInfinity;

    /// <summary>
    /// represents the task duration in days for this simulation of the schedule.
    /// This will change slightly for each simulation run. 
    /// </summary>
    public int Duration { get; } = duration;

    /// <summary>
    /// Like milestone tasks with children, this task has no duration, but is a placeholder for a group of tasks.
    /// </summary>
    /// <remarks>
    /// <para>Are depended on all child task, even if they them self are also a parent task.</para>
    /// </remarks>
    public bool IsParentTask { get; } = isParentTask;

    public bool CanBeScheduled => !Task.IsMilestone
                                  && !IsParentTask
                                  && Math.Abs(Duration) > 0.01;

    public bool IsRootTask { get; } = isRootTask;

    /// <summary>
    /// The name of the root Task, used for grouping tasks in the UI.
    /// </summary>
    public string ProjectName { get; } = projectName;

    public string Id => Task.Id;
    public int Index { get; } = index;
    public string Name => Task.Name;

    public string? AssignedResourceId { get; set; }
    public bool IsScheduled => AssignedResourceId != null;

    private int? _durationResourceAdjusted;

    public int DurationResourceAdjusted
    {
        get => _durationResourceAdjusted ?? Duration;
        set => _durationResourceAdjusted = value;
    }

    private uint? _ranking;
    private int? _earliestStart;
    private int? _latestFinish;
    public static readonly string RootTaskId = "<<ROOT>>";

    /// <summary>
    /// The ranking order of the task, the lower number means it's done sooner.
    /// </summary>
    /// <remarks>
    /// This is Low first, where priority is High first. We want the user to be able to set a priority with higher number
    /// meaning higher priority, but from a algorithmic perspective, it was easier to use ordinal ranking.
    /// </remarks>
    public uint? Ranking
    {
        get => _ranking;
        set => _ranking = value;
    }

    public bool HasRanking => _ranking.HasValue;

    /// <summary>
    /// An optional priority given by the user for this task, and in turn it's dependencies. Higher number means it is done sooner.
    /// </summary>
    public int? UserPriority => Task.Priority;

    public bool HasUserPriority => Task.Priority.HasValue;

    public static (Dictionary<string, SchedulingTask> allTaskLookup,
        Dictionary<string, List<(string id, int ordinal, bool AllowParallelScheduling)>> descendantsLookup )
        CreateAllTasksLookup(ImmutableList<TaskItem> tasks,
            float targetProbability,
            GlobalSettings settings,
            IDurationSimulation? durationSimulator = null)
    {
        Dictionary<string, SchedulingTask> allTasksLookup = new();
        Dictionary<string, List<(string id, int ordinal, bool AllowParallelScheduling)>> descendantsLookup = new();
        Dictionary<string, TaskItem> taskLookup = tasks.ToDictionary(t => t.Id);

        durationSimulator ??= new MonteCarloPertSimulation();
        var index = 0;
        var orderedTasks = tasks
            .DistinctBy(t => t.Id) // Should really throw an error here, but...
            .OrderBy(t => t.ParentTaskId)
            .ThenBy(t => t.SiblingOrdinal)
            .ToImmutableList();

        // Build the descendants lookup first
        foreach (var task in orderedTasks)
        {
            var parentTaskId = task.ParentTaskId ?? RootTaskId;
            if (!descendantsLookup.ContainsKey(parentTaskId))
                descendantsLookup[parentTaskId] = new List<(string id, int ordinal, bool AllowParallelScheduling)>();
            descendantsLookup[parentTaskId].Add((task.Id, task.SiblingOrdinal, task.AllowParallelScheduling));
        }

        foreach (var task in orderedTasks)
        {
            var duration = task.IsMilestone
                ? null
                : durationSimulator.RunSimulation(task.Duration, targetProbability);

            var rootTask = FindRootTask(task, ImmutableList<string>.Empty);
            if (rootTask.ParentTaskId != null)
                Console.WriteLine($"Task {task.Id} is not connected to a {rootTask.Id} root task");

            var schedulingTask = new SchedulingTask(
                task,
                task.ParentTaskId == null,
                rootTask.Name,
                (int)Math.Ceiling(duration ?? 0),
                index,
                HasChildren(task));
            index++;
            AdjustForDateConstraints(schedulingTask, settings);
            allTasksLookup.Add(task.Id, schedulingTask);
        }

        return (allTasksLookup, descendantsLookup);

        bool HasChildren(TaskItem task) => descendantsLookup.TryGetValue(task.Id, out var children) && children.Any();

        TaskItem FindRootTask(TaskItem task, ImmutableList<string> visited)
        {
            if (task.ParentTaskId == null) return task;
            if (visited.Contains(task.Id)) throw new InvalidOperationException("Circular reference detected");
            visited = visited.Add(task.Id);
            if (taskLookup.TryGetValue(task.ParentTaskId, out var parentTask))
                return FindRootTask(parentTask, visited);

            return task;
        }

        static void AdjustForDateConstraints(SchedulingTask task, GlobalSettings settings)
        {
            if (task.Task.StartAfter.HasValue)
            {
                var startAfter =
                    settings.ConvertDateRangeToWorkday(settings.ProjectStartDate, task.Task.StartAfter.Value);
                task.EarliestStart = startAfter;
                task.StartAfter = startAfter;
            }

            if (task.Task.DueBefore.HasValue)
            {
                var dueBefore =
                    settings.ConvertDateRangeToWorkday(settings.ProjectStartDate, task.Task.DueBefore.Value);
                task.LatestFinish = dueBefore;
                task.DueBefore = dueBefore;
            }
        }
    }
}