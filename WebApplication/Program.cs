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
            private readonly string _jwtKey = "YourSuperSecretKeyForChessApp123456789";
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
            //HTTP GET method that retrieves a specific
            //match by ID and verifies the current user is part of that match.
            public async Task<IActionResult> GetMatch(int matchId)
            {
                var userId = GetCurrentUserId();
                var match = await _db.GetMatchByIdAsync(matchId);
                if (match == null)
                    return NotFound("Match not found");

                if (match.WhiteUserID != userId && match.BlackUserID != userId)
                    return Forbid("You are not part of this match");

                return Ok(match);
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
                if (match == null)
                    return NotFound("Match not found");

                if (match.WhiteUserID != userId && match.BlackUserID != userId)
                    return Forbid("You are not part of this match");

                return Ok(new { message = "Move accepted", move = req.Move });
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
