using Gantt.Bot.DataModel;

namespace Gantt.Bot.Scheduler.Helpers;

public interface IDurationSimulation
{
    double? RunSimulation(Duration? duration, float targetProbability);
}