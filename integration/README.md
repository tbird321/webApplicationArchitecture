# Facebook Group Publishing Assistant (C# + Playwright)

A helper for posting your own content to your own Facebook groups, in **your** Chrome,
under a session **you** log into. It posts **one group at a time** with randomized,
human-paced delays throughout.

### What one run does
1. You run the script.
2. It launches **your Chrome** (a dedicated profile) and you **log in yourself**.
3. It reads `posts.json`, picks the **next un-posted article**, and posts it to your
   groups **one group at a time** — a separate native post in each group, so N groups
   means N posts. For each group it:
   - goes to the group, opens the composer, attaches the optional image,
   - **pastes the post in one Ctrl+V** — body, blank line, then the article URL as the
     last line, which is what Facebook builds its preview card from,
   - **pauses 5–10s** (randomized), then clicks **Post**,
   - advances once the post box closes, which is how it confirms the post landed.
4. The article is marked posted and the run **stops** (one article per run). Schedule
   it (e.g. daily via Task Scheduler) to advance one article at a time.

> **Why one post per group?** Facebook's "post to more groups" picker lets you hook ~8
> groups onto one post, but ticking them in quick succession is exactly what its
> rapid-activity checks flag — that pattern gets blocked even when a person does it by
> hand. So the tool doesn't use the picker at all: each group gets its own native post,
> spaced out. Slower, and much less likely to be throttled.

### Pacing (all randomized)

| Where | Delay |
|-------|-------|
| Before each post, the first one included | **10–30s** (`BetweenPostsMinMs`/`MaxMs`) |
| Landing on the group page | 4–12s (`MinDelayMs`/`MaxDelayMs`) |
| Between composer steps | 1.2s (`StepDelayMs`) |
| Filled composer → Post click | **5–10s** (`PreClickPostMinMs`/`MaxMs`) |
| After the link lands, for the preview card | 6s (`LinkPreviewWaitMs`) |

The first-post pause applies too, so a rerun resuming a part-finished article doesn't open
a composer the instant it launches. Clicks are humanized as well: a stepped cursor path to
a random point inside the control, with a hover dwell (`HumanizeInput`).

**If Facebook blocks you mid-run, the run stops.** After each post the tool reads any open
dialog for the rate-limit wording ("temporarily blocked", "action was blocked", "try again
later", …) and aborts instead of working through the remaining groups — progress is saved
per group, so a later rerun picks up exactly where it stopped. Matching is scoped to
dialogs on purpose: these group feeds are full of members *talking* about being blocked,
and scanning the whole page would abort on someone else's post text.

> ⚠️ **A note on Facebook's terms.** Any automation that touches the page is technically
> against Meta's ToS, and there's always *some* account risk. **You** log in interactively
> in a real Chrome — there is no programmatic login. But with `AutoClickPost` on (the
> default) the tool submits the post for you, which is unattended publishing: the behavior
> Meta's anti-automation systems target hardest. Set `"AutoClickPost": false` in
> `appsettings.json` to go back to reviewing and clicking Post yourself — everything else
> (paste, pacing, block detection, resume) behaves the same. Keep volume human-paced either
> way; randomized delays help, but they don't make 25 posts in 20 minutes look like a person.

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

Easiest: **double-click `start.cmd`** and pick **3) Run**. Or:
```
run.cmd                 (one post per group, auto-posts)
run.cmd -- --dry-run    (fill composers, never post, never mark — great for testing)
run.cmd -- links        (rewrite legacy ?page= links to canonical; no browser needed)
```
One post per group is the only behavior — there's no batch/multi-group mode. (An old
`single` argument on a scheduled task is harmless; it's simply ignored.)

