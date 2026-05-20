# LDS Discussions Info — Full Content Build Plan

## Project Overview

**Site:** ldsdiscussions.info (WEBSITE_ID=6)  
**Purpose:** Scholarly one-for-one refutation of ldsdiscussions.com. Every page they have, we answer. Same menu structure. Same topic order. Better evidence.  
**Tone:** Academic. Citation-based. No emotional language. The refutation is more believable, more logical, and better sourced than the original claim. Every assertion is demonstrated and referenced.  
**Tool:** Claude uses the `webcms` MCP tools to create pages, write HTML, and publish. WEBSITE_ID must be 6.

---

## Workflow Per Page

Use `create_page_with_article` (single atomic call) — not the individual tools.  
This tool holds a semaphore so parallel agents cannot interleave DB writes.

1. `create_page_with_article` — creates article + page, uploads HTML to S3, links them, publishes both. Returns `pageId` and `articleId`.
2. `add_menu_item` — wire into navigation using the returned `pageId`

**Do not** call `create_article` / `create_page` / `update_page` / `publish_article` / `publish_page` individually for new pages — use the composite tool.

---

## HTML Styling (all article content)

Inline styles only — white/scholarly palette:

```
H1:        font-size:1.8em; font-weight:bold; color:#1a1f2e; margin:0 0 8px 0; font-family:Georgia,serif
H2:        font-size:1.3em; font-weight:bold; color:#1a1f2e; border-bottom:2px solid #c8a84b; padding-bottom:6px; margin:24px 0 14px 0
H3:        font-size:1.05em; font-weight:bold; color:#1a1f2e; margin:0 0 10px 0
Paragraph: font-size:0.97em; line-height:1.8; color:#1a1a1a; margin:0 0 14px 0; font-family:Georgia,serif
Blockquote (claim being refuted):
           border-left:3px solid #c8a84b; background:#fafafa; margin:20px 0;
           padding:12px 20px; color:#444; font-style:italic; font-size:0.97em
Citation:  font-size:0.82em; color:#555; margin:0 0 8px 0; font-family:Arial,sans-serif
```

---

## Complete Menu Structure (mirroring ldsdiscussions.com exactly)

