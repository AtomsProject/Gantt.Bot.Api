using System.Text;
using Gantt.Bot.DataModel.Utilities;
using Gantt.Bot.Scheduler.Model;
using QuikGraph;

namespace Gantt.Bot.Scheduler.Helpers;

public static class MermaidHelpers
{
    /// <summary>
    /// Build a Markdown representation of the graph, comparable with mermaid.
    /// </summary>
    public static string GenerateGraphMarkdown(this IBidirectionalGraph<SchedulingTask, Edge<SchedulingTask>> graph)
    {
        var sb = new StringBuilder();
        sb.AppendLine("graph TD");

        // Assuming TaskGraph has a way to enumerate all tasks and dependencies
        var allTasks = graph.Vertices;
        var dependencies = graph.Edges;

        // Define nodes (tasks)
        AppendNodeDefinitions(allTasks, sb);

        // Define edges (dependencies)
        foreach (var dep in dependencies)
        {
            sb.AppendLine($"    {dep.Source.Id} --> {dep.Target.Id}");
        }

        return sb.ToString();
    }

    public static string GenerateGraphMarkdownDepthFirst(
        this IBidirectionalGraph<SchedulingTask, Edge<SchedulingTask>> graph)
    {
        // Not what I was looking for, but better then the previous implementation
        var sb = new StringBuilder();
        sb.AppendLine("graph TD");

        // Assuming TaskGraph has a way to enumerate all tasks and dependencies
        var allTasks = graph.Vertices.ToList();

        // Define nodes (tasks)
        AppendNodeDefinitions(allTasks, sb);

        // Initialize visited set to track visited nodes
        var visited = new HashSet<SchedulingTask>();
        var edgeList = new List<Edge<SchedulingTask>>();

        // Depth-first traversal to populate the edge list without redundant edges
        void DepthFirstTraversal(SchedulingTask task)
        {
            if (!visited.Add(task)) // If task has already been visited, return
                return;

            // Get children of the current task
            var children = graph.OutEdges(task).Select(e => e.Target);
            foreach (var child in children)
            {
                edgeList.Add(new Edge<SchedulingTask>(task, child)); // Add edge to list
                DepthFirstTraversal(child); // Recurse with child
            }
        }

        // Start DFS from all root nodes (nodes with no incoming edges)
        foreach (var root in allTasks.Where(t => !graph.InEdges(t).Any()))
        {
            DepthFirstTraversal(root);
        }

        // Define edges (dependencies) based on the populated edge list
        foreach (var edge in edgeList)
        {
            sb.AppendLine($"    {edge.Source.Id} --> {edge.Target.Id}");
        }

        return sb.ToString();
    }

    private static void AppendNodeDefinitions(IEnumerable<SchedulingTask> schedulingTasks, StringBuilder stringBuilder)
    {
        foreach (var project in schedulingTasks.GroupBy(t => t.ProjectName))
        {
            stringBuilder.AppendLine($"   subgraph \"{project.Key}\"");
            foreach (var task in project)
            {
                var priority =
                    $"\\nIX: {task.Index} Rank:{(task.Ranking.HasValue ? task.Ranking.Value.ToLimitString() : "N/A")}";
                if (task.UserPriority is not null)
                {
                    priority = $"{priority} ({task.UserPriority.ToLimitString()})";
                }

                if (task.Duration > 0)
                {
                    priority = $"{priority} Duration:{task.Duration.ToLimitString()}";
                }

                if (task.CanBeScheduled)
                {
                    priority =
                        $"{priority}\\nES:{task.EarliestStart.ToLimitString()} EF:{task.EarliestFinish.ToLimitString()} LS:{task.LatestStart.ToLimitString()} LF:{task.LatestFinish.ToLimitString()}";
                }

                if (task.Task.AllowParallelScheduling)
                {
                    priority = $"{priority} \\nParallel";
                }

                if (task.IsRootTask)
                    stringBuilder.AppendLine($"    {task.Id}([\"{task.Name}{priority}\"])");
                if (task.Task.IsMilestone)
                    stringBuilder.AppendLine($"    {task.Id}>\"{task.Name}{priority}\"]");
                else if (task.IsParentTask)
                    stringBuilder.AppendLine($"    {task.Id}[\"{task.Name}{priority}\"]");
                else
                    stringBuilder.AppendLine($"    {task.Id}[[\"{task.Name}{priority}\"]]");
            }

            stringBuilder.AppendLine("   end");
        }
    }

