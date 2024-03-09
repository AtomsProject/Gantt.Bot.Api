using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Gantt.Bot.DataModel;

public class Resource
{
    [Required] public string Id { get; init; } = null!;
    [Required] public string Name { get; init; } = null!;
    public List<ResourceWorkTypeAssignment> WorkTypeAssignments { get; init; } = new();
    [Required] public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; } = null;
    public List<UnavailablePeriod> UnavailablePeriods { get; } = new();

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\tResource: {Name} ({Id})");
        sb.AppendLine($"\tStart: {StartDate:yyyy-MM-dd}");
        if(EndDate.HasValue) sb.AppendLine($" End: {EndDate:yyyy-MM-dd}");
        sb.AppendLine("\tWorkTypeAssignments:");
        foreach (var workTypeAssignment in WorkTypeAssignments)
        {
            sb.Append("\t\t");
            sb.AppendLine(workTypeAssignment.ToString());
        }
        sb.AppendLine("\tUnavailablePeriods:");
        foreach (var unavailablePeriod in UnavailablePeriods)
        {
            sb.Append("\t\t");
            sb.AppendLine(unavailablePeriod.ToString());
        }

        return sb.ToString();
    }
}

public class UnavailablePeriod
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? Reason { get; set; }
    
    public override string ToString()
    {
        return $"{StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd} {Reason}";
    }
}

public record ResourceWorkTypeAssignment
{
    [Required] public string WorkTypeId { get; init; } = null!;

    /// <summary>
    /// Represents how skilled or familiar the resource is in this type of work.
    /// 1 means the resource is very familiar with this type of work and will not
    /// require much time to ramp up or learn new things.
    /// </summary>
    /// <example>
    /// <para>1 = are fully ramped and accomplish tasks in-line with task estimates</para>
    /// <para>0.5 = they are somewhat familiar with this type of work, and may take around twice as long to complete tasks, compared to a fully ramped resource.</para>
    /// <para>0 = they have no expand or training with this, and should not be assigned this type of work.</para>
    /// </example>
    [Range(0, 1)]
    public double FamiliarScore { get; init; }

    /// <summary>
    /// Represents the resource's preference towards this type of work.
    /// Is this kind of work something they enjoy doing or want to do more of,
    /// or is this something they are avoiding or don't want to do more of.
    /// </summary>
    /// <example>1 = work they enjoy, 0 = wont assign this kind of work</example>
    [Range(0, 1)]
    public double PreferenceFactor { get; init; }
    
    public override string ToString()
    {
        return $"{WorkTypeId,-15} Familiar: {FamiliarScore:N5} Preference: {PreferenceFactor:N5}";
    }
}