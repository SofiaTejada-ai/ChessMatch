namespace WebApplication.DataManagement.Models
{
    /// <summary>
    /// Contains player statistics including win/loss/draw counts, current and best win streaks, 
    /// rating, and last game timestamp. Used for raw SQL operations with 
    /// StatsSchema.UserStatsTable to maintain player performance metrics.
    /// </summary>
    public class UserStats
    {
        public int UserID { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int CurrentWinStreak { get; set; }
        public int BestWinStreak { get; set; }
        public int Rating { get; set; }
        public DateTime? LastGameEndedAt { get; set; }
    }
}