```
Home
├── Summary of Church Historical Topics
└── Book of Mormon Study Guide

Overview Project
  (landing page linking to all 39 overview topics)

Blog
├── Please Don't Look Under the Hood
├── The Prophet's New Clothes
├── Navigating Doubts Through Fear (Renlund)
├── Rasband Devotional on Doubts and Suicide
├── FAIR Mormon: Homewreckers for Profit
├── Rebranding Revelation in the LDS Church
├── About the Church's LGBT Reversal
├── Not Blind Faith, but Big Faith (BYU-I)
├── The Joseph Smith "Now You Know" Video
├── Ensign Peak Investment Fund and Tithing
├── Follow the Footnotes
├── It's OK To Let Go
├── Doubts Are Not Dangerous
├── Thank You President Nelson (LGBT Policy)
├── Am I True? BYU Idaho Devotional Recap
├── Our Favorite Podcasts on Mormonism
├── The Church Does Not Take a Position
├── Tad Callister: Strawman Slayer
└── Faith Crisis in the Mormon Church Report

Book of Mormon
├── Joseph Smith and Treasure Digging (Part 1)
├── Book of Mormon Overview: The Gold Plates (Part 2)
├── Book of Mormon Overview: The Translation (Part 3)
├── Book of Mormon Overview: The Lost 116 Pages (Part 4)
├── Book of Mormon Overview: DNA and the BoM (Part 5)
├── Book of Mormon Overview: Surrounding Influences (Part 6)
├── Book of Mormon Overview: Anachronisms (Part 7)
├── Book of Mormon Overview: Tight vs Loose Translation (Part 8)
├── Book of Mormon Overview: How the BoM was Composed (Part 9)
├── LDS Expanded Essay — Translation (Annotated)
├── LDS Expanded Essay — DNA Studies (Annotated)
├── Book of Mormon Geography (Annotated Essay)
├── Faith Promoting Stories
├── Official Seer Stone "Now You Know" Video Response
├── FAIR Mormon: LOL Seer Stones Are Awesome (Response)
└── Official Gold Plates "Now You Know" Video Response

Biblical Scholarship
├── The Book of Mormon and Adam and Eve
├── The Global Flood and Mormonism
├── The Book of Mormon and Tower of Babel
├── The King James Bible and the Book of Mormon
├── The Sermon on the Mount and the Book of Mormon
├── The Long Ending of Mark and the Book of Mormon
└── Deutero-Isaiah and the Book of Mormon

Polygamy
├── Overview of Polygamy, Part 1: Background and D&C 132
├── Overview of Polygamy, Part 2: Proposals and Key Marriages
├── Overview of Polygamy, Part 3: Spiritual Wifery and Conclusion
├── LDS Expanded Essay — Polygamy in Kirtland and Nauvoo (Annotated)
├── LDS Expanded Essay — Polygamy in Utah (Annotated)
├── What D&C 132 Reveals About Revelation
└── Joseph Smith and the Happiness Letter

Book of Abraham / JST / D&C
├── Book of Abraham Overview, Part 1: Translation and Source Materials
├── Book of Abraham Overview, Part 2: The Text
├── Overview of the Word of Wisdom
├── Overview of Changes to Revelations in the D&C
├── Overview of the Kinderhook Plates
├── Overview of Joseph Smith's Translations
├── Overview of the Temple Endowment and Masonry
├── LDS Expanded Essay — Book of Abraham (Annotated)
├── FAIR Mormon: Book of Abraham vs the CES Letter (Response)
├── Joseph Smith Translation Problems
├── Saints: The Standard of Truth? (Chapter-by-Chapter Review)
├── Masonry and the Temple "Now You Know" Video Response
└── I Cannot Read a Sealed Book

Church History
├── Overview of First Vision Accounts
├── Overview of the Priesthood Restoration
├── Overview of Race in Mormon Scriptures
├── Overview of Spiritual Witnesses and Testimonies
├── Overview of How the Church Handles Doubt
├── Revelation Overview, Part 1: Backdating Prophecy
├── Revelation Overview, Part 2: Joseph Smith's Revelations
├── Revelation Overview, Part 3: After Joseph Smith
├── Revelation Overview, Part 4: Personal Revelation
├── The Transfiguration of Brigham Young
├── Russell M. Nelson's Miracle Stories
├── Overview of Apologetics within Mormonism
├── Overview: If Joseph Smith Got It Right, Then Who Got It Wrong?
├── Overview: Final Summary on Mormonism and What Now?
├── LDS Expanded Essay — First Vision Accounts (Annotated)
├── The First Vision "Now You Know" Video Response
├── First Vision Accounts: Church News Response
├── LDS Expanded Essay — Race and the Priesthood (Annotated)
├── What LDS Scriptures Teach About Race Today
├── Come Follow Me: The Curse of Dark Skin
├── Timeline and Quotes on the Priesthood Ban
└── What if Skin Doesn't Mean Skin? (Response)
```

**Total pages: ~87**

---

## Task List (in order)

## Resuming on a New Machine

1. Pull the repo
2. Set env vars (see CLAUDE.md for full commands):
   - `WEBSITE_ID=6`
   - `MCP_API_KEY` (retrieve from deployed Lambda via CLAUDE.md instructions)
   - `LAMBDA_API_BASE_URL=https://usmczy4mu1.execute-api.us-west-2.amazonaws.com/prod`
3. `cd api/mcp && npm install`
4. Restart VS Code — the `webcms` MCP server starts automatically
5. Open this file and tell Claude: **"continue the ldsdiscussions build — next pages are listed under CURRENT STATUS"**

