using MathNet.Numerics.Distributions;

namespace Gantt.Bot.Scheduler.Helpers;

public class MonteCarloBetaSimulation
{
    private readonly Random _random = new();

    public double RunSimulation(float optimistic, float mostLikely, float pessimistic, int iterations,
        float targetConfidence)
    {
        // Define beta distribution parameters based on your mapping strategy
        var (alpha, beta) = ComputeAlphaBeta(optimistic, mostLikely, pessimistic);

        // Define the range of the distribution based on optimistic and pessimistic
        double scale = pessimistic - optimistic;
        double shift = optimistic;

        var samples = Enumerable.Range(0, iterations)
            .Select(_ => BetaSample(alpha, beta, scale, shift))
            .OrderBy(x => x)
            .ToList();

        var index = (int)(targetConfidence * iterations);
        return samples[index];
    }

    private double BetaSample(double alpha, double beta, double scale, double shift)
    {
        // Sample from a beta distribution and scale/translate the sample
        var sample = Beta.Sample(_random, alpha, beta);
        return sample * scale + shift;
    }

    public static (double Alpha, double Beta) ComputeAlphaBeta(double optimistic, double mostLikely, double pessimistic)
    {
        // Calculate mean and variance of PERT
        var meanPert = (optimistic + 4 * mostLikely + pessimistic) / 6;
        var variancePert = Math.Pow((pessimistic - optimistic) / 6, 2);

        // Normalize to [0, 1]
        var scale = pessimistic - optimistic;
        var meanNormalized = (meanPert - optimistic) / scale;
        var varianceNormalized = variancePert / Math.Pow(scale, 2);

        // Solve for alpha and beta
        var alpha = ((1 - meanNormalized) / varianceNormalized - 1 / meanNormalized) * Math.Pow(meanNormalized, 2);
        var beta = alpha * (1 / meanNormalized - 1);

        return (Alpha: alpha, Beta: beta);
    }
}