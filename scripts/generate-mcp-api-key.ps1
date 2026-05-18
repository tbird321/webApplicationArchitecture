<#
.SYNOPSIS
Generates a new MCP_API_KEY and persists it as a user environment variable.

.DESCRIPTION
Generates a random API key, saves it to the current user's environment so it
survives across terminal sessions, and sets it in the current session so deploy
scripts can use it immediately without reopening the terminal.
#>

$existing = [System.Environment]::GetEnvironmentVariable('MCP_API_KEY', 'User')
if (-not [string]::IsNullOrEmpty($existing)) {
    $overwrite = Read-Host 'MCP_API_KEY already exists for this user. Overwrite? (Y/N)'
    if ($overwrite -notin @('Y', 'y', 'Yes', 'yes')) {
        Write-Host 'Keeping existing MCP_API_KEY.' -ForegroundColor Yellow
        $env:MCP_API_KEY = $existing
        Write-Host 'Loaded into current session.' -ForegroundColor Green
        exit 0
    }
}

$key = [System.Guid]::NewGuid().ToString('N').ToUpper() + [System.Guid]::NewGuid().ToString('N').ToUpper()

[System.Environment]::SetEnvironmentVariable('MCP_API_KEY', $key, 'User')
$env:MCP_API_KEY = $key

Write-Host 'MCP_API_KEY generated and saved to user environment.' -ForegroundColor Green
Write-Host 'It is available in this session and all future terminal sessions.' -ForegroundColor Green
Write-Host 'Keep it secret — it is the key agents use to call the MCP endpoint.' -ForegroundColor Yellow
