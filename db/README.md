# Banner Campaign DB — Local Testing with Docker

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
- Port **1433** free on localhost (not occupied by a local MSSQL instance)

---

## Quick Start

### Option A: Local PowerShell / LocalDB (No Docker required)

If you have SQL Server or LocalDB installed locally, run the script by bypassing the execution policy:

```powershell
powershell -ExecutionPolicy Bypass -File .\test-local.ps1
```

Or connect to a specific instance:
```powershell
powershell -ExecutionPolicy Bypass -File .\test-local.ps1 -ServerInstance "localhost" -UseSqlAuth -Username "sa" -Password "BanneryQuest#2026"
```

> [!TIP]
> If you are already inside an active PowerShell session, you can bypass the policy for the current scope and run it directly:
> ```powershell
> Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
> .\test-local.ps1
> ```

### Option B: Docker Compose

```powershell
docker compose up
```

This will:
1. Pull `mcr.microsoft.com/mssql/server:2022-latest` (~1.6 GB, once only)
2. Start MSSQL on `localhost:1433`
3. Wait until MSSQL is healthy (up to ~60 s on first run)
4. Run all scripts automatically:
   - `01-schema.sql` — creates all 8 tables
   - `02-constraints.sql` — FKs, CHECKs, triggers
   - `03-indexes.sql` — performance indexes
   - `07-seed-data.sql` — inserts realistic test data
5. Execute and print results of both analytical queries

Watch the `bannery-db-init` container logs for output and query results.

---

## Connection Details

| Setting  | Value |
|---|---|
| Host | `localhost` |
| Port | `1433` |
| Database | `BanneryDB` |
| User | `sa` |
| Password | `BanneryQuest#2026` |

> [!NOTE]
> Connect with **Azure Data Studio**, **DBeaver**, **SSMS**, or any MSSQL client using the credentials above.

---

## Useful Commands

```powershell
# Run local automated test suite
powershell -ExecutionPolicy Bypass -File .\test-local.ps1

# Start Docker (detached, background)
docker compose up -d

# Watch init logs
docker logs -f bannery-db-init

# Watch MSSQL server logs
docker logs -f bannery-mssql

# Stop containers (keeps data)
docker compose stop

# Stop and destroy everything (including data volume)
docker compose down -v

# Re-run only the init scripts (after schema is already in place)
docker compose run --rm db-init
```

---

## Re-seeding / Re-initializing

If you want a clean slate:

```powershell
# Stop, remove containers AND the data volume
docker compose down -v

# Start fresh
docker compose up
```

---

## Running Queries Manually

After the database is up, you can run individual SQL files:

```powershell
# Via PowerShell locally:
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d BanneryDB -f 65001 -I -i "04-query-campaign-balance.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d BanneryDB -f 65001 -I -i "05-query-daily-balance.sql"

# Or from Docker container:
docker exec -it bannery-mssql /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "BanneryQuest#2026" -C -I `
  -d BanneryDB `
  -i /sql/04-query-campaign-balance.sql
```

Or use Azure Data Studio / SSMS connected to `localhost,1433`.

---

## File Structure

```
db/
├── test-local.ps1                  ← PowerShell test runner (LocalDB / MSSQL)
├── docker-compose.yml              ← Docker compose setup
├── docker-init/
│   ├── run-scripts.sh              ← Auto-executed by db-init container
│   └── 07-seed-data.sql            ← Test data
├── 01-schema.sql
├── 02-constraints.sql
├── 03-indexes.sql
├── 04-query-campaign-balance.sql   ← Query b: campaign balance
├── 05-query-daily-balance.sql      ← Query c: weekday FIX balance
└── 06-erd.md                       ← ER diagram
```

---

## Expected Query Results

### Query b — Campaign Balance (`04-query-campaign-balance.sql`)

| campaign_id | campaign_name | start_date | end_date | revenue | total_cost | balance |
|---|---|---|---|---|---|---|
| 4 | Black Friday 2026 | 2026-11-20 | 2026-11-26 | 30300.00 | 3550.00 | +26750.00 |
| 3 | Podzim 2026 | 2026-08-01 | 2026-08-07 | 1270.00 | 1416.00 | −146.00 |
| 2 | Výprodej | 2026-07-15 | 2026-07-21 | 1960.00 | 3517.50 | −1557.50 |
| 1 | Léto 2026 | 2026-07-01 | 2026-07-14 | 3840.00 | 11940.00 | −8100.00 |

### Query c — Daily Balance (FIX banners) (`05-query-daily-balance.sql`)

| day_of_week | day_name | revenue | cost | balance |
|---|---|---|---|---|
| 1 | Pondělí | 4270.00 | 2900.00 | +1370.00 |
| 2 | Úterý | 4210.00 | 2900.00 | +1310.00 |
| 3 | Středa | 1100.00 | 2900.00 | −1800.00 |
| 4 | Čtvrtek | 5000.00 | 2900.00 | +2100.00 |
| 5 | Pátek | 4500.00 | 2900.00 | +1600.00 |
| 6 | Sobota | 6800.00 | 2900.00 | +3900.00 |
| 7 | Neděle | 450.00 | 2900.00 | −2450.00 |

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `db-init` fails with "Login failed" | MSSQL not ready yet — it retries automatically. If persistent, check `bannery-mssql` logs. |
| Port 1433 already in use | Stop local MSSQL: `Stop-Service MSSQL*` or change port in `docker-compose.yml` |
| Filtered index creation fails | Ensure `QUOTED_IDENTIFIER ON` and `ANSI_NULLS ON` are set (use `sqlcmd -I`). |
| `MAXRECURSION` error in query c | Add `OPTION (MAXRECURSION 0);` at end of query — only needed if placement spans >100 days |
| Container won't start on Windows | Ensure Docker Desktop is set to **Linux containers** (not Windows containers) |
