# Security Issues & Remediation Checklist

> Created: 2026-05-17. Update this file as items are resolved.

---

## 🔧 Complete Setup Reference — All Secrets & Where They Go

### Lambda Environment Variables (set in AWS Console)

Go to **Lambda Console → your function → Configuration → Environment variables → Edit**

| Variable | What it is | How to generate | Required before deploy? |
|---|---|---|---|
| `TOKEN_SECRET` | AES encryption key for user session tokens | `[System.Guid]::NewGuid().ToString().ToUpper()` in PowerShell | ✅ Yes — Lambda throws on startup without it |
| `TOKEN_IV` | AES initialization vector (must be exactly 16 chars) | `-join ((65..90)+(97..122) \| Get-Random -Count 16 \| % {[char]$_})` in PowerShell | ✅ Yes — Lambda throws on startup without it |

> ⚠️ The old values (`91692457-0D03-491A-98D4-0E5462F67D7B` / `pemgail9uzpgzl88`) are now in the public git history — do NOT reuse them. Generate fresh ones.

### AWS Secrets Manager (already in use — verify these exist)

The Lambda retrieves its MySQL connection string from Secrets Manager automatically via `ConnectionManager.cs`. The secret name format is `{environment}/websites/webadmin` (e.g., `prod/websites/webadmin`).

Go to **AWS Console → Secrets Manager** and confirm these secrets exist:

| Secret name | Contents | Used by |
|---|---|---|
| `prod/websites/webadmin` | MySQL host, port, user, password, database | All Lambda functions in prod |

If they don't exist yet, create a new secret with a JSON value matching `ConnectionModel.cs`:
```json
{
  "Server": "your-rds-endpoint",
  "Port": 3306,
  "Database": "your-db-name",
  "Username": "your-db-user",
  "Password": "your-db-password"
}
```

### Keys to Rotate in AWS IAM

| Key ID | Status | Action |
|---|---|---|
| `AKIAJ63WVYL3I6MZPUIA` | **COMPROMISED** — was committed to local git history | Deactivate + delete in IAM immediately |

### Cognito (already secure — no action needed unless rotating)

The Cognito pool IDs in `aws-exports.js` and `config.json` are not secrets — they're embedded in the client app by design and are gitignored from the repo. No rotation needed unless there's a security incident.

| Config value | File | Status |
|---|---|---|
| `userPoolId` | `aws-exports.js` / `config.json` | Gitignored ✅ |
| `userPoolWebClientId` | `aws-exports.js` / `config.json` | Gitignored ✅ |
| `identityPoolId` | `aws-exports.js` / `config.json` | Gitignored ✅ |

### Local Dev (your machine only — never commit)

For local development, you can set env vars in PowerShell before running the API:

```powershell
$env:TOKEN_SECRET = "your-new-token-secret-uuid"
$env:TOKEN_IV     = "your16charstring"
```

Or add them to a `.env` file (which is gitignored) and load them via your IDE/launch config.

---

## ⛔ IMMEDIATE — Do Before Next Deploy

### 1. Rotate compromised AWS access key

**Status:** OPEN

The key `AKIAJ63WVYL3I6MZPUIA` was committed to local git history in `api/WebApplicationArch/security/SiteAuth.json` and `api/WebApplicationArch/Function - Copy.cs`. GitHub's push protection blocked the push so the key never reached GitHub, but it exists in the local `.git` history and must be treated as compromised.

