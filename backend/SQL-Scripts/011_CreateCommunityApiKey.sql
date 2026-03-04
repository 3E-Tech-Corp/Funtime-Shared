-- Create API Key for pickleball.community notification pipeline access
-- This key allows the community site to send notifications via the centralized pipeline

SET QUOTED_IDENTIFIER ON;
GO

USE FuntimeIdentity;
GO

-- Generate a secure API key
DECLARE @ApiKeyValue NVARCHAR(64) = 'sk_community_' + REPLACE(CONVERT(NVARCHAR(36), NEWID()), '-', '');
DECLARE @KeyPrefix NVARCHAR(10) = 'sk_comm_';

-- Check if API key for community site already exists
IF NOT EXISTS (SELECT 1 FROM ApiKeys WHERE PartnerKey = 'community')
BEGIN
    INSERT INTO ApiKeys (
        PartnerKey,
        PartnerName,
        ApiKey,
        KeyPrefix,
        Scopes,
        RateLimitPerMinute,
        IsActive,
        Description,
        CreatedBy,
        CreatedAt
    )
    VALUES (
        'community',
        'Pickleball Community',
        @ApiKeyValue,
        @KeyPrefix,
        '["notifications:send", "users:read"]',
        120,
        1,
        'API key for pickleball.community to send templated notifications via the centralized pipeline',
        'system',
        GETUTCDATE()
    );

    PRINT '';
    PRINT '========================================';
    PRINT 'Created API key for community site:';
    PRINT @ApiKeyValue;
    PRINT '';
    PRINT 'Add this to pickleball.community config:';
    PRINT '';
    PRINT '  "NotificationPipeline": {';
    PRINT '    "BaseUrl": "https://shared.funtimepb.com",';
    PRINT '    "ApiKey": "' + @ApiKeyValue + '"';
    PRINT '  }';
    PRINT '';
    PRINT '========================================';
END
ELSE
BEGIN
    PRINT 'API key for community site already exists:';
    SELECT PartnerKey, PartnerName, KeyPrefix + '...' AS ApiKey, Scopes 
    FROM ApiKeys WHERE PartnerKey = 'community';
END
GO
