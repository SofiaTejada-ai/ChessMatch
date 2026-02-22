using System.Collections.Generic;

namespace WebApplication.ChessLogic
{
    /// <summary>
    /// Rook chess piece implementation
    /// Moves horizontally or vertically any distance
    /// Cannot jump over other pieces
    /// </summary>
    public class Rook : ChessPiece
    {
        public Rook(PieceColor color, int row, int col) : base(color, PieceType.Rook, row, col) { }

        public override List<(int Row, int Col)> GetPossibleMoves(ChessPiece?[,] board)
        {
            var moves = new List<(int, int)>();
            int[] directions = { -1, 1 };

            // Vertical moves (up and down)
            foreach (var dir in directions)
            {
                for (int r = Position.Row + dir; IsInsideBoard(r, Position.Col); r += dir)
                {
                    if (board[r, Position.Col] == null)
                    {
                        moves.Add((r, Position.Col));
                    }
                    else
                    {
                        // Can capture enemy piece but cannot move past it
                        if (board[r, Position.Col]!.Color != Color)
                            moves.Add((r, Position.Col));
                        break;
                    }
                }
            }

            // Horizontal moves (left and right)
            foreach (var dir in directions)
            {
                for (int c = Position.Col + dir; IsInsideBoard(Position.Row, c); c += dir)
                {
                    if (board[Position.Row, c] == null)
                    {
                        moves.Add((Position.Row, c));
                    }
                    else
                    {
                        // Can capture enemy piece but cannot move past it
                        if (board[Position.Row, c]!.Color != Color)
                            moves.Add((Position.Row, c));
                        break;
                    }
                }
            }

            return moves;
        }
    }

    /// <summary>
    /// Knight chess piece implementation
    /// Moves in L-shape: 2 squares in one direction, 1 in perpendicular
    /// Can jump over other pieces
    /// </summary>
    public class Knight : ChessPiece
    {
        public Knight(PieceColor color, int row, int col) : base(color, PieceType.Knight, row, col) { }

        public override List<(int Row, int Col)> GetPossibleMoves(ChessPiece?[,] board)
        {
            var moves = new List<(int, int)>();
            
            // All 8 possible L-shaped moves for a knight
            int[,] offsets = { 
                { 2, 1 }, { 2, -1 }, { -2, 1 }, { -2, -1 },
                { 1, 2 }, { 1, -2 }, { -1, 2 }, { -1, -2 } 
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

            return moves;
        }
    }

    /// <summary>
    /// Bishop chess piece implementation
    /// Moves diagonally any distance
    /// Cannot jump over other pieces
    /// </summary>
    public class Bishop : ChessPiece
    {
        public Bishop(PieceColor color, int row, int col) : base(color, PieceType.Bishop, row, col) { }

        public override List<(int Row, int Col)> GetPossibleMoves(ChessPiece?[,] board)
        {
            var moves = new List<(int, int)>();
            int[] directions = { -1, 1 };

            // Diagonal moves
            foreach (var rowDir in directions)
            {
                foreach (var colDir in directions)
                {
                    for (int r = Position.Row + rowDir, c = Position.Col + colDir; 
                         IsInsideBoard(r, c); 
                         r += rowDir, c += colDir)
                    {
                        if (board[r, c] == null)
                        {
                            moves.Add((r, c));
                        }
                        else
                        {
                            // Can capture enemy piece but cannot move past it
                            if (board[r, c]!.Color != Color)
                                moves.Add((r, c));
                            break;
                        }
                    }
                }
            }

            return moves;
        }
    }

    /// <summary>
    /// Queen chess piece implementation
    /// Combines rook and bishop movement
    /// Most powerful piece on the board
    /// </summary>
    public class Queen : ChessPiece
    {
        public Queen(PieceColor color, int row, int col) : base(color, PieceType.Queen, row, col) { }

        public override List<(int Row, int Col)> GetPossibleMoves(ChessPiece?[,] board)
        {
            var moves = new List<(int, int)>();
            
            // Queen moves like both rook and bishop
            // Horizontal and vertical (like rook)
            int[] directions = { -1, 1 };
            
            foreach (var dir in directions)
            {
                // Vertical
                for (int r = Position.Row + dir; IsInsideBoard(r, Position.Col); r += dir)
                {
                    if (board[r, Position.Col] == null)
                    {
                        moves.Add((r, Position.Col));
                    }
                    else
                    {
                        if (board[r, Position.Col]!.Color != Color)
                            moves.Add((r, Position.Col));
                        break;
                    }
                }

                // Horizontal
                for (int c = Position.Col + dir; IsInsideBoard(Position.Row, c); c += dir)
                {
                    if (board[Position.Row, c] == null)
                    {
                        moves.Add((Position.Row, c));
                    }
                    else
                    {
                        if (board[Position.Row, c]!.Color != Color)
                            moves.Add((Position.Row, c));
                        break;
                    }
                }
            }

            // Diagonal (like bishop)
            foreach (var rowDir in directions)
            {
                foreach (var colDir in directions)
                {
                    for (int r = Position.Row + rowDir, c = Position.Col + colDir; 
                         IsInsideBoard(r, c); 
                         r += rowDir, c += colDir)
                    {
                        if (board[r, c] == null)
                        {
                            moves.Add((r, c));
                        }
                        else
                        {
                            if (board[r, c]!.Color != Color)
                                moves.Add((r, c));
                            break;
                        }
                    }
                }
            }

            return moves;
        }
    }
}
