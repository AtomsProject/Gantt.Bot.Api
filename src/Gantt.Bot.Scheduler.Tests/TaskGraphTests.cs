using System.Collections.Immutable;
using System.Numerics;
using Gantt.Bot.Scheduler.Model;
using Gantt.Bot.Scheduler.Tests.MockData;

namespace Gantt.Bot.Scheduler.Tests;

public class TaskGraphTests
{
    [Test]
    public void VectorSize()
    {
        var columns = Vector<float>.Count;
        Console.WriteLine($"Number of float columns in a Vector: {columns}");
    }

    [Test]
    public void BuildTaskGraph()
    {
        // Arrange
        var settings = MockGlobalSettings.Build();
        var r = MockResources.Build();
        var tasks = MockTask.Build(settings.WorkTypes).WithNewEmployee(r, settings.ProjectStartDate);

        var graph = TaskGraph.Create(tasks, 0.9f, r, settings, new MockDurationSimulation());
        Console.WriteLine(graph.GenerateGraphMarkdown());
        // Assert
        Assert.Pass();
    }

    [Test]
    public void ScheduleTasks()
    {
        // Arrange
        var settings = MockGlobalSettings.Build();
        var r = MockResources.Build();
        var tasks = MockTask.Build(settings.WorkTypes).WithNewEmployee(r, settings.ProjectStartDate);

        var graph = TaskGraph.Create(tasks, 0.9f, r,settings, new MockDurationSimulation());
        graph.ScheduleResources();

        Console.WriteLine(graph.DumpGantt(GraphGroupBy.Project));
        // Assert
        Assert.Pass();
    }
}