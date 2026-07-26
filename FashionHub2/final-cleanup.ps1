# Remove EnsureDataSeeded() calls from test constructors

$testFiles = @(
    "FashionHub.Tests/Controllers/ProductsControllerTests.cs",
    "FashionHub.Tests/Controllers/CartControllerTests.cs",
    "FashionHub.Tests/Controllers/AccountControllerTests.cs",
    "FashionHub.Tests/Controllers/OrderControllerTests.cs",
    "FashionHub.Tests/IntegrationTests/ShoppingFlowTests.cs",
    "FashionHub.Tests/Areas/Admin/DashboardControllerTests.cs",
    "FashionHub.Tests/Areas/Admin/ProductsControllerTests.cs"
)

foreach ($file in $testFiles) {
    $path = Join-Path $PSScriptRoot $file
    if (Test-Path $path) {
        Write-Host "Processing: $file"
        $content = Get-Content $path -Raw
        $content = $content -replace '\s+_factory\.EnsureDataSeeded\(\);', ''
        Set-Content $path $content -NoNewline
        Write-Host "  Done"
    } else {
        Write-Host "  Not found: $path"
    }
}

Write-Host "Complete"