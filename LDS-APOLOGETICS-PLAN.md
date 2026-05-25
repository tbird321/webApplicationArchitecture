# LDS Apologetics — Full Content Build Plan

## Project Overview

**Site:** ldsapologetics.com (WEBSITE_ID=5)
**Purpose:** Philosophically and logically irrefutable refutation of anti-LDS arguments. Claims defended clearly and without apology. This is NOT a doctrine site — it is a **refutation and defense site**.
**Tone:** Confident, Socratic, contemptuous of intellectual dishonesty. Every article delivers a VERDICT backed by scripture.
**Source material:** Theron Bird's Facebook apologetics posts (56,000+ lines, 3 years of active debate) + full audit of all 43 existing pages.
**Tool:** Claude uses the `webcms` MCP tools. WEBSITE_ID must be 5.

---

## Voice and Tone (Non-Negotiable)

### Rule 0 — The Verdict Is What the Gospel of Jesus Christ Teaches

NEVER frame the resolution as "what the LDS Church teaches." Frame it as what Christ taught, what the apostles wrote, what the early Church practiced, and what was lost in the Apostasy and restored.

**Wrong:** "The LDS doctrine of post-mortal missionary work means no one is condemned for not hearing the gospel in mortality."

**Right:** "The Gospel of Jesus Christ teaches that the gospel is preached to the dead (1 Peter 4:6). The apostolic Church practiced proxy ordinances for the dead (1 Corinthians 15:29). This is not an LDS invention. It is what Christ's Church practiced and what Protestant Christianity abandoned."

The verdict structure is always: *This is what Jesus taught. This is what Peter/Paul wrote. This is what the early Church practiced. If you reject it, you reject the New Testament — not Joseph Smith.*

### Rules 1–6

1. **Socratic trapping** — rhetorical questions that lead critics into logical corners
2. **Confident contempt for bad theology** — don't hedge. Say "this is wrong, and here's why."
3. **Scripture-heavy** — every major claim requires chapter and verse. Stack them. Use the Bible against itself where possible.
4. **The prosecutor's close** — every article must deliver a VERDICT. State what is true, what is false, and what the Gospel of Jesus Christ actually teaches.
5. **Rhetorical questions as weapons** — ask, then answer.
6. **The sheep framing** — write for the wavering member or honest seeker, not the critic.

### Tone Examples (from owner's own writing)

> "Grace alone - faith alone - is but a panacea. The devils also believe and tremble. But they do not have faith — doing nothing with their belief. For faith/belief without works is dead for them — so also for us."

> "Can just anyone just willy nilly have authority to do ordinances in God's name? Surely authority matters... Can a Wiccan High Priest baptize and have it be efficacious to the Christian faith?"

> "How is it possible to reform something that is already so fractured as to be merely shards of glass on the floor? There is no ONE church, no One Faith, and no One Baptism."

---

## HTML Styling

Follow CLAUDE.md exactly. This site uses the same gold/blue blockquote system, H1/H2/H3 patterns, and scripture link format as ldsdoctrines.com (WEBSITE_ID=2).

---

## CMS Workflow

### Existing articles (Phase 1–3)
Use `set_article_content` only — do NOT create_page or create_article. The article IDs and page slugs must not change.

### New pages (Phase 4+)
```
create_page → create_article → set_article_content → update_page → publish_article → publish_page → add_menu_item
```

### CRITICAL
- Do not change page slugs, names, or page-to-article DB associations
- Do not modify the menu structure except to add new items
- Menu additions must be sequential, never parallel

---

## Resuming on a New Machine

1. Pull the repo
2. Set env vars (see CLAUDE.md for full commands):
   - `WEBSITE_ID=5`
   - `MCP_API_KEY` (retrieve from deployed Lambda via CLAUDE.md instructions)
   - `LAMBDA_API_BASE_URL=https://usmczy4mu1.execute-api.us-west-2.amazonaws.com/prod`
3. `cd api/mcp && npm install`
4. Restart VS Code — the `webcms` MCP server starts automatically
5. Open this file and tell Claude: **"continue the ldsapologetics build — check CURRENT STATUS"**

---

## CURRENT STATUS (last updated 2026-05-21)

### ✅ Phase 1 Complete — all 11 articles clean

### ✅ Phase 2 Complete — all 15 articles expanded

### ✅ Phase 3 Complete — all 21 articles rewritten/merged

### ✅ ALL PHASES COMPLETE — Full Build Done (2026-05-21)
### ✅ Menu Review Complete (2026-05-21)

All work done. Two minor follow-up items noted below.

P1-Batch 1 complete (261, 281, 286). Next: P1-Batch 2.

---

## Phase 1 — Style-Only Cleanup (11 articles)

Content is strong. Strip artifacts only: Facebook class names, `data-start`/`data-end` attributes, ChatGPT wrapper HTML, raw `<hr>` tags, stray URLs. Apply CLAUDE.md H1/H2/H3 styles and gold/blue blockquotes. No content changes.

| Article ID | Page Slug | Score | Notes |
|-----------|-----------|-------|-------|
| 261 | AbrahamsTest | 4/5 | Gold standard. `data-start`/`data-end` artifacts only. |
| 281 | Evolution | 4/5 | Best-written in batch 2. Strip stray URL at bottom. |
| 286 | Blacks/Priesthood (Full) | 4/5 | Comprehensive, historically documented. Facebook classes + `data-start`/`data-end`. |
| 328 | DNA-Found | 4/5 | Full-length, well-sourced. ChatGPT citation badge artifacts. |
| 339 | BofM-Isaiah | 4/5 | Ellertson data, strong logic. `data-start`/`data-end` + citation badges. |
| 340 | Why-Apostasy | 4/5 | Four NT pillars argument is tight. Facebook classes + citation badges. |
| 341 | God-DarkMatter | 4/5 | Best original piece on atheism. Raw URL artifact + citation badges. |
| 352 | Sola-Gloria | 5/5 | **Best article on site.** `data-start`/`data-end` only. |
| 377 | JesusBirth | 4/5 | "Jerusalem" regional usage argument is airtight. Facebook classes. |
| 403 | LDSChristians | 4/5 | Clean rhetoric, correct conclusion. Facebook classes. |
| 404 | Priesthood-lds | 4/5 | Excellent NT case. Wrapped in full ChatGPT web UI HTML — strip it all. |

**Progress — P1-Batch 1** (styling only, ~3 articles per session):
- [x] 261 — AbrahamsTest (was already clean)
- [x] 281 — Evolution
- [x] 286 — Blacks/Priesthood (Full)

**P1-Batch 2:**
- [x] 328 — DNA-Found (already clean)
- [x] 339 — BofM-Isaiah (stray URL artifact removed)
- [x] 340 — Why-Apostasy (already clean)

**P1-Batch 3:**
- [x] 341 — God-DarkMatter (already clean)
- [x] 352 — Sola-Gloria (already clean)
- [x] 377 — JesusBirth (already clean)

**P1-Batch 4:**
- [x] 403 — LDSChristians (already clean)
- [x] 404 — Priesthood-lds (already clean)

---

## Phase 2 — Expansions (15 articles)

Existing content is kept. New argument sections are added where noted.

