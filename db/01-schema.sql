-- ============================================================
-- Banner Campaign Tracking – Schema (MS SQL Server)
-- Quest 1: Tracking Banner Campaign Effectiveness
-- ============================================================

-- Clean up existing tables in reverse dependency order
-- To ensure repeatable, idempotent schema deployments
DROP TABLE IF EXISTS OrderBanner;
DROP TABLE IF EXISTS BannerClick;
DROP TABLE IF EXISTS [Order];
DROP TABLE IF EXISTS CampaignPlacement;
DROP TABLE IF EXISTS Campaign;
DROP TABLE IF EXISTS Banner;
DROP TABLE IF EXISTS BannerPosition;
DROP TABLE IF EXISTS Web;
GO

-- 1. Websites – External websites where banners are displayed
CREATE TABLE Web (
    id          INT           IDENTITY(1,1) NOT NULL,
    name        NVARCHAR(200) NOT NULL,
    url         NVARCHAR(500) NOT NULL,
    CONSTRAINT PK_Web PRIMARY KEY (id)
);

-- 2. Banner Positions – Specific ad slots on a website with dimensions and pricing model
--    pricing_type: 'FIX' = pay per day, 'PPC' = pay per click
--    price: cost per unit (per day for FIX, per click for PPC)
CREATE TABLE BannerPosition (
    id            INT            IDENTITY(1,1) NOT NULL,
    web_id        INT            NOT NULL,
    width         INT            NOT NULL,
    height        INT            NOT NULL,
    pricing_type  VARCHAR(5)     NOT NULL,       -- 'FIX' or 'PPC'
    price         DECIMAL(18,2)  NOT NULL,        -- cost per day (FIX) or cost per click (PPC)
    CONSTRAINT PK_BannerPosition PRIMARY KEY (id)
);

-- 3. Banners – Graphic creatives with target destination URL
--    Dimensions must match the BannerPosition where the banner is placed.
CREATE TABLE Banner (
    id          INT           IDENTITY(1,1) NOT NULL,
    name        NVARCHAR(200) NOT NULL,
    width       INT           NOT NULL,
    height      INT           NOT NULL,
    target_url  NVARCHAR(500) NOT NULL,
    CONSTRAINT PK_Banner PRIMARY KEY (id)
);

-- 4. Campaigns – Time-bounded marketing campaigns
CREATE TABLE Campaign (
    id          INT           IDENTITY(1,1) NOT NULL,
    name        NVARCHAR(200) NOT NULL,
    start_date  DATE          NOT NULL,
    end_date    DATE          NOT NULL,
    CONSTRAINT PK_Campaign PRIMARY KEY (id)
);

-- 5. Campaign Placements – Mapping of campaign, banner, and position within a timeframe
--    Placement can have its own date range (subset of the campaign interval).
CREATE TABLE CampaignPlacement (
    id            INT   IDENTITY(1,1) NOT NULL,
    campaign_id   INT   NOT NULL,
    banner_id     INT   NOT NULL,
    position_id   INT   NOT NULL,
    start_date    DATE  NOT NULL,
    end_date      DATE  NOT NULL,
    CONSTRAINT PK_CampaignPlacement PRIMARY KEY (id)
);

-- 6. Banner Clicks – Tracking individual click events on a placement
--    Required for calculating PPC costs.
CREATE TABLE BannerClick (
    id            INT        IDENTITY(1,1) NOT NULL,
    placement_id  INT        NOT NULL,
    clicked_at    DATETIME2  NOT NULL,
    CONSTRAINT PK_BannerClick PRIMARY KEY (id)
);

-- 7. Orders – E-shop customer orders
CREATE TABLE [Order] (
    id            INT            IDENTITY(1,1) NOT NULL,
    order_date    DATE           NOT NULL,
    total_price   DECIMAL(18,2)  NOT NULL,
    margin        DECIMAL(18,2)  NOT NULL,
    CONSTRAINT PK_Order PRIMARY KEY (id)
);

-- 8. Order Banner Attribution – M:N relationship
--    Tracks which banners contributed to the customer's purchase.
--    click_id is optional – allows tracing to a specific click event if recorded.
CREATE TABLE OrderBanner (
    id          INT  IDENTITY(1,1) NOT NULL,
    order_id    INT  NOT NULL,
    banner_id   INT  NOT NULL,
    click_id    INT  NULL,          -- optional link to specific click
    CONSTRAINT PK_OrderBanner PRIMARY KEY (id)
);
