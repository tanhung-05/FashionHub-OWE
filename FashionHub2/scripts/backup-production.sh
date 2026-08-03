#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${root_dir}"

env_file="${ENV_FILE:-.env.production}"
compose=(docker compose --env-file "${env_file}" -f compose.production.yml)
backup_dir="${BACKUP_DIR:-backups/sqlserver}"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup_file="FashionHub_${timestamp}.bak"
container_path="/var/opt/mssql/backup/${backup_file}"

if [[ ! -f "${env_file}" ]]; then
    echo "Missing ${env_file}." >&2
    exit 1
fi

container_id="$("${compose[@]}" ps -q sqlserver)"
if [[ -z "${container_id}" ]]; then
    echo "The production SQL Server container is not running." >&2
    exit 1
fi

mkdir -p "${backup_dir}"

backup_sql="BACKUP DATABASE [QL_SHOPQUANAO_PRO] TO DISK = N'${container_path}' WITH COPY_ONLY, INIT, CHECKSUM, STATS = 10;"
verify_sql="RESTORE VERIFYONLY FROM DISK = N'${container_path}' WITH CHECKSUM;"

echo "Creating ${backup_file}..."
# Expansion is intentionally deferred to bash inside the SQL container.
# shellcheck disable=SC2016
"${compose[@]}" exec -T sqlserver bash -lc \
    '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -No -b -Q "$1"' \
    -- "${backup_sql}"

echo "Verifying SQL Server backup..."
# Expansion is intentionally deferred to bash inside the SQL container.
# shellcheck disable=SC2016
"${compose[@]}" exec -T sqlserver bash -lc \
    '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -No -b -Q "$1"' \
    -- "${verify_sql}"

docker cp "${container_id}:${container_path}" "${backup_dir}/${backup_file}"
echo "Backup copied to ${backup_dir}/${backup_file}"
echo "Copy this file to storage outside the VPS; a same-server copy is not disaster recovery."
