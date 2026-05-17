# Plan: Admin UI Refactor + Draft/Publish + MCP Agent Integration

> **Status**: Approved 2026-05-17. **All local work complete.** Awaiting user-triggered DB migration + deploy to go live.

---

## ✅ Progress (2026-05-17)

| # | Task | Status | Notes |
|---|---|---|---|
| 1 | Track 1 — Admin UI shell (AdminLayout + SidebarNav) | ✅ Done | `react/baseProject/src/admin/AdminLayout.jsx`, `admin/components/SidebarNav.jsx` |
| 2 | Track 1 — Dashboard.jsx | ✅ Done | `admin/pages/Dashboard.jsx` — recent pages, quick actions, status summary |
| 3 | Track 1 — Manager wrappers (Pages/Articles/Menu/Collections/Theme) | ✅ Done | 5 files under `admin/pages/` |
| 4 | Track 1 — Wire routes in App.js + admin link in UserStatus | ✅ Done | `App.js`, `UserStatus.jsx`, `SiteContainer.jsx` updated |
| 5 | Track 2 — Backend C# code (local only, NOT deployed) | ✅ Done | Models + DAOs + ApiFunctions + serverless.template updated. **MockWebsiteProcessing also updated to satisfy the interface.** |
| 6 | Track 2 — Frontend hooks (publish/unpublish + createPageWithDefaultArticle) | ✅ Done | `DatabaseProcessing.js` extended |
| 7 | Track 3 — MCP server scaffold + tools | ✅ Done | `mcp/` directory + `.mcp.json` at repo root |

### Files created (this pass)

**Track 1 (admin UI):**
- `react/baseProject/src/admin/AdminLayout.jsx`
- `react/baseProject/src/admin/components/SidebarNav.jsx`
- `react/baseProject/src/admin/pages/Dashboard.jsx`
- `react/baseProject/src/admin/pages/PagesManager.jsx`
- `react/baseProject/src/admin/pages/ArticlesManager.jsx`
- `react/baseProject/src/admin/pages/MenuManager.jsx`
- `react/baseProject/src/admin/pages/CollectionsManager.jsx`
- `react/baseProject/src/admin/pages/ThemeManager.jsx`

**Track 3 (MCP server):**
- `mcp/package.json`
- `mcp/.env.example`
- `mcp/.gitignore`
- `mcp/src/index.js`
- `mcp/src/apiClient.js`
- `mcp/src/tools/pages.js`
- `mcp/src/tools/articles.js`
- `mcp/src/tools/navigation.js` (stubbed — see deferred items)
- `mcp/src/tools/collections.js`
- `mcp/src/tools/metadata.js`
- `.mcp.json` (repo root)

### Files modified (this pass)

**Frontend:**
- `react/baseProject/src/App.js` — added `/admin/*` routes
- `react/baseProject/src/sitecontent/SiteContainer.jsx` — removed `SiteAdministration` toggle
- `react/baseProject/src/sitecontent/UserStatus.jsx` — admin icon now navigates to `/admin` via `useNavigate`
- `react/baseProject/src/hooks/appState/DatabaseProcessing.js` — added `publishPage`, `unpublishPage`, `publishArticle`, `unpublishArticle`, `createPageWithDefaultArticle`

**Backend (NOT deployed, code lives on disk):**
- `api/MySQLConnector/Models/PageModel.cs` — added `status` field
- `api/MySQLConnector/Models/ArticleModel.cs` — added `status` field
- `api/MySQLConnector/PageDAO.cs` — added `SetPageStatus(int pageId, string status)`
- `api/MySQLConnector/ArticleDAO.cs` — added `SetArticleStatus(int articleId, string status)`
- `api/MySQLConnector/Interfaces/IWebsietProcessing.cs` — added publish/unpublish methods
- `api/MySQLConnector/WebsiteProcessing.cs` — implemented publish/unpublish wrappers
- `api/WebApplicationArch/ApiFunctions.cs` — added `PublishPage`, `UnpublishPage`, `PublishArticle`, `UnpublishArticle` Lambda handlers
- `api/WebApplicationArch/serverless.template` — added 4 new function definitions + routes (`/page/{id}/publish`, `/page/{id}/unpublish`, `/article/{id}/publish`, `/article/{id}/unpublish`)
- `api/WebApplicationArch.Tests/MockWebsiteProcessing.cs` — implemented new interface methods as no-ops