    public static string DumpTable(this TaskGraph taskGraph)
    {
        var sb = new StringBuilder();
        var allTasks = taskGraph.Graph.Vertices.Where(v => v.CanBeScheduled || v.Task.IsMilestone);

        // Add header row
        sb.AppendLine(string.Join('\t',
            "Project",
            "Id",
            "Name",
            "Resource",
            "Is Parent",
            "Is Milestone",
            "Earliest Start",
            "Earliest End",
            "Earliest Start Days",
            "Duration",
            "Duration Resource Adjusted",
            "Slack"
        ));

        foreach (var task in allTasks)
        {
            var taskEarliestStart = task.EarliestStart;
            var taskEarliestFinish = task.EarliestFinish;

            if (taskEarliestStart < 0)
                taskEarliestStart = 0;

            if (taskEarliestFinish < 0)
                taskEarliestFinish = 0;

            var taskStart = taskGraph.Settings.ConvertWorkingDaysToDate(taskEarliestStart).ToString("yyyy-MM-dd");
            var taskEnd = taskGraph.Settings.ConvertWorkingDaysToDate(taskEarliestFinish + 1).ToString("yyyy-MM-dd");

            var resourceName = string.IsNullOrWhiteSpace(task.AssignedResourceId)
                ? "Unassigned"
                : taskGraph.ResourcesLookup.TryGetValue(task.AssignedResourceId, out var resource)
                    ? resource.Name
                    : "Unknown";

            sb.AppendLine(string.Join('\t',
                task.ProjectName,
                task.Id,
                task.Name,
                resourceName,
                task.IsParentTask,
                task.Task.IsMilestone,
                taskStart,
                taskEnd,
                task.EarliestStart,
                task.Duration,
                task.DurationResourceAdjusted,
                task.Slack.ToLimitString()
            ));
        }

        return sb.ToString();
    }


    public static string DumpTable(this TaskGraph taskGraph, TaskGraph taskGraph2)
    {
        var sb = new StringBuilder();
        var allTasks = taskGraph.Graph.Vertices.Where(v => v.CanBeScheduled || v.Task.IsMilestone);

        // Add header row
        sb.AppendLine(string.Join('\t',
            "Project",
            "Id",
            "Name",
            "Resource",
            "Is Parent",
            "Is Milestone",
            "Earliest Start",
            "Earliest End",
            "Latest Start",
            "Latest End",
            "Earliest Start Days",
            "Latest Start Days",
            "Duration",
            "Duration Resource Adjusted",
            "Slack"
        ));

        foreach (var task in allTasks)
        {
            var latestTask = taskGraph2.AllTasksLookup[task.Id];

            var taskEarliestStart = Math.Max(0, task.EarliestStart);
            var taskEarliestFinish = Math.Max(0, task.EarliestFinish);

            var taskLatestStart = Math.Max(0, latestTask.EarliestStart);
            var taskLatestFinish = Math.Max(0, latestTask.EarliestFinish);

            var earliestStart = taskGraph.Settings.ConvertWorkingDaysToDate(taskEarliestStart).ToString("yyyy-MM-dd");
            var earliestEnd = taskGraph.Settings.ConvertWorkingDaysToDate(taskEarliestFinish + 1)
                .ToString("yyyy-MM-dd");

            var latestStart = taskGraph.Settings.ConvertWorkingDaysToDate(taskLatestStart).ToString("yyyy-MM-dd");
            var latestEnd = taskGraph.Settings.ConvertWorkingDaysToDate(taskLatestFinish + 1).ToString("yyyy-MM-dd");

            var resourceName = string.IsNullOrWhiteSpace(task.AssignedResourceId)
                ? "Unassigned"
                : taskGraph.ResourcesLookup.TryGetValue(task.AssignedResourceId, out var resource)
                    ? resource.Name
                    : "Unknown";

            sb.AppendLine(string.Join('\t',
                task.ProjectName,
                task.Id,
                task.Name,
                resourceName,
                task.IsParentTask,
                task.Task.IsMilestone,
                earliestStart,
                earliestEnd,
                latestStart,
                latestEnd,
                task.EarliestStart,
                latestTask.EarliestStart,
                task.Duration,
                task.DurationResourceAdjusted, 
                task.Slack.ToLimitString()
            ));
        }

        return sb.ToString();
    }
}