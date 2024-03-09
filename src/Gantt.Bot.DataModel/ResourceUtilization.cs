namespace Gantt.Bot.DataModel;

public class ResourceUtilization
{
    public string ResourceId { get; set; } = null!;
    public List<UtilizationEntry> UtilizationEntries { get; set; } = new();
}

public class UtilizationEntry
{
    public DateTime Date { get; set; }
    public List<TaskAssignment> Assignments { get; set; } = new();
}

public class TaskAssignment
{
    public string TaskId { get; set; } = null!;
    public string TaskName { get; set; } = null!;
    public string WorkTypeId { get; set; } = null!;
    public double Allocation { get; set; }
}