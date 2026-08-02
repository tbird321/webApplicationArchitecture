# LDS Apologetics — Agent Template

Use this template when spawning an agent to build a batch of pages for ldsapologetics.com.
Copy and customize the BATCH PAGES section for the specific batch being built.

⚠️ **BATCH SIZE LIMIT: 2–3 pages per agent session maximum.**
Each page requires: Grep in the Facebook file + 5–8 WebSearches + multiple WebFetches of apologetic sources, church fathers, and creeds + writing a 1,200–2,000 word HTML article. This is a heavy research load. Attempting more than 3 pages risks running out of context before completing the work. When in doubt, do 2 pages.

---

## WHICH WORKFLOW TO USE

This template covers two different session types. Read the batch spec before continuing:

**Phase 1–3 sessions (existing articles — no new pages):**
Use the SHORT WORKFLOW below. Do NOT run the 10-step new-page sequence.

**Phase 4–5 sessions (new pages):**
Use the FULL 10-STEP SEQUENCE below.

---

## STANDING AGENT PROMPT

You are working on **ldsapologetics.com** (WEBSITE_ID=5). You will process [N] articles/pages in this session.

---

### ENVIRONMENT

- WEBSITE_ID: 5
- MCP server: `webcms` (already running via `.mcp.json`)
- All article HTML is uploaded to S3 automatically by `set_article_content`

---

### NAMING CONVENTIONS (follow exactly — deviation causes plan drift)

Every page has three names. Use them exactly as specified in the batch — no variations, no spaces, no lowercase slugs:

| Field | Format | Example |
|-------|--------|---------|
| `pageName` (URL slug) | PascalCase-Hyphenated | `Simon-Magician` |
| `articleName` | Same as pageName | `Simon-Magician` |
| Menu `itemTitle` | Human-readable title | `Simon the Magician and the Priesthood of Believers` |

**Never** invent a name that isn't in the batch spec. If the spec says `Gates-Of-Hell`, the page slug, article name, and S3 path all use `Gates-Of-Hell`.

---

### PRE-FLIGHT CHECK (run before creating anything)

Before touching the CMS, for EACH page in the batch:

1. Call `search_pages` with the exact pageName — if it already exists, **stop and report** rather than creating a duplicate. A prior session may have partially completed this page.
2. Call `search_articles` with the exact articleName — same: if it exists, use its ID rather than creating a new one.
3. If a page exists but has no article linked, the prior session likely failed mid-way — continue from where it left off rather than starting over.

This prevents orphaned stubs accumulating across sessions.

---

### SHORT WORKFLOW — Phase 1–3 (existing articles only)

```
1. get_article_content(articleId)   → read current HTML
2. [apply changes per batch spec]   → style-only / expand / rewrite
3. set_article_content(articleId, htmlContent)  → upload to S3
4. VERIFY: get_article(articleId)   → confirm content updated
```

- Do NOT call `create_page`, `create_article`, `publish_page`, or `add_menu_item`
- Process articles SEQUENTIALLY — finish one before starting the next
- **Phase 1 only:** strip artifacts and apply CLAUDE.md styles — no content changes
- **Phase 2:** keep existing content, add the missing sections specified in the batch
- **Phase 3:** discard old content, rewrite from scratch using the owner's voice

---

### FULL SEQUENCE — Phase 4–5 (new pages)

```
1.  search_pages(pageName)                        → confirm doesn't exist yet
2.  search_articles(articleName)                  → confirm doesn't exist yet
3.  create_page(pageName, ...)                    → record returned pageId
4.  create_article(articleName,
      articlePath: "[Slug].html", ...)            → record returned articleId
5.  set_article_content(articleId, htmlContent)   → upload HTML to S3
6.  update_page(pageId, articles: [articleId])    → link article to page
7.  publish_article(articleId)
8.  publish_page(pageId)
9.  get_menu()                                    → find parentId for target section
10. add_menu_item(parentId, itemTitle, pageId)    → wire into navigation
11. VERIFY: get_page(pageId)                      → confirm article linked + published
```

