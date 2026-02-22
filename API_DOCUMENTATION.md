# ChessHub API Documentation

## 🎯 **Project Overview**
ChessHub is a C# backend API for a chess game application using raw SQL queries, JWT authentication, and ASP.NET Core.

## 📚 **Key Programming Concepts Explained**

### **Functions (Methods)**
- **Purpose:** Reusable blocks of code that perform specific tasks
- **Examples:** `CreateUserAsync()`, `Login()`, `GetUserMatches()`
- **Benefits:** Code organization, reusability, easier testing

### **Classes**
- **Purpose:** Blueprints for creating objects
- **Examples:** `User`, `Match`, `UserStats`, `RegisterRequest`
- **Benefits:** Organize related data and behavior together

### **Type Variables**
- **Purpose:** Variables that hold specific types of data
- **Examples:** `string username`, `int userId`, `DateTime createdAt`
- **Benefits:** Type safety, compiler error checking

---

## 🔐 **Authentication Endpoints**

### **POST /register**
**Purpose:** Create a new user account
**Request Body:**
```json
{
  "username": "john_doe",
  "email": "john@email.com",
  "password": "password123"
}
```
**Response:**
```json
{
  "message": "User registered successfully",
  "userId": 1
}
```

### **POST /login**
**Purpose:** Authenticate user and get JWT token
**Request Body:**
```json
{
  "username": "john_doe",
  "password": "password123"
}
```
**Response:**
```json
{
  "userId": 1,
  "username": "john_doe",
  "email": "john@email.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

---

## 👤 **User Profile Endpoints** *(Requires JWT Token)*

### **GET /user/profile**
**Purpose:** Get current user's profile information
**Headers:** `Authorization: Bearer <jwt_token>`
**Response:**
```json
{
  "userId": 1,
  "username": "john_doe",
  "email": "john@email.com",
  "createdAt": "2024-01-15T10:30:00Z",
  "lastSeenAt": "2024-01-15T15:45:00Z",
  "isActive": true
}
```

### **PUT /user/profile**
**Purpose:** Update current user's profile
**Headers:** `Authorization: Bearer <jwt_token>`
**Request Body:**
```json
{
  "username": "john_doe_updated",
  "email": "john_new@email.com"
}
```

---

## ♟️ **Chess Game Endpoints** *(Requires JWT Token)*

### **POST /chess/match/create**
**Purpose:** Create a new chess match
**Headers:** `Authorization: Bearer <jwt_token>`
**Request Body:**
```json
{
  "matchType": "Random", // "Random", "Friend", or "Direct"
  "opponentId": null,    // For Direct matches
  "inviteCode": null     // For Friend matches
}
```
**Response:**
```json
{
  "message": "Match created successfully",
  "matchId": 123
}
```

### **GET /chess/match/{matchId}**
**Purpose:** Get specific match details
**Headers:** `Authorization: Bearer <jwt_token>`
**Response:**
```json
{
  "matchId": 123,
  "createdAt": "2024-01-15T10:30:00Z",
  "endedAt": null,
  "whiteUserId": 1,
  "blackUserId": 2,
  "winnerId": null,
  "matchState": "Active",
  "result": null,
  "endReason": null,
  "matchType": "Random",
  "inviteCode": null
}
```

### **GET /chess/matches**
**Purpose:** Get all matches for current user
**Headers:** `Authorization: Bearer <jwt_token>`
**Response:**
```json
[
  {
    "matchId": 123,
    "createdAt": "2024-01-15T10:30:00Z",
    "whiteUserId": 1,
    "blackUserId": 2,
    "matchState": "Active",
    "matchType": "Random"
  }
]
```

### **POST /chess/match/{matchId}/move**
**Purpose:** Make a chess move
**Headers:** `Authorization: Bearer <jwt_token>`
**Request Body:**
```json
{
  "move": "e2e4",
  "moveNotation": "e4"
}
```
**Response:**
```json
{
  "message": "Move accepted",
  "move": "e2e4"
}
```

---

## 📊 **Statistics Endpoints** *(Requires JWT Token)*

### **GET /stats/my-stats**
**Purpose:** Get current user's statistics
**Headers:** `Authorization: Bearer <jwt_token>`
**Response:**
```json
{
  "userId": 1,
  "wins": 5,
  "losses": 3,
  "draws": 2,
  "currentWinStreak": 2,
  "bestWinStreak": 4,
  "rating": 1250,
  "lastGameEndedAt": "2024-01-15T15:45:00Z"
}
```

### **GET /stats/leaderboard**
**Purpose:** Get top players by rating
**Headers:** `Authorization: Bearer <jwt_token>`
**Response:**
```json
[
  {
    "userId": 1,
    "wins": 10,
    "losses": 2,
    "draws": 1,
    "rating": 1450
  }
]
```

### **POST /stats/update-stats**
**Purpose:** Update statistics after a game
**Headers:** `Authorization: Bearer <jwt_token>`
**Request Body:**
```json
{
  "result": "Win" // "Win", "Loss", or "Draw"
}
```
**Response:**
```json
{
  "message": "Statistics updated successfully",
  "newRating": 1275
}
```

---

## 🚀 **How to Test the Backend**

### **1. Start the Application**
```bash
dotnet run
```
The API will be available at `http://localhost:5000`

### **2. Test Registration**
```bash
curl -X POST http://localhost:5000/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","email":"test@email.com","password":"password123"}'
```

### **3. Test Login**
```bash
curl -X POST http://localhost:5000/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"password123"}'
```

### **4. Use JWT Token for Protected Endpoints**
```bash
# Replace <token> with the token from login response
curl -X GET http://localhost:5000/user/profile \
  -H "Authorization: Bearer <token>"
```

---

## 🏗️ **Architecture Overview**

```
Frontend (Love2D) ←→ HTTP API ←→ C# Controllers ←→ DatabaseHelper ←→ SQL Database
                      (JWT Auth)      (Business Logic)   (Raw SQL)
```

### **Key Components:**
- **Controllers:** Handle HTTP requests and responses
- **Models:** Data blueprints (User, Match, UserStats)
- **DatabaseHelper:** Raw SQL operations
- **JWT Authentication:** Secure token-based auth
- **SQL Database:** ChessHub database with schemas

---

## 🎯 **Next Steps for Frontend**

1. **Authentication Flow:**
   - Register/Login forms
   - Store JWT token
   - Include token in API calls

2. **Chess Game Interface:**
   - Create/join matches
   - Display chess board
   - Handle move validation

3. **User Dashboard:**
   - Profile management
   - Match history
   - Statistics display

4. **Real-time Features:**
   - WebSocket for live games
   - Move notifications
   - Match status updates

This backend provides a solid foundation for your chess game and demonstrates key programming concepts perfect for your Ria internship preparation!
