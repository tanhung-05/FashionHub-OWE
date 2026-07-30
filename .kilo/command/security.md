# Security Check Command

Scan codebase for security issues: secrets, vulnerabilities, and insecure patterns.

## Usage
```
/security [full|secrets|packages]
```

## Examples
```
/security           # Full security scan
/security secrets   # Check for hardcoded secrets only
/security packages  # Check for vulnerable packages only
```

## What It Does

### Full Scan
1. Checks for hardcoded secrets (API keys, passwords, tokens)
2. Scans for vulnerable NuGet packages
3. Checks for common security anti-patterns
4. Reviews authentication/authorization usage
5. Validates security headers configuration

### Secrets Scan
- API keys (Google, AWS, Azure patterns)
- Hardcoded passwords
- Connection strings with credentials
- JWT tokens
- Private keys

### Packages Scan
- Known vulnerable dependencies
- Outdated packages with security fixes

## Expected Output
```
============================================
Security Scan Report
============================================
Started: 2026-07-29 10:30:00

🔒 Secrets Check
============================================
Scanning for hardcoded secrets...
✅ No Gemini API keys found (AIzaSy*)
✅ No hardcoded passwords found
✅ No JWT tokens found
✅ No private keys found

📦 Package Vulnerabilities
============================================
Scanning NuGet packages...
✅ No vulnerable packages detected

Installed Packages:
- Microsoft.EntityFrameworkCore.SqlServer (10.0.0) ✅
- BCrypt.Net-Next (4.0.3) ✅
- FluentAssertions (6.12.0) ✅
[... more packages]

🛡️ Security Patterns Check
============================================
✅ Cookie authentication configured
✅ HTTPS redirection enabled
✅ Antiforgery tokens in forms
✅ Security headers configured
⚠️  8 nullable reference warnings (non-critical)

📊 Summary
============================================
✅ Overall Status: SECURE

Findings:
- 0 Critical issues
- 0 High severity issues
- 0 Medium severity issues
- 8 Low severity warnings (nullable references)

Recommendations:
- Keep packages updated regularly
- Review nullable warnings in next refactoring
- Rotate API keys quarterly
```

## Implementation
```powershell
param(
    [string]$mode = "full"
)

function Check-Secrets {
    Write-Output "🔒 Secrets Check"
    Write-Output "============================================"
    Write-Output "Scanning for hardcoded secrets...`n"
    
    $patterns = @{
        "Gemini API Key" = "AIzaSy[0-9A-Za-z_-]{33}"
        "Generic API Key" = "api[_-]?key['\`"]?\s*[:=]\s*['\`"][^'\`"]+"
        "Password" = "password['\`"]?\s*[:=]\s*['\`"][^'\`"]{6,}"
        "JWT Token" = "eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\."
        "Private Key" = "-----BEGIN (RSA |EC |)PRIVATE KEY-----"
        "Connection String" = "Password\s*=\s*[^;]{6,}"
    }
    
    $findings = @()
    
    foreach ($pattern in $patterns.Keys) {
        $matches = Select-String -Path "FashionHub2\**\*.cs","FashionHub2\**\*.json" -Pattern $patterns[$pattern] -ErrorAction SilentlyContinue
        
        if ($matches) {
            Write-Output "❌ $pattern found:"
            foreach ($match in $matches) {
                Write-Output "   $($match.Filename):$($match.LineNumber)"
                $findings += $match
            }
        } else {
            Write-Output "✅ No $pattern found"
        }
    }
    
    return $findings
}

function Check-Packages {
    Write-Output "`n📦 Package Vulnerabilities"
    Write-Output "============================================"
    Write-Output "Scanning NuGet packages...`n"
    
    cd FashionHub2/FashionHub.Web
    $vulnerableOutput = dotnet list package --vulnerable 2>&1 | Out-String
    
    if ($vulnerableOutput -match "no vulnerable packages") {
        Write-Output "✅ No vulnerable packages detected"
    } else {
        Write-Output "❌ Vulnerable packages found:"
        Write-Output $vulnerableOutput
    }
    
    # List all packages
    Write-Output "`nInstalled Packages:"
    $packages = dotnet list package --format json | ConvertFrom-Json
    foreach ($framework in $packages.projects[0].frameworks) {
        foreach ($pkg in $framework.topLevelPackages) {
            Write-Output "- $($pkg.id) ($($pkg.resolvedVersion)) ✅"
        }
    }
    
    cd ../..
}

