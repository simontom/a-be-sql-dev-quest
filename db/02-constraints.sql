-- ============================================================
-- Banner Campaign Tracking – Constraints (MS SQL Server)
-- Foreign Keys, Check Constraints, Placement Integrity Trigger
-- ============================================================

-- ============================================================
-- FOREIGN KEYS
-- ============================================================

-- BannerPosition -> Web
ALTER TABLE BannerPosition
    ADD CONSTRAINT FK_BannerPosition_Web
    FOREIGN KEY (web_id) REFERENCES Web(id);

-- CampaignPlacement -> Campaign
ALTER TABLE CampaignPlacement
    ADD CONSTRAINT FK_CampaignPlacement_Campaign
    FOREIGN KEY (campaign_id) REFERENCES Campaign(id);

-- CampaignPlacement -> Banner
ALTER TABLE CampaignPlacement
    ADD CONSTRAINT FK_CampaignPlacement_Banner
    FOREIGN KEY (banner_id) REFERENCES Banner(id);

-- CampaignPlacement -> BannerPosition
ALTER TABLE CampaignPlacement
    ADD CONSTRAINT FK_CampaignPlacement_Position
    FOREIGN KEY (position_id) REFERENCES BannerPosition(id);

-- BannerClick -> CampaignPlacement
ALTER TABLE BannerClick
    ADD CONSTRAINT FK_BannerClick_Placement
    FOREIGN KEY (placement_id) REFERENCES CampaignPlacement(id);

-- OrderBanner -> Order
ALTER TABLE OrderBanner
    ADD CONSTRAINT FK_OrderBanner_Order
    FOREIGN KEY (order_id) REFERENCES [Order](id);

-- OrderBanner -> Banner
ALTER TABLE OrderBanner
    ADD CONSTRAINT FK_OrderBanner_Banner
    FOREIGN KEY (banner_id) REFERENCES Banner(id);

-- OrderBanner -> BannerClick (optional link)
ALTER TABLE OrderBanner
    ADD CONSTRAINT FK_OrderBanner_Click
    FOREIGN KEY (click_id) REFERENCES BannerClick(id);

-- ============================================================
-- CHECK CONSTRAINTS
-- ============================================================

-- Position dimensions must be positive integers
ALTER TABLE BannerPosition
    ADD CONSTRAINT CK_BannerPosition_Width  CHECK (width > 0),
        CONSTRAINT CK_BannerPosition_Height CHECK (height > 0);

-- Position price must be positive
ALTER TABLE BannerPosition
    ADD CONSTRAINT CK_BannerPosition_Price CHECK (price > 0);

-- Pricing model – allowed enum values ('FIX' or 'PPC')
ALTER TABLE BannerPosition
    ADD CONSTRAINT CK_BannerPosition_PricingType CHECK (pricing_type IN ('FIX', 'PPC'));

-- Banner dimensions must be positive integers
ALTER TABLE Banner
    ADD CONSTRAINT CK_Banner_Width  CHECK (width > 0),
        CONSTRAINT CK_Banner_Height CHECK (height > 0);

-- Campaign dates – start date cannot be after end date
ALTER TABLE Campaign
    ADD CONSTRAINT CK_Campaign_DateRange CHECK (start_date <= end_date);

-- Placement dates – start date cannot be after end date
ALTER TABLE CampaignPlacement
    ADD CONSTRAINT CK_CampaignPlacement_DateRange CHECK (start_date <= end_date);

-- Order total price must be non-negative
ALTER TABLE [Order]
    ADD CONSTRAINT CK_Order_TotalPrice CHECK (total_price >= 0);

-- Order margin – non-negative
ALTER TABLE [Order]
    ADD CONSTRAINT CK_Order_Margin CHECK (margin >= 0);

-- ============================================================
-- TRIGGER: Placement Integrity Validation
-- ============================================================
-- MS SQL Server does not support cross-table CHECK constraints.
-- Validations are enforced via an AFTER INSERT, UPDATE trigger:
-- 1. Banner dimensions must match BannerPosition dimensions (width and height).
-- 2. Placement active date range must be within the parent campaign date range.
GO

CREATE OR ALTER TRIGGER TR_CampaignPlacement_Validate
ON CampaignPlacement
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Validate dimension matching
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN Banner b   ON b.id = i.banner_id
        INNER JOIN BannerPosition p ON p.id = i.position_id
        WHERE b.width  <> p.width
           OR b.height <> p.height
    )
    BEGIN
        RAISERROR (
            N'Banner dimensions must match the banner position dimensions (width and height).',
            16, 1
        );
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- 2. Validate placement timeframe within campaign boundaries
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN Campaign c ON c.id = i.campaign_id
        WHERE i.start_date < c.start_date
           OR i.end_date   > c.end_date
    )
    BEGIN
        RAISERROR (
            N'Placement date range (start_date, end_date) must fall within the campaign date range.',
            16, 1
        );
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO
