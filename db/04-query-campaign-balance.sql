-- ============================================================
-- Banner Campaign Tracking – Query b): Campaign Balance
-- MS SQL Server
-- ============================================================
-- Output: All campaigns with balance (revenue - total cost),
--         ordered from most profitable to least profitable.
--
-- CALCULATION LOGIC:
--
-- REVENUE:
--   Sum of order margins ([Order].margin) attributed to banners in the campaign.
--   Attribution chain: Order -> OrderBanner -> Banner -> CampaignPlacement -> Campaign.
--   "Full credit" model: each attributed banner accounts for order margin.
--   Deduplication: if an order is attributed to multiple banners in the SAME
--   campaign, the margin is counted only once per campaign (DISTINCT order).
--   Attribution is verified either via specific click (click_id) or order date range.
--
-- COST:
--   a) FIX positions: daily price * number of active days (inclusive: DATEDIFF + 1).
--   b) PPC positions: price per click * click count recorded during placement.
-- ============================================================

WITH
PlacementDetail AS (
    -- Enrich placement records with pricing model and slot price
    SELECT
        cp.id              AS placement_id,
        cp.campaign_id,
        cp.banner_id,
        cp.position_id,
        cp.start_date,
        cp.end_date,
        bp.pricing_type,
        bp.price
    FROM CampaignPlacement cp
    INNER JOIN BannerPosition bp ON bp.id = cp.position_id
),

-- FIX costs: daily price * number of active days
FixCost AS (
    SELECT
        campaign_id,
        SUM(price * (DATEDIFF(DAY, start_date, end_date) + 1)) AS fix_cost
    FROM PlacementDetail
    WHERE pricing_type = 'FIX'
    GROUP BY campaign_id
),

-- PPC costs: price per click * number of clicks during placement timeframe
PpcCost AS (
    SELECT
        pd.campaign_id,
        SUM(pd.price) AS ppc_cost   -- click count * position price per click
    FROM PlacementDetail pd
    INNER JOIN BannerClick bc ON bc.placement_id = pd.placement_id
    WHERE pd.pricing_type = 'PPC'
      AND bc.clicked_at >= CAST(pd.start_date AS DATETIME2)
      AND bc.clicked_at <  DATEADD(DAY, 1, CAST(pd.end_date AS DATETIME2))
    GROUP BY pd.campaign_id
),

-- Revenue: sum of margins from orders attributed to banners in each campaign
-- Deduplication: each order is counted once per campaign
-- Attribution occurs via specific click (click_id) or within active placement timeframe
Revenue AS (
    SELECT
        dist.campaign_id,
        SUM(o.margin) AS revenue
    FROM (
        -- Distinct (campaign_id, order_id) pairs to avoid multi-banner double counting
        SELECT DISTINCT
            pd.campaign_id,
            ob.order_id
        FROM PlacementDetail pd
        INNER JOIN OrderBanner ob ON ob.banner_id = pd.banner_id
        LEFT JOIN BannerClick bc  ON bc.id = ob.click_id
        INNER JOIN [Order] o      ON o.id = ob.order_id
        WHERE (ob.click_id IS NOT NULL AND bc.placement_id = pd.placement_id)
           OR (ob.click_id IS NULL AND o.order_date >= pd.start_date AND o.order_date <= pd.end_date)
    ) AS dist
    INNER JOIN [Order] o ON o.id = dist.order_id
    GROUP BY dist.campaign_id
)

-- Final Output: balance = revenue - total_cost
SELECT
    c.id                                              AS campaign_id,
    c.name                                            AS campaign_name,
    c.start_date,
    c.end_date,
    ISNULL(r.revenue, 0)                              AS revenue,
    ISNULL(fc.fix_cost, 0) + ISNULL(pc.ppc_cost, 0)  AS total_cost,
    ISNULL(r.revenue, 0)
        - ISNULL(fc.fix_cost, 0)
        - ISNULL(pc.ppc_cost, 0)                      AS balance
FROM Campaign c
LEFT JOIN Revenue r   ON r.campaign_id  = c.id
LEFT JOIN FixCost fc  ON fc.campaign_id = c.id
LEFT JOIN PpcCost pc  ON pc.campaign_id = c.id
ORDER BY balance DESC;