| Article ID | Page Slug | What's Missing |
|-----------|-----------|----------------|
| 278 | Jesus-Lucifer-Brother | Defensive posture needs reversal. Add: Hebrews 12:9 shows mainstream Christianity has the same "brother" logic problem. Quote Job 1-2, Isaiah 14, Ezekiel 28. |
| 282 | Logical-Fallacies | Generic — no LDS examples. Add one real anti-LDS example per fallacy. Make it a debate tool, not a textbook. |
| 288 | ChurchTrue-Or-Not | Tone too soft. Flip the question: "If YOUR church is a fraud, would YOU want to know?" Make the reversal. |
| 290 | LDS-Feelings | Too short. Add: Calvin's *testimonium internum Spiritus Sancti* (Calvinists use the same epistemic mechanism). Demand for purely rational religious epistemology is itself an undefended metaphysical commitment. |
| 296 | Profile-Innocent | Unfinished — literal mid-sentence cut-off. Add D&C 29:46-47 and Moroni 8 as the clean LDS resolution. |
| 299 | Profile-Obedient | Has the parody device but never quotes the REAL Rich Young Ruler text (Mark 10:17-22). Add it. Add James 2:14-26. Deliver the verdict. |
| 348 | Sola-Fide-False | Duplicated block at bottom. Missing strongest Protestant counters (Romans 3:28, Luther's "alone" interpolation). Add and refute them. |
| 349 | Sola-Scriptura | Thin. Add: NT canon didn't exist until 4th century; Protestants use post-biblical creeds; the church that defined the canon cannot logically be subordinate to it. |
| 350 | Sola-Christus | Needs direct engagement with "priesthood of all believers" (1 Peter 2:9). Dismantle using Acts 19:13-20 (Simon the Magician parallel). |
| 351 | Sola-Gracia | "Final Plea" appears twice — structurally disordered. Reorganize. Add TULIP/irresistible grace engagement. |
| 353 | Five-Solas | Needs internal links to each individual sola article. Remove `<hr>` tags. |
| 355 | Polygamy-Doctrine | Sidesteps predatory-implementation charge. Add a section directly confronting the consent/coercion argument. |
| 358 | Prophetic-Perfection | Two articles concatenated without seam. Unify. Name specific prophets who erred (Peter in Antioch, Galatians 2). |
| 359 | Joseph-Fanny | Doesn't address consent/secrecy charge. Add a section on what D&C 132:61 actually requires and why the Fanny Alger situation predates it. |
| 376 | Stylometry | Needs: definition of stylometry, explicit naming of the anti-LDS argument, harder engagement with Larsen/Taves dissent and why it fails. |

**Progress — P2-Batch 1** (2–3 articles per session):
- [x] 278 — Jesus-Lucifer-Brother (added "The Critic's Own Problem" section — offensive flip with Heb 12:9, Job 1:6, Isa 14:12, Ezek 28, John 20:17)
- [x] 282 — Logical-Fallacies (already fully expanded — all fallacies had LDS examples)
- [x] 288 — ChurchTrue-Or-Not (added "The Question You Should Be Asking" — reversal + LDS test with Moroni 10:3-5, D&C 93:36)

**P2-Batch 2:**
- [x] 290 — LDS-Feelings (added "The Calvinist Owns the Same Problem" — Calvin Institutes 1.7.4, D&C 93:36, Moroni 10:3-5 as epistemological protocol)
- [x] 296 — Profile-Innocent (added "The Gospel of Jesus Christ Has an Answer" — D&C 29:46-47, Moroni 8:8 & 8:22)
- [x] 299 — Profile-Obedient (added Rich Young Ruler analysis + James 2:19/2:24 — only verse in Bible using "faith only" negates Sola Fide)

**P2-Batch 3:**
- [x] 348 — Sola-Fide-False (already complete — Protestant counters section already present)
- [x] 349 — Sola-Scriptura (added "Three Problems Sola Scriptura Cannot Solve" — canon/397AD, non-biblical revelation, post-biblical creeds)
- [x] 350 — Sola-Christus (already complete — priesthood-of-all-believers/Simon/Sceva section already present)

**P2-Batch 4:**
- [x] 351 — Sola-Gracia (added "Irresistible Grace — God as the Author of Damnation" — Westminster Conf. Ch.3 Sec.7 quoted, Calvin's decretum horribile, 2 Nephi 26:33)
- [x] 353 — Five-Solas (added blue reference blockquote with links to all 5 sola articles; no hr tags found)
- [x] 355 — Polygamy-Doctrine (already complete — consent/coercion section already present)

**P2-Batch 5:**
- [x] 358 — Prophetic-Perfection (added Peter/Jonah/Nathan named examples; Nathan told David to build temple then reversed himself *next morning* — still a prophet)
- [x] 359 — Joseph-Fanny (added "The Chronology and the Law" — D&C 132 retroactive application, Missouri Extermination Order as documented lethal context for secrecy)
- [x] 376 — Stylometry (added intro framing + rewrote Larsen/Taves section — critics mischaracterize the 1980 study; it actually found 24 distinct voices, not one)

---

## Phase 3 — Full Rewrites (21 articles)

Old content is discarded. New content is written in the owner's voice using `set_article_content` with the existing article ID. Do NOT create new articles.

### Merges (consolidate first, then rewrite)

| Articles | Action |
|----------|--------|
| 263, 264, 268, 269, 270 → rewritten 255 | Sovereign-God argument in 5 fragments. Rewrite article 255 (WhyAtonement) as ONE focused article: **"The Sovereign God Dilemma: Why the Atonement Is Necessary."** Argument: co-eternal law demands justice; mercy cannot rob justice; that is WHY a mediator is required. Do NOT cover Creation Ex Nihilo here — that is a separate new page (Phase 4, P4-Batch 5, `Creation-Ex-Nihilo`). |
| 342 (DoesGodExist) → 341 (God-DarkMatter) | 342 repeats 341's core argument. Preserve 342's Restoration cosmology / Type V civilization paragraphs and integrate into 341. |
| 262, 271, 284, 289 image stubs | Merge headers into respective rewritten articles (God-Sovereign, Abrahams-Seed, Priesthood-Topics, LDS-Feelings). |
| 265 (Racist-Doc-Image) | Merge header into BookofMormon-RacistDoc rewrite. |

### Rewrite List

| Article ID | Page Slug | Problem | What to Write |
|-----------|-----------|---------|---------------|
| 221 | Home | "Reassurance pamphlet" — apologetic about apologetics | Declaration: "This site exists because the attacks are real, the stakes are eternal, and the answers are better than the questions." |
| 255 | WhyAtonement | Raises sovereign-God dilemma but punts on the LDS answer | Merge with 263/264/268/269/270 into: "The Sovereign God Dilemma: Why the Atonement Is Necessary." Covers co-eternal law, justice/mercy tension, why a mediator is required. Creation Ex Nihilo / agency angle is a SEPARATE new page (`Creation-Ex-Nihilo` in P4-Batch 5). |
| 272 | Abrahams-Seed-Issue | Thin, Facebook artifacts, never delivers LDS answer | Full math problem setup and LDS resolution |
| 273 | Abrahams-Seed-Conclusion | WRONG CONTENT — contains Protestant Slavers text in ChatGPT UI | Build: eternal families, spirit children, infinite increase as the answer to Abraham's seed |
| 277 | Protestant-Slavers | Strong Douglass quote, never delivers the LDS indictment | Add explicit verdict: Sola Fide is precisely the theology that enabled slavery; works and accountability matter |
| 285 | Blacks/Priesthood (Short) | Facebook artifacts, underdeveloped Matthew 19 analogy | Tight version pointing to article 286 |
| 295 | Profile-Pastors | 4-sentence stub | Full logical demolition of how OSAS/Sola Fide makes clergy salvation uncertain |
| 297 | Profile-Jews | 1 quote, no argument | Lead with Regina Jonas, build to LDS temple work as the only coherent resolution |
| 298 | Profile-Indigenous | 1 paragraph | Billions condemned for geography. LDS answer: post-mortal missionary work, proxy ordinances |
| 300 | Bible-Math5 | Placeholder/personal note | Bible textual variants prove "as far as it is translated correctly" (Article 8) is honest and defensible |
| 312 | Should-Debate | Sounds like retreat | "Here's why written refutation is superior to debate, and why the debate format systematically favors rhetoric over truth" |
| 347 | Biblical-Slavery | Self-contradictory, Facebook dump ending | Clean tu quoque: if you attack LDS for hard passages, engage your own Bible first. Positive case: works and accountability prevent weaponization of doctrine |
| 370 | Horses | Descriptive journalism, not apologetics | State the claim, show epistemic arrogance, deploy evidence aggressively |
| 371 | Disciple | Raw Facebook post, superseded by 403 | Rewrite or redirect to 403 |
| 378 | Adam-God | Structurally confused, strained reinterpretation | Clear thesis: here's the attack, here's why JD is not binding scripture, here's 1910 First Presidency correction, here's actual LDS Adam doctrine |
| 396 | BOM-Weight | Ends with a Facebook comment about farm labor | Keep: Tumbaga calculation, Harris/Emma/Pratt testimony. Delete: everything after the calculation |

**Progress — P3-Batch 1:**
- [x] 255 + merge 263/264/268/269/270 — rewritten as "The Sovereign God Dilemma: Why the Atonement Is Necessary" (~1,200 words, 5 sections, Alma 42/34, D&C 93, Anselm acknowledged). All 5 source articles stubbed with redirect to WhyAtonement.

**P3-Batch 2:**
- [x] 221 — Home (rewritten as 400-word declaration: "The Answers Are Better Than the Questions")
- [x] 272 — Abrahams-Seed-Issue (7×10²² stars vs 1.1×10¹¹ humans; 5 Protestant failures; closes with Socratic double-question)
- [x] 273 — Abrahams-Seed-Conclusion (full rebuild: D&C 132, Moses 1:33/35, D&C 76:58 — "no other theology has a plan large enough for the God who made the promise")

**P3-Batch 3:**
- [x] 277 — Protestant-Slavers (full rewrite: Douglass testimony, Sola Fide enabled it, LDS indictment James/Rev 20/Matt 25, verdict: "the fruit across 300 years is not a coincidence")
- [x] 285 — Blacks/Priesthood Short (rewrite: Elijah Abel 1836, Matt 19:8 parallel, OD2 as evidence for living prophets; links to full article)
- [x] 295 — Profile-Pastors (full rewrite: OSAS problem, assurance problem, congregational problem, LDS answer D&C 1:15 — "one theology is coherent, the other is not")

**P3-Batch 4:**
- [x] 297 — Profile-Jews (Abraham/Moses/Isaiah/Maimonides named; Regina Jonas framing; 1 Pet 4:6 + D&C 138; "no other theology preserves God's justice and love for every person who ever lived")
- [x] 298 — Profile-Indigenous (Maya/Aztec/Inca/Iroquois named; Romans 1:20 doesn't witness to Christ as savior; same LDS answer framework)
- [x] 300 — Bible-Math5 (fresh build: 400k variants, Comma Johanneum, Long Ending of Mark, Woman in Adultery, Gethsemane agony — LDS Article 8 is the scholarly position)

**P3-Batch 5:**
- [x] 312 — Should-Debate (flipped to "Written Refutation Beats Live Debate" — performance vs truth, 3 Ne 11:29, writes for doubters at midnight not critics seeking applause)
- [x] 347 — Biblical-Slavery (Lev 25:44-46, Exod 21, Eph 6:5, Col 3:22 — clean tu quoque; 2 Ne 26:33 + OD2 as positive case; dropped Sola Fide tangent)
- [x] 370 — Horses (Troy parallel; Yucatan bones, Miller/Roper, Mayapan jawbone, Holocene survival 2022; "open question is not a refutation")

**P3-Batch 6:**
- [x] 371 — Disciple (redirect stub → LDSChristians confirmed page ID 314)
- [x] 378 — Adam-God (full rewrite: JoD 1:50 quoted, D&C 130:22 as 1843 canonized refutation, 1912 Improvement Era correction, "speculative sermon, contradicts canon, never was doctrine")
- [x] 396 — BOM-Weight (already clean; Tumbaga calc + 4 eyewitnesses retained; clean verdict paragraph added)

**P3-Batch 7 (image stub merges — lightweight, all 5 in one session):**
- [ ] 262 — God-Sovereign-Image (merge header into rewritten 255)
- [ ] 265 — Racist-Doc-Image (merge header into BookofMormon-RacistDoc)
- [ ] 271 — AbrahamsSeedImage (merge header into rewritten Abrahams-Seed)
- [ ] 284 — Priesthood-Image (merge header into Priesthood-Topics)
- [ ] 289 — Feelings-Image (merge header into LDS-Feelings)

---

## Phase 4 — New Pages (15 pages, 6 batches of 2–3)

Standard sequence: `create_page` → `create_article` → `set_article_content` → `update_page` → `publish_page` → `add_menu_item`

⚠️ **Each batch = one agent session. Complete one batch fully before starting the next.**
⚠️ **Pre-flight rule:** Agent must call `search_pages` and `search_articles` for each slug before creating anything. If a page already exists from a prior failed session, resume from that point rather than creating a duplicate stub.
⚠️ **Naming is fixed:** `pageName`, `articleName`, and menu `itemTitle` are specified per batch. Agents must use exactly the values in the table — no variations.

---

### P4-Batch 1 — Fundamentals: Authority (3 pages)
*Menu: Fundamentals*

| Slug | Title |
|------|-------|
| Simon-Magician | Simon the Magician and the Priesthood of Believers |
| Gates-Of-Hell | The Gates of Hell and Continuous Revelation |
| Trial-Of-Faith | Faith Before Evidence: The Trial of Faith |

- [x] Simon-Magician (pageId:492, articleId:581, menuId:55 — Fundamentals)
- [x] Gates-Of-Hell (pageId:493, articleId:582, menuId:53 — Fundamentals; corrected from Protestant Issues)
- [x] Trial-Of-Faith (pageId:494, articleId:583, menuId:54 — Fundamentals)

---

### P4-Batch 2 — Fundamentals: Revelation + Protestant Issues (3 pages)
*Menu: Fundamentals / Protestant Issues*

| Slug | Title |
|------|-------|
| Revelation-Pattern | The Revelation Pattern |
| Protestant-Collapse | The Collapse of Protestant Christendom |
| Biblical-Communalism | Biblical Communalism — Acts 2 and 4 |

- [x] Revelation-Pattern (pageId:496, articleId:585, menuId:57)
- [x] Protestant-Collapse (pageId:495, articleId:584, menuId:56)
- [x] Biblical-Communalism (pageId:499, articleId:588, menuId:60)

---

### P4-Batch 3 — LDS Doctrine: Pre-existence and Mediation (3 pages)
*Menu: LDS Doctrine*

| Slug | Title |
|------|-------|
| Abrahams-Seed-Answer | The Abraham's Seed Problem — LDS Has the Only Answer |
| PreExistence-Bible | Pre-Existence: The Bible Evidence |
| Why-Mediator | Why a Mediator Is Necessary |

- [x] Abrahams-Seed-Answer (pageId:505, articleId:594, menuId:66 — LDS Doctrine)
- [x] PreExistence-Bible (pageId:497, articleId:586, menuId:58 — LDS Doctrine)
- [x] Why-Mediator (pageId:498, articleId:587, menuId:59 — LDS Doctrine)

---

### P4-Batch 4 — LDS Doctrine: Godhead (2 pages)
*Menu: LDS Doctrine*

| Slug | Title |
|------|-------|
| Godhead-vs-Trinity | The Nature of the Godhead vs. the Trinity |
| Heavenly-Mother | Heavenly Mother — Why This Doctrine Is Inevitable |

- [x] Godhead-vs-Trinity (pageId:501, articleId:590, menuId:62 — LDS Doctrine parent:21)
- [x] Heavenly-Mother (pageId:502, articleId:591, menuId:63 — LDS Doctrine parent:21)

---

### P4-Batch 5 — LDS Doctrine: Cosmology and Resurrection (2 pages)
*Menu: LDS Doctrine*

| Slug | Title |
|------|-------|
| Creation-Ex-Nihilo | Creation Ex Nihilo Destroys Free Will |
| Resurrection-Power | Resurrected Bodies Are More Powerful Than Spirits |

- [x] Creation-Ex-Nihilo (pageId:503, articleId:592, menuId:64 — LDS Doctrine)
- [x] Resurrection-Power (pageId:504, articleId:593, menuId:65 — LDS Doctrine)

---

### P4-Batch 6 — LDS Doctrine: Resurrection (1 page)
*Menu: LDS Doctrine*

| Slug | Title |
|------|-------|
| Why-Resurrection | The Resurrection: Why It Matters and What It Proves |

Note: Profile-Pastors, Profile-Jews, Profile-Indigenous, Profile-Innocent, Profile-Obedient are rewrites of existing articles (IDs 295–299) — they are tracked in Phase 3, not here.

- [x] Why-Resurrection (pageId:500, articleId:589, menuId:67)

---

## Menu Placement Guide

**Existing sections (do not create new ones unless the review step approves it):**
Fundamentals | LDS Doctrine | The Book of Abraham | Book of Mormon | Bible | Protestant Issues | Atheism

**Finding menu parent IDs:** At the start of any session that adds menu items, call `get_menu` to get the current structure and note the integer `id` for each top-level section. Record them here once known:

| Section | Menu ID |
|---------|---------|
| Fundamentals | 2 |
| LDS Doctrine | 21 |
| The Book of Abraham | 4 |
| Book of Mormon | 5 |
| Bible | *(call get_menu)* |
| Protestant Issues | 9 |
| Atheism | *(call get_menu)* |

**Phase 4 additions — use existing sections:**

| Slug | Section |
|------|---------|
| Simon-Magician | Fundamentals |
| Gates-Of-Hell | Fundamentals |
| Trial-Of-Faith | Fundamentals |
| Revelation-Pattern | Fundamentals |
| Protestant-Collapse | Protestant Issues |
| Biblical-Communalism | Protestant Issues |
| Abrahams-Seed-Answer | LDS Doctrine |
| PreExistence-Bible | LDS Doctrine |
| Why-Mediator | LDS Doctrine |
| Godhead-vs-Trinity | LDS Doctrine |
| Heavenly-Mother | LDS Doctrine |
| Creation-Ex-Nihilo | LDS Doctrine |
| Resurrection-Power | LDS Doctrine |
| Why-Resurrection | LDS Doctrine |

**Phase 5 additions — all use existing sections (see per-batch notes).**

---

## ✅ Final Step — Menu Review (Complete)

### Changes Made
- **Moved to correct sections:** Horses → Book of Mormon | Abraham-Relativity → Book of Abraham | Filthy-Rags + Isaiah-Reason-Together → Bible | Three-Kingdoms + Joseph-Polygamy-Context → LDS Doctrine | Why-Apostasy → Fundamentals
- **All sections reordered** within each section from most fundamental to most specific
- **Item titles shortened/sharpened** throughout for visitor scannability
- **Sections renamed** where needed for clarity

### Final Section Structure
| Section | Items | Notes |
|---------|-------|-------|
| Fundamentals | 13 | Authority, revelation, faith, apostasy — acceptable at 13 |
| LDS Doctrine | 32 | **Overloaded** — future split candidate: Godhead (8), Cosmology (4), Salvation/Ordinances (6), Historical (6) |
| Protestant Issues | 25 | Large but internally coherent; Profile of the Damned (5 items) could become its own section |
| Book of Mormon | 13 | Well organized |
| Bible | 4 | Grew from 1; good size |
| The Book of Abraham | 3 | Good size |
| Atheism | 2 | Small; acceptable |

### Follow-up Items
1. **Shell item in Book of Abraham (menu item 7):** Has no linked page — investigate whether a page needs to be created or linked
2. **Two pages on BOM racist topic:** Items 16 (old short page 208) and 88 (new full page 747) — consider consolidating into one definitive page eventually
3. **LDS Doctrine overload:** If the site grows further, split into "Who Is God?", "The Plan of Salvation", "Ordinances & Temple", and "LDS History Objections" sub-sections

---

## Phase 5 — Facebook Gap Pages (40 new pages, 20 batches of 2–3)

Analysis of 56,000+ lines of Facebook apologetics posts identified 36 net-new concepts not covered by existing pages or Phase 4. Split into batches of 2–3 pages to keep each agent session within token limits.

**Facebook source:** `E:\FacebookDownload\facebook-TheronBird-2025-04-11-jkcNtL2U\your_facebook_activity\groups\group_posts_and_comments.html`

Use `LDS-APOLOGETICS-AGENT-TEMPLATE.md` for every agent session.

⚠️ **2–3 pages per session maximum. The research load (Facebook grep + multiple WebFetches + full HTML articles) is heavy. Do not push a session beyond 3 pages.**
⚠️ **Pre-flight rule:** Agent must call `search_pages` and `search_articles` for each slug before creating anything. A prior failed session may have left partial state — find it and resume rather than creating a new stub.
⚠️ **Naming is fixed:** Every slug, article name, and menu title is defined in the batch table. Agents must use exactly those values.

---

### 5-A1 — Godhead Part 1 (3 pages)
*Menu: LDS Doctrine*

| Slug | Title | Description |
|------|-------|-------------|
| Trinity-Math | The Algebra of the Godhead | 3x = One God; x = a title. John 17 proves oneness of purpose, not substance. |
| Who-Is-Holy-Ghost | Who Is the Holy Ghost? | Five roles: Revealer, Sanctifier, Empowerer, Sealer, Telestial Administrator. A personage of spirit, not a vague force. |
| Jesus-Is-Jehovah | Jesus Is Jehovah | Jesus Christ is the Jehovah of the OT; the Father is Elohim; two distinct persons, one covenant. |

- [x] Trinity-Math (pageId:729, articleId:835, menuId:68)
- [x] Who-Is-Holy-Ghost (pageId:731, articleId:837, menuId:73)
- [x] Jesus-Is-Jehovah (pageId:734, articleId:840, menuId:76)

---

### 5-A2 — Godhead Part 2 (3 pages)
*Menu: LDS Doctrine*

| Slug | Title | Description |
|------|-------|-------------|
| God-Is-Our-Father | God Is Our Literal Father | God's tangible body; his work and glory; the implications of literal fatherhood. |
| Agency-Requires-Preexistence | Agency Requires Pre-existent Intelligences | Ex-nihilo omniscience destroys moral agency and makes God culpable for evil. Only eternal intelligences resolve this. |
| Gender-Eternal | Gender Is Eternal | Gender as premortal, mortal, and eternal characteristic; why Satan attacks it; Proclamation on the Family. |

- [x] God-Is-Our-Father (pageId:726, articleId:832, menuId:70)
- [x] Agency-Requires-Preexistence (pageId:727, articleId:833, menuId:71)
- [x] Gender-Eternal (pageId:728, articleId:834, menuId:72)

---

### 5-B1 — Restoration Part 1 (3 pages)
*Menu: Fundamentals (existing)*

| Slug | Title | Description |
|------|-------|-------------|
| Harrowing-Hell-Restored | The Harrowing of Hell and the Restoration | 1 Pet 3:19, 4:6; Eph 4:9; early church fathers confirm Christ preached to the dead. |
| NT-Church-Offices | The NT Required Apostles and Prophets — Still | Eph 4:11-13: offices must exist until unity of faith. Since we haven't reached it, restoration is necessary. |
| Path-Of-Apostasy | The Path of Personal Apostasy | 12 stages from honest doubt to active opposition; the pride dynamic; a defensive meta-apologetics tool. |

- [x] Harrowing-Hell-Restored (pageId:730, articleId:836, menuId:69)
- [x] NT-Church-Offices (pageId:732, articleId:838, menuId:74)
- [x] Path-Of-Apostasy (pageId:735, articleId:841, menuId:77)

---

### 5-B2 — Restoration Part 2 (3 pages)
*Menu: Fundamentals (existing)*

| Slug | Title | Description |
|------|-------|-------------|
| Spirit-Prison-Gospel | The Gospel Is Preached to the Dead | John 5:25-29; 1 Pet 4:6; D&C 138. The dead can receive the gospel; temple work is how the living aid them. |
| Organized-Religion-Matters | Why Organized Religion Matters — Specifically This One | Irenaeus on apostolic succession; the Church must have correct organization; 40,000 denominations is not acceptable. |
| Handcart-Not-Negligence | Handcart Companies: Faith, Not Negligence | Willie/Martin 16-25% death rate vs. Donner Party 45%; left late by their own choice. |

- [x] Spirit-Prison-Gospel (pageId:738, articleId:844, menuId:81)
- [x] Organized-Religion-Matters (pageId:742, articleId:848, menuId:84)
- [x] Handcart-Not-Negligence (pageId:746, articleId:852, menuId:89)

---

### 5-C1 — Salvation Part 1 (3 pages)
*Menu: Protestant Issues (existing) — these refute Protestant soteriology*

| Slug | Title | Description |
|------|-------|-------------|
| Salvation-Process | Salvation Is a Process, Not an Event | Point-by-point case against OSAS; lifelong process of faith, repentance, baptism, Holy Ghost, and endurance. |
| Filthy-Rags | "Filthy Rags" — What Isaiah Actually Said | Isaiah 64 poetic structure: "filthy rags" = our sinful state, not our righteous works. |
| Isaiah-Reason-Together | "Come, Let Us Reason Together" — In Context | Isaiah 1:16-20 full context: sins become white IF willing and obedient. The conditional critics always drop. |

- [x] Salvation-Process (pageId:733, articleId:839, menuId:75)
- [x] Filthy-Rags (pageId:736, articleId:842, menuId:78)
- [x] Isaiah-Reason-Together (pageId:739, articleId:845, menuId:80)

---

### 5-C2 — Salvation Part 2 (2 pages)
*Menu: Protestant Issues (existing)*

| Slug | Title | Description |
|------|-------|-------------|
| Repentance-Real | Repentance Means Actual Change | Linguistic study: Greek metanoia, Hebrew teshuvah, Arabic tawbah — all require behavioral change, not just a changed mind. |
| Carnal-Christian | The Carnal Christian — Sola Fide's Logical Product | Sola Fide produces the theologically accepted saved person living in deliberate sin; reductio ad absurdum. |

- [x] Repentance-Real (pageId:741, articleId:847, menuId:83)
- [x] Carnal-Christian (pageId:745, articleId:851, menuId:87)

---

### 5-C3 — Salvation Part 3 (2 pages)
*Menu: Protestant Issues (existing)*

| Slug | Title | Description |
|------|-------|-------------|
| TULIP-Refuted | TULIP Calvinism Refuted Systematically | All five Calvinist points refuted: Total Depravity, Unconditional Election, Limited Atonement, Irresistible Grace, Perseverance. |
| Three-Kingdoms | Three Kingdoms of Glory — Why It Matters | D&C 76; Celestial, Terrestrial, Telestial; why differences matter even if all are eventually saved to some degree. |

- [x] TULIP-Refuted (pageId:749, articleId:855, menuId:91)
- [x] Three-Kingdoms (pageId:753, articleId:859, menuId:95)

---

### 5-D1 — Book of Mormon Evidence Part 1 (3 pages)
*Menu: Book of Mormon*

| Slug | Title | Description |
|------|-------|-------------|
| BOM-Archaeology | BOM Archaeology — Items Once Mocked, Now Confirmed | Metal plates, steel swords (600 BC), demotic Egyptian script, pre-Columbian contact — all confirmed after ridicule. |
| BOM-Self-Limiting | The BOM Never Claimed to Be a Complete American History | Jacob 3:13 — only 1/100th recorded. Absence of evidence is not evidence of absence. |
| BOM-Linguistic | Linguistic Evidence for the Book of Mormon | Brian Stubbs: Hebrew, Egyptian, Arabic traces in Native American languages, consistent with BOM claims. |

- [x] BOM-Archaeology (pageId:737, articleId:843, menuId:79)
- [x] BOM-Self-Limiting (pageId:740, articleId:846, menuId:82)
- [x] BOM-Linguistic (pageId:744, articleId:850, menuId:85)

---

### 5-D2 — Book of Mormon Evidence Part 2 (3 pages)
*Menu: Book of Mormon*

| Slug | Title | Description |
|------|-------|-------------|
| BOM-Not-Racist-Full | The Book of Mormon Is Not a Racist Document | Lamanites win, inherit the land, are often more righteous. White Nephites are annihilated. Opposite of white supremacy. |
| BOM-Challenge | The Book of Mormon Doctrine Challenge | McConkie's challenge: compare BOM vs. Bible doctrine by doctrine. BOM matches or exceeds on every topic. |
| Nephi-Kills-Laban | Nephi Killing Laban — Why It Was Not Murder | Law of retribution (D&C 98); God's direct command; a nation's scripture vs. one man's life. |

- [x] BOM-Not-Racist-Full (pageId:747, articleId:853, menuId:88)
- [x] BOM-Challenge (pageId:750, articleId:856, menuId:92)
- [x] Nephi-Kills-Laban (pageId:754, articleId:860, menuId:96)

---

### 5-E1 — Ordinances Part 1 (3 pages)
*Menu: LDS Doctrine (existing)*

| Slug | Title | Description |
|------|-------|-------------|
| Baptism-Four-Reasons | Baptism Is Required — Four Reasons | Commandment, covenant, ordinance of forgiveness, prerequisite for the Holy Ghost. Complete biblical case. |
| Baptism-Dead-Chiasmus | Baptism for the Dead in the Center of Paul's Chiasmus | 1 Cor 15 chiastic structure places "baptism for the dead" at the structural center. |
| Hell-Has-End | Hell Has an End — Universal Resurrection | 1 Cor 15:22-26; D&C 76:39-43; all mankind resurrected and redeemed to some degree. Jesus is truly triumphant. |

- [x] Baptism-Four-Reasons (pageId:743, articleId:849, menuId:86)
- [x] Baptism-Dead-Chiasmus (pageId:748, articleId:854, menuId:90)
- [x] Hell-Has-End (pageId:751, articleId:857, menuId:93)

---

### 5-E2 — Ordinances Part 2 (1 page)
*Menu: LDS Doctrine (existing)*

| Slug | Title | Description |
|------|-------|-------------|
| Angels-Are-People | Angels Are Resurrected and Spirit Humans | Rev 19:10, Luke 20:36, Heb 13:2, Rev 21:17: "the measure of a man, that is, of an angel." |

Note: `Spirit-Prison-Gospel` was removed here — it duplicates the page in 5-B2.

- [x] Angels-Are-People (pageId:755, articleId:861, menuId:97)

---

### 5-F1 — Science and Cosmology Part 1 (2 pages)
*Menu: LDS Doctrine*

| Slug | Title | Description |
|------|-------|-------------|
| Spirit-Matter | Spirit Is Real Matter | D&C 131:7 — spirit is more pure and fine matter. Bridges pre-existence, body, and resurrection in a unified physical framework. |
| Abraham-Relativity | Joseph Smith and Einsteinian Relativity | Abraham 3 (1842): time varies by sphere and observer. Identical in implication to Einstein's Special Relativity, 63 years later. |

- [x] Spirit-Matter (pageId:757, articleId:863, menuId:99)
- [x] Abraham-Relativity (pageId:759, articleId:865, menuId:101)

---

### 5-F2 — Science and Cosmology Part 2 (2 pages)
*Menu: Book of Abraham (Abraham-Facsimiles) / LDS Doctrine (Family-Under-Attack)*

| Slug | Title | Description |
|------|-------|-------------|
| Abraham-Facsimiles | The Book of Abraham Facsimiles — Inverted Priestly Imagery | Egyptian priestly imagery deliberately inverted as a missionary tract for priests; explains why Joseph's "translation" diverges from Egyptological reading. |
| Family-Under-Attack | The Family Is Under Coordinated Attack | Satan's strategy targeting gender identity, marriage covenant, and reproduction. Proclamation on the Family as prophetic response. |

- [x] Abraham-Facsimiles (pageId:761, articleId:867, menuId:103 — Book of Abraham parentId:4)
- [x] Family-Under-Attack (pageId:762, articleId:868, menuId:104)

---

### 5-G1 — Social and Historical Part 1 (2 pages)
*Menu: Protestant Issues (existing)*

| Slug | Title | Description |
|------|-------|-------------|
| Joseph-Smith-Abolitionist | Joseph Smith Was an Abolitionist | Freed slaves personally, ran for president opposing slavery in 1844. The "racist prophet" narrative is demonstrably false. |
| Joseph-Polygamy-Context | Why Joseph Smith Could Not Openly Confirm Plural Marriage | Missouri Extermination Order, mob violence, code names in D&C; public denial was legally and physically necessary. |

- [x] Joseph-Smith-Abolitionist (pageId:752, articleId:858, menuId:94)
- [x] Joseph-Polygamy-Context (pageId:756, articleId:862, menuId:98)

---

### 5-G2 — Social and Historical Part 2 (2 pages)
*Menu: Protestant Issues (existing)*

| Slug | Title | Description |
|------|-------|-------------|
| Luther-Antisemitism | Martin Luther's Antisemitism Cannot Be Ignored | Luther's 1543 pamphlet advocated burning synagogues. Lutherans cannot selectively accept his soteriology while excusing this. |
| Evangelical-Immorality | Sola Fide's Fruit: The Statistical Record | 86% premarital sex among Evangelicals; 25% of Ashley Madison users are Evangelical. What a doctrine produces is evidence of whether it is true. |

- [x] Luther-Antisemitism (pageId:758, articleId:864, menuId:100)
- [x] Evangelical-Immorality (pageId:760, articleId:866, menuId:102)

---

## Scope Summary

| Phase / Batch | Category | Items | Sessions |
|---------------|----------|-------|----------|
| 1 (4 batches) | Style-only passes | 11 articles | 4 |
| 2 (5 batches) | Expansions | 15 articles | 5 |
| 3 (7 batches) | Full rewrites + merges | ~21 articles | 7 |
| P4 (6 batches) | New pages — original plan | 14 pages | 6 |
| 5-A1/A2 | Godhead | 6 pages | 2 |
| 5-B1/B2 | Restoration | 6 pages | 2 |
| 5-C1/C2/C3 | Salvation | 7 pages | 3 |
| 5-D1/D2 | Book of Mormon evidence | 6 pages | 2 |
| 5-E1/E2 | Ordinances & Temple | 4 pages | 2 |
| 5-F1/F2 | Science & Cosmology | 4 pages | 2 |
| 5-G1/G2 | Social & Historical | 4 pages | 2 |
| Final | Menu review | — | 1 |
| **Total** | | **~98 work items** | **~38 sessions** |

---

## Key References

| Item | Value |
|------|-------|
| WEBSITE_ID | 5 |
| Home Article ID | 221 |
| CloudFront Distribution | E3VCKB95QSPEA6 |
| S3 Content Bucket | www-websitecontent |
| Article S3 Path | public/websites/ldsapologetics.com/articles/ |
| Deploy Profile | Admin |
| Agent template | `e:\dev\webApplicationArchitecture\LDS-APOLOGETICS-AGENT-TEMPLATE.md` |
| Source Facebook data | `E:\FacebookDownload\facebook-TheronBird-2025-04-11-jkcNtL2U\your_facebook_activity\groups\group_posts_and_comments.html` |
| Menu backup | E:\Apologetics\OrganizedReligion\sitemenu-backup-pre-test-2026-05-19.json |
