using System.Collections.Concurrent;
using Abstractions;
using LegendaryLearning.Database;
using LegendaryLearning.Seed;
using LegendaryLearning.Services;

namespace LegendaryLearning
{
    /// <summary>
    /// Legendary User Learning Module (LULmodule)
    /// Provides self-paced, voluntary learning courses based on permission levels.
    /// This is a free school module that teaches users about the ASHATOS system.
    /// Courses auto-update when new features are added to ASHATOS.
    /// Includes trophy and achievement system for completing courses.
    /// Earns RaCoins upon course completion based on course level and duration.
    /// </summary>
    [RaModule(Category = "extensions")]
    public sealed class LegendaryUserLearningModule : ModuleBase, ILearningModule
    {
        public override string Name => "Learn ASHAT OS";

        private readonly LearningDatabase _database;
        private readonly CourseService _courseService;
        private readonly LessonService _lessonService;
        private readonly ProgressService _progressService;
        private readonly AchievementService _achievementService;
        private readonly TrophyService _trophyService;
        private readonly AssessmentService _assessmentService;
        private readonly AshatLearningGuideService _ashatGuide;

        public LegendaryUserLearningModule()
        {
            _database = new LearningDatabase();
            _courseService = new CourseService(_database);
            _lessonService = new LessonService(_database);
            _achievementService = new AchievementService(Name);
            _trophyService = new TrophyService(Name);
            _progressService = new ProgressService(
                _database,
                _lessonService,
                _courseService,
                _achievementService,
                _trophyService,
                Name);
            _assessmentService = new AssessmentService(_database, _lessonService, Name);
            _ashatGuide = new AshatLearningGuideService(_database, Name);
        }

