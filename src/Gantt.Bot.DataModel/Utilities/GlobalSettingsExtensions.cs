namespace Gantt.Bot.DataModel.Utilities;

public static class GlobalSettingsExtensions
{
    /// <summary>
    /// Converts a given date to the equivalent number of workdays relative to the project start date.
    /// </summary>
    public static int ConvertDateToWorkday(this GlobalSettings settings, DateTime date)
    {
        return ConvertDateRangeToWorkday(settings, settings.ProjectStartDate, date);
    }
    
    /// <summary>
    /// Converts a specific calendar date range to the equivalent number of workdays relative to a given start date.
    /// </summary>
    public static int ConvertDateRangeToWorkday(this GlobalSettings settings, DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
        {
            return 0;
        }

        var workdays = 0;
        var currentDate = startDate;
        while (currentDate < endDate)
        {
            if (settings.IsWorkingDay(currentDate))
            {
                workdays++;
            }

            currentDate = currentDate.AddDays(1);
        }

        return workdays;
    }

    public static DateTime ConvertWorkingDaysToDate(this GlobalSettings settings, double duration)
    {
        return ConvertWorkingDaysToDate(settings, settings.ProjectStartDate, duration);
    }

    /// <summary>
    /// Converts a start date and a duration in days to a finish date, considering only working days.
    /// </summary>
    public static DateTime ConvertWorkingDaysToDate(this GlobalSettings settings, DateTime startDate, double duration)
    {
        var endDate = startDate;
        var addedDays = 0;

        while (addedDays < duration)
        {
            endDate = endDate.AddDays(1);

            // Check if the current day is a working day
            if (settings.IsWorkingDay(endDate))
            {
                addedDays++;
            }
        }

        return endDate;
    }

    /// <summary>
    /// Checks if a given day is a working day, considering weekends and holidays
    /// </summary>
    public static bool IsWorkingDay(this GlobalSettings settings, DateTime date)
    {
        // Check if the day is a weekend
        var isWeekend = (date.DayOfWeek == DayOfWeek.Saturday) || (date.DayOfWeek == DayOfWeek.Sunday);
        if (!isWeekend && settings.DaysInWorkWeek < 7) // Adjust for non-standard work weeks
        {
            isWeekend = ((int)date.DayOfWeek < (int)settings.StartDay) ||
                        ((int)date.DayOfWeek >= (int)settings.StartDay + settings.DaysInWorkWeek);
        }

        var isHoliday = settings.Holidays.Any(holiday => holiday.Date.Date == date.Date);
        return !isWeekend && !isHoliday;
    }
}