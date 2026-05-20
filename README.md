# Web Application Architecture

This repository contains a full-stack web application platform with:
- an AWS Lambda backend written in .NET
- a React-based frontend
- an optional MCP agent support layer
- deployment automation via AWS SAM and Lambda tools

## Repository structure

- `api/`
  - `WebApplicationArch/` - Lambda backend project, AWS SAM template, and Lambda deployment defaults
  - `mcp/` - MCP Lambda handler (Node.js, deployed as part of the SAM stack)
  - `MySQLConnector/` - data access layer for MySQL, DAO classes, and models
  - `WebApplicationArch.Tests/` - backend unit and integration tests

- `react/`
  - `baseProject/` - React application shell, admin UI, site content runtime, and Amplify integration
  - `reactcomponents/` - reusable React components library
  - `WebTemplates/` - legacy site templates and web configuration assets

- `ApplicationDocumentation.txt` - website hosting setup and S3/CloudFront configuration notes
- `PLAN.md` - project plan, feature progress, and implementation summary
- `SECURITYISSUES.md` - security issues and remediation checklist

## What this application does

This project supports a website CMS architecture with:
- serverless API endpoints for pages, articles, keywords, layout, topics, and websites
- MySQL-backed content storage configured through AWS Secrets Manager
- a React admin UI with page/article management, publish workflow, and site navigation
- support for publishing static website assets to S3 and CloudFront

## Backend deployment

The backend is deployed from `api/WebApplicationArch/`.

Defaults are stored in `api/WebApplicationArch/aws-lambda-tools-defaults.json`:
- `stack-name`: `webapplicationarch`
- `s3-bucket`: `webapparchdeploy`

A SAM template is available at `api/WebApplicationArch/serverless.template`.

### Deploy with SAM

From `api/WebApplicationArch/`:

```powershell
sam build --template-file serverless.template
sam deploy --template-file .aws-sam\build\template.yaml --stack-name webapplicationarch --s3-bucket webapparchdeploy --capabilities CAPABILITY_IAM --parameter-overrides TokenSecret="<your-secret>" TokenIV="<your-16-char-iv>"
```

The repository also includes PowerShell helpers in `scripts/` that build before deploy and verify Lambda env vars.

### Deploy with .NET Lambda tools

From `api/WebApplicationArch/`:

```powershell
dotnet lambda deploy-serverless
```

## Frontend

The main React application lives in `react/baseProject/`.

- run `npm install` inside `react/baseProject/`
- build the app or deploy it to a static site host
- `react/baseProject/.gitignore` excludes generated config and Amplify files

The component library is in `react/reactcomponents/`.

## MCP / agent tools

The MCP server is deployed as a Lambda function (`McpFunction`) in the same SAM stack as the backend API. It exposes tools for pages, articles, collections, and metadata to any MCP-compatible AI agent (Claude Code, Claude Desktop, etc.).

### Deploy

The MCP Lambda is included in the normal backend deploy. You need one additional parameter:

- `MCP_API_KEY` — a long random string the agent sends as `x-api-key` to call the MCP endpoint. Generate and persist it once using the helper script, then the deploy scripts pick it up automatically.

```powershell
# Run once — generates the key and saves it to your user environment permanently
.\scripts\generate-mcp-api-key.ps1

# Then deploy as normal
.\scripts\deploy-lambda-with-tokens.ps1 -ProfileName <your-aws-profile>
```

### Get the API URL after deploy

```powershell
aws cloudformation describe-stacks --stack-name webapplicationarch --profile <your-aws-profile> --region us-west-2 --query "Stacks[0].Outputs[?OutputKey=='ApiURL'].OutputValue" --output text
```

### Configure local MCP server for Claude Code

The repo includes a local stdio MCP server at `api/mcp/src/index.js`. Claude Code reads `.mcp.json` at the repo root and launches it automatically when you open Claude Code from this directory.

**One-time setup — set these as persistent user environment variables:**

```powershell
$profile = '<your-aws-profile>'

# Retrieve MCP_API_KEY from the deployed Lambda
$mcpFn  = (aws cloudformation list-stack-resources --stack-name webapplicationarch --profile $profile --region us-west-2 --query "StackResourceSummaries[?LogicalResourceId=='McpFunction'].PhysicalResourceId" --output text)
$mcpKey = (aws lambda get-function-configuration --function-name $mcpFn --profile $profile --region us-west-2 --query "Environment.Variables.MCP_API_KEY" --output text)
[System.Environment]::SetEnvironmentVariable('MCP_API_KEY', $mcpKey, 'User')
$env:MCP_API_KEY = $mcpKey

# Retrieve the API base URL — fix casing (/Prod -> /prod)
$url = (aws cloudformation describe-stacks --stack-name webapplicationarch --profile $profile --region us-west-2 --query "Stacks[0].Outputs[?OutputKey=='ApiURL'].OutputValue" --output text).TrimEnd('/') -replace '/Prod$', '/prod'
[System.Environment]::SetEnvironmentVariable('LAMBDA_API_BASE_URL', $url, 'User')
$env:LAMBDA_API_BASE_URL = $url
```

