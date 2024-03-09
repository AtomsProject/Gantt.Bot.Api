using Gantt.Bot.DataModel;

namespace Gantt.Bot.Scheduler.Tests.MockData;

public static class MockGlobalSettings
{
    public static readonly DateTime ProjectStartDate = new(2022, 1, 1);

    public static GlobalSettings Build()
    {
        return new GlobalSettings
        {
            ProjectStartDate = ProjectStartDate,
            Holidays =
            [
                new() { Date = new DateTime(2022, 1, 1), Description = "New Year's Day" },
                new() { Date = new DateTime(2022, 4, 15), Description = "Good Friday" },
                new() { Date = new DateTime(2022, 4, 18), Description = "Easter Monday" },
                new() { Date = new DateTime(2022, 5, 2), Description = "Early May Bank Holiday" },
            ],
            StartDay = DayOfWeek.Monday,
            DaysInWorkWeek = 5,
            WorkTypes =
            [
                new() { Id = "UI", Name = "JS Dev" },
                new() { Id = "API", Name = "API Dev" },
                new() { Id = "DevOps", Name = "SRE" },
                new() { Id = "ML", Name = "Machine Learning" },
            ]
        };
    }
}