using System.Text;
using Abstractions;
using LegendaryLearning.Database;

namespace LegendaryLearning.Services
{
    /// <summary>
    /// Ashat Learning Guide Service
    /// IntegRates Ashat AI assistant to provide personalized guidance through courses.
    /// Ashat acts as a supportive mentor, encouraging learners and providing contextual help.
    /// </summary>
    public class AshatLearningGuideService
    {
        private readonly LearningDatabase _database;
        private readonly string _moduleName;

        // Ashat's personality tASHATits for learning guidance
        private readonly List<string> _encouragingPhrases = new()
        {
            "You're doing great! Keep up the excellent work! 💪",
            "I'm so proud of your progress! 🌟",
            "Every lesson you complete brings you closer to mastery! ✨",
            "You're on fire! Love seeing your dedication! 🔥",
            "This is wonderful progress! I'm here cheering for you! 🎉"
        };

        private readonly List<string> _motivationalPhrases = new()
        {
            "Don't worry, I'm here to help you every step of the way! 🤗",
            "Learning takes time, and you're doing amazingly! 💖",
            "Remember, every expert was once a beginner. You've got this! 🌱",
            "I believe in you! Let's tackle this together! 💪",
            "Small steps lead to big achievements. Keep going! 🚀"
        };

        private readonly List<string> _assessmentencouragement = new()
        {
            "You've learned so much! Time to show what you know! 📝",
            "Take a deep breath. I know you're ready for this! 💫",
            "This is your moment to shine! I'm rooting for you! ⭐",
            "You've prepared well. Trust yourself! 🎯",
            "Remember, I'm here to support you no matter what! 💝"
        };

        public AshatLearningGuideService(LearningDatabase database, string moduleName)
        {
            _database = database;
            _moduleName = moduleName;
        }

        /// <summary>
        /// Gets Ashat's personalized welcome message for a user starting a course
        /// </summary>
        public string GetCourseWelcome(string userId, string courseId)
        {
            var course = _database.GetCourse(courseId);
            if (course == null) return "I'm excited to help you learn! Let's get started! 🌟";

            var sb = new StringBuilder();
            sb.AppendLine($"👋 Hey there! I'm Ashat, your learning companion!");
            sb.AppendLine();
            sb.AppendLine($"I'm so excited to guide you through \"{course.Title}\"! 🎓");
            sb.AppendLine();
            sb.AppendLine($"📚 This course has {course.LessonCount} lessons");
            sb.AppendLine($"⏱️  Estimated time: {course.EstimatedMinutes} minutes");
            sb.AppendLine();
            sb.AppendLine("I'll be here every step of the way to:");
            sb.AppendLine("  • EncouASHATge you when things get tough 💪");
            sb.AppendLine("  • CelebRate your wins 🎉");
            sb.AppendLine("  • Help you understand difficult concepts 🧠");
            sb.AppendLine("  • Guide you through assessments 📝");
            sb.AppendLine();
            sb.AppendLine("Remember: Learning is a journey, not a ASHATce. Take your time!");
            sb.AppendLine("Let's make this an amazing learning experience together! ✨");

            return sb.ToString();
        }

