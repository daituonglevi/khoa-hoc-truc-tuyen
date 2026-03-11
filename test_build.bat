@echo off
echo Testing build...
dotnet build --verbosity normal
echo Build completed with exit code: %ERRORLEVEL%
pause
