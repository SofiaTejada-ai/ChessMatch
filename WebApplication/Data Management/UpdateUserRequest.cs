namespace WebApplication.DataManagement
{
    /// <summary>
    /// Data Transfer Object for updating user profile information.
    /// Only contains fields that users are allowed to update.
    /// </summary>
    public class UpdateUserRequest
    {
        public string? Email { get; set; }
        public string? Username { get; set; }
    }

    /// <summary>
    /// Response object containing user profile information.
    /// Excludes sensitive information like password hash.
    /// </summary>
    public class UserProfileResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public bool IsActive { get; set; }
    }
}
