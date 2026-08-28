-- ============================================================
-- Seed Data – Banner Campaign Tracking
-- Realistic test dataset for analytical queries verification.
-- ============================================================

USE BanneryDB;
GO

-- ============================================================
-- Websites
-- ============================================================
INSERT INTO Web (name, url) VALUES
    (N'Seznam.cz',  'https://www.seznam.cz'),
    (N'Heureka.cz', 'https://www.heureka.cz'),
    (N'Zboží.cz',   'https://www.zbozi.cz');

-- ============================================================
-- Banner Positions (FIX and PPC, various dimensions)
-- ============================================================
INSERT INTO BannerPosition (web_id, width, height, pricing_type, price) VALUES
    -- Seznam.cz
    (1, 728, 90,  'FIX',   500.00),   -- leaderboard, 500 CZK/day
    (1, 300, 250, 'PPC',     5.00),   -- medium rectangle, 5 CZK/click
    -- Heureka.cz
    (2, 728, 90,  'FIX',   350.00),   -- leaderboard, 350 CZK/day
    (2, 300, 250, 'PPC',     3.50),   -- medium rectangle, 3.50 CZK/click
    -- Zboží.cz
    (3, 160, 600, 'FIX',   200.00),   -- wide skyscraper, 200 CZK/day
    (3, 300, 600, 'PPC',     4.00);   -- half page, 4 CZK/click

-- ============================================================
-- Banners (dimensions must match target positions)
-- ============================================================
INSERT INTO Banner (name, width, height, target_url) VALUES
    (N'Léto 2026 – Leaderboard',         728, 90,  'https://eshop.cz/kampan/leto2026'),
    (N'Léto 2026 – Medium Rect',         300, 250, 'https://eshop.cz/kampan/leto2026'),
    (N'Výprodej – Leaderboard',          728, 90,  'https://eshop.cz/kampan/vyprodej'),
    (N'Výprodej – Medium Rect',          300, 250, 'https://eshop.cz/kampan/vyprodej'),
    (N'Skyscraper – Podzim',             160, 600, 'https://eshop.cz/kampan/podzim'),
    (N'Half Page – Podzim',              300, 600, 'https://eshop.cz/kampan/podzim'),
    (N'Black Friday – Leaderboard',      728, 90,  'https://eshop.cz/kampan/blackfriday'),
    (N'Black Friday – Medium Rect',      300, 250, 'https://eshop.cz/kampan/blackfriday');

-- ============================================================
-- Campaigns
-- ============================================================
INSERT INTO Campaign (name, start_date, end_date) VALUES
    (N'Léto 2026',           '2026-07-01', '2026-07-14'),   -- 14 days
    (N'Výprodej',            '2026-07-15', '2026-07-21'),   -- 7 days
    (N'Podzim 2026',         '2026-08-01', '2026-08-07'),   -- 7 days
    (N'Black Friday 2026',   '2026-11-20', '2026-11-26');   -- 7 days (highly profitable)

-- ============================================================
-- Campaign Placements (CampaignPlacement)
-- Banner dimensions must match position dimensions!
-- ============================================================
INSERT INTO CampaignPlacement (campaign_id, banner_id, position_id, start_date, end_date) VALUES
    -- Léto 2026: banner 1 (728×90) on position 1 (Seznam 728×90 FIX)
    (1, 1, 1, '2026-07-01', '2026-07-14'),
    -- Léto 2026: banner 2 (300×250) on position 2 (Seznam 300×250 PPC)
    (1, 2, 2, '2026-07-01', '2026-07-14'),
    -- Léto 2026: banner 1 (728×90) on position 3 (Heureka 728×90 FIX)
    (1, 1, 3, '2026-07-01', '2026-07-14'),

    -- Výprodej: banner 3 (728×90) on position 1 (Seznam FIX)
    (2, 3, 1, '2026-07-15', '2026-07-21'),
    -- Výprodej: banner 4 (300×250) on position 4 (Heureka PPC)
    (2, 4, 4, '2026-07-15', '2026-07-21'),

    -- Podzim: banner 5 (160×600) on position 5 (Zboží FIX)
    (3, 5, 5, '2026-08-01', '2026-08-07'),
    -- Podzim: banner 6 (300×600) on position 6 (Zboží PPC)
    (3, 6, 6, '2026-08-01', '2026-08-07'),

    -- Black Friday: banner 7 (728×90) on position 1 (Seznam FIX)
    (4, 7, 1, '2026-11-20', '2026-11-26'),
    -- Black Friday: banner 8 (300×250) on position 2 (Seznam PPC)
    (4, 8, 2, '2026-11-20', '2026-11-26');

-- ============================================================
-- Clicks (BannerClick)
-- ============================================================
INSERT INTO BannerClick (placement_id, clicked_at) VALUES
    -- Placement 2: Seznam PPC (5 CZK/click) – Léto 2026
    (2, '2026-07-01 09:15:00'),
    (2, '2026-07-02 11:30:00'),
    (2, '2026-07-03 14:00:00'),
    (2, '2026-07-04 08:45:00'),
    (2, '2026-07-07 16:20:00'),
    (2, '2026-07-09 10:00:00'),
    (2, '2026-07-11 13:00:00'),
    (2, '2026-07-14 17:00:00'),
    -- Placement 5: Heureka PPC (3.50 CZK/click) – Výprodej
    (5, '2026-07-15 09:00:00'),
    (5, '2026-07-16 10:30:00'),
    (5, '2026-07-17 12:00:00'),
    (5, '2026-07-18 15:00:00'),
    (5, '2026-07-21 09:00:00'),
    -- Placement 7: Zboží PPC (4 CZK/click) – Podzim
    (7, '2026-08-01 08:00:00'),
    (7, '2026-08-02 09:00:00'),
    (7, '2026-08-04 14:00:00'),
    (7, '2026-08-06 11:00:00'),
    -- Placement 9: Seznam PPC (5 CZK/click) – Black Friday (10 clicks = 50 CZK)
    (9, '2026-11-20 10:00:00'),
    (9, '2026-11-20 14:30:00'),
    (9, '2026-11-21 11:15:00'),
    (9, '2026-11-21 19:45:00'),
    (9, '2026-11-22 09:30:00'),
    (9, '2026-11-23 12:00:00'),
    (9, '2026-11-24 15:20:00'),
    (9, '2026-11-25 10:10:00'),
    (9, '2026-11-26 13:40:00'),
    (9, '2026-11-26 18:00:00');

-- ============================================================
-- Orders ([Order])
-- ============================================================
INSERT INTO [Order] (order_date, total_price, margin) VALUES
    -- Léto 2026
    ('2026-07-02', 2500.00, 600.00),   -- id=1
    ('2026-07-03', 1800.00, 420.00),   -- id=2
    ('2026-07-07', 3200.00, 780.00),   -- id=3
    ('2026-07-11', 1500.00, 310.00),   -- id=4
    ('2026-07-14', 2100.00, 530.00),   -- id=5
    -- Výprodej
    ('2026-07-15', 4500.00, 1100.00),  -- id=6
    ('2026-07-17', 900.00,  190.00),   -- id=7
    ('2026-07-20', 2800.00, 670.00),   -- id=8
    -- Podzim
    ('2026-08-02', 1900.00, 450.00),   -- id=9
    ('2026-08-06', 3300.00, 820.00),   -- id=10
    -- Multi-banner attribution order (margin deduplication test)
    ('2026-07-09', 5000.00, 1200.00),  -- id=11
    -- Black Friday 2026 – high conversion and strong margins
    ('2026-11-20', 15000.00, 4500.00), -- id=12 (Friday)
    ('2026-11-21', 22000.00, 6800.00), -- id=13 (Saturday)
    ('2026-11-22', 18500.00, 5200.00), -- id=14 (Sunday)
    ('2026-11-23', 12000.00, 3600.00), -- id=15 (Monday)
    ('2026-11-24',  9500.00, 2900.00), -- id=16 (Tuesday)
    ('2026-11-25', 14000.00, 4100.00), -- id=17 (Wednesday)
    ('2026-11-26', 11000.00, 3200.00); -- id=18 (Thursday)

-- ============================================================
-- Order to Banner Attribution (OrderBanner)
-- Tracks which banners brought the customer to the shop
-- ============================================================
INSERT INTO OrderBanner (order_id, banner_id, click_id) VALUES
    -- Léto 2026 – via banner 1 (Leaderboard)
    (1,  1, NULL),
    (2,  2, 2),     -- via specific click id=2
    (3,  1, NULL),
    (4,  2, 6),     -- via specific click id=6
    (5,  1, NULL),
    -- Výprodej – via banners 3 and 4
    (6,  3, NULL),
    (7,  4, 9),
    (8,  3, NULL),
    -- Podzim – via banners 5 and 6
    (9,  5, NULL),
    (10, 6, 14),
    -- Order 11: attributed to BOTH banners 1 and 2 (margin deduplication test)
    (11, 1, 7),
    (11, 2, NULL),
    -- Black Friday 2026 (orders 12-18)
    (12, 7, NULL),  -- via Black Friday Leaderboard (FIX)
    (13, 7, NULL),  -- via Black Friday Leaderboard (FIX)
    (14, 8, 22),    -- via Black Friday Medium Rect (PPC click id=22)
    (15, 7, NULL),  -- via Black Friday Leaderboard (FIX)
    (16, 7, NULL),  -- via Black Friday Leaderboard (FIX)
    (17, 8, 25),    -- via Black Friday Medium Rect (PPC click id=25)
    (18, 7, NULL);  -- via Black Friday Leaderboard (FIX)

GO

-- ============================================================
-- Row Counts Verification
-- ============================================================
SELECT 'Web' AS [Table], COUNT(*) AS [Rows] FROM Web UNION ALL
SELECT 'BannerPosition', COUNT(*) FROM BannerPosition UNION ALL
SELECT 'Banner',         COUNT(*) FROM Banner UNION ALL
SELECT 'Campaign',       COUNT(*) FROM Campaign UNION ALL
SELECT 'CampaignPlacement', COUNT(*) FROM CampaignPlacement UNION ALL
SELECT 'BannerClick',    COUNT(*) FROM BannerClick UNION ALL
SELECT 'Order',          COUNT(*) FROM [Order] UNION ALL
SELECT 'OrderBanner',    COUNT(*) FROM OrderBanner;
