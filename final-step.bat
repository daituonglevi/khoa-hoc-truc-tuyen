@echo off
chcp 65001 >nul
cd C:\Website_Ban_Khoa_Hoc_CNTT

echo ===== Building project =====
dotnet build ELearningWebsite.csproj

if %errorlevel% equ 0 (
    echo.
    echo ✅ Build SUCCESS!
    echo.
    echo ===== Next Step: Execute database.sql =====
    echo.
    echo 1. Open SQL Management Studio
    echo 2. Open: C:\Website_Ban_Khoa_Hoc_CNTT\database.sql
    echo 3. Connect to your Azure SQL Database (or local SQL Server)
    echo 4. Click Execute or press F5
    echo.
    echo After database.sql finishes, run:
    echo    dotnet run
    echo.
) else (
    echo.
    echo ❌ Build FAILED! Fix errors above.
)

pause
