using System;
using System.ComponentModel.DataAnnotations;
//Brings in attributes like [Key] for defining primary keys in the model classes.

namespace WebApplication.Models
{
    public class UserStats
    {
        [Key]
        public int UserID { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int CurrentWinStreak { get; set; }
        public int BestWinStreak { get; set; }
        public int Rating { get; set; }
        public DateTime? LastGameEndedAt { get; set; }
    }
}