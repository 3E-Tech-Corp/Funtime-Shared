-- ============================================================================
-- Unified Notification Pipeline Tables
-- ============================================================================

-- NotificationTypes: Defines types of notifications (welcome, reminder, etc.)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationTypes')
BEGIN
    CREATE TABLE NotificationTypes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500),
        Category NVARCHAR(50),              -- 'transactional', 'marketing', 'system'
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    CREATE UNIQUE INDEX UQ_NotificationType_Code ON NotificationTypes(Code);
    
    -- Seed common notification types
    INSERT INTO NotificationTypes (Code, Name, Category) VALUES
        ('welcome', 'Welcome Email', 'transactional'),
        ('password_reset', 'Password Reset', 'transactional'),
        ('email_verification', 'Email Verification', 'transactional'),
        ('tournament_reminder', 'Tournament Reminder', 'transactional'),
        ('registration_confirmation', 'Registration Confirmation', 'transactional'),
        ('payment_receipt', 'Payment Receipt', 'transactional'),
        ('schedule_update', 'Schedule Update', 'transactional'),
        ('match_result', 'Match Result', 'transactional'),
        ('announcement', 'General Announcement', 'marketing'),
        ('newsletter', 'Newsletter', 'marketing');
END
GO

-- NotificationTemplates: Per-site, per-type, per-channel templates
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationTemplates')
BEGIN
    CREATE TABLE NotificationTemplates (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SiteKey NVARCHAR(50) NOT NULL,
        TypeCode NVARCHAR(50) NOT NULL,
        ChannelCode NVARCHAR(20) NOT NULL,  -- 'email', 'sms', 'webpush', 'signal'
        LangCode NVARCHAR(10) NOT NULL DEFAULT 'en',
        
        -- Template content (Handlebars/Scriban syntax with {{variable}})
        Subject NVARCHAR(500),              -- For email
        BodyHtml NVARCHAR(MAX),             -- For email
        BodyText NVARCHAR(MAX),             -- For SMS/Signal/WebPush
        
        -- Metadata
        IsActive BIT NOT NULL DEFAULT 1,
        Version INT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2,
        CreatedBy INT
    );
    
    CREATE UNIQUE INDEX UQ_Template ON NotificationTemplates(SiteKey, TypeCode, ChannelCode, LangCode);
    CREATE INDEX IX_Template_Site ON NotificationTemplates(SiteKey);
END
GO

-- NotificationQueue: Central queue for all notifications
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationQueue')
BEGIN
    CREATE TABLE NotificationQueue (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        
        -- Source
        SiteKey NVARCHAR(50) NOT NULL,
        TypeCode NVARCHAR(50) NOT NULL,
        
        -- Recipient
        UserId INT,                         -- Shared auth user ID
        RecipientEmail NVARCHAR(255),
        RecipientPhone NVARCHAR(50),
        RecipientName NVARCHAR(200),
        
        -- Content (rendered from template)
        ChannelCode NVARCHAR(20) NOT NULL,
        Subject NVARCHAR(500),
        BodyHtml NVARCHAR(MAX),
        BodyText NVARCHAR(MAX),
        
        -- Original data (for re-rendering if needed)
        TemplateData NVARCHAR(MAX),         -- JSON
        
        -- Status & Moderation
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        -- Pending, Held, Approved, Sending, Sent, Failed, Cancelled
        ModerationStatus NVARCHAR(20),      -- NULL, 'Held', 'Approved', 'Rejected'
        ModerationNote NVARCHAR(500),
        ModeratedBy INT,
        ModeratedAt DATETIME2,
        
        -- Dispatch tracking
        ExternalId NVARCHAR(100),           -- FXNotification ID, etc.
        Attempts INT NOT NULL DEFAULT 0,
        LastAttemptAt DATETIME2,
        SentAt DATETIME2,
        ErrorMessage NVARCHAR(MAX),
        
        -- Metadata
        Priority INT NOT NULL DEFAULT 5,    -- 1=highest, 10=lowest
        ScheduledFor DATETIME2,             -- For delayed sending
        ExpiresAt DATETIME2,                -- Don't send after this time
        ObjectId BIGINT,                    -- Related object (eventId, etc.)
        ObjectType NVARCHAR(50),            -- 'Event', 'Registration', etc.
        
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2
    );
    
    CREATE INDEX IX_Queue_Status ON NotificationQueue(Status, Priority, CreatedAt);
    CREATE INDEX IX_Queue_Site ON NotificationQueue(SiteKey, TypeCode, CreatedAt);
    CREATE INDEX IX_Queue_User ON NotificationQueue(UserId, CreatedAt);
    CREATE INDEX IX_Queue_Moderation ON NotificationQueue(ModerationStatus, CreatedAt) WHERE ModerationStatus IS NOT NULL;
