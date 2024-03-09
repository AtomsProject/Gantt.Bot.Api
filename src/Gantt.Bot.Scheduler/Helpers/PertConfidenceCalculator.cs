namespace Gantt.Bot.Scheduler.Helpers;

public class PertConfidenceCalculator
{
    public static (double targetDuration, double standardDeviation) CalculateDuration(float optimistic,
        float mostLikely, float pessimistic, float targetConfidence)
    {
        // Calculate mean and standard deviation of the PERT distribution
        double mean = (optimistic + 4 * mostLikely + pessimistic) / 6;

        // Introduce a calibration factor derived from empirical data comparison with Monte Carlo results
        var calibrationFactor = 0.7; // Example factor, adjust based on empirical testing
        var standardDeviation = ((pessimistic - optimistic) / 6) * calibrationFactor;

        // Convert target confidence to z-score
        var zScore = CalculateZScore(targetConfidence);

        // Estimate duration at target confidence level
        var targetDuration = mean + zScore * standardDeviation;
        return (targetDuration, standardDeviation);
    }

    private static double CalculateZScore(double targetConfidence)
    {
        var tail = (1 + targetConfidence) / 2;
        var zScore = Math.Sqrt(2) * ErfInv(2 * tail - 1);
        return zScore;
    }

    private static double ErfInv(double x)
    {
        double tt1, tt2, lnx, sgn;
        sgn = (x < 0) ? -1.0 : 1.0;

        x = (1 - x) * (1 + x);
        lnx = Math.Log(x);

        tt1 = 2 / (Math.PI * 0.147) + 0.5 * lnx;
        tt2 = 1 / (0.147) * lnx;

        return sgn * Math.Sqrt(-tt1 + Math.Sqrt(tt1 * tt1 - tt2));
    }
}