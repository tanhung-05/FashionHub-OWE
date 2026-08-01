#!/usr/bin/env bash
set -euo pipefail

SQLCMD=/opt/mssql-tools18/bin/sqlcmd
SERVER="${SQL_SERVER:-sqlserver}"
DATABASE="QL_SHOPQUANAO_PRO"
SCRIPT=/scripts/DB_Fixed.sql

echo "Checking whether ${DATABASE} needs to be initialized..."

database_exists="$(${SQLCMD} \
  -S "${SERVER}" \
  -U sa \
  -P "${MSSQL_SA_PASSWORD}" \
  -No \
  -h -1 \
  -W \
  -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'${DATABASE}') IS NULL THEN 0 ELSE 1 END;")"

if [[ "${database_exists}" == "1" ]]; then
  echo "Database ${DATABASE} already exists; initialization skipped."
  exit 0
fi

echo "Creating ${DATABASE} from DB_Fixed.sql..."
${SQLCMD} \
  -S "${SERVER}" \
  -U sa \
  -P "${MSSQL_SA_PASSWORD}" \
  -No \
  -b \
  -i "${SCRIPT}"

echo "Database ${DATABASE} initialized successfully."
