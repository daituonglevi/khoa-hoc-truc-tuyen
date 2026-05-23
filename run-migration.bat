@echo off
REM Run database migration and build

cd C:\Website_Ban_Khoa_Hoc_CNTT

echo ===== Creating Database Migration =====
dotnet ef migrations add AddLiveClassPhase4Hangfire

echo.
echo ===== Updating Database =====
dotnet ef database update

echo.
echo ===== Building Project =====
dotnet build

echo.
echo ===== DONE! =====
echo You can now run: dotnet run
pause
