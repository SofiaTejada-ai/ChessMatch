using System.Collections.Generic;

namespace WebApplication.ChessLogic
{
    /// <summary>
    /// King chess piece implementation
    /// Moves one square in any direction
    /// Must be protected at all costs - game ends when checkmated
    /// </summary>
    public class King : ChessPiece
    {
        public King(PieceColor color, int row, int col) : base(color, PieceType.King, row, col) { }

        public override List<(int Row, int Col)> GetPossibleMoves(ChessPiece?[,] board)
        {
            var moves = new List<(int, int)>();
            
            // King can move one square in any direction
            int[,] offsets = { 
                { -1, -1 }, { -1, 0 }, { -1, 1 },
                { 0, -1 },           { 0, 1 },
                { 1, -1 },  { 1, 0 }, { 1, 1 }
            };

            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                int newRow = Position.Row + offsets[i, 0];
                int newCol = Position.Col + offsets[i, 1];

                if (IsInsideBoard(newRow, newCol) &&
                    (board[newRow, newCol] == null || board[newRow, newCol]!.Color != Color))
                {
                    moves.Add((newRow, newCol));
                }
            }

            // TODO: Add castling logic later
            // Castling is a special king move that requires:
            // 1. King hasn't moved
            // 2. Rook hasn't moved
            // 3. No pieces between king and rook
            // 4. King is not in check
            // 5. King doesn't pass through or end up in check

            return moves;
        }
    }

    /// <summary>
    /// Pawn chess piece implementation
    /// Most complex piece due to special moves:
    /// - Forward movement only
    /// - Double move on first turn
    /// - Diagonal capture only
    /// - En passant capture
    /// - Promotion at opposite end
    /// </summary>
    public class Pawn : ChessPiece
    {
        public Pawn(PieceColor color, int row, int col) : base(color, PieceType.Pawn, row, col) { }

        public override List<(int Row, int Col)> GetPossibleMoves(ChessPiece?[,] board)
        {
            var moves = new List<(int, int)>();
            
            // Pawns move in different directions based on color
            int direction = Color == PieceColor.White ? 1 : -1;
            int startRow = Color == PieceColor.White ? 1 : 6;

            // Forward move (1 square)
            int newRow = Position.Row + direction;
            if (IsInsideBoard(newRow, Position.Col) && board[newRow, Position.Col] == null)
            {
                moves.Add((newRow, Position.Col));

                // Double move on first turn (2 squares)
                if (Position.Row == startRow)
                {
                    newRow += direction;
                    if (IsInsideBoard(newRow, Position.Col) && board[newRow, Position.Col] == null)
                    {
                        moves.Add((newRow, Position.Col));
                    }
                }
            }

            // Diagonal captures
            int[] captureCols = { -1, 1 };
            foreach (var colOffset in captureCols)
            {
                int newCol = Position.Col + colOffset;
                newRow = Position.Row + direction;

                if (IsInsideBoard(newRow, newCol))
                {
                    var targetPiece = board[newRow, newCol];
                    if (targetPiece != null && targetPiece.Color != Color)
                    {
                        moves.Add((newRow, newCol));
                    }
                }
            }

            // TODO: Add en passant logic later
            // En passant is a special pawn capture that occurs when:
            // 1. Enemy pawn moves 2 squares forward from starting position
            // 2. Your pawn is positioned to capture it as if it moved only 1 square
            // 3. Capture must be done immediately on the next turn

            return moves;
        }

        /// <summary>
        /// Check if pawn can be promoted (reached opposite end of board)
        /// </summary>
        public bool CanBePromoted()
        {
            return (Color == PieceColor.White && Position.Row == 7) ||
                   (Color == PieceColor.Black && Position.Row == 0);
        }
    }
}
