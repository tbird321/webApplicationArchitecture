# Web Application Architecture — Project Guide

## CMS Article HTML Styling Guide

The styling below applies **only to ldsdoctrines.com (WEBSITE_ID=2) and ldsapologetics.com (WEBSITE_ID=5)**. Other sites managed by the `webcms` MCP server use different styling — do not apply this guide to them unless explicitly instructed.

---

## CES Letter Site Styling Guide (WEBSITE_ID=8 — cesletter.info only)

Mirrors the visual design of **read.cesletter.org** — white background, red accent, Open Sans font. Apply this styling to all article HTML on cesletter.info.

### Color Palette

| Role | Value |
|------|-------|
| Brand red (headings, accents, borders) | `#f93724` |
| Body text | `#2c3e50` |
| Border / divider | `#eaecef` |
| Link blue | `#377dff` |
| Background | `#ffffff` |
| Blockquote background | `#fff8f7` |
| Table stripe | `#f6f8fa` |
| Table border | `#dfe2e5` |

### Element Styles

**H1:**
```html
<h1 style="font-size:2.2rem; font-weight:600; color:#2c3e50; line-height:1.25; margin:0 0 0.5rem 0; font-family:'Open Sans',sans-serif;">Title</h1>
```

**H2** (red underline):
```html
<h2 style="font-size:1.65rem; font-weight:600; color:#2c3e50; border-bottom:1px solid #eaecef; padding-bottom:0.3rem; margin:2rem 0 1rem 0; font-family:'Open Sans',sans-serif;">Section</h2>
```

**H3:**
```html
<h3 style="font-size:1.35rem; font-weight:600; color:#2c3e50; margin:1.5rem 0 0.75rem 0; font-family:'Open Sans',sans-serif;">Sub-section</h3>
```

**Body paragraph:**
```html
<p style="font-size:1rem; line-height:1.7; color:#2c3e50; margin:0 0 1rem 0; font-family:'Open Sans',sans-serif;">Text</p>
```

**Lede / italic:**
```html
<p style="font-size:1.05rem; font-style:italic; color:#2c3e50; margin:0 0 1.5rem 0; font-family:'Open Sans',sans-serif;">Lede text</p>
```

**Blockquote** (red left border — quoting the CES Letter being refuted):
```html
<blockquote style="border-left:4px solid #f93724; background:#fff8f7; margin:20px 0; padding:12px 20px; color:#2c3e50; font-style:italic; font-size:1rem; line-height:1.7;">
  Quote text
</blockquote>
```

**Red section title bar** (for chapter headings):
```html
<div style="background:#f93724; color:#ffffff; padding:1rem 1.5rem; font-weight:600; text-transform:uppercase; letter-spacing:0.05em; font-family:'Open Sans',sans-serif; margin:0 0 1.5rem 0;">Chapter Title</div>
```

**Table** (red header):
```html
<table style="border-collapse:collapse; width:100%; margin:1rem 0 1.5rem 0; font-size:0.9rem; font-family:'Open Sans',sans-serif;">
  <tbody>
    <tr>
      <th style="background:#f93724; color:#ffffff; font-weight:600; padding:0.6em 1em; border:1px solid #dfe2e5; text-align:left;">Column</th>
    </tr>
    <tr>
      <td style="padding:0.6em 1em; border:1px solid #dfe2e5; color:#2c3e50; vertical-align:top;">Cell</td>
    </tr>
    <tr style="background:#f6f8fa;">
      <td style="padding:0.6em 1em; border:1px solid #dfe2e5; color:#2c3e50; vertical-align:top;">Stripe row</td>
    </tr>
  </tbody>
</table>
```

### General Rules

- Font: `'Open Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif`
- All scripture links: `https://www.churchofjesuschrist.org/study/scriptures/...` with `target="_blank" rel="noopener"` and `style="color:#377dff"`
- No raw `<hr>` tags — use margin spacing
- Use `&ldquo;` / `&rdquo;` for curly quotes, `&mdash;` for em dashes
- CES Letter quotes being refuted always use the red-border blockquote style

### Color Palette

| Role | Value |
|------|-------|
| Heading text | `#1a1410` |
| Body text | `#2a1f14` |
| Secondary / italic text | `#5c4a35` |
| Gold accent (borders, labels) | `#b8860b` |
| Gold blockquote background | `#fdf8ee` |
| Gold blockquote border | `#e8d9c3` |
| Blue accent (reference blocks) | `#2c4a6e` |
| Blue blockquote background | `#f0f4f8` |

---

### Element Styles

**H1 — Page title**
```html
<h1 style="font-size: 2em; font-weight: bold; color: #1a1410; margin: 0 0 6px 0;">Title</h1>
```

**Subtitle / italic lede** (immediately after H1)
```html
<p style="font-size: 1.05em; font-style: italic; color: #5c4a35; margin: 0 0 28px 0;">Subtitle text</p>
```

**H2 — Section heading** (gold underline)
```html
<h2 style="font-size: 1.3em; font-weight: bold; color: #1a1410; border-bottom: 2px solid #b8860b; padding-bottom: 6px; margin: 0 0 16px 0;">Section</h2>
```

**H3 — Sub-section heading**
```html
<h3 style="font-size: 1.05em; font-weight: bold; color: #2a1f14; margin: 0 0 10px 0;">Sub-section</h3>
```

**Body paragraph**
```html
<p style="font-size: 0.97em; line-height: 1.8; color: #2a1f14; margin: 0 0 14px 0;">Text here.</p>
```

Use `margin: 0 0 28px 0` on the last paragraph before a new section to add extra breathing room.

