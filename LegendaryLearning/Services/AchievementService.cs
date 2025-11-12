using System.Collections.Concurrent;
using Abstractions;
using LegendaryLearning.Abstractions;

namespace LegendaryLearning.Services
{
    /// <summary>
    /// Service for achievement management.
    /// </summary>
    public class AchievementService : IAchievementService
    {
        private readonly ConcurrentDictionary<string, List<LearningAchievement>> _userAchievements = new();
        private readonly string _moduleName;

        public AchievementService(string moduleName)
        {
            _moduleName = moduleName;
        }

        public async Task<List<LearningAchievement>> GetUserAchievementsAsync(string userId)
        {
            await Task.CompletedTask;

            return _userAchievements.TryGetValue(userId, out var achievements)
                ? new List<LearningAchievement>(achievements)
                : new List<LearningAchievement>();
        }

        public async Task AwardAchievementAsync(string userId, LearningAchievement achievement)
        {
            await Task.CompletedTask;

            if (!_userAchievements.ContainsKey(userId))
            {
                _userAchievements[userId] = new List<LearningAchievement>();
            }

            _userAchievements[userId].Add(achievement);
            Console.WriteLine($"[{_moduleName}] Awarded achievement to {userId}: {achievement.Title}");
        }
    }
}
