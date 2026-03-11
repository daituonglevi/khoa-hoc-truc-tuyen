@echo off
echo Starting ELearning CNTT Website...
echo.

echo Restoring packages...
dotnet restore

echo.
echo Building project...
dotnet build

echo.
echo Starting application...
echo Website will be available at:
echo - HTTP: http://localhost:5000
echo - HTTPS: https://localhost:5001
echo - Admin: https://localhost:5001/Admin
echo.
echo Press Ctrl+C to stop the application
echo.

dotnet run
