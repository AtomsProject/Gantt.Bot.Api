namespace Gantt.Bot.DataModel;

public class GanttChart
{
    public List<GanttTask> Tasks { get; set; } = new();
    public List<ResourceUtilization> ResourceUtilizations { get; set; } = new();
}

public class GanttTask
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public int Duration { get; set; }
    public List<string> DependencyIds { get; set; } = new();

    public string AssignedResourceId { get; set; } = null!;

    // Indicates if the task is a milestone for filtering purposes.
    public bool IsMilestone { get; set; }

    // Parent task ID to represent the hierarchical structure of tasks and sub-tasks.
    public string? ParentTaskId { get; set; }
}

// Additional classes to represent resources in the context of Gantt chart visualization.
public class GanttResource
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;
    // This could include more details relevant for Gantt chart display, such as availability or skill sets.
}