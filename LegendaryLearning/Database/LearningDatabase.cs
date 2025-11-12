using System.Text.Json;
using Abstractions;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace LegendaryLearning.Database
{
    /// <summary>
    /// Database persistence layer for Learning Module courses, lessons, and assessments.
    /// Provides SQLite Storage for educational content and user progress.
    /// </summary>
    public class LearningDatabase : IDisposable
    {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

        public LearningDatabase(string? dbPath = null)
        {
            Batteries_V2.Init();
            _dbPath = string.IsNullOrWhiteSpace(dbPath)
                ? Path.Combine(AppContext.BaseDirectory, "Databases", "learning.sqlite")
                : dbPath;

            // Ensure Databases directory exists
            var directory = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString = $"Data Source={_dbPath}";
            EnsureSchema();
        }

        private void EnsureSchema()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // Ensure foreign keys are enforced for this connection
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            // Courses table
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Courses (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    Description TEXT,
    PermissionLevel TEXT NOT NULL DEFAULT 'User',
    Category TEXT,
    LessonCount INTEGER NOT NULL DEFAULT 0,
    EstimatedMinutes INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    PrerequisiteCourseId TEXT
);

CREATE TABLE IF NOT EXISTS Lessons (
    Id TEXT PRIMARY KEY,
    CourseId TEXT NOT NULL,
    Title TEXT NOT NULL,
    Content TEXT NOT NULL,
    OrderIndex INTEGER NOT NULL,
    EstimatedMinutes INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT,
    Type TEXT NOT NULL DEFAULT 'Reading',
    VideoUrl TEXT,
    CodeExample TEXT,
    Tags TEXT,
    FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Assessments (
    Id TEXT PRIMARY KEY,
    CourseId TEXT NOT NULL,
    Title TEXT NOT NULL,
    Description TEXT,
    PassingScore INTEGER NOT NULL DEFAULT 70,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Questions (
    Id TEXT PRIMARY KEY,
    AssessmentId TEXT NOT NULL,
    LessonId TEXT NOT NULL,
    QuestionText TEXT NOT NULL,
    Type TEXT NOT NULL DEFAULT 'MultipleChoice',
    OrderIndex INTEGER NOT NULL,
    Points INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (AssessmentId) REFERENCES Assessments(Id) ON DELETE CASCADE,
    FOREIGN KEY (LessonId) REFERENCES Lessons(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Answers (
    Id TEXT PRIMARY KEY,
    QuestionId TEXT NOT NULL,
    AnswerText TEXT NOT NULL,
    IsCorrect INTEGER NOT NULL DEFAULT 0,
    OrderIndex INTEGER NOT NULL,
    FOREIGN KEY (QuestionId) REFERENCES Questions(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS UserAssessmentResults (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    AssessmentId TEXT NOT NULL,
    Score INTEGER NOT NULL,
    Passed INTEGER NOT NULL,
    AttemptedAt TEXT NOT NULL,
    FailedLessonIds TEXT,
    UserASHATAnswers TEXT,
    FOREIGN KEY (AssessmentId) REFERENCES Assessments(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS CourseProgress (
    UserId TEXT NOT NULL,
    CourseId TEXT NOT NULL,
    CompletedLessonIds TEXT,
    StartedAt TEXT NOT NULL,
    CompletedAt TEXT,
    ProgressPercentage INTEGER NOT NULL DEFAULT 0,
    LastAccessedAt TEXT NOT NULL,
    PRIMARY KEY (UserId, CourseId),
    FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_lessons_courseid ON Lessons(CourseId);
CREATE INDEX IF NOT EXISTS idx_questions_assessmentid ON Questions(AssessmentId);
CREATE INDEX IF NOT EXISTS idx_questions_lessonid ON Questions(LessonId);
CREATE INDEX IF NOT EXISTS idx_answers_questionid ON Answers(QuestionId);
CREATE INDEX IF NOT EXISTS idx_UserAssessmentResults_userid ON UserAssessmentResults(UserId);
CREATE INDEX IF NOT EXISTS idx_UserAssessmentResults_assessmentid ON UserAssessmentResults(AssessmentId);
CREATE INDEX IF NOT EXISTS idx_courseprogress_userid ON CourseProgress(UserId);
CREATE INDEX IF NOT EXISTS idx_courses_permissionlevel ON Courses(PermissionLevel);
";
            cmd.ExecuteNonQuery();
        }

        #region Course Operations

        public void SaveCourse(Course course)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
INSERT OR REPLACE INTO Courses (Id, Title, Description, PermissionLevel, Category, LessonCount, EstimatedMinutes, CreatedAt, UpdatedAt, IsActive, PrerequisiteCourseId)
VALUES ($id, $title, $description, $permissionLevel, $category, $lessonCount, $estimatedMinutes, $createdAt, $updatedAt, $isActive, $prerequisiteCourseId);";

            cmd.Parameters.AddWithValue("$id", course.Id);
            cmd.Parameters.AddWithValue("$title", course.Title);
            cmd.Parameters.AddWithValue("$description", course.Description ?? "");
            cmd.Parameters.AddWithValue("$permissionLevel", course.PermissionLevel);
            cmd.Parameters.AddWithValue("$category", course.Category ?? "");
            cmd.Parameters.AddWithValue("$lessonCount", course.LessonCount);
            cmd.Parameters.AddWithValue("$estimatedMinutes", course.EstimatedMinutes);
            cmd.Parameters.AddWithValue("$createdAt", course.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$updatedAt", course.UpdatedAt?.ToString("o") ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$isActive", course.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("$prerequisiteCourseId", course.PrerequisiteCourseId ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public Course? GetCourse(string courseId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT * FROM Courses WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", courseId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Course
                {
                    Id = reader.GetString(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    PermissionLevel = reader.GetString(3),
                    Category = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    LessonCount = reader.GetInt32(5),
                    EstimatedMinutes = reader.GetInt32(6),
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    UpdatedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                    IsActive = reader.GetInt32(9) == 1,
                    PrerequisiteCourseId = reader.IsDBNull(10) ? null : reader.GetString(10)
                };
            }

            return null;
        }

        public List<Course> GetCourses(string? permissionLevel = null)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            if (permissionLevel != null)
            {
                cmd.CommandText = "SELECT * FROM Courses WHERE PermissionLevel = $permissionLevel AND IsActive = 1 ORDER BY Title";
                cmd.Parameters.AddWithValue("$permissionLevel", permissionLevel);
            }
            else
            {
                cmd.CommandText = "SELECT * FROM Courses WHERE IsActive = 1 ORDER BY Title";
            }

            var courses = new List<Course>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                courses.Add(new Course
                {
                    Id = reader.GetString(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    PermissionLevel = reader.GetString(3),
                    Category = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    LessonCount = reader.GetInt32(5),
                    EstimatedMinutes = reader.GetInt32(6),
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    UpdatedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                    IsActive = reader.GetInt32(9) == 1,
                    PrerequisiteCourseId = reader.IsDBNull(10) ? null : reader.GetString(10)
                });
            }

            return courses;
        }

        public int GetCourseCount()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT COUNT(*) FROM Courses WHERE IsActive = 1";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        #endregion

        #region Lesson Operations

        public void SaveLesson(Lesson lesson)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
INSERT OR REPLACE INTO Lessons (Id, CourseId, Title, Content, OrderIndex, EstimatedMinutes, CreatedAt, UpdatedAt, Type, VideoUrl, CodeExample, Tags)
VALUES ($id, $courseId, $title, $content, $orderIndex, $estimatedMinutes, $createdAt, $updatedAt, $type, $videoUrl, $codeExample, $tags);";

            cmd.Parameters.AddWithValue("$id", lesson.Id);
            cmd.Parameters.AddWithValue("$courseId", lesson.CourseId);
            cmd.Parameters.AddWithValue("$title", lesson.Title);
            cmd.Parameters.AddWithValue("$content", lesson.Content);
            cmd.Parameters.AddWithValue("$orderIndex", lesson.OrderIndex);
            cmd.Parameters.AddWithValue("$estimatedMinutes", lesson.EstimatedMinutes);
            cmd.Parameters.AddWithValue("$createdAt", lesson.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$updatedAt", lesson.UpdatedAt?.ToString("o") ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$type", lesson.Type.ToString());
            cmd.Parameters.AddWithValue("$videoUrl", lesson.VideoUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$codeExample", lesson.CodeExample ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$tags", JsonSerializer.Serialize(lesson.Tags, _jsonOptions));

            cmd.ExecuteNonQuery();
        }

        public Lesson? GetLesson(string lessonId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT * FROM Lessons WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", lessonId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Lesson
                {
                    Id = reader.GetString(0),
                    CourseId = reader.GetString(1),
                    Title = reader.GetString(2),
                    Content = reader.GetString(3),
                    OrderIndex = reader.GetInt32(4),
                    EstimatedMinutes = reader.GetInt32(5),
                    CreatedAt = DateTime.Parse(reader.GetString(6)),
                    UpdatedAt = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
                    Type = Enum.Parse<LessonType>(reader.GetString(8)),
                    VideoUrl = reader.IsDBNull(9) ? null : reader.GetString(9),
                    CodeExample = reader.IsDBNull(10) ? null : reader.GetString(10),
                    Tags = reader.IsDBNull(11) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(reader.GetString(11)) ?? new List<string>()
                };
            }

            return null;
        }

        public List<Lesson> GetLessons(string courseId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT * FROM Lessons WHERE CourseId = $courseId ORDER BY OrderIndex";
            cmd.Parameters.AddWithValue("$courseId", courseId);

            var lessons = new List<Lesson>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lessons.Add(new Lesson
                {
                    Id = reader.GetString(0),
                    CourseId = reader.GetString(1),
                    Title = reader.GetString(2),
                    Content = reader.GetString(3),
                    OrderIndex = reader.GetInt32(4),
                    EstimatedMinutes = reader.GetInt32(5),
                    CreatedAt = DateTime.Parse(reader.GetString(6)),
                    UpdatedAt = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
                    Type = Enum.Parse<LessonType>(reader.GetString(8)),
                    VideoUrl = reader.IsDBNull(9) ? null : reader.GetString(9),
                    CodeExample = reader.IsDBNull(10) ? null : reader.GetString(10),
                    Tags = reader.IsDBNull(11) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(reader.GetString(11)) ?? new List<string>()
                });
            }

            return lessons;
        }

        public int GetLessonCount()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT COUNT(*) FROM Lessons";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        #endregion

        #region Assessment Operations

        public void SaveAssessment(Assessment assessment)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
INSERT OR REPLACE INTO Assessments (Id, CourseId, Title, Description, PassingScore, CreatedAt, UpdatedAt, IsActive)
VALUES ($id, $courseId, $title, $description, $passingScore, $createdAt, $updatedAt, $isActive);";

            cmd.Parameters.AddWithValue("$id", assessment.Id);
            cmd.Parameters.AddWithValue("$courseId", assessment.CourseId);
            cmd.Parameters.AddWithValue("$title", assessment.Title);
            cmd.Parameters.AddWithValue("$description", assessment.Description ?? "");
            cmd.Parameters.AddWithValue("$passingScore", assessment.PassingScore);
            cmd.Parameters.AddWithValue("$createdAt", assessment.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$updatedAt", assessment.UpdatedAt?.ToString("o") ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$isActive", assessment.IsActive ? 1 : 0);

            cmd.ExecuteNonQuery();
        }

        public Assessment? GetAssessment(string assessmentId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT * FROM Assessments WHERE Id = $id AND IsActive = 1";
            cmd.Parameters.AddWithValue("$id", assessmentId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Assessment
                {
                    Id = reader.GetString(0),
                    CourseId = reader.GetString(1),
                    Title = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    PassingScore = reader.GetInt32(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5)),
                    UpdatedAt = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
                    IsActive = reader.GetInt32(7) == 1
                };
            }

            return null;
        }

        public Assessment? GetAssessmentByCourseId(string courseId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT * FROM Assessments WHERE CourseId = $courseId AND IsActive = 1 LIMIT 1";
            cmd.Parameters.AddWithValue("$courseId", courseId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Assessment
                {
                    Id = reader.GetString(0),
                    CourseId = reader.GetString(1),
                    Title = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    PassingScore = reader.GetInt32(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5)),
                    UpdatedAt = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
                    IsActive = reader.GetInt32(7) == 1
                };
            }

            return null;
        }

        public void SaveQuestion(Question question)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
INSERT OR REPLACE INTO Questions (Id, AssessmentId, LessonId, QuestionText, Type, OrderIndex, Points, CreatedAt)
VALUES ($id, $assessmentId, $lessonId, $questionText, $type, $orderIndex, $points, $createdAt);";

            cmd.Parameters.AddWithValue("$id", question.Id);
            cmd.Parameters.AddWithValue("$assessmentId", question.AssessmentId);
            cmd.Parameters.AddWithValue("$lessonId", question.LessonId);
            cmd.Parameters.AddWithValue("$questionText", question.QuestionText);
            cmd.Parameters.AddWithValue("$type", question.Type.ToString());
            cmd.Parameters.AddWithValue("$orderIndex", question.OrderIndex);
            cmd.Parameters.AddWithValue("$points", question.Points);
            cmd.Parameters.AddWithValue("$createdAt", question.CreatedAt.ToString("o"));

            cmd.ExecuteNonQuery();
        }

        public List<Question> GetQuestions(string assessmentId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT * FROM Questions WHERE AssessmentId = $assessmentId ORDER BY OrderIndex";
            cmd.Parameters.AddWithValue("$assessmentId", assessmentId);

            var questions = new List<Question>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                questions.Add(new Question
                {
                    Id = reader.GetString(0),
                    AssessmentId = reader.GetString(1),
                    LessonId = reader.GetString(2),
                    QuestionText = reader.GetString(3),
                    Type = Enum.Parse<QuestionType>(reader.GetString(4)),
                    OrderIndex = reader.GetInt32(5),
                    Points = reader.GetInt32(6),
                    CreatedAt = DateTime.Parse(reader.GetString(7))
                });
            }

            return questions;
        }

        public void SaveAnswer(Answer answer)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
INSERT OR REPLACE INTO Answers (Id, QuestionId, AnswerText, IsCorrect, OrderIndex)
VALUES ($id, $questionId, $answerText, $isCorrect, $orderIndex);";

            cmd.Parameters.AddWithValue("$id", answer.Id);
            cmd.Parameters.AddWithValue("$questionId", answer.QuestionId);
            cmd.Parameters.AddWithValue("$answerText", answer.AnswerText);
            cmd.Parameters.AddWithValue("$isCorrect", answer.IsCorrect ? 1 : 0);
            cmd.Parameters.AddWithValue("$orderIndex", answer.OrderIndex);

            cmd.ExecuteNonQuery();
        }

        public List<Answer> GetAnswers(string questionId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT * FROM Answers WHERE QuestionId = $questionId ORDER BY OrderIndex";
            cmd.Parameters.AddWithValue("$questionId", questionId);

            var answers = new List<Answer>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                answers.Add(new Answer
                {
                    Id = reader.GetString(0),
                    QuestionId = reader.GetString(1),
                    AnswerText = reader.GetString(2),
                    IsCorrect = reader.GetInt32(3) == 1,
                    OrderIndex = reader.GetInt32(4)
                });
            }

            return answers;
        }

        #endregion

        #region User Assessment Results

        public void SaveUserAssessmentResult(UserAssessmentResult result)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            // Use INSERT OR REPLACE to be consistent with other save methods
            cmd.CommandText = @"
INSERT OR REPLACE INTO UserAssessmentResults (Id, UserId, AssessmentId, Score, Passed, AttemptedAt, FailedLessonIds, UserASHATAnswers)
VALUES ($id, $userId, $assessmentId, $score, $passed, $attemptedAt, $failedLessonIds, $userASHATAnswers);";

            cmd.Parameters.AddWithValue("$id", result.Id);
            cmd.Parameters.AddWithValue("$userId", result.UserId);
            cmd.Parameters.AddWithValue("$assessmentId", result.AssessmentId);
            cmd.Parameters.AddWithValue("$score", result.Score);
            cmd.Parameters.AddWithValue("$passed", result.Passed ? 1 : 0);
            cmd.Parameters.AddWithValue("$attemptedAt", result.AttemptedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$failedLessonIds", JsonSerializer.Serialize(result.FailedLessonIds ?? new List<string>(), _jsonOptions));
            cmd.Parameters.AddWithValue("$userASHATAnswers", JsonSerializer.Serialize(result.UserASHATAnswers ?? new Dictionary<string, string>(), _jsonOptions));

            cmd.ExecuteNonQuery();
        }

        public List<UserAssessmentResult> GetUserAssessmentResults(string userId, string assessmentId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT * FROM UserAssessmentResults WHERE UserId = $userId AND AssessmentId = $assessmentId ORDER BY AttemptedAt DESC";
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$assessmentId", assessmentId);

            var results = new List<UserAssessmentResult>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var failedLessonIds = new List<string>();
                var userASHATAnswers = new Dictionary<string, string>();

                if (!reader.IsDBNull(6))
                {
                    var failedJson = reader.GetString(6);
                    if (!string.IsNullOrWhiteSpace(failedJson))
                    {
                        failedLessonIds = JsonSerializer.Deserialize<List<string>>(failedJson) ?? new List<string>();
                    }
                }

                if (!reader.IsDBNull(7))
                {
                    var ashaJson = reader.GetString(7);
                    if (!string.IsNullOrWhiteSpace(ashaJson))
                    {
                        userASHATAnswers = JsonSerializer.Deserialize<Dictionary<string, string>>(ashaJson) ?? new Dictionary<string, string>();
                    }
                }

                results.Add(new UserAssessmentResult
                {
                    Id = reader.GetString(0),
                    UserId = reader.GetString(1),
                    AssessmentId = reader.GetString(2),
                    Score = reader.GetInt32(3),
                    Passed = reader.GetInt32(4) == 1,
                    AttemptedAt = DateTime.Parse(reader.GetString(5)),
                    FailedLessonIds = failedLessonIds,
                    UserASHATAnswers = userASHATAnswers
                });
            }

            return results;
        }

        #endregion

        #region Course Progress

        public void SaveCourseProgress(CourseProgress progress)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
