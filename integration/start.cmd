@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET SDK not found on PATH. Install .NET 8 SDK from https://dotnet.microsoft.com/download
  pause
  exit /b 1
)

:sitemenu
echo(
echo ===================================================
echo   Choose a site:
echo ===================================================
echo   1. LDS Apologetics   (posts.json)
echo   2. LDS Doctrines     (doctrinepost.json)
echo ===================================================
set "SITECHOICE="
set /p SITECHOICE="Choose [1-2]: "

if "%SITECHOICE%"=="1" (
  set "PLANNAME="
  set "PLANARG="
  set "PLANLABEL=LDS Apologetics  ^(posts.json^)"
  goto menuready
)
if "%SITECHOICE%"=="2" (
  set "PLANNAME=doctrinepost.json"
  set "PLANARG=--plan doctrinepost.json"
  set "PLANLABEL=LDS Doctrines  ^(doctrinepost.json^)"
  goto menuready
)
echo Invalid choice.
goto sitemenu

:menuready
if "%PLANNAME%"=="" if not exist posts.json (
  if exist posts.example.json (
    echo No posts.json found - creating one from posts.example.json ...
    copy /y posts.example.json posts.json >nul
    echo Edit posts.json with your groups, then run again.
    echo.
  )
)

:menu
echo(
echo ===================================================
echo   Facebook Group Publishing Assistant
echo   Site: %PLANLABEL%
echo ===================================================
echo   1. Log in            (one-time session setup)
echo   2. Run - semi-auto   (you click Post)   ^<- normal
echo   3. Run - dry run      (fill only, never post)
echo   4. Run - FULL AUTO    (script clicks Post; higher risk)
echo   5. Scrape my groups   (list all joined groups as JSON)
echo   6. Edit list file
echo   7. Exit
echo(
set "choice="
set /p choice="Choose [1-7]: "

if "%choice%"=="1" goto login
if "%choice%"=="2" goto semi
if "%choice%"=="3" goto dry
if "%choice%"=="4" goto auto
if "%choice%"=="5" goto scrape
if "%choice%"=="6" goto edit
if "%choice%"=="7" exit /b 0
echo Invalid choice.
goto menu

:login
dotnet run -- login
goto done

:semi
dotnet run -- %PLANARG%
goto done

:dry
dotnet run -- --dry-run %PLANARG%
goto done

:auto
dotnet run -- --auto %PLANARG%
goto done

:scrape
dotnet run -- scrape-groups
goto done

:edit
if "%PLANNAME%"=="" (notepad posts.json) else (notepad "%PLANNAME%")
goto menu

:done
echo(
pause
