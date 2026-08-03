#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${root_dir}"

email="${1:-}"
env_file="${ENV_FILE:-.env.production}"
compose=(docker compose --env-file "${env_file}" -f compose.production.yml)

if [[ ! "${email}" =~ ^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$ ]]; then
    echo "Usage: $0 your-registered-email@example.com" >&2
    exit 1
fi

sql="USE [QL_SHOPQUANAO_PRO]; UPDATE dbo.NguoiDung SET IDVaiTro = 1 WHERE Email = N'${email}'; IF @@ROWCOUNT <> 1 THROW 51000, 'Registered user not found or email is not unique.', 1;"

# Expansion is intentionally deferred to bash inside the SQL container.
# shellcheck disable=SC2016
"${compose[@]}" exec -T sqlserver bash -lc \
    '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -No -b -Q "$1"' \
    -- "${sql}"

echo "${email} is now an administrator. Sign out and sign in again to refresh the role cookie."
