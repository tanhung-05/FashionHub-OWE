# Test Command

Run all tests and report detailed results.

## Usage
```
/test [filter]
```

## Examples
```
/test                           # Run all tests
/test cart                      # Run cart-related tests only
/test AccountControllerTests    # Run specific test class
```

## What It Does
1. Navigates to FashionHub2 directory
2. Runs `dotnet test` with detailed output
3. Reports pass/fail counts
4. Lists all failing tests with error details
5. Calculates test coverage percentage

## Expected Output
```
Test Results: 29 Passed, 3 Failed, 0 Skipped
Total: 32 tests
Success Rate: 90.6%

❌ Failing Tests:

1. AccountControllerTests.Register_Get_ReturnsRegisterPage
   Expected: content to contain "Đăng ký"
   Actual: Content has "Tạo tài khoản"
   Location: AccountControllerTests.cs:29

2. CartControllerTests.GetCartCount_ReturnsCorrectCount
   Expected: count to contain "2"
   Actual: {"success":true,"count":0}
   Location: CartControllerTests.cs:133

3. ShoppingFlowTests.CartManagement_AddUpdateRemove
   Expected: count to contain "3"
   Actual: {"success":true,"count":0}
   Location: ShoppingFlowTests.cs:102

Recommended Actions:
- Fix failing tests before committing
- See .kilo/skill/test-fixing.md for guidance
```

## Implementation
```powershell
cd FashionHub2

if ($args.Count -gt 0) {
    $filter = $args[0]
    $output = dotnet test --filter "FullyQualifiedName~$filter" --logger "console;verbosity=detailed" 2>&1 | Out-String
} else {
    $output = dotnet test --logger "console;verbosity=detailed" 2>&1 | Out-String
}

Write-Output $output

# Parse results
if ($output -match "Passed:\s+(\d+).*Failed:\s+(\d+).*Skipped:\s+(\d+).*Total:\s+(\d+)") {
    $passed = [int]$matches[1]
    $failed = [int]$matches[2]
    $skipped = [int]$matches[3]
    $total = [int]$matches[4]
    $successRate = [math]::Round(($passed / $total) * 100, 1)
    
    Write-Output "`n============================================"
    Write-Output "Test Summary"
    Write-Output "============================================"
    Write-Output "✅ Passed: $passed"
    Write-Output "❌ Failed: $failed"
    Write-Output "⏭️  Skipped: $skipped"
    Write-Output "📊 Total: $total"
    Write-Output "📈 Success Rate: $successRate%"
    Write-Output "============================================"
    
    if ($failed -eq 0) {
        Write-Output "`n🎉 All tests passed!"
    } else {
        Write-Output "`n⚠️  $failed test(s) failing. Review output above for details."
        Write-Output "See .kilo/skill/test-fixing.md for troubleshooting guidance."
    }
}
```

## When to Use
- After code changes
- Before committing
- During debugging
- After fixing tests

## Success Criteria
- All tests pass (0 failures)
- No skipped tests (unless intentional)
- Test execution time < 30 seconds
