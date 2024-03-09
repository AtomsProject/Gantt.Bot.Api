using Gantt.Bot.DataModel;
using Gantt.Bot.Scheduler.Helpers;

namespace Gantt.Bot.Scheduler.Tests.MockData;

public sealed class MockDurationSimulation : IDurationSimulation
{
    public double? RunSimulation(Duration? duration, float targetProbability)
    {
        if (duration is null) return null;
        // Calculate mean and standard deviation of the PERT distribution
        double mean = (duration.Optimistic + 4 * duration.MostLikely + duration.Pessimistic) / 6;

        // Introduce a calibration factor derived from empirical data comparison with Monte Carlo results
        double standardDeviation = ((duration.Pessimistic - duration.Optimistic) / 6) * targetProbability;
        return mean + standardDeviation;
    }
}