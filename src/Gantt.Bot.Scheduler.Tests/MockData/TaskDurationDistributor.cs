using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;

namespace Gantt.Bot.Scheduler.Tests.MockData;

public class TaskDurationDistributor
{
    private readonly int[] _fibonacciSequence = [1, 2, 3, 5, 8, 13, 21];
    private readonly Normal _normalDist;

    public TaskDurationDistributor(int seed, double mean = 3, double stdDev = 1)
    {
        // Use the seeded random source
        var randomSource = new SystemRandomSource(seed);
        // Adjust mean and stdDev if needed to fit your exact requirements
        _normalDist = new Normal(mean, stdDev, randomSource);
    }

    public int GetWeightedFibonacciDuration()
    {
        var index = Math.Round(_normalDist.Sample());

        // Ensure index is within bounds
        index = Math.Max(0, Math.Min(index, _fibonacciSequence.Length - 1));

        return _fibonacciSequence[(int)index];
    }
}