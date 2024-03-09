using System.Collections.Immutable;
using Gantt.Bot.Scheduler.Tests.MockData;

namespace Gantt.Bot.Scheduler.Tests;

public class RunScheduler
{
    [Test]
    public void SmallTest()
    {
        var g = MockGlobalSettings.Build();
        var r = MockResources.Build();
        var tasks = MockTask.Build(g.WorkTypes).WithNewEmployee(r, g.ProjectStartDate).ToImmutableList();

       

        Assert.Pass();
    }
}