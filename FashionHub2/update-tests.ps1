# Update remaining test constructors
$files = @(
    'FashionHub2/FashionHub.Tests/Controllers/OrderControllerTests.cs',
    'FashionHub2/FashionHub.Tests/Controllers/AccountControllerTests.cs',
    'FashionHub2/FashionHub.Tests/Areas/Admin/DashboardControllerTests.cs',
    'FashionHub2/FashionHub.Tests/Areas/Admin/ProductsControllerTests.cs'
)

foreach ($file in $files) {
    $content = Get-Content $file -Raw
    
    # Add _factory field
    $content = $content -replace '(private readonly HttpClient _client;)', '$1`r`n    private readonly CustomWebApplicationFactory<Program> _factory;'
    
    # Update constructor
    $content = $content -replace '(public \w+ControllerTests\(CustomWebApplicationFactory<Program> factory\)\s+\{)\s+(_client = factory\.CreateClient)', '$1`r`n        _factory = factory;`r`n        _factory.SeedData();`r`n        $2'
    
    Set-Content $file $content -NoNewline
    Write-Host "Updated $file"
}

Write-Host "Done updating test files"