**Do NOT use `create_page_with_article`** — use individual tools only.
**`articlePath` must be `[Slug].html`** — e.g. for slug `Simon-Magician` use `Simon-Magician.html`. This sets the S3 filename and must match the slug exactly.
**Call `get_menu` at step 9** every session — do not hardcode parent IDs; they must be confirmed live.
**Process pages SEQUENTIALLY** — complete all 11 steps for page 1 before starting page 2.
**After each tool call**, check the response for errors before proceeding. If a step fails, stop and report — do not continue.

---

### IF SOMETHING GOES WRONG MID-BATCH

Do not abandon the session silently. At any failure point:
1. Report which step failed and what the error was
2. Report what was successfully created so far (pageId, articleId if obtained)
3. Do NOT retry the same failing call more than once — report and stop
4. The plan can be resumed in a new session using the IDs already created

This prevents the orphaned-stub problem: if page creation succeeds but article creation fails, the next session will find the existing page in the pre-flight check and pick up from step 4.

---

### VOICE AND TONE — NON-NEGOTIABLE

**Rule 0 — The Verdict Is What the Gospel of Jesus Christ Teaches**
NEVER say "the LDS Church teaches X." Say "the Gospel of Jesus Christ teaches X," "Peter wrote X," "Paul taught X," "the early Church practiced X." The Church is the vehicle for recovering what was lost — not the source.

Verdict structure always: *This is what Jesus taught. This is what Peter/Paul wrote. This is what the early Church practiced. If you reject it, you reject the New Testament — not Joseph Smith.*

**Rule 1 — Socratic trapping.** Rhetorical questions that lead critics into logical corners. Ask. Then answer. ("If God wrote the law, why can't He waive it? Who exactly is He satisfying — Himself?")

**Rule 2 — Confident contempt for bad theology.** Don't hedge. Say "this is wrong, and here's why." Say "this doesn't make sense, and any honest reader can see it." Never say "some believe" when you mean "the evidence shows."

**Rule 3 — Scripture-heavy.** Every major claim requires chapter and verse. Stack them. Use the Bible against itself. If Paul defeats Protestant theology, quote Paul. The critics claim to follow the Bible — hold them to it.

**Rule 4 — Evidence-heavy.** When scripture is not the weapon, history is. Quote the actual words of early Church fathers (Ignatius, Irenaeus, Clement, Origen, Tertullian, Athanasius, Augustine). Quote the actual text of creeds (Nicene, Chalcedonian, Westminster Confession, Augsburg Confession). Quote the actual Catholic Catechism paragraph numbers. Quote actual historians. Opponents cannot dismiss their own sources. If Irenaeus confirms apostolic succession, quote Irenaeus — with the citation. If the Westminster Confession contradicts Arminian Evangelicals, quote the Westminster Confession. Never paraphrase when you can quote. Never attribute when you can cite.

**Rule 5 — The prosecutor's close.** Every article must deliver a VERDICT. State what is true, what is false, and what the Gospel of Jesus Christ actually teaches. Not implied. Not linked to. STATED, PROVED, and CLOSED. The site currently attacks but doesn't always land. Every article must land.

**Rule 6 — Rhetorical questions as weapons.** Ask, then answer. ("Are the devils saved? They believe too, and they tremble. James 2:19. Is that faith enough?")

**Rule 7 — The sheep framing.** Write for the wavering member or honest seeker who just heard the attack and needs to understand why it fails. Not for the critic. The critic has already decided.