**Scripture blockquote** (gold accent — primary quotes)
```html
<blockquote style="background: #fdf8ee; border-left: 3px solid #b8860b; padding: 14px 20px; margin: 0 0 14px 0;">
  <p style="font-size: 0.78em; font-weight: 600; letter-spacing: 0.18em; text-transform: uppercase; color: #b8860b; margin: 0 0 6px 0;">Book Chapter:Verse</p>
  <p style="font-size: 1em; font-style: italic; color: #5c4a35; line-height: 1.7; margin: 0;">&ldquo;Quote text.&rdquo;</p>
</blockquote>
```

**Reference / list blockquote** (blue accent — supporting scripture lists)
```html
<blockquote style="background: #f0f4f8; border-left: 2px solid #2c4a6e; padding: 12px 16px; margin: 0 0 20px 0;">
  <p style="font-size: 0.75em; font-weight: 600; letter-spacing: 0.2em; text-transform: uppercase; color: #2c4a6e; margin: 0 0 6px 0;">Section Label</p>
  <ul style="margin: 0; padding-left: 18px; color: #5c4a35; font-size: 0.85em; line-height: 1.65;">
    <li style="margin-bottom: 5px;"><strong style="color: #2c4a6e;">Ref</strong> &mdash; Description</li>
  </ul>
</blockquote>
```

**Table** (gold header — for schedules, comparisons)
```html
<table style="width: 100%; border-collapse: collapse; margin: 0 0 28px 0; font-size: 0.9em;">
  <tbody>
    <tr style="background: #fdf8ee; border-bottom: 2px solid #b8860b;">
      <th style="color: #b8860b; padding: 10px 14px; text-align: left; font-weight: bold;">Column</th>
      <th style="color: #b8860b; padding: 10px 14px; text-align: left; font-weight: bold; border-left: 1px solid #e8d9c3;">Column</th>
    </tr>
    <tr style="border-top: 1px solid #e8d9c3;">
      <td style="padding: 10px 14px; color: #2a1f14; vertical-align: top;">Cell</td>
      <td style="padding: 10px 14px; color: #2a1f14; vertical-align: top; border-left: 1px solid #e8d9c3;">Cell</td>
    </tr>
  </tbody>
</table>
```

**Centered image** (at top of article)
```html
<p style="text-align: center; margin: 0 0 20px 0;"><img src="..." width="..." height="..."></p>
```

---

### General Rules

- Never use raw `<hr>` tags for section breaks — use margin spacing on paragraphs instead.
- Remove `data-start` / `data-end` attributes if present (artifacts from AI-generated content).
- Remove stray URLs or editor artifacts (e.g. Facebook class names) from content.
- All scripture references should link to `https://www.churchofjesuschrist.org/study/scriptures/...` with `target="_blank" rel="noopener"`.
- Use `&ldquo;` / `&rdquo;` for curly quotes, `&mdash;` for em dashes, `&ndash;` for en dashes.
- The last paragraph before a major section break should use `margin: 0 0 28px 0`.

---

## MCP Server Setup (run once after cloning)

The `webcms` MCP server is a local Node.js stdio process that Claude Code launches automatically via `.mcp.json`. It calls the deployed Lambda API using environment variables.

**1. Install dependencies:**
```powershell
cd api/mcp
npm install
```

**2. Set required environment variables (persist permanently):**
```powershell
# Set your AWS profile name
$profile = 'your-aws-profile-name'

# Retrieve MCP_API_KEY from the deployed Lambda (it was set at deploy time)
$mcpFn  = (aws cloudformation list-stack-resources --stack-name webapplicationarch --profile $profile --region us-west-2 --query "StackResourceSummaries[?LogicalResourceId=='McpFunction'].PhysicalResourceId" --output text)
$mcpKey = (aws lambda get-function-configuration --function-name $mcpFn --profile $profile --region us-west-2 --query "Environment.Variables.MCP_API_KEY" --output text)
[System.Environment]::SetEnvironmentVariable('MCP_API_KEY', $mcpKey, 'User')
$env:MCP_API_KEY = $mcpKey

# Retrieve the API base URL from the stack output — fix casing (/Prod -> /prod)
$url = (aws cloudformation describe-stacks --stack-name webapplicationarch --profile $profile --region us-west-2 --query "Stacks[0].Outputs[?OutputKey=='ApiURL'].OutputValue" --output text).TrimEnd('/') -replace '/Prod$', '/prod'
[System.Environment]::SetEnvironmentVariable('LAMBDA_API_BASE_URL', $url, 'User')
$env:LAMBDA_API_BASE_URL = $url

# Set the site to manage (see table below)
[System.Environment]::SetEnvironmentVariable('WEBSITE_ID', '2', 'User')
$env:WEBSITE_ID = '2'
```

**3. Restart VS Code** — Claude Code reads `.mcp.json` at the repo root and starts the `webcms` server automatically. No further configuration needed.

---

## MCP Server

The `webcms` MCP server manages content for multiple sites. The active site is controlled by `WEBSITE_ID`:

| ID | Site |
|----|------|
| 1 | ldsfaithincrisis.com |
| 2 | ldsdoctrines.com |
| 4 | reflectiverealizations.com |
| 5 | ldsapologetics.com |
| 6 | ldsdiscussions.info |
| 7 | alexisbpetersen.com |

To switch sites, update `WEBSITE_ID` as a Windows User environment variable and restart VS Code.

## S3 Content Path

Article HTML is stored at:
```
s3://www-websitecontent/public/websites/{websiteName}/articles/{articlePath}
```

## AWS

- Region: `us-west-2`
- Deploy profile: `Admin`
- Lambda stack: `webapplicationarch`
- Deploy command (from `api/WebApplicationArch/`): `dotnet lambda deploy-serverless --profile Admin`
