using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;
using WebApplication.DataManagement.Models;

namespace WebApplication.DataManagement
{
    /// <summary>
    /// Raw SQL database helper class that replaces Entity Framework
    /// Uses direct SQL queries to interact with the ChessHub database
    /// Supports both SQL Server (local) and PostgreSQL (Railway)
    /// </summary>
    public class DatabaseHelper
    {
        private readonly string _connectionString;
        private readonly bool _usePostgres;

        public DatabaseHelper(string connectionString, bool usePostgres = false)
        {
            _connectionString = connectionString;
            _usePostgres = usePostgres;
        }

        private DbConnection GetConnection()
        {
            if (_usePostgres)
                return new NpgsqlConnection(_connectionString);
            return new SqlConnection(_connectionString);
        }

        private DbCommand CreateCommand(string sql, DbConnection connection)
        {
            if (_usePostgres)
                return new NpgsqlCommand(sql, (NpgsqlConnection)connection);
            return new SqlCommand(sql, (SqlConnection)connection);
        }

        private DbParameter CreateParameter(string name, object? value)
        {
            if (_usePostgres)
                return new NpgsqlParameter(name, value ?? DBNull.Value);
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        private string Q(string identifier)
        {
            // Quote identifiers for PostgreSQL, leave as-is for SQL Server
            return _usePostgres ? $"\"{identifier}\"" : identifier;
        }

        private string SchemaTable(string schema, string table)
        {
            return _usePostgres ? $"\"{schema}\".\"{table}\"" : $"{schema}.{table}";
        }

        public async Task<int> CreateUserAsync(User user)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var sql = _usePostgres
                    ? @"INSERT INTO ""UsersSchema"".""UsersTable"" (""Username"", ""Email"", ""PasswordHash"", ""CreatedAt"", ""IsActive"")
                       VALUES (@Username, @Email, @PasswordHash, @CreatedAt, @IsActive)
                       RETURNING ""UserID"";"
                    : @"INSERT INTO UsersSchema.UsersTable (Username, Email, PasswordHash, CreatedAt, IsActive)
                       VALUES (@Username, @Email, @PasswordHash, @CreatedAt, @IsActive);
                       SELECT SCOPE_IDENTITY();";

                using (var command = CreateCommand(sql, connection))
                {
                    command.Parameters.Add(CreateParameter("@Username", user.Username));
                    command.Parameters.Add(CreateParameter("@Email", user.Email));
                    command.Parameters.Add(CreateParameter("@PasswordHash", user.PasswordHash));
                    command.Parameters.Add(CreateParameter("@CreatedAt", user.CreatedAt));
                    command.Parameters.Add(CreateParameter("@IsActive", user.IsActive));

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
                
                var usersTable = SchemaTable("UsersSchema", "UsersTable");
                var sql = $@"
                    SELECT {Q("UserID")}, {Q("Username")}, {Q("Email")}, {Q("PasswordHash")}, {Q("CreatedAt")}, {Q("LastSeenAt")}, {Q("IsActive")}
                    FROM {usersTable}
                    WHERE {Q("UserID")} = @UserID";

                using (var command = CreateCommand(sql, connection))
                {
                    command.Parameters.Add(CreateParameter("@UserID", userId));

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
                
                var usersTable = SchemaTable("UsersSchema", "UsersTable");
                var sql = $@"
                    SELECT {Q("UserID")}, {Q("Username")}, {Q("Email")}, {Q("PasswordHash")}, {Q("CreatedAt")}, {Q("LastSeenAt")}, {Q("IsActive")}
                    FROM {usersTable}
                    WHERE {Q("Username")} = @Username";

                using (var command = CreateCommand(sql, connection))
                {
                    command.Parameters.Add(CreateParameter("@Username", username));

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
                
                var matchesTable = SchemaTable("MatchesSchema", "MatchesTable");
                var sql = $@"
                    SELECT {Q("MatchID")}, {Q("CreatedAt")}, {Q("EndedAt")}, {Q("WhiteUserID")}, {Q("BlackUserID")}, {Q("WinnerID")}, 
                           {Q("MatchState")}, {Q("Result")}, {Q("EndReason")}, {Q("MatchType")}, {Q("InviteCode")}
                    FROM {matchesTable}
                    WHERE {Q("WhiteUserID")} = @UserID OR {Q("BlackUserID")} = @UserID
                    ORDER BY {Q("CreatedAt")} DESC";

                using (var command = CreateCommand(sql, connection))
                {
                    command.Parameters.Add(CreateParameter("@UserID", userId));

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
                
                var statsTable = SchemaTable("StatsSchema", "UserStatsTable");
                var sql = $@"
                    SELECT {Q("UserID")}, {Q("Wins")}, {Q("Losses")}, {Q("Draws")}, {Q("CurrentWinStreak")}, {Q("BestWinStreak")}, {Q("Rating")}, {Q("LastGameEndedAt")}
                    FROM {statsTable}
                    WHERE {Q("UserID")} = @UserID";

                using (var command = CreateCommand(sql, connection))
                {
                    command.Parameters.Add(CreateParameter("@UserID", userId));

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
                
                var usersTable = SchemaTable("UsersSchema", "UsersTable");
                var sql = $@"
                    UPDATE {usersTable} 
                    SET {Q("Username")} = @Username, {Q("Email")} = @Email, {Q("LastSeenAt")} = @LastSeenAt
                    WHERE {Q("UserID")} = @UserID";

                using (var command = CreateCommand(sql, connection))
                {
                    command.Parameters.Add(CreateParameter("@UserID", user.UserID));
                    command.Parameters.Add(CreateParameter("@Username", user.Username));
                    command.Parameters.Add(CreateParameter("@Email", user.Email));
                    command.Parameters.Add(CreateParameter("@LastSeenAt", user.LastSeenAt));

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> CreateMatchAsync(Match match)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var matchesTable = SchemaTable("MatchesSchema", "MatchesTable");
                var sql = _usePostgres
                    ? $@"INSERT INTO {matchesTable} 
                        ({Q("CreatedAt")}, {Q("WhiteUserID")}, {Q("BlackUserID")}, {Q("MatchState")}, {Q("MatchType")}, {Q("InviteCode")})
                        VALUES (@CreatedAt, @WhiteUserID, @BlackUserID, @MatchState, @MatchType, @InviteCode)
                        RETURNING {Q("MatchID")};"
                    : $@"INSERT INTO {matchesTable} 
                        ({Q("CreatedAt")}, {Q("WhiteUserID")}, {Q("BlackUserID")}, {Q("MatchState")}, {Q("MatchType")}, {Q("InviteCode")})
                        VALUES (@CreatedAt, @WhiteUserID, @BlackUserID, @MatchState, @MatchType, @InviteCode);
                        SELECT SCOPE_IDENTITY();";

                using (var command = CreateCommand(sql, connection))
                {
                    command.Parameters.Add(CreateParameter("@CreatedAt", match.CreatedAt));
                    command.Parameters.Add(CreateParameter("@WhiteUserID", match.WhiteUserID));
                    command.Parameters.Add(CreateParameter("@BlackUserID", match.BlackUserID));
                    command.Parameters.Add(CreateParameter("@MatchState", match.MatchState));
                    command.Parameters.Add(CreateParameter("@MatchType", match.MatchType));
                    command.Parameters.Add(CreateParameter("@InviteCode", (object?)match.InviteCode ?? DBNull.Value));

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
                
                var matchesTable = SchemaTable("MatchesSchema", "MatchesTable");
                var sql = $@"
                    SELECT {Q("MatchID")}, {Q("CreatedAt")}, {Q("EndedAt")}, {Q("WhiteUserID")}, {Q("BlackUserID")}, {Q("WinnerID")}, 
                           {Q("MatchState")}, {Q("Result")}, {Q("EndReason")}, {Q("MatchType")}, {Q("InviteCode")}
                    FROM {matchesTable}
                    WHERE {Q("MatchID")} = @MatchID";

                using (var command = CreateCommand(sql, connection))
                {
                    command.Parameters.Add(CreateParameter("@MatchID", matchId));

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
                
                var statsTable = SchemaTable("StatsSchema", "UserStatsTable");
                var sql = $@"
                    INSERT INTO {statsTable} 
                    ({Q("UserID")}, {Q("Wins")}, {Q("Losses")}, {Q("Draws")}, {Q("CurrentWinStreak")}, {Q("BestWinStreak")}, {Q("Rating")}, {Q("LastGameEndedAt")})
                    VALUES (@UserID, @Wins, @Losses, @Draws, @CurrentWinStreak, @BestWinStreak, @Rating, @LastGameEndedAt)";

                using (var command = CreateCommand(sql, connection))
                {
                    command.Parameters.Add(CreateParameter("@UserID", stats.UserID));
                    command.Parameters.Add(CreateParameter("@Wins", stats.Wins));
                    command.Parameters.Add(CreateParameter("@Losses", stats.Losses));
                    command.Parameters.Add(CreateParameter("@Draws", stats.Draws));
                    command.Parameters.Add(CreateParameter("@CurrentWinStreak", stats.CurrentWinStreak));
                    command.Parameters.Add(CreateParameter("@BestWinStreak", stats.BestWinStreak));
                    command.Parameters.Add(CreateParameter("@Rating", stats.Rating));
                    command.Parameters.Add(CreateParameter("@LastGameEndedAt", (object?)stats.LastGameEndedAt ?? DBNull.Value));

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task UpdateUserStatsAsync(UserStats stats)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                
                var statsTable = SchemaTable("StatsSchema", "UserStatsTable");
                var sql = $@"
                    UPDATE {statsTable} 
                    SET {Q("Wins")} = @Wins, {Q("Losses")} = @Losses, {Q("Draws")} = @Draws, 
                        {Q("CurrentWinStreak")} = @CurrentWinStreak, {Q("BestWinStreak")} = @BestWinStreak, 
                        {Q("Rating")} = @Rating, {Q("LastGameEndedAt")} = @LastGameEndedAt
                    WHERE {Q("UserID")} = @UserID";

                using (var command = CreateCommand(sql, connection))
                {
                    command.Parameters.Add(CreateParameter("@UserID", stats.UserID));
                    command.Parameters.Add(CreateParameter("@Wins", stats.Wins));
                    command.Parameters.Add(CreateParameter("@Losses", stats.Losses));
                    command.Parameters.Add(CreateParameter("@Draws", stats.Draws));
                    command.Parameters.Add(CreateParameter("@CurrentWinStreak", stats.CurrentWinStreak));
                    command.Parameters.Add(CreateParameter("@BestWinStreak", stats.BestWinStreak));
                    command.Parameters.Add(CreateParameter("@Rating", stats.Rating));
                    command.Parameters.Add(CreateParameter("@LastGameEndedAt", (object?)stats.LastGameEndedAt ?? DBNull.Value));

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
                
                var statsTable = SchemaTable("StatsSchema", "UserStatsTable");
                var usersTable = SchemaTable("UsersSchema", "UsersTable");
                var sql = _usePostgres
                    ? $@"SELECT us.{Q("UserID")}, us.{Q("Wins")}, us.{Q("Losses")}, us.{Q("Draws")}, 
                           us.{Q("CurrentWinStreak")}, us.{Q("BestWinStreak")}, us.{Q("Rating")}, us.{Q("LastGameEndedAt")},
                           u.{Q("Username")}
                    FROM {statsTable} us
                    INNER JOIN {usersTable} u ON us.{Q("UserID")} = u.{Q("UserID")}
                    ORDER BY us.{Q("Rating")} DESC
                    LIMIT @TopCount"
                    : $@"SELECT TOP (@TopCount) us.{Q("UserID")}, us.{Q("Wins")}, us.{Q("Losses")}, us.{Q("Draws")}, 
                           us.{Q("CurrentWinStreak")}, us.{Q("BestWinStreak")}, us.{Q("Rating")}, us.{Q("LastGameEndedAt")},
                           u.{Q("Username")}
                    FROM {statsTable} us
                    INNER JOIN {usersTable} u ON us.{Q("UserID")} = u.{Q("UserID")}
                    ORDER BY us.{Q("Rating")} DESC";

                using (var command = CreateCommand(sql, connection))
                {
                    command.Parameters.Add(CreateParameter("@TopCount", topCount));

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
