namespace WebApplication.DataManagement
{
    /// <summary>
    /// Request object for creating a new chess match.
    /// Contains opponent information and match type.
    /// </summary>
    public class CreateMatchRequest
    {
        public string MatchType { get; set; } = "Random"; // Random, Friend, Direct
        public int? OpponentId { get; set; } // For Direct matches
        public string? InviteCode { get; set; } // For Friend matches
    }

    /// <summary>
    /// Response object for match information with player details.
    /// Includes usernames and match status.
    /// </summary>
    public class MatchResponse
    {
        public int MatchId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string WhitePlayerUsername { get; set; }
        public string BlackPlayerUsername { get; set; }
        public string? WinnerUsername { get; set; }
        public string MatchState { get; set; }
        public string? Result { get; set; }
        public string? EndReason { get; set; }
        public string MatchType { get; set; }
        public string? InviteCode { get; set; }
    }

    /// <summary>
    /// Request object for making a chess move.
    /// Contains move information in algebraic notation.
    /// </summary>
    public class MakeMoveRequest
    {
        public string Move { get; set; } // e.g., "e2e4", "g1f3"
        public string? MoveNotation { get; set; } // Optional: "Nf3", "e4"
    }

    /// <summary>
    /// Response object for move information.
    /// Contains move details and updated game state.
    /// </summary>
    public class MoveResponse
    {
        public int MoveId { get; set; }
        public int MatchId { get; set; }
        public string PlayerUsername { get; set; }
        public string Move { get; set; }
        public string? MoveNotation { get; set; }
        public DateTime MadeAt { get; set; }
        public string Fen { get; set; } // Forsyth-Edwards Notation for board state
        public bool IsCheck { get; set; }
        public bool IsCheckmate { get; set; }
        public bool IsStalemate { get; set; }
    }
}
