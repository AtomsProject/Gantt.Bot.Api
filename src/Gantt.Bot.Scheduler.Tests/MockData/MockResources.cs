using System.Collections.Immutable;
using Gantt.Bot.DataModel;

namespace Gantt.Bot.Scheduler.Tests.MockData;

public static class MockResources
{
    public static ImmutableList<Resource> Build()
    {
        return
        [
            new()
            {
                Id = "UI1",
                Name = "Sr JS Dev",
                StartDate = new DateTime(2020, 1, 1),
                WorkTypeAssignments =
                [
                    new() { PreferenceFactor = 1, WorkTypeId = "UI", FamiliarScore = 1 },
                    new() { PreferenceFactor = 0.3, WorkTypeId = "API", FamiliarScore = 0.7 },
                    new() { PreferenceFactor = 1, WorkTypeId = "Orientation", FamiliarScore = .7 },
                    new() { FamiliarScore = 1, WorkTypeId = "Training", PreferenceFactor = 1 }
                ]
            },
            new()
            {
                Id = "UI2",
                Name = "JS Dev",
                StartDate = new DateTime(2022, 3, 1),
                WorkTypeAssignments =
                [
                    new() { PreferenceFactor = 1, WorkTypeId = "UI", FamiliarScore = 1 },
                    new() { PreferenceFactor = 0.6, WorkTypeId = "API", FamiliarScore = 0.4 },
                    new() { FamiliarScore = 1, PreferenceFactor = 1, WorkTypeId = "Training" }
                ]
            },
            new()
            {
                Id = "API1",
                Name = "Sr API Dev",
                StartDate = new DateTime(2022, 2, 1),
                WorkTypeAssignments =
                [
                    new() { PreferenceFactor = .5, WorkTypeId = "UI", FamiliarScore = .3 },
                    new() { PreferenceFactor = 1, WorkTypeId = "API", FamiliarScore = 0.7 },
                    new() { FamiliarScore = 1, PreferenceFactor = 1, WorkTypeId = "Training" }
                ]
            },
            new()
            {
                Id = "STAFF1",
                Name = "Staff API Dev",
                StartDate = new DateTime(2020, 1, 1),
                WorkTypeAssignments =
                [
                    new() { PreferenceFactor = .3, WorkTypeId = "UI", FamiliarScore = .9 },
                    new() { PreferenceFactor = 1, WorkTypeId = "API", FamiliarScore = 1 },
                    new() { PreferenceFactor = 1, WorkTypeId = "ML", FamiliarScore = 1 },
                    new() { PreferenceFactor = 1, WorkTypeId = "DevOps", FamiliarScore = .7 },
                    new() { PreferenceFactor = 1, WorkTypeId = "Orientation", FamiliarScore = .7 },
                    new() { FamiliarScore = 1, PreferenceFactor = 1, WorkTypeId = "Training" }
                ],
                UnavailablePeriods =
                {
                    new()
                    {
                        StartDate = new DateTime(2022, 3, 1), EndDate = new DateTime(2022, 3, 15), Reason = "Vacation"
                    }
                }
            },
            new()
            {
                Id = "SRE1",
                Name = "Sr SRE",
                StartDate = new DateTime(2022, 1, 1),
                WorkTypeAssignments =
                [
                    new() { PreferenceFactor = 1, WorkTypeId = "DevOps", FamiliarScore = 1 },
                    new() { PreferenceFactor = .2, WorkTypeId = "API", FamiliarScore = 0.5 },
                    new() { FamiliarScore = 1, PreferenceFactor = 1, WorkTypeId = "Training" }
                ]
            }
        ];
    }
}