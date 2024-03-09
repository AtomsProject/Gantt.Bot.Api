using Gantt.Bot.Scheduler.Helpers;

namespace Gantt.Bot.Scheduler.Tests;

public class ConfidenceCalculator
{
    [TestCase(5f, 10f, 20f)]
    [TestCase(2f, 4f, 8f)]
    [TestCase(1f, 2f, 4f)]
    [TestCase(1f, 5f, 20f)]
    public void CompareSimulations(float optimistic, float mostLikely, float pessimistic)
    {
        var monteCarloPertSimulation = new MonteCarloPertSimulation();
        Console.WriteLine($"{optimistic}, {mostLikely}, {pessimistic}");
        for (var i = 0.3f; i <= 1f; i += 0.05f)
        {
            var (perfDuration, stdDev) =
                PertConfidenceCalculator.CalculateDuration(optimistic, mostLikely, pessimistic, i);
            var monteCarloDuration =
                monteCarloPertSimulation.RunSimulation(optimistic, mostLikely, pessimistic, 1000, i);
            var betaSimulation = new MonteCarloBetaSimulation();
            var betaDuration = betaSimulation.RunSimulation(optimistic, mostLikely, pessimistic, 1000, i);

            Console.WriteLine(
                $"Est {i * 100:n2}%  monte Carlo: {monteCarloDuration:n5} PERT: {perfDuration:N5} stdDev: {stdDev:N5}, Beta: {betaDuration:N5}, bata monte diff: {(betaDuration - monteCarloDuration):N5}, beta monte % diff: {((betaDuration - monteCarloDuration) / monteCarloDuration) * 100:N2}%");
        }

        Assert.Pass();
    }
}