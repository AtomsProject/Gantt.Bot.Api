namespace Gantt.Bot.DataModel;

public class Roadmap
{
    public List<RoadmapItem> Milestones { get; set; } = new();
}

public class RoadmapItem
{
    public string TaskId { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>
    /// The expected completion date for the milestone.
    /// </summary>
    public DateTime ExpectedCompletionDate { get; set; }

    public DateTime OptimisticCompletionDate { get; set; }
    public DateTime PessimisticCompletionDate { get; set; }
}