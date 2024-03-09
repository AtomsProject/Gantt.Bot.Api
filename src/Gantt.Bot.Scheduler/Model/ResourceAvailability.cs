using System.Collections.Immutable;
using Gantt.Bot.DataModel;
using Gantt.Bot.DataModel.Utilities;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Gantt.Bot.Scheduler.Model;

/// <summary>
/// Maintains a list of resources and their availability.
/// </summary>
public class ResourceAvailability
{
    private readonly GlobalSettings _settings;
    private readonly ImmutableList<Resource> _resources;

    /// <summary>
    /// This is a matrix of availability for each day of the simulation.
    /// Each value of the vector is the availability of the resource for that day.
    /// The index of the list is the work day of the simulation.
    /// work days exclude weekends and holidays, day zero is ProjectStartDate from <see cref="GlobalSettings"/>.
    /// </summary>
    private readonly List<Vector> _availabilityMatrix = new();

    private readonly Dictionary<string, Vector> _durationFactorMatrix = new();

    public IReadOnlyList<Vector> GetAvailabilityMatrix() => _availabilityMatrix;

    private ResourceAvailability(GlobalSettings settings, ImmutableList<Resource> resources)
    {
        _settings = settings;
        _resources = resources;

        _settings.WorkTypes.ForEach(w =>
        {
            var durationFactor = BuildDurationFactorForWorkType(w.Id);
            _durationFactorMatrix.Add(w.Id, durationFactor);
        });
    }

    public static ResourceAvailability Create(GlobalSettings settings, ImmutableList<Resource> resources)
    {
        var instance = new ResourceAvailability(settings, resources);

        // Look through the resources, and mark the availability for any Unavailable Periods listed.
        for (var i = 0; i < resources.Count; i++)
        {
            var resource = resources[i];
            foreach (var absence in resource.UnavailablePeriods)
            {
                var startDay = settings.ConvertDateToWorkday(absence.StartDate);
                var duration = settings.ConvertDateRangeToWorkday(absence.StartDate, absence.EndDate);
                instance.BlockSchedulingAvailability(new(startDay, duration, i, resource.Name));
            }

            if (resource.StartDate > settings.ProjectStartDate)
            {
                var duration = settings.ConvertDateToWorkday(resource.StartDate);
                instance.BlockSchedulingAvailability(new(0, duration, i, resource.Name));
            }
        }

        return instance;
    }

    public void GetAvailabilityMatrix(int duration, int startDay, string? taskWorkTypeId)
    {
        // Work in progress...

        // Going to run all the resources through the availability matrix at once.

        var durationMatrix = Vector.Build.Dense(_resources.Count, duration);
        if (taskWorkTypeId is not null)
        {
            if (!_durationFactorMatrix.TryGetValue(taskWorkTypeId, out var durationFactor))
            {
                durationFactor = BuildDurationFactorForWorkType(taskWorkTypeId);
                _durationFactorMatrix.Add(taskWorkTypeId, durationFactor);
            }

            for (int i = 0; i < durationMatrix.Count; i++)
            {
                var d = durationFactor[i];
                durationMatrix[i] = d <= 0 ? 0 : duration / d;
            }
        }

        // Use to return the start date for the resource's availability
        var startMatrix = Vector.Build.Dense(_resources.Count, -1);

        // Use to return the end date for the resource's availability
        var endMatrix = Vector.Build.Dense(_resources.Count, 0);

        // Use to for tracking the remaining duration for the resource
        // when it hit's zero we know we have found a slot for the duration.
        var remainingMatrix = durationMatrix.Clone(); // We will keep going until this is all zero.

        // We are going to start from the start day, and keep going until we find a slot for the duration.
        // We don't need to go past the end of the availability matrix,
        // because we know that the resources are available past that.
        for (var day = startDay; day < _availabilityMatrix.Count; day++)
        {
            var availability = _availabilityMatrix[day];
        }
    }

    private Vector BuildDurationFactorForWorkType(string taskWorkTypeId)
    {
        var durationFactor = (Vector)Vector.Build.Dense(_resources.Count, 0);
        for (int i = 0; i < _resources.Count; i++)
        {
            var resource = _resources[i];
            var workTypeAssignment = resource.WorkTypeAssignments.FirstOrDefault(r => r.WorkTypeId == taskWorkTypeId);
            if (workTypeAssignment is not null)
            {
                // We are going to divide the duration by the familiar score.
                // so we can't have a zero score, so we are going to use -1 to indicate that this resource can't do this work.
                durationFactor[i] = workTypeAssignment.FamiliarScore;
            }
        }

        return durationFactor;
    }

