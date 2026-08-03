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
else
  echo "Database ${DATABASE} is missing or incomplete; rebuilding it from DB_Fixed.sql..."
  ${SQLCMD} \
    -S "${SERVER}" \
    -U sa \
    -P "${MSSQL_SA_PASSWORD}" \
    -No \
    -b \
    -i "${SCRIPT}"

  echo "Database ${DATABASE} initialized successfully."
fi

if [[ "${DISABLE_DEMO_ACCOUNTS:-false}" == "true" ]]; then
  echo "Disabling seeded demo accounts for production..."
  ${SQLCMD} \
    -S "${SERVER}" \
    -U sa \
    -P "${MSSQL_SA_PASSWORD}" \
    -No \
    -b \
    -Q "
      USE [${DATABASE}];
      UPDATE dbo.NguoiDung
      SET TrangThai = 0,
          SecurityStamp = NEWID(),
          NgayCapNhat = SYSDATETIME()
      WHERE Email IN (
        'admin@fashionhub.local',
        'lan.nguyen@fashionhub.local',
        'binh.tran@fashionhub.local'
      );"
fi

if [[ -n "${APP_DB_PASSWORD:-}" ]]; then
  sqlcmd_variable_marker="$(printf '\044\050')"
  if (( ${#APP_DB_PASSWORD} < 20 )) \
    || [[ "${APP_DB_PASSWORD}" == *"'"* ]] \
    || [[ "${APP_DB_PASSWORD}" == *"${sqlcmd_variable_marker}"* ]]; then
    echo "APP_DB_PASSWORD must be at least 20 characters and cannot contain a single quote or a dollar-parenthesis sequence." >&2
    exit 1
  fi

  echo "Provisioning the least-privilege application database login..."
  ${SQLCMD} \
    -S "${SERVER}" \
    -U sa \
    -P "${MSSQL_SA_PASSWORD}" \
    -No \
    -b \
    -v APP_DB_PASSWORD="${APP_DB_PASSWORD}" \
    -Q "
      DECLARE @Password NVARCHAR(128) = N'\$(APP_DB_PASSWORD)';
      DECLARE @Sql NVARCHAR(MAX);

      IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'fashionhub_app')
        SET @Sql = N'CREATE LOGIN [fashionhub_app] WITH PASSWORD = N''' +
          REPLACE(@Password, N'''', N'''''') +
          N''', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = [${DATABASE}];';
      ELSE
        SET @Sql = N'ALTER LOGIN [fashionhub_app] WITH PASSWORD = N''' +
          REPLACE(@Password, N'''', N'''''') + N''';';

      EXEC sys.sp_executesql @Sql;

      USE [${DATABASE}];
      IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'fashionhub_app')
        CREATE USER [fashionhub_app] FOR LOGIN [fashionhub_app];

      IF IS_ROLEMEMBER(N'db_datareader', N'fashionhub_app') <> 1
        ALTER ROLE [db_datareader] ADD MEMBER [fashionhub_app];
      IF IS_ROLEMEMBER(N'db_datawriter', N'fashionhub_app') <> 1
        ALTER ROLE [db_datawriter] ADD MEMBER [fashionhub_app];

      GRANT EXECUTE TO [fashionhub_app];"

  echo "Application database login is ready."
fi
