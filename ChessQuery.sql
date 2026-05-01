USE ChessHub;
GO

CREATE SCHEMA [UsersSchema] AUTHORIZATION dbo;
GO

CREATE SCHEMA [MatchesSchema] AUTHORIZATION dbo;
GO

CREATE SCHEMA [StatsSchema] AUTHORIZATION dbo;
GO

CREATE TABLE [UsersSchema].[UsersTable] (
    UserID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    LastSeenAt DATETIME NULL,
    IsActive BIT DEFAULT 1
);
GO

CREATE TABLE [MatchesSchema].[MatchesTable] (
    MatchID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    EndedAt DATETIME NULL,
    WhiteUserID INT NOT NULL,
    BlackUserID INT NOT NULL,
    WinnerID INT NULL,
    MatchState NVARCHAR(20) NOT NULL DEFAULT 'Active',
    Result NVARCHAR(20) NULL DEFAULT NULL,
    EndReason NVARCHAR(50) NULL DEFAULT NULL,
    MatchType NVARCHAR(20) NOT NULL DEFAULT 'Random',
    InviteCode NVARCHAR(10) NULL,-- used for friend matches
    CONSTRAINT ForeginKey_Matches_WhiteUser FOREIGN KEY (WhiteUserID) REFERENCES [UsersSchema].[UsersTable](UserID),
    CONSTRAINT ForeginKey_Matches_BlackUser FOREIGN KEY (BlackUserID) REFERENCES [UsersSchema].[UsersTable](UserID),
    CONSTRAINT ForeginKey_Matches_Winner FOREIGN KEY (WinnerID) REFERENCES [UsersSchema].[UsersTable](UserID),
    CONSTRAINT CHECK_MatchState CHECK (MatchState IN ('Active','Finished','Abandoned')),
    CONSTRAINT CHECK_Result CHECK (Result IS NULL OR Result IN ('WhiteWin','BlackWin','Draw','unknown')),
    CONSTRAINT CHECK_EndReason CHECK (EndReason IS NULL OR EndReason IN ('Checkmate','Resignation','Timeout','Stalemate','DrawByAgreement','unknown')),
    CONSTRAINT CHECK_MatchType CHECK (MatchType IN ('Random','Friend','Direct'))
);
GO

CREATE TABLE [StatsSchema].[UserStatsTable] (
    UserID INT NOT NULL PRIMARY KEY,
    Wins INT NOT NULL DEFAULT 0,
    Losses INT NOT NULL DEFAULT 0,
    Draws INT NOT NULL DEFAULT 0,
    CurrentWinStreak INT NOT NULL DEFAULT 0,
    BestWinStreak INT NOT NULL DEFAULT 0,
    Rating INT NOT NULL DEFAULT 1200,
    LastGameEndedAt DATETIME NULL,
    CONSTRAINT ForeignKey_UserStatsTable_UserID
        FOREIGN KEY (UserID) REFERENCES [UsersSchema].[UsersTable](UserID)
);
