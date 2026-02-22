namespace WebApplication.DataManagement
{
    /// <summary>
    /// Data Transfer Object for user login requests.
    /// Contains only the essential fields needed for authentication.
    /// </summary>
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Response object returned after successful login.
    /// Contains user information and authentication token.
    /// </summary>
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }  // JWT token for authentication
    }
}