### Conscious scope reductions (decisions worth knowing about)

1. **Did NOT update read paths to surface `status`.** The plan called for adding `status` to `GetPageDetails` / `RetrievePagesMapPage` / `RetrievePagesMapArticle` reads. Those go through MySQL views (`page_keywords_topics_view`, `get_page_articles_view`, `article_keywords_topics_view`) which would need to be updated server-side. Skipped to keep deploy-safe: the publish/unpublish endpoints do simple direct `UPDATE` statements that work as soon as the `ALTER TABLE` migration runs. Status field will start flowing through API responses once the views are updated.
2. **Did NOT update `UpsertPage` / `UpsertArticle` stored procs to accept `_status` param.** Same reason — that's a DB-side change. The new pages created via `createPageWithDefaultArticle` set `status: 'draft'` in the C# model, but the stored proc will ignore that param until updated. Result: new pages created post-migration but pre-stored-proc-update default to `published` (the column default). Acceptable interim state.
3. **Menu tools in the MCP server are stubbed.** Menu data lives in S3 as a JSON file. Either add `/menu` Lambda endpoints OR give the MCP server S3 credentials. Stubbed with an explanatory error message.

---

---

## ⛔ DEFERRED — DO NOT RUN UNTIL USER EXPLICITLY APPROVES

These actions can break prod. They are intentionally NOT being executed during the current implementation pass. The code changes that depend on them ARE being written locally — they just must not be triggered until the user is ready to cut over.

| Action | Command / Trigger | Why deferred |
|---|---|---|
| DB migration — add `status` column to `page` | `ALTER TABLE page ADD COLUMN status ENUM('draft','published') NOT NULL DEFAULT 'published';` | Schema change on live DB |
| DB migration — add `status` column to `article` | `ALTER TABLE article ADD COLUMN status ENUM('draft','published') NOT NULL DEFAULT 'published';` | Schema change on live DB |
| Update `UpsertPage` / `UpsertArticle` stored procs to accept `status` param | (run against live DB) | Schema change on live DB |
| Deploy backend API | `sam deploy` from `api/WebApplicationArch/` | Backend code references the `status` column — deploying before migration breaks API |
| Run MCP server against prod | `node mcp/src/index.js` with prod `LAMBDA_API_BASE_URL` | Server calls publish endpoints that only exist after the deploy above |
| `npm publish` reactcomponents | n/a | All new admin code goes in `baseProject/src/admin/`, no component-library publish needed |
| `git push` | n/a | Only commit/push when user asks |

**Local-only work that IS being done now**: Track 1 (admin UI shell), Track 2 backend C# code (sits on disk, NOT deployed), Track 2 frontend hooks/UI, Track 3 MCP server files. All changes are reversible by deleting/reverting files.

---

## Constraints

- **Local files only** for this implementation pass. No DB writes, no deploys, no publishes, no pushes.
- Backend C# changes in Track 2 reference a column that does not exist on prod yet. **Do not run `sam deploy` until the migration has been run.**

## Context

The current CMS admin uses a visibility-toggle pattern (boolean state per screen) inside `SiteAdministration.jsx`, with no persistent navigation, no content status workflow, and no agent API surface. The goal is to:

1. Refactor the admin UI into a proper sidebar-nav shell with React Router routes and dedicated screens
2. Add draft/publish workflow so content can be staged before going live (required for agent-authored content)
3. Build an MCP server so AI agents can research, create, edit, and publish content via typed tools

