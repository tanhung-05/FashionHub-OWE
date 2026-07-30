# FashionHub - Agent Instructions

## Project Overview
FashionHub e-commerce platform - ASP.NET Core MVC (.NET 10) migration from .NET Framework 4.8.
Portfolio project for internship application.

## Critical Rules

### 1. Evidence-Based Reporting
**MANDATORY**: All numbers, statistics, and status claims MUST include the actual command output that produced them.

❌ **WRONG**:
```
Build: SUCCESS with 24 warnings
Tests: 29/32 passing
```

✅ **CORRECT**:
```
Build: SUCCESS with 24 warnings

Command: cd FashionHub2; dotnet build
Output:
  Build succeeded.
  24 Warning(s)
  0 Error(s)
  [actual output here]

Tests: 29/32 passing

Command: cd FashionHub2; dotnet test
Output:
  Passed: 29, Failed: 3, Skipped: 0, Total: 32
  [actual output here]
```

### 2. No Fabrication
- NEVER invent file contents, test results, or error messages
- If you don't have output, run the command to get it
- If you can't run the command, say "NOT VERIFIED" explicitly
- Don't fill lists with "implied" or "similar to" placeholders

### 3. Verify Before Claiming
Before saying something is done:
```powershell
# Controllers count
Get-ChildItem -Path "FashionHub2\FashionHub.Web\Controllers" -File | Measure-Object

# Tests status
cd FashionHub2; dotnet test --logger "console;verbosity=minimal"

# Check if ImageFeatureService is used
Select-String -Path "FashionHub2\FashionHub.Web\**\*.cs" -Pattern "ImageFeatureService" -SimpleMatch
```

### 4. Test Fixing Protocol
When tests fail:
1. Get ACTUAL error message and expected vs actual values
2. Investigate root cause (code wrong? test wrong? data wrong?)
3. Fix root cause, not symptoms
4. Verify fix with: `dotnet test --filter "FullyQualifiedName~TestName"`
5. Run all tests to check for regression

### 5. Architecture Decisions - DO NOT CHANGE
- Database-first (scaffold from SQL Server)
- Cookie Authentication (NOT JWT)
- BCrypt password hashing
- Gemini API for chat (NOT ONNX/ML.NET)
- ImageFeatureService is DISABLED (Windows-only, waiting for ImageSharp)

## Commands Available

### Build & Test
```powershell
# Build
cd FashionHub2; dotnet build

# Test all
cd FashionHub2; dotnet test

# Test specific
cd FashionHub2; dotnet test --filter "FullyQualifiedName~CartController"

# Test with details
cd FashionHub2; dotnet test --logger "console;verbosity=detailed"
```

### Security Checks
```powershell
# Check for secrets (Gemini API keys)
Select-String -Path "FashionHub2\**\*.cs","FashionHub2\**\*.json" -Pattern "AIzaSy" -SimpleMatch

# Check for hardcoded passwords
Select-String -Path "FashionHub2\**\*.cs" -Pattern 'password.*=.*"[^"]+' -ErrorAction SilentlyContinue

# Check package vulnerabilities
cd FashionHub2/FashionHub.Web; dotnet list package --vulnerable
```

### Docker
```powershell
# Build image
cd FashionHub2; docker-compose build

# Start services
cd FashionHub2; docker-compose up -d

# Check logs
docker-compose logs -f web

# Health check
curl http://localhost:5167/health
```

### Git
```powershell
# Status
git status

# Commit count
git log --oneline --all | Measure-Object -Line

# Check for secrets in history
git log -p | Select-String "AIzaSy"
```

## Current Status (Last Verified: 2026-07-29)

### Build
- **Status**: ✅ SUCCESS
- **Errors**: 0
- **Warnings**: 24 (13 CA1416 ImageFeatureService, 8 CS8602 nullable, 2 CS0168 unused var, 1 CS8629 nullable value)

### Tests
- **Status**: ⚠️ 29/32 PASS (90.6%)
- **Failing**: 3 tests
  1. AccountControllerTests.Register_Get_ReturnsRegisterPage (UI text mismatch)
  2. CartControllerTests.GetCartCount_ReturnsCorrectCount (JSON parsing issue)
  3. ShoppingFlowTests.CartManagement_AddUpdateRemove (JSON parsing issue)

### Controllers
- **Customer**: 6 (Account, Cart, Chat, Home, Order, Products)
- **Admin**: 7 (Categories, Coupons, Dashboard, Orders, Products, Reports, Users)
- **Total**: 13

### Git
- **Commits**: 37
- **Tags**: v1.0.0

### Docker
- **Status**: ✅ Ready
- **Files**: docker-compose.yml, Dockerfile, .env.example

