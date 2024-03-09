using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Gantt.Bot.DataModel;
using Gantt.Bot.Scheduler;
using Gantt.Bot.Scheduler.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Gantt.Bot.Scheduler.Helpers;

namespace Gantt.Bot.Api;

public static class SimulationApi
{
    public static IResult RunSimulation([FromBody] SimulationRequest request)
    {
        if (request.Resources.Count == 0)
        {
            return TypedResults.BadRequest("No resources provided");
        }

        var graph = TaskGraph.Create(request.Tasks, request.ConfidenceLevel, request.Resources, request.Settings);
        graph.ScheduleResources();

        TaskGraph? taskGraph2 = null;
        string graphTable = string.Empty;

        if (request.RangeConfidenceLevel.HasValue)
        {
            taskGraph2 = TaskGraph.Create(request.Tasks, request.RangeConfidenceLevel.Value, request.Resources,
                request.Settings);
            taskGraph2.ScheduleResources();

            if (request.RangeConfidenceLevel.Value > request.ConfidenceLevel)
            {
                graphTable = graph.DumpTable(taskGraph2);
            }
            else
            {
                graphTable = taskGraph2.DumpTable(graph);
            }
        }
        else
        {
            graphTable = graph.DumpTable();
        }

        SimulationResult result = new()
        {
            Tasks = graph.AllTasksLookup.Values.ToImmutableList(),
            ProjectSchedule = graph.DumpGantt(GraphGroupBy.Project),
            MilestoneSchedule = graph.DumpGantt(GraphGroupBy.Milestone),
            ResourceSchedule = graph.DumpGantt(GraphGroupBy.Resource),
            ResourceProjectSchedule = graph.DumpGantt(GraphGroupBy.ResourceAndProject),
            TaskMap = graph.GenerateGraphMarkdown(),
            TaskMapDepth = graph.GenerateGraphMarkdownDepthFirst(),
            Table = graphTable
        };

        return TypedResults.Ok(result);
    }
}

public record SimulationRequest
{
    [Required, Range(0, 1)] public float ConfidenceLevel { get; set; }
    [Range(0, 1)] public float? RangeConfidenceLevel { get; set; }
    [Required] public ImmutableList<TaskItem> Tasks { get; set; } = ImmutableList<TaskItem>.Empty;
    [Required] public GlobalSettings Settings { get; set; } = new();
    [Required] public ImmutableList<Resource> Resources { get; set; } = ImmutableList<Resource>.Empty;
}

public record SimulationResult
{
    public ImmutableList<SchedulingTask> Tasks { get; set; } = ImmutableList<SchedulingTask>.Empty;
    public string ProjectSchedule { get; set; } = String.Empty;
    public string ResourceSchedule { get; set; } = String.Empty;

    public string ResourceProjectSchedule { get; set; } = String.Empty;

    //public string ResourceAvailability { get; set; }= String.Empty;
    public string TaskMap { get; set; } = String.Empty;
    public string MilestoneSchedule { get; set; } = string.Empty;
    public string TaskMapDepth { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
}