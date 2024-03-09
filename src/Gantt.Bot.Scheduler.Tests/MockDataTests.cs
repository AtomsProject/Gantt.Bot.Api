using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Gantt.Bot.DataModel;
using Gantt.Bot.Scheduler.Tests.MockData;

namespace Gantt.Bot.Scheduler.Tests;

public class MockDataTests
{
    [Test]
    public void DumpTasks()
    {
        var g = MockGlobalSettings.Build();
        var r = MockResources.Build();
        var tasks = MockTask.Build(g.WorkTypes);

        // convert tasks to json
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(tasks, jsonSerializerOptions);
        Console.WriteLine(json);
    }
}