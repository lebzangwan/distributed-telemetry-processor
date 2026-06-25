

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TelemetryDb')
BEGIN
    CREATE DATABASE TelemetryDb;
END
GO

USE TelemetryDb;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PendingReadings]') AND type in (N'U'))
BEGIN
    CREATE TABLE PendingReadings (
      Id VARCHAR(50) PRIMARY KEY,
      [Timestamp] DATETIME2 NOT NULL,
      [Value] FLOAT NOT NULL,
      SensorType VARCHAR(50) NOT NULL,
      CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AnalysisResults]') AND type in (N'U'))
BEGIN
    CREATE TABLE AnalysisResults (
      Id INT IDENTITY(1, 1) PRIMARY KEY,
      SensorReadingId VARCHAR(50) NOT NULL,
      AnalysisType VARCHAR(50) NOT NULL,
      Result FLOAT NOT NULL,
      ProcessedAt DATETIME2 NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PendingReadings_Timestamp' AND object_id = OBJECT_ID('PendingReadings'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PendingReadings_Timestamp ON PendingReadings ([Timestamp] ASC);
END
GO

GO
CREATE OR ALTER PROCEDURE sp_SavePendingReading 
    @Id VARCHAR(50),
    @Timestamp DATETIME2,
    @Value FLOAT,
    @SensorType VARCHAR(50) 
AS 
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY 
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM PendingReadings WHERE Id = @Id) 
        BEGIN
            INSERT INTO PendingReadings (Id, [Timestamp], [Value], SensorType)
            VALUES (@Id, @Timestamp, @Value, @SensorType);
        END 

        COMMIT TRANSACTION;
    END TRY 
    BEGIN CATCH 
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH 
END;
GO

CREATE OR ALTER PROCEDURE sp_GetOldestPending 
AS 
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY 
        BEGIN TRANSACTION; 
        
        DECLARE @TargetId VARCHAR(50);

        SELECT TOP (1) @TargetId = Id
        FROM PendingReadings WITH (UPDLOCK, READPAST)
        ORDER BY [Timestamp] ASC;

        IF @TargetId IS NOT NULL 
        BEGIN
            DELETE FROM PendingReadings 
            OUTPUT deleted.Id, deleted.[Timestamp], deleted.[Value], deleted.SensorType
            WHERE Id = @TargetId;
        END 

        COMMIT TRANSACTION;
    END TRY 
    BEGIN CATCH 
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH 
END;
GO

CREATE OR ALTER PROCEDURE sp_InsertAnalysisResults 
    @SensorReadingId VARCHAR(50),
    @AnalysisType VARCHAR(50),
    @Result FLOAT,
    @ProcessedAt DATETIME2 
AS 
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO AnalysisResults (SensorReadingId, AnalysisType, Result, ProcessedAt)
        VALUES (@SensorReadingId, @AnalysisType, @Result, @ProcessedAt);

        COMMIT TRANSACTION;
    END TRY 
    BEGIN CATCH 
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH 
END;
GO