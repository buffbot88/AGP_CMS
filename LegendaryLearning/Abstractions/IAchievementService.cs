using Abstractions;

namespace LegendaryLearning.Abstractions
{
    /// <summary>
    /// Service interface for achievement management.
    /// </summary>
    public interface IAchievementService
    {
        /// <summary>
        /// Get all achievements for a user.
        /// </summary>
        Task<List<LearningAchievement>> GetUserAchievementsAsync(string userId);

        /// <summary>
        /// Award an achievement to a user.
        /// </summary>
        Task AwardAchievementAsync(string userId, LearningAchievement achievement);
    }
}
