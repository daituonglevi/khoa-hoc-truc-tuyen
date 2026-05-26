@echo off
chcp 65001 >nul
cd C:\Website_Ban_Khoa_Hoc_CNTT

REM Step 1: Build project
echo ===== Step 1: Building project =====
dotnet build ELearningWebsite.csproj
if %errorlevel% neq 0 (
    echo ❌ Build failed! Check errors above.
    pause
    exit /b 1
)
echo ✅ Build success!
echo.

REM Step 2: Append LiveClass SQL to database.sql
echo ===== Step 2: Appending LiveClass SQL to database.sql =====
if exist LiveClassFeature.sql (
    type LiveClassFeature.sql >> database.sql
    echo ✅ LiveClass SQL appended!
    del /q LiveClassFeature.sql
) else (
    echo ⚠️  LiveClassFeature.sql not found, skipping...
)
echo.

REM Step 3: Show next steps
echo ===== ✅ Complete! Next steps: =====
echo 1. Open database.sql in SQL Management Studio (or Azure Portal)
echo 2. Execute the entire script on Azure SQL Database
echo 3. Your system will have all tables (existing + LiveClass)
echo.
echo Then run: dotnet run
pause
