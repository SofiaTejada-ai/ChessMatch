using System.Collections.Generic;
using System.Text.Json;

namespace WebApplication.ChessLogic
{
    /// <summary>
    /// Represents the chess board and game state
    /// Manages piece positions, turn order, and game rules
    /// </summary>
    public class ChessBoard
    {
        public ChessPiece?[,] Squares { get; private set; }
        public PieceColor CurrentTurn { get; private set; }
        public List<ChessMove> MoveHistory { get; private set; }
        public bool WhiteKingInCheck { get; private set; }
        public bool BlackKingInCheck { get; private set; }
        public string GameStatus { get; private set; } // "Active", "WhiteWins", "BlackWins", "Draw"

        public ChessBoard()
        {
            Squares = new ChessPiece?[8, 8];
            CurrentTurn = PieceColor.White;
            MoveHistory = new List<ChessMove>();
            WhiteKingInCheck = false;
            BlackKingInCheck = false;
            GameStatus = "Active";
            InitializeBoard();
        }

        /// <summary>
        /// Sets up the chess board in starting position
        /// </summary>
        private void InitializeBoard()
        {
            // Place pawns
            for (int col = 0; col < 8; col++)
            {
                Squares[1, col] = new Pawn(PieceColor.White, 1, col);
                Squares[6, col] = new Pawn(PieceColor.Black, 6, col);
            }

            // Place other pieces
            // White pieces (row 0)
            Squares[0, 0] = new Rook(PieceColor.White, 0, 0);
            Squares[0, 1] = new Knight(PieceColor.White, 0, 1);
            Squares[0, 2] = new Bishop(PieceColor.White, 0, 2);
            Squares[0, 3] = new Queen(PieceColor.White, 0, 3);
            Squares[0, 4] = new King(PieceColor.White, 0, 4);
            Squares[0, 5] = new Bishop(PieceColor.White, 0, 5);
            Squares[0, 6] = new Knight(PieceColor.White, 0, 6);
            Squares[0, 7] = new Rook(PieceColor.White, 0, 7);

            // Black pieces (row 7)
            Squares[7, 0] = new Rook(PieceColor.Black, 7, 0);
            Squares[7, 1] = new Knight(PieceColor.Black, 7, 1);
            Squares[7, 2] = new Bishop(PieceColor.Black, 7, 2);
            Squares[7, 3] = new Queen(PieceColor.Black, 7, 3);
            Squares[7, 4] = new King(PieceColor.Black, 7, 4);
            Squares[7, 5] = new Bishop(PieceColor.Black, 7, 5);
            Squares[7, 6] = new Knight(PieceColor.Black, 7, 6);
            Squares[7, 7] = new Rook(PieceColor.Black, 7, 7);
        }

        /// <summary>
        /// Gets a piece at the specified position
        /// </summary>
        public ChessPiece? GetPiece(int row, int col)
        {
            return IsInsideBoard(row, col) ? Squares[row, col] : null;
        }

        /// <summary>
        /// Sets a piece at the specified position
        /// </summary>
        public void SetPiece(int row, int col, ChessPiece? piece)
        {
            if (IsInsideBoard(row, col))
            {
                Squares[row, col] = piece;
                if (piece != null)
                {
                    piece.MoveTo(row, col);
                }
            }
        }

        /// <summary>
        /// Checks if a position is within the board boundaries
        /// </summary>
        public bool IsInsideBoard(int row, int col)
        {
            return row >= 0 && row < 8 && col >= 0 && col < 8;
        }

        /// <summary>
        /// Gets all valid moves for the current player
        /// </summary>
        public List<(int Row, int Col)> GetValidMoves(int fromRow, int fromCol)
        {
            var piece = GetPiece(fromRow, fromCol);
            if (piece == null || piece.Color != CurrentTurn)
                return new List<(int, int)>();

            var possibleMoves = piece.GetPossibleMoves(Squares);
            var validMoves = new List<(int, int)>();

            foreach (var move in possibleMoves)
            {
                if (IsValidMove(fromRow, fromCol, move.Row, move.Col))
                {
                    validMoves.Add(move);
                }
            }

            return validMoves;
        }

        /// <summary>
        /// Validates if a move is legal according to chess rules
        /// </summary>
        public bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            var piece = GetPiece(fromRow, fromCol);
            if (piece == null || piece.Color != CurrentTurn)
                return false;

