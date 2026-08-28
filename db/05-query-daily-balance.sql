-- ============================================================
-- Banner Campaign Tracking – Query c): Daily Balance for FIX Banners
-- MS SQL Server
-- ============================================================
-- Output: Exactly 7 rows (Monday-Sunday), total balance (revenue - cost)
--         for banner positions with 'FIX' (pay per day) pricing model.
--
-- CALCULATION LOGIC:
--
-- For each day a banner is active in a FIX position:
--   COST:    Daily position price (BannerPosition.price).
--   REVENUE: Sum of order margins ([Order].margin) created on that day,
--            attributed to that banner.
--
-- Weekday indexing: DATEPART(WEEKDAY, ...) with SET DATEFIRST 1 (Monday = 1).
--
-- Note: A recursive CTE expands each placement interval into individual dates,
-- enabling costs and revenues to be aggregated accurately by weekday.
-- ============================================================

-- Configure Monday as the first day of the week (ISO 8601 standard)
SET DATEFIRST 1;

WITH
-- Calendar day generator for every FIX placement
-- Recursive CTE generates a sequence of dates from start_date to end_date
PlacementDays AS (
    SELECT
        cp.id           AS placement_id,
        cp.banner_id,
        cp.position_id,
        cp.start_date   AS day_date,
        cp.end_date,
        bp.price        AS daily_cost
    FROM CampaignPlacement cp
    INNER JOIN BannerPosition bp ON bp.id = cp.position_id
    WHERE bp.pricing_type = 'FIX'

    UNION ALL

    SELECT
        placement_id,
        banner_id,
        position_id,
        DATEADD(DAY, 1, day_date),
        end_date,
        daily_cost
    FROM PlacementDays
    WHERE day_date < end_date
),

-- Daily costs: aggregated by day of week
DailyCost AS (
    SELECT
        DATEPART(WEEKDAY, day_date) AS day_of_week,
        SUM(daily_cost)             AS total_cost
    FROM PlacementDays
    GROUP BY DATEPART(WEEKDAY, day_date)
),

-- Daily revenue: margins from orders placed on that day, attributed to FIX banners
-- Orders are deduplicated (DISTINCT) so margin is not multiplied
-- if a banner was active on multiple FIX positions simultaneously on that day.
DailyRevenue AS (
    SELECT
        DATEPART(WEEKDAY, fix_orders.order_date) AS day_of_week,
        SUM(fix_orders.margin)                   AS total_revenue
    FROM (
        SELECT DISTINCT
            o.id,
            o.order_date,
            o.margin
        FROM [Order] o
        INNER JOIN OrderBanner ob   ON ob.order_id = o.id
        INNER JOIN PlacementDays pd ON pd.banner_id = ob.banner_id
                                   AND pd.day_date = o.order_date
    ) fix_orders
    GROUP BY DATEPART(WEEKDAY, fix_orders.order_date)
),

-- All 7 days of the week (guarantees 7 rows even if data is missing for some days)
WeekDays AS (
    SELECT
        1           AS day_of_week,
        N'Pondělí'  AS day_name
    UNION ALL
    SELECT 2, N'Úterý'
    UNION ALL
    SELECT 3, N'Středa'
    UNION ALL
    SELECT 4, N'Čtvrtek'
    UNION ALL
    SELECT 5, N'Pátek'
    UNION ALL
    SELECT 6, N'Sobota'
    UNION ALL
    SELECT 7, N'Neděle'
)

-- Final Output: 7 rows, Monday through Sunday
SELECT
    wd.day_of_week,
    wd.day_name,
    ISNULL(dr.total_revenue, 0)                              AS revenue,
    ISNULL(dc.total_cost, 0)                                 AS cost,
    ISNULL(dr.total_revenue, 0) - ISNULL(dc.total_cost, 0)  AS balance
FROM WeekDays wd
LEFT JOIN DailyCost dc    ON dc.day_of_week = wd.day_of_week
LEFT JOIN DailyRevenue dr ON dr.day_of_week = wd.day_of_week
ORDER BY wd.day_of_week;

-- Note: For recursive CTEs spanning large date ranges (> 100 days), append:
-- OPTION (MAXRECURSION 0);
-- (default recursion limit is 100 levels)
