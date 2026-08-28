#!/bin/bash
# ============================================================
# Entrypoint: runs all SQL scripts against the MSSQL instance
# Called by the db-init container once MSSQL is healthy.
#
# Volume mounts (set in docker-compose.yml):
#   /sql        -> ./         (all *.sql deliverables)
#   /docker-init -> ./docker-init  (this script + seed data)
# ============================================================

SERVER="mssql"
USER="sa"
PASS="BanneryQuest#2026"
DB="BanneryDB"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
OPTS="-S $SERVER -U $USER -P $PASS -C -b -I"

echo "==> Creating database $DB ..."
$SQLCMD $OPTS -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '$DB') CREATE DATABASE [$DB];"
if [ $? -ne 0 ]; then
    echo "ERROR: Could not create database. Check MSSQL connectivity."
    exit 1
fi

run_script() {
    local label=$1
    local file=$2
    echo ""
    echo "==> [$label] $file ..."
    $SQLCMD $OPTS -d "$DB" -i "$file"
    if [ $? -ne 0 ]; then
        echo "ERROR: $file failed. Aborting."
        exit 1
    fi
}

# --- Schema, Constraints, Indexes --------------------------
run_script "Schema"      /sql/01-schema.sql
run_script "Constraints" /sql/02-constraints.sql
run_script "Indexes"     /sql/03-indexes.sql

# --- Seed Data ---------------------------------------------
run_script "Seed"        /docker-init/07-seed-data.sql

# --- Analytical Queries (output to console) ----------------
echo ""
echo "=============================================="
echo " Query b: Campaign Balance"
echo "=============================================="
$SQLCMD $OPTS -d "$DB" -i /sql/04-query-campaign-balance.sql

echo ""
echo "=============================================="
echo " Query c: Daily Balance for FIX Banners"
echo "=============================================="
$SQLCMD $OPTS -d "$DB" -i /sql/05-query-daily-balance.sql

echo ""
echo "==> All done. Database '$DB' is ready."
echo "    Connect: localhost:1433 | user: sa | pass: BanneryQuest#2026"