---

## CURRENT STATUS (last updated 2026-05-20)

### ⚠️ CRITICAL: ldsdiscussions.com BLOCKS WebFetch from agents
Use `mcp__webcms__fetch_source_page` for all ldsdiscussions.com URLs. WebFetch for all other sources (FAIR, Interpreter, BYU Studies, etc.) works fine.

### ⚠️ KNOWN ISSUE: create_page_with_article 500 errors
The first failed batch (permissions-blocked) left orphaned stub pages in the DB. If `create_page_with_article` returns a 500 error, agents should fall back to individual tools: `create_article` → `set_article_content` → `create_page` → `update_page` → `publish_article` → `publish_page`. This pattern worked reliably.

### ⚠️ PERMISSIONS REQUIRED
Ensure `.claude/settings.json` allow list includes:
- `mcp__webcms__fetch_source_page`
- `mcp__webcms__create_page_with_article`
- `WebSearch`
- `WebFetch`

### Pages Published So Far
| Page | ID | Status |
|------|----|--------|
| Home (article 595) | 224 | ✅ published |
| Treasure Digging | 507 | ✅ published |
| Gold Plates | 516 | ✅ published |
| Translation | 529 | ✅ published |
| Lost 116 Pages | 515 | ✅ published |
| DNA & Lamanites | 512 | ✅ published |
| Surrounding Influences | 506 | ✅ published |
| Anachronisms | 522 | ✅ published |
| Tight vs Loose | 521 | ✅ published |
| BOM Authorship | 510 | ✅ published |
| LDS Essay Translation (BOM-10) | 536 | ✅ published |
| LDS Essay DNA (BOM-11) | 557 | ✅ published |
| BOM Geography (BOM-12) | 552 | ✅ published |
| Faith Promoting Stories (BOM-13) | 574 | ✅ published |
| Adam and Eve / BOM (BS-1) | 580 | ✅ published |
| Global Flood (BS-2) | 572 | ✅ published |
| Tower of Babel (BS-3) | 553 | ✅ published |
| KJV and BOM (BS-4) | 578 | ✅ published |
| Sermon on the Mount (BS-5) | 586 | ✅ published |
| Long Ending of Mark (BS-6) | 577 | ✅ published |
| Deutero-Isaiah (BS-7) | 583 | ✅ published |

### Menu IDs
| Section | ID |
|---------|----|
| Home (leaf) | 1 |
| Overview Project | 2 |
| Blog | 3 |
| Book of Mormon | 4 |
| Biblical Scholarship | 5 |
| Polygamy | 6 |
| Book of Abraham / JST / D&C | 7 |
| Church History | 8 |

### Pages Published So Far (continued)
| Page | ID | Status |
|------|----|--------|
| Polygamy Part 1 (POL-1) | 599 | ✅ published |
| Polygamy Part 2 (POL-2) | 600 | ✅ published |
| Polygamy Part 3 (POL-3) | 592 | ✅ published |
| LDS Essay Polygamy Kirtland/Nauvoo (POL-4) | 595 | ✅ published |
| LDS Essay Polygamy Utah (POL-5) | 598 | ✅ published |
| D&C 132 Revelation (POL-6) | 591 | ✅ published |
| Happiness Letter (POL-7) | 590 | ✅ published |

### Correct URLs Found (update agent prompts accordingly)
- /polygamy → POL-1 overview
- /polygamy-proposals → POL-2 proposals
- /polygamy-final → POL-3 spiritual wifery
- /polygamy-nauvoo → POL-4 Kirtland/Nauvoo essay
- /polygamy-utah → POL-5 Utah essay
- /blog-revelation-and-dc132 → POL-6 D&C 132
- /happiness-letter → POL-7