**Steps:**
1. Go to **AWS Console → IAM → Users → your user → Security credentials**
2. Find `AKIAJ63WVYL3I6MZPUIA` → **Deactivate**, then **Delete**
3. Create a replacement key if S3 access is still needed for that use case (see item #4)
4. Check CloudTrail for any unexpected use of that key since it was created

---

### 2. Set TOKEN_SECRET and TOKEN_IV in Lambda environment variables

**Status:** OPEN — Lambda deploy will fail without these

`UserSecurity.cs` now reads token signing secrets from environment variables instead of hardcoded constants. The old values (`91692457-0D03-491A-98D4-0E5462F67D7B` / `pemgail9uzpgzl88`) are now public on GitHub history — generate new ones.

**Steps:**
1. Generate new values in PowerShell:
   ```powershell
   # TOKEN_SECRET (new UUID — not the old one)
   [System.Guid]::NewGuid().ToString().ToUpper()

   # TOKEN_IV (exactly 16 characters)
   -join ((65..90) + (97..122) | Get-Random -Count 16 | % {[char]$_})
   ```
2. Go to **Lambda Console → your function → Configuration → Environment variables**
3. Add `TOKEN_SECRET` = (new UUID from step 1)
4. Add `TOKEN_IV` = (16-char string from step 1)
5. **Do not commit these values anywhere** — Lambda env vars are encrypted at rest via KMS

**Effect of rotating:** Existing user session tokens will be invalidated (users will need to log in again). This is acceptable and expected when rotating signing secrets.

---

## ⚠️ HIGH — Address Before Making Repo Public or Sharing Access

### 3. Old UserSecurity.cs crypto values are now in GitHub history

**Status:** OPEN

Even though `UserSecurity.cs` was updated to use env vars, the old hardcoded values (`91692457-0D03-491A-98D4-0E5462F67D7B` and `pemgail9uzpgzl88`) are visible in the commit history at `a3441e4~1`. Anyone with repo access can read them.

**Steps:**
1. Complete item #2 (rotate to new env var values) — this makes the leaked values useless
2. If the repo becomes public later, consider rewriting git history with `git filter-repo` to remove the old values from all commits. This is destructive and requires force-push — do it before anyone forks the repo.

---

### 4. SiteAuth.json credentials (ldsgospeldoctrine.info S3 bucket)

**Status:** OPEN

`api/WebApplicationArch/security/SiteAuth.json` contained an AWS access key and secret for the `ldsgospeldoctrine.info` S3 bucket. This file is now gitignored and deleted locally. The credentials in it are covered by item #1 (same key).

**Long-term fix:** If direct S3 access for that bucket is still needed:
- The Lambda execution role (`AWSLambdaBasicExecutionRole`) should be granted an IAM policy for that bucket instead of using explicit keys
- Use `new AmazonS3Storage(bucketName, region)` (the new IAM constructor) rather than passing keys
- Store bucket name in a Lambda environment variable, not a committed file

---

### 6. Lambda API endpoints are not validating X-Api-Key

**Status:** CODED — awaiting staged deployment (frontend first, then backend)

#### What has been implemented

**Backend — `ValidateApiKey()` in `ApiBaseFunctions`** ✅
- `api/WebApplicationArch/ApiBaseFunctions.cs` — `ValidateApiKey()` method added. Checks the `X-Api-Key` request header against `MCP_API_KEY` env var. Returns 401 if wrong; passes through if env var is not set (safe for local dev).
- All four new menu endpoints already call it: `GetMenu`, `AddMenuItem`, `UpdateMenuItem`, `DeleteMenuItem` in `ApiMenuFunctions.cs`.
- The ~20 existing handlers in `ApiFunctions.cs` and `ApiArticleFunctions.cs` do **not** call it yet — intentional, see deployment plan below.

**Frontend — `useConfig.js` sends `X-Api-Key` on every API call** ✅
- `react/baseProject/src/hooks/configuration/useConfig.js` — reads `api_key` from `config.json` and injects it as an Amplify `custom_header` function on all endpoints before calling `Amplify.configure()`. Every `API.get`, `API.post`, and `API.del` call will automatically include `X-Api-Key: <value>`.
- `react/baseProject/configs/config.example.json` — documents the new `api_key` field.
- Each deployed site's `config.json` (gitignored) needs `"api_key": "<MCP_API_KEY value>"` added before the site is redeployed.

#### Deployment plan (staged — do in order)

**Step 1 — Deploy frontend for each site** (no backend change yet)
1. Add `"api_key": "<MCP_API_KEY>"` to `config.json` for the site
2. Build and deploy the React frontend
3. Verify the site loads and admin UI still works (backend ignores the header — no risk)
4. Repeat for all sites

**Step 2 — Deploy backend** (only after ALL frontend sites are updated)
1. Add `var auth = ValidateApiKey(request); if (auth != null) return auth;` to the top of every handler in:
   - `api/WebApplicationArch/ApiFunctions.cs` (~20 methods)
   - `api/WebApplicationArch/ApiArticleFunctions.cs` (~8 methods)
2. Deploy the Lambda stack: `dotnet lambda deploy-serverless --profile Admin`
3. Verify all sites still work — admin UI now requires the key; MCP server already sends it

**Why this order is safe:**
- Frontend sends key → backend ignores it → ✅ nothing breaks
- Frontend sends key → backend validates it → ✅ works correctly
- Frontend missing key → backend ignores it → ✅ nothing breaks (Step 1 window)
- Frontend missing key → backend validates it → ❌ 401 (Step 2 must not happen until all frontends are updated)

---

### 5. UserData.json — SHA-1 password hashing

**Status:** LOW RISK (file is gitignored, may be unused)

`api/WebApplicationArch/userData/UserData.json` stores local user credentials hashed with SHA-1 (via `UserSecurity.EncodePassword`). SHA-1 is cryptographically weak for password storage.

**Steps:**
1. Confirm whether `UserData.json` / `UserSecurity.EncodePassword` is still in active use (Cognito handles auth for the Lambda stack — this may be dead code)
2. If still in use, migrate to BCrypt or Argon2

---

## ✅ DONE — Already Fixed

| Item | Fix Applied | Commit |
|---|---|---|
| Hardcoded `TOKEN_SECRET` / `TOKEN_IV` in `UserSecurity.cs` | Moved to env vars; throws if missing | `a3441e4` |
| `AmazonS3Storage.cs` taking explicit AWS keys | Added IAM-role constructor as default | `a3441e4` |
| `SiteAuth.json` with AWS credentials | Gitignored + deleted locally | `1839b01` / `a3441e4` |
| `Function - Copy.cs` with hardcoded credentials | Gitignored + deleted locally | `1839b01` / `a3441e4` |
| `team-provider-info.json` (AWS account ID + ARNs) | Gitignored | `1839b01` |
| `aws-exports.js` (Cognito pool IDs) | Already gitignored in baseProject | pre-existing |
| `config.json` (prod API URL + Cognito config) | Already gitignored | pre-existing |
| `.claude/` session memory | Gitignored | `1839b01` |

---

## 📋 General Security Reminders

- **Never commit files containing:** AWS access keys, secret keys, connection strings, passwords, API tokens, or private keys
- **Files that should always be gitignored in this project:** `config.json`, `aws-exports.js`, `SiteAuth.json`, `team-provider-info.json`, `*.env`, `UserData.json`
- **Credential storage pattern to follow:** Use AWS Secrets Manager (see `ConnectionManager.cs`) for database credentials; use Lambda environment variables for short string secrets like token signing keys
- **IAM pattern to follow:** Grant the Lambda execution role the S3/DynamoDB permissions it needs via IAM policy — never embed access keys in code or config files
