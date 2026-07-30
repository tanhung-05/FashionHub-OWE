# Quick Start Guide

## For New Agent Starting Work

### Step 1: Read Main Instructions
```
📖 Read AGENTS.md
```
This file contains:
- Critical rules (evidence-based reporting, no fabrication)
- Architecture decisions (database-first, cookie auth, etc.)
- Current verified status
- Common commands

### Step 2: Understand Project
```
📖 Read .kilo/project-context.md
```
Quick overview:
- Project: ASP.NET Core MVC (.NET 10) e-commerce
- Status: Migration complete, 29/32 tests passing
- Purpose: Portfolio project for internship

### Step 3: Verify Current State
```powershell
cd FashionHub2
dotnet build
dotnet test
```

Expected results:
- Build: ✅ SUCCESS (24 warnings OK)
- Tests: ⚠️ 29/32 PASS (3 failing - known issues)

### Step 4: Check Priority Tasks
```
📖 Read AGENTS.md → "Next Steps" section
```

Current priorities:
1. Fix 3 failing tests (root causes known)
2. Verify security (API key rotation)
3. Run full verification

### Step 5: Choose Your Task

#### If Fixing Tests:
```
📖 Read .kilo/skill/test-fixing.md
Then: cd FashionHub2; dotnet test --logger "console;verbosity=detailed"
```

#### If Adding Feature:
```
📖 Read .kilo/coding-standards.md
📖 Read .kilo/skill/database-migration.md (if DB changes needed)
Then: Follow the workflow
```

#### If Running Security Check:
```
📖 Read .kilo/command/security.md
Then: Execute the PowerShell commands
```

#### If Deploying:
```
📖 Read .kilo/command/deploy.md
📖 Read docs/docker-deployment.md
Then: Follow deployment checklist
```

## Quick Reference

### Essential Commands
```powershell
# Build
cd FashionHub2; dotnet build

# Test
cd FashionHub2; dotnet test

# Test specific
cd FashionHub2; dotnet test --filter "FullyQualifiedName~CartController"

# Security check (secrets)
Select-String -Path "FashionHub2\**\*.cs","FashionHub2\**\*.json" -Pattern "AIzaSy" -SimpleMatch

# Docker build
cd FashionHub2; docker-compose build

# Docker run
cd FashionHub2; docker-compose up -d
```

### File Structure Guide
```
AGENTS.md                          # START HERE
.kilo/
├── project-context.md             # Project overview
├── architecture.md                # Architecture details
├── coding-standards.md            # Code style guide
├── git-workflow.md                # Git conventions
├── testing-guidelines.md          # Test patterns
├── SUMMARY.md                     # Complete overview
├── command/
│   ├── build.md                   # Build command
│   ├── test.md                    # Test command
│   ├── verify.md                  # Full verification
│   ├── security.md                # Security scan
│   └── deploy.md                  # Deployment check
└── skill/
    ├── test-fixing.md             # How to fix tests
    └── database-migration.md      # How to handle DB changes
```

### Critical Rules

1. **Evidence-Based Reporting**
   - All numbers must have command output
   - No fabrication allowed
   - Say "NOT VERIFIED" if unknown

2. **Do NOT Change**
   - Database-first approach
   - Cookie Authentication
   - BCrypt password hashing
   - Gemini API for chat
   - ImageFeatureService (disabled)

3. **PowerShell 5.1**
   - Use `;` not `&&`
   - All scripts compatible

## Common Workflows

### Fix Failing Test
```
1. Read .kilo/skill/test-fixing.md
2. Run: cd FashionHub2; dotnet test --logger "console;verbosity=detailed"
3. Identify root cause (not symptom)
4. Fix the cause
5. Verify: dotnet test --filter "TestName"
6. Run all tests: dotnet test
7. Commit with evidence
```

### Add New Feature
```
1. Plan (read existing code, check DB schema)
2. DB changes if needed (follow .kilo/skill/database-migration.md)
3. Create/modify controller (follow .kilo/coding-standards.md)
4. Create/modify views
5. Add tests (follow .kilo/testing-guidelines.md)
6. Verify: dotnet build && dotnet test
7. Commit (follow .kilo/git-workflow.md)
```

### Security Review
```
1. Read .kilo/command/security.md
2. Run secret scan
3. Run package vulnerability check
4. Review results
5. Fix any issues found
6. Document in commit
```

### Deploy to Production
```
1. Read .kilo/command/deploy.md
2. Run deployment readiness check
3. Fix any blocking issues
4. Build Docker image
5. Test in staging
6. Follow deployment checklist
7. Monitor after deployment
```

## Need Help?

- **General question**: Check AGENTS.md
- **How to code**: Check .kilo/coding-standards.md
- **How to test**: Check .kilo/testing-guidelines.md
- **How to commit**: Check .kilo/git-workflow.md
- **Specific workflow**: Check .kilo/skill/*.md
- **Run command**: Check .kilo/command/*.md
- **Architecture question**: Check .kilo/architecture.md

## Before You Commit

```powershell
# 1. Check status
git status
git diff

# 2. Build
cd FashionHub2; dotnet build

# 3. Test
cd FashionHub2; dotnet test

# 4. Security (if touched secrets)
Select-String -Path "FashionHub2\**\*.cs" -Pattern "AIzaSy|password" -SimpleMatch

# 5. Stage selectively (NOT git add .)
git add FashionHub2/FashionHub.Web/Controllers/SpecificFile.cs

# 6. Commit with proper message
git commit -m "type(scope): description"

# See .kilo/git-workflow.md for commit format
```

## Pro Tips

1. **Always verify** - Run commands yourself, don't trust old reports
2. **Read before acting** - Check relevant .kilo/ files first
3. **Follow patterns** - Match existing code style
4. **Test incrementally** - Test after each logical change
5. **Commit frequently** - Small, focused commits
6. **Include evidence** - Paste actual command output in reports

## Emergency Procedures

### If Build Breaks
```
1. Check error message carefully
2. Review what changed: git diff
3. Undo if needed: git restore <file>
4. Fix and verify: dotnet build
```

### If Tests Fail After Your Change
```
1. Run specific test: dotnet test --filter "TestName"
2. Read error message and expected vs actual
3. Check if test is wrong or code is wrong
4. Fix root cause
5. Verify: dotnet test
```

### If You Committed Secret
```
1. IMMEDIATELY rotate the secret
2. Remove from code
3. If not pushed: git reset --soft HEAD~1
4. If pushed: coordinate with team, force push may be needed
5. Document incident
```

## Ready to Start?

1. ✅ Read this guide
2. ✅ Read AGENTS.md
3. ✅ Verify current state (build + test)
4. ✅ Choose priority task
5. ✅ Read relevant .kilo/ files
6. ✅ Start working!

**Remember**: Evidence-based reporting, no fabrication, follow immutable architecture decisions.
