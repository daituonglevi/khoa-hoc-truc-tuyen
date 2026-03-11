Write-Host "Starting ELearning Website..." -ForegroundColor Green
Write-Host ""

Write-Host "Building project..." -ForegroundColor Yellow
$buildResult = dotnet build ELearningWebsite.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Build successful! Starting server..." -ForegroundColor Green
Write-Host ""
Write-Host "Server will be available at: http://localhost:5000" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

dotnet run --project ELearningWebsite.csproj --urls "http://localhost:5000"
