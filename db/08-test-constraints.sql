-- ============================================================
-- Banner Campaign Tracking – Constraint & Trigger Tests
-- MS SQL Server
-- ============================================================
-- Verification script for all foreign keys, check constraints,
-- unique indexes, and placement validation triggers.
-- ============================================================

USE BanneryDB;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '============================================================';
PRINT ' Running Constraint & Trigger Unit Tests...';
PRINT '============================================================';

DECLARE @FailedTests INT = 0;
DECLARE @TestName NVARCHAR(200);

-- Helper procedure simulation via temporary table for test results logging
IF OBJECT_ID('tempdb..#TestResults') IS NOT NULL DROP TABLE #TestResults;
CREATE TABLE #TestResults (
    TestID INT IDENTITY(1,1),
    ConstraintName NVARCHAR(100),
    Status NVARCHAR(10),
    Details NVARCHAR(500)
);

-- ------------------------------------------------------------
-- Test 1: FK_BannerPosition_Web (Invalid web_id)
-- ------------------------------------------------------------
SET @TestName = N'FK_BannerPosition_Web (Invalid web_id)';
BEGIN TRY
    INSERT INTO BannerPosition (web_id, width, height, pricing_type, price)
    VALUES (99999, 728, 90, 'FIX', 100.00);
    
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'FAIL', N'Inserting non-existent web_id succeeded.');
    SET @FailedTests = @FailedTests + 1;
END TRY
BEGIN CATCH
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'PASS', ERROR_MESSAGE());
END CATCH;

-- ------------------------------------------------------------
-- Test 2: CK_BannerPosition_Width (Width <= 0)
-- ------------------------------------------------------------
SET @TestName = N'CK_BannerPosition_Width (width <= 0)';
BEGIN TRY
    INSERT INTO BannerPosition (web_id, width, height, pricing_type, price)
    VALUES (1, 0, 90, 'FIX', 100.00);

    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'FAIL', N'Inserting position with width=0 succeeded.');
    SET @FailedTests = @FailedTests + 1;
END TRY
BEGIN CATCH
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'PASS', ERROR_MESSAGE());
END CATCH;

-- ------------------------------------------------------------
-- Test 3: CK_BannerPosition_Height (Height <= 0)
-- ------------------------------------------------------------
SET @TestName = N'CK_BannerPosition_Height (height <= 0)';
BEGIN TRY
    INSERT INTO BannerPosition (web_id, width, height, pricing_type, price)
    VALUES (1, 728, -10, 'FIX', 100.00);

    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'FAIL', N'Inserting position with negative height succeeded.');
    SET @FailedTests = @FailedTests + 1;
END TRY
BEGIN CATCH
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'PASS', ERROR_MESSAGE());
END CATCH;

-- ------------------------------------------------------------
-- Test 4: CK_BannerPosition_Price (Price <= 0)
-- ------------------------------------------------------------
SET @TestName = N'CK_BannerPosition_Price (price <= 0)';
BEGIN TRY
    INSERT INTO BannerPosition (web_id, width, height, pricing_type, price)
    VALUES (1, 728, 90, 'FIX', 0.00);

    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'FAIL', N'Inserting position with price=0 succeeded.');
    SET @FailedTests = @FailedTests + 1;
END TRY
BEGIN CATCH
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'PASS', ERROR_MESSAGE());
END CATCH;

-- ------------------------------------------------------------
-- Test 5: CK_BannerPosition_PricingType (Invalid pricing type)
-- ------------------------------------------------------------
SET @TestName = N'CK_BannerPosition_PricingType (Invalid enum value)';
BEGIN TRY
    INSERT INTO BannerPosition (web_id, width, height, pricing_type, price)
    VALUES (1, 728, 90, 'CPM', 100.00);

    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'FAIL', N'Inserting position with pricing_type="CPM" succeeded.');
    SET @FailedTests = @FailedTests + 1;
END TRY
BEGIN CATCH
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'PASS', ERROR_MESSAGE());
END CATCH;

-- ------------------------------------------------------------
-- Test 6: CK_Banner_Width (Banner width <= 0)
-- ------------------------------------------------------------
SET @TestName = N'CK_Banner_Width (Banner width <= 0)';
BEGIN TRY
    INSERT INTO Banner (name, width, height, target_url)
    VALUES (N'Invalid Banner', 0, 90, 'https://example.com');

    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'FAIL', N'Inserting banner with width=0 succeeded.');
    SET @FailedTests = @FailedTests + 1;
END TRY
BEGIN CATCH
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'PASS', ERROR_MESSAGE());
END CATCH;

-- ------------------------------------------------------------
-- Test 7: CK_Campaign_DateRange (start_date > end_date)
-- ------------------------------------------------------------
SET @TestName = N'CK_Campaign_DateRange (start_date > end_date)';
BEGIN TRY
    INSERT INTO Campaign (name, start_date, end_date)
    VALUES (N'Invalid Campaign', '2026-12-31', '2026-01-01');

    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'FAIL', N'Inserting campaign with start > end succeeded.');
    SET @FailedTests = @FailedTests + 1;
