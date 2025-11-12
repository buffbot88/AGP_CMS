using Abstractions;
using LegendaryLearning.Abstractions;
using LegendaryLearning.Database;

namespace LegendaryLearning.Services
{
    /// <summary>
    /// Service for user progress management and RaCoin rewards.
    /// </summary>
    public class ProgressService : IProgressService
    {
        private readonly LearningDatabase _database;
        private readonly ILessonService _lessonService;
        private readonly ICourseService _courseService;
        private readonly IAchievementService _achievementService;
        private readonly ITrophyService _trophyService;
        private readonly string _moduleName;
        private IRaCoinModule? _racoinModule;

        public ProgressService(
            LearningDatabase database,
            ILessonService lessonService,
            ICourseService courseService,
            IAchievementService achievementService,
            ITrophyService trophyService,
            string moduleName)
        {
            _database = database;
            _lessonService = lessonService;
            _courseService = courseService;
            _achievementService = achievementService;
            _trophyService = trophyService;
            _moduleName = moduleName;
        }

        /// <summary>
        /// Set the RaCoin module for awarding rewards on course completion.
        /// </summary>
        public void SetRaCoinModule(IRaCoinModule? racoinModule)
        {
            _racoinModule = racoinModule;
        }

        public async Task<bool> CompleteLessonAsync(string userId, string lessonId)
        {
            await Task.CompletedTask;

            var lesson = await _lessonService.GetLessonByIdAsync(lessonId);
            if (lesson == null)
            {
                return false;
            }

            var progress = _database.GetCourseProgress(userId, lesson.CourseId);

            if (progress == null)
            {
                progress = new CourseProgress
                {
                    UserId = userId,
                    CourseId = lesson.CourseId,
                    StartedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                };
            }

            if (!progress.CompletedLessonIds.Contains(lessonId))
            {
                progress.CompletedLessonIds.Add(lessonId);
                progress.LastAccessedAt = DateTime.UtcNow;

                // Calculate progress percentage
                var totalLessons = ((LessonService)_lessonService).GetTotalLessonCount(lesson.CourseId);
                if (totalLessons > 0)
                {
                    progress.ProgressPercentage = (int)((double)progress.CompletedLessonIds.Count / totalLessons * 100);

                    // Check if course completed
                    if (progress.ProgressPercentage == 100 && progress.CompletedAt == null)
                    {
                        progress.CompletedAt = DateTime.UtcNow;
                        await AwardCourseCompletionAsync(userId, lesson.CourseId);
                    }
                }

                _database.SaveCourseProgress(progress);

                // Award achievement for first lesson
                if (progress.CompletedLessonIds.Count == 1)
                {
                    await _achievementService.AwardAchievementAsync(userId, new LearningAchievement
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "First Steps",
                        Description = "Completed your first lesson",
                        Points = 10,
                        Type = LearningAchievementType.FirstLesson,
                        EarnedAt = DateTime.UtcNow
                    });
                }
            }

            Console.WriteLine($"[{_moduleName}] User {userId} completed lesson {lessonId}");
            return true;
        }

        public async Task<CourseProgress?> GetUserProgressAsync(string userId, string courseId)
        {
            await Task.CompletedTask;
            return _database.GetCourseProgress(userId, courseId);
        }

        public async Task<bool> HasCompletedSuperAdminCoursesAsync(string userId)
        {
            await Task.CompletedTask;

            // Get all SuperAdmin courses
            var SuperAdminCourses = await _courseService.GetCoursesAsync("SuperAdmin");
            if (SuperAdminCourses.Count == 0)
            {
                return false; // No courses to complete
            }

            // Check if user has completed all SuperAdmin courses
            foreach (var course in SuperAdminCourses)
            {
                var progress = await GetUserProgressAsync(userId, course.Id);
                if (progress == null || progress.CompletedAt == null)
                {
                    return false; // At least one course not completed
                }
            }

            return true;
        }

        public async Task<bool> MarkSuperAdminCoursesCompletedAsync(string userId)
        {
            await Task.CompletedTask;

            // Get all SuperAdmin courses
            var SuperAdminCourses = await _courseService.GetCoursesAsync("SuperAdmin");
            if (SuperAdminCourses.Count == 0)
            {
                return false;
            }

            // Mark all lessons in all SuperAdmin courses as completed
            foreach (var course in SuperAdminCourses)
            {
                var lessons = await _lessonService.GetLessonsAsync(course.Id);
                foreach (var lesson in lessons)
                {
                    await CompleteLessonAsync(userId, lesson.Id);
                }
            }

            Console.WriteLine($"[{_moduleName}] Marked all SuperAdmin courses as completed for user: {userId}");
            return true;
        }

        private async Task AwardCourseCompletionAsync(string userId, string courseId)
        {
            var course = await _courseService.GetCourseByIdAsync(courseId);
            if (course == null) return;

            // Award achievement
            var achievement = new LearningAchievement
            {
                Id = Guid.NewGuid().ToString(),
                Title = $"Completed: {course.Title}",
                Description = $"Finished all lessons in {course.Title}",
                Points = 100,
                Type = LearningAchievementType.CourseCompletion,
                EarnedAt = DateTime.UtcNow
            };
            await _achievementService.AwardAchievementAsync(userId, achievement);

            // Award trophy based on permission level
            var trophy = new LearningTrophy
            {
                Id = Guid.NewGuid().ToString(),
                Title = course.PermissionLevel switch
                {
                    "SuperAdmin" => "Master Class Trophy",
                    "Admin" => "Advanced Class Trophy",
                    _ => "Beginner Class Trophy"
                },
                Description = $"Completed {course.Title}",
                Tier = course.PermissionLevel switch
                {
                    "SuperAdmin" => LearningTrophyTier.Diamond,
                    "Admin" => LearningTrophyTier.Gold,
                    _ => LearningTrophyTier.Bronze
                },
                EarnedAt = DateTime.UtcNow,
                RelatedCourseId = courseId
            };

            await _trophyService.AwardTrophyAsync(userId, trophy);

            // Award RaCoins based on course level and estimated time
            // Formula: EstimatedMinutes * PermissionLevelMultiplier * 10 RaCoins
            // This rewards learning based on effort and complexity
            if (_racoinModule != null && Guid.TryParse(userId, out var userGuid))
            {
                decimal permissionMultiplier = course.PermissionLevel switch
                {
                    "SuperAdmin" => 3.0m,  // Master classes worth 3x
                    "Admin" => 2.0m,       // Advanced classes worth 2x
                    _ => 1.0m              // Beginner classes worth 1x
                };

                // Base reward: 10 RaCoins per estimated minute of learning
                decimal racoinReward = course.EstimatedMinutes * permissionMultiplier * 10;

                var result = await _racoinModule.TopUpAsync(
                    userGuid,
                    racoinReward,
                    $"Course completion reward: {course.Title}");

                if (result.Success)
                {
                    Console.WriteLine($"[{_moduleName}] Awarded {racoinReward} RaCoins to user {userId} for completing {course.Title}");
                }
            }
        }
    }
}