## Workflow for New Features

### 1. Planning Phase
```markdown
1. Read relevant existing code
2. Check database schema
3. Identify affected controllers/views
4. Plan service layer if needed
5. Estimate test coverage needed
```

### 2. Implementation Phase
```markdown
1. Database changes (if needed)
   - Create SQL script
   - Apply to dev database
   - Re-scaffold models: dotnet ef dbcontext scaffold

2. Create/modify controller
   - Follow coding-standards.md
   - Add [Authorize] if needed
   - Include navigation properties in queries

3. Create/modify views
   - Use partials for reusable components
   - Add antiforgery tokens to forms
   - Follow UI patterns from existing views

4. Create tests
   - Unit tests for controller actions
   - Integration tests for flows
   - Follow testing-guidelines.md
```

### 3. Verification Phase
```powershell
# Build
cd FashionHub2; dotnet build

# Test
cd FashionHub2; dotnet test

# Security check
Select-String -Path "FashionHub2\**\*.cs" -Pattern "TODO|FIXME" -ErrorAction SilentlyContinue

# Manual test in browser
cd FashionHub2/FashionHub.Web; dotnet run
# Navigate to http://localhost:5167 and test feature
```

### 4. Commit Phase
```powershell
# Review changes
git status
git diff

# Stage selectively
git add FashionHub2/FashionHub.Web/Controllers/WishlistController.cs
git add FashionHub2/FashionHub.Web/Views/Wishlist/

# Commit with proper message
git commit -m "feat(wishlist): add wishlist functionality

- Add WishlistController with CRUD operations
- Create wishlist views
- Add tests for wishlist feature
- Update navigation menu

Closes #123"
```

## Common Tasks

### Fix Failing Tests
See `.kilo/skill/test-fixing.md`

### Add New Feature
See `.kilo/skill/database-migration.md` for DB changes
See `.kilo/coding-standards.md` for code style
See `.kilo/testing-guidelines.md` for tests

### Security Review
See `.kilo/command/security.md`

### Deploy
See `.kilo/command/deploy.md`
See `docs/docker-deployment.md`

## Known Issues & Workarounds

### ImageFeatureService Warnings
- **Issue**: 13 CA1416 warnings (Windows-only APIs)
- **Status**: INTENTIONAL - service is disabled, not used
- **Action**: No action needed, planned for ImageSharp refactor

### Nullable Reference Warnings
- **Issue**: 8 CS8602 warnings in views/controllers
- **Status**: Non-critical, low priority
- **Action**: Fix during refactoring sprint

### 3 Failing Tests
- **Issue**: Text mismatch + JSON parsing
- **Status**: Root cause identified, fix pending
- **Action**: Update test assertions + add JSON parsing

## Environment

### Development
- **OS**: Windows
- **Shell**: PowerShell 5.1 (use `;` not `&&` for chaining)
- **.NET**: 10.0
- **Database**: SQL Server (local or Docker)

### User Secrets Setup
```powershell
cd FashionHub2/FashionHub.Web
dotnet user-secrets set "GeminiAI:ApiKey" "your_key_here"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=FashionHub;..."
```

### Docker Setup
```powershell
cd FashionHub2
cp .env.example .env
# Edit .env with your values
docker-compose up -d
```

## Lint & Typecheck Commands

**IMPORTANT**: When asked to run lint/typecheck:

```powershell
# There are NO specific lint/typecheck commands configured
# Use build as the primary validation:
cd FashionHub2; dotnet build

# For code analysis:
cd FashionHub2; dotnet build /p:TreatWarningsAsErrors=false

# For security:
cd FashionHub2/FashionHub.Web; dotnet list package --vulnerable
```

If asked to write these to AGENTS.md, respond that:
- **Build validation**: `cd FashionHub2; dotnet build`
- **Tests**: `cd FashionHub2; dotnet test`
- **Security scan**: See `.kilo/command/security.md`

## References

### Configuration Files
- `.kilo/project-context.md` - Project overview
- `.kilo/architecture.md` - Architecture decisions
- `.kilo/coding-standards.md` - Code style
- `.kilo/git-workflow.md` - Git conventions
- `.kilo/testing-guidelines.md` - Test patterns

### Documentation
- `docs/production-readiness-report.md` - Production status
- `docs/docker-deployment.md` - Docker guide
- `docs/gemini-api-key-setup.md` - Secrets management

### Key Files
- `FashionHub2/FashionHub.Web/Program.cs` - App configuration
- `FashionHub2/FashionHub.Web/Data/ApplicationDbContext.cs` - Database context
- `FashionHub2/docker-compose.yml` - Docker setup