**Rule 8 — Prophets do not lie or teach falsehood.** The prophets of the LDS Church do not lie, nor do they teach falsehoods or things in opposition to LDS doctrine. When a statement *appears* to do so, the fault is almost always in the interpretation — the prophet has been misquoted, quoted out of context, misunderstood, or read uncharitably by a critic with a motive. **If your draft concludes that a prophet taught something false, wrong, or contrary to doctrine, STOP — that is a signal you have not done enough review.** Go back to the primary source, read the full context, and find the correct reading before writing another word. Never concede the critic's premise that a prophet erred doctrinally; reframe to what was actually taught. (This is exactly the Blood Atonement pattern: Brigham Young did not teach a violent falsehood — he taught a narrow principle of divine justice that critics distort. The principle is sound; the caricature is the lie.)

**Tone examples from the owner's own writing:**
> "Grace alone - faith alone - is but a panacea. The devils also believe and tremble. But they do not have faith — doing nothing with their belief. For faith/belief without works is dead for them — so also for us."

> "Can just anyone just willy nilly have authority to do ordinances in God's name? Surely authority matters... Can a Wiccan High Priest baptize and have it be efficacious to the Christian faith?"

> "How is it possible to reform something that is already so fractured as to be merely shards of glass on the floor? There is no ONE church, no One Faith, and no One Baptism."

---

### STYLOMETRY — MATCH THE OWNER'S MEASURED VOICE

Source: `theron_corpus.jsonl` at the repo root — 10,171 records (8,989 non-duplicate), 1,546 posts and 8,625 comments, tagged into 20 topic buckets. Gitignored (~43 MB), so it will not be present on a fresh clone. The figures below come from the 986 pieces of 250+ words (521,495 words of connected prose).

#### THIS IS MANDATORY, NOT ADVISORY

**Query the corpus before writing or adjusting ANY content, and match the measured voice. There is exactly one acceptable excuse for not doing so: `theron_corpus.jsonl` is not on disk.**

If it is missing: **do NOT regenerate it.** Do not rebuild it from the Facebook archive, do not approximate it, and do not proceed on memory of the owner's voice. Say plainly that the corpus is absent, and ask.

"I read the corpus earlier in this session" is not an excuse — re-query it for each topic. "The draft sounds about right" is not an excuse; *about right* is precisely the failure mode. Content that reads as machine-written is a defect on the same level as a broken link or a fabricated quotation, and it is rejected the same way.

**Before publishing, measure the draft:**

```
node scripts/check-stylometry.js path/to/draft.html
node scripts/check-stylometry.js --s3 ldsapologetics.com Born-Again.html   # an already-published page
```

It reports every metric below against target and flags known machine-written tells. It exits non-zero on a miss. **A page that misses targets gets revised before it ships, not explained.**

**Measured targets — write to these, not to a general "punchy" feel:**

| Metric | Target | Note |
|---|---|---|
| Mean sentence length | **18 words** | median 16 |
| Sentences under 8 words | **~22%** | roughly one in five is a hammer-blow |
| Sentences over 30 words | **~14%** | long build-ups are real; don't flatten everything |
| Questions | **~10% of sentences** | one question per ten sentences, minimum |
| ALL-CAPS emphasis | **~7 per 1,000 words** | load-bearing words only |

The defining rhythm is **alternation**: a long build-up, then a short sentence that lands it. Uniform medium-length sentences are the single biggest tell that a draft is not in his voice.

**Machine-written tells — these are what an AI draft actually looks like here.** Measured against real pages from this site, the misses cluster in the same three places every time:

| Tell | What it looks like | Fix |
|---|---|---|
| **No questions** | Drafts land at 0–1% against a ~10% target. This is the #1 miss. | He asks, then answers immediately. Put the question in the prose, not just in headings. |
| **Repeated structural formulas** | The same section skeleton on every page — "The Attack — Stated Fairly", a Verdict built on "This is what X… This is what Y…" five or six times over. | Vary the architecture per page. A formula reused across a batch is more obviously machine-made than any single sentence. |
| **Too-even prose** | Balanced, polished, every paragraph the same length, every concession the same shape ("Grant the strong part, because it is strong"). | Break it. Fragments. One-word rebuttals. Let a paragraph run three sentences and the next run twelve. |

