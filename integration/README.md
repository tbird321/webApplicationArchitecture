# Facebook Group Publishing Assistant (C# + Playwright)

A **human-in-the-loop** helper for cross-posting your own content to your own Facebook
groups. It is **not** an unattended bot — **you** click Post.

### What one run does
1. You run the script.
2. It launches **your Chrome** (a dedicated profile) and you **log in yourself**.
3. It reads `posts.json`, picks the **next un-posted article**, and cross-posts it to
   your shared groups. Facebook only lets you hook ~8 groups onto a single post, so it
   works in **batches of `GroupsPerPost`**:
   - goes to a group, opens the composer, fills your text + link (+ optional image),
   - tries to **hook the other groups in that batch** ("Post to more groups"),
   - **waits for you to review and click _Post_**,
   - then takes control again and moves to the next batch — repeating until the
     article has reached every group.
4. The article is marked posted and the run **stops** (one article per run). Schedule
   it (e.g. daily via Task Scheduler) to advance one article at a time.

> ⚠️ **A note on Facebook's terms.** Any automation that touches the page is
> technically against Meta's ToS, and there's always *some* account risk. This tool is
> built to stay on the defensible side: **you** log in interactively and **you** click
> Post — it does not do programmatic login or unattended publishing (the behaviors
> Meta's anti-automation systems most target). Keep volume human-paced.

---

## How it controls Chrome

It uses the **Chrome DevTools Protocol (CDP)**. The run launches Chrome with
`--remote-debugging-port=9222` against a dedicated `--user-data-dir`, then Playwright
**attaches over CDP** to that real browser. So:

- Your real Chrome + a persistent profile (log into Facebook once; the session sticks).
- No bundled-browser download — attaching over CDP needs only the managed driver
  (pulled in by the NuGet package), not Playwright's browser binaries.
- You can watch everything and intervene at any point.

If you'd rather start Chrome yourself, set `"AttachOnly": true` and use
`launch-chrome.cmd`; the runner then only attaches.

---

## Setup

1. **Install the .NET 8 SDK** (https://dotnet.microsoft.com/download).
2. From this folder: `dotnet restore`
3. `copy posts.example.json posts.json` and edit it (see below).
4. Check `appsettings.json` — especially `ChromePath`.

## First run / login

```
start.cmd        (choose 1) Log in)   — or —   run.cmd -- login
```
Log into Facebook in the Chrome window, then press **Enter**. The session persists in
`.fb-profile/`.

## Normal use

Easiest: **double-click `start.cmd`** and pick **2) semi-auto**. Or:
```
run.cmd                 (semi-auto — you click Post)
run.cmd -- --dry-run    (fill composers, never mark posted — great for testing)
```
There is **no auto-post mode** — the tool never clicks Post. That is always you.

> **Running from a terminal you typed into?** On systems where Windows sets
> `NoDefaultCurrentDirectoryInExePath`, cmd won't run a script in the current folder by
> bare name — use `.\start.cmd` / `.\run.cmd` (with the `.\`) or the full path.
> Double-clicking `start.cmd` in Explorer is unaffected.

**Recommended first time:** log in (menu **1**), then do a **dry run** (menu **3**) to
watch it fetch the text, open the composer, add your groups, and fill everything —
without posting. Check `screenshots/` if anything looks off.

In semi-auto, for each batch you'll see which groups to confirm, then:
```
→ Make sure the groups are selected, click POST yourself, then press [Enter] (s+[Enter] to skip):
```

---

## The plan (`posts.json`)

Groups are shared (you always post to the same ones), so they live **once** at the top.
Each article is posted to all of them, in batches of `GroupsPerPost`.

```jsonc
{
  "Groups": [
    { "Name": "My LDS Group", "Url": "https://www.facebook.com/groups/000000000000000" }
    // Name is required — Facebook's "post to more groups" picker searches by name.
  ],
  "GroupsPerPost": 8,          // Facebook's per-post group hook limit (~8)
  "Articles": [
    {
      "Id": "Blood-Atonement", // unique; used in screenshot names
      "Link": "https://ldsapologetics.com/?page=Blood-Atonement",  // required
      "ImagePath": null,       // optional absolute path to an image
      "PostedAtUtc": null,     // set automatically once posted to ALL groups
      "PostedGroups": []       // group URLs done so far (resume-safe)
      // "Message": "..."      // OPTIONAL override; omit to fetch text from the page
    }
  ]
}
```

**Post text comes from the site.** You don't write the message in the plan — at run
time the tool opens the article's `Link` and builds a hook from the page itself:

- the **H1 title**,
- the **intro lede** (the italic subtitle right under the H1), and
- the **opening line of the closing section** (the `Verdict` heading, falling back to
  `Conclusion` / `Summary` / `The Bottom Line`).

So the post leads with the hook and closes with the payoff, then the `Link` is appended
so Facebook renders its preview card. The Verdict line is skipped if it just restates the
lede. Add an optional `Message` to an article only if you want to override all of this.

> This intro+verdict shape assumes the article follows the site's standard layout
> (H1 → italic lede → body H2s → a `Verdict` H2). Articles that don't will still post the
> title + first paragraph.

The runner takes the **first article whose `PostedAtUtc` is null**, posts it, marks it,
and stops. It ships with a single article so you can validate the flow end-to-end
before building out the full list.

## Multiple lists (apologetics, ldsdoctrines, …)

A "list" is just its own plan file with its own `Groups` **and** `Articles`. Keep as
many as you like and choose one per run — same Facebook login, different groups + content:

```
run.cmd -- --plan apologetics       # uses posts.apologetics.json
run.cmd -- --plan ldsdoctrines      # uses posts.ldsdoctrines.json
run.cmd -- --plan posts.foo.json    # or an explicit file path
run.cmd                             # no --plan -> default posts.json
```

`--plan <name>` resolves to `posts.<name>.json` in this folder; a value containing a
dot or slash is treated as a literal path. (`start.cmd` also prompts for a list name.)
Schedule each list as its own Task Scheduler entry with its own `--plan` argument. See
`posts.ldsdoctrines.example.json` for a second-list template.

---

## Files

| File | Purpose |
|------|---------|
| `Program.cs` | Entry point, modes, the per-batch post loop |
| `ChromeSession.cs` | Launch/attach Chrome over CDP |
| `Poster.cs` | Compose + hook groups (candidate selectors) |
| `PostItem.cs` | `Plan`/`Article`/`Group` model + load/save |
| `Config.cs` | `appsettings.json` loader |
| `posts.example.json` | Sample plan (copy to `posts.json`) |
| `launch-chrome.cmd` | Manual Chrome launch for `AttachOnly` mode |
| `run.cmd` | Pass-through wrapper |
| `start.cmd` | **Easiest: double-click** — login / semi-auto / dry-run / auto menu |

## How a post is assembled (the Poster flow)

For each batch, `Poster.PrepareMultiAsync` does, in this exact order:

1. Navigate to the first group and open its **Create post** composer.
2. **Add the other groups first.** Facebook hides the "Add groups" control the moment the
   composer has text (unless an image is attached), so groups must be ticked before typing.
   It opens the **Add groups** picker, ticks each group's row checkbox, and clicks **Done**.
3. Attach the image (if `ImagePath` is set).
4. **Type the body** into the composer, then wait for the link preview card to render.
5. Stop and let **you** review and click **Post**. The tool never submits on its own.

It writes screenshots at each step to `screenshots/` (`group-picker`,
`group-picker-selected`, `after-add-groups`, `prepared`, plus `*-fail` shots), which is
the fastest way to see where a selector drifted.

## Maintenance note

Facebook's markup is obfuscated and shifts often, so the selectors in `Poster.cs` are
candidate lists matched by role/aria/text — expect to tune them. Things that have bitten
us and are worth knowing when they break again:

- **Composer text box.** Match it by its `Write something…` placeholder / `data-lexical-editor`
  and scope to the dialog — a looser `div[role='textbox']` also matches feed **comment boxes**
  sitting behind the dialog. The editor is a **Lexical** contenteditable; the filler types,
  verifies the box actually has text, and falls back to direct `InsertText` if key events
  get dropped.
- **Group picker.** Keep all picker actions **scoped to the dialog** — an unscoped search
  box matches Facebook's global top search bar, and clicking a result navigates the whole
  page away. The row **checkbox/button** is the toggle, not the name text span, so the code
  walks up from the name to the nearest interactive ancestor and verifies `aria-checked`.

The group-hooking step is best-effort; if it can't auto-select a group it prints the name
so you can tick it manually before posting. `--dry-run` + `screenshots/` make diagnosis quick.

## Never commit
`.fb-profile/` holds your logged-in session/cookies (git-ignored). So is `posts.json`
(your real content/groups). Don't override those ignores.
