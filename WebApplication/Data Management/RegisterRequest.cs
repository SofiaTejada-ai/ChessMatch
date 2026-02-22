/// <summary>
/// Data Transfer Object used for user registration that contains only Username, Email, 
/// and Password properties. Unlike User.cs, it doesn't include database-specific 
/// fields like UserID or CreatedAt and is specifically designed to handle 
/// registration form submissions.
/// </summary>
namespace WebApplication.DataManagement
{
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}