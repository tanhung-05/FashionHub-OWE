# Deploy Command

Build Docker image and verify deployment readiness.

## Usage
```
/deploy [check|build]
```

## Examples
```
/deploy check    # Check deployment readiness only
/deploy build    # Build Docker image
/deploy          # Full check + build
```

## What It Does

### Check Mode
1. Runs /verify command
2. Validates environment configuration
3. Checks .env.example exists
4. Verifies Dockerfile and docker-compose.yml
5. Reports deployment readiness

### Build Mode
1. Runs check first
2. Builds Docker image
3. Validates image was created
4. Reports image size and tags

## Expected Output

### Check Mode
```
============================================
Deployment Readiness Check
============================================

✅ Build: SUCCESS
✅ Tests: 32/32 PASSED
✅ Security: No secrets detected
✅ Docker Files: Present
✅ Environment: .env.example exists

⚠️  Pre-Deployment Checklist:
- [ ] Update .env with production values
- [ ] Set GEMINI_API_KEY environment variable
- [ ] Set SA_PASSWORD for SQL Server
- [ ] Review ConnectionStrings in docker-compose.yml
- [ ] Backup existing database (if applicable)

Ready to build Docker image.
```

### Build Mode
```
============================================
Building Docker Image
============================================

Building for platform: linux/amd64
Context: FashionHub2
Dockerfile: FashionHub.Web/Dockerfile

[Docker build output...]

✅ Image built successfully
Image: fashionhub-web:latest
Size: 245 MB

Next Steps:
1. Start services:
   docker-compose up -d

2. Check health:
   curl http://localhost:5167/health

3. View logs:
   docker-compose logs -f web

4. Access application:
   http://localhost:5167
```

## Implementation
```powershell
param(
    [string]$mode = "full"
)

function Check-Deployment {
    Write-Output "============================================"
    Write-Output "Deployment Readiness Check"
    Write-Output "============================================`n"
    
    # Run verification
    & .kilo/command/verify.md
    
    # Check Docker files
    Write-Output "`n🐳 Checking Docker configuration..."
    
    $dockerFiles = @{
        "docker-compose.yml" = Test-Path "FashionHub2/docker-compose.yml"
        "Dockerfile" = Test-Path "FashionHub2/FashionHub.Web/Dockerfile"
        ".env.example" = Test-Path "FashionHub2/.env.example"
        ".dockerignore" = Test-Path "FashionHub2/.dockerignore"
    }
    
    $allPresent = $true
    foreach ($file in $dockerFiles.Keys) {
        if ($dockerFiles[$file]) {
            Write-Output "✅ $file exists"
        } else {
            Write-Output "❌ $file missing"
            $allPresent = $false
        }
    }
    
    # Check .env
    Write-Output "`n⚙️  Environment Configuration..."
    if (Test-Path "FashionHub2/.env") {
        Write-Output "✅ .env file exists"
        Write-Output "⚠️  Ensure .env has production values (not committed to git)"
    } else {
        Write-Output "⚠️  .env file not found"
        Write-Output "   Copy .env.example to .env and configure:"
        Write-Output "   - SA_PASSWORD"
        Write-Output "   - GEMINI_API_KEY"
    }
    
    Write-Output "`n============================================"
    Write-Output "Pre-Deployment Checklist"
    Write-Output "============================================"
    Write-Output "- [ ] .env configured with production values"
    Write-Output "- [ ] GEMINI_API_KEY set"
    Write-Output "- [ ] SA_PASSWORD set (min 8 chars, complex)"
    Write-Output "- [ ] Database backup completed (if applicable)"
    Write-Output "- [ ] All tests passing"
    Write-Output "- [ ] No hardcoded secrets"
    
    return $allPresent
}

function Build-DockerImage {
    Write-Output "`n============================================"
    Write-Output "Building Docker Image"
    Write-Output "============================================`n"
    
    cd FashionHub2
    
    Write-Output "Building fashionhub-web image..."
    $buildOutput = docker-compose build 2>&1 | Out-String
    Write-Output $buildOutput
    
    if ($LASTEXITCODE -eq 0) {
        Write-Output "`n✅ Image built successfully"
        
        # Get image info
        $imageInfo = docker images fashionhub2-web --format "{{.Size}}" 2>&1
        Write-Output "Image: fashionhub2-web:latest"
        Write-Output "Size: $imageInfo"
        
        Write-Output "`n============================================"
        Write-Output "Next Steps"
        Write-Output "============================================"
        Write-Output "1. Start services:"
        Write-Output "   cd FashionHub2"
        Write-Output "   docker-compose up -d"
        Write-Output ""
        Write-Output "2. Check health:"
        Write-Output "   curl http://localhost:5167/health"
        Write-Output ""
        Write-Output "3. View logs:"
        Write-Output "   docker-compose logs -f web"
        Write-Output ""
        Write-Output "4. Access application:"
        Write-Output "   http://localhost:5167"
        
        return $true
    } else {
        Write-Output "`n❌ Docker build failed"
        Write-Output "Review the output above for errors."
        return $false
    }
}

# Main execution
switch ($mode.ToLower()) {
    "check" {
        Check-Deployment
    }
    "build" {
        if (Check-Deployment) {
            Build-DockerImage
        } else {
            Write-Output "`n❌ Deployment check failed. Fix issues before building."
        }
    }
    default {
        if (Check-Deployment) {
            $response = Read-Host "`nProceed with Docker build? (y/n)"
            if ($response -eq 'y') {
                Build-DockerImage
            }
        }
    }
}
```

## When to Use
- Before deploying to production
- After major changes
- Setting up new environment
- Testing Docker configuration

## Success Criteria
- All checks pass
- Docker image builds successfully
- Image size reasonable (< 500MB)
- Services start successfully
- Health check responds

## Related Files
- `docs/docker-deployment.md` - Full deployment guide
- `FashionHub2/docker-compose.yml` - Docker configuration
- `FashionHub2/.env.example` - Environment template
