namespace WebApplication.DataManagement.Models 
{
    /// <summary>
    /// Defines the core user entity with properties like UserID, Username, Email, 
    /// PasswordHash, CreatedAt, LastSeenAt, and IsActive. Used for raw SQL 
    /// operations with the UsersSchema.UsersTable in your ChessHub database.
    /// </summary>
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public bool IsActive { get; set; }
    }
}