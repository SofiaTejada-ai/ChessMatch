using System.Collections.Generic;

namespace WebApplication.ChessLogic
{
    /// <summary>
    /// Base abstract class for all chess pieces
    /// Contains common properties and methods that all chess pieces share
    /// </summary>
    public abstract class ChessPiece
    {
        public PieceColor Color { get; }
        public PieceType Type { get; }
        public (int Row, int Col) Position { get; private set; }
        public bool HasMoved { get; private set; }

        protected ChessPiece(PieceColor color, PieceType type, int row, int col)
        {
            Color = color;
            Type = type;
            Position = (row, col);
            HasMoved = false;
        }

        /// <summary>
        /// Abstract method that each piece type must implement
        /// Returns all possible valid moves for this piece on the given board
        /// </summary>
        public abstract List<(int Row, int Col)> GetPossibleMoves(ChessPiece?[,] board);

        /// <summary>
        /// Moves the piece to a new position and marks it as having moved
        /// Important for tracking pawn double moves and castling eligibility
        /// </summary>
        public void MoveTo(int row, int col)
        {
            Position = (row, col);
            HasMoved = true;
        }

        /// <summary>
        /// Helper method to check if a position is within the 8x8 chess board
        /// </summary>
        protected bool IsInsideBoard(int row, int col)
        {
            return row >= 0 && row < 8 && col >= 0 && col < 8;
        }

        /// <summary>
        /// Gets the standard chess notation for this piece
        /// Used for move notation and display purposes
        /// </summary>
        public virtual string GetNotation()
        {
            return Type switch
            {
                PieceType.King => "K",
                PieceType.Queen => "Q",
                PieceType.Rook => "R",
                PieceType.Bishop => "B",
                PieceType.Knight => "N",
                PieceType.Pawn => "",
                _ => ""
            };
        }
    }
}