**Set WEBSITE_ID to the site you want the agent to manage:**

| Site | URL | WEBSITE_ID |
|------|-----|------------|
| Faith In Crisis | ldsfaithincrisis.com | 1 |
| LDS Doctrines | ldsdoctrines.com | 2 |
| LDS Apologetics | ldsapologetics.com | 5 |

```powershell
[System.Environment]::SetEnvironmentVariable('WEBSITE_ID', '1', 'User')  # ldsfaithincrisis.com
[System.Environment]::SetEnvironmentVariable('WEBSITE_ID', '2', 'User')  # ldsdoctrines.com
[System.Environment]::SetEnvironmentVariable('WEBSITE_ID', '5', 'User')  # ldsapologetics.com
```

**Install dependencies (run once after cloning):**

```powershell
cd api/mcp
npm install
```

Once env vars are set, open Claude Code from the repo root — the `webcms` MCP tools (search_pages, create_page, publish_page, create_article, etc.) will be available automatically.

### Configure in Claude Code / Claude Desktop

Add to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "webcms": {
      "type": "http",
      "url": "https://<api-id>.execute-api.us-west-2.amazonaws.com/prod/mcp?websiteId=<your-website-id>",
      "headers": {
        "x-api-key": "<your-mcp-api-key>"
      }
    }
  }
}
```

- Replace `<api-id>` with the value from the stack output above
- Replace `<your-website-id>` with the numeric ID of the site you want the agent to manage
- Replace `<your-mcp-api-key>` with the value you set for `MCP_API_KEY` during deploy
- The `TOKEN_SECRET` is reused as the endpoint key — no extra secret to manage

### Retrieve current token values (if you need to redeploy without rotating)

```powershell
$profile = '<your-aws-profile>'
$fn = (aws cloudformation list-stack-resources --stack-name webapplicationarch --profile $profile --region us-west-2 --query "StackResourceSummaries[?LogicalResourceId=='GetWebsites'].PhysicalResourceId" --output text)
$env:TOKEN_SECRET = (aws lambda get-function-configuration --function-name $fn --profile $profile --region us-west-2 --query "Environment.Variables.TOKEN_SECRET" --output text)
$env:TOKEN_IV     = (aws lambda get-function-configuration --function-name $fn --profile $profile --region us-west-2 --query "Environment.Variables.TOKEN_IV" --output text)
```

Then run `deploy-lambda-no-rotate.ps1`.

## Local development notes

- `api/WebApplicationArch/security/UserSecurity.cs` reads `TOKEN_SECRET` and `TOKEN_IV` from environment variables
- local development can set these values in PowerShell or an IDE launch profile
- database connection details are loaded from AWS Secrets Manager through `ConnectionManager.cs`

## Code quality & security

Pre-commit hooks run automatically before each commit to catch issues early:

**Install (run once after cloning):**
```powershell
pip install --user pre-commit
pre-commit install
```

**What the hooks check:**
- `detect-secrets` — block commits containing AWS keys, tokens, or other secrets
- `trailing-whitespace` and `end-of-file-fixer` — enforce whitespace and file formatting
- `check-yaml` and `check-json` — validate YAML and JSON syntax
- `dotnet format --verify-no-changes` — enforce .NET code formatting

**Run manually (all files):**
```bash
pre-commit run --all-files
```

**Bypass hooks (not recommended):**
```bash
git commit --no-verify
```

## Useful files

- `ApplicationDocumentation.txt` — website bucket setup, DNS, CloudFront, and deployment notes
- `PLAN.md` — feature plan, progress tracking, and implementation summary
- `api/WebApplicationArch/aws-lambda-tools-defaults.json` — default Lambda stack and bucket settings
- `api/WebApplicationArch/serverless.template` — SAM function and API definitions
- `.pre-commit-config.yaml` — pre-commit hook configuration

## TODO

- **Lock down Lambda API endpoints with `X-Api-Key` validation** — the `/mcp` entry point is protected, but the underlying API endpoints (`/page`, `/article`, `/menu`, etc.) currently accept requests without key validation. `ValidateApiKey()` is already implemented in `ApiBaseFunctions` and wired into the new menu functions. To extend it to all existing handlers, the React admin frontend must first be updated to send `X-Api-Key` on every request — otherwise existing sites will break. See `SECURITYISSUES.md` item #6 for full details.

## Notes

This README is informational and focused on application structure and deployment. For security-specific guidance, see `SECURITYISSUES.md`.
