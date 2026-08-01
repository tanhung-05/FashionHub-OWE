# FashionHub Docker deployment

Read [Deployment for beginners](deployment-for-beginners.md) first if web
hosting, containers, volumes, and production databases are new concepts.

## Requirements

- Docker Desktop on Windows, or Docker Engine with Compose on Linux
- At least 4 GB RAM available for the SQL Server container
- A production HTTPS URL before testing password-reset email

## Configuration

From the repository root:

```powershell
Copy-Item FashionHub2/.env.example FashionHub2/.env
notepad FashionHub2/.env
```

Replace every placeholder. `PUBLIC_BASE_URL` must be HTTPS in Production. Do
not commit `.env`; production secrets belong in the host or platform secret
store.

## Start and verify

```powershell
cd FashionHub2
docker compose config
docker compose up -d --build
docker compose ps
docker compose logs db-init
docker compose logs web
```

Expected local endpoints:

- Application: `http://localhost:5167`
- Health: `http://localhost:5167/health`
- SQL Server from the same host only: `localhost,1433`

Swagger is intentionally enabled only in Development. The Compose environment
is Production, so `/swagger` is not exposed by this stack.

## Database lifecycle

FashionHub is database-first and does not use EF Core migrations to create the
production schema.

On first startup, `db-init` checks for `QL_SHOPQUANAO_PRO`. If it does not exist,
the service runs the root `DB_Fixed.sql`. If the database already exists, it
exits without executing the destructive rebuild script.

For future schema changes:

1. Write a dated, idempotent SQL upgrade script.
2. Test it against a restored copy of production data.
3. Back up production.
4. Apply the script once.
5. Deploy compatible application code and run smoke tests.

Never use `DB_Fixed.sql` to upgrade a database containing user data.

## Persistent data

Compose uses two named volumes:

- `sqlserver_data` for SQL Server files
- `product_images` for uploaded product images

Normal stop and start commands preserve them:

```powershell
docker compose down
docker compose up -d
```

This command deletes both volumes and therefore deletes local database and
uploaded image data:

```powershell
docker compose down -v
```

Use it only for an intentional local reset.

## Useful commands

```powershell
docker compose ps
docker compose logs -f web
docker compose logs -f sqlserver
docker compose restart web
docker compose pull
docker compose up -d --build
```

Connect to SQL inside the container:

```powershell
docker exec -it fashionhub-sqlserver /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "YOUR_PASSWORD" -No
```

Prefer entering the password interactively or loading it from a local secret;
commands may be retained in shell history.

## Production network rules

The local Compose file binds SQL to `127.0.0.1`, so it is not available through
all host interfaces. On a production host, remove the SQL `ports` mapping
entirely unless an explicit administration path requires it. The web service
reaches SQL by the internal hostname `sqlserver`.

Put a reverse proxy or managed ingress in front of port 5167, expose only ports
80 and 443 publicly, and terminate TLS there. Configure firewall rules before
opening the server to users.

## Backup and restore

A Docker volume prevents data loss when a container is recreated, but it is not
a backup. Schedule SQL Server `.bak` backups to storage outside the VPS and back
up product images separately. Regularly restore both into a test environment;
an untested backup cannot be treated as recoverable.

Official references:

- [Deploy SQL Server containers](https://learn.microsoft.com/sql/linux/containers/deploy)
- [Persist SQL Server container data](https://learn.microsoft.com/sql/linux/containers/configure)
- [Restore a SQL Server backup in a container](https://learn.microsoft.com/sql/linux/migrate/tutorial-restore-backup-sql-server-container)
