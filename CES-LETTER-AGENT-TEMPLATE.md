# CES Letter — Content Agent Template

This template is the same as **LDS-APOLOGETICS-AGENT-TEMPLATE.md** with the following overrides.
Read that file for the full source material, voice rules, and research approach.
Only the differences are documented here.

---

## KEY DIFFERENCES FROM APOLOGETICS TEMPLATE

### WEBSITE_ID
```
WEBSITE_ID = 8  (cesletter.info — NOT 5)
```

### BATCH SIZE LIMIT
Same — **2–3 pages per agent session maximum.**

---

## CMS WORKFLOW (same as apologetics template)

### SHORT WORKFLOW — Phase 1–3 (existing articles)
```
1. get_article_content(articleId)
2. apply changes per batch spec
3. set_article_content(articleId, htmlContent)
4. get_article(articleId) → verify
```

### FULL SEQUENCE — Phase 4+ (new pages)
```
1.  search_pages(pageName)
2.  search_articles(articleName)
3.  create_page(pageName)                          → record pageId
4.  create_article(articleName, articlePath:"[Slug].html")  → record articleId
5.  set_article_content(articleId, htmlContent)
6.  update_page(pageId, articles:[articleId])
7.  publish_article(articleId)
8.  publish_page(pageId)
9.  get_menu() → find parentId for target section
10. add_menu_item(parentId, itemTitle, pageId, pageName)
11. get_page(pageId) → verify
```

**Pre-flight:** `search_pages` + `search_articles` before every create.
**Naming:** pageName = articleName = exact slug. articlePath = `[Slug].html`.

---

## STEP 1 — READ THE SOURCE ARGUMENT (unique to this site)

Before writing, fetch the corresponding page on **read.cesletter.org**:

```
WebFetch: https://read.cesletter.org/[chapter-slug]/
```

Read their exact argument carefully. List every specific claim they make before proceeding.
This is what the article is responding to — quote it accurately using the blockquote style below.

> ⚠️ If WebFetch fails on read.cesletter.org, use `mcp__webcms__fetch_source_page` as fallback.

---

## STYLING (cesletter.info — different from apologetics site)

Colors: brand red `#f93724` | body text `#2c3e50` | border `#eaecef` | link blue `#377dff`
Font: `'Open Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif`

```
H1:   font-size:2.2rem; font-weight:600; color:#2c3e50; line-height:1.25; margin:0 0 0.5rem 0; font-family:'Open Sans',sans-serif
H2:   font-size:1.65rem; font-weight:600; color:#2c3e50; border-bottom:1px solid #eaecef; padding-bottom:0.3rem; margin:2rem 0 1rem 0; font-family:'Open Sans',sans-serif
H3:   font-size:1.35rem; font-weight:600; color:#2c3e50; margin:1.5rem 0 0.75rem 0; font-family:'Open Sans',sans-serif
P:    font-size:1rem; line-height:1.7; color:#2c3e50; margin:0 0 1rem 0; font-family:'Open Sans',sans-serif
Lede: font-size:1.05rem; font-style:italic; color:#2c3e50; margin:0 0 1.5rem 0; font-family:'Open Sans',sans-serif
```

