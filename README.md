# ChessMatch

A full stack web application for playing chess online against an AI opponent or other human players, with real time chat, persistent match history, and user statistics.

**Live demo:** https://chessmate-connect-production.up.railway.app

## Overview

ChessMatch is a production deployed chess platform built from the ground up. It includes a complete chess engine written in C# (move validation, check and checkmate detection, castling, en passant, promotion), a secure REST API with JWT authentication, a PostgreSQL database hosted on Supabase, and a React frontend. The AI opponent is powered by Google Gemini, which generates contextual moves and in game commentary based on the current board position.

## Features

- **Complete chess engine** implemented in C# with full rules enforcement, including castling, en passant, pawn promotion, check, checkmate, and stalemate detection.
- **AI opponent (ChessHub AI)** backed by Google Gemini, with three difficulty levels (easy, medium, hard) and a legal move fallback when the model returns an invalid response.
- **In game chat** with context aware AI responses that reference the live board position and conversation history.
- **JWT based authentication** with user registration, login, and guest demo sessions.
- **Match history and statistics**, including wins, losses, draws, win streaks, and a global leaderboard.
- **Forfeit, move log, and full match replay** via stored move notation and FEN snapshots.
- **Production deployment** on Railway with automatic builds from the main branch.

## Tech Stack

**Backend**
- ASP.NET Core 10 (C#)
- Npgsql for PostgreSQL access
- JWT Bearer authentication
- Custom chess engine (no third party chess library)

**Frontend**
- React (separate repository)
- Served on Railway

**Database**
- PostgreSQL on Supabase (session pooler)

**Infrastructure**
- Railway for backend and frontend hosting
- Docker for containerization
- GitHub for source control and CI

**External services**
- Google Gemini API for AI moves and chat responses

## Architecture

```
React frontend  ───►  ASP.NET Core REST API  ───►  PostgreSQL (Supabase)
                              │
                              └──►  Google Gemini API (AI opponent and chat)
```

All secrets (JWT signing key, database URL, Gemini API key) are loaded from environment variables. CORS is configured globally to allow the frontend origin, and a custom middleware ensures CORS headers are preserved even on error responses.

## Key API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/auth/register` | Create a new user account |
| POST | `/auth/login` | Authenticate and receive a JWT |
| POST | `/demo/session` | Start a guest session without registering |
| POST | `/chess/match/ai` | Create a match against ChessHub AI |
| GET | `/chess/match/{id}` | Get current board state, turn, and move history |
| POST | `/chess/match/{id}/move` | Submit a move (triggers AI response in AI matches) |
| POST | `/chess/match/{id}/forfeit` | Resign the match |
| GET | `/chess/match/{id}/chat` | Retrieve chat messages |
| POST | `/chess/match/{id}/chat` | Send a chat message (AI replies in AI matches) |
| GET | `/chess/stats/{userId}` | Retrieve a user's match statistics |
| GET | `/chess/leaderboard` | Global leaderboard by rating |

See `API_DOCUMENTATION.md` for the complete API reference.

## Project Structure

```
ChessMatch/
├── WebApplication/
│   ├── Chess Logic/            Custom chess engine (board, pieces, move validation)
│   ├── Controllers/            Additional REST controllers
│   ├── Data Management/        Database helper, models, request DTOs
│   ├── Program.cs              Controllers and endpoints
│   ├── Startup.cs              Middleware, CORS, JWT, DB configuration
│   └── appsettings.json        Non sensitive configuration
├── ChessQuery.sql              Database schema
├── Dockerfile                  Container build
├── railway.json                Railway deployment configuration
└── API_DOCUMENTATION.md        Full API reference
```

## Security

- JWT signing key loaded from the `JWT_SECRET` environment variable.
- Database connection string loaded from the `DATABASE_URL` environment variable.
- Gemini API key loaded from the `GEMINI_API_KEY` environment variable.
- Passwords are never stored in plain text.
- CORS is configured to allow only the production frontend origin.
- Git history has been scrubbed of any previously committed development secrets.

## Running Locally

Requirements: .NET 10 SDK, PostgreSQL, Google Gemini API key.

1. Clone the repository.
2. Create `WebApplication/appsettings.Development.json` with your local database connection string and JWT key.
3. Set environment variables:
   ```
   JWT_SECRET=your_secret_here
   DATABASE_URL=postgresql://user:pass@host:port/db
   GEMINI_API_KEY=your_gemini_key_here
   ```
4. Run the backend:
   ```
   cd WebApplication
   dotnet run
   ```
5. The API will be available at `http://localhost:5000`.

## Deployment

The backend is deployed to Railway using the provided Dockerfile. Pushes to the `main` branch trigger automatic rebuilds. Environment variables are configured in the Railway dashboard.

## Author

**Sofia Tejada**
GitHub: [@SofiaTejada-ai](https://github.com/SofiaTejada-ai)

## License

See `LICENSE` for details.
