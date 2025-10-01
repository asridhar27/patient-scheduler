-- SQL Stored Procedures for Bulk Operations
-- These procedures handle efficient bulk updates with transaction rollback capabilities

-- =============================================
-- Procedure: BulkRescheduleAppointments
-- Description: Efficiently reschedules multiple appointments in a single transaction
-- =============================================
CREATE PROCEDURE BulkRescheduleAppointments
    @AppointmentIds NVARCHAR(MAX),
    @NewDateTime DATETIME2,
    @Reason NVARCHAR(500) = 'Bulk reschedule'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Create a temporary table to hold the appointment IDs
        CREATE TABLE #TempAppointmentIds (Id INT);
        
        -- Parse the comma-separated appointment IDs
        DECLARE @SQL NVARCHAR(MAX) = 'INSERT INTO #TempAppointmentIds (Id) VALUES (' + 
            REPLACE(@AppointmentIds, ',', '),(') + ')';
        EXEC sp_executesql @SQL;
        
        -- Validate that all appointments exist and are in a reschedulable state
        IF EXISTS (
            SELECT 1 FROM #TempAppointmentIds t
            LEFT JOIN Appointments a ON t.Id = a.Id
            WHERE a.Id IS NULL OR a.Status IN ('Completed', 'Cancelled')
        )
        BEGIN
            RAISERROR('One or more appointments cannot be rescheduled (not found or in invalid state)', 16, 1);
        END
        
        -- Update appointments with new datetime
        UPDATE a
        SET 
            AppointmentDateTime = @NewDateTime,
            UpdatedAt = GETUTCDATE(),
            Notes = CASE 
                WHEN a.Notes IS NULL OR a.Notes = '' THEN @Reason
                ELSE a.Notes + ' | ' + @Reason
            END
        FROM Appointments a
        INNER JOIN #TempAppointmentIds t ON a.Id = t.Id;
        
        -- Update related invoices if they exist
        UPDATE i
        SET 
            UpdatedAt = GETUTCDATE(),
            Notes = CASE 
                WHEN i.Notes IS NULL OR i.Notes = '' THEN 'Rescheduled: ' + @Reason
                ELSE i.Notes + ' | Rescheduled: ' + @Reason
            END
        FROM Invoices i
        INNER JOIN Appointments a ON i.AppointmentId = a.Id
        INNER JOIN #TempAppointmentIds t ON a.Id = t.Id;
        
        -- Log the bulk operation
        INSERT INTO JobLogs (OperationType, AppointmentIds, Parameters, Status, CreatedAt)
        VALUES ('BulkReschedule', @AppointmentIds, 
                JSON_OBJECT('NewDateTime', @NewDateTime, 'Reason', @Reason), 
                'Completed', GETUTCDATE());
        
        COMMIT TRANSACTION;
        
        SELECT 
            COUNT(*) as ProcessedCount,
            'Success' as Status,
            'Appointments rescheduled successfully' as Message;
            
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SELECT 
            ERROR_NUMBER() as ErrorNumber,
            ERROR_MESSAGE() as ErrorMessage,
            'Failed' as Status;
            
        -- Log the error
        INSERT INTO JobLogs (OperationType, AppointmentIds, Parameters, Status, ErrorMessage, CreatedAt)
        VALUES ('BulkReschedule', @AppointmentIds, 
                JSON_OBJECT('NewDateTime', @NewDateTime, 'Reason', @Reason), 
                'Failed', ERROR_MESSAGE(), GETUTCDATE());
    END CATCH
    
    DROP TABLE #TempAppointmentIds;
END
GO

-- =============================================
-- Procedure: BulkUpdateBilling
-- Description: Updates billing information for multiple appointments
-- =============================================
CREATE PROCEDURE BulkUpdateBilling
    @AppointmentIds NVARCHAR(MAX),
    @Adjustment DECIMAL(10,2) = 0,
    @Notes NVARCHAR(500) = 'Bulk billing update'
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Create a temporary table to hold the appointment IDs
        CREATE TABLE #TempAppointmentIds (Id INT);
        
        -- Parse the comma-separated appointment IDs
        DECLARE @SQL NVARCHAR(MAX) = 'INSERT INTO #TempAppointmentIds (Id) VALUES (' + 
            REPLACE(@AppointmentIds, ',', '),(') + ')';
        EXEC sp_executesql @SQL;
        
        -- Update invoices with billing adjustment
        UPDATE i
        SET 
            Amount = CASE 
                WHEN @Adjustment > 0 THEN i.Amount + @Adjustment
                WHEN @Adjustment < 0 THEN GREATEST(i.Amount + @Adjustment, 0)
                ELSE i.Amount
            END,
            UpdatedAt = GETUTCDATE(),
            Notes = CASE 
                WHEN i.Notes IS NULL OR i.Notes = '' THEN @Notes
                ELSE i.Notes + ' | ' + @Notes
            END
        FROM Invoices i
        INNER JOIN Appointments a ON i.AppointmentId = a.Id
        INNER JOIN #TempAppointmentIds t ON a.Id = t.Id
        WHERE i.Status != 'Paid'; -- Don't modify paid invoices
        
        -- Create new invoices for appointments without invoices if adjustment is positive
        IF @Adjustment > 0
        BEGIN
            INSERT INTO Invoices (PatientId, AppointmentId, Amount, Status, DueDate, CreatedAt, Notes)
            SELECT 
                a.PatientId,
                a.Id,
                @Adjustment,
                'Pending',
                DATEADD(day, 30, GETUTCDATE()),
                GETUTCDATE(),
                @Notes
            FROM Appointments a
            INNER JOIN #TempAppointmentIds t ON a.Id = t.Id
            WHERE NOT EXISTS (
                SELECT 1 FROM Invoices i WHERE i.AppointmentId = a.Id
            );
        END
        
        -- Log the bulk operation
        INSERT INTO JobLogs (OperationType, AppointmentIds, Parameters, Status, CreatedAt)
        VALUES ('BulkBillingUpdate', @AppointmentIds, 
                JSON_OBJECT('Adjustment', @Adjustment, 'Notes', @Notes), 
                'Completed', GETUTCDATE());
        
        COMMIT TRANSACTION;
        
        SELECT 
            COUNT(*) as ProcessedCount,
            'Success' as Status,
            'Billing updated successfully' as Message;
            
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SELECT 
            ERROR_NUMBER() as ErrorNumber,
            ERROR_MESSAGE() as ErrorMessage,
            'Failed' as Status;
            
        -- Log the error
        INSERT INTO JobLogs (OperationType, AppointmentIds, Parameters, Status, ErrorMessage, CreatedAt)
        VALUES ('BulkBillingUpdate', @AppointmentIds, 
                JSON_OBJECT('Adjustment', @Adjustment, 'Notes', @Notes), 
                'Failed', ERROR_MESSAGE(), GETUTCDATE());
    END CATCH
    
    DROP TABLE #TempAppointmentIds;
