using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using WebApplication.Data;
using WebApplication.Models;
using static WebApplication.Controllers.Program;

namespace WebApplication.Controllers
{
    public class WeatherForecastController
    {
        // This class represents a controller for weather forecasts.
        // It does not need to have any service configuration logic.
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            // Create the host builder.
            var hostBuilder = new HostBuilder()
            //In ASP.NET Core, the HostBuilder class is used to configure
            //and build the host for your application. The host is responsible
            //for managing the lifetime of your application and hosting the services
            //and middleware components.
                .ConfigureServices((hostContext, services) =>
                {
                    // Add services to the container.
                    services.AddControllers();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });  

            // Build and run the host.
            hostBuilder.Build().Run();
        }

        // Models live in WebApplication/Models; DbContext is in WebApplication/Data.
        [ApiController]
        [Route("[controller]")]
        public class RegisterController : ControllerBase
        {
            private readonly ChessHubContext _db;

            public RegisterController(ChessHubContext db)
            {
                _db = db;
            }

            [HttpPost]
            public async Task<IActionResult> Register([FromBody] RegisterRequest req)
            {
                if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                    return BadRequest("Missing required fields");

                var user = new User
                {
                    Username = req.Username,
                    Email = req.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _db.Users.AddAsync(user);
                await _db.SaveChangesAsync();

                return Ok(new { message = "User registered successfully", userId = user.UserID });
            }
        }


    }
}