END
GO

-- ModerationRules: Defines what requires moderation vs auto-send
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ModerationRules')
BEGIN
    CREATE TABLE ModerationRules (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SiteKey NVARCHAR(50),               -- NULL = all sites
        TypeCode NVARCHAR(50),              -- NULL = all types
        ChannelCode NVARCHAR(20),           -- NULL = all channels
        
        Action NVARCHAR(20) NOT NULL,       -- 'AutoSend', 'Hold', 'Block'
        
        -- Conditions (JSON for complex rules)
        Conditions NVARCHAR(MAX),           -- {"recipientCount": ">100", "category": "marketing"}
        
        Priority INT NOT NULL DEFAULT 100,  -- Lower = higher priority (first match wins)
        IsActive BIT NOT NULL DEFAULT 1,
        
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2
    );
    
    -- Default rules
    INSERT INTO ModerationRules (SiteKey, TypeCode, ChannelCode, Action, Priority) VALUES
        (NULL, NULL, NULL, 'AutoSend', 1000),           -- Default: auto-send everything
        (NULL, NULL, 'sms', 'Hold', 50),                -- Hold all SMS for review
        (NULL, 'newsletter', NULL, 'Hold', 10),         -- Hold newsletters
        (NULL, 'announcement', NULL, 'Hold', 10);       -- Hold announcements
END
GO

-- ChannelConfigs: Per-site channel configuration
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChannelConfigs')
BEGIN
    CREATE TABLE ChannelConfigs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SiteKey NVARCHAR(50) NOT NULL,
        ChannelCode NVARCHAR(20) NOT NULL,
        
        IsEnabled BIT NOT NULL DEFAULT 1,
        
        -- Channel-specific config (JSON)
        Config NVARCHAR(MAX),
        -- Email: { "fxTaskCode": "PB_COMMUNITY_EMAIL", "fromName": "Pickleball Community" }
        -- SMS: { "fxTaskCode": "PB_COMMUNITY_SMS" }
        -- WebPush: { "vapidPublicKey": "...", "vapidPrivateKey": "..." }
        
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2
    );
    
    CREATE UNIQUE INDEX UQ_ChannelConfig ON ChannelConfigs(SiteKey, ChannelCode);
    
    -- Seed community channel config (placeholder - will need real FXNotification task codes)
    INSERT INTO ChannelConfigs (SiteKey, ChannelCode, Config) VALUES
        ('community', 'email', '{"fxTaskCode": "PB_COMMUNITY_EMAIL", "fromName": "Pickleball Community"}'),
        ('community', 'sms', '{"fxTaskCode": "PB_COMMUNITY_SMS"}');
END
GO

-- NotificationLog: Audit trail
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationLog')
BEGIN
    CREATE TABLE NotificationLog (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        NotificationId BIGINT NOT NULL,
        Action NVARCHAR(50) NOT NULL,       -- 'Created', 'Sent', 'Failed', 'Approved', 'Rejected', 'Retried'
        Details NVARCHAR(MAX),              -- JSON with details
        PerformedBy INT,                    -- UserId (NULL for system)
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        INDEX IX_Log_Notification (NotificationId, CreatedAt)
    );
END
GO

PRINT 'Notification tables created successfully';
GO
