using System.Data;
using Microsoft.Data.SqlClient;
using WebApplication.DataManagement.Models;

namespace WebApplication.DataManagement
{
    /// <summary>
    /// Raw SQL database helper class that replaces Entity Framework
    /// Uses direct SQL queries to interact with the ChessHub database
    /// </summary>
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<int> CreateUserAsync(User user)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var sql = @"
                    INSERT INTO UsersSchema.UsersTable (Username, Email, PasswordHash, CreatedAt, IsActive)
                    VALUES (@Username, @Email, @PasswordHash, @CreatedAt, @IsActive);
                    SELECT SCOPE_IDENTITY();";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Username", user.Username);
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
                    command.Parameters.AddWithValue("@IsActive", user.IsActive);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT UserID, Username, Email, PasswordHash, CreatedAt, LastSeenAt, IsActive
                    FROM UsersSchema.UsersTable
                    WHERE UserID = @UserID";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                UserID = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Email = reader.GetString(2),
                                PasswordHash = reader.GetString(3),
                                CreatedAt = reader.GetDateTime(4),
                                LastSeenAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                                IsActive = reader.GetBoolean(6)
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT UserID, Username, Email, PasswordHash, CreatedAt, LastSeenAt, IsActive
                    FROM UsersSchema.UsersTable
                    WHERE Username = @Username";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                UserID = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Email = reader.GetString(2),
                                PasswordHash = reader.GetString(3),
                                CreatedAt = reader.GetDateTime(4),
                                LastSeenAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                                IsActive = reader.GetBoolean(6)
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task<List<Match>> GetUserMatchesAsync(int userId)
        {
            var matches = new List<Match>();
            
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT MatchID, CreatedAt, EndedAt, WhiteUserID, BlackUserID, WinnerID, 
                           MatchState, Result, EndReason, MatchType, InviteCode
                    FROM MatchesSchema.MatchesTable
                    WHERE WhiteUserID = @UserID OR BlackUserID = @UserID
                    ORDER BY CreatedAt DESC";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            matches.Add(new Match
                            {
                                MatchID = reader.GetInt32(0),
                                CreatedAt = reader.GetDateTime(1),
                                EndedAt = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                                WhiteUserID = reader.GetInt32(3),
                                BlackUserID = reader.GetInt32(4),
                                WinnerID = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                                MatchState = reader.GetString(6),
                                Result = reader.IsDBNull(7) ? null : reader.GetString(7),
                                EndReason = reader.IsDBNull(8) ? null : reader.GetString(8),
                                MatchType = reader.GetString(9),
                                InviteCode = reader.IsDBNull(10) ? null : reader.GetString(10)
                            });
                        }
                    }
                }
            }
            
            return matches;
        }

        public async Task<UserStats?> GetUserStatsAsync(int userId)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT UserID, Wins, Losses, Draws, CurrentWinStreak, BestWinStreak, Rating, LastGameEndedAt
                    FROM StatsSchema.UserStatsTable
                    WHERE UserID = @UserID";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new UserStats
                            {
                                UserID = reader.GetInt32(0),
                                Wins = reader.GetInt32(1),
                                Losses = reader.GetInt32(2),
                                Draws = reader.GetInt32(3),
                                CurrentWinStreak = reader.GetInt32(4),
                                BestWinStreak = reader.GetInt32(5),
                                Rating = reader.GetInt32(6),
                                LastGameEndedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task UpdateUserAsync(User user)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var sql = @"
                    UPDATE UsersSchema.UsersTable 
                    SET Username = @Username, Email = @Email, LastSeenAt = @LastSeenAt
                    WHERE UserID = @UserID";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", user.UserID);
                    command.Parameters.AddWithValue("@Username", user.Username);
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@LastSeenAt", user.LastSeenAt);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> CreateMatchAsync(Match match)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var sql = @"
                    INSERT INTO MatchesSchema.MatchesTable 
                    (CreatedAt, WhiteUserID, BlackUserID, MatchState, MatchType, InviteCode)
                    VALUES (@CreatedAt, @WhiteUserID, @BlackUserID, @MatchState, @MatchType, @InviteCode);
                    SELECT SCOPE_IDENTITY();";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CreatedAt", match.CreatedAt);
                    command.Parameters.AddWithValue("@WhiteUserID", match.WhiteUserID);
                    command.Parameters.AddWithValue("@BlackUserID", match.BlackUserID);
                    command.Parameters.AddWithValue("@MatchState", match.MatchState);
                    command.Parameters.AddWithValue("@MatchType", match.MatchType);
                    command.Parameters.AddWithValue("@InviteCode", (object?)match.InviteCode ?? DBNull.Value);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<Match?> GetMatchByIdAsync(int matchId)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT MatchID, CreatedAt, EndedAt, WhiteUserID, BlackUserID, WinnerID, 
                           MatchState, Result, EndReason, MatchType, InviteCode
                    FROM MatchesSchema.MatchesTable
                    WHERE MatchID = @MatchID";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@MatchID", matchId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Match
                            {
                                MatchID = reader.GetInt32(0),
                                CreatedAt = reader.GetDateTime(1),
                                EndedAt = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                                WhiteUserID = reader.GetInt32(3),
                                BlackUserID = reader.GetInt32(4),
                                WinnerID = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                                MatchState = reader.GetString(6),
                                Result = reader.IsDBNull(7) ? null : reader.GetString(7),
                                EndReason = reader.IsDBNull(8) ? null : reader.GetString(8),
                                MatchType = reader.GetString(9),
                                InviteCode = reader.IsDBNull(10) ? null : reader.GetString(10)
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task CreateUserStatsAsync(UserStats stats)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var sql = @"
                    INSERT INTO StatsSchema.UserStatsTable 
                    (UserID, Wins, Losses, Draws, CurrentWinStreak, BestWinStreak, Rating, LastGameEndedAt)
                    VALUES (@UserID, @Wins, @Losses, @Draws, @CurrentWinStreak, @BestWinStreak, @Rating, @LastGameEndedAt)";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", stats.UserID);
                    command.Parameters.AddWithValue("@Wins", stats.Wins);
                    command.Parameters.AddWithValue("@Losses", stats.Losses);
                    command.Parameters.AddWithValue("@Draws", stats.Draws);
                    command.Parameters.AddWithValue("@CurrentWinStreak", stats.CurrentWinStreak);
                    command.Parameters.AddWithValue("@BestWinStreak", stats.BestWinStreak);
                    command.Parameters.AddWithValue("@Rating", stats.Rating);
                    command.Parameters.AddWithValue("@LastGameEndedAt", (object?)stats.LastGameEndedAt ?? DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task UpdateUserStatsAsync(UserStats stats)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var sql = @"
                    UPDATE StatsSchema.UserStatsTable 
                    SET Wins = @Wins, Losses = @Losses, Draws = @Draws, 
                        CurrentWinStreak = @CurrentWinStreak, BestWinStreak = @BestWinStreak, 
                        Rating = @Rating, LastGameEndedAt = @LastGameEndedAt
                    WHERE UserID = @UserID";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", stats.UserID);
                    command.Parameters.AddWithValue("@Wins", stats.Wins);
                    command.Parameters.AddWithValue("@Losses", stats.Losses);
                    command.Parameters.AddWithValue("@Draws", stats.Draws);
                    command.Parameters.AddWithValue("@CurrentWinStreak", stats.CurrentWinStreak);
                    command.Parameters.AddWithValue("@BestWinStreak", stats.BestWinStreak);
                    command.Parameters.AddWithValue("@Rating", stats.Rating);
                    command.Parameters.AddWithValue("@LastGameEndedAt", (object?)stats.LastGameEndedAt ?? DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<UserStats>> GetLeaderboardAsync(int topCount)
        {
            var leaderboard = new List<UserStats>();
            
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT TOP (@TopCount) us.UserID, us.Wins, us.Losses, us.Draws, 
                           us.CurrentWinStreak, us.BestWinStreak, us.Rating, us.LastGameEndedAt,
                           u.Username
                    FROM StatsSchema.UserStatsTable us
                    INNER JOIN UsersSchema.UsersTable u ON us.UserID = u.UserID
                    ORDER BY us.Rating DESC";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@TopCount", topCount);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            leaderboard.Add(new UserStats
                            {
                                UserID = reader.GetInt32(0),
                                Wins = reader.GetInt32(1),
                                Losses = reader.GetInt32(2),
                                Draws = reader.GetInt32(3),
                                CurrentWinStreak = reader.GetInt32(4),
                                BestWinStreak = reader.GetInt32(5),
                                Rating = reader.GetInt32(6),
                                LastGameEndedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                            });
                        }
                    }
                }
            }
            
            return leaderboard;
        }
    }
}