All new admin components go directly into `baseProject/src/admin/` (no reactcomponents publish cycle). Agents authenticate via API key.

---

## Track 1 — Admin UI Refactor

### New File Structure

```
baseProject/src/
└── admin/
    ├── AdminLayout.jsx          ← MUI Drawer + AppBar shell (NEW)
    ├── components/
    │   └── SidebarNav.jsx       ← Persistent sidebar nav (NEW)
    └── pages/
        ├── Dashboard.jsx        ← Home with quick stats/actions (NEW)
        ├── PagesManager.jsx     ← Page list + create/edit (NEW wrapper)
        ├── ArticlesManager.jsx  ← Article list + editor (NEW wrapper)
        ├── MenuManager.jsx      ← Menu tree editor (NEW wrapper)
        ├── CollectionsManager.jsx ← Collections (NEW wrapper)
        └── ThemeManager.jsx     ← Theme editor (NEW wrapper)
```

### Routing Changes

`App.js` — Add protected `/admin/*` routes alongside existing `/` route:

```
/admin                → redirect to /admin/dashboard
/admin/dashboard      → Dashboard.jsx
/admin/pages          → PagesManager.jsx
/admin/pages/:id      → PagesManager.jsx (edit mode)
/admin/articles       → ArticlesManager.jsx
/admin/menus          → MenuManager.jsx
/admin/collections    → CollectionsManager.jsx
/admin/themes         → ThemeManager.jsx
```

Wrap all `/admin/*` routes in an auth guard that redirects to login if not authenticated.

### AdminLayout.jsx

Uses MUI `Box` + `Drawer` (permanent on desktop, temporary/hamburger on mobile):

```
┌─────────────────────────────────────────────┐
│  AppBar: site name + user info + logout      │
├──────────┬──────────────────────────────────┤
│ Sidebar  │                                  │
│ Nav      │   <Outlet /> (route content)     │
│ (240px)  │                                  │
└──────────┴──────────────────────────────────┘
```

- Uses `useMediaQuery` for responsive collapse
- Active route highlighted via `useLocation()`

### SidebarNav.jsx

MUI `List` with `ListItemButton`, `ListItemIcon`, `ListItemText`, and `Collapse` for nested items:

```
Dashboard          (DashboardIcon)
Content
  ├── Pages        (ArticleIcon)
  └── Articles     (DescriptionIcon)
Structure
  ├── Menus        (MenuIcon)
  └── Collections  (CollectionsIcon)
Appearance
  └── Themes       (PaletteIcon)
```

### UX Principles (minimize clicks, minimize setup)

1. **New page → default article auto-created** — when a page is created, a default article is automatically generated with a unique system name (`{PageName} - Content`, deduplicated with a suffix if needed). No manual "Add Article" step required to get started.
2. **Inline editing over modals** — page name, description, layout, keywords all editable inline on the page editor screen; no "Open Edit Modal" layer before you can type.
3. **One-screen page editor** — page metadata + its articles visible and editable on a single scrollable screen. No separate "load article" step for the default article.
4. **Smart defaults** — layout, keywords, topics pre-populate from the most recently used values where sensible.
5. **Direct actions** — publish, delete, duplicate accessible from list rows without opening the editor first (icon buttons on each row).
6. **No redundant confirmation dialogs** — confirmations only for destructive irreversible actions (delete), not for save/publish.

### Dashboard.jsx (new)

3-column MUI `Grid` of `Card` widgets:

- Recent pages (last 5 modified) — click row goes directly to editor
- Quick actions: "New Page" (1-click, auto-names, auto-article, opens editor), "Edit Menu"
- Content status summary (draft vs published counts once Track 2 is done)

### Page/Manager Screens (refactored wrappers)

Each screen reuses existing components from current codebase:

