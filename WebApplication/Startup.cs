using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using WebApplication.DataManagement;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using System.IO;

namespace WebApplication
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

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
            
            // Determine database connection string and type
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            string connectionString;
            bool usePostgres = false;

            if (!string.IsNullOrEmpty(databaseUrl))
            {
                // Railway provides DATABASE_URL in postgres:// format
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');
                var password = Uri.UnescapeDataString(userInfo[1]);
                connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={password};SSL Mode=Require;Trust Server Certificate=true";
                usePostgres = true;
            }
            else
            {
                // Local development: read from appsettings
                connectionString = _configuration.GetConnectionString("ChessHubDb")!;
            }

            services.AddSingleton(new DatabaseHelper(connectionString, usePostgres));
            
            // Read JWT settings from config or environment variables
            var jwtKey = Environment.GetEnvironmentVariable("JWT__Key") 
                      ?? Environment.GetEnvironmentVariable("JWT__Secret")
                      ?? _configuration["Jwt:Key"]!;
            var jwtIssuer = Environment.GetEnvironmentVariable("JWT__Issuer") 
                         ?? _configuration["Jwt:Issuer"]!;
            var jwtAudience = Environment.GetEnvironmentVariable("JWT__Audience") 
                           ?? _configuration["Jwt:Audience"]!;

            // Store JWT settings so controllers can access them
            services.AddSingleton(new JwtSettings { Key = jwtKey, Issuer = jwtIssuer, Audience = jwtAudience });

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
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey))
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

            // Serve static files from wwwroot (frontend build)
            var wwwrootPath = Path.Combine(env.ContentRootPath, "wwwroot");
            if (Directory.Exists(wwwrootPath))
            {
                app.UseDefaultFiles();
                app.UseStaticFiles();
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

                // Fallback to index.html for SPA routing
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
