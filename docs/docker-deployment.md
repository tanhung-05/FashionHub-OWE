# Docker Deployment Guide - FashionHub

Comprehensive guide for deploying FashionHub using Docker and Docker Compose.

## Prerequisites

### Required Software
- Docker Desktop 4.0+ (Windows/Mac) or Docker Engine 20.10+ (Linux)
- Docker Compose 2.0+
- Git (for cloning the repository)
- 4GB+ RAM available for containers
- 10GB+ free disk space

### Check Installation
```bash
docker --version
docker-compose --version
```

## Quick Start

### 1. Clone Repository
```bash
git clone <repository-url>
cd Fasssshionnnnnn/FashionHub2
```

### 2. Configure Environment
```bash
# Copy environment template
cp .env.example .env

# Edit .env with your configuration
notepad .env  # Windows
nano .env     # Linux/Mac
```

Required environment variables:
- `SA_PASSWORD`: SQL Server SA password (minimum 8 characters, must include uppercase, lowercase, numbers, and special characters)
- `GEMINI_API_KEY`: Your Gemini AI API key (optional, for chat feature)
- `ASPNETCORE_ENVIRONMENT`: Set to `Production` or `Development`

### 3. Build and Run
```bash
# Build Docker images
docker-compose build

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f web
```

### 4. Access Application
- Web Application: http://localhost:5167
- Health Check: http://localhost:5167/health
- SQL Server: localhost:1433

Default admin credentials will be seeded during first run (if configured).

## Detailed Configuration

### Docker Compose Services

#### SQL Server Service
```yaml
sqlserver:
  image: mcr.microsoft.com/mssql/server:2022-latest
  ports:
    - "1433:1433"
  environment:
    - SA_PASSWORD=YourStrong@Passw0rd
    - MSSQL_PID=Developer
```

**Configuration Options:**
- `MSSQL_PID`: `Developer` (free), `Express` (free), or `Enterprise` (requires license)
- Data persisted in `sqlserver_data` volume

#### Web Service
```yaml
web:
  build:
    context: .
    dockerfile: FashionHub.Web/Dockerfile
  ports:
    - "5167:8080"
  environment:
    - ASPNETCORE_ENVIRONMENT=Production
    - ConnectionStrings__DefaultConnection=...
```

**Port Configuration:**
- External: 5167 (change in docker-compose.yml if needed)
- Internal: 8080 (ASP.NET Core default)

### Database Initialization

#### Automatic Migration
The application will automatically apply migrations on startup if the database doesn't exist.

#### Manual Migration
```bash
# Enter web container
docker exec -it fashionhub-web bash

# Run migrations
dotnet ef database update

# Exit container
exit
```

#### Restore from Backup
```bash
# Copy SQL backup to container
docker cp backup.bak fashionhub-sqlserver:/var/opt/mssql/data/

# Restore using sqlcmd
docker exec -it fashionhub-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -Q "RESTORE DATABASE FashionHub FROM DISK='/var/opt/mssql/data/backup.bak' WITH MOVE 'FashionHub' TO '/var/opt/mssql/data/FashionHub.mdf', MOVE 'FashionHub_log' TO '/var/opt/mssql/data/FashionHub_log.ldf'"
```

## Docker Commands Reference

### Container Management
```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# Restart services
docker-compose restart

# View running containers
docker-compose ps

# View logs
docker-compose logs -f web
docker-compose logs -f sqlserver

# Execute command in container
docker exec -it fashionhub-web bash
docker exec -it fashionhub-sqlserver bash
```

### Image Management
```bash
# Build images
docker-compose build

# Rebuild without cache
docker-compose build --no-cache

# Pull latest base images
docker-compose pull

# Remove unused images
docker image prune -a
```

### Volume Management
```bash
# List volumes
docker volume ls

# Inspect volume
docker volume inspect fashionhub2_sqlserver_data

# Remove volumes (WARNING: deletes data)
docker-compose down -v

# Backup volume
docker run --rm -v fashionhub2_sqlserver_data:/data -v $(pwd):/backup ubuntu tar czf /backup/sqlserver-backup.tar.gz -C /data .

# Restore volume
docker run --rm -v fashionhub2_sqlserver_data:/data -v $(pwd):/backup ubuntu tar xzf /backup/sqlserver-backup.tar.gz -C /data
```

## Production Deployment

### Security Considerations

1. **Environment Variables**
   - Never commit `.env` to version control
   - Use strong passwords (16+ characters)
   - Rotate passwords regularly
   - Use secrets management in production (Azure Key Vault, AWS Secrets Manager)

2. **Network Security**
   - Use reverse proxy (nginx, traefik) for SSL/TLS
   - Don't expose SQL Server port externally
   - Configure firewall rules
   - Use Docker networks for service isolation

