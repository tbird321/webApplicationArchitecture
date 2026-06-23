@echo off
REM Run the publishing assistant from this folder so relative paths resolve.
cd /d "%~dp0"
dotnet run -- %*