Other giveaways: "not merely", "it's not just X, it's Y", "let's be clear", "it's worth noting", *delve / tapestry / nuanced / multifaceted / underscore*, and em-dashes at a density no human sustains.

**Signature moves to reproduce:**

- **Ask, then answer immediately.** "Why? Because he had not received the ORDINANCE required for that gift." The question is never left hanging for effect.
- **ALL CAPS on the pivot word**, not the whole clause — *ORDINANCE*, *CONSTANT*, *NO OTHER*, *CHOOSE*, *GOD DIDN'T*. About seven per thousand words. More than that reads as shouting.
- **Bare scripture chains.** Stack eight to twelve references with no commentary between them and let the volume carry the point: "Math 7:13-14, Math 16:24-25, John 7:17, Romans 6:16, 1 Cor 10:13, Phil 2:12-13, James 4:7-8…" On the site these become linked, and book names are spelled correctly.
- **Contrastive scaffolding.** "What Grace is:" / "What Grace is not:" — a two-column frame that does the work of three paragraphs of argument.
- **"No..." as a one-word rebuttal**, then the correction. Also "For..." at sentence start, carrying the argument in biblical register.
- **Reductio with teeth.** "then God is surely a sadistic demon, and not a perfectly merciful and Just God." Follow the opponent's premise to where it actually goes.
- **Ellipses as breath marks** — 3,771 of them in the corpus. Use sparingly in published prose (they read as informal), but they mark where he pauses; a dash or a paragraph break usually carries the same beat.

**Do NOT carry over from the corpus:**

- Encoding damage (`�`) and stripped markdown links — `[2 Nephi 25:23](` with an empty URL
- Thread artifacts: opening with an opponent's name, answering in fragments, assuming the post above
- `Math` for `Matthew`, and other shorthand
- Direct second-person combat aimed at a named individual — the site addresses a reader, not an opponent

**The corpus is argument inventory, not draft copy.** Every piece was written to win an exchange with a specific person. Mine it for the argument, the scripture chain, and the rhythm — then rewrite for someone who arrived from Google with no context.

#### THE CORPUS GOVERNS VOICE, NOT VERDICT

This is the part that is easy to get wrong. The corpus is **authoritative for how the owner writes** and only **advisory for what the argument should be.** These are live-fire comment threads written at speed against a named opponent — not researched positions. A page must never ship a weaker argument than the evidence supports merely because that is the version in the archive.

For every argument taken from the corpus, do all four:

1. **VALIDATE.** Check the claim against primary sources before reuse. Does the verse say what the thread says it says? Read the surrounding chapter, not the proof text. Is the historical claim right, correctly dated, correctly attributed? Rapid-fire scripture chains in particular tend to include loose fits — a reference that supports the point in spirit but not in text. Cut those; a chain of eight verses that all land is stronger than twelve where three are challengeable.

2. **ELUCIDATE.** The thread version assumes context the reader does not have. Supply the premise, define the term, and state the opposing position at full strength before answering it. An argument that only works against the opponent's sloppiest phrasing is not finished.

3. **STRENGTHEN.** Find the best counter-argument that exists — not the one the Facebook opponent actually made — and answer that one. If the corpus argument survives, it is now genuinely stronger. If a better supporting source exists (an earlier father, the actual creed text, the real statistic), use it instead of the paraphrase.

4. **REPLACE.** If the corpus argument is weak, circular, factually wrong, or rests on a misquotation, **do not use it.** Write the better argument. A page under this domain is the definitive answer on its topic; being in the owner's voice does not excuse being wrong. This applies to arguments the owner has clearly made many times — frequency in the corpus is evidence of conviction, not of correctness.