    /// <summary>
    /// Get the availability of a resource for a given duration.
    /// </summary>
    /// <returns>Returns the start day for the availability</returns>
    public SchedulingAvailability GetAvailability(int resourceIndex, int duration, int startDay, string? taskWorkTypeId)
    {
        var r = _resources[resourceIndex];
        if (taskWorkTypeId is not null)
        {
            // Look for this work time in the resource's work type assignments.
            // So we can adjust the duration using there familiar score.

            var workTypeAssignment = r.WorkTypeAssignments.FirstOrDefault(w => w.WorkTypeId == taskWorkTypeId);
            if (workTypeAssignment is not null)
            {
                // If familiar score is 1, then the resulting duration is the same.
                // if FamiliarScore is 0.5, then the duration should be 150% of the original duration.
                // if FamiliarScore is 0.75, then the duration should be 125% of the original duration.
                duration = (int)(duration / workTypeAssignment.FamiliarScore);
            }
        }

        // TODO: Do we want to return information here about this leaving a gap.
        // TODO: We may wan to track what tasks are scheduled for this resource, so we can move them if we want to schedule something else.

        // If we haven't created the availability matrix for this day,
        // that means we haven't scheduled anything yet, so we know it's free.
        if (_availabilityMatrix.Count < startDay)
            return new(startDay, duration, resourceIndex, r.Name);

        // Outer loop is going to move our start day along.
        // 1. We start from the start day, then look for the first day that has enough availability.
        // That's our new proposed start day, if this start day is past _availabilityMatrix.Count,
        // we know we haven't scheduled anything yet, so we know it's free.
        //
        // 2. from our new start date, we will look at the next duration of days, and see if we have enough availability.
        // If we do, we return the start day. if we reach the end of the _availabilityMatrix then we also return start day
        // 
        // 3. we find something that's already blocked, we then to back to the outer loop and try again.
        // kicking it off from the first day that we found that was blocked.
        //
        // TODO: It could be that the person is not available due to taking a day off of work
        // If they are blocked because of PTO, we can still schedule around that.
        // Also if they other task, or this task is something that can be run concurrently, we can still schedule around that.
        //
        // 3a. For now, we are going to use this over the strictest of step 3 above.
        // If we find a block of the that's not available, we are simply going to find the next day that is free
        // and add the number of blocked day to the duration of this task.
        // We will want some threshold for this, but the idea is that the other task will just move up in the schedule
        // but for the purposes of this simulation, we are going to keep it simple.

        var hasStarted = false;
        var proposedStartDay = startDay;
        var adjustedDuration = duration;
        var reamingDuration = duration;
        var consecutiveBlockedDays = 0;
        var allowedBlockedDays = Math.Max(2, duration / 4);

        for (var day = startDay; day < _availabilityMatrix.Count; day++)
        {
            var availability = _availabilityMatrix[day][resourceIndex];
            if (availability <= 0)
            {
                consecutiveBlockedDays++;

                if (hasStarted)
                {
                    adjustedDuration += 1;
                }

                continue;
            }

            // If the resource is unavailable relatively long period during the task
            // that's not ideal, so we will delay they ability to start the task.

            if (consecutiveBlockedDays > allowedBlockedDays)
            {
                // Reset the start day, and the duration.
                hasStarted = false;
                adjustedDuration = duration;
                reamingDuration = duration;
            }
            consecutiveBlockedDays = 0;
            
            if (!hasStarted)
            {
                proposedStartDay = day;
                hasStarted = true;
            }

            reamingDuration--;

            if (reamingDuration <= 0)
            {
                return new(proposedStartDay, adjustedDuration, resourceIndex,  r.Name);
            }
        }

        if (hasStarted)
        {
            return new(proposedStartDay, adjustedDuration + reamingDuration, resourceIndex,  r.Name);
        }

        // If we haven't scheduled anything yet, we know it's free.
        return new(_availabilityMatrix.Count, duration, resourceIndex, r.Name);
    }

    /// <summary>
    /// Block the availability of a resource for a given duration.
    /// </summary>
    public void BlockSchedulingAvailability(SchedulingAvailability availability)
    {
        var endDay = availability.EndDay;

        while (endDay > _availabilityMatrix.Count - 1)
        {
            _availabilityMatrix.Add(
                (Vector)Vector.Build.Dense(_resources.Count, 1.0));
        }

        for (var i = availability.StartDay; i <= endDay; i++)
        {
            _availabilityMatrix[i][availability.ResourceIndex] = 0;
        }
    }
}

public record SchedulingAvailability
{
    public SchedulingAvailability(int startDay, int duration, int resourceIndex, string resourceName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startDay, nameof(startDay));
        ArgumentOutOfRangeException.ThrowIfNegative(duration, nameof(duration));
        ArgumentOutOfRangeException.ThrowIfNegative(resourceIndex, nameof(resourceIndex));

        StartDay = startDay;
        Duration = duration;
        ResourceIndex = resourceIndex;
        ResourceName = resourceName;
    }

    public string? ResourceName { get; init; }
    
    public int StartDay { get; }
    public int Duration { get; }
    public int ResourceIndex { get; }

    public double ScoreOrg { get; init; }
    public double Score { get; init; }

    public int EndDay => StartDay + Duration;
    public double DelayFactor { get; set; }
    public double DurationFactor { get; set; }

    public override string ToString()
    {
        return
            $"Start: {StartDay} End: {EndDay} ({Duration}) Resource: {ResourceName} [{ResourceIndex}] Score: {Score:N2} [{ScoreOrg:N2}] DelayFactor: {DelayFactor:N2} DurationFactor: {DurationFactor:N2}";
    }
}