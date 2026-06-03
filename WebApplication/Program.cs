using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using WebApplication.DataManagement;
using WebApplication.DataManagement.Models;
using WebApplication.ChessLogic;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WebApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
            
            // Add services from Startup
            var startup = new Startup();
            startup.ConfigureServices(builder.Services);
            
            var app = builder.Build();
            
            // Configure middleware from Startup
            startup.Configure(app, app.Environment);
            
            app.Run();
        }
    }

    [ApiController]
    [Route("[controller]")]
    public class RegisterController : ControllerBase 
    {
            private readonly DatabaseHelper _db; //declares private field
            //container for DatabaseHelper instance

            public RegisterController(DatabaseHelper db)
            {
                _db = db; //Contrustor that recieves DatabaseHelper instance
                //and assigns it to the private field
            }

            [HttpPost]
            public async Task<IActionResult> Register([FromBody] RegisterRequest req)
            //Method that handles HTTP POST requests for user registration
            //Accepts RegisterRequest object as a parameter
            {
                if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                    return BadRequest("Missing required fields");

                var user = new User
                {
                    Username = req.Username,
                    Email = req.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password), //Hashes password
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var userId = await _db.CreateUserAsync(user); //Calls CreateUserAsync method from DatabaseHelper.cs
                return Ok(new { message = "User registered successfully", userId = userId });
            }
        }

        [ApiController]
        [Route("[controller]")]
        //Attributes that mark this class as an API controller and set its URL route.
       // An API Controller is a class that handles HTTP requests and returns HTTP responses for web APIs
        public class LoginController : ControllerBase 
        //Class definition for LoginController that inherits from ControllerBase
        {
            private readonly DatabaseHelper _db; //Private readonly field that stores a DatabaseHelper instance
           //readonly means that the value cannot be changed after initialization
            private static readonly string _jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set");
            //Stores a JWT (JSON Web Token) key used for authentication
            //JWT Key is a secret password used to create and verify JWT tokens for secure authentication
            public LoginController(DatabaseHelper db)
            {
                _db = db;
            }

            [HttpPost]
            public async Task<IActionResult> Login([FromBody] LoginRequest req)
            {
                if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                    return BadRequest("Missing username or password");

                var user = await _db.GetUserByUsernameAsync(req.Username);
                if (user == null)
                    return Unauthorized("Invalid username or password");

                if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                    return Unauthorized("Invalid username or password");

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtKey);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[] { 
                        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email)
                    }),
                    // creates user identity claims containing user ID, username, and email 
                    //claims store user information in the JWT token so the server can identiy who made the request
                    Expires = DateTime.UtcNow.AddDays(7),
                    //Sets the expiration time for the token
                    Issuer = "ChessHub",
                    Audience = "ChessHub",
                    //issuer identifies who created the token, Audience identifies who can use the token
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    //Specifies the signing credentials for the token
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Ok(new LoginResponse 
                { 
                    UserId = user.UserID,
                    Username = user.Username,
                    Email = user.Email,
                    Token = tokenString
                });
            }
        }

        [ApiController]
        [Route("demo")]
        public class DemoController : ControllerBase
        {
            private readonly DatabaseHelper _db;
            private static readonly string _jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set");

            public DemoController(DatabaseHelper db)
            {
                _db = db;
            }

            // POST /demo/session — creates a guest demo user and returns a JWT token
            [HttpPost("session")]
            [HttpGet("session")]
            public async Task<IActionResult> StartSession()
            {
                // Create a unique guest username
                var guestUsername = $"guest_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                var guestEmail = $"{guestUsername}@demo.chesshub.com";
                var guestPassword = Guid.NewGuid().ToString();

                int userId;
                try
                {
                    var user = new User
                    {
                        Username = guestUsername,
                        Email = guestEmail,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(guestPassword),
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    userId = await _db.CreateUserAsync(user);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = "Could not create demo user", detail = ex.GetType().Name, message = ex.Message.Length > 200 ? ex.Message.Substring(0, 200) : ex.Message });
                }

                // Generate JWT token for the guest
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtKey);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[] {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Name, guestUsername),
                        new Claim(ClaimTypes.Email, guestEmail),
                        new Claim("isGuest", "true")
                    }),
                    Expires = DateTime.UtcNow.AddDays(1),
                    Issuer = "ChessHub",
                    Audience = "ChessHub",
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Ok(new
                {
                    userId = userId,
                    username = guestUsername,
                    token = tokenString,
                    isGuest = true
                });
            }
        }

        [ApiController]
        [Route("[controller]")]
        [Authorize]
        public class UserController : ControllerBase
        //Class definition for UserController that inherits from ControllerBase (Microsoft.AspNetCore.Mvc.ControllerBase from the ASP.NET Core framework.)
        //Attributes that mark this class as an API controller and set its URL route.
       // An API Controller is a class that handles HTTP requests and returns HTTP responses for web APIs
        {
            private readonly DatabaseHelper _db;
            //Private readonly field that stores a DatabaseHelper instance
           //readonly means that the value cannot be changed after initialization

            public UserController(DatabaseHelper db)
            {
                _db = db;
            }

            private int GetCurrentUserId()
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                //Finds the user ID claim from the JWT token in the current HTTP request
                //User IDs are automatically generated by the database when a new user is created.
                if (userIdClaim == null)
                    throw new UnauthorizedAccessException("Invalid token");
                //If the user ID claim is not found, throws an exception
                return int.Parse(userIdClaim.Value);
            }

            [HttpGet("profile")]
            public async Task<IActionResult> GetProfile()
            {
                var userId = GetCurrentUserId();
                var user = await _db.GetUserByIdAsync(userId);
                if (user == null)
                    return NotFound("User not found");

                return Ok(new UserProfileResponse
                {
                    UserId = user.UserID,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    LastSeenAt = user.LastSeenAt,
                    IsActive = user.IsActive
                });
            }

            [HttpPut("profile")]
            public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest req)
            {
                var userId = GetCurrentUserId();
                var user = await _db.GetUserByIdAsync(userId);
                if (user == null)
                    return NotFound("User not found");

                if (!string.IsNullOrWhiteSpace(req.Username))
                    user.Username = req.Username;
                    
                if (!string.IsNullOrWhiteSpace(req.Email))
                    user.Email = req.Email;

                user.LastSeenAt = DateTime.UtcNow;
                await _db.UpdateUserAsync(user);

                return Ok(new UserProfileResponse
                {
                    UserId = user.UserID,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    LastSeenAt = user.LastSeenAt,
                    IsActive = user.IsActive
                });
            }
        }

        [ApiController]
        [Route("[controller]")]
        [Authorize]
        //ChessController class that handles chess game
        //  HTTP requests and requires JWT authentication.
        public class ChessController : ControllerBase
        {
            private readonly DatabaseHelper _db;

            public ChessController(DatabaseHelper db)
            {
                _db = db;
            }

            private int GetCurrentUserId()
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    throw new UnauthorizedAccessException("Invalid token");
                return int.Parse(userIdClaim.Value);
            }

            // POST /chess/match/ai — creates a match against the ChessHub AI (user_id = 1)
            // Body: { difficulty: "easy"|"medium"|"hard" }  (difficulty stored in invite_code)
            [HttpPost("match/ai")]
            public async Task<IActionResult> CreateAiMatch([FromBody] CreateAiMatchRequest? req)
            {
                try
                {
                    var userId = GetCurrentUserId();
                    var difficulty = req?.Difficulty ?? "medium";
                    if (difficulty != "easy" && difficulty != "medium" && difficulty != "hard")
                        difficulty = "medium";

                    var match = new Match
                    {
                        WhiteUserID = userId,
                        BlackUserID = 1,
                        MatchState  = "Active",
                        MatchType   = "Direct",
                        InviteCode  = difficulty, // reuse invite_code to store difficulty for AI matches
                        CreatedAt   = DateTime.UtcNow
                    };

                    var matchId = await _db.CreateMatchAsync(match);
                    return Ok(new { message = "AI match created", matchId = matchId, opponentName = "ChessHub AI" });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = "Could not create AI match", detail = ex.GetType().Name, message = ex.Message.Length > 300 ? ex.Message.Substring(0, 300) : ex.Message });
                }
            }

            [HttpPost("match/create")]
            public async Task<IActionResult> CreateMatch([FromBody] CreateMatchRequest req)
            {
                var userId = GetCurrentUserId();

                if (req.MatchType != "Random" && req.MatchType != "Friend" && req.MatchType != "Direct")
                    return BadRequest("Invalid match type");

                if (req.MatchType == "Friend" && req.OpponentId == null)
                    return BadRequest("Opponent ID is required for Friend matches");

                //Whoever creates the match is White player and 
                // the invited opponent is Black player
                var match = new Match
                {
                    WhiteUserID = userId,
                    BlackUserID = req.OpponentId ?? 0, //Sets BlackUserID
                    //  to the opponent ID if provided, otherwise sets it to 0
                    MatchState = "Active",
                    MatchType = req.MatchType,
                    InviteCode = req.MatchType == "Friend" ? GenerateInviteCode() : null,
                    //Sets InviteCode to a generated code if match
                    //  type is Friend, otherwise sets it to null.
                    CreatedAt = DateTime.UtcNow
                };

                if (req.MatchType == "Random")
                {
                    match.BlackUserID = userId;
                }

                var matchId = await _db.CreateMatchAsync(match);
                return Ok(new { message = "Match created successfully", matchId = matchId });
            }

            [HttpGet("match/{matchId}")]
            public async Task<IActionResult> GetMatch(int matchId)
            {
                var userId = GetCurrentUserId();
                var match  = await _db.GetMatchByIdAsync(matchId);
                if (match == null) return NotFound("Match not found");
                if (match.WhiteUserID != userId && match.BlackUserID != userId)
                    return Forbid("You are not part of this match");

                var whiteUser  = await _db.GetUserByIdAsync(match.WhiteUserID);
                var blackUser  = await _db.GetUserByIdAsync(match.BlackUserID);
                var storedMoves = await _db.GetMovesForMatchAsync(matchId);

                var board = ChessBoard.Replay(storedMoves.Select(m => m.MoveNotation));
                var turn  = board.CurrentTurn == PieceColor.White ? "white" : "black";
                bool inCheck    = turn == "white" ? board.WhiteKingInCheck : board.BlackKingInCheck;
                bool checkmate  = board.GameStatus != "Active";

                return Ok(new
                {
                    matchId     = match.MatchID,
                    createdAt   = match.CreatedAt,
                    matchState  = match.MatchState,
                    matchType   = match.MatchType,
                    result      = match.Result,
                    endReason   = match.EndReason,
                    inviteCode  = (string?)null, // invite_code stores difficulty for AI matches — don't expose
                    whiteUserId = match.WhiteUserID,
                    blackUserId = match.BlackUserID,
                    players     = new { white = whiteUser?.Username ?? "Unknown", black = blackUser?.Username ?? "Unknown" },
                    board       = BoardToArray(board),
                    turn,
                    moves       = storedMoves.Select(m => m.MoveNotation).ToList(),
                    check       = inCheck,
                    checkmate
                });
            }

            [HttpGet("matches")]
            //HTTP GET method that retrieves all matches for the current user.
            public async Task<IActionResult> GetUserMatches()
            {
                var userId = GetCurrentUserId();
                var matches = await _db.GetUserMatchesAsync(userId);
                return Ok(matches);
            }

            [HttpPost("match/{matchId}/move")]
            public async Task<IActionResult> MakeMove(int matchId, [FromBody] MakeMoveRequest req)
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(req.Move) || req.Move.Length < 4)
                    return BadRequest("Invalid move format");

                var match = await _db.GetMatchByIdAsync(matchId);
                if (match == null) return NotFound("Match not found");
                if (match.WhiteUserID != userId && match.BlackUserID != userId)
                    return Forbid("You are not part of this match");
                if (match.MatchState != "Active")
                    return BadRequest("Match is not active");

                var storedMoves = await _db.GetMovesForMatchAsync(matchId);
                var board = ChessBoard.Replay(storedMoves.Select(m => m.MoveNotation));

                // Verify it's this player's turn
                bool isWhitePlayer = match.WhiteUserID == userId;
                bool isWhiteTurn   = board.CurrentTurn == PieceColor.White;
                if (isWhitePlayer != isWhiteTurn)
                    return BadRequest("Not your turn");

                // Parse and apply move
                var mv = req.Move.ToLower();
                int fromCol = mv[0] - 'a', fromRow = mv[1] - '1';
                int toCol   = mv[2] - 'a', toRow   = mv[3] - '1';

                if (!board.MakeMove(fromRow, fromCol, toRow, toCol))
                    return BadRequest("Illegal move");

                var newFen = board.ToFEN();
                await _db.CreateMoveAsync(matchId, userId, mv, mv, newFen);

                // Check for game-over after player move
                if (board.GameStatus != "Active")
                {
                    string result    = board.GameStatus == "WhiteWins" ? "WhiteWin" : board.GameStatus == "BlackWins" ? "BlackWin" : "Draw";
                    string endReason = board.GameStatus == "Draw"      ? "Stalemate" : "Checkmate";
                    int? winnerId    = board.GameStatus == "WhiteWins" ? match.WhiteUserID : board.GameStatus == "BlackWins" ? match.BlackUserID : (int?)null;
                    await _db.EndMatchAsync(matchId, result, endReason, winnerId);
                    return Ok(new { message = "Move accepted", move = mv, gameOver = true, result, endReason });
                }

                // AI response (blackUserId == 1 means ChessHub AI)
                if (match.BlackUserID == 1)
                {
                    var difficulty = match.InviteCode ?? "medium";
                    var aiMove = await GetGeminiMoveAsync(newFen, difficulty, board);
                    if (aiMove != null)
                    {
                        int aiFromCol = aiMove[0] - 'a', aiFromRow = aiMove[1] - '1';
                        int aiToCol   = aiMove[2] - 'a', aiToRow   = aiMove[3] - '1';
                        if (board.MakeMove(aiFromRow, aiFromCol, aiToRow, aiToCol))
                        {
                            var aiFen = board.ToFEN();
                            await _db.CreateMoveAsync(matchId, 1, aiMove, aiMove, aiFen);

                            if (board.GameStatus != "Active")
                            {
                                string result    = board.GameStatus == "WhiteWins" ? "WhiteWin" : board.GameStatus == "BlackWins" ? "BlackWin" : "Draw";
                                string endReason = board.GameStatus == "Draw"      ? "Stalemate" : "Checkmate";
                                int? winnerId    = board.GameStatus == "WhiteWins" ? match.WhiteUserID : board.GameStatus == "BlackWins" ? match.BlackUserID : (int?)null;
                                await _db.EndMatchAsync(matchId, result, endReason, winnerId);
                            }
                        }
                    }
                }

                return Ok(new { message = "Move accepted", move = mv });
            }

            [HttpPost("match/{matchId}/forfeit")]
            public async Task<IActionResult> Forfeit(int matchId)
            {
                var userId = GetCurrentUserId();
                var match  = await _db.GetMatchByIdAsync(matchId);
                if (match == null) return NotFound("Match not found");
                if (match.WhiteUserID != userId && match.BlackUserID != userId)
                    return Forbid("You are not part of this match");
                if (match.MatchState != "Active")
                    return BadRequest("Match is not active");

                int? winnerId = match.WhiteUserID == userId ? match.BlackUserID : match.WhiteUserID;
                string result = match.WhiteUserID == userId ? "BlackWin" : "WhiteWin";
                await _db.EndMatchAsync(matchId, result, "Resignation", winnerId);
                return Ok(new { message = "Match forfeited" });
            }

            [HttpGet("match/{matchId}/moves")]
            public async Task<IActionResult> GetMoves(int matchId)
            {
                var userId = GetCurrentUserId();
                var match  = await _db.GetMatchByIdAsync(matchId);
                if (match == null) return NotFound("Match not found");
                if (match.WhiteUserID != userId && match.BlackUserID != userId)
                    return Forbid("You are not part of this match");

                var moves = await _db.GetMovesForMatchAsync(matchId);
                return Ok(moves.Select((m, i) => new
                {
                    moveId     = i + 1,
                    matchId,
                    playerUserId                    = m.PlayerUserId,
                    moveNotation                    = m.MoveNotation,
                    moveStandardAlgebraicNotation   = m.MoveNotation,
                    moveForsythEdwardsNotation      = m.FEN,
                    moveTime                        = DateTime.UtcNow
                }));
            }

            [HttpPost("match/{matchId}/chat/ask-ai")]
            public async Task<IActionResult> AskAI(int matchId)
            {
                var userId = GetCurrentUserId();
                var match  = await _db.GetMatchByIdAsync(matchId);
                if (match == null) return NotFound("Match not found");
                if (match.WhiteUserID != userId && match.BlackUserID != userId)
                    return Forbid("You are not part of this match");

                var storedMoves = await _db.GetMovesForMatchAsync(matchId);
                var board = ChessBoard.Replay(storedMoves.Select(m => m.MoveNotation));
                var fen   = board.ToFEN();
                var lastMove = storedMoves.LastOrDefault()?.MoveNotation ?? "none";

                var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                    return Ok(new { message = "AI analysis unavailable (no API key configured)." });

                var prompt = $"You are ChessHub AI, a chess opponent. The current position (FEN): {fen}. The last move was: {lastMove}. In 1-2 sentences, explain what that move did tactically and hint at what you (as Black) are thinking about next. Be a little competitive about it.";
                var (reply, askError) = await CallGeminiTextAsync(apiKey, prompt);
                if (reply == null)
                {
                    var msg = askError == "RATE_LIMITED"
                        ? "My thinking quota ran out — even grandmasters need a breather. Try again in a minute!"
                        : $"[AI analysis unavailable: {askError}]";
                    return Ok(new { message = msg });
                }

                await _db.CreateChatMessageAsync(matchId, 1, reply);
                return Ok(new { message = reply });
            }

            // ── Helpers ──────────────────────────────────────────────────────────

            private static string[][] BoardToArray(ChessBoard board)
            {
                // Frontend row 0 = rank 8 = backend row 7 (black's home rank)
                var arr = new string[8][];
                for (int r = 0; r < 8; r++)
                {
                    arr[r] = new string[8];
                    for (int c = 0; c < 8; c++)
                    {
                        var piece = board.Squares[7 - r, c];
                        if (piece == null) { arr[r][c] = ""; continue; }
                        char ch = piece.Type switch
                        {
                            PieceType.King   => 'K',
                            PieceType.Queen  => 'Q',
                            PieceType.Rook   => 'R',
                            PieceType.Bishop => 'B',
                            PieceType.Knight => 'N',
                            PieceType.Pawn   => 'P',
                            _ => '?'
                        };
                        arr[r][c] = piece.Color == PieceColor.White ? ch.ToString() : char.ToLower(ch).ToString();
                    }
                }
                return arr;
            }

            private async Task<string?> GetGeminiMoveAsync(string fen, string difficulty, ChessBoard board)
            {
                var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (string.IsNullOrEmpty(apiKey)) return FallbackMove(board);

                var difficultyNote = difficulty switch
                {
                    "easy" => "You are a beginner. Make simple, occasionally suboptimal moves.",
                    "hard" => "You are a grandmaster. Make the strongest possible move.",
                    _      => "You are an intermediate player. Make solid, reasonable moves."
                };
                var prompt = $"You are playing chess as Black. {difficultyNote}\nCurrent position (FEN): {fen}\nReply with ONLY your move in UCI format (e.g. e7e5). Nothing else — just the 4-character move string.";
                var (reply, _) = await CallGeminiTextAsync(apiKey, prompt);

                if (reply != null)
                {
                    var mv = reply.Trim().ToLower().Split(' ')[0];
                    if (mv.Length >= 4 && char.IsLetter(mv[0]) && char.IsDigit(mv[1]) && char.IsLetter(mv[2]) && char.IsDigit(mv[3]))
                    {
                        int fc = mv[0]-'a', fr = mv[1]-'1', tc = mv[2]-'a', tr = mv[3]-'1';
                        if (board.IsValidMove(fr, fc, tr, tc))
                            return mv.Substring(0, 4);
                    }
                }
                return FallbackMove(board);
            }

            private static string? FallbackMove(ChessBoard board)
            {
                // Pick the first legal move for the current player
                for (int r = 0; r < 8; r++)
                    for (int c = 0; c < 8; c++)
                    {
                        var piece = board.Squares[r, c];
                        if (piece == null || piece.Color != board.CurrentTurn) continue;
                        var moves = board.GetValidMoves(r, c);
                        if (moves.Count == 0) continue;
                        var (tr, tc) = moves[0];
                        return $"{(char)('a' + c)}{r + 1}{(char)('a' + tc)}{tr + 1}";
                    }
                return null;
            }

            private static async Task<(string? text, string? error)> CallGeminiTextAsync(string apiKey, string prompt)
            {
                try
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                    var url  = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={apiKey}";
                    var body = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        contents = new[] { new { parts = new[] { new { text = prompt } } } }
                    });
                    var resp = await http.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
                    var responseText = await resp.Content.ReadAsStringAsync();
                    if ((int)resp.StatusCode == 429)
                        return (null, "RATE_LIMITED");
                    if (!resp.IsSuccessStatusCode)
                        return (null, $"HTTP {(int)resp.StatusCode}: {responseText.Substring(0, Math.Min(200, responseText.Length))}");
                    var doc = System.Text.Json.JsonDocument.Parse(responseText);
                    var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
                    return (text, null);
                }
                catch (Exception ex)
                {
                    return (null, ex.Message.Substring(0, Math.Min(150, ex.Message.Length)));
                }
            }

            [HttpGet("match/{matchId}/chat")]
            public async Task<IActionResult> GetChat(int matchId)
            {
                var userId = GetCurrentUserId();
                var match  = await _db.GetMatchByIdAsync(matchId);
                if (match == null) return NotFound("Match not found");
                if (match.WhiteUserID != userId && match.BlackUserID != userId) return Forbid();
                var messages = await _db.GetChatMessagesAsync(matchId);
                // Return camelCase shape the frontend expects: { messageId, user, text, sentAt }
                return Ok(messages.Select(m => new
                {
                    messageId = m.MessageId,
                    user      = m.User,
                    text      = m.Text,
                    sentAt    = m.SentAt
                }));
            }

            [HttpPost("match/{matchId}/chat")]
            public async Task<IActionResult> SendChat(int matchId, [FromBody] SendChatRequest req)
            {
                var userId = GetCurrentUserId();
                var match  = await _db.GetMatchByIdAsync(matchId);
                if (match == null) return NotFound("Match not found");
                if (match.WhiteUserID != userId && match.BlackUserID != userId) return Forbid();
                if (string.IsNullOrWhiteSpace(req.Message)) return BadRequest("Message cannot be empty");

                // Snapshot conversation history BEFORE storing new message
                var history = await _db.GetChatMessagesAsync(matchId);

                // Store the player's message
                await _db.CreateChatMessageAsync(matchId, userId, req.Message.Trim());

                // AI match: generate a contextual Gemini reply
                if (match.BlackUserID == 1)
                {
                    var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        // Build conversation history string
                        var historyText = history.Count == 0
                            ? "(no previous messages)"
                            : string.Join("\n", history.Select(m =>
                                $"{(m.SenderUserId == 1 ? "ChessHub AI" : "Player")}: {m.Text}"));

                        // Get current game state for context
                        var storedMoves = await _db.GetMovesForMatchAsync(matchId);
                        var board       = ChessBoard.Replay(storedMoves.Select(m => m.MoveNotation));
                        var fen         = board.ToFEN();
                        var turn        = board.CurrentTurn == PieceColor.White ? "White" : "Black";

                        var prompt = $@"You are ChessHub AI — an intelligent, competitive chess opponent with personality. You are in the middle of a chess game against the player.

Game state:
- Position (FEN): {fen}
- Moves played so far: {storedMoves.Count}
- It is {turn}'s turn next

Conversation history (most recent at bottom):
{historyText}

Player just said: ""{req.Message.Trim()}""

Reply as ChessHub AI. Rules:
- Keep it SHORT: 1-2 sentences maximum
- Stay in character: you are an AI chess opponent — competitive, clever, occasionally witty
- If the player asks about the game or strategy, give a brief comment about the current position
- If they trash-talk, respond confidently
- If they ask for help, give a small hint without giving away the best move
- Never break character or acknowledge you are a large language model";

                                var (aiReply, aiError) = await CallGeminiTextAsync(apiKey, prompt);
                        if (aiReply != null)
                        {
                            var clean = aiReply.Trim();
                            await _db.CreateChatMessageAsync(matchId, 1, clean);
                            return Ok(new { message = "Message sent", aiReply = clean });
                        }
                        var debugMsg = aiError == "RATE_LIMITED"
                            ? "My thinking quota ran out — even grandmasters need a breather. Try again in a minute!"
                            : $"[AI unavailable: {aiError}]";
                        await _db.CreateChatMessageAsync(matchId, 1, debugMsg);
                        return Ok(new { message = "Message sent", aiReply = debugMsg });
                    }
                    // No API key configured
                    var noKeyMsg = "[AI unavailable: GEMINI_API_KEY not set in backend service]";
                    await _db.CreateChatMessageAsync(matchId, 1, noKeyMsg);
                    return Ok(new { message = "Message sent", aiReply = noKeyMsg });
                }

                return Ok(new { message = "Message sent" });
            }

            private string GenerateInviteCode()
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var random = new Random();
                var code = new char[6];
                for (int i = 0; i < code.Length; i++)
                {
                    code[i] = chars[random.Next(chars.Length)];
                }
                return new string(code);
            }
        }
}

namespace WebApplication.DataManagement
{
    public class SendChatRequest
    {
        public string Message { get; set; } = "";
    }

    public class CreateAiMatchRequest
    {
        public string Difficulty { get; set; } = "medium";
    }
}