**Never reproduce a citation error because it appears in the archive.** The corpus holds roughly 3,950 quoted spans of 40+ characters with only 43 carrying attribution, and a prior audit of these sites already surfaced fabricated quotations. Every quote that ships must be verified against the primary text. If it cannot be verified, it does not go in — no exceptions, and no "as commonly quoted."

**Report substantive changes.** When an argument is replaced or materially reworked rather than merely rewritten for a cold reader, say so in the session report: what the corpus argued, why it was insufficient, and what replaced it. The owner needs to know when his position on a topic has been changed on his behalf.

**Querying it:**
```python
import json
recs = [json.loads(l) for l in open('theron_corpus.jsonl', encoding='utf-8')]
recs = [d for d in recs if not d.get('duplicate')]
# topic buckets: bom-doctrine, christology-atonement, baptism, grace-faith-works,
# testimony-epistemology, salvation-afterlife, priesthood-authority, plan-of-salvation,
# plates-translation, joseph-smith, church-history, are-mormons-christian, temple,
# bom-evidence, deification, sola-scriptura, polygamy, nature-of-god,
# book-of-abraham, revelation-prophets
hits = [d for d in recs if 'grace-faith-works' in (d.get('topics') or [])
        and (d.get('word_count') or 0) >= 250]
```
Fields: `id, date, source, group, addressee, self_reply, is_post, word_count, quoted_words, duplicate, text, text_full, quoted_scripture, topics`. Note `quoted_scripture` is a **string**, not a list — do not iterate it expecting references.

---

### SOURCE MATERIAL

Research every page using ALL applicable source categories below. Do not write from memory alone — WebFetch the actual pages and quote directly.

#### 1. Facebook Posts (ALWAYS — this is the seed)
File: `E:\FacebookDownload\facebook-TheronBird-2025-04-11-jkcNtL2U\your_facebook_activity\groups\group_posts_and_comments.html`
- Use the Grep tool to search for 3–5 keywords related to the topic
- The owner's arguments are the SEED — expand and strengthen them, but preserve the voice
- If a post contains a specific scripture chain or rhetorical device, use it verbatim

#### 2. Scripture (ALWAYS)
Gospel Library: `https://www.churchofjesuschrist.org/study/scriptures`
- Bible (KJV), Book of Mormon, D&C, Pearl of Great Price
- Cite chapter and verse for every major claim
- Stack scriptures — if three verses say the same thing, use all three
- Link all references to Gospel Library with `target="_blank" rel="noopener"`

#### 3. LDS Apologetic Sources (use for every page — search all that apply)

| Source | Best For | URL |
|--------|----------|-----|
| FAIR Latter-day Saints | All topics, most comprehensive | https://www.fairlatterdaysaints.org |
| Book of Mormon Central | BOM text, archaeology, authorship, historicity | https://www.bookofmormoncentral.org |
| Interpreter Foundation | Academic peer-reviewed LDS scholarship | https://www.interpreterfoundation.org |
| Maxwell Institute / FARMS archive | Deep academic, FARMS papers pre-2012 | https://mi.byu.edu |
| BYU Studies | Peer-reviewed LDS history & scripture studies | https://byustudies.byu.edu |
| Gospel Topics Essays | Church's official position on contested topics | https://www.churchofjesuschrist.org/study/manual/gospel-topics-essays |
| Meridian Magazine | Faith-affirming commentary and devotional | https://latterdaysaintmag.com |
| Book of Mormon Evidence | BOM archaeology and geography | https://www.bookofmormonevidence.org |
| Scripture Central | Cross-reference and commentary tool | https://scripturecentral.org |
| LDS Living | Accessible faith-promoting content | https://www.ldsliving.com |

**Search pattern:** For each source, run:
- `WebSearch: "[TOPIC] site:fairlatterdaysaints.org"`
- `WebSearch: "[TOPIC] Interpreter Foundation"`
- `WebSearch: "[TOPIC] Book of Mormon Central"`
- Then WebFetch the most relevant pages and read the FULL content — not just titles

