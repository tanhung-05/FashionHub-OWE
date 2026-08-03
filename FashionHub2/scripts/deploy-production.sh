#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${root_dir}"

env_file="${ENV_FILE:-.env.production}"
compose=(docker compose --env-file "${env_file}" -f compose.production.yml)

if [[ ! -f "${env_file}" ]]; then
    echo "Missing ${env_file}. Copy .env.production.example and fill every required value." >&2
    exit 1
fi

domain="$(sed -n 's/^DOMAIN=//p' "${env_file}" | tail -n 1)"
if [[ -z "${domain}" || "${domain}" == *example.com ]]; then
    echo "DOMAIN must be a real hostname in ${env_file}." >&2
    exit 1
fi

if grep -Eq '^(SA_PASSWORD|APP_DB_PASSWORD|VNPAY_TMN_CODE|VNPAY_HASH_SECRET|SMTP_PASSWORD)=(replace-|your-)' "${env_file}"; then
    echo "Replace all credential placeholders in ${env_file} before deployment." >&2
    exit 1
fi

echo "Validating production Compose configuration..."
"${compose[@]}" config --quiet

echo "Building the FashionHub web image..."
"${compose[@]}" build --pull web

echo "Starting production services..."
"${compose[@]}" up -d
"${compose[@]}" ps

echo
echo "Deployment started. After DNS resolves, verify: https://${domain}/health"
echo "Follow logs with: docker compose --env-file ${env_file} -f compose.production.yml logs -f caddy web"
