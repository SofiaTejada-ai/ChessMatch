namespace WebApplication.ChessLogic
{
    /// <summary>
    /// Represents the different types of chess pieces
    /// Each enum value corresponds to a specific chess piece with unique movement rules
    /// </summary>
    public enum PieceType
    {
        /// <summary>Pawn - moves forward 1 square (2 on first move), captures diagonally</summary>
        Pawn,
        
        /// <summary>Rook - moves horizontally or vertically any distance</summary>
        Rook,
        
        /// <summary>Knight - moves in L-shape (2,1 or 1,2 squares), can jump over pieces</summary>
        Knight,
        
        /// <summary>Bishop - moves diagonally any distance</summary>
        Bishop,
        
        /// <summary>Queen - combines rook and bishop movement (most powerful piece)</summary>
        Queen,
        
        /// <summary>King - moves 1 square in any direction, must be protected</summary>
        King
    }
}