### Pages Published So Far (continued)
| Page | ID | Status |
|------|----|--------|
| Book of Abraham Part 1 (BOA-1) | 613 | ✅ published |
| Book of Abraham Part 2 (BOA-2) | 612 | ✅ published |
| Word of Wisdom (BOA-3) | 611 | ✅ published |
| D&C Changes (BOA-4) | 610 | ✅ published |
| Kinderhook Plates (BOA-5) | 602 | ✅ published |
| JS Translations (BOA-6) | 605 | ✅ published |
| Temple/Masonry (BOA-7) | 614 | ✅ published |

### Correct URLs Found (BOA series)
- /abraham-translation → BOA-1
- /abraham-text → BOA-2
- /wow → BOA-3
- (search required) → BOA-4
- /kinderhook-plates → BOA-5
- /translations → BOA-6
- /temple → BOA-7

### ▶ RESUME HERE — Next — Book of Abraham remaining (6 pages, parentId:7)
- BOA-8: LDS-Essay-Book-of-Abraham (parentId:7)
- BOA-9: FAIR-Mormon-BOA-CES-Letter (parentId:7)
- BOA-10: JST-Problems-Response (parentId:7)
- BOA-11: Saints-Standard-Truth-Review (parentId:7)
- BOA-12: Masonry-Temple-Video-Response (parentId:7)
- BOA-13: Cannot-Read-Sealed-Book (parentId:7)

---

### ✅ ITERATION 0 — Site Setup (done)
- [x] Site created, BaseStyles.css deployed
- [x] Header image, favicon uploaded
- [x] theme.css, header.html, sitemenu.json uploaded to S3
- [x] Home page (ID 224) exists in DB

---

### ITERATION 1 — Home Page Article