3. **Container Security**
   - Run containers as non-root user
   - Use read-only root filesystem where possible
   - Scan images for vulnerabilities: `docker scan fashionhub-web`
   - Keep base images updated

### SSL/TLS Configuration

Use a reverse proxy for SSL termination:

```yaml
# docker-compose.prod.yml
services:
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - web
```

### Resource Limits

Add resource constraints in `docker-compose.yml`:

```yaml
services:
  web:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '1'
          memory: 1G
  
  sqlserver:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 4G
        reservations:
          cpus: '1'
          memory: 2G
```

### Health Monitoring

```bash
# Check health status
docker-compose ps
curl http://localhost:5167/health

# View resource usage
docker stats

# Set up monitoring (example with Prometheus)
# Add to docker-compose.yml:
  prometheus:
    image: prom/prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"
```

### Backup Strategy

1. **Database Backups**
```bash
# Create backup script
cat > backup-db.sh << 'EOF'
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
docker exec fashionhub-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "BACKUP DATABASE FashionHub TO DISK='/var/opt/mssql/data/backup_$DATE.bak'"
docker cp fashionhub-sqlserver:/var/opt/mssql/data/backup_$DATE.bak ./backups/
EOF
chmod +x backup-db.sh

# Schedule with cron
0 2 * * * /path/to/backup-db.sh
```

2. **Volume Backups**
```bash
# Backup all volumes
docker run --rm \
  -v fashionhub2_sqlserver_data:/data \
  -v $(pwd)/backups:/backup \
  ubuntu tar czf /backup/volumes_$(date +%Y%m%d).tar.gz -C /data .
```

### Scaling

```bash
# Scale web service
docker-compose up -d --scale web=3

# Use load balancer (nginx/traefik) to distribute traffic
```

## Troubleshooting

### Common Issues

#### SQL Server Won't Start
```bash
# Check logs
docker-compose logs sqlserver

# Common issues:
# - Insufficient memory (needs 2GB minimum)
# - Password doesn't meet complexity requirements
# - Port 1433 already in use

# Solution: Adjust memory, fix password, or change port mapping
```

#### Web Application Can't Connect to Database
```bash
# Check if SQL Server is healthy
docker-compose ps

# Check connection string
docker-compose exec web printenv | grep ConnectionStrings

# Test connection from web container
docker-compose exec web ping sqlserver

# Solution: Ensure sqlserver service is healthy before web starts
```

#### Migration Errors
```bash
# View detailed logs
docker-compose logs web

# Manually run migrations
docker-compose exec web dotnet ef database update --verbose

# Reset database (WARNING: deletes all data)
docker-compose down -v
docker-compose up -d
```

#### Port Already in Use
```bash
# Find what's using the port
netstat -ano | findstr :5167  # Windows
lsof -i :5167                 # Linux/Mac

# Solution: Change port in docker-compose.yml or stop conflicting service
```

### Performance Issues

```bash
# Check resource usage
docker stats

# Check logs for errors
docker-compose logs --tail=100 web

# Increase memory limits in docker-compose.yml
# Enable SQL Server query logging for slow queries
```

### Debug Mode

```bash
# Run with debug output
docker-compose --verbose up

# Enter container shell
docker exec -it fashionhub-web bash

# Check environment variables
docker-compose exec web env

# Check file system
docker-compose exec web ls -la /app
```

## Maintenance

### Update Application
```bash
# Pull latest code
git pull

# Rebuild and restart
docker-compose build
docker-compose up -d

# Check health
curl http://localhost:5167/health
```

### Clean Up
```bash
# Remove stopped containers
docker-compose down

# Remove unused images
docker image prune

# Remove unused volumes (WARNING: data loss)
docker volume prune

# Full cleanup (WARNING: removes everything)
docker system prune -a --volumes
```

### Log Rotation
```bash
# Configure in docker-compose.yml
services:
  web:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

## CI/CD Integration

### GitHub Actions Example
```yaml
name: Docker Build and Deploy

on:
  push:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Build Docker image
        run: docker-compose build
      
      - name: Run tests
        run: docker-compose run web dotnet test
      
      - name: Push to registry
        run: |
          echo ${{ secrets.DOCKER_PASSWORD }} | docker login -u ${{ secrets.DOCKER_USERNAME }} --password-stdin
          docker-compose push
```

## Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [ASP.NET Core in Docker](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)
- [SQL Server in Docker](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker)
- [Docker Compose Reference](https://docs.docker.com/compose/compose-file/)

## Support

For issues or questions:
1. Check this documentation
2. Review application logs: `docker-compose logs`
3. Check Docker logs: `docker logs <container-name>`
4. Consult project README.md
5. Open an issue in the project repository