@echo off
REM Optional: start Chrome yourself with the CDP debug port + the dedicated profile.
REM Use this only if you set "AttachOnly": true in appsettings.json. Otherwise the
REM runner launches Chrome for you automatically.

set PROFILE=%~dp0.fb-profile
"C:\Program Files\Google\Chrome\Application\chrome.exe" ^
  --remote-debugging-port=9222 ^
  --user-data-dir="%PROFILE%" ^
  --no-first-run ^
  --no-default-browser-check ^
  https://www.facebook.com
