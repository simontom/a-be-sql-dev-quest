-- ============================================================
-- Banner Campaign Tracking – Indexes (MS SQL Server)
-- Performance Indexes for Analytical Queries & Business Logic
-- ============================================================

-- Required by MS SQL Server engine for filtered indexes
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================
-- TABLE: BannerClick (High volume table)
-- ============================================================

-- Query b) counts clicks on PPC positions within a campaign.
-- Quickly filters clicks by placement and timestamp range.
CREATE NONCLUSTERED INDEX IX_BannerClick_PlacementId_ClickedAt
    ON BannerClick (placement_id, clicked_at);

-- ============================================================
-- TABLE: OrderBanner (High volume table)
-- ============================================================

-- Query b) joins on banner_id for margin attribution.
-- Covering index: order_id is included to join on [Order] for margin retrieval.
CREATE NONCLUSTERED INDEX IX_OrderBanner_BannerId
    ON OrderBanner (banner_id)
    INCLUDE (order_id);

-- Reverse lookup: find all banners associated with a specific order
CREATE NONCLUSTERED INDEX IX_OrderBanner_OrderId
    ON OrderBanner (order_id)
    INCLUDE (banner_id);

-- Attribution lookup via specific click event
CREATE NONCLUSTERED INDEX IX_OrderBanner_ClickId
    ON OrderBanner (click_id)
    WHERE click_id IS NOT NULL;

-- ============================================================
-- TABLE: [Order] (High volume table)
-- ============================================================

-- Query c) aggregates orders by day of week (order_date).
-- Query b) joins on [Order] to retrieve margin.
CREATE NONCLUSTERED INDEX IX_Order_OrderDate
    ON [Order] (order_date)
    INCLUDE (margin);

-- ============================================================
-- TABLE: CampaignPlacement
-- ============================================================

-- Query b) joins on campaign_id and needs banner_id, position_id, and dates.
-- Covering index for the primary analytical join.
CREATE NONCLUSTERED INDEX IX_CampaignPlacement_CampaignId
    ON CampaignPlacement (campaign_id)
    INCLUDE (banner_id, position_id, start_date, end_date);

-- Query c) retrieves placements by position (for FIX positions).
CREATE NONCLUSTERED INDEX IX_CampaignPlacement_PositionId
    ON CampaignPlacement (position_id)
    INCLUDE (banner_id, start_date, end_date, campaign_id);

-- ============================================================
-- TABLE: BannerPosition
-- ============================================================

-- Filters positions by pricing model ('FIX' vs 'PPC') – Queries b) and c).
CREATE NONCLUSTERED INDEX IX_BannerPosition_PricingType
    ON BannerPosition (pricing_type)
    INCLUDE (price, web_id, width, height);

-- Lookup positions by website (business logic)
CREATE NONCLUSTERED INDEX IX_BannerPosition_WebId
    ON BannerPosition (web_id);

-- ============================================================
-- TABLE: Banner
-- ============================================================

-- Lookup banners by dimensions (business logic – matching slot sizes)
CREATE NONCLUSTERED INDEX IX_Banner_Dimensions
    ON Banner (width, height);

-- ============================================================
-- UNIQUE CONSTRAINTS (Business Rules)
-- ============================================================

-- Prevent duplicate placement of the same banner into the same position
-- on the same start date within a single campaign.
CREATE UNIQUE NONCLUSTERED INDEX UQ_CampaignPlacement_NoDuplicate
    ON CampaignPlacement (campaign_id, banner_id, position_id, start_date);

-- Prevent duplicate attribution of the same banner to an order
CREATE UNIQUE NONCLUSTERED INDEX UQ_OrderBanner_NoDuplicate
    ON OrderBanner (order_id, banner_id);
