using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebApplication.DataManagement;
using WebApplication.DataManagement.Models;
using System.Security.Claims;

namespace WebApplication
{
    [ApiController]
    [Route("[controller]")]
    [Authorize] // CLASS: Requires JWT token to access statistics
    public class StatsController : ControllerBase
    {
        private readonly DatabaseHelper _db;

        // CONSTRUCTOR: Dependency injection of DatabaseHelper
        // PURPOSE: Provides database access for user statistics
        public StatsController(DatabaseHelper db)
        {
            _db = db;
        }

        [HttpGet("my-stats")]
        // FUNCTION: Get current user's statistics
        // PURPOSE: Retrieve player performance metrics and ranking information
        // RETURNS: UserStats object with wins, losses, rating, etc.
        public async Task<IActionResult> GetMyStats()
        {
            // TYPE VARIABLE: userId (int) - Extracted from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Invalid token");

            var userId = int.Parse(userIdClaim.Value);

            // FUNCTION CALL: Get user statistics from database
            var stats = await _db.GetUserStatsAsync(userId);
            if (stats == null)
            {
                // If no stats exist, create default stats for new user
                stats = new UserStats
                {
                    UserID = userId,
                    Wins = 0,
                    Losses = 0,
                    Draws = 0,
                    CurrentWinStreak = 0,
                    BestWinStreak = 0,
                    Rating = 1200 // Default chess rating
                };
                
                // FUNCTION CALL: Create initial stats record
                await _db.CreateUserStatsAsync(stats);
            }

            return Ok(stats);
        }

        [HttpGet("leaderboard")]
        // FUNCTION: Get top players by rating
        // PURPOSE: Display ranking system for competitive play
        // RETURNS: List of top players with their stats
        public async Task<IActionResult> GetLeaderboard()
        {
            // FUNCTION CALL: Get top 10 players by rating
            var leaderboard = await _db.GetLeaderboardAsync(10);

            return Ok(leaderboard);
        }

        [HttpPost("update-stats")]
        // FUNCTION: Update user statistics after a match
        // PURPOSE: Process game results and update player metrics
        // PARAMETER: result - Game outcome (Win/Loss/Draw)
        public async Task<IActionResult> UpdateStats([FromBody] UpdateStatsRequest req)
        {
            // TYPE VARIABLE: userId (int) - Extracted from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Invalid token");

            var userId = int.Parse(userIdClaim.Value);

            // Validate result
            if (req.Result != "Win" && req.Result != "Loss" && req.Result != "Draw")
                return BadRequest("Invalid result. Must be Win, Loss, or Draw");

            // FUNCTION CALL: Get current stats
            var stats = await _db.GetUserStatsAsync(userId);
            if (stats == null)
                return NotFound("User statistics not found");

            // Update statistics based on result
            switch (req.Result)
            {
                case "Win":
                    stats.Wins++;
                    stats.CurrentWinStreak++;
                    if (stats.CurrentWinStreak > stats.BestWinStreak)
                        stats.BestWinStreak = stats.CurrentWinStreak;
                    // Increase rating (simple ELO-like system)
                    stats.Rating += 25;
                    break;
                case "Loss":
                    stats.Losses++;
                    stats.CurrentWinStreak = 0;
                    // Decrease rating
                    stats.Rating -= 20;
                    break;
                case "Draw":
                    stats.Draws++;
                    stats.CurrentWinStreak = 0;
                    // Small rating change for draw
                    stats.Rating += 5;
                    break;
            }

            // Update last game timestamp
            stats.LastGameEndedAt = DateTime.UtcNow;

            // FUNCTION CALL: Update statistics in database
            await _db.UpdateUserStatsAsync(stats);

            return Ok(new { message = "Statistics updated successfully", newRating = stats.Rating });
        }
    }

    /// <summary>
    /// Request object for updating user statistics after a game.
    /// Contains the game result information.
    /// </summary>
    public class UpdateStatsRequest
    {
        public string Result { get; set; } // Win, Loss, or Draw
    }
}