END TRY
BEGIN CATCH
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'PASS', ERROR_MESSAGE());
END CATCH;

-- ------------------------------------------------------------
-- Test 8: CK_CampaignPlacement_DateRange (start_date > end_date)
-- ------------------------------------------------------------
SET @TestName = N'CK_CampaignPlacement_DateRange (start > end)';
BEGIN TRY
    INSERT INTO CampaignPlacement (campaign_id, banner_id, position_id, start_date, end_date)
    VALUES (1, 1, 1, '2026-07-10', '2026-07-01');

    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'FAIL', N'Inserting placement with start > end succeeded.');
    SET @FailedTests = @FailedTests + 1;
END TRY
BEGIN CATCH
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'PASS', ERROR_MESSAGE());
END CATCH;

-- ------------------------------------------------------------
-- Test 9: TR_CampaignPlacement_Validate (Dimension mismatch)
-- ------------------------------------------------------------
SET @TestName = N'TR_CampaignPlacement_Validate (Dimension mismatch)';
BEGIN TRY
    -- Banner 1 is 728x90, Position 2 is 300x250
    INSERT INTO CampaignPlacement (campaign_id, banner_id, position_id, start_date, end_date)
    VALUES (1, 1, 2, '2026-07-01', '2026-07-14');

    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'FAIL', N'Placing 728x90 banner in 300x250 slot succeeded.');
    SET @FailedTests = @FailedTests + 1;
END TRY
BEGIN CATCH
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'PASS', ERROR_MESSAGE());
END CATCH;

-- ------------------------------------------------------------
-- Test 10: TR_CampaignPlacement_Validate (Outside campaign timeframe)
-- ------------------------------------------------------------
SET @TestName = N'TR_CampaignPlacement_Validate (Outside campaign dates)';
BEGIN TRY
    -- Campaign 1 runs 2026-07-01 to 2026-07-14. Placement starts 2026-06-25.
    INSERT INTO CampaignPlacement (campaign_id, banner_id, position_id, start_date, end_date)
    VALUES (1, 1, 1, '2026-06-25', '2026-07-14');

    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'FAIL', N'Placement starting before campaign start date succeeded.');
    SET @FailedTests = @FailedTests + 1;
END TRY
BEGIN CATCH
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'PASS', ERROR_MESSAGE());
END CATCH;

-- ------------------------------------------------------------
-- Test 11: UQ_CampaignPlacement_NoDuplicate (Duplicate placement)
-- ------------------------------------------------------------
SET @TestName = N'UQ_CampaignPlacement_NoDuplicate (Duplicate placement)';
BEGIN TRY
    -- Placement (1, 1, 1, '2026-07-01') already exists in seed data
    INSERT INTO CampaignPlacement (campaign_id, banner_id, position_id, start_date, end_date)
    VALUES (1, 1, 1, '2026-07-01', '2026-07-14');

    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'FAIL', N'Inserting duplicate placement record succeeded.');
    SET @FailedTests = @FailedTests + 1;
END TRY
BEGIN CATCH
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'PASS', ERROR_MESSAGE());
END CATCH;

-- ------------------------------------------------------------
-- Test 12: UQ_OrderBanner_NoDuplicate (Duplicate order banner)
-- ------------------------------------------------------------
SET @TestName = N'UQ_OrderBanner_NoDuplicate (Duplicate order banner)';
BEGIN TRY
    -- OrderBanner (1, 1) already exists in seed data
    INSERT INTO OrderBanner (order_id, banner_id, click_id)
    VALUES (1, 1, NULL);

    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'FAIL', N'Inserting duplicate OrderBanner attribution succeeded.');
    SET @FailedTests = @FailedTests + 1;
END TRY
BEGIN CATCH
    INSERT INTO #TestResults (ConstraintName, Status, Details)
    VALUES (@TestName, N'PASS', ERROR_MESSAGE());
END CATCH;

-- ------------------------------------------------------------
-- Display Test Results Summary
-- ------------------------------------------------------------
SELECT 
    TestID,
    ConstraintName,
    Status,
    Details
FROM #TestResults
ORDER BY TestID;

IF @FailedTests > 0
BEGIN
    DECLARE @ErrMsg NVARCHAR(200) = FORMATMESSAGE(N'Constraint test suite failed with %d error(s).', @FailedTests);
    RAISERROR (@ErrMsg, 16, 1);
END
ELSE
BEGIN
    PRINT '------------------------------------------------------------';
    PRINT ' ALL CONSTRAINT & TRIGGER TESTS PASSED (12/12)!';
    PRINT '------------------------------------------------------------';
END
GO