INSERT OR REPLACE INTO CourseProgress (UserId, CourseId, CompletedLessonIds, StartedAt, CompletedAt, ProgressPercentage, LastAccessedAt)
VALUES ($userId, $courseId, $completedLessonIds, $startedAt, $completedAt, $progressPercentage, $lastAccessedAt);";

            cmd.Parameters.AddWithValue("$userId", progress.UserId);
            cmd.Parameters.AddWithValue("$courseId", progress.CourseId);
            cmd.Parameters.AddWithValue("$completedLessonIds", JsonSerializer.Serialize(progress.CompletedLessonIds ?? new List<string>(), _jsonOptions));
            cmd.Parameters.AddWithValue("$startedAt", progress.StartedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$completedAt", progress.CompletedAt?.ToString("o") ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$progressPercentage", progress.ProgressPercentage);
            cmd.Parameters.AddWithValue("$lastAccessedAt", progress.LastAccessedAt.ToString("o"));

            cmd.ExecuteNonQuery();
        }

        public CourseProgress? GetCourseProgress(string userId, string courseId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT * FROM CourseProgress WHERE UserId = $userId AND CourseId = $courseId";
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$courseId", courseId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var completedLessonIds = new List<string>();
                if (!reader.IsDBNull(2))
                {
                    var completedJson = reader.GetString(2);
                    if (!string.IsNullOrWhiteSpace(completedJson))
                    {
                        completedLessonIds = JsonSerializer.Deserialize<List<string>>(completedJson) ?? new List<string>();
                    }
                }

                return new CourseProgress
                {
                    UserId = reader.GetString(0),
                    CourseId = reader.GetString(1),
                    CompletedLessonIds = completedLessonIds,
                    StartedAt = DateTime.Parse(reader.GetString(3)),
                    CompletedAt = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                    ProgressPercentage = reader.GetInt32(5),
                    LastAccessedAt = DateTime.Parse(reader.GetString(6))
                };
            }

            return null;
        }

        #endregion

        public void Dispose()
        {
            // Connection is created and disposed per Operation, nothing to dispose here
        }
    }
}