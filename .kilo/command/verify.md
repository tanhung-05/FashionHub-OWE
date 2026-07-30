# Verify Command

Run comprehensive verification: build + test + security checks.

## Usage
```
/verify
```

## What It Does
1. Runs full build
2. Runs all tests
3. Checks for hardcoded secrets
4. Checks for TODO/FIXME comments
5. Validates Docker configuration
6. Reports comprehensive status

## Expected Output
```
============================================
FashionHub Verification Report
============================================

✅ Build: SUCCESS (0 errors, 24 warnings)
✅ Tests: 29/32 PASSED (90.6%)
✅ Security: No secrets found
⚠️  TODOs: 5 found
✅ Docker: Configuration valid

============================================
Detailed Results
============================================

Build Warnings (24):
- 13x CA1416: ImageFeatureService (intentional - disabled)
- 8x CS8602: Nullable reference warnings
- 2x CS0168: Unused exception variables
- 1x CS8629: Nullable value type

Test Failures (3):
1. AccountControllerTests.Register_Get_ReturnsRegisterPage
2. CartControllerTests.GetCartCount_ReturnsCorrectCount
3. ShoppingFlowTests.CartManagement_AddUpdateRemove

TODOs Found (5):
- FashionHub.Web/Controllers/ProductsController.cs:45
- FashionHub.Web/Services/ChatAiService.cs:120
- FashionHub.Tests/Controllers/OrderControllerTests.cs:88
- [See full list in output]

============================================
Deployment Readiness
============================================

❌ NOT READY FOR DEPLOYMENT

Blocking Issues:
- 3 tests failing - must fix before deployment

Recommendations:
1. Fix failing tests (see .kilo/skill/test-fixing.md)
2. Review and resolve TODOs
3. Run /verify again before deployment
```

## Implementation
```powershell
Write-Output "============================================"
Write-Output "FashionHub Verification Report"
Write-Output "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Output "============================================`n"

# 1. Build check
Write-Output "🔨 Running build..."
cd FashionHub2
$buildOutput = dotnet build 2>&1 | Out-String
$buildSuccess = $buildOutput -match "Build succeeded"
$errorCount = ([regex]::Matches($buildOutput, " error ")).Count
$warningCount = ([regex]::Matches($buildOutput, " warning ")).Count

if ($buildSuccess) {
    Write-Output "✅ Build: SUCCESS (0 errors, $warningCount warnings)"
} else {
    Write-Output "❌ Build: FAILED ($errorCount errors)"
    Write-Output $buildOutput
    exit 1
}

# 2. Test check
Write-Output "`n🧪 Running tests..."
$testOutput = dotnet test --logger "console;verbosity=minimal" 2>&1 | Out-String
if ($testOutput -match "Passed:\s+(\d+).*Failed:\s+(\d+).*Total:\s+(\d+)") {
    $passed = [int]$matches[1]
    $failed = [int]$matches[2]
    $total = [int]$matches[3]
    $successRate = [math]::Round(($passed / $total) * 100, 1)
    
    if ($failed -eq 0) {
        Write-Output "✅ Tests: $passed/$total PASSED (100%)"
    } else {
        Write-Output "⚠️  Tests: $passed/$total PASSED ($successRate%)"
    }
}

# 3. Security check
Write-Output "`n🔒 Checking for secrets..."
cd ..
$secrets = Select-String -Path "FashionHub2\**\*.cs","FashionHub2\**\*.json" -Pattern "AIzaSy|password.*=.*['\`"][^'\`"]+['\`"]" -SimpleMatch -ErrorAction SilentlyContinue
if ($secrets) {
    Write-Output "❌ Security: Potential secrets found!"
    Write-Output $secrets
} else {
    Write-Output "✅ Security: No hardcoded secrets detected"
}

# 4. TODO check
Write-Output "`n📝 Checking for TODOs..."
$todos = Select-String -Path "FashionHub2\FashionHub.Web\**\*.cs" -Pattern "TODO|FIXME|HACK" -ErrorAction SilentlyContinue
$todoCount = if ($todos) { $todos.Count } else { 0 }
if ($todoCount -gt 0) {
    Write-Output "⚠️  TODOs: $todoCount found"
    $todos | Select-Object -First 5 | ForEach-Object {
        Write-Output "   - $($_.Filename):$($_.LineNumber)"
    }
    if ($todoCount -gt 5) {
        Write-Output "   ... and $($todoCount - 5) more"
    }
} else {
    Write-Output "✅ TODOs: None found"
}

# 5. Docker check
Write-Output "`n🐳 Validating Docker configuration..."
if (Test-Path "FashionHub2/docker-compose.yml") {
    Write-Output "✅ Docker: docker-compose.yml exists"
    if (Test-Path "FashionHub2/.env.example") {
        Write-Output "✅ Docker: .env.example exists"
    } else {
        Write-Output "⚠️  Docker: .env.example not found"
    }
} else {
    Write-Output "❌ Docker: docker-compose.yml not found"
}

# Final verdict
Write-Output "`n============================================"
Write-Output "Deployment Readiness"
Write-Output "============================================"

$blocking = @()
if (!$buildSuccess) { $blocking += "Build failed" }
if ($failed -gt 0) { $blocking += "$failed tests failing" }
if ($secrets) { $blocking += "Hardcoded secrets found" }

if ($blocking.Count -eq 0) {
    Write-Output "✅ READY FOR DEPLOYMENT"
} else {
    Write-Output "❌ NOT READY FOR DEPLOYMENT`n"
    Write-Output "Blocking Issues:"
    $blocking | ForEach-Object { Write-Output "- $_" }
}

Write-Output "`nCompleted: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
```

## When to Use
- Before creating PR
- Before deployment
- Weekly project health check
- After major refactoring

## Success Criteria
- Build succeeds
- All tests pass
- No secrets found
- Docker config valid
- Ready for deployment
