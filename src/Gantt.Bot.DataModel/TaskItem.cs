using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Gantt.Bot.DataModel;

public class TaskItem
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public Duration? Duration { get; init; }
    public ImmutableList<string>? Dependencies { get; init; }
    /// <summary>
    /// Order of the task in the project. Higher number means it is done sooner.
    /// </summary>
    public int? Priority { get; init; }

    /// <summary>
    /// Task should start on or after midnight on this date.
    /// </summary>
    public DateTime? StartAfter { get; init; }

    /// <summary>
    /// Task is due on or before on this date.
    /// </summary>
    public DateTime? DueBefore { get; init; }

    //public ImmutableList<TaskItem> ChildTasks { get; init; } = ImmutableList<TaskItem>.Empty;

    public string? ParentTaskId { get; init; } = null!;
    public int SiblingOrdinal { get; init; }

    /// <summary>
    /// Milestone task are task that show up on the roadmap.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsMilestone { get; init; }

    /// <summary>
    /// Allows for task-specific assignment of resources, enabling nuanced resource allocation decisions.
    /// If not specified the task will be assigned to resources based solely on the work type and skill level.
    /// </summary>
    public List<IndividualPriority>? IndividualPriorities { get; init; }

    /// <summary>
    ///  Links a task to a specific type of work, facilitating matching tasks with resources skilled or preferred in that work type.
    /// </summary>
    /// <remarks>
    /// TODO: If there is IndividualPriorities, then this shouldn't be required.
    /// </remarks>
    public string? WorkTypeId { get; init; }

    /// <summary>
    /// indicates the minimum level of familiarity of a given work type a resource must have to be assigned to this task, ensuring task-resource compatibility.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float RequiredFamiliarScore { get; init; }

    /// <summary>
    /// Indicates that this task should supersede any active task the resource is working on, allowing for task-specific prioritization of resources.
    /// </summary>
    /// <remarks>
    /// This is used for tasks that are very time sensitive, and need to be completed as soon as possible. 
    /// </remarks>
    public bool SupersedeActiveTask { get; init; }

    /// <summary>
    /// Indicates that this task is not inherently dependent on its sibling tasks, allowing for parallel scheduling.
    /// Any explicit dependencies will still be respected.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool AllowParallelScheduling { get; init; }

    // TODO: Think about a option to say that multiple resources can work on this task at the same time.
    // May have a max concurrent resource count, and a min concurrent resource count.
}

public record Duration
{
    public float Optimistic { get; init; }
    public float MostLikely { get; init; }
    public float Pessimistic { get; init; }

    public override string ToString()
    {
        return $"Duration: {Optimistic}-{MostLikely}-{Pessimistic}";
    }
}

/// <summary>
/// Specify priorities for an individual on a given task, say I want person X to work on this, but if they are working on other higher priority work, we could assign this to someone else
/// </summary>
/// <param name="ResourceId">The resource who we are setting the priority</param>
/// <param name="Priority">   
/// Priority here allows for task-specific prioritization of resources, enabling nuanced resource allocation decisions.
/// </param>
public record IndividualPriority(string ResourceId, float Priority);