- **PagesManager**: `SearchGrid` list → click row goes directly to inline `PageEditor` (no intermediate modal). "New Page" button creates page + default article and navigates to editor in one action.
- **ArticlesManager**: `SearchGrid` for listing standalone articles → click row opens inline `ArticleEditor`. Used for managing shared/reusable articles.
- **MenuManager**: Mounts existing `MenuTreeEditor.jsx` directly. Track unsaved changes with an `isDirty` flag — prompt "Save before leaving?" (MUI `Dialog`) if user navigates away with unsaved changes. Use React Router's `useBeforeUnload` + a custom `usePrompt` hook to intercept both in-app navigation and browser tab close. Auto-save can also be triggered on any structural change (add/rename/delete/reorder) with a debounce so the file stays in sync without requiring a manual "Save" click.
- **CollectionsManager**: Mounts existing `PageCollectionsEditor.jsx` directly
- **ThemeManager**: Mounts existing `ThemeEditor.jsx` directly

### Entry Point Change

`SiteContainer.jsx` — Remove `SiteAdministration` rendering. When logged in admin navigates to `/admin`, React Router handles it. Public site stays at `/`.

`SiteAdministration.jsx` — Can be deprecated once all screens are wrapped in new routes.

### Files Modified (Track 1)

- `react/baseProject/src/App.js` — add admin routes
- `react/baseProject/src/sitecontent/SiteContainer.jsx` — remove admin toggle, add "Go to Admin" link for logged-in users

### Files Created (Track 1)

- `react/baseProject/src/admin/AdminLayout.jsx`
- `react/baseProject/src/admin/components/SidebarNav.jsx`
- `react/baseProject/src/admin/pages/Dashboard.jsx`
- `react/baseProject/src/admin/pages/PagesManager.jsx`
- `react/baseProject/src/admin/pages/ArticlesManager.jsx`
- `react/baseProject/src/admin/pages/MenuManager.jsx`
- `react/baseProject/src/admin/pages/CollectionsManager.jsx`
- `react/baseProject/src/admin/pages/ThemeManager.jsx`

---

## Track 2 — Draft/Publish Workflow

### Database Schema Changes (DEFERRED — user will run later)

```sql
ALTER TABLE page    ADD COLUMN status ENUM('draft', 'published') NOT NULL DEFAULT 'published';
ALTER TABLE article ADD COLUMN status ENUM('draft', 'published') NOT NULL DEFAULT 'published';
```

Default `published` keeps existing content live with no migration needed.

> ⚠️ **Migration must run before Track 2 backend code is deployed**, or API calls referencing the `status` column will fail.

### Backend Changes

**`api/MySQLConnector/Models/PageModel.cs`**
- Add: `public string? status { get; set; }`

**`api/MySQLConnector/Models/ArticleModel.cs`**
- Add: `public string? status { get; set; }`

**`api/MySQLConnector/PageDAO.cs`**
- Update `InsertPage()` / `UpdatePage()` SQL to include `status` column
- Update `UpsertPage` stored procedure call to pass `status` parameter
- Update `GetPageAsync()` / `GetPageByNameAsync()` SELECT to include `status`
- Add optional `statusFilter` param to `SearchPages()` — when `statusFilter = "published"`, add `WHERE status = 'published'` (used by public site)

**`api/MySQLConnector/ArticleDAO.cs`**
- Same changes as `PageDAO` for `article` table

**`api/WebApplicationArch/ApiFunctions.cs`**
- Add `PublishPage(int id)` → calls `websiteProcessing.PublishPage(id)` (sets status = 'published')
- Add `UnpublishPage(int id)` → sets status = 'draft'
- Same for article: `PublishArticle`, `UnpublishArticle`
- Public `GetPageByName` passes `statusFilter = "published"` so visitors only see published content

**`api/WebApplicationArch/serverless.template`**
- Add 4 new Lambda functions: `PublishPage`, `UnpublishPage`, `PublishArticle`, `UnpublishArticle`
- Routes:
  - `POST /page/{id}/publish`
  - `POST /page/{id}/unpublish`
  - `POST /article/{id}/publish`
  - `POST /article/{id}/unpublish`

