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
            var connectionString = System.Environment.GetEnvironmentVariable("DATABASE_URL") 
                ?? "Host=localhost;Database=ChessHub;Username=postgres;Password=postgres";
            
            // Supabase requires SSL — ensure it's set
            if (!connectionString.Contains("SSL Mode") && !connectionString.Contains("sslmode"))
            {
                if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
                    connectionString += connectionString.Contains("?") ? "&sslmode=require" : "?sslmode=require";
                else
                    connectionString += ";SSL Mode=Require;Trust Server Certificate=true";
            }
            
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
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("REDACTED"))
                    };
                });
                
            services.AddAuthorization();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // Enable CORS before auth
            app.UseCors("AllowFrontend");

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
            });
        }
    }
}
