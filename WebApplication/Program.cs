using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
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

        public class User
        {
            public string Username { get; set; }
            public string Email { get; set; }
            public string PasswordHash { get; set; }
 //In this step, we define a User class with three fields: Username, Email,
 //and PasswordHash. These fields represent the properties of a user
 //that you want to store in your application. The get and set accessors
 //allow you to get and set the values of the fields.By default, these
 //accessors provide read-write access to the fields.Now that we have
 //defined the User class, we can use it to store the user data when
 //handling the registration endpoint.
        }
        [ApiController]
        [Route("[controller]")]
        public class  RegisterController : ControllerBase

        {
            [HttpPost]
            public IActionResult Register([FromBody] User user)
            {


                // Deserialize the JSON data into the User object.
                // Validate the User object.
                // Store the user in the database or perform other operations.

        return Ok("User registered successfully!");
            }
            
        }


    }
}