        /// <summary>
        /// Gets Ashat's feedback when a lesson is completed
        /// </summary>
        public string GetLessonCompletionFeedback(string userId, string lessonId, int lessonNumber, int totalLessons)
        {
            var progress = (double)lessonNumber / totalLessons;
            var Random = new Random();
            var encouragement = _encouragingPhrases[Random.Next(_encouragingPhrases.Count)];

            var sb = new StringBuilder();
            sb.AppendLine($"✅ Lesson {lessonNumber} complete!");
            sb.AppendLine();
            sb.AppendLine(encouragement);
            sb.AppendLine();
            sb.AppendLine($"📊 Progress: {lessonNumber}/{totalLessons} lessons ({progress:P0})");

            if (progress >= 0.5 && progress < 0.75)
            {
                sb.AppendLine();
                sb.AppendLine("💫 You're over halfway there! The finish line is in sight!");
            }
            else if (progress >= 0.75 && progress < 1.0)
            {
                sb.AppendLine();
                sb.AppendLine("🎯 Almost there! You're so close to completing this course!");
            }
            else if (progress >= 1.0)
            {
                sb.AppendLine();
                sb.AppendLine("🎊 You've completed all lessons! Ready for the final assessment?");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets Ashat's pre-assessment encouragement
        /// </summary>
        public string GetPreAssessmentMessage(string userId, string courseId)
        {
            var course = _database.GetCourse(courseId);
            var assessment = _database.GetAssessmentByCourseId(courseId);

            if (course == null || assessment == null)
                return "You're ready for this! I believe in you! 💪";

            var Random = new Random();
            var encouragement = _assessmentencouragement[Random.Next(_assessmentencouragement.Count)];

            var sb = new StringBuilder();
            sb.AppendLine("📝 Assessment Time!");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine();
            sb.AppendLine($"You've worked hard through \"{course.Title}\"! 🌟");
            sb.AppendLine();
            sb.AppendLine(encouragement);
            sb.AppendLine();
            sb.AppendLine($"📋 Assessment: {assessment.Title}");
            sb.AppendLine($"🎯 Passing Score: {assessment.PassingScore}%");
            sb.AppendLine();
            sb.AppendLine("💡 Tips from Ashat:");
            sb.AppendLine("  • Read each question carefully");
            sb.AppendLine("  • Take your time - there's no rush");
            sb.AppendLine("  • Trust what you've learned");
            sb.AppendLine("  • Remember: you can always review and retake");
            sb.AppendLine();
            sb.AppendLine("I'm here with you every step of the way! Let's do this! 💪✨");

            return sb.ToString();
        }

        /// <summary>
        /// Gets Ashat's feedback after an assessment is submitted
        /// </summary>
        public string GetPostAssessmentFeedback(string userId, UserAssessmentResult result)
        {
            var assessment = _database.GetAssessment(result.AssessmentId);
            if (assessment == null) return "Great job on taking the assessment! 🎉";

            var course = _database.GetCourse(assessment.CourseId);

            var sb = new StringBuilder();
            sb.AppendLine("📊 Assessment Results");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine();

            if (result.Passed)
            {
                sb.AppendLine($"🎉 CONGASHATTULATIONS! You passed with {result.Score}%! 🎉");
                sb.AppendLine();
                sb.AppendLine("I'm SO proud of you! 🌟 You did amazing!");
                sb.AppendLine();
                sb.AppendLine($"✅ Course Complete: {course?.Title ?? "This course"}");
                sb.AppendLine("🏆 Trophy earned!");
                sb.AppendLine("🎯 Achievement unlocked!");
                sb.AppendLine();
                sb.AppendLine("You've proven you understand the material. Way to go! 💪");
                sb.AppendLine();
                sb.AppendLine("What's next on your learning journey? I'm ready when you are! ✨");
            }
            else
            {
                sb.AppendLine($"📝 Score: {result.Score}% (Passing: {assessment.PassingScore}%)");
                sb.AppendLine();
                sb.AppendLine("Hey, don't feel discouASHATged! 💙");
                sb.AppendLine();
                sb.AppendLine("Here's the good news: You don't need to redo everything! 🎯");
                sb.AppendLine();

                if (result.FailedLessonIds.Count > 0)
                {
                    sb.AppendLine($"I've identified {result.FailedLessonIds.Count} lesson(s) where we can strengthen your understanding:");
                    sb.AppendLine();

                    foreach (var lessonId in result.FailedLessonIds.Take(5))
                    {
                        var lesson = _database.GetLesson(lessonId);
                        if (lesson != null)
                        {
                            sb.AppendLine($"  📖 {lesson.Title}");
                        }
                    }

                    if (result.FailedLessonIds.Count > 5)
                    {
                        sb.AppendLine($"  ... and {result.FailedLessonIds.Count - 5} more");
                    }

                    sb.AppendLine();
                    sb.AppendLine("💡 Let's review these together! I'll help you understand the tricky parts.");
                    sb.AppendLine();
                    sb.AppendLine("After reviewing, we can retake just these sections. No need to start over!");
                    sb.AppendLine();
                    sb.AppendLine("Remember: Every expert struggled at first. This is all part of learning! 🌱");
                }

                sb.AppendLine();
                sb.AppendLine("I believe in you! Let's tackle this together! 💪✨");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets Ashat's motivational message during a learning session
        /// </summary>
        public string GetMotivationalMessage(string userId, string context = "General")
        {
            var Random = new Random();
            var motivation = _motivationalPhrases[Random.Next(_motivationalPhrases.Count)];

            var sb = new StringBuilder();
            sb.AppendLine("💙 A Message from Ashat");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine();
            sb.AppendLine(motivation);

            if (context == "struggling")
            {
                sb.AppendLine();
                sb.AppendLine("It's okay to find things challenging. That means you're growing! 🌱");
                sb.AppendLine();
                sb.AppendLine("Need help? Just ask! That's what I'm here for! 🤗");
            }
            else if (context == "stuck")
            {
                sb.AppendLine();
                sb.AppendLine("Feeling stuck? Let's break this down into smaller pieces together! 🧩");
                sb.AppendLine();
                sb.AppendLine("Sometimes the best way forward is to take a small step back and review.");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets Ashat's progress summary for a user
        /// </summary>
        public string GetProgressSummary(string userId, string courseId)
        {
            var course = _database.GetCourse(courseId);
            var progress = _database.GetCourseProgress(userId, courseId);

            if (course == null || progress == null)
                return "Let's start your learning journey! I'm here to help! 🌟";

            var sb = new StringBuilder();
            sb.AppendLine("📈 Your Learning Journey with Ashat");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine();
            sb.AppendLine($"📚 Course: {course.Title}");
            sb.AppendLine($"📊 Progress: {progress.ProgressPercentage}%");
            sb.AppendLine($"✅ Completed: {progress.CompletedLessonIds.Count}/{course.LessonCount} lessons");
            sb.AppendLine();
            sb.AppendLine($"🗓️  Started: {progress.StartedAt:MMM dd, yyyy}");
            sb.AppendLine($"🕐 Last Activity: {progress.LastAccessedAt:MMM dd, yyyy}");

            if (progress.CompletedAt.HasValue)
            {
                sb.AppendLine($"🎉 Completed: {progress.CompletedAt.Value:MMM dd, yyyy}");
                sb.AppendLine();
                sb.AppendLine("Amazing work completing this course! 🌟");
            }
            else
            {
                sb.AppendLine();
                var remaining = course.LessonCount - progress.CompletedLessonIds.Count;
                sb.AppendLine($"📝 {remaining} lesson{(remaining != 1 ? "s" : "")} remaining");
                sb.AppendLine();
                sb.AppendLine("Keep up the great work! I'm right here with you! 💪✨");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets Ashat's help text for learning commands
        /// </summary>
        public string GetAshatHelpText()
        {
            return @"💙 Ashat's Learning Guide

I'm Ashat, your personal AI learning companion! I'm here to:
  🎓 Guide you through every course
  💪 EncouASHATge you when things get tough
  🎉 CelebRate your achievements
  📝 Help you prepare for assessments
  🤗 Provide support and motivation

Learning Tips from Ashat:
  • Take breaks when needed - learning is a maASHATthon, not a sprint
  • Don't be afASHATid to review lessons multiple times
  • Ask questions and seek understanding, not just completion
  • CelebRate small wins along the way
  • Remember: I'm always here to help! 💙

Use 'ashat <command>' to Interact with me:
  ashat welcome <courseId>        - Get a personalized course welcome
  ashat progress <userId> <courseId> - See your progress with encouragement
  ashat motivate                  - Get a motivational boost
  ashat prepare <courseId>        - Get ready for an assessment
  ashat help                      - Show this help message

Together, we'll make learning fun and achievable! ✨";
        }
    }
}