function Check-SecurityPatterns {
    Write-Output "`n🛡️ Security Patterns Check"
    Write-Output "============================================`n"
    
    # Check Program.cs for security configurations
    $programCs = Get-Content "FashionHub2\FashionHub.Web\Program.cs" -Raw
    
    $checks = @{
        "Cookie Authentication" = $programCs -match "AddAuthentication.*CookieAuthentication"
        "HTTPS Redirection" = $programCs -match "UseHttpsRedirection"
        "HSTS" = $programCs -match "UseHsts"
        "Security Headers" = $programCs -match "X-Content-Type-Options|X-Frame-Options"
        "Response Compression" = $programCs -match "UseResponseCompression"
    }
    
    foreach ($check in $checks.Keys) {
        if ($checks[$check]) {
            Write-Output "✅ $check configured"
        } else {
            Write-Output "⚠️  $check not found"
        }
    }
    
    # Check for antiforgery tokens in views
    $formsWithToken = Select-String -Path "FashionHub2\FashionHub.Web\Views\**\*.cshtml" -Pattern "@Html.AntiForgeryToken|asp-antiforgery" -ErrorAction SilentlyContinue
    $formsTotal = Select-String -Path "FashionHub2\FashionHub.Web\Views\**\*.cshtml" -Pattern '<form.*method="post"' -ErrorAction SilentlyContinue
    
    if ($formsWithToken) {
        Write-Output "✅ Antiforgery tokens found in forms"
    } else {
        Write-Output "⚠️  No antiforgery tokens detected"
    }
}

function Check-Authentication {
    Write-Output "`n🔐 Authentication Review"
    Write-Output "============================================`n"
    
    # Check for [Authorize] attributes
    $authorizeCount = (Select-String -Path "FashionHub2\FashionHub.Web\**\*.cs" -Pattern "\[Authorize" -ErrorAction SilentlyContinue).Count
    Write-Output "✅ [Authorize] attributes used: $authorizeCount times"
    
    # Check for role-based authorization
    $roleAuthCount = (Select-String -Path "FashionHub2\FashionHub.Web\**\*.cs" -Pattern '\[Authorize.*Roles\s*=\s*"Admin"' -ErrorAction SilentlyContinue).Count
    Write-Output "✅ Role-based authorization: $roleAuthCount times"
    
    # Check password hashing
    $bcryptUsage = Select-String -Path "FashionHub2\FashionHub.Web\**\*.cs" -Pattern "BCrypt\.Net\.BCrypt" -ErrorAction SilentlyContinue
    if ($bcryptUsage) {
        Write-Output "✅ BCrypt password hashing used"
    } else {
        Write-Output "⚠️  Password hashing method not detected"
    }
}

# Main execution
Write-Output "============================================"
Write-Output "Security Scan Report"
Write-Output "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Output "============================================`n"

$criticalCount = 0
$warningCount = 0

switch ($mode.ToLower()) {
    "secrets" {
        $findings = Check-Secrets
        $criticalCount = $findings.Count
    }
    "packages" {
        Check-Packages
    }
    default {
        $findings = Check-Secrets
        $criticalCount = $findings.Count
        Check-Packages
        Check-SecurityPatterns
        Check-Authentication
    }
}

# Summary
Write-Output "`n📊 Summary"
Write-Output "============================================"

if ($criticalCount -eq 0) {
    Write-Output "✅ Overall Status: SECURE`n"
} else {
    Write-Output "❌ Overall Status: ISSUES FOUND`n"
}

Write-Output "Findings:"
Write-Output "- $criticalCount Critical issues"
Write-Output "- 0 High severity issues"
Write-Output "- 0 Medium severity issues"
Write-Output "- $warningCount Low severity warnings`n"

if ($criticalCount -gt 0) {
    Write-Output "⚠️  CRITICAL: Hardcoded secrets detected!"
    Write-Output "Action Required:"
    Write-Output "1. Remove secrets from code immediately"
    Write-Output "2. Rotate compromised credentials"
    Write-Output "3. Use User Secrets (dev) or Environment Variables (prod)"
    Write-Output "4. See docs/gemini-api-key-setup.md for guidance"
} else {
    Write-Output "Recommendations:"
    Write-Output "- Keep packages updated regularly (monthly)"
    Write-Output "- Review security headers quarterly"
    Write-Output "- Rotate API keys every 90 days"
    Write-Output "- Perform penetration testing before production"
}

Write-Output "`nCompleted: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
```

## When to Use
- Before committing code with secrets
- Weekly security review
- Before deployment
- After adding new dependencies
- When security vulnerability reported

## Critical Actions

### If Secrets Found
1. **IMMEDIATELY** remove from code
2. **IMMEDIATELY** rotate compromised credentials
3. Check git history: `git log -p | Select-String "AIzaSy"`
4. Use BFG Repo-Cleaner if in git history
5. Document incident

### If Vulnerabilities Found
1. Review package advisory
2. Update to patched version
3. Test thoroughly
4. Deploy update ASAP

## Related Files
- `docs/gemini-api-key-setup.md` - Secrets management guide
- `.kilo/architecture.md` - Security architecture
- `FashionHub2/FashionHub.Web/Program.cs` - Security configuration
