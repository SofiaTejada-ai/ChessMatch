namespace WebApplication.DataManagement.Models
{
    /// <summary>
    /// Represents chess game matches with properties for MatchID, creation/end timestamps, 
    /// player IDs (WhiteUserID, BlackUserID, WinnerID), game state (MatchState, Result, EndReason), 
    /// match type (Random/Friend/Direct), and an invite code for friend matches. 
    /// Used for raw SQL operations with MatchesSchema.MatchesTable.
    /// </summary>
    public class Match
    {
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