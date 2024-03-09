using System.Collections.Immutable;
using Bogus;
using Gantt.Bot.DataModel;

namespace Gantt.Bot.Scheduler.Tests.MockData;

public static class MockTask
{
    private const float SubProjectProbability = 0.7f;
    private static readonly TaskDurationDistributor TaskDurationDistributor = new(97263624);

    /// <summary>
    /// Add On-boarded employee to the task list.
    /// </summary>
    public static ImmutableList<TaskItem> WithNewEmployee(this ImmutableList<TaskItem> tasks,
        IReadOnlyCollection<Resource> resources,
        DateTime projectStartDate)
    {
        var newEmployees = resources.Where(r => r.StartDate > projectStartDate).ToList();
        var onboardingTasks = new List<TaskItem>();

        var onboardingRootTask = new TaskItem
        {
            Id = "OnBoarding",
            Name = "Employee Onboarding",
            Priority = 1000,
            AllowParallelScheduling = true,
        };

    
        onboardingTasks.Add(onboardingRootTask);
        var onboardingRootTaskIndex = 0;
        foreach (var newEmployee in newEmployees)
        {
            var taskId = $"OnBoarding-{newEmployee.Id}";
            var taskName = $"Onboarding {newEmployee.Name}";

            var onBoardingTask = new TaskItem
            {
                Id = taskId,
                Name = taskName,
                AllowParallelScheduling = true,
                ParentTaskId = onboardingRootTask.Id,
                SiblingOrdinal = onboardingRootTaskIndex++
            };

            TaskItem[] childTasks =
            [
                new TaskItem
                {
                    Id = $"{taskId}-StartDate",
                    Name = $"{newEmployee.Id} StartDate",
                    IsMilestone = true,
                    StartAfter = newEmployee.StartDate,
                    ParentTaskId = onBoardingTask.Id,
                    SiblingOrdinal = 10,
                },
                new TaskItem
                {
                    Id = $"{taskId}-Orientation",
                    Name = $"{taskName} Orientation",
                    Duration = new Duration { Optimistic = 1, MostLikely = 2, Pessimistic = 3 },
                    WorkTypeId = "Orientation",
                    IndividualPriorities = [new IndividualPriority(newEmployee.Id, 0)],
                    SupersedeActiveTask = true,
                    StartAfter = newEmployee.StartDate,
                    AllowParallelScheduling = true,
                    ParentTaskId = onBoardingTask.Id,
                    SiblingOrdinal = 20
                },
                new TaskItem
                {
                    Id = $"{taskId}-Training",
                    Name = $"{taskName} Training",
                    Duration = new Duration { MostLikely = 5, Optimistic = 5, Pessimistic = 5 },
                    IndividualPriorities = [new IndividualPriority(newEmployee.Id, 1)],
                    StartAfter = newEmployee.StartDate,
                    WorkTypeId = "Training",
                    AllowParallelScheduling = true,
                    ParentTaskId = onBoardingTask.Id,
                    SiblingOrdinal = 30,
                    // TODO: Need a way of saying these task MUST run in parallel.
                    // This could maybe happen if we had a dependency on another task starting
                    // Then the high priority and SupersedeActiveTask would make sure that the
                    // task is started as soon as possible.
                    //
                    // For now, we will just set start after date to the higher date.
                }
            ];

            onboardingTasks.Add(onBoardingTask);
            onboardingTasks.AddRange(childTasks);
        }

        return tasks.Concat(onboardingTasks).ToImmutableList();
    }

    public static ImmutableList<TaskItem> Build(
        IReadOnlyCollection<WorkType> workTypes,
        int projectCount = 4,
        int seed = 97263644)
    {
        var r = new Randomizer(seed);
        var taskList = new List<TaskItem>();

        for (var i = 0; i < projectCount; i++)
        {
            var projectId = $"P{i + 1}";
            var projectName = $"Project {r.Words(2)}".Replace(',', '-');
           
            var hasSubProject = r.Bool(SubProjectProbability);
            
            var project = new TaskItem
            {
                Id = projectId,
                Name = projectName,
                Priority = r.Int(10, 80),   
            };
            taskList.Add(project);

            if (!hasSubProject)
            {
                taskList.AddRange(BuildWorkTasks(projectId, projectName, r, workTypes));
            }
            else
            {
                var subProjectCount = r.Int(1, 6);
                var hasMilestones = r.Bool(subProjectCount / 6f); // more subprojects, more chance of milestone
                for (var j = 0; j < subProjectCount; j++)
                {
                    var isMilestone = hasMilestones && r.Bool();
                    var taskId = $"{projectId}-SP{j + 1}";
                    var taskName = $"{projectName} | {r.Words(2)}".Replace(',', '-');
                    var subProject = new TaskItem
                    {
                        Id = taskId,
                        Name = taskName,
                        AllowParallelScheduling = r.Bool(0.2f),
                        IsMilestone = isMilestone,
                        Priority = isMilestone ? r.Int(10, 80) : default,
                        ParentTaskId = project.Id,
                        SiblingOrdinal = j * 100
                    };
                    taskList.Add(subProject);
                    taskList.AddRange(BuildWorkTasks(subProject.Id, taskName, r, workTypes));
                }
            }
        }

        return taskList.ToImmutableList();
    }

    private static IEnumerable<TaskItem> BuildWorkTasks(
        string parentTaskId,
        string projectName,
        Randomizer r,
        IReadOnlyCollection<WorkType> workTypes)
    {
        var take = r.Int(1, workTypes.Count);
        if (take != workTypes.Count)
        {
            workTypes = r.Shuffle(workTypes).Take(take).ToList();
        }

        var incrementor = 1;
        // Add option IndividualPriorities for a specific type.
        foreach (var workType in workTypes)
        {
            var task = new TaskItem
            {
                Id = $"{parentTaskId}-{workType.Id}",
                Name = $"{projectName} > {workType.Name}",
                WorkTypeId = workType.Id,
                Duration = BuildDuration(r),
                ParentTaskId = parentTaskId,
                SiblingOrdinal = 100 * incrementor++
            };

            yield return task;
        }
    }

    private static Duration BuildDuration(Randomizer r, int? probableSize = null)
    {
        var probable = probableSize is not null
            ? r.Int((int)(probableSize * .75), (int)(probableSize * 1.25))
            : TaskDurationDistributor.GetWeightedFibonacciDuration();
        var optimistic = r.Int((int)(probable * .2), (int)(probable * .8));
        var pessimistic = r.Int((int)(probable * 1.2), (int)(probable * 3d));
        return new Duration { Optimistic = optimistic, Pessimistic = pessimistic, MostLikely = probable };
    }
}