> **Running from a terminal you typed into?** On systems where Windows sets
> `NoDefaultCurrentDirectoryInExePath`, cmd won't run a script in the current folder by
> bare name — use `.\start.cmd` / `.\run.cmd` (with the `.\`) or the full path.
> Double-clicking `start.cmd` in Explorer is unaffected.

**Recommended first time:** log in (menu **1**), then do a **dry run** (menu **4**) to
watch it fetch the text, open the composer, and paste everything — without posting. Check
`screenshots/` if anything looks off. Dry-run never clicks Post regardless of
`AutoClickPost`, and pauses after each group so you can discard the draft yourself.

For each group you'll see:
```
— Group 3/12: Christianity Vs Mormonism
  · pausing 22.4s before composing…
  · pasting 412 chars (Ctrl+V)…
  · source url present as the last line: https://www.ldsapologetics.com/abrahamstest/
  · waiting for link preview…
  ✓ composer filled. Give it a look, then post it yourself.
  · reading it over for 7.1s, then clicking Post…
  · clicked Post.
  ✓ posted to Christianity Vs Mormonism (3/12).
```
Press **s** at any point to skip the current group — it stays un-posted and the next run
picks it up.

---

## The plan (`posts.json`)

Groups are shared (you always post to the same ones), so they live **once** at the top.
Each article gets its own separate post in every one of them.

```jsonc
{
  "Groups": [
    { "Name": "My LDS Group", "Url": "https://www.facebook.com/groups/000000000000000" }
    // Url is what's posted to; Name is how the console and validation report label it.
  ],
  "Articles": [
    {
      "Id": "Blood-Atonement", // unique; used in screenshot names
      "Link": "https://www.ldsapologetics.com/blood-atonement/",   // required, canonical
      "ImagePath": null,       // optional absolute path to an image
      "PostedAtUtc": null      // set automatically once posted to ALL groups
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

So the post leads with the hook and closes with the payoff, then the `Link` is appended as
the **last line** so Facebook renders its preview card. The Verdict line is skipped if it
just restates the lede. Add an optional `Message` to an article only if you want to
override all of this — the URL is still appended below it.

### Link form: canonical, not `?page=`

The legacy `?page=Slug` URL still works but **301-redirects**, which is worth avoiding in a
post: the reader sees a URL that isn't where they land, and the preview card is built off
the redirect. The canonical form is lowercase-slug-with-trailing-slash on the **www** host
(the apex 404s on article paths):

```
https://www.ldsapologetics.com/?page=AbrahamsTest   →   https://www.ldsapologetics.com/abrahamstest/
```

Every run rewrites the plan's links to that form and saves the file, so it self-heals. To
do it up front without a browser: `run.cmd -- links` (add `--dry-run` to preview). Beyond
that, when the text fetch navigates to an article the tool adopts whatever URL the browser
actually landed on, and warns if a link redirects to the site root — that means the page is
gone, and it would otherwise post a link to the homepage.

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
| `Program.cs` | Entry point, modes, the per-group post loop |
| `ChromeSession.cs` | Launch/attach Chrome over CDP |
| `Poster.cs` | Compose one post per group (candidate selectors) |
| `PostItem.cs` | `Plan`/`Article`/`Group` model + load/save |
| `Links.cs` | `?page=` → canonical URL rewriting (the `links` mode) |
| `Config.cs` | `appsettings.json` loader |
| `posts.example.json` | Sample plan (copy to `posts.json`) |
| `launch-chrome.cmd` | Manual Chrome launch for `AttachOnly` mode |
| `run.cmd` | Pass-through wrapper |
| `start.cmd` | **Easiest: double-click** — login / run / dry-run / scrape menu |

## How a post is assembled (the Poster flow)

For each group, `Poster.PrepareAsync` does, in this exact order:

1. Navigate to the group and open its **Create post** composer.
2. Attach the image (if `ImagePath` is set) — before the text, because Facebook reflows the
   composer footer once there's content and a link preview. A remote `ImagePath` URL is
   downloaded once per run and reused for every group.
3. **Paste the body** (see below), then confirm the URL is the last line and wait for the
   link preview card to render.
4. Pause 5–10s, then click **Post** (unless `AutoClickPost` is false, in which case it waits
   for you), and confirm by watching the post box close.

It writes screenshots at each step to `screenshots/` (`before-photo-btn`, `prepared`,
plus `*-fail` shots), which is the fastest way to see where a selector drifted. Groups
whose composer couldn't be prepared are appended to `failed-groups.log`.

### How the text gets in: one paste

The body goes on the clipboard (`navigator.clipboard.writeText`, with clipboard permission
granted once per run) and is pasted with a single **Ctrl+V** — exactly like drafting the
post elsewhere and pasting it in. Lexical's paste handler preserves the paragraph breaks,
and the whole post lands in one shot rather than as hundreds of keystroke events.

Two fallbacks, because clipboard access can be refused (e.g. the window isn't focused) and
Lexical occasionally ignores a key event mid-render:

1. a **synthetic paste event** carrying the same text as `clipboardData` — still a paste as
   far as the editor is concerned, just without the OS clipboard round trip;
2. **`InsertText` line by line**, accepted through Lexical's `beforeinput` handler.

Each stage verifies the box actually has text before moving on, and the console says which
one worked. After the text is in, `EnsureLinkAtEndAsync` checks the article URL survived
(tolerantly — ignoring scheme, `www.`, and a trailing slash, since Facebook normalizes what
it displays) and appends it if not.

## Maintenance note

Facebook's markup is obfuscated and shifts often, so the selectors in `Poster.cs` are
candidate lists matched by role/aria/text — expect to tune them. Things that have bitten
us and are worth knowing when they break again:

- **Composer text box.** Match it by its `Write something…` placeholder / `data-lexical-editor`
  and scope to the dialog — a looser `div[role='textbox']` also matches feed **comment boxes**
  sitting behind the dialog. The editor is a **Lexical** contenteditable; the filler pastes,
  verifies the box actually has text, and has two fallbacks if the paste doesn't take.
- **Post button.** Matched by `aria-label='Post'` or exact text only, never a loose
  `has-text('Post')` — other composer chrome contains that word, and a stray click in a
  dialog that is about to publish has no undo. It's also checked for `aria-disabled` (Post
  stays disabled while an image upload or link preview resolves) and re-resolved after the
  5–10s pause, since a preview card landing mid-wait re-renders the dialog and detaches the
  old handle.
- **Wait for the dialog, not just the box.** The composer is matched only after
  `aria-label='Create post'` is visible; hunting for the editor mid-animation used to grab
  a comment box still painted behind the overlay.
- **The "Add groups" picker is gone on purpose.** If you ever reintroduce it: every picker
  action must be **scoped to the dialog** (an unscoped search box matches Facebook's global
  top search bar, and clicking a result navigates the page away), the row
  **checkbox/button** is the toggle rather than the name span, and its list is virtualized
  so a row doesn't exist in the DOM until you filter to it. That whole class of breakage is
  what one-post-per-group avoids.

`--dry-run` + `screenshots/` make diagnosis quick.

## Never commit
`.fb-profile/` holds your logged-in session/cookies (git-ignored). So is `posts.json`
(your real content/groups). Don't override those ignores.
