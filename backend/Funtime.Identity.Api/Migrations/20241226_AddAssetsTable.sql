-- Migration: Add Assets table
-- Date: 2024-12-26

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Assets')
BEGIN
    CREATE TABLE Assets (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FileName NVARCHAR(255) NOT NULL,
        ContentType NVARCHAR(100) NOT NULL,
        FileSize BIGINT NOT NULL,
        StorageUrl NVARCHAR(1000) NOT NULL,
        StorageType NVARCHAR(20) NOT NULL DEFAULT 'local',
        Category NVARCHAR(50) NULL,
        UploadedBy INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IsPublic BIT NOT NULL DEFAULT 1
    );

    -- Indexes
    CREATE INDEX IX_Assets_Category ON Assets(Category);
    CREATE INDEX IX_Assets_UploadedBy ON Assets(UploadedBy);
    CREATE INDEX IX_Assets_CreatedAt ON Assets(CreatedAt);
END
GO