            // Check if move is in piece's possible moves
            var possibleMoves = piece.GetPossibleMoves(Squares);
            if (!possibleMoves.Contains((toRow, toCol)))
                return false;

            // Simulate the move to check if it would leave king in check
            var capturedPiece = GetPiece(toRow, toCol);
            SetPiece(toRow, toCol, piece);
            SetPiece(fromRow, fromCol, null);

            bool wouldBeInCheck = IsKingInCheck(CurrentTurn);

            // Undo the move
            SetPiece(fromRow, fromCol, piece);
            SetPiece(toRow, toCol, capturedPiece);

            return !wouldBeInCheck;
        }

        /// <summary>
        /// Makes a move on the board
        /// </summary>
        public bool MakeMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (!IsValidMove(fromRow, fromCol, toRow, toCol))
                return false;

            var piece = GetPiece(fromRow, fromCol)!;
            var capturedPiece = GetPiece(toRow, toCol);

            // Execute the move
            SetPiece(toRow, toCol, piece);
            SetPiece(fromRow, fromCol, null);

            // Record the move
            var move = new ChessMove
            {
                From = (fromRow, fromCol),
                To = (toRow, toCol),
                Piece = piece,
                CapturedPiece = capturedPiece
            };
            MoveHistory.Add(move);

            // Switch turns
            CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;

            // Update check status
            UpdateCheckStatus();

            // Check for game-ending conditions
            UpdateGameStatus();

            return true;
        }

        /// <summary>
        /// Checks if the specified king is in check
        /// </summary>
        public bool IsKingInCheck(PieceColor kingColor)
        {
            // Find the king
            (int kingRow, int kingCol) kingPos = (-1, -1);
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = GetPiece(row, col);
                    if (piece is King && piece.Color == kingColor)
                    {
                        kingPos = (row, col);
                        break;
                    }
                }
                if (kingPos.kingRow != -1) break;
            }

            if (kingPos.kingRow == -1) return false; // King not found

            // Check if any enemy piece can attack the king
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = GetPiece(row, col);
                    if (piece != null && piece.Color != kingColor)
                    {
                        var moves = piece.GetPossibleMoves(Squares);
                        if (moves.Contains(kingPos))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the check status for both kings
        /// </summary>
        private void UpdateCheckStatus()
        {
            WhiteKingInCheck = IsKingInCheck(PieceColor.White);
            BlackKingInCheck = IsKingInCheck(PieceColor.Black);
        }

        /// <summary>
        /// Updates the game status (checkmate, stalemate, etc.)
        /// </summary>
        private void UpdateGameStatus()
        {
            if (GameStatus != "Active") return;

            var currentColor = CurrentTurn;
            bool hasValidMoves = false;

            // Check if current player has any valid moves
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = GetPiece(row, col);
                    if (piece != null && piece.Color == currentColor)
                    {
                        var moves = GetValidMoves(row, col);
                        if (moves.Count > 0)
                        {
                            hasValidMoves = true;
                            break;
                        }
                    }
                }
                if (hasValidMoves) break;
            }

            // Determine game status
            bool isInCheck = IsKingInCheck(currentColor);
            if (!hasValidMoves)
            {
                if (isInCheck)
                {
                    // Checkmate
                    GameStatus = currentColor == PieceColor.White ? "BlackWins" : "WhiteWins";
                }
                else
                {
                    // Stalemate
                    GameStatus = "Draw";
                }
            }
        }

        /// <summary>
        /// Converts board to JSON format for storage/transmission
        /// </summary>
        public string ToJson()
        {
            var boardState = new
            {
                squares = Squares,
                currentTurn = CurrentTurn,
                moveHistory = MoveHistory,
                whiteKingInCheck = WhiteKingInCheck,
                blackKingInCheck = BlackKingInCheck,
                gameStatus = GameStatus
            };
            return JsonSerializer.Serialize(boardState);
        }
    }

    /// <summary>
    /// Represents a single chess move
    /// </summary>
    public class ChessMove
    {
        public (int Row, int Col) From { get; set; }
        public (int Row, int Col) To { get; set; }
        public ChessPiece Piece { get; set; } = null!;
        public ChessPiece? CapturedPiece { get; set; }
        public string MoveNotation { get; set; } = "";
    }
}
