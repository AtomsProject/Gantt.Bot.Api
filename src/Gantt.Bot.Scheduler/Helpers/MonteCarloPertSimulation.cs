using Gantt.Bot.DataModel;

namespace Gantt.Bot.Scheduler.Helpers;

public class MonteCarloPertSimulation : IDurationSimulation
{
    private readonly Random _random = new Random();

    public double RunSimulation(float optimistic, float mostLikely, float pessimistic, int iterations,
        float targetConfidence)
    {
        var (mean, stdDev) =
            PertConfidenceCalculator.CalculateDuration(optimistic, mostLikely, pessimistic, targetConfidence);

        var samples = Enumerable.Range(0, iterations)
            .Select(_ => SampleNormal(mean, stdDev))
            .OrderBy(x => x)
            .ToList();

        var index = (int)(targetConfidence * iterations);
        return samples[index];
    }

    private double SampleNormal(double mean, double stdDev)
    {
        // Box-Muller transform
        var u1 = 1.0 - _random.NextDouble(); //uniform(0,1] random doubles
        var u2 = 1.0 - _random.NextDouble();
        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
        return mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
    }

    public double? RunSimulation(Duration? duration, float targetProbability)
    {
        if (duration is null) return null;

        if (Math.Abs(duration.Optimistic - duration.Pessimistic) < 0.1
            && Math.Abs(duration.Pessimistic - duration.MostLikely) < 0.1)
            return duration.Optimistic;

        return RunSimulation(duration.Optimistic, duration.MostLikely, duration.Pessimistic,
            1000, targetProbability);
    }
}
// // Usage
// var simulation = new MonteCarloPertSimulation();
// var targetConfidence = 0.95; // 95%
// var result = simulation.RunSimulation(optimistic: 5, mostLikely: 10, pessimistic: 20, iterations: 10000, targetConfidence: targetConfidence);
// Console.WriteLine($"Duration at {targetConfidence*100}% confidence: {result}");