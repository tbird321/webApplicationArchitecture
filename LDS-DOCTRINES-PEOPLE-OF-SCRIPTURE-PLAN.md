# LDS Doctrines — People of Scripture Planning List
*Generated: 2026-05-24. Updated: 2026-05-24 — menu strategy + alphabetization.*

Planning document. Owner has resolved all flagged decisions (see USER DECISIONS APPLIED below). Pages are built using LDS-DOCTRINES-AGENT-TEMPLATE.md, 2–3 per batch, HIGH-priority first. All pages live under a single top-level **People** menu entry; tables below are alphabetized by name within each canon for ease of finding.

Cross-referenced against:
- `LDS-DOCTRINES-PLAN.md` (existing pages section + T4 character pages + menu mapping)
- `LDS-DOCTRINES-EXISTING-PAGE-IMPROVEMENTS.md`
- `E:\Apologetics\OrganizedReligion\` corpus (drafted notes flagged below)
- Facebook archive (voice signal — names the owner has personally taught about)

---

## SESSION HANDOFF — RESUME HERE AFTER `git pull`

**Last session ended:** 2026-05-24. Plan finalized (decisions applied, alphabetized, menu strategy set). **Zero People-of-Scripture pages have been built yet.** Session pivoted to a different workstation because the live MCP server was bound to `WEBSITE_ID=5` (apologetics), and the ldsdoctrines build needs `WEBSITE_ID=2`.

### Status checklist
- ✅ All 7 user decisions resolved (see USER DECISIONS APPLIED below)
- ✅ Tables alphabetized by name within each canon
- ✅ Menu strategy defined (single top-level **People** entry with 4 canonical sub-sections)
- ✅ Pre-drafted material in `E:\Apologetics\OrganizedReligion\` inventoried — those pages build first
- ⏳ **Next: build pages on ldsdoctrines.com (WEBSITE_ID=2), 10 at a time, checkpoint after each round**

### Environment setup on the new box (DO BEFORE SAYING "GO")
1. Confirm `WEBSITE_ID=2` is set permanently:
   ```powershell
   [System.Environment]::SetEnvironmentVariable('WEBSITE_ID', '2', 'User')
   ```
2. **Fully close and reopen VS Code** (reload window is NOT sufficient — the `webcms` MCP server reads `WEBSITE_ID` once at startup).
3. Verify `$env:WEBSITE_ID` shows `2` and `get_menu` returns the ldsdoctrines menu structure (not apologetics).
4. Verify the following local paths exist on the new box. If either is missing, builds still work but with reduced research signal:
   - `E:\Apologetics\OrganizedReligion\` — drafted Word docs and reference corpus
   - `E:\FacebookDownload\facebook-TheronBird-2025-04-11-jkcNtL2U\...\group_posts_and_comments.html` — voice-signal source

### Literal resume prompt for the new session
> "Pick up from `LDS-DOCTRINES-PEOPLE-OF-SCRIPTURE-PLAN.md` SESSION HANDOFF. WEBSITE_ID is 2, VS Code has been restarted. Begin Round 1: build the People menu scaffold per the plan, then report back before starting Round 2."

### Execution plan (10 at a time, with checkpoint after each round)

| Round | Scope | Agent strategy |
|---|---|---|
| **1** | **People menu scaffold.** Create top-level "People" (parentId=0, droppable=true), then 4 sub-section headers under People: `Old Testament`, `New Testament`, `Book of Mormon`, `Restoration & D&C`. Record all 5 new parentIds. | Single agent, 5 menu calls. Fast. |
| **2** | **10 HIGH-priority pages with OR drafts** (voice/research already in hand): Balaam, David Whitmer, Elisha, Isaiah, Jeremiah, Job, John the Baptist, Joseph Smith Jr., Matthew, Oliver Cowdery | 4 parallel agents × 2–3 pages each, wired under People → matching sub-section |
| **3** | **10 HIGH-priority T4 reservations:** Abraham, Alma the Younger, Ammon, Brother of Jared, Captain Moroni, Daniel, Deborah, Enoch, Ezekiel, Hannah | 4 parallel agents × 2–3 pages |
| **4** | **10 more HIGH (T4 + new):** Joseph of Egypt, King Benjamin, Lehi, Moses (absorbs `MosesLastWords` then deprecate it), Nephi (son of Lehi), Noah, Paul, Peter, Ruth, Samuel the Lamanite | 4 parallel agents × 2–3 pages |
| **5** | **10 HIGH new (BoM + NT):** Amulek, Cornelius, David, Enos, Jacob/Israel, Jacob (son of Lehi), James (the Lord's brother), Jonah, Joshua, Lazarus | 4 parallel agents × 2–3 pages |
| **6** | **10 HIGH new (NT + BoM):** Lamoni's father, Malachi, Mary (mother of Jesus), Mary Magdalene, Mary of Bethany, Melchizedek, Mormon, Moroni (son of Mormon), Nephi (son of Helaman), Nephi the disciple | 4 parallel agents × 2–3 pages |
| **7** | **Final HIGH wave (NT + BoM + Restoration):** Nicodemus, Samaritan woman at the well, Samuel, Solomon, Stephen, Stripling Warriors and Their Mothers, Three Nephites, Thomas, Woman taken in adultery, plus Restoration HIGHs (Eliza R. Snow, Emma Smith, Hyrum Smith, Joseph F. Smith, Lucy Mack Smith, Martin Harris, Three Witnesses joint) — may overflow to Round 8 | 4 parallel agents × 2–3 pages |
| **8+** | **MEDIUM tier**, then **LOW tier**, batched 10 at a time | Same pattern |

After each round: report pageIds, articleIds, menuIds, word counts, sources quoted. Stop and confirm before next round.

### Existing-page review notes (insert between rounds when convenient)
- `Eve`, `Elijah`, `TheOldTestament` — flagged for depth review in `LDS-DOCTRINES-EXISTING-PAGE-IMPROVEMENTS.md`
- `MosesLastWords` — merge target; deprecate after Moses (T4-05) is built
- `Aaron` — **kept as-is** per user decision; do not modify

### Prior-session context (for awareness only — no action on ldsdoctrines)
The prior session ran on **WEBSITE_ID=5 (ldsapologetics)** and produced:
- Update to `Revelation-Pattern` (article 585) — added James 1:6–8 + extended D&C 9 quote to v.9
- New `BOM-Kitchen-Sink` page (pageId 818, articleId 922) — Gish Gallop response to a kitchen-sink BOM critique with cross-links to deep-dive pages
- New `Laban-Sword-Steel` page (pageId 819, articleId 923) — Mt. Adir sword + Vered Jericho pick vindicate the Book of Mormon's metallurgy
None of this affects the ldsdoctrines build — listed only so the diff in `LDS-APOLOGETICS-PLAN.md` makes sense if reviewed.

### Voice reminder
The People-of-Scripture pages follow `LDS-DOCTRINES-AGENT-TEMPLATE.md` voice — direct, scripture-anchored, confident, pastoral landing. **NOT combative.** The apologetics-mode voice from the prior session does not transfer to this site.

---

## USER DECISIONS APPLIED (2026-05-24)

1. **Aaron** — Keep the existing `Aaron` page **as-is**. No new character page, no rewrite. Removed from the proposed-new-pages table.
2. **MosesLastWords** — **Merge into the planned `Moses` page (T4-05)**. The existing `MosesLastWords` content becomes part of the new Moses page. After the Moses page is built and published, `MosesLastWords` is to be deprecated (redirect or removal — decide at build time).
3. **Stripling warriors + their mothers** — **One combined page**. Slug: `Stripling-Warriors`. Covers both the 2,000 sons and the mothers whose faith armed them.
4. **Twelve Nephite disciples** — **Only Nephi the disciple** gets a page (`Nephi-Disciple`). No collective "Twelve Nephite disciples" page. (The Three Nephites page remains — that is a separate scriptural category.)
5. **Brigham Young / John Taylor / Wilford Woodruff** — **Full character pages** for each. Confirmed.
6. **PoGP angles for Adam / Enoch / Abraham / Moses** — **One page per person**, *unless* they already have one. Concretely:
   - Adam already exists → no PoGP-specific page; PoGP material folds into the existing page when it is reviewed.
   - Enoch (T4-01), Abraham (T4-03), Moses (T4-05) → each single planned page covers BOTH biblical and PoGP angles. No separate PoGP pages.
7. **Joseph Smith III / RLDS lineage** — **Excluded**. Out of scope.

---

## MENU STRATEGY

All People-of-Scripture pages live under a **single top-level "People" menu entry** on ldsdoctrines.com — for clarity and ease of finding. Within People, four sub-section headers (Old Testament, New Testament, Book of Mormon, Restoration & D&C) provide canonical grouping. Within each sub-section, items are listed **alphabetically by name**.

**Proposed menu structure:**

```
People (NEW top-level — parentId TBD; create with parentId=0, droppable=true)
├── Old Testament (sub-section header — droppable=true under People)
│   ├── Aaron
│   ├── Abigail (OT)
│   ├── Abraham
│   ├── Adam
│   └── … (alphabetical)
├── New Testament (sub-section header)
│   ├── Andrew
│   ├── Anna the prophetess
│   └── … (alphabetical)
├── Book of Mormon (sub-section header)
│   ├── Abinadi
│   ├── Abish
│   └── … (alphabetical)
└── Restoration & D&C (sub-section header)
    ├── Brigham Young
    ├── David Whitmer
    └── … (alphabetical)