#### 4. Early Church Fathers (use when arguing Apostasy, Restoration, doctrine of God, baptism, priesthood, afterlife)

These are the opponents' own ancestors. Quote them directly — critics cannot dismiss their own tradition.

| Source | What It Contains | URL |
|--------|-----------------|-----|
| Christian Classics Ethereal Library | Full text of Ante-Nicene and Post-Nicene Fathers | https://www.ccel.org |
| New Advent — Church Fathers | Indexed by author and topic, Catholic-edited | https://www.newadvent.org/fathers |
| Early Christian Writings | Pre-Nicene texts with commentary | https://www.earlychristianwritings.com |

**Key fathers to quote by topic:**
- **Apostolic succession / authority:** Ignatius of Antioch (*Epistle to the Smyrnaeans*, ~108 AD), Irenaeus (*Against Heresies* 3.3, ~180 AD), Clement of Rome
- **Baptism for the dead / salvation of the dead:** Tertullian (*On the Resurrection*, *Against Marcion*), Origen, Cyril of Alexandria, Ambrose, Gregory the Great
- **Nature of God / pre-existence of souls:** Origen (*De Principiis*), Justin Martyr, Clement of Alexandria
- **Physical resurrection:** Irenaeus, Tertullian (*On the Flesh of Christ*), Justin Martyr (*First Apology*)
- **Apostasy foretold:** Hippolytus, Tertullian (*Prescription Against Heretics*)
- **Trinity development / controversy:** Arius, Athanasius (*On the Incarnation*), Eusebius of Caesarea

#### 5. Creeds, Confessions, and Catechisms (use when arguing against Protestant or Catholic theology)

Quote the actual text — make them defend their own documents.

| Document | Tradition | Where to Find |
|----------|-----------|---------------|
| Nicene Creed (325/381 AD) | Universal | https://www.newadvent.org/fathers/3809.htm |
| Chalcedonian Definition (451 AD) | Universal | https://www.ccel.org/creeds/chalcedon.htm |
| Athanasian Creed | Western Catholic/Lutheran | https://www.ccel.org/creeds/athanasian.creed.html |
| Westminster Confession of Faith (1646) | Reformed / Presbyterian | https://www.ccel.org/ccel/schaff/creeds3.iv.xx.html |
| Augsburg Confession (1530) | Lutheran | https://www.ccel.org/ccel/schaff/creeds3.iii.ii.html |
| 39 Articles of Religion (1563) | Anglican / Episcopalian | https://www.churchofengland.org/prayer-and-worship/worship-texts-and-resources/book-common-prayer/articles-religion |
| Catholic Catechism (1992) | Roman Catholic | https://www.vatican.va/archive/ENG0015/_INDEX.HTM |
| Baptist Faith and Message (2000) | Southern Baptist | https://bfm.sbc.net/bfm2000/ |

**Search pattern:** `WebSearch: "[DOCTRINE] Westminster Confession"` or fetch directly and search by article number.

#### 6. Historical and Academic Sources (use for historical claims, Joseph Smith, early Church, archaeology)

| Source | Best For | URL |
|--------|----------|-----|
| New Advent Catholic Encyclopedia | Historical Catholic theology, Church history | https://www.newadvent.org/cathen |
| JSTOR / Google Scholar | Peer-reviewed academic articles | https://www.jstor.org / https://scholar.google.com |
| Joseph Smith Papers | Primary source Joseph Smith documents | https://www.josephsmithpapers.org |
| Church History Library | Official LDS historical documents | https://history.churchofjesuschrist.org |
| Wikipedia (as a pointer only) | Find scholarly sources to then WebFetch directly — never cite Wikipedia itself | https://www.wikipedia.org |

**Never cite Wikipedia as a source.** Use it to find the primary source, then fetch and cite that.

---