        public override void Initialize(object? manager)
        {
            base.Initialize(manager);

            Console.WriteLine($"[{Name}] Initializing Legendary User Learning Module (LULmodule)...");
            Console.WriteLine($"[{Name}] Self-paced learning with real-time updates");
            Console.WriteLine($"[{Name}] Trophy and achievement system enabled");
            Console.WriteLine($"[{Name}] End-of-course adaptive assessments enabled");
            Console.WriteLine($"[{Name}] Ashat AI learning guide: ACTIVE 💙");

            // Get RaCoin module for rewarding course completion
            // Note: We use reflection here to avoid direct dependency on ASHATCore types,
            // maintaining the separation between LegendaryLearning and ASHATCore projects.
            if (manager != null)
            {
                try
                {
                    // Use reflection to call GetModuleByName without direct type dependency
                    var getModuleMethod = manager.GetType().GetMethod("GetModuleByName");
                    if (getModuleMethod != null)
                    {
                        var racoinModule = getModuleMethod.Invoke(manager, new object[] { "RaCoin" }) as IRaCoinModule;
                        if (racoinModule != null)
                        {
                            _progressService.SetRaCoinModule(racoinModule);
                            Console.WriteLine($"[{Name}] RaCoin reward system enabled 🪙");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{Name}] Could not initialize RaCoin rewards: {ex.Message}");
                }
            }

            var seeder = new CourseSeeder(_courseService, _lessonService, _assessmentService, Name);
            seeder.SeedInitialCourses();

            Console.WriteLine($"[{Name}] Learning Module initialized");
            Console.WriteLine($"[{Name}] Ashat is ready to guide learners through all courses!");
        }

        public override string Process(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return GetHelp();

            var command = parts[0].ToLowerInvariant();

            return command switch
            {
                "courses" when parts.Length >= 2 => ListCourses(parts[1]),
                "lessons" when parts.Length >= 2 => ListLessons(parts[1]),
                "progress" when parts.Length >= 3 => ShowProgress(parts[1], parts[2]),
                "complete" when parts.Length >= 3 => CompleteLesson(parts[1], parts[2]),
                "achievements" when parts.Length >= 2 => ShowAchievements(parts[1]),
                "trophies" when parts.Length >= 2 => ShowTrophies(parts[1]),
                "test" when parts.Length >= 2 => ShowTest(parts[1]),
                "results" when parts.Length >= 3 => ShowResults(parts[1], parts[2]),
                "ashat" when parts.Length >= 2 => ProcessAshatCommand(parts.Skip(1).ToArray()),
                "help" => GetHelp(),
                _ => GetHelp()
            };
        }

        private string GetHelp()
        {
            return @"Learn ASHATOS Module (LULmodule) Commands:
  courses <level> - List courses for permission level (User, Admin, SuperAdmin)
  lessons <courseId> - List lessons for a course
  progress <userId> <courseId> - Show user progress for a course
  complete <userId> <lessonId> - Mark a lesson as completed
  test <courseId> - Show test information for a course
  results <userId> <courseId> - Show user's assessment results for a course
  achievements <userId> - Show user achievements
  trophies <userId> - Show user trophies
  ashat <command> - Interact with Ashat, your AI learning guide 💙
  help - Show this help message

Ashat Commands:
  ashat welcome <courseId> - Get Ashat's personalized course welcome
  ashat progress <userId> <courseId> - See your progress with Ashat's encouragement
  ashat motivate - Get a motivational boost from Ashat
  ashat prepare <courseId> - Get ready for an assessment with Ashat
  ashat help - Show Ashat's detailed help";
        }

        private string ListCourses(string permissionLevel)
        {
            var task = _courseService.GetCoursesAsync(permissionLevel);
            task.Wait();
            var courses = task.Result;

            if (courses.Count == 0)
            {
                return $"No courses found for {permissionLevel} level.";
            }

            var lines = new List<string> { $"Found {courses.Count} courses for {permissionLevel}:" };
            foreach (var course in courses.OrderBy(c => c.Title))
            {
                lines.Add($"  [{course.Id}] {course.Title} - {course.LessonCount} lessons (~{course.EstimatedMinutes} min)");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string ListLessons(string courseId)
        {
            var task = _lessonService.GetLessonsAsync(courseId);
            task.Wait();
            var lessons = task.Result;

            if (lessons.Count == 0)
            {
                return $"No lessons found for course {courseId}.";
            }

            var lines = new List<string> { $"Found {lessons.Count} lessons:" };
            foreach (var lesson in lessons.OrderBy(l => l.OrderIndex))
            {
                lines.Add($"  {lesson.OrderIndex}. [{lesson.Id}] {lesson.Title} ({lesson.Type}) - ~{lesson.EstimatedMinutes} min");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string ShowProgress(string userId, string courseId)
        {
            var task = _progressService.GetUserProgressAsync(userId, courseId);
            task.Wait();
            var progress = task.Result;

            if (progress == null)
            {
                return $"No progress found for user {userId} in course {courseId}.";
            }

            return $"Progress: {progress.ProgressPercentage}% ({progress.CompletedLessonIds.Count} lessons completed)";
        }

        private string CompleteLesson(string userId, string lessonId)
        {
            var lesson = _lessonService.GetLessonByIdAsync(lessonId).Result;
            if (lesson == null)
            {
                return $"Lesson {lessonId} not found.";
            }

            var task = _progressService.CompleteLessonAsync(userId, lessonId);
            task.Wait();

            if (!task.Result)
            {
                return $"Failed to complete lesson {lessonId}.";
            }

            // Get total lessons for progress calculation
            var lessons = _lessonService.GetLessonsAsync(lesson.CourseId).Result;
            var progress = _progressService.GetUserProgressAsync(userId, lesson.CourseId).Result;

            if (progress != null)
            {
                // Get Ashat's encouraging feedback
                var ashatFeedback = _ashatGuide.GetLessonCompletionFeedback(
                    userId,
                    lessonId,
                    progress.CompletedLessonIds.Count,
                    lessons.Count);

                return ashatFeedback;
            }

            return $"Lesson {lessonId} marked as completed for user {userId}.";
        }

        private string ShowAchievements(string userId)
        {
            var task = _achievementService.GetUserAchievementsAsync(userId);
            task.Wait();
            var achievements = task.Result;

            if (achievements.Count == 0)
            {
                return $"No achievements earned yet for user {userId}.";
            }

            var lines = new List<string> { $"Earned {achievements.Count} achievements:" };
            foreach (var achievement in achievements.OrderByDescending(a => a.EarnedAt))
            {
                lines.Add($"  🏆 {achievement.Title} - {achievement.Points} pts ({achievement.EarnedAt:d})");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string ShowTrophies(string userId)
        {
            var task = _trophyService.GetUserTrophiesAsync(userId);
            task.Wait();
            var trophies = task.Result;

            if (trophies.Count == 0)
            {
                return $"No trophies earned yet for user {userId}.";
            }

            var lines = new List<string> { $"Earned {trophies.Count} trophies:" };
            foreach (var trophy in trophies.OrderByDescending(t => t.Tier))
            {
                var tierIcon = trophy.Tier switch
                {
                    LearningTrophyTier.Diamond => "💎",
                    LearningTrophyTier.Platinum => "⭐",
                    LearningTrophyTier.Gold => "🥇",
                    LearningTrophyTier.Silver => "🥈",
                    LearningTrophyTier.Bronze => "🥉",
                    _ => "🏆"
                };
                lines.Add($"  {tierIcon} {trophy.Title} ({trophy.Tier}) - {trophy.Description}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string ShowTest(string courseId)
        {
            var task = _assessmentService.GetCourseAssessmentAsync(courseId);
            task.Wait();
            var assessment = task.Result;

            if (assessment == null)
            {
                return $"No assessment found for course {courseId}.";
            }

            var lines = new List<string>
            {
                $"Assessment: {assessment.Title}",
                $"Description: {assessment.Description}",
                $"Passing Score: {assessment.PassingScore}%",
                "",
                "Complete all lessons in the course to take this assessment.",
                "If you fail specific sections, you'll only need to restudy and retake those sections."
            };

            return string.Join(Environment.NewLine, lines);
        }

        private string ShowResults(string userId, string courseId)
        {
            var task = _assessmentService.GetUserAssessmentResultsAsync(userId, courseId);
            task.Wait();
            var results = task.Result;

            if (results.Count == 0)
            {
                return $"No assessment results found for user {userId} in course {courseId}.";
            }

            var lines = new List<string> { $"Assessment Results ({results.Count} attempts):" };
            foreach (var result in results)
            {
                var status = result.Passed ? "✅ PASSED" : "❌ FAILED";
                lines.Add($"  {status} - Score: {result.Score}% ({result.AttemptedAt:g})");

                if (!result.Passed && result.FailedLessonIds.Count > 0)
                {
                    lines.Add($"    Need to restudy {result.FailedLessonIds.Count} lesson(s):");
                    foreach (var lessonId in result.FailedLessonIds.Take(5))
                    {
                        var lessonTask = _lessonService.GetLessonByIdAsync(lessonId);
                        lessonTask.Wait();
                        var lesson = lessonTask.Result;
                        if (lesson != null)
                        {
                            lines.Add($"      - {lesson.Title}");
                        }
                    }
                    if (result.FailedLessonIds.Count > 5)
                    {
                        lines.Add($"      ... and {result.FailedLessonIds.Count - 5} more");
                    }
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string ProcessAshatCommand(string[] parts)
        {
            if (parts.Length == 0) return _ashatGuide.GetAshatHelpText();

            var subCommand = parts[0].ToLowerInvariant();

            return subCommand switch
            {
                "welcome" when parts.Length >= 2 => _ashatGuide.GetCourseWelcome("user", parts[1]),
                "progress" when parts.Length >= 3 => _ashatGuide.GetProgressSummary(parts[1], parts[2]),
                "motivate" => _ashatGuide.GetMotivationalMessage("user"),
                "prepare" when parts.Length >= 2 => _ashatGuide.GetPreAssessmentMessage("user", parts[1]),
                "help" => _ashatGuide.GetAshatHelpText(),
                _ => _ashatGuide.GetAshatHelpText()
            };
        }

        // ILearningModule interface implementation
        public async Task<List<Course>> GetCoursesAsync(string permissionLevel)
        {
            return await _courseService.GetCoursesAsync(permissionLevel);
        }

        public async Task<Course?> GetCourseByIdAsync(string courseId)
        {
            return await _courseService.GetCourseByIdAsync(courseId);
        }

        public async Task<List<Lesson>> GetLessonsAsync(string courseId)
        {
            return await _lessonService.GetLessonsAsync(courseId);
        }

        public async Task<Lesson?> GetLessonByIdAsync(string lessonId)
        {
            return await _lessonService.GetLessonByIdAsync(lessonId);
        }

        public async Task<bool> CompleteLessonAsync(string userId, string lessonId)
        {
            return await _progressService.CompleteLessonAsync(userId, lessonId);
        }

        public async Task<CourseProgress?> GetUserProgressAsync(string userId, string courseId)
        {
            return await _progressService.GetUserProgressAsync(userId, courseId);
        }

        public async Task<List<LearningAchievement>> GetUserAchievementsAsync(string userId)
        {
            return await _achievementService.GetUserAchievementsAsync(userId);
        }

        public async Task<List<LearningTrophy>> GetUserTrophiesAsync(string userId)
        {
            return await _trophyService.GetUserTrophiesAsync(userId);
        }

        public async Task<(bool success, string message)> UpdateCourseAsync(Course course)
        {
            return await _courseService.UpdateCourseAsync(course);
        }

        public async Task<(bool success, string message)> UpdateLessonAsync(Lesson lesson)
        {
            return await _lessonService.UpdateLessonAsync(lesson);
        }

        public async Task<bool> HasCompletedSuperAdminCoursesAsync(string userId)
        {
            return await _progressService.HasCompletedSuperAdminCoursesAsync(userId);
        }

        public async Task<bool> MarkSuperAdminCoursesCompletedAsync(string userId)
        {
            return await _progressService.MarkSuperAdminCoursesCompletedAsync(userId);
        }

        public async Task<Assessment?> GetCourseAssessmentAsync(string courseId)
        {
            return await _assessmentService.GetCourseAssessmentAsync(courseId);
        }

        public async Task<UserAssessmentResult> SubmitAssessmentAsync(string userId, string assessmentId, Dictionary<string, string> answers)
        {
            return await _assessmentService.SubmitAssessmentAsync(userId, assessmentId, answers);
        }

        public async Task<List<UserAssessmentResult>> GetUserAssessmentResultsAsync(string userId, string courseId)
        {
            return await _assessmentService.GetUserAssessmentResultsAsync(userId, courseId);
        }

        public async Task<bool> CanTakeAssessmentAsync(string userId, string courseId)
        {
            return await _assessmentService.CanTakeAssessmentAsync(userId, courseId);
        }
    }
}