**`react/baseProject/src/hooks/appState/DatabaseProcessing.js`**
- Add: `publishPage(pageId)`, `unpublishPage(pageId)`, `publishArticle(articleId)`, `unpublishArticle(articleId)`
- Add: `createPageWithDefaultArticle(pageName, websiteId)` — convenience method that calls `savePage` with a pre-built page object containing one auto-named article (`{pageName} - Content`). Generates a unique articleId (UUID), creates an empty HTML placeholder in S3, then saves the page. Returns the new page ID so the admin UI can navigate directly to the editor.

### Admin UI — Status Indicators

- `PagesManager` list: add "Status" column to `SearchGrid` with MUI `Chip` (green=published, orange=draft)
- `PagesManager` edit view: "Publish" / "Unpublish" button in header bar
- `ArticlesManager`: same pattern

---

## Track 3 — MCP Server (Agent Integration)

### New Directory

```
mcp/
├── package.json         ← Node.js, @modelcontextprotocol/sdk, node-fetch
├── src/
│   ├── index.js         ← MCP server entry, API key validation, tool registration
│   ├── apiClient.js     ← HTTP wrapper around Lambda API endpoints
│   └── tools/
│       ├── pages.js     ← search_pages, get_page, create_page, update_page, publish_page
│       ├── articles.js  ← search_articles, get_article, create_article, update_article, publish_article
│       ├── navigation.js ← get_menu, update_menu_item, add_menu_item, delete_menu_item
│       ├── collections.js ← search_collections, get_collection, upsert_collection, add_page_to_collection
│       └── metadata.js  ← get_topics, get_keywords, get_layouts
└── .env.example         ← MCP_API_KEY, LAMBDA_API_BASE_URL, WEBSITE_ID
```

### MCP Tools Defined

| Tool             | Description                                                    |
|------------------|----------------------------------------------------------------|
| search_pages     | Search pages by name/keywords/topics, returns list with status |
| get_page         | Get full page with articles and metadata                       |
| create_page      | Create new page (draft by default)                             |
| update_page      | Update page name, description, layout, keywords, topics        |
| publish_page     | Set page status to published                                   |
| search_articles  | Search articles by name/keywords/topics                        |
| get_article      | Get article metadata + HTML content from S3                    |
| create_article   | Create article with HTML content (uploads to S3)               |
| update_article   | Update article HTML content + metadata                         |
| publish_article  | Set article status to published                                |
| get_menu         | Retrieve site menu structure as tree                           |
| update_menu_item | Rename menu item or change linked page                         |
| add_menu_item    | Add new menu item (root or child)                              |
| get_topics       | List all available topics                                      |
| get_keywords     | List all available keywords                                    |

### Authentication

MCP server validates `X-API-Key` header against `MCP_API_KEY` env var on every request. All Lambda calls use the existing API Gateway endpoint (no Cognito required — API Gateway handles Cognito on the public routes; admin routes can use an IAM or API key authorizer).

### Usage by Claude Agent

```json
// .mcp.json (added to project root)
{
  "mcpServers": {
    "webcms": {
      "command": "node",
      "args": ["mcp/src/index.js"],
      "env": {
        "MCP_API_KEY": "<key>",
        "LAMBDA_API_BASE_URL": "<api-gateway-url>",
        "WEBSITE_ID": "<site-id>"
      }
    }
  }
}
```

Once configured, Claude can execute flows like:

> "Research [topic], create a draft article with that content, attach it to the Home page, then publish it."

---

## Implementation Order

