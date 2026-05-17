# Web Application Architecture

This repository contains a full-stack web application platform with:
- an AWS Lambda backend written in .NET
- a React-based frontend
- an optional MCP agent support layer
- deployment automation via AWS SAM and Lambda tools

## Repository structure

- `api/`
  - `WebApplicationArch/` - Lambda backend project, AWS SAM template, and Lambda deployment defaults
  - `MySQLConnector/` - data access layer for MySQL, DAO classes, and models
  - `WebApplicationArch.Tests/` - backend unit and integration tests

- `react/`
  - `baseProject/` - React application shell, admin UI, site content runtime, and Amplify integration
  - `reactcomponents/` - reusable React components library
  - `WebTemplates/` - legacy site templates and web configuration assets

- `mcp/`
  - support tooling for the MCP/agent integration
  - includes a small Node.js server, API client, and example tool wrappers

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

The `mcp/` folder contains tooling for the MCP server and agent integration.

- `mcp/.env.example` shows required environment variables
- `mcp/src/index.js` bootstraps the local MCP server
- `mcp/src/apiClient.js` reads environment variables for backend API access

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

## Notes

This README is informational and focused on application structure and deployment. For security-specific guidance, see `SECURITYISSUES.md`.