**CES Letter quote blockquote** (red border — quoting Jeremy's argument):
```html
<blockquote style="border-left:4px solid #f93724; background:#fff8f7; margin:20px 0; padding:12px 20px; color:#2c3e50; font-style:italic; font-size:1rem; line-height:1.7;">
  <p style="font-size:0.78em; font-weight:600; letter-spacing:0.18em; text-transform:uppercase; color:#f93724; margin:0 0 6px 0; font-family:'Open Sans',sans-serif;">CES Letter — [Section Name]</p>
  <p style="margin:0; font-family:'Open Sans',sans-serif;">&ldquo;Jeremy's exact words here.&rdquo;</p>
</blockquote>
```

**Red section title bar** (chapter/section headers):
```html
<div style="background:#f93724; color:#ffffff; padding:1rem 1.5rem; font-weight:600; text-transform:uppercase; letter-spacing:0.05em; font-family:'Open Sans',sans-serif; margin:0 0 1.5rem 0;">Section Title</div>
```

**Reference blockquote** (blue — supporting scholarly sources):
```html
<blockquote style="background:#f0f4f8; border-left:2px solid #2c4a6e; padding:12px 16px; margin:0 0 20px 0;">
  <p style="font-size:0.75em; font-weight:600; letter-spacing:0.2em; text-transform:uppercase; color:#2c4a6e; margin:0 0 6px 0; font-family:'Open Sans',sans-serif;">Label</p>
  <ul style="margin:0; padding-left:18px; color:#2c3e50; font-size:0.9em; line-height:1.65; font-family:'Open Sans',sans-serif;">
    <li style="margin-bottom:5px;"><strong style="color:#2c4a6e;">Ref</strong> &mdash; Description</li>
  </ul>
</blockquote>
```

Scripture links: `<a href="https://www.churchofjesuschrist.org/study/scriptures/..." target="_blank" rel="noopener" style="color:#377dff;">ref</a>`
No `<hr>` tags. Use `&ldquo;`/`&rdquo;` for quotes, `&mdash;` for em dashes.

---

## ARTICLE STRUCTURE

```html
<h1 style="...">Response to: [CES Letter Section Title]</h1>
<p style="lede...">One sentence stating what Jeremy claims and what the evidence actually shows.</p>

<div style="red title bar...">The Claim</div>
[Blockquote Jeremy's argument — his exact words, fairly represented]

<div style="red title bar...">What the Evidence Actually Shows</div>
[Core response with citations — engage the strongest version of the argument]

<div style="red title bar...">The Scholarly Consensus</div>
[What LDS and non-LDS scholars agree on — cite FAIR, Interpreter, Maxwell Institute, etc.]

<div style="red title bar...">The Verdict</div>
[Explicit statement: where the evidence lands, what remains genuinely uncertain, what is resolved]
```

---

## VOICE (same as apologetics template — key rules)

**Rule 0:** Frame resolution as what Christ taught / what the early Church practiced — not "what the LDS Church teaches."

**Rule 1:** Socratic trapping — rhetorical questions that lead the critic into logical corners.

**Rule 2:** Confident. Don't hedge on things the evidence supports.

**Rule 3:** Stack scripture chapter and verse.

**Rule 4:** Quote church fathers, creeds, historians directly — opponents cannot dismiss their own sources.

**Rule 5:** Every article delivers a VERDICT — explicit, stated, proved, closed.

**Rule 6:** Rhetorical questions as weapons — ask, then answer.

**Rule 7:** Write for the doubting member, not for Jeremy.

---

## RESEARCH SOURCES (same as LDS-APOLOGETICS-AGENT-TEMPLATE.md)

See `LDS-APOLOGETICS-AGENT-TEMPLATE.md` Section "SOURCE MATERIAL" for the full list. In brief:

1. **Facebook Posts** — `E:\FacebookDownload\facebook-TheronBird-2025-04-11-jkcNtL2U\your_facebook_activity\groups\group_posts_and_comments.html`
2. **Scripture** — Gospel Library (`churchofjesuschrist.org/study/scriptures`)
3. **LDS Apologetic Sources** — FAIR (`fairlatterdaysaints.org`), Interpreter, Maxwell Institute, BYU Studies, Book of Mormon Central, Scripture Central, Gospel Topics Essays
4. **Early Church Fathers** — CCEL (`ccel.org`), New Advent (`newadvent.org/fathers`), Early Christian Writings
5. **Creeds & Confessions** — Nicene, Westminster, Augsburg, Catholic Catechism (see template for URLs)
6. **Historical/Academic** — Joseph Smith Papers, Church History Library, JSTOR, Google Scholar

---

## KNOWN MENU IDs (cesletter.info — fill in as built)

| Section | Parent ID |
|---------|-----------|
| *(call get_menu at session start)* | |

---

## REPORT BACK (end of every session)

- Pages created: title, pageId, articleId, menuId
- Source URL fetched from read.cesletter.org
- Key claims addressed
- Any errors or fallback patterns
- Suggested checkbox updates for CES-LETTER-PLAN.md
