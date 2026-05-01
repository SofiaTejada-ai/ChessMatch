using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApplication.DataManagement;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WebApplication
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Enable CORS so frontend can connect to backend
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            
            // Add DatabaseHelper with connection string
            // Use DATABASE_URL env var on Railway/Supabase, fall back to local PostgreSQL for dev
            var rawConnStr = System.Environment.GetEnvironmentVariable("DATABASE_URL") 
                ?? "Host=localhost;Database=ChessHub;Username=postgres;Password=postgres";
            
            // Convert Supabase URI format to Npgsql key-value format
            var connectionString = ConvertToNpgsqlConnectionString(rawConnStr);
            
            services.AddSingleton(new DatabaseHelper(connectionString));
            
            // Add JWT Authentication
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "ChessHub",
                        ValidAudience = "ChessHub",
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(System.Environment.GetEnvironmentVariable("JWT_SECRET") ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set")))
                    };
                });
                
            services.AddAuthorization();
        }

        private static string ConvertToNpgsqlConnectionString(string url)
        {
            // Already key-value format
            if (!url.StartsWith("postgresql://") && !url.StartsWith("postgres://"))
                return url;

            // Strip scheme
            var withoutScheme = url.Replace("postgresql://", "").Replace("postgres://", "");
            
            // Split userinfo from hostinfo at the last @ before the host
            var atIndex = withoutScheme.LastIndexOf('@');
            var userInfo = withoutScheme.Substring(0, atIndex);
            var hostInfo = withoutScheme.Substring(atIndex + 1);

            // Split username:password (only first colon is the separator)
            var colonIndex = userInfo.IndexOf(':');
            var username = Uri.UnescapeDataString(colonIndex >= 0 ? userInfo.Substring(0, colonIndex) : userInfo);
            var password = Uri.UnescapeDataString(colonIndex >= 0 ? userInfo.Substring(colonIndex + 1) : "");

            // Split host:port/database
            var slashIndex = hostInfo.IndexOf('/');
            var hostPort = slashIndex >= 0 ? hostInfo.Substring(0, slashIndex) : hostInfo;
            var database = slashIndex >= 0 ? hostInfo.Substring(slashIndex + 1).Split('?')[0] : "postgres";
            var hostParts = hostPort.Split(':');
            var host = hostParts[0];
            var port = hostParts.Length > 1 ? hostParts[1] : "5432";

            // Supabase pooler requires: no prepared statements, small pool, short idle lifetime
            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Maximum Pool Size=5;Connection Idle Lifetime=30;Max Auto Prepare=0;No Reset On Close=true;Include Error Detail=true";
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Force CORS headers on every single response - must be first, before auth can intercept
            // Also catches all exceptions so 500 errors don't lose their CORS headers
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                    context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS, PATCH";
                    context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, Accept";
                    context.Response.Headers["Access-Control-Expose-Headers"] = "*";
                    context.Response.Headers["Vary"] = "Origin";
                    context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
                    context.Response.Headers["Pragma"] = "no-cache";
                    return System.Threading.Tasks.Task.CompletedTask;
                });
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    await context.Response.CompleteAsync();
                    return;
                }
                try
                {
                    await next(context);
                }
                catch (Exception ex)
                {
                    // Ensure even unhandled exceptions return CORS headers
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/json";
                        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                        var msg = ex.Message.Length > 400 ? ex.Message.Substring(0, 400) : ex.Message;
                        msg = msg.Replace("\"", "'").Replace("\n", " ").Replace("\r", "");
                        await context.Response.WriteAsync($"{{\"error\":\"Internal server error\",\"type\":\"{ex.GetType().Name}\",\"message\":\"{msg}\"}}");
                    }
                }
            });

            app.UseCors("AllowFrontend");
            app.UseRouting();

            // Add authentication middleware
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                // Health check at root so Railway knows the app is running
                endpoints.MapGet("/", async context =>
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"status\":\"ok\",\"service\":\"ChessHub API\"}");
                });

                // DB diagnostic endpoint — remove after debugging
                endpoints.MapGet("/health/db", async context =>
                {
                    context.Response.ContentType = "application/json";
                    try
                    {
                        var db = context.RequestServices.GetRequiredService<DatabaseHelper>();
                        var user = await db.GetUserByIdAsync(1);
                        await context.Response.WriteAsync($"{{\"db\":\"connected\",\"user\":\"{user?.Username ?? "none"}\"}}");
                    }
                    catch (System.Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync($"{{\"db\":\"error\",\"message\":\"{ex.Message.Replace("\"", "'")}\",\"type\":\"{ex.GetType().Name}\"}}");
                    }
                });
            });
        }
    }
}
