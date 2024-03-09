using System.Collections.Immutable;

namespace Gantt.Bot.DataModel;

public class GlobalSettings
{
    public DateTime ProjectStartDate { get; init; }
    public ImmutableList<Holiday> Holidays { get; init; } = ImmutableList<Holiday>.Empty;
    public DayOfWeek StartDay { get; init; } = DayOfWeek.Monday; // Default to Monday, customizable.
    public int DaysInWorkWeek { get; init; } = 5; // Default to 5, customizable for different work patterns.
    public ImmutableList<WorkType> WorkTypes { get; init; } = ImmutableList<WorkType>.Empty;
}

public class WorkType
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
}

public class Holiday
{
    public DateTime Date { get; init; }
    public string? Description { get; set; }
}