END
GO

-- =============================================
-- Procedure: BulkCancelAppointments
-- Description: Cancels multiple appointments and processes refunds
-- =============================================
CREATE PROCEDURE BulkCancelAppointments
    @AppointmentIds NVARCHAR(MAX),
    @Reason NVARCHAR(500) = 'Bulk cancellation'
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Create a temporary table to hold the appointment IDs
        CREATE TABLE #TempAppointmentIds (Id INT);
        
        -- Parse the comma-separated appointment IDs
        DECLARE @SQL NVARCHAR(MAX) = 'INSERT INTO #TempAppointmentIds (Id) VALUES (' + 
            REPLACE(@AppointmentIds, ',', '),(') + ')';
        EXEC sp_executesql @SQL;
        
        -- Validate that all appointments can be cancelled
        IF EXISTS (
            SELECT 1 FROM #TempAppointmentIds t
            LEFT JOIN Appointments a ON t.Id = a.Id
            WHERE a.Id IS NULL OR a.Status IN ('Completed', 'Cancelled')
        )
        BEGIN
            RAISERROR('One or more appointments cannot be cancelled (not found or in invalid state)', 16, 1);
        END
        
        -- Update appointment status to cancelled
        UPDATE a
        SET 
            Status = 'Cancelled',
            UpdatedAt = GETUTCDATE(),
            Notes = CASE 
                WHEN a.Notes IS NULL OR a.Notes = '' THEN 'Cancelled: ' + @Reason
                ELSE a.Notes + ' | Cancelled: ' + @Reason
            END
        FROM Appointments a
        INNER JOIN #TempAppointmentIds t ON a.Id = t.Id;
        
        -- Process refunds for paid invoices
        UPDATE i
        SET 
            Status = 'Refunded',
            PaidDate = GETUTCDATE(),
            UpdatedAt = GETUTCDATE(),
            Notes = CASE 
                WHEN i.Notes IS NULL OR i.Notes = '' THEN 'Refunded due to cancellation: ' + @Reason
                ELSE i.Notes + ' | Refunded due to cancellation: ' + @Reason
            END
        FROM Invoices i
        INNER JOIN Appointments a ON i.AppointmentId = a.Id
        INNER JOIN #TempAppointmentIds t ON a.Id = t.Id
        WHERE i.Status = 'Paid';
        
        -- Log the bulk operation
        INSERT INTO JobLogs (OperationType, AppointmentIds, Parameters, Status, CreatedAt)
        VALUES ('BulkCancel', @AppointmentIds, 
                JSON_OBJECT('Reason', @Reason), 
                'Completed', GETUTCDATE());
        
        COMMIT TRANSACTION;
        
        SELECT 
            COUNT(*) as ProcessedCount,
            'Success' as Status,
            'Appointments cancelled and refunds processed successfully' as Message;
            
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SELECT 
            ERROR_NUMBER() as ErrorNumber,
            ERROR_MESSAGE() as ErrorMessage,
            'Failed' as Status;
            
        -- Log the error
        INSERT INTO JobLogs (OperationType, AppointmentIds, Parameters, Status, ErrorMessage, CreatedAt)
        VALUES ('BulkCancel', @AppointmentIds, 
                JSON_OBJECT('Reason', @Reason), 
                'Failed', ERROR_MESSAGE(), GETUTCDATE());
    END CATCH
    
    DROP TABLE #TempAppointmentIds;
END
GO

-- =============================================
-- Table: JobLogs (for audit trail)
-- =============================================
CREATE TABLE JobLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OperationType NVARCHAR(50) NOT NULL,
    AppointmentIds NVARCHAR(MAX) NOT NULL,
    Parameters NVARCHAR(MAX),
    Status NVARCHAR(20) NOT NULL,
    ErrorMessage NVARCHAR(4000),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- =============================================
-- Indexes for performance
-- =============================================
CREATE INDEX IX_JobLogs_OperationType ON JobLogs(OperationType);
CREATE INDEX IX_JobLogs_Status ON JobLogs(Status);
CREATE INDEX IX_JobLogs_CreatedAt ON JobLogs(CreatedAt);
CREATE INDEX IX_Jobs_Status ON Jobs(Status);
CREATE INDEX IX_Jobs_CreatedAt ON Jobs(CreatedAt);