1. **Track 1** — Admin shell (no DB changes, pure frontend, immediately visible improvement)
2. **Track 2** — Draft/publish (requires DB migration + backend deploy before frontend) ← **migration & deploy DEFERRED per user**
3. **Track 3** — MCP server (builds on Track 2's publish endpoints)

---

## Verification

### Track 1

- `npm start` in `react/baseProject` — navigate to `/admin`, verify sidebar renders
- Click each nav item — verify correct screen loads
- Resize to mobile — verify sidebar collapses to hamburger
- Log out — verify redirect away from `/admin`

### Track 2

- Run SQL migration locally, verify no existing content breaks
- Deploy Lambda, call `GET /page/search/{name}/{websiteId}` as public — verify only published pages return
- In admin, create page as draft — verify it doesn't appear on public site
- Click Publish — verify it appears

### Track 3

- `node mcp/src/index.js` — verify server starts
- Call `search_pages` tool with test criteria — verify results
- Call `create_article` → `publish_article` — verify content appears live on public site
- Call with invalid API key — verify 401 rejection

---

## 🚦 Go-Live Checklist (DO NOT START UNTIL READY TO CUT OVER)

When you're ready to make Track 2 / Track 3 live, run these in order. Each step is independent from the local code already on disk, so you can pause between any of them.

### Step 1 — Test the local UI first (safe, no prod impact)

```powershell
cd react\baseProject
npm start
```

- Log in, click the admin icon (top right). Should navigate to `/admin` with the new sidebar shell.
- Click each nav item: Dashboard, Pages, Articles, Menus, Collections, Themes.
- Resize the browser to mobile width — sidebar should collapse to a hamburger.
- Status chips on the Pages/Articles list will show "published" for all rows (no `status` column yet, so frontend defaults via fallback).
- "New Page" button will fail with an API error because the `/page/.../publish` endpoint and `createPageWithDefaultArticle` rely on Track 2 backend code that isn't deployed yet. This is expected.

### Step 2 — Database migration

```sql
ALTER TABLE page    ADD COLUMN status ENUM('draft','published') NOT NULL DEFAULT 'published';
ALTER TABLE article ADD COLUMN status ENUM('draft','published') NOT NULL DEFAULT 'published';
```

After this runs, the publish/unpublish endpoints (after Step 4) will work.

### Step 3 — Update stored procs / views (optional, makes status visible everywhere)

These are *not* required for publish/unpublish to function, but they make `status` show up in API responses (which the admin UI's status chip uses):

- Update `UpsertPage` to accept a `_status` parameter and write it.
- Update `UpsertArticle` to accept a `_status` parameter and write it.
- Update `get_page_articles_view`, `page_keywords_topics_view`, `article_keywords_topics_view`, and the `GetPageDetails` stored proc to include the `status` column in their output.
- (If you skip this step, all pages will show as "published" in the admin chip until you update the views.)

### Step 4 — Deploy the API

```powershell
cd api\WebApplicationArch
sam deploy
```

After deploy, the new endpoints are live:
- `POST /page/{id}/publish`
- `POST /page/{id}/unpublish`
- `POST /article/{id}/publish`
- `POST /article/{id}/unpublish`

### Step 5 — Configure and start the MCP server (only if you want agent automation)

```powershell
cd mcp
npm install
copy .env.example .env
# Edit .env: set MCP_API_KEY (any random string), LAMBDA_API_BASE_URL (from sam deploy output), WEBSITE_ID
node src/index.js
```

The `.mcp.json` at the repo root reads env vars from your shell — set `$env:MCP_API_KEY`, `$env:LAMBDA_API_BASE_URL`, `$env:WEBSITE_ID` before launching Claude Code in this directory and the MCP server will be auto-configured.

### Step 6 (optional) — Deprecate old admin files

Once you've confirmed the new admin works end-to-end, you can delete:
- `react/baseProject/src/sitecontent/contentAdmin/SiteAdministration.jsx` (no longer referenced)

Leave the underlying editors (`PageEditor.jsx`, `MenuTreeEditor.jsx`, `PageCollectionsEditor.jsx`, `ThemeEditor.jsx`) in place — the new admin wraps them.
