-- ✅ Dynamically targets whatever name is specified in your docker environment variables 
USE [$(TargetDb)]; 
GO

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PendingReadings]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.PendingReadings (
        Id VARCHAR(50) NOT NULL,
        [Timestamp] DATETIME2 NOT NULL,
        [Value] FLOAT NOT NULL,
        SensorType VARCHAR(50) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_PendingReadings PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AnalysisResults]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.AnalysisResults (
        Id VARCHAR(50) NOT NULL,
        SensorReadingId VARCHAR(50) NOT NULL,
        AnalysisType VARCHAR(50) NOT NULL,
        Result FLOAT NOT NULL,
        ProcessedAt DATETIME2 NOT NULL,
        CONSTRAINT PK_AnalysisResults PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PendingReadings_Timestamp' AND object_id = OBJECT_ID(N'[dbo].[PendingReadings]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PendingReadings_Timestamp 
    ON dbo.PendingReadings ([Timestamp] ASC);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AnalysisResults_SensorReadingId' AND object_id = OBJECT_ID(N'[dbo].[AnalysisResults]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AnalysisResults_SensorReadingId 
    ON dbo.AnalysisResults (SensorReadingId ASC);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SavePendingReading
    @Id VARCHAR(50),
    @Timestamp DATETIME2,
    @Value FLOAT,
    @SensorType VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF NOT EXISTS (SELECT 1 FROM dbo.PendingReadings WHERE Id = @Id)
        BEGIN
            INSERT INTO dbo.PendingReadings (Id, [Timestamp], [Value], SensorType, CreatedAt)
            VALUES (@Id, @Timestamp, @Value, @SensorType, SYSUTCDATETIME());
        END
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetOldestPending
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;

        WITH OldestRecord AS (
            SELECT TOP (1) Id, [Timestamp], [Value], SensorType
            FROM dbo.PendingReadings WITH (ROWLOCK, UPDLOCK, READPAST)
            ORDER BY [Timestamp] ASC
        )
        DELETE FROM OldestRecord
        OUTPUT deleted.Id, deleted.[Timestamp], deleted.[Value], deleted.SensorType;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_InsertAnalysisResults
    @Id VARCHAR(50),
    @SensorReadingId VARCHAR(50),
    @AnalysisType VARCHAR(50),
    @Result FLOAT,
    @ProcessedAt DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.AnalysisResults (Id, SensorReadingId, AnalysisType, Result, ProcessedAt)
        VALUES (@Id, @SensorReadingId, @AnalysisType, @Result, @ProcessedAt);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO