#!/usr/bin/env bash
set -euo pipefail

SQLCMD=/opt/mssql-tools18/bin/sqlcmd
SERVER="${SQL_SERVER:-sqlserver}"
DATABASE="QL_SHOPQUANAO_PRO"
SCRIPT=/scripts/DB_Fixed.sql

echo "Checking whether ${DATABASE} is fully initialized..."

database_ready="$(${SQLCMD} \
  -S "${SERVER}" \
  -U sa \
  -P "${MSSQL_SA_PASSWORD}" \
  -No \
  -h -1 \
  -W \
  -Q "SET NOCOUNT ON;
      IF DB_ID(N'${DATABASE}') IS NULL
          SELECT 0;
      ELSE IF EXISTS (
          SELECT 1 FROM [${DATABASE}].sys.tables WHERE name = N'TinNhanChat'
      ) AND EXISTS (
          SELECT 1 FROM [${DATABASE}].sys.indexes WHERE name = N'IX_TinNhanChat_CuocTroChuyen_NgayTao'
      ) AND EXISTS (
          SELECT 1 FROM [${DATABASE}].dbo.SanPham WHERE Slug = N'ao-so-mi-oxford-regular'
      )
          SELECT 1;
      ELSE
          SELECT 0;")"

if [[ "${database_ready}" == "1" ]]; then
  echo "Database ${DATABASE} is already fully initialized; initialization skipped."
  exit 0
fi

echo "Database ${DATABASE} is missing or incomplete; rebuilding it from DB_Fixed.sql..."
${SQLCMD} \
  -S "${SERVER}" \
  -U sa \
  -P "${MSSQL_SA_PASSWORD}" \
  -No \
  -b \
  -i "${SCRIPT}"

echo "Database ${DATABASE} initialized successfully."
