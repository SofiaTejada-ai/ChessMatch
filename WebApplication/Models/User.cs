using System;
using System.ComponentModel.DataAnnotations;
//Brings in attributes like [Key] for defining primary keys in the model classes.

namespace WebApplication.Models // Note: actual namespace depends on the project name.
{
    public class User
    {
        [Key] 
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public bool IsActive { get; set; }
    }
}