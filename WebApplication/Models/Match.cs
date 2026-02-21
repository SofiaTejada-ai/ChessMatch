using System;
using System.ComponentModel.DataAnnotations;
//Brings in attributes like [Key] for defining primary keys in the model classes.

namespace WebApplication.Models
{
    public class Match
    {
        [Key]
        public int MatchID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int WhiteUserID { get; set; }
        public int BlackUserID { get; set; }
        public int? WinnerID { get; set; }
        public string MatchState { get; set; }
        public string Result { get; set; }
        public string EndReason { get; set; }
        public string MatchType { get; set; }
        public string InviteCode { get; set; }
    }
}