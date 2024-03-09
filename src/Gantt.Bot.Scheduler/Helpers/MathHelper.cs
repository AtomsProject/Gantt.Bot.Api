using MathNet.Numerics.Statistics;

namespace Gantt.Bot.Scheduler.Helpers;

public static class MathHelper
{
    public static List<double> CalculateZScores(List<double> data)
    {
        var mean = data.Mean();
        var stdDev = data.StandardDeviation();
        var zScores = data.Select(x => (x - mean) / stdDev).ToList();
        return zScores;
    }

    private const uint MaxUint = 1147483641;
    private const int MaxInt = 1147483641;
    private const double MaxDouble = 1147483641;

    public static string ToLimitString(this uint? number)
    {
        return number switch
        {
            null => "NaN",
            >= MaxUint => "\u221E",
            _ => $"{number:N0}"
        };
    }

    public static string ToLimitString(this int? number)
    {
        return number switch
        {
            null => "NaN",
            >= MaxInt => "\u221E",
            int.MinValue => "-\u221E",
            _ => $"{number:N0}"
        };
    }


    public static string ToLimitString(this uint number)
    {
        return number switch
        {
            >= MaxUint => "\u221E",
            _ => $"{number:N0}"
        };
    }

    public static string ToLimitString(this int number)
    {
        return number switch
        {
            >= MaxInt => "\u221E",
            int.MinValue => "-\u221E",
            _ => $"{number:N0}"
        };
    }


    public static string ToLimitString(this double number)
    {
        return number switch
        {
            >= MaxDouble => "\u221E",
            <= 0 => "-\u221E",
            _ => $"{number:N3}"
        };
    }
}