```

**Implementation notes for build time:**

1. Call `get_menu` first to confirm whether a "People" parent already exists. If not, create it: `add_menu_item(parentId=0, itemTitle="People")` — record the returned id as the People parentId.
2. Create the four sub-section headers under People: `Old Testament`, `New Testament`, `Book of Mormon`, `Restoration & D&C`.
3. As each character page is built (page → article → set_article_content → update_page → publish), add its menu item under the matching sub-section in **alphabetical position**.
4. **Existing pages currently wired into other menu sections** (Adam, Eve, Cain, Aaron, Elijah, MosesLastWords, Abigail, Esther, TheOldTestament) should be moved to the new People menu structure:
   - Easiest path: add a NEW menu item under People that points to the existing pageId (the page itself doesn't move; only the menu pointer is added).
   - Optional: remove the old menu entry afterward. Decide per-page at build time — some may justify staying in two places.
5. **Alphabetization at build time:** if `add_menu_item` always appends to the end of the parent, build pages alphabetically within each batch so insertion order produces an alphabetical menu. If the CMS supports reordering, do a one-time menu sort after each canon section completes.

---

## ALREADY COVERED ON LDSDOCTRINES.COM
*(Existing pages. Move into the new People menu at build time; do not duplicate.)*

| Person | Existing Page / Slug | Note |
|--------|----------------------|------|
| Aaron | `Aaron` | EXISTS — **KEEP AS-IS** per user decision; do not rewrite or add a parallel page |
| Abigail | `Abigail` | EXISTS — gold-standard narrative reference |
| Adam | `Adam` | EXISTS — full page |
| Adam & Eve (joint) | `TheFall` | EXISTS — covers the Fall doctrine |
| Cain | `Cain` | EXISTS |
| Elijah | `Elijah` | EXISTS — flagged for review re: D&C 110 / translation |
| Esther | `Esther` | EXISTS |
| Eve | `Eve` (slug 299) | EXISTS — flagged in IMPROVEMENTS as possibly shallow |
| Moses (partial) | `MosesLastWords` | EXISTS — **to be merged into planned `Moses` page (T4-05)** then deprecated |
| OT (umbrella) | `TheOldTestament` | EXISTS — unknown depth |

## ALREADY PLANNED (NOT YET BUILT) IN LDS-DOCTRINES-PLAN.md TIER 4
*(These slugs are reserved in the existing plan. Honored as-is; do not redefine.)*

| Person | Planned Slug | Plan Location |
|--------|--------------|---------------|
| Abinadi | `Abinadi` | T4-18 |
| Abraham | `Abraham` | T4-03 |
| Alma the Younger | `Alma-Younger` | T4-17 |
| Ammon | `Ammon` | T4-19 |
| Brother of Jared | `Brother-Of-Jared` | T4-14 |
| Captain Moroni | `Captain-Moroni` | T4-15 |
| Daniel | `Daniel` | T4-13 |
| Deborah | `Deborah` | T4-06 |
| Elisha | `Elisha` | T4-09 |
| Enoch | `Enoch` | T4-01 |
| Ezekiel | `Ezekiel` | T4-12 |
| Gideon | `Gideon` | T4-26 |
| Hannah | `Hannah` | T4-08 |
| Hosea | `Hosea` | T4-25 |
| Isaiah | `Isaiah-Prophet` | T4-10 |
| Jeremiah | `Jeremiah` | T4-11 |
| Job | `Job` | T4-24 |
| Joseph of Egypt | `Joseph-Egypt` | T4-04 |
| King Benjamin | `King-Benjamin` | T4-20 |
| Lehi | `Lehi` | T4-21 |
| Moses | `Moses` | T4-05 |
| Nephi (son of Lehi) | `Nephi` | T4-22 |
| Noah | `Noah` | T4-02 |
| Paul | `Paul` | T4-28 |
| Peter | `Peter` | T4-27 |
| Ruth | `Ruth` | T4-07 |
| Samuel the Lamanite | `Samuel-Lamanite` | T4-16 |
| Solomon | `Solomon` | T4-23 |

**Implication:** Where a slug is already reserved in T4, the existing slug is honored. The full People-of-Scripture build includes everyone above (so the user has a single people-list) **and** adds the missing figures.

---

## OWNER'S DRAFTED MATERIAL (in E:\Apologetics\OrganizedReligion)
*(People for whom the user has already drafted Word documents or notes — these should be the **first** pages built since the voice/research is already in hand.)*

| Person | File path | Brief description |
|--------|-----------|-------------------|
| Adam | `E:\Apologetics\OrganizedReligion\Subjects\People\Adam.doc` | Legacy `.doc` — character notes |
| Alma | `E:\Apologetics\OrganizedReligion\Lessons\Alma 5-7\Introduction to the Sermon of Alma.doc` + `Sermon of Alma.doc` | Alma 5–7 set |
| Balaam | `E:\Apologetics\OrganizedReligion\Subjects\Balaam\AProphetsMadness.docx` and `Balaam.txt` | Full Word draft — strong candidate |
| Bathsheba | `E:\Apologetics\OrganizedReligion\Subjects\Bathsheba\Lineage.docx` | Lineage / Christ ancestry notes |
| David & Goliath | `E:\Apologetics\OrganizedReligion\Subjects\People\DavidGoliath\Facts.txt` | Sharing-time facts (also `Sharingtime\David and Goliath.txt`) |
| David Whitmer (witness) | `E:\Apologetics\OrganizedReligion\BookOfMormon\TheThreeWitnesses\DavidWhitmer\DavidWhitmerTestimony.txt` + `AdditionalReferences.txt` | Three-witness research |
| Elijah | `E:\Apologetics\OrganizedReligion\Lessons\1 Kings 17-19\Elijah and the Priests.docx` + `Quotes.docx` | Teaching set (existing site page may not use this) |
| Elisha | `E:\Apologetics\OrganizedReligion\Lessons\2Kings 2-6\2Kings 2.docx` + `Poetic Form - Elisha called of God.txt` + `Summary.txt` | Multi-file teaching set |
| Isaiah | `E:\Apologetics\OrganizedReligion\Lessons\IsaiahMortalMessiah\IsaiahMortalMessiah.docx` + `IsaiahMellenialMessiah.docx` + `Lessons\IsaiahMortalMessiah\Isaiah Chapter53.txt` + `Isaiah63.txt` + `ScripturalCommentary\OldTestament\Isaiah1-6\*` + `Subjects\IsaiahVariants in Book of Mormon\Isaiah Chapter 1.docx` | Heavy, multi-file Isaiah corpus |
| Jeremiah | `E:\Apologetics\OrganizedReligion\ScripturalCommentary\OldTestament\Jeremiah\JeremiahLesson1.docx` + `JeremiahLesson2.docx` + `ForeOrdianation.docx` | Multi-lesson set |
| Job | `E:\Apologetics\OrganizedReligion\Lessons\Job\Job.doc` | Single legacy lesson |
| John the Baptist | `E:\Apologetics\OrganizedReligion\Subjects\People\JohnTheBaptist.docx` | Full Word draft |
| Joseph Smith | `E:\Apologetics\OrganizedReligion\Subjects\Joseph Smith\Joseph Smith - Theoretical Physics.doc` + `JosephAMartyr.txt` + `What is the best concrete evidence .txt` + `Lessons\JosephSmith- My Word through you\JosephSmith.doc` + folder `Treasure Digging\` | Multi-file Joseph Smith corpus |
| Lehi | `E:\Apologetics\OrganizedReligion\Subjects\People\LEHI.WRI` | Legacy WordPerfect/Write file |
| Lehi / Nephi (1 Nephi 1–2) | `E:\Apologetics\OrganizedReligion\Lessons\1Nephi1-2.doc.docx` | Lesson covering Lehi's call + Nephi's response |
| Matthew | `E:\Apologetics\OrganizedReligion\Subjects\NewTestament Commentary\Matthew\Who was Matthew.docx` | Full Word draft on the apostle Matthew |
| Mormon | `E:\Apologetics\OrganizedReligion\Lessons\3Nephi1-5\Mormon and His Book.txt` | Notes on Mormon as compiler/prophet |
| Nephi | `E:\Apologetics\OrganizedReligion\Subjects\People\NEPHI.WRI` | Legacy file |
| Noah | `E:\Apologetics\OrganizedReligion\Subjects\People\NOAH.docx` and `Subjects\Flood\NOAH.docx` | Full Word draft on Noah and the Flood |
| Oliver Cowdery | `E:\Apologetics\OrganizedReligion\Subjects\TheThreeWitnesses\Oliver Cowdery.doc` + `OliverCowdry.txt` + `OliverCowdryNeg.txt` | Three-witness file set (negative file is helpful for honest treatment) |
| Christ (do not duplicate — for reference) | `E:\Apologetics\OrganizedReligion\Subjects\People\CHRIST.WRI`, `CHRIST\YOUTH.WRI`, `Subjects\TheLifeofChrist\CHSTWIFE.docx` | Christ pages live elsewhere on the site; not in scope here |
| Satan (excluded) | `E:\Apologetics\OrganizedReligion\Subjects\People\SATAN.WRI` | Found-Satan already exists; out of scope here |

**Discussion `.doc` files (cannot easily inventory by Read tool — listed for awareness):**
- `E:\Apologetics\OrganizedReligion\Discussions\Discussion 2.doc`
- `E:\Apologetics\OrganizedReligion\Discussions\Discussion 3.doc`

---

## VOICE-SIGNAL READING (from Facebook archive)
*(Raw mention counts in `group_posts_and_comments.html` — names the owner has personally taught about most often. Used to flag HIGH priority.)*

| Name | Mentions |
|------|----------|
| Alma | ~2,606 |
| Nephi | ~1,683 |
| Abraham | ~1,048 |
| Moses | ~973 |
| Peter | ~921 |
| Isaiah | ~690 |
| Paul | ~292 |
| Mary Magdalene / Joseph Smith / Brigham Young / Job / Esther / Ruth (combined) | ~416 |

These counts are noisy (Alma also = the city; Peter also = surname), but the relative pattern confirms: **Alma the Younger, Nephi, Abraham, Moses, Peter, Isaiah, and Paul are top-tier voice anchors** and should all be HIGH priority.

---

## PROPOSED NEW PAGES — OLD TESTAMENT
*(Alphabetized. Pages already in T4 of the existing plan are marked **[T4]** with their reserved slug; pages already live are marked **[EXISTS]**.)*

| Person | Era | Primary Scripture | Lesson Cluster | Priority |
|--------|-----|-------------------|----------------|----------|
| Aaron **[EXISTS — DO NOT MODIFY]** | Exodus | Exodus 4–32; Leviticus 8–10 | (existing page kept as-is per user decision; no rewrite) | — |
| Abigail (OT) **[EXISTS]** | United Kingdom | 1 Samuel 25 | The peacemaker who stopped a vendetta — gold-standard narrative page | HIGH (already live) |
| Abraham **[T4-03]** | Patriarchal | Genesis 12–22; Abraham 1–3 | Faith under test; covenant of seed; intercession for Sodom; binding of Isaac | HIGH |
| Adam **[EXISTS]** | Patriarchal | Genesis 1–5; Moses 1–6 | The first man, first covenant, first repentance; the Fall as forward motion | HIGH (review depth) |
| Ahab | Divided Kingdom | 1 Kings 16–22 | The weak king who let evil rule the house; Naboth's vineyard | LOW |
| Amos | Prophets | Amos 5, 7, 9 | The herdsman called against luxury; justice rolling like waters | MEDIUM |
| Balaam | Wilderness | Numbers 22–24; 2 Peter 2:15 | A prophet who heard God and still served money; the donkey that saw | MEDIUM (drafted) |
| Bathsheba | United Kingdom | 2 Samuel 11–12; 1 Kings 1; Matthew 1:6 | Sin, grief, restoration — the mother of Solomon; grace in the lineage | MEDIUM |
| Boaz | Judges | Ruth 2–4 | Kinsman-redeemer as type of Christ; gentleness of righteous power | MEDIUM |
| Caleb | Wilderness | Numbers 13–14; Joshua 14 | "I wholly followed the Lord" — the man who got his mountain at 85 | MEDIUM |
| Daniel **[T4-13]** | Exile | Daniel 1–7, 12 | Covenant integrity in Babylon; the stone cut without hands | HIGH |
| David | United Kingdom | 1 & 2 Samuel; Psalms | Shepherd, king, psalmist, sinner, repentant — the most layered OT life | HIGH |
| Deborah **[T4-06]** | Judges | Judges 4–5 | Prophetess, judge, commander; God uses whomever is willing | HIGH |
| Elijah **[EXISTS]** | Divided Kingdom | 1 Kings 17–19; 2 Kings 2; D&C 110; 3 Nephi 25 | Translated prophet, sealing keys restored at Kirtland, the still small voice | HIGH (review existing) |
| Elisha **[T4-09]** | Divided Kingdom | 2 Kings 2–13 | The double portion; the unseen chariots of fire; sixteen miracles | HIGH |
| Enoch **[T4-01]** | Antediluvian | Moses 6–7; Genesis 5 | Walking with God; the city translated; God who weeps | HIGH |
| Esther **[EXISTS]** | Post-Exile | Book of Esther | Providence in the silence of God; for such a time as this | HIGH (already live) |
| Eve **[EXISTS]** | Patriarchal | Genesis 2–4; Moses 4–5 | The "transgression" reframed; mother of all living; partner in the Fall | HIGH (review depth) |
| Ezekiel **[T4-12]** | Exile | Ezekiel 1, 36–37, 40–48 | Valley of dry bones; the two sticks; visionary prophet of restoration | HIGH |
| Ezra | Post-Exile | Book of Ezra; Nehemiah 8 | The scribe who set his heart to seek the law; restoration of Torah | MEDIUM |
| Gideon **[T4-26]** | Judges | Judges 6–8 | "The Lord is with thee, mighty man of valour" — divine sufficiency in weakness | HIGH |
| Goliath (David vs.) | United Kingdom | 1 Samuel 17 | Faith over fear; the giant that falls to a stone (already drafted) | MEDIUM |
| Habakkuk | Prophets | Habakkuk 2–3 | "The just shall live by his faith" — wrestling honestly with God | MEDIUM |
| Hagar | Patriarchal | Genesis 16, 21 | God who sees the cast-out; the well of the Living One who sees | MEDIUM |
| Haggai | Prophets | Book of Haggai | "Consider your ways" — putting God's house first | LOW |
| Hannah **[T4-08]** | Judges/Kings transition | 1 Samuel 1–2; Luke 1:46–55 | Barren prayer answered; the Magnificat's OT model | HIGH |
| Hezekiah | Late Kingdom | 2 Kings 18–20; Isaiah 36–39 | The king who turned to the wall; fifteen years added | MEDIUM |
| Hosea **[T4-25]** | Prophets | Hosea 1–3, 11, 14 | Covenant love that does not let go of the unfaithful | MEDIUM |
| Isaac | Patriarchal | Genesis 21–27 | The promised son; the willing sacrifice; the quiet covenant | MEDIUM |
| Isaiah **[T4-10]** | Late Kingdom | Isaiah 6, 53, 61; 2 Nephi 25 | "Here am I, send me"; the Suffering Servant; Messianic seer | HIGH |
| Jacob / Israel | Patriarchal | Genesis 25–35, 48–49 | Wrestling at Peniel; the new name; the twelve tribes set | HIGH |
| Jephthah | Judges | Judges 11 | Rash vows; the cost of speaking carelessly before God | LOW |
| Jeremiah **[T4-11]** | Exile | Jeremiah 1, 32; Lamentations 3 | Foreordained before birth; the field bought as Jerusalem burned | HIGH |
| Jeroboam | Divided Kingdom | 1 Kings 11–14 | The king who built false altars to keep power; sin that names a dynasty | LOW |
| Jethro | Exodus | Exodus 18 | Counsel from outside the camp; delegation as priesthood pattern | LOW |
| Jezebel | Divided Kingdom | 1 Kings 16–21; 2 Kings 9 | The corruption of Israel from the throne; consequences of strange gods | LOW |
| Job **[T4-24]** | Patriarchal (book) | Job 1–2, 38–42 | Suffering without explanation; integrity preserved; the whirlwind answer | HIGH |
| Joel | Prophets | Joel 2 | Pour out my Spirit on all flesh — the last-days promise | MEDIUM |
| Jonah | Prophets | Book of Jonah | The runaway prophet; mercy on the city he wanted destroyed | HIGH |
| Jonathan | United Kingdom | 1 Samuel 14, 18–20 | Loyalty beyond inheritance; covenant friendship | MEDIUM |
| Joseph of Egypt **[T4-04]** | Patriarchal | Genesis 37–50; 2 Nephi 3 | Sold for silver, raised to save; the most complete OT type of Christ | HIGH |
| Joshua | Conquest | Joshua 1–24 | Be strong and of a good courage; covenant renewal; "as for me and my house" | HIGH |
| Josiah | Late Kingdom | 2 Kings 22–23 | The boy king who rediscovered the law; reform too late to stop judgment | MEDIUM |
| Judah | Patriarchal | Genesis 38, 44 | The brother who pledged himself; lineage of David and Christ | MEDIUM |
| Lot | Patriarchal | Genesis 13–14, 19 | The cost of pitching one's tent toward Sodom | MEDIUM |
| Malachi | Prophets | Malachi 3–4 | Tithing window; Elijah the prophet shall be sent; the day that burns | HIGH |
| Melchizedek | Patriarchal | Genesis 14; JST Genesis 14; Hebrews 7; Alma 13 | The priesthood after his order; type of Christ; king of Salem | HIGH |
| Methuselah | Antediluvian | Genesis 5; Moses 8 | Longevity as a witness; the man whose death triggered the flood | LOW |
| Methuselah / Lamech (Noah's father) | Antediluvian | Genesis 5; Moses 8 | The hope baked into a son's name | LOW |
| Micah | Prophets | Micah 5, 6 | Bethlehem prophesied; "what doth the Lord require of thee" | MEDIUM |
| Miriam | Exodus | Exodus 2, 15; Numbers 12 | The watchful sister; the song at the sea; the cost of envy of authority | MEDIUM |
| Mordecai | Post-Exile | Book of Esther | Quiet faithfulness behind the courageous; honor delayed but given | LOW |
| Moses **[T4-05]** | Exodus | Moses 1; Exodus 3–34; Deuteronomy 34 | Face to face with God; the pattern of deliverance; lawgiver and type of Christ | HIGH |
| Naaman | Divided Kingdom | 2 Kings 5 | The general who almost despised the simple command; obedience and pride | MEDIUM |
| Nahum | Prophets | Book of Nahum | The fall of Nineveh — mercy received does not last forever | LOW |
| Nathan | United Kingdom | 2 Samuel 7, 12 | "Thou art the man" — the prophet who confronts kings | MEDIUM |
| Nehemiah | Post-Exile | Book of Nehemiah | Rebuilding the wall; prayer plus action; opposition and integrity | MEDIUM |
| Noah **[T4-02]** | Antediluvian | Genesis 6–9; Moses 8 | 120 years of warning; obedience without precedent; the rainbow covenant | HIGH |
| Obadiah | Prophets | Obadiah 1 | The shortest OT book; judgment on Edom; the day of the Lord | LOW |
| Rachel & Leah | Patriarchal | Genesis 29–35 | Loved and unloved; the long providence behind the mothers of Israel | MEDIUM |
| Rahab | Conquest | Joshua 2, 6; Matthew 1:5; Hebrews 11:31 | Faith from outside Israel; the scarlet cord as type of the Atonement | MEDIUM |
| Rebekah | Patriarchal | Genesis 24, 27 | Hospitality at the well; revelation about the elder serving the younger | MEDIUM |
| Rehoboam | Divided Kingdom | 1 Kings 12 | The young man who would not listen to old counsel | LOW |
| Ruth **[T4-07]** | Judges | Ruth 1–4 | Hesed across every boundary; Boaz as kinsman-redeemer; the line to Christ | HIGH |
| Samson | Judges | Judges 13–16 | Strength misused; the Nazarite who failed; mercy in the final pillar | MEDIUM |
| Samuel | United Kingdom | 1 Samuel 1–16, 28 | The boy who heard God speak; the prophet who anointed kings | HIGH |
| Sarah | Patriarchal | Genesis 12, 17–18, 21 | The promise that outlasts the laughter; mother of nations | MEDIUM |
| Saul | United Kingdom | 1 Samuel 9–31 | The king who started well and ended consulting a witch — pride is the root | MEDIUM |
| Shadrach, Meshach, Abednego | Exile | Daniel 3 | "But if not" — faith that does not require deliverance | MEDIUM |
| Shem / Ham / Japheth | Post-Flood | Genesis 9–10 | Nations from a single family; the blessing and the curse | LOW |
| Shunammite woman | Divided Kingdom | 2 Kings 4 | Hospitality, loss, and resurrection; "It is well" | MEDIUM |
| Solomon **[T4-23]** | United Kingdom | 1 Kings 3–11; Proverbs; Ecclesiastes | Wisdom asked for, apostasy chosen — slow compromise destroys | HIGH |
| Tamar | Patriarchal | Genesis 38 | Justice from the margins; the unexpected mother of Christ's line | MEDIUM |
| Widow of Zarephath | Divided Kingdom | 1 Kings 17 | Faith of the last meal; the cruse that did not fail | MEDIUM |
| Zechariah (OT) | Prophets | Zechariah 9, 12–14 | The pierced one; the King who comes riding on an ass; latter-day visions | MEDIUM |
| Zephaniah | Prophets | Zephaniah 3:17 | God in the midst of His people, rejoicing over them with singing | LOW |
| Zerubbabel | Post-Exile | Ezra 3–6; Haggai 2; Zechariah 4 | Rebuilding the temple; "not by might, nor by power, but by my Spirit" | LOW |

---

## PROPOSED NEW PAGES — NEW TESTAMENT
*(Alphabetized.)*

| Person | Primary Scripture | Lesson Cluster | Priority |
|--------|-------------------|----------------|----------|
| Andrew | John 1, 6; Mark 1 | The brother who brought people to Christ — including Peter | MEDIUM |
| Anna the prophetess | Luke 2:36–38 | Eighty-four years of fasting; recognizing the Christ child | MEDIUM |
| Apollos | Acts 18; 1 Corinthians 1–3 | Eloquent learner — discipled by Aquila and Priscilla | LOW |
| Barnabas | Acts 4, 9, 11–15 | The son of consolation; the man who vouched for Saul | MEDIUM |
| Bartholomew / Nathanael | John 1 | "Can any good thing come out of Nazareth?" — guile-less Israelite | LOW |
| Bartimaeus | Mark 10 | Persistent cry for mercy that Jesus would not silence | MEDIUM |
| Centurion of Capernaum | Matt 8; Luke 7 | "Speak the word only, and my servant shall be healed" | MEDIUM |
| Cleopas and the Emmaus disciple | Luke 24 | Hearts burning as He opened the scriptures on the road | MEDIUM |
| Cornelius | Acts 10 | The gentile centurion — gospel opens to the world | HIGH |
| Elisabeth | Luke 1 | The cousin who recognized the Christ in utero | MEDIUM |
| James (son of Alphaeus) | Matt 10; Mark 3 | The lesser-known apostle; faithfulness without spotlight | LOW |
| James (son of Zebedee) | Matt 17; Acts 12 | First apostolic martyr; the brother on the mount | MEDIUM |
| James (the Lord's brother) | Acts 15; Galatians 1–2; Epistle of James | "Faith without works is dead" — the rock of the Jerusalem church | HIGH |
| Joanna | Luke 8, 24 | Steward's wife who funded the ministry; resurrection witness | LOW |
| John the Baptist | Matthew 3; Luke 1, 3; John 1; D&C 13 | The voice in the wilderness; Aaronic keys restored | HIGH (drafted) |
| John the Beloved | John 13–21; Revelation; 3 Nephi 28; D&C 7 | The translated apostle who tarries till He comes | HIGH |
| Joseph (the carpenter) | Matthew 1–2; Luke 2 | The quiet just man; obedience without spotlight | HIGH |
| Joseph of Arimathaea | Matt 27; Mark 15; John 19 | The rich man who buried Him in his own new tomb | MEDIUM |
| Judas Iscariot | Matt 26–27; John 13; Acts 1 | The cost of harboring covetousness in covenant company | MEDIUM |
| Jude | Epistle of Jude | Contending earnestly for the faith once delivered | MEDIUM |
| Lazarus | John 11–12 | The friend Jesus wept for; the public proof of resurrection power | HIGH |
| Lois and Eunice | 2 Timothy 1:5 | Grandmother and mother whose faith made Timothy | MEDIUM |
| Luke | Luke; Acts; Colossians 4 | The beloved physician; the careful historian of the gospel | MEDIUM |
| Lydia | Acts 16 | First convert in Europe; "whose heart the Lord opened" | MEDIUM |
| Mark | Mark; Acts 13, 15; 2 Timothy 4 | The young man who failed once and was restored to usefulness | MEDIUM |
| Martha of Bethany | Luke 10; John 11 | Faithful service and "I am the resurrection" said to her | MEDIUM |
| Mary (mother of Jesus) | Luke 1–2; John 2, 19 | "Be it unto me"; the Magnificat; the sword that pierced her own soul | HIGH |
| Mary Magdalene | Luke 8; John 19–20 | Cast out of, restored, first witness of the resurrection | HIGH |
| Mary of Bethany | Luke 10; John 11–12 | The one who chose the better part; anointed Him for burial | HIGH |
| Matthew (Levi) | Matt 9; Mark 2; Luke 5 | The tax collector who left everything at the table | MEDIUM (drafted) |
| Matthias | Acts 1 | The replacement chosen by revelation; the quorum kept full | LOW |
| Nicodemus | John 3, 7, 19 | The night-secret seeker who anointed Him publicly at the tomb | HIGH |
| Onesimus | Philemon | The runaway servant returned as a beloved brother | LOW |
| Paul **[T4-28]** | Acts 9, 13–28; Romans; Galatians; 2 Timothy 4 | Damascus road; the most unlikely apostle; finished the course | HIGH |
| Peter **[T4-27]** | Matt 16; Luke 22; John 21; Acts 2–5, 10 | Impulsive faith, denial, restoration, foundation of the apostolic church | HIGH |
| Phebe | Romans 16:1–2 | A servant of the church; Paul's letter-bearer to Rome | LOW |
| Philemon | Philemon | Forgiveness asked and given between brothers in Christ | LOW |
| Philip (the apostle) | John 1, 6, 14 | "Lord, show us the Father"; quiet seeker | LOW |
| Philip the Evangelist | Acts 6, 8, 21 | The Spirit-led missionary; baptizing the Ethiopian eunuch | MEDIUM |
| Priscilla and Aquila | Acts 18; Romans 16; 1 Cor 16 | Tentmaker couple who taught Apollos more perfectly | MEDIUM |
| Rich young ruler | Matt 19; Mark 10; Luke 18 | The good young man who would not pay the price | MEDIUM |
| Salome (mother of James & John) | Matt 20; Mark 15–16 | "Grant that these my two sons may sit..." — ambition rebuked, devotion kept | LOW |
| Samaritan woman at the well | John 4 | The first explicit "I am" given to an outsider woman | HIGH |
| Sapphira and Ananias | Acts 5 | The deception that broke the unity of consecration | LOW |
| Silas | Acts 15–18 | Singing in the Philippian jail; the partner in suffering | LOW |
| Simeon | Luke 2:25–35 | The man who could die because his eyes had seen salvation | MEDIUM |
| Simon the Zealot | Matt 10 | From political revolutionary to apostle of the Lamb | LOW |
| Stephen | Acts 6–7 | First Christian martyr; saw the standing Son of Man | HIGH |
| Thaddeus / Judas (not Iscariot) | John 14:22 | "How is it that thou wilt manifest thyself unto us, and not unto the world?" | LOW |
| Thomas | John 11, 14, 20 | The doubt that became the strongest confession: "My Lord and my God" | HIGH |
| Timothy | 1 & 2 Timothy; Acts 16 | The young pastor mentored by Paul; faith of Lois and Eunice | MEDIUM |
| Titus | Titus | Ordained for Crete; setting things in order | LOW |
| Woman taken in adultery | John 8 | "Neither do I condemn thee — go, and sin no more" | HIGH |
| Zacchaeus | Luke 19 | Restitution as fruit of repentance; "today is salvation come to this house" | MEDIUM |
| Zacharias | Luke 1 | The priest struck silent; faith that doubted and was still answered | MEDIUM |

---

## PROPOSED NEW PAGES — BOOK OF MORMON
*(Alphabetized.)*

| Person | Primary Scripture | Lesson Cluster | Priority |
|--------|-------------------|----------------|----------|
| Abinadi **[T4-18]** | Mosiah 11–17 | Returned in disguise, burned alive, one convert started a movement | HIGH |
| Abish | Alma 19 | The Lamanite servant whose private testimony broke open a public miracle | MEDIUM |
| Alma the Elder | Mosiah 18, 23–24; Alma 5 | The priest who repented; founded the church at the Waters of Mormon | HIGH |
| Alma the Younger **[T4-17]** | Mosiah 27; Alma 36 | Total reversal through Christ; the bitterness of repentance, sweetness of redemption | HIGH |
| Amulek | Alma 8–16, 34 | "I would that ye should hearken unto my words" — the great sermon on the Atonement | HIGH |
| Brother of Jared **[T4-14]** | Ether 2–3 | Pierced the veil — the finger of the Lord touching the stones | HIGH |
| Captain Moroni **[T4-15]** | Alma 43–62 | If all men had been like him — righteous warrior | HIGH |
| Coriantumr (Jaredite king) | Ether 13–15; Omni 1:21 | The last Jaredite — lived to meet Mulek's people | MEDIUM |
| Enos | Enos 1 | Hunger of the soul; the all-night wrestle for forgiveness | HIGH |
| Ether | Ether 12–15 | The prophet who lived in a cave and watched a civilization die | MEDIUM |
| Gideon (Nephite) | Mosiah 19–22; Alma 1 | The faithful soldier-judge martyred by Nehor | LOW |
| Helaman I (son of Alma) | Alma 45–62; Alma 56–58 | The father of the stripling warriors' captain; record-keeper | MEDIUM |
| Helaman II (son of Helaman I) | Helaman 1–5 | The chief judge who passed the seat back to faithful family | MEDIUM |
| Ishmael / Ishmael's wife | 1 Nephi 7, 16 | The other family on the journey; daughters who married into Lehi's house | LOW |
| Jacob (son of Lehi) | 2 Nephi 6–10; Jacob 1–7 | Born in tribulation; preached the great and last sacrifice | HIGH |
| Jared (Jaredite founder) | Ether 1–6 | "Cry unto the Lord" — the language preserved | MEDIUM |
| Jarom / Omni / Amaron / Chemish / Abinadom / Amaleki | Jarom; Omni | The quiet keepers of the record; faithfulness without spectacle | LOW |
| Joseph (son of Lehi) | 2 Nephi 3 | The prophecy about Joseph Smith from the wilderness | MEDIUM |
| King Benjamin **[T4-20]** | Mosiah 2–6 | The sermon that changed a nation; natural-man doctrine | HIGH |
| King Lamoni | Alma 17–19 | The king laid as if dead — three days of darkness become light | MEDIUM |
| King Noah (wicked) | Mosiah 11–19 | The cost of priestly corruption; fire at the end of luxury | MEDIUM |
| Korihor | Alma 30 | The anti-Christ whose mouth was shut; the wages of mocking God | MEDIUM |
| Laman and Lemuel | 1 Nephi 2–18 | The brothers who never softened — what hardness of heart looks like | MEDIUM |
| Lamoni's father | Alma 22 | "I will give away all my sins to know thee" — a king's whole-soul prayer | HIGH |
| Lamoni's queen | Alma 19 | "Blessed art thou for thy exceeding faith" — faith greater than the kings' | MEDIUM |
| Lehi **[T4-21]** | 1 Nephi 1–2; 2 Nephi 1–4 | Left everything because God said so; dying patriarchal blessing | HIGH |
| Lehi (Nephite captain) | Alma 49–62 | The trusted right hand of Captain Moroni | LOW |
| Lehi (son of Helaman) | Helaman 5, 10 | The brother encircled by fire with Nephi in prison | MEDIUM |
| Limhi | Mosiah 7–8, 19–22 | The king whose people repented from under bondage | MEDIUM |
| Mormon | Mormon 1–7; Words of Mormon | The boy historian who compiled a thousand years of records | HIGH |
| Moroni (son of Mormon) | Mormon 8–9; Moroni 1–10 | The lone survivor who finished the record and delivered the plates | HIGH |
| Mosiah I | Omni 1:12–22 | The king led by the Lord out of Nephi to discover Zarahemla | MEDIUM |
| Mosiah II | Mosiah 6, 25–29 | The king who gave up the throne to establish judges | MEDIUM |
| Mulek (and the people of Zarahemla) | Helaman 6:10; Omni 1:14–19 | The escape from Jerusalem nobody knew about | LOW |
| Nehor | Alma 1 | The first priestcraft preacher in the new republic | LOW |
| Nephi (son of Helaman) | Helaman 5, 7–11 | Sealing power given; rain withheld and granted on a prophet's word | HIGH |
| Nephi (son of Lehi) **[T4-22]** | 1 Nephi 1–18; 2 Nephi 31–33 | "I will go and do"; doctrine of Christ in its clearest statement | HIGH |
| Nephi the disciple (3 Nephi) — slug `Nephi-Disciple` | 3 Nephi 1, 7, 11, 19 | The chief disciple who saw the Lord come down out of heaven | HIGH |
| Nephi's wife (unnamed) | 1 Nephi 7, 18 | The faithful unnamed — God remembers what scripture does not record | LOW |
| Orihah | Ether 7 | The first righteous king of the new world | LOW |
| Pahoran | Alma 60–62 | "I rejoiced in the greatness of your heart" — meekness under accusation | MEDIUM |
| Sam | 1 Nephi 2; 2 Nephi 4:11 | The faithful brother who stood with Nephi without seeking lead | LOW |
| Samuel the Lamanite **[T4-16]** | Helaman 13–15 | Outsider preaching from the wall; signs of birth and death | HIGH |
| Sariah | 1 Nephi 5 | The faithful mother who grew through the wilderness | MEDIUM |
| Sherem | Jacob 7 | The first recorded anti-Christ — confessed at the end | LOW |
| Shiz | Ether 14–15 | The other half of the final battle; pride's terminal velocity | LOW |
| Sons of Mosiah — Aaron | Alma 17, 21–22 | Taught King Lamoni's father — gospel from Adam through Christ | MEDIUM |
| Sons of Mosiah — Ammon **[T4-19]** | Alma 17–19, 26 | The greatest missionary in the Book of Mormon | HIGH |
| Sons of Mosiah — Omner & Himni | Alma 17 | The two whose deeds are quietly counted with the four | LOW |
| Stripling Warriors and Their Mothers (combined) — slug `Stripling-Warriors` | Alma 53, 56–58; Alma 56:47–48 | "We do not doubt our mothers knew it"; the 2,000 sons and the unnamed women whose faith armed an army | HIGH |
| Teancum | Alma 50–62 | Personal valor that ended two Lamanite kings | LOW |
| Three Nephites (The) | 3 Nephi 28 | Translated; tarrying till He comes; ministering hidden among us | HIGH |
| Zeezrom | Alma 11–15 | The lawyer who tried to entrap Alma and Amulek and was converted | MEDIUM |
| Zeniff | Mosiah 9–10 | The over-zealous return that cost a generation | LOW |
| Zoram | 1 Nephi 4; 2 Nephi 1:30–32 | The servant who became a brother through covenant | LOW |

---

## PROPOSED NEW PAGES — RESTORATION & D&C
*(Alphabetized by first word as written. PoGP-specific angles fold into the main planned pages for Enoch, Abraham, and Moses per User Decision #6.)*

| Person | Primary Source | Lesson Cluster | Priority |
|--------|---------------|----------------|----------|
| Brigham Young | History of the Church; D&C 124, 136 | The colonizer-prophet; the Mountain West church | MEDIUM |
| David Whitmer | D&C 17, 18; Three Witnesses | The three witnesses; the man who left but never denied | HIGH (drafted) |
| Eight Witnesses (joint) | Testimony in BoM | "Hefted; we did handle with our hands" | MEDIUM |
| Eliza R. Snow | "O My Father" hymn; RS history | Heavenly Mother in hymn; Relief Society second president | HIGH |
| Emma Smith | D&C 25 | The elect lady; the costs and witness of her place | HIGH |
| Emmeline B. Wells | RS history; suffrage writings | The voice of LDS women in the public square | MEDIUM |
| Hyrum Smith | D&C 11, 23, 124, 135 | The elder brother who died with the prophet | HIGH |
| John Taylor | D&C 135; martyrdom narrative | "I shall ring through all eternity" | MEDIUM |
| Joseph F. Smith | D&C 138 | The vision of the redemption of the dead | HIGH |
| Joseph Smith Jr. | JS-H 1; D&C; History of the Church | Boy in the grove; the cost of being a prophet in modern memory | HIGH (drafted material) |
| Joseph Smith Sr. | Lucy Mack Smith memoir; D&C 4 | Father, patriarch, dreamer of the tree of life dream | MEDIUM |
| Lucy Mack Smith | Lucy Mack Smith memoir | The mother who kept the family through every loss | HIGH |
| Martin Harris | D&C 5, 19; lost-pages episode | The believer who paid for the plates with his farm | HIGH |
| Oliver Cowdery | D&C 6, 8, 9, 13, 18; JS-H 1 | Second elder; scribe; restoration of priesthood by John the Baptist | HIGH (drafted) |
| Sidney Rigdon | D&C 35, 76, 90 | The orator-companion in the Vision; trajectory of falling away | MEDIUM |
| Three Witnesses (joint) | D&C 17; Testimony in BoM | "An angel of God came down from heaven" | HIGH |
| Wilford Woodruff | D&C OD-1; journals | The Manifesto; the temple dead | MEDIUM |
| Zina D. H. Young | RS history | The unseen leadership behind much of Relief Society | LOW |

---

## PRIORITY TIERS — SUMMARY

- **HIGH** — central salvation-history figures, frequently in the owner's voice on Facebook, or anchor figures for major doctrines.
- **MEDIUM** — important but less central; build after HIGH tier.
- **LOW** — minor figures who still teach something specific; build last.

### HIGH-priority pages (build first)

**Already exist (review/update only):** Adam, Eve, Abigail, Esther, Elijah.

**Already planned in T4:** Abraham, Alma the Younger, Ammon, Brother of Jared, Captain Moroni, Daniel, Deborah, Elisha, Enoch, Ezekiel, Gideon, Hannah, Isaiah, Jeremiah, Job, Joseph of Egypt, King Benjamin, Lehi, Moses, Nephi (son of Lehi), Noah, Paul, Peter, Ruth, Samuel the Lamanite, Solomon. (Hosea is in T4 but MEDIUM here.)

**New HIGH adds from this list (alphabetized):** Amulek, Cornelius, David, Eliza R. Snow, Emma Smith, Enos, Hyrum Smith, Jacob (son of Lehi), Jacob / Israel, James (the Lord's brother), John the Baptist, John the Beloved, Jonah, Joseph (the carpenter), Joseph F. Smith, Joseph Smith Jr., Joshua, Lamoni's father, Lazarus, Lucy Mack Smith, Malachi, Martin Harris, Mary (mother of Jesus), Mary Magdalene, Mary of Bethany, Melchizedek, Mormon, Moroni (son of Mormon), Nephi (son of Helaman), Nephi the disciple, Nicodemus, Oliver Cowdery, Samaritan woman at the well, Samuel, Stephen, Stripling Warriors and Their Mothers, Thomas, Three Nephites, Three Witnesses (joint), David Whitmer, Woman taken in adultery.

---

## NOTES & CAVEATS

- Where two figures share a name, slug them distinctly:
  - **Joseph-Egypt** (OT) vs. **Joseph-Carpenter** (NT husband of Mary) vs. **Joseph-Smith** (Restoration) vs. **Joseph-Lehi** (son of Lehi) vs. **Joseph-F-Smith** (sixth president).
  - **Alma-Elder** vs. **Alma-Younger** (already reserved as `Alma-Younger`).
  - **Mosiah-I** vs. **Mosiah-II**.
  - **Nephi-Lehi** (son of Lehi, T4-22 reserved as `Nephi`) vs. **Nephi-Helaman** (son of Helaman) vs. **Nephi-Disciple** (3 Nephi).
  - **Lehi-Founder** (T4-21 reserved as `Lehi`) vs. **Lehi-Captain** (Alma 49–62) vs. **Lehi-son-of-Helaman**.
  - **James-Zebedee** vs. **James-Alphaeus** vs. **James-Lords-Brother**.
  - **Mary-Mother-Of-Jesus** vs. **Mary-Magdalene** vs. **Mary-Bethany**.
- **Christ is intentionally excluded** from this list. He has dedicated pages elsewhere on the site and the whole site centers on Him.
- **Satan** is intentionally excluded — `Found-Satan` already exists.
- Where the doctrine has been better taught topically (faith, baptism, covenants), prefer the existing topic page over creating a biography page that duplicates it. For example, a "Faith" article (Found-Faith) exists — the value of a Hebrews 11 page would be the narrative cycle, not a re-teaching of faith doctrine.
- **Existing pages flagged for depth review** in `LDS-DOCTRINES-EXISTING-PAGE-IMPROVEMENTS.md`: Eve, Elijah, MosesLastWords (now merge target), TheOldTestament — review BEFORE building adjacent pages. (`Aaron` is **NOT** in this review list — per user decision, the existing Aaron page is kept as-is.)

---

## TOTALS (after USER DECISIONS APPLIED)

- **Old Testament proposed (incl. already-reserved and existing):** 77
- **New Testament proposed:** 55
- **Book of Mormon proposed (incl. already-reserved):** 55
- **Restoration / D&C proposed:** 18
- **Already live on site:** 10 (Adam, Eve, Cain, TheFall, Aaron, Elijah, MosesLastWords, Abigail, Esther, TheOldTestament)
- **Already reserved as slugs in LDS-DOCTRINES-PLAN.md Tier 4 (planned, unbuilt):** 28
- **Truly NEW page slugs to build:** ~167

---

## SOURCES CONSULTED

- `e:\dev\webApplicationArchitecture\LDS-DOCTRINES-PLAN.md` (lines 1–400, 740–1090, plus grep for character names)
- `e:\dev\webApplicationArchitecture\LDS-DOCTRINES-EXISTING-PAGE-IMPROVEMENTS.md` (full read)
- `e:\dev\webApplicationArchitecture\LDS-DOCTRINES-AGENT-TEMPLATE.md` (full read for voice/style anchors)
- `e:\dev\webApplicationArchitecture\LDS-DOCTRINES-CMS-BASELINE.md` (full read)
- `e:\dev\webApplicationArchitecture\CLAUDE.md` (style/site context, in-conversation)
- `E:\Apologetics\OrganizedReligion\` — directory inventory and listing of `.docx`, `.doc`, `.txt`, `.WRI` files relevant to scriptural characters (no `.md` files exist in OR)
- `E:\Apologetics\OrganizedReligion\Discussions\Discussion 2.doc` and `Discussion 3.doc` — noted, not parsed (legacy `.doc` format)
- `E:\FacebookDownload\facebook-TheronBird-2025-04-11-jkcNtL2U\your_facebook_activity\groups\group_posts_and_comments.html` — name-frequency grep for voice signal

---

*This is a planning document only. No CMS calls have been made. No pages have been created. Review and approve before building.*
