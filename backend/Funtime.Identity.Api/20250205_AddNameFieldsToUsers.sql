-- Add FirstName and LastName to Users table
-- These fields are required for new registrations to avoid "New User" defaults

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FirstName')
BEGIN
    ALTER TABLE Users ADD FirstName NVARCHAR(100) NOT NULL DEFAULT '';
    PRINT 'Added FirstName column to Users table';
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'LastName')
BEGIN
    ALTER TABLE Users ADD LastName NVARCHAR(100) NOT NULL DEFAULT '';
    PRINT 'Added LastName column to Users table';
END