**Goal:** Write and publish the Home page intro content (mirroring ldsdiscussions.com's 4-paragraph intro only — no "Latest additions" list yet).

Content to write (faithfully reframed):
- Site title: "LDS Discussions — Done the Right Way"
- "NEW: Overview Project" paragraph
- Welcome / mission paragraph
- LDS Essays annotation paragraph
- Saints book review paragraph

- [ ] Write Home Hook article HTML
- [ ] `create_article` (name: "Home Hook", articlePath: "Home-Hook.html")
- [ ] `set_article_content` with HTML
- [ ] `update_page` (ID 224) — link article, sequence_no: 5
- [ ] `publish_article`
- [ ] `publish_page` (ID 224)

---

### ITERATION 2 — Top-Level Menu Structure

**Goal:** Build the nav skeleton before adding child pages.

- [ ] `add_menu_item` — Home (parent:0, droppable:false, pageId:224, pageName:"Home")
- [ ] `add_menu_item` — Overview Project (parent:0, droppable:true)
- [ ] `add_menu_item` — Blog (parent:0, droppable:true)
- [ ] `add_menu_item` — Book of Mormon (parent:0, droppable:true)
- [ ] `add_menu_item` — Biblical Scholarship (parent:0, droppable:true)
- [ ] `add_menu_item` — Polygamy (parent:0, droppable:true)
- [ ] `add_menu_item` — Book of Abraham / JST / D&C (parent:0, droppable:true)
- [ ] `add_menu_item` — Church History (parent:0, droppable:true)

---

### ITERATION 3 — Home Subpages

- [ ] Summary of Church Historical Topics
- [ ] Book of Mormon Study Guide

---

### ITERATION 4 — Overview Project Landing Page

**Goal:** One landing page listing all 39 response topic links (populated as pages are built).

- [ ] Create Overview Project landing page + article
- [ ] Add to menu under Overview Project

---

### ITERATION 5 — Book of Mormon (16 pages)

For each: create article → write scholarly refutation HTML → upload → create page → link → publish → add to menu

- [ ] BOM-1: Joseph Smith and Treasure Digging
- [ ] BOM-2: The Gold Plates
- [ ] BOM-3: The Translation
- [ ] BOM-4: The Lost 116 Pages
- [ ] BOM-5: DNA and the Lamanites
- [ ] BOM-6: Surrounding Influences
- [ ] BOM-7: Anachronisms
- [ ] BOM-8: Tight vs Loose Translation Theories
- [ ] BOM-9: How the Book of Mormon was Composed
- [ ] BOM-10: LDS Essay — Translation (Annotated)
- [ ] BOM-11: LDS Essay — DNA Studies (Annotated)
- [ ] BOM-12: Book of Mormon Geography Essay
- [ ] BOM-13: Faith Promoting Stories
- [ ] BOM-14: Seer Stone Video Response
- [ ] BOM-15: FAIR Mormon Seer Stones Response
- [ ] BOM-16: Gold Plates Video Response

---

### ITERATION 6 — Biblical Scholarship (7 pages)

- [ ] BS-1: Adam and Eve and the Book of Mormon
- [ ] BS-2: The Global Flood and Mormonism
- [ ] BS-3: Tower of Babel and the Book of Mormon
- [ ] BS-4: The King James Bible and the Book of Mormon
- [ ] BS-5: Sermon on the Mount and the Book of Mormon
- [ ] BS-6: Long Ending of Mark and the Book of Mormon
- [ ] BS-7: Deutero-Isaiah and the Book of Mormon

---

### ITERATION 7 — Polygamy (7 pages)

- [ ] POL-1: Polygamy Overview Part 1 — Background and D&C 132
- [ ] POL-2: Polygamy Overview Part 2 — Proposals and Key Marriages
- [ ] POL-3: Polygamy Overview Part 3 — Spiritual Wifery and Conclusion
- [ ] POL-4: LDS Essay — Polygamy in Kirtland and Nauvoo (Annotated)
- [ ] POL-5: LDS Essay — Polygamy in Utah (Annotated)
- [ ] POL-6: What D&C 132 Reveals About Revelation
- [ ] POL-7: Joseph Smith and the Happiness Letter

---

### ITERATION 8 — Book of Abraham / JST / D&C (13 pages)

- [ ] BOA-1: Book of Abraham Part 1 — Translation and Source Materials
- [ ] BOA-2: Book of Abraham Part 2 — The Text
- [ ] BOA-3: Overview of the Word of Wisdom
- [ ] BOA-4: Overview of Changes to Revelations in the D&C
- [ ] BOA-5: Overview of the Kinderhook Plates
- [ ] BOA-6: Overview of Joseph Smith's Translations
- [ ] BOA-7: Overview of the Temple Endowment and Masonry
- [ ] BOA-8: LDS Essay — Book of Abraham (Annotated)
- [ ] BOA-9: FAIR Mormon — Book of Abraham vs CES Letter (Response)
- [ ] BOA-10: Joseph Smith Translation Problems
- [ ] BOA-11: Saints: The Standard of Truth? (Chapter Review)
- [ ] BOA-12: Masonry and the Temple Video Response
- [ ] BOA-13: I Cannot Read a Sealed Book

---

### ITERATION 9 — Church History (22 pages)

- [ ] CH-1: Overview of First Vision Accounts
- [ ] CH-2: Overview of the Priesthood Restoration
- [ ] CH-3: Overview of Race in Mormon Scriptures
- [ ] CH-4: Overview of Spiritual Witnesses and Testimonies
- [ ] CH-5: Overview of How the Church Handles Doubt
- [ ] CH-6: Revelation Part 1 — Backdating Prophecy
- [ ] CH-7: Revelation Part 2 — Joseph Smith's Revelations
- [ ] CH-8: Revelation Part 3 — After Joseph Smith
- [ ] CH-9: Revelation Part 4 — Personal Revelation
- [ ] CH-10: The Transfiguration of Brigham Young
- [ ] CH-11: Russell M. Nelson's Miracle Stories
- [ ] CH-12: Overview of Apologetics within Mormonism
- [ ] CH-13: If Joseph Smith Got It Right, Then Who Got It Wrong?
- [ ] CH-14: Final Summary on Mormonism and What Now?
- [ ] CH-15: LDS Essay — First Vision Accounts (Annotated)
- [ ] CH-16: First Vision "Now You Know" Video Response
- [ ] CH-17: First Vision Accounts — Church News Response
- [ ] CH-18: LDS Essay — Race and the Priesthood (Annotated)
- [ ] CH-19: What LDS Scriptures Teach About Race Today
- [ ] CH-20: Come Follow Me — The Curse of Dark Skin
- [ ] CH-21: Timeline and Quotes on the Priesthood Ban
- [ ] CH-22: What if Skin Doesn't Mean Skin?

---

### ITERATION 10 — Blog (19 posts)

- [ ] BLOG-1: Please Don't Look Under the Hood
- [ ] BLOG-2: The Prophet's New Clothes
- [ ] BLOG-3: Navigating Doubts Through Fear (Renlund)
- [ ] BLOG-4: Rasband Devotional on Doubts and Suicide
- [ ] BLOG-5: FAIR Mormon: Homewreckers for Profit
- [ ] BLOG-6: Rebranding Revelation in the LDS Church
- [ ] BLOG-7: About the Church's LGBT Reversal
- [ ] BLOG-8: Not Blind Faith, but Big Faith (BYU-I)
- [ ] BLOG-9: The Joseph Smith "Now You Know" Video
- [ ] BLOG-10: Ensign Peak Investment Fund and Tithing
- [ ] BLOG-11: Follow the Footnotes
- [ ] BLOG-12: It's OK To Let Go
- [ ] BLOG-13: Doubts Are Not Dangerous
- [ ] BLOG-14: Thank You President Nelson (LGBT Policy)
- [ ] BLOG-15: Am I True? BYU Idaho Devotional Recap
- [ ] BLOG-16: Our Favorite Podcasts on Mormonism
- [ ] BLOG-17: The Church Does Not Take a Position
- [ ] BLOG-18: Tad Callister: Strawman Slayer
- [ ] BLOG-19: Faith Crisis in the Mormon Church Report

---

## Verification Checklist (after each iteration)

- `search_pages` — confirm page exists in DB
- `get_page` — confirm article is linked
- `get_menu` — confirm menu item appears
- Visit ldsdiscussions.info/[pageName] in browser
- CloudFront invalidation if styles/layout changed:
  `aws cloudfront create-invalidation --distribution-id E3VCKB95QSPEA6 --paths "/*" --profile tbird321`

---

## Agent Research Template

See **LDS-DISCUSSIONS-AGENT-TEMPLATE.md** at the repo root for the full agent prompt template.

Each content agent:
1. Fetches the ldsdiscussions.com source page (their exact argument)
2. Searches FAIR Mormon, Book of Mormon Central, Interpreter Foundation, Maxwell Institute,
   BYU Studies, LDS Essays, Gospel Library, and other apologetic sources
3. Fetches and reads the actual content of the best pages found
4. Synthesizes the strongest composite argument from all sources
5. Writes an EPIC, irrefutable, evidence-based HTML response — definitive, not casual
6. Publishes via `create_page_with_article` + `add_menu_item`

**Content standard:** Every claim cited. Every citation real. Engage their strongest argument.
Defeat it with better evidence. Leave no major claim unaddressed.

---

## Key References

| Item | Value |
|------|-------|
| WEBSITE_ID | 6 |
| Home Page DB ID | 224 |
| CloudFront Distribution | E3VCKB95QSPEA6 |
| S3 Build Bucket | www.ldsdiscussions.info |
| S3 Content Bucket | www-websitecontent |
| Article S3 Path | public/websites/ldsdiscussions.info/articles/ |
| Asset S3 Path | public/assets/ldsdiscussions.info/ |
| Deploy Profile | tbird321 |