### HTML STYLING

Follow CLAUDE.md exactly (same as ldsdoctrines.com). Key rules:
- Inline styles only
- H1: `font-size:2em; font-weight:bold; color:#1a1410; margin:0 0 6px 0`
- H2: gold underline `border-bottom:2px solid #b8860b`
- Body paragraph: `font-size:0.97em; line-height:1.8; color:#2a1f14; margin:0 0 14px 0`
- Scripture blockquote: gold accent (`border-left:3px solid #b8860b; background:#fdf8ee`)
- Reference blockquote: blue accent (`border-left:2px solid #2c4a6e; background:#f0f4f8`)
- NO `<hr>` tags
- NO `data-start`/`data-end` attributes
- NO Facebook class names
- Curly quotes: `&ldquo;` / `&rdquo;`. Em dash: `&mdash;`
- Last paragraph before a new section: `margin:0 0 28px 0`

**Article structure:**
```html
<h1 style="...">Title</h1>
<p style="font-size:1.05em; font-style:italic; color:#5c4a35; margin:0 0 28px 0;">One-sentence lede that states the attack and promises the verdict.</p>

<h2 style="...">The Attack — Stated Fairly</h2>
<p ...>State the best version of the opposing argument. Don't strawman it.</p>

<h2 style="...">Why This Fails</h2>
[Core refutation with scripture stacks]

<h2 style="...">What the Gospel of Jesus Christ Actually Teaches</h2>
[Positive case — the LDS answer, grounded in NT, OT, and Restoration scripture]

<h2 style="...">The Verdict</h2>
<p ...>Explicit statement of what is true, what is false, and why it matters. Prosecutor's close.</p>
```

---

### CONTENT STANDARD

Each article must be:
- **Complete** — no stubs, no "further reading needed," no half-arguments
- **Irrefutable** — engage the strongest version of the opposing argument, not a strawman
- **Evidence-based** — every factual claim cited; every citation real
- **Logical** — the argument must hold independently of any appeal to authority
- **Pastoral** — end with something that strengthens the reader's faith, not just defeats the critic
- **Epic** — these are the definitive responses on these topics. Write like it.

Minimum length: 800 words per article. Strong articles run 1,200–2,000 words.

---

## BATCH PAGES

*(Customize this section for the specific batch. Use the correct format below.)*

---

### FOR PHASE 1–3 SESSIONS (existing article update)

#### Article 1: [Article ID] — [Page Slug]
- **Session type:** Phase [1 / 2 / 3] — [Style-only / Expansion / Rewrite]
- **What to change:** [Specific instructions from the plan]
- **Key Facebook posts to grep for:** [keywords]
- **Preserve:** [anything that must not be touched]

#### Article 2: [Article ID] — [Page Slug]
*(repeat)*

---

### FOR PHASE 4–5 SESSIONS (new page creation)

#### Page 1: [Slug]
- **pageName / articleName:** `[Slug]` (identical)
- **articlePath:** `[Slug].html`
- **menuItemTitle:** [Human-readable full title]
- **Menu section:** [e.g. Fundamentals — get parentId from get_menu]
- **Core argument:** [2–3 sentences]
- **Key scriptures:** [list]
- **Key Facebook posts to grep for:** [keywords]
- **Key sources to WebFetch:** [FAIR / Interpreter / church fathers / creed — whichever apply]
- **Specific points to hit:**
  - [bullet]
  - [bullet]

#### Page 2: [Slug]
*(repeat)*

---

## VERIFICATION AFTER EACH PAGE

After each page:
1. `get_page` — confirm article is linked
2. `get_menu` — confirm menu item appears
3. Record the pageId and articleId in your output so the plan can be updated

At the end of the session, report:
- Pages created (title, pageId, articleId, menuId)
- Any errors or fallback patterns used
- Suggested updates to LDS-APOLOGETICS-PLAN.md checkboxes
