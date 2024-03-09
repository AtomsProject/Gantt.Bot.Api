using BenchmarkDotNet.Attributes;
using Gantt.Bot.Scheduler.Helpers;

namespace Gantt.Bot.Scheduler.Benchmark;

[MemoryDiagnoser(false)]
public class MonteCarloPertBenchmark
{
    [Params(0.85f, 0.9f, 0.95f, 0.5f)] public float TargetConfidence { get; set; }

    public float Optimistic { get; set; } = 5f;
    public float MostLikely { get; set; } = 8f;
    public float Pessimistic { get; set; } = 15f;

    [Benchmark]
    public double RunSimulationMonteCarlo()
    {
        var monteCarloPertSimulation = new MonteCarloPertSimulation();
        return monteCarloPertSimulation.RunSimulation(Optimistic, MostLikely, Pessimistic, 1000, TargetConfidence);
    }

    [Benchmark]
    public (double, double) RunSimulationPert()
    {
        return PertConfidenceCalculator.CalculateDuration(Optimistic, MostLikely, Pessimistic, TargetConfidence);
    }

    [Benchmark]
    public double RunSimulationBeta()
    {
        var betaSimulation = new MonteCarloBetaSimulation();
        return betaSimulation.RunSimulation(Optimistic, MostLikely, Pessimistic, 1000, TargetConfidence);
    }
}