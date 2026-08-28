# Banner Campaign Tracking — Entity Relationship Diagram

```mermaid
erDiagram
    Web ||--o{ BannerPosition : "has positions"
    BannerPosition ||--o{ CampaignPlacement : "used in"
    Banner ||--o{ CampaignPlacement : "placed as"
    Campaign ||--o{ CampaignPlacement : "contains"
    CampaignPlacement ||--o{ BannerClick : "generates"
    Banner ||--o{ OrderBanner : "attributed to"
    Order ||--o{ OrderBanner : "came from"
    BannerClick |o--o{ OrderBanner : "optionally linked"

    Web {
        INT id PK
        NVARCHAR_200 name
        NVARCHAR_500 url
    }

    BannerPosition {
        INT id PK
        INT web_id FK
        INT width
        INT height
        VARCHAR_5 pricing_type "FIX or PPC"
        DECIMAL_18_2 price
    }

    Banner {
        INT id PK
        NVARCHAR_200 name
        INT width
        INT height
        NVARCHAR_500 target_url
    }

    Campaign {
        INT id PK
        NVARCHAR_200 name
        DATE start_date
        DATE end_date
    }

    CampaignPlacement {
        INT id PK
        INT campaign_id FK
        INT banner_id FK
        INT position_id FK
        DATE start_date
        DATE end_date
    }

    BannerClick {
        INT id PK
        INT placement_id FK
        DATETIME2 clicked_at
    }

    Order {
        INT id PK
        DATE order_date
        DECIMAL_18_2 total_price
        DECIMAL_18_2 margin
    }

    OrderBanner {
        INT id PK
        INT order_id FK
        INT banner_id FK
        INT click_id FK "nullable"
    }
```

## Relationship Summary

| Relationship | Cardinality | Description |
|---|---|---|
| Web -> BannerPosition | 1:N | Each website contains multiple banner positions/slots |
| Campaign -> CampaignPlacement | 1:N | A campaign contains multiple banner placements |
| Banner -> CampaignPlacement | 1:N | A banner creative can be placed in multiple positions/campaigns |
| BannerPosition -> CampaignPlacement | 1:N | A position slot can host different banners over time |
| CampaignPlacement -> BannerClick | 1:N | Placements record individual user click events |
| Order ↔ Banner (via OrderBanner) | M:N | An order can be attributed to one or more banners |
| OrderBanner -> BannerClick | N:1 (optional) | Optional link to trace attribution to a specific click event |
