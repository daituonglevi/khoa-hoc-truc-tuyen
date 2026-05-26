@echo off
cd C:\Website_Ban_Khoa_Hoc_CNTT

echo ===== Step 1: Remove last migration =====
dotnet ef migrations remove --project ELearningWebsite.csproj
timeout /t 2

echo ===== Step 2: Reset database to initial state =====
dotnet ef database update 0 --project ELearningWebsite.csproj
timeout /t 2

echo ===== Step 3: Append LiveClass SQL to database.sql =====
type LiveClassFeature.sql >> database.sql
echo.
echo ✅ Done! Now execute database.sql in SQL Management Studio
pause
