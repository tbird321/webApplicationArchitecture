# LDSDoctrine.com Content Plan
*Generated: 2026-05-19 | Updated: 2026-05-19*

This document proposes new pages for ldsdoctrines.com. All items marked **[EXISTS]** already
have a page on the site and must NOT be duplicated. Everything else is new content to be created.

---

## AGENT INSTRUCTIONS — READ BEFORE BUILDING ANY PAGE

### Do Not Touch Existing Pages or Their Linkages

All work in this plan creates **NEW pages and articles only**. Agents must not:
- Modify any existing article content
- Change any `?page=` navigation link in existing articles
- Alter page-to-article DB associations on existing pages
- Add new cross-links INTO existing pages (they may link OUT to new pages from new content)

If an existing page seems relevant to new content, the new page can link to it.
The existing page stays untouched.

### Atomic DB Operations When Running in Parallel

If multiple agents build pages simultaneously, each agent must treat its CMS operations atomically:

1. **Claim your slug first** — call `create_page` with your assigned slug before calling
   `create_article`. This reserves the slug so no other agent can collide with it.
2. **Never share article IDs** — each agent creates its own article via `create_article`
   and links only that article to its own page via `update_page`.
3. **No two agents should call `create_page` with the same slug** — assign slugs from this
   plan before spawning parallel agents; each slug is for exactly one agent.
4. **Sequence within a single page**: `create_page` → `create_article` → `set_article_content`
   → `update_page` (link article) → `publish_page`. Never skip steps or reverse order.
5. **Menu operations are protected by SemaphoreSlim + S3 ETag optimistic locking** in the
   Lambda. Concurrent writes will retry automatically (up to 5 times with backoff). However,
   agents should still add menu items one at a time after all pages are created rather than
   firing them all in parallel — this keeps the menu clean and avoids hammering retries.

---

## MENU STRUCTURE — LETTER SECTIONS

The site menu uses an **alphabetical A–Z structure** plus a **"Basics" section** for core doctrine.
New pages are added under the appropriate letter section using `add_menu_item`.

After creating and publishing a page, call:
```
add_menu_item(parentId: <letter_id>, itemTitle: "<display text>", pageId: <page_id>, pageName: "<slug>")
```

### Live Letter Section IDs (from get_menu, 2026-05-19)

| Letter | parentId | Letter | parentId |
|--------|----------|--------|----------|
| Basics | 7 | M | 47 |
| A | 1 | N | 48 |
| B | 3 | O | 49 |
| C | 39 | P | 28 |
| D | 40 | Q | 57 |
| E | 41 | R | 58 |
| F | 42 | S | 38 |
| G | 31 | T | 128 |
| H | 26 | U | *(create first — see below)* |
| I | 43 | V | 52 |
| J | 44 | W | 53 |
| K | 45 | X | 54 |
| L | 46 | Y | 55 |
| | | Z | 56 |

### Creating the U Section (needed for Urim-Thummim)

No "U" section exists yet. Create it once before adding any U pages:
```
add_menu_item(parentId: 0, itemTitle: "U")
```
This returns the new item's `id` — use that as `parentId` for all U pages.

### CMS Operation Sequence Per Page (complete pipeline)

```
1. create_page    — name, description, layout="Standard", WEBSITE_ID=2
2. create_article — name, description, articlePath
3. set_article_content — full HTML
4. update_page    — link article (articles: [{id, sequence_no: 5}])
5. publish_page   — make it live
6. add_menu_item  — parentId: <letter_id>, itemTitle, pageId, pageName
```

Step 6 runs after all pages in a batch are published. Menu calls must be sequential.

### Page → Letter Mapping

| Page | Letter | parentId | Suggested Menu Text |
|------|--------|----------|---------------------|
| Found-SpiritWorld | S | 38 | Spirit World |
| Found-ThreeGlories | T | 128 | Three Degrees of Glory |
| Found-Exaltation | E | 41 | Exaltation |
| Found-WordOfWisdom | W | 53 | Word of Wisdom |
| Found-Fasting | F | 42 | Fasting |
| Found-Prayer | P | 28 | Prayer |
| Found-Sabbath | S | 38 | The Sabbath |
| Found-EternalMarriage | E | 41 | Eternal Marriage |
| Found-Family | F | 42 | The Family |
| Found-OuterDarkness | O | 49 | Outer Darkness |
| Found-Consecration | C | 39 | Law of Consecration |
| Found-PatriarchalBlessings | P | 28 | Patriarchal Blessings |
| Found-SpiritualGifts | S | 38 | Spiritual Gifts |
| Found-Apostle | A | 1 | Apostle - What Is One? |
| Found-Obedience | O | 49 | Obedience |
| Found-Temple | T | 128 | The Temple |
| Found-GatheringOfIsrael | G | 31 | Gathering of Israel |
| Zion-Defined | Z | 56 | Zion |
| Found-MissionaryWork | M | 47 | Missionary Work |
| Found-Forgiveness | F | 42 | Forgiveness - Full Doctrine |
| Council-In-Heaven | C | 39 | Council in Heaven |
| Adam-Ondi-Ahman | A | 1 | Adam-ondi-Ahman |
| Intelligences | I | 43 | Intelligences |
| Translated-Beings | T | 128 | Translated Beings |
| City-Of-Enoch | C | 39 | City of Enoch |
| Kolob | K | 45 | Kolob |
| King-Follett | K | 45 | King Follett Discourse |
| Sons-Of-Perdition | S | 38 | Sons of Perdition |
| Salvation-For-Dead | S | 38 | Salvation for the Dead |
| New-Jerusalem | N | 48 | New Jerusalem |
| The-Millennium | M | 47 | The Millennium |
| Celestial-Kingdom | C | 39 | Celestial Kingdom |
| Mysteries-Of-God | M | 47 | Mysteries of God |
| Elias-Elijah-John | E | 41 | Elias, Elijah, and John |
| Dead-Sea-Scrolls | D | 40 | Dead Sea Scrolls and LDS |
| Kabbalah-LDS | K | 45 | Kabbalah and LDS Doctrine |
| Blood-Atonement | B | 3 | Blood Atonement |
| Two-Sticks | T | 128 | Two Sticks - Ezekiel 37 |
| Free-Agency | F | 42 | Free Agency |
| Calling-And-Election | C | 39 | Calling and Election Made Sure |
| Sealing-Power | S | 38 | The Sealing Power |
| Anti-Christ | A | 1 | Anti-Christs of the Book of Mormon |
| Great-And-Abominable | G | 31 | Great and Abominable Church |
| Foreordination | F | 42 | Foreordination |
| The-Veil | V | 52 | The Veil of Forgetfulness |
| Why-Suffering | W | 53 | Why God Allows Suffering |
| Doctrine-Of-Christ | D | 40 | The Doctrine of Christ |
| BofM-Evidences | B | 3 | Book of Mormon Evidences |
| Book-Of-Abraham | B | 3 | Book of Abraham |
| Joseph-Smith-Prophet | J | 44 | Joseph Smith - Prophet or Fraud? |
| First-Vision | F | 42 | The First Vision |
| Mountain-Meadows | M | 47 | Mountain Meadows Massacre |
| BofM-Isaiah | B | 3 | Book of Mormon and Isaiah |
| Evolution-LDS | E | 41 | Evolution - LDS Position |
| Masonry-Temple | M | 47 | Masonry and the Temple |
| BofM-Witnesses | B | 3 | Witnesses of the Book of Mormon |
| BofM-Arabia | B | 3 | Nahom, Bountiful, Arabian Journey |
| BofM-Chiasmus | B | 3 | Chiasmus and the Book of Mormon |
| Bible-Reliability | B | 3 | Can We Trust the Bible? |
| Enoch | E | 41 | Enoch and the City of Zion |
| Noah | N | 48 | Noah |
| Abraham | A | 1 | Abraham |
| Joseph-Egypt | J | 44 | Joseph of Egypt |
| Moses | M | 47 | Moses |
| Deborah | D | 40 | Deborah |
| Ruth | R | 58 | Ruth |
| Hannah | H | 26 | Hannah |
| Elisha | E | 41 | Elisha |
| Isaiah-Prophet | I | 43 | Isaiah - The Messianic Prophet |
| Jeremiah | J | 44 | Jeremiah |
| Ezekiel | E | 41 | Ezekiel |
| Daniel | D | 40 | Daniel |
| Brother-Of-Jared | B | 3 | Brother of Jared |
| Captain-Moroni | C | 39 | Captain Moroni |
| Samuel-Lamanite | S | 38 | Samuel the Lamanite |
| Alma-Younger | A | 1 | Alma the Younger |
| Abinadi | A | 1 | Abinadi |
| Ammon | A | 1 | Ammon |
| King-Benjamin | K | 45 | King Benjamin |
| Lehi | L | 46 | Lehi |
| Nephi | N | 48 | Nephi |
| Solomon | S | 38 | Solomon |
| Job | J | 44 | Job |
| Hosea | H | 26 | Hosea |
| Gideon | G | 31 | Gideon |
| Peter | P | 28 | Peter |
| Paul | P | 28 | Paul |
| Resurrection-Detail | R | 58 | Resurrection - What Kind of Body? |
| Urim-Thummim | U | *(create U first)* | Urim and Thummim |
| Liahona | L | 46 | The Liahona |
| Consecrated-Oil | C | 39 | Consecrated Oil |
| Law-Of-Moses | L | 46 | Law of Moses |
| Tabernacle | T | 128 | The Tabernacle |
| Passover-Type | P | 28 | The Passover as Type of Christ |
| Three-Bodies | T | 128 | Three Glories - Paul's Teaching |
| General-Conference | G | 31 | General Conference |
| Name-Of-Church | N | 48 | Name of the Church |
| Healing-Sick | H | 26 | Healing the Sick |

---

## EXISTING PAGES — DO NOT RECREATE

Cross-referenced against the live site (155 pages, as of 2026-05-19).

### Foundational Doctrine (Found-) — All Exist
- **[EXISTS]** Found-God — Who is God
- **[EXISTS]** Found-Jesus — Who is Jesus Christ
- **[EXISTS]** Found-Holy-Ghost — Who is the Holy Ghost
- **[EXISTS]** Found-PreExistence — Where did I come from
- **[EXISTS]** Found-Why-Here — Why are we here
- **[EXISTS]** Found-Gospel — The Gospel of Jesus Christ
- **[EXISTS]** Found-Grace — What is Grace
- **[EXISTS]** Found-Baptism — What is Baptism
- **[EXISTS]** Found-Plan-Of-Salvation — The Plan of Salvation
- **[EXISTS]** Found-Repentance — Repentance
- **[EXISTS]** Found-Sacrament — The Sacrament
- **[EXISTS]** Found-Scripture — The Scriptures
- **[EXISTS]** Found-Priesthood — Priesthood
- **[EXISTS]** Found-Prophet — The Prophet
- **[EXISTS]** Found-After-Death — What happens when we die
- **[EXISTS]** Found-Commandments — Why do we have commandments
- **[EXISTS]** Found-Organized-Church — Why is an organized Church required
- **[EXISTS]** Found-Spirit — What is a Spirit
- **[EXISTS]** Found-Sin — What is Sin
- **[EXISTS]** Found-Faith — What is Faith
- **[EXISTS]** Found-Atonement — What is the Atonement
- **[EXISTS]** Found-Eternal-Progression — What is Eternal Progression
- **[EXISTS]** Found-Satan — Who is Satan/Lucifer
- **[EXISTS]** Found-Resurrection — What is the Resurrection
- **[EXISTS]** Found-Child-Of-God
- **[EXISTS]** Found-Tithing — Tithing (a stewardship)
- **[EXISTS]** Found-Doctrine — What is Doctrine

### Other Existing Doctrine/Apologetics Pages
- **[EXISTS]** Apostacy — The Path of Apostasy
- **[EXISTS]** Priesthood-Keys — What are the Keys of the Priesthood
- **[EXISTS]** Priesthood-Restrictions
- **[EXISTS]** Trinity — The Trinity
- **[EXISTS]** Revelation-How — How do you receive revelation
- **[EXISTS]** Salvation-Event — Salvation is not an Event
- **[EXISTS]** Salvation-Journey — Salvation the Journey
- **[EXISTS]** FaithAlone — Is Salvation by Faith Alone
- **[EXISTS]** Sola-Fide-Refutation — Why Sola Fide Just Can't be true
- **[EXISTS]** Good-Evil — What is Good and Evil
- **[EXISTS]** Angel — What is an Angel
- **[EXISTS]** Confidence-In-Gods-Presence
- **[EXISTS]** Discipleship
- **[EXISTS]** Prepare-To-Meet-God
- **[EXISTS]** Polygamy
- **[EXISTS]** Forgiveness
- **[EXISTS]** ReasonForRepentance
- **[EXISTS]** ThoseWhoFallAway
- **[EXISTS]** SitAlone
- **[EXISTS]** Conversion
- **[EXISTS]** BofM-Horses — Horses in the Book of Mormon
- **[EXISTS]** Prophets-Test
- **[EXISTS]** Blood-Oath — Blood Oaths
- **[EXISTS]** AbrahamCovenant-Blood
- **[EXISTS]** Covenant-Defined — What is a Covenant
- **[EXISTS]** Love-Of-Christ — Charity the Pure Love of Jesus Christ
- **[EXISTS]** JesusChrist-No-Ordinary — Jesus Christ is No Ordinary Man
- **[EXISTS]** BookOfMormon-Racist — Is the Book of Mormon Racist?
- **[EXISTS]** Evolution
- **[EXISTS]** Aaron
- **[EXISTS]** Baal — Evil Deity Worshiped by Israel
- **[EXISTS]** Called-Of-God — What does it mean to be Called of God
- **[EXISTS]** DNA-Book-of-Mormon
- **[EXISTS]** BookOfMormon-Stylometry — Stylometry and the Book of Mormon
- **[EXISTS]** Gates-Of-Hell
- **[EXISTS]** Baptism-ForDead — Baptism for the Dead
- **[EXISTS]** Bethlehem-Jesus — Bethlehem is the Birthplace of Jesus
- **[EXISTS]** SelfDeception
- **[EXISTS]** Prophets-Warning
- **[EXISTS]** Signs-of-the-Times
- **[EXISTS]** Prophets-Calling — How are Prophets Called of God

### Existing Biblical Character Pages
- **[EXISTS]** Adam
- **[EXISTS]** Cain
- **[EXISTS]** TheFall — The Fall of Adam and Eve
- **[EXISTS]** Eve
- **[EXISTS]** Abigail
- **[EXISTS]** Esther
- **[EXISTS]** Elijah
- **[EXISTS]** MosesLastWords — Moses' Last Words
- **[EXISTS]** TheOldTestament

### Existing Lesson / CFM / AoF Pages
- **[EXISTS]** Lesson-Plans, Lesson-Charity, Lesson-Spiritual-Gifts, Lesson-93
- **[EXISTS]** ArticlesOfFaith, Article1 through Article10
- **[EXISTS]** ComeFollowMe, CFM26-W01 through W51, CFM26-C01 through C26
- **[EXISTS]** 2025 CFM pages (Jan5–Feb2)

---

## NEW CONTENT TO CREATE

Everything below is NEW. None of these slugs exist on the site.

---

## TIER 1 — Complete the Foundational Suite

These are the pages that complete what the "Found-" series started. Every serious doctrine
site needs these and the site currently lacks them.

### T1-01 — The Spirit World
**Slug:** `Found-SpiritWorld`
**Why:** Found-After-Death exists but covers death generally. The Spirit World is its own
doctrine — Paradise vs. Spirit Prison, what life is like there, missionary work for the dead,
the veil. The most-searched topic after someone loses a family member. Richer and more hopeful
than anything any other tradition teaches.
**Key scriptures:** D&C 138, Alma 40, Luke 16:19–31, 1 Peter 3:18–20

### T1-02 — The Three Degrees of Glory
**Slug:** `Found-ThreeGlories`
**Why:** Replaces the binary heaven/hell framework that most seekers bring. Scripturally
grounded in 1 Corinthians 15 and D&C 76. Profoundly hopeful — most of humanity will inherit
some degree of glory. One of the most compelling LDS doctrines for non-members.
**Key scriptures:** D&C 76, 1 Corinthians 15:40–42, John 14:2

### T1-03 — Exaltation — Becoming Like God
**Slug:** `Found-Exaltation`
**Why:** Found-Eternal-Progression exists but is broad. Exaltation is the specific capstone
doctrine — joint-heirs with Christ, inheriting all the Father has. Most Christians don't know
LDS teach this. Scripture supports it extensively. This IS the purpose of the plan of salvation.
**Key scriptures:** Romans 8:17, D&C 132:19–20, 2 Peter 1:4, John 17:3, Psalm 82:6

### T1-04 — The Word of Wisdom
**Slug:** `Found-WordOfWisdom`
**Why:** Unique LDS health law revealed in 1833 — no tobacco, alcohol, harmful substances;
wholesome herbs and grains. Remarkable modern medical validation. Also a covenant and a test
of obedience.
**Key scriptures:** D&C 89, Daniel 1:8–17, 1 Corinthians 6:19–20

### T1-05 — Fasting
**Slug:** `Found-Fasting`
**Why:** A covenant practice with deep OT/NT roots largely forgotten by modern Christianity.
How LDS fast, why, and the covenant dimension (fast offerings for the poor). Isaiah 58 is the
definitive scriptural treatment.
**Key scriptures:** Isaiah 58:6–7, Matthew 6:16–18, Alma 5:46, D&C 59:13–14

### T1-06 — Prayer
**Slug:** `Found-Prayer`
**Why:** Every Christian tradition prays. LDS prayer differs in important ways — addressing
the Father in the name of Christ, expecting personal revelation, the relationship it implies.
How to pray vs. vain repetition. Personal revelation through prayer.
**Key scriptures:** Matthew 6:5–13, 3 Nephi 18:19–21, James 1:5, Enos 1:2–8

### T1-07 — The Sabbath
**Slug:** `Found-Sabbath`
**Why:** One of the Ten Commandments, re-affirmed by Christ, with covenant significance most
Christians have abandoned. Why the Lord's day matters, what it means to keep it holy, and what
blessings are attached.
**Key scriptures:** Exodus 20:8–11, D&C 59:9–16, Isaiah 58:13–14, Mark 2:27

### T1-08 — Eternal Marriage — Sealed for Time and All Eternity
**Slug:** `Found-EternalMarriage`
**Why:** One of the most distinctive and emotionally compelling LDS doctrines. Families aren't
just "until death do you part" — they're sealed forever. The most-asked LDS distinctive by
investigators. AbrahamCovenant-Blood exists but doesn't treat this directly.
**Key scriptures:** D&C 132:15–22, Matthew 19:6, 1 Corinthians 11:11, Malachi 4:5–6

### T1-09 — The Family — The Eternal Organizing Unit
**Slug:** `Found-Family`
**Why:** The family is not just a social institution; it is the eternal structure of heaven.
The Proclamation on the Family. Why family sealing matters eternally. How this differs from
every other Christian tradition.
**Key scriptures:** The Family: A Proclamation to the World, D&C 131:1–4, Genesis 2:24

### T1-10 — Outer Darkness — The Doctrine of Willful Final Rejection
**Slug:** `Found-OuterDarkness`
**Why:** Most people think "outer darkness" means hell for everyone who isn't saved. Wrong.
LDS doctrine teaches only a tiny number — those who receive a perfect witness and then
consciously reject it — will go there. This is actually a doctrine of mercy.
**Key scriptures:** D&C 76:31–38, Matthew 12:31–32, Hebrews 6:4–6, John 17:12

### T1-11 — The Law of Consecration
**Slug:** `Found-Consecration`
**Why:** The economic and social law of Zion — giving everything to God and receiving back
what you need. The United Order. Deeply relevant today and almost completely unknown outside
active LDS circles.
**Key scriptures:** D&C 42:30–35, D&C 82:17–19, Acts 2:44–45, Acts 4:32

### T1-12 — Patriarchal Blessings
**Slug:** `Found-PatriarchalBlessings`
**Why:** Every worthy member can receive a personal prophecy — their lineage in Israel
declared, their specific spiritual gifts named, their life mission outlined. Nothing else like
it in Christianity. Profound and largely unknown.
**Key scriptures:** Genesis 49 (Jacob's blessings), Deuteronomy 33 (Moses' blessings),
D&C 107:39–56

### T1-13 — Spiritual Gifts — The Gifts of the Spirit in Their Fullness
**Slug:** `Found-SpiritualGifts`
**Why:** Lesson-Spiritual-Gifts exists but is a teaching resource. This is the full doctrinal
treatment: tongues, interpretation, healing, prophecy, discernment — all of them active in the
Church today. Most Protestants believe these ceased with the apostles. They did not.
**Key scriptures:** Moroni 10:8–18, 1 Corinthians 12:4–11, D&C 46:11–29

### T1-14 — What Is an Apostle?
**Slug:** `Found-Apostle`
**Why:** Aaron and Prophets-Calling exist but don't treat this directly. An apostle is a
special witness of the resurrection of Christ — not just a title. How LDS apostleship differs
from Protestant usage. Why apostles are necessary (Ephesians 4:11–13).
**Key scriptures:** Hebrews 5:4, Acts 1:21–22, Ephesians 4:11–14, D&C 107:23

### T1-15 — Obedience — Covenant Alignment, Not Blind Compliance
**Slug:** `Found-Obedience`
**Why:** Obedience is the most misunderstood principle in the Church. It is not blind rule-
following. It is covenantal alignment — saying yes to the God who already demonstrated His
love at Calvary. The difference between compliance and conversion.
**Key scriptures:** John 14:15, D&C 130:20–21, 1 Samuel 15:22, Alma 57:21

### T1-16 — What Is a Temple and Why Does It Matter?
**Slug:** `Found-Temple`
**Why:** The single biggest gap in the foundational content. The temple is central to LDS
doctrine — endowments, sealings, proxy work for the dead — and it is completely unknown to
outsiders. This page should explain what temples are doctrinally without revealing sacred
content: the purpose, the covenants in principle, the connection to ancient Israel.
**Key scriptures:** Haggai 2:7, Ezekiel 43:1–7, D&C 110, Malachi 3:1, Isaiah 2:2–3

### T1-17 — The Gathering of Israel — Still Happening Right Now
**Slug:** `Found-GatheringOfIsrael`
**Why:** One of the great covenant promises of the Old Testament — actively fulfilling in our
time. Why LDS missionary work is covenant fulfillment, not just evangelism. The two sticks of
Ezekiel 37. Where the gathering is happening and why it matters.
**Key scriptures:** Ezekiel 37:15–22, Isaiah 11:11–12, 3 Nephi 16:11–12, D&C 110:11

### T1-18 — Zion — What It Is, Where It Will Be, and How to Build It
**Slug:** `Zion-Defined`
**Why:** Zion is both a people and a place. The LDS concept of Zion (the pure in heart) and
the literal New Jerusalem to be built in America is one of the most specific and remarkable
doctrines on the site. Also connects to Enoch's city and the Millennium.
**Key scriptures:** Moses 7:18, D&C 57:1–3, D&C 82:14, Isaiah 52:1–2, Revelation 21:2

### T1-19 — Missionary Work — Why We're Commanded to Share It
**Slug:** `Found-MissionaryWork`
**Why:** The doctrinal basis for missionary work — not just cultural tradition. The worth of
souls, the obligation of those warned to warn others, and the covenant dimension of taking the
gospel to every nation.
**Key scriptures:** D&C 18:10–16, Matthew 28:19–20, Alma 29:1–9, D&C 4

### T1-20 — Forgiveness — The Full Doctrine
**Slug:** `Found-Forgiveness`
**Why:** Forgiveness exists as a page but may be shallow. The full doctrine: why God forgives,
what forgiveness requires of us, the obligation to forgive others as a condition of receiving
forgiveness ourselves, and the healing that follows.
**Key scriptures:** D&C 64:9–10, Matthew 18:21–35, Mosiah 26:29–31, Alma 7:13

---

## TIER 2 — Thought-Provoking Unique Doctrines

These are the pages that make ldsdoctrines.com exceptional — doctrines most Christians have
never heard, explained clearly and compellingly with full scriptural grounding.

### T2-01 — The Council in Heaven — We Were There
**Slug:** `Council-In-Heaven`
**Why:** Before the world was created, God convened a grand council of His spirit children.
A plan was presented. Christ was chosen as Redeemer. Lucifer rebelled. We chose. This is
one of the most stunning doctrines in all of scripture and reshapes everything about why we
are here.
**Key scriptures:** Abraham 3:22–28, Job 38:4–7, Revelation 12:7–9, D&C 29:36–38

### T2-02 — Adam-ondi-Ahman — The Final Gathering Before He Comes
**Slug:** `Adam-Ondi-Ahman`
**Why:** A future gathering — probably the most remarkable prophecy in modern revelation —
where every dispensation head who ever lived will return their keys to Adam, who will return
them to Christ, just before His Second Coming. And we know where it will happen: Missouri.
**Key scriptures:** D&C 116, D&C 107:53–57, Daniel 7:9–14

### T2-03 — Intelligences — What You Were Before You Were Born
**Slug:** `Intelligences`
**Why:** Humans are not created from nothing. We have an eternal core — an intelligence —
co-eternal with God. This is not a minor doctrine; it reshapes the entire question of free will,
identity, and divine potential. Joseph Smith taught this; Abraham 3 confirms it.
**Key scriptures:** Abraham 3:18–22, D&C 93:29–33, D&C 93:23

### T2-04 — Translated Beings — Still Among Us
**Slug:** `Translated-Beings`
**Why:** Translation — being changed so you cannot die — is a real doctrine with named
individuals. The Three Nephites still walk the earth. John the Beloved remains. Moses and Elijah
were translated. Why God uses this, what it is, and what it means for mortality.
**Key scriptures:** 3 Nephi 28:1–9, John 21:20–23, D&C 7, Moses 7:27, 2 Kings 2:11

### T2-05 — The City of Enoch — Zion Already Exists in Heaven
**Slug:** `City-Of-Enoch`
**Why:** Enoch built a society so righteous that God took the entire city to heaven. This is
the most extraordinary story in Moses 6–7: God weeping, Enoch interceding, a whole people
removed from the earth. The city will return at the Second Coming to join New Jerusalem.
**Key scriptures:** Moses 7:13–21, 62–65; Genesis 5:21–24; Hebrews 11:5

### T2-06 — Kolob — The Nature of Eternity and Time
**Slug:** `Kolob`
**Why:** Probably the most mocked LDS doctrine. It is actually a profound cosmological
statement: time and eternity are not the same thing. God's time is not our time. The star
nearest His throne governs celestial time. Abraham 3 is one of the most remarkable texts in
all of Restoration scripture.
**Key scriptures:** Abraham 3:1–10, 16–19; Facsimile 2 explanations; D&C 130:4–7

### T2-07 — The King Follett Discourse — What Kind of Being Is God?
**Slug:** `King-Follett`
**Why:** Joseph Smith's most radical sermon, delivered six weeks before his death. "As man is,
God once was; as God is, man may become." What does this actually mean? Is it scriptural?
Does it contradict omnipotence? This page engages the doctrine directly.
**Key scriptures:** Psalm 82:6, John 10:34, Acts 17:29, Romans 8:16–17, D&C 130:22

### T2-08 — Sons of Perdition — The Doctrine That Proves God Is Just
**Slug:** `Sons-Of-Perdition`
**Why:** Only those who receive a perfect, undeniable witness of God and then consciously,
deliberately reject it become sons of perdition. The doctrine is actually a testimony to God's
mercy — almost no one qualifies. Yet it proves that agency is real and final rejection is
possible.
**Key scriptures:** D&C 76:31–38, Hebrews 6:4–8, 2 Peter 2:20–21, Matthew 12:31–32

### T2-09 — Salvation for the Dead — The Most Fair Doctrine in Christianity
**Slug:** `Salvation-For-Dead`
**Why:** Baptism-ForDead exists as a page about the ordinance. This page treats the full
doctrine: why no one is condemned for not hearing the gospel in mortality, how proxy work
functions covenantally, and why this is the only theological framework that fully reconciles
God's justice with the billions who died without the gospel.
**Key scriptures:** D&C 138, 1 Peter 3:18–20, 1 Peter 4:6, 1 Corinthians 15:29, Malachi 4:5–6

### T2-10 — The New Jerusalem — Two Holy Cities, Two World Capitals
**Slug:** `New-Jerusalem`
**Why:** Most Christians know of Jerusalem's future restoration. Few know that LDS doctrine
teaches a second holy city — New Jerusalem — to be built in America before the Millennium.
Two capitals of a unified world kingdom under Christ. Specific, geographical, remarkable.
**Key scriptures:** D&C 57:1–3, 3 Nephi 20:22, Revelation 21:2, Ether 13:2–6

### T2-11 — The Millennium — What 1000 Years of Peace Actually Looks Like
**Slug:** `The-Millennium`
**Why:** Not rapture, not clouds, not floating. A literal 1000-year period of earth's Sabbath.
What happens to people alive at Christ's coming? What about non-members? Does missionary work
continue? Temple work for the dead accelerates. A concrete, doctrine-grounded description.
**Key scriptures:** D&C 101:22–31, Isaiah 65:20–25, Revelation 20:1–6, D&C 63:51

### T2-12 — The Celestial Kingdom — Life With God, Not Just Near Him
**Slug:** `Celestial-Kingdom`
**Why:** Not clouds and harps. The highest degree of the highest kingdom: eternal families,
eternal creation, the companionship of the Father and Son, joint-heirship with Christ.
What scripture actually says about what it will be like to live in God's presence.
**Key scriptures:** D&C 76:50–70, D&C 131:1–4, D&C 132:19–20, Revelation 21:3–4

### T2-13 — Mysteries of God — Why Not Everything Is Revealed Yet
**Slug:** `Mysteries-Of-God`
**Why:** Why did Joseph Smith receive so much revelation? Why does it come line upon line?
What are the "mysteries" of the kingdom and how do we qualify to receive them? The doctrine
of progressive revelation is itself one of the most interesting doctrines on the site.
**Key scriptures:** Alma 12:9–11, D&C 42:61–65, D&C 76:5–10, Matthew 13:11

### T2-14 — Elias, Elijah, and John — Three Forerunners, Three Offices
**Slug:** `Elias-Elijah-John`
**Why:** One of the most misunderstood doctrines in scripture. "Elias" is an office (prepare
the way), "Elijah" is an office (restore sealing keys), and John the Baptist held both. Joseph
Smith clarified this in ways no other tradition has. The keys Elijah returned in 1836 are
actively operating today.
**Key scriptures:** D&C 110:12–16, D&C 27:6–7, Malachi 4:5–6, Matthew 11:14, John 1:21

### T2-15 — The Dead Sea Scrolls and LDS Doctrine — Convergences
**Slug:** `Dead-Sea-Scrolls`
**Why:** The Qumran community (100 BC – 70 AD) held doctrines strikingly similar to early
LDS teachings: pre-mortal existence, a council of gods, light vs. darkness dualism, covenant
community structure, a Teacher of Righteousness, and more. Not coincidence — evidence of
original Christianity preserved outside the mainstream.
**Key scriptures:** DSS War Scroll, Community Rule, Temple Scroll compared to LDS texts

### T2-16 — Kabbalah and LDS Doctrine — Ancient Threads in Common
**Slug:** `Kabbalah-LDS`
**Why:** Jewish mysticism (Kabbalah) preserves ancient concepts with remarkable LDS parallels:
Adam Kadmon (primordial man), Ein Sof (eternal intelligence), the Tree of Life, heavenly
councils, and divine anthropomorphism. This is one of the most fascinating pages on the site —
evidence that Joseph Smith restored things scholars were finding in ancient sources.

### T2-17 — Blood Atonement — What Was Actually Taught
**Slug:** `Blood-Atonement`
**Why:** Probably the most misrepresented LDS doctrine. What Brigham Young actually taught,
in context, for what purpose, to what audience, and what the Church has clarified since.
Honest treatment rather than defensive deflection.

### T2-18 — The Stick of Judah and the Stick of Ephraim
**Slug:** `Two-Sticks`
**Why:** Ezekiel 37:15–22 prophesies two records — one from Judah (the Bible) and one from
Ephraim (the Book of Mormon) — that would be joined together in God's hand. This is one of
the most specific OT prophecies about the Restoration. Compelling for Protestant readers.
**Key scriptures:** Ezekiel 37:15–22, 2 Nephi 3:12, 2 Nephi 29:6–8

### T2-19 — Free Agency — The Most Sacred Gift
**Slug:** `Free-Agency`
**Why:** Agency is not peripheral to the plan — it IS the plan. Without it, nothing means
anything. The war in heaven was fought over it. The Fall activated it. The Atonement protects
it. Why God will not override it even to prevent suffering.
**Key scriptures:** 2 Nephi 2:27, Moses 4:3, D&C 29:35–36, Helaman 14:30–31

### T2-20 — Calling and Election Made Sure
**Slug:** `Calling-And-Election`
**Why:** One of the deeper covenant doctrines — making your calling and election sure means
receiving the Second Comforter, the personal assurance of exaltation from Christ Himself.
Joseph Smith taught it. Peter refers to it. Few members understand it.
**Key scriptures:** 2 Peter 1:10–11, D&C 131:5–6, D&C 132:49, John 14:18–23

### T2-21 — The Sealing Power — Keys That Bind on Earth and in Heaven
**Slug:** `Sealing-Power`
**Why:** Matthew 16:19 — "whatsoever thou shalt bind on earth shall be bound in heaven."
This is not figurative. The sealing power operates today through priesthood keys held by the
President of the Church. Everything from eternal marriage to proxy baptism flows through this.
**Key scriptures:** Matthew 16:19, Helaman 10:7, D&C 132:7, D&C 110:13–16

### T2-22 — The Anti-Christs of the Book of Mormon — A Warning for Our Day
**Slug:** `Anti-Christ`
**Why:** Sherem, Nehor, Korihor — three anti-Christs in the Book of Mormon, each with a
distinct false doctrine that mirrors specific modern philosophies. Korihor's arguments
(relativism, no God, self-reliance, evolution as doctrine) are the arguments of modern
secularism word for word.
**Key scriptures:** Jacob 7, Alma 1, Alma 30

### T2-23 — The Great and Abominable Church of the Devil
**Slug:** `Great-And-Abominable`
**Why:** 1 Nephi 14 describes a worldwide institution of spiritual corruption opposing God's
covenant. What is it? LDS interpretation, historical context, and why this doctrine is not
anti-anyone — it is a description of a system, not a denomination.
**Key scriptures:** 1 Nephi 13:26–29, 1 Nephi 14:3–4, 10–17; Revelation 17

### T2-24 — Foreordination — Called Before You Were Born
**Slug:** `Foreordination`
**Why:** Before mortality, many were called to specific missions. Jeremiah was foreordained
a prophet before birth. Abraham was among the noble and great. Paul was set apart before he
was formed. This is not predestination — it is calling without compulsion.
**Key scriptures:** Jeremiah 1:5, Abraham 3:22–23, Alma 13:3–5, Romans 8:29–30

### T2-25 — The Veil of Forgetfulness — Why We Don't Remember Heaven
**Slug:** `The-Veil`
**Why:** We lived with God before birth. Why don't we remember it? The veil is not a
punishment — it is the condition that makes faith possible, that makes this life a genuine
test, that allows us to choose God without the unfair advantage of having seen His face.
**Key scriptures:** D&C 93:38, Abraham 3:25, D&C 122:7, Job 38:4

### T2-26 — Theodicy — Why God Allows Suffering (The LDS Answer)
**Slug:** `Why-Suffering`
**Why:** The question every thoughtful person asks. The LDS answer is more coherent than
anything traditional Christianity offers: suffering is not punishment, not accident, not
evidence of God's absence. It is the necessary environment for moral growth, agency, and
becoming. Job is the central text.
**Key scriptures:** 2 Nephi 2:11, D&C 122:7–8, Job 1–2, Romans 8:28, Ether 12:27

### T2-27 — The Doctrine of Christ — Simple and Complete
**Slug:** `Doctrine-Of-Christ`
**Why:** 2 Nephi 31 and 3 Nephi 11 give the clearest statement of what "the doctrine of
Christ" actually is. Not theology. Not tradition. Five specific things. Everything else is
the "works of men." A page that cuts through centuries of theological accretion to state
what Christ Himself defined as His doctrine.
**Key scriptures:** 2 Nephi 31:17–21, 3 Nephi 11:31–40, D&C 10:67–68

---

## TIER 3 — Apologetics

Honest, rigorous engagement with the real questions. Not defensive — direct.

### T3-01 — The Book of Mormon — The Case for Authenticity
**Slug:** `BofM-Evidences`
**Why:** The site has individual pages on Horses, Stylometry, DNA, and Racist accusations.
This is the master overview — chiasmus, Nahom, Bountiful, the 1830 complexity argument,
internal consistency, witnesses — all in one place.
**Key sources:** FARMS/BYU Studies research, Sorenson's geography work, Welch's chiasmus work

### T3-02 — The Book of Abraham — What LDS Actually Believe
**Slug:** `Book-Of-Abraham`
**Why:** The most-attacked LDS scriptural text. What Joseph claimed, what the papyri are,
what they aren't, what the facsimile explanations mean, and what the scholarship — both
critical and LDS — actually shows. Honest treatment without defensiveness.
**Key sources:** Gospel Topics Essay on the Book of Abraham; LDS scholarly responses

### T3-03 — Joseph Smith — Prophet or Fraud? The Real Question
**Slug:** `Joseph-Smith-Prophet`
**Why:** The site treats prophets generally but has no page dedicated to the question most
investigators actually ask. The First Vision, the Book of Mormon translation, the Restoration,
and the character of the man himself. Direct engagement with the skeptic's case.

### T3-04 — The First Vision — What Actually Happened and Why It Matters
**Slug:** `First-Vision`
**Why:** The foundational claim of the Restoration. Four accounts, the apparent discrepancies,
what they mean (and don't mean), and why the appearance of two distinct beings completely
destroys the Nicene Trinity. One of the most important apologetics pages on the site.
**Key scriptures:** Joseph Smith—History 1:14–17

### T3-05 — Mountain Meadows Massacre — An Honest History
**Slug:** `Mountain-Meadows`
**Why:** Honest, non-defensive treatment. What happened, who was responsible, the historical
context, what Church leaders at the time said, and what the Church has acknowledged since.
Avoiding it makes the site less credible. Addressing it honestly builds trust.

### T3-06 — The Book of Mormon and Isaiah — The Variants Are Evidence
**Slug:** `BofM-Isaiah`
**Why:** Critics claim the Isaiah variants in the Book of Mormon prove it was plagiarized from
the KJV. Scholars like Tvedtnes and others have shown the variants are actually evidence
FOR the Book of Mormon, reflecting a Hebrew textual tradition independent of the Masoretic.
**Key sources:** FARMS papers on Isaiah variants

### T3-07 — Evolution — The LDS Position
**Slug:** `Evolution-LDS`
**Why:** Evolution page exists but may be shallow. The LDS Church has no official position
on biological evolution — only on the spiritual truths of the Fall and divine creation.
What the Church actually teaches vs. what members sometimes assume.

### T3-08 — Was Joseph Smith a Mason? The Temple and Freemasonry
**Slug:** `Masonry-Temple`
**Why:** Critics claim LDS temple ceremonies were copied from Masonic ritual. The honest
treatment: what similarities exist, what they mean, the theory of ancient common origins,
and why this is actually evidence of authentic ancient ceremony rather than plagiarism.

### T3-09 — The Witnesses of the Book of Mormon
**Slug:** `BofM-Witnesses`
**Why:** Eleven witnesses signed formal, public declarations that they saw the gold plates.
None recanted. Several left the Church and still maintained their testimony. This is
historically remarkable and deserves its own dedicated treatment.
**Key scriptures:** Official testimonies of the Three and Eight Witnesses

### T3-10 — Nahom, Bountiful, and the Arabian Journey
**Slug:** `BofM-Arabia`
**Why:** BofM-Horses exists for one evidence. The Arabian journey evidence (NHM inscriptions,
Bountiful at Khor Kharfot) is arguably the strongest archaeological confirmation of the Book
of Mormon. It deserves a full page.
**Key sources:** Warren Aston's Bountiful research; Hilton on Nahom

### T3-11 — Chiasmus and the Book of Mormon
**Slug:** `BofM-Chiasmus`
**Why:** The BofM-Stylometry page exists. Chiasmus is different — a specific Hebrew literary
form unknown to 1830 America that appears throughout the Book of Mormon in structurally
complex forms impossible to fake. John Welch's discovery in 1967 is one of the strongest
internal evidences.
**Key sources:** John Welch's FARMS paper on chiasmus

### T3-12 — Can We Trust the Bible? — The LDS View of Scripture Reliability
**Slug:** `Bible-Reliability`
**Why:** LDS believe the Bible is the word of God "as far as it is translated correctly."
What does that mean? The Eighth Article of Faith explained. How the restoration supplements
rather than replaces the Bible. A page every Protestant visitor needs.
**Key scriptures:** Article of Faith 8, 1 Nephi 13:26–29, D&C 91

---

## TIER 4 — Biblical Character Narratives

Deep narrative pages on biblical figures that illuminate LDS doctrine while being compelling
reading. Note: Adam, Cain, Eve, TheFall, Abigail, Esther, Elijah, Aaron, MosesLastWords
already exist — not listed here.

### T4-01 — Enoch and the City of Zion
**Slug:** `Enoch`
**Why:** The most extraordinary story most members barely know. Enoch walked with God,
built a society so righteous that God wept over the world that rejected it, and then took
the entire city to heaven. Moses 6–7 is magnificent. This page also introduces the full
doctrine of Zion.
**Key scriptures:** Moses 6:26–36, Moses 7:1–21, 62–65; Genesis 5:21–24

### T4-02 — Noah — 120 Years of Faithful Warning
**Slug:** `Noah`
**Why:** Not just a boat story. A man who preached repentance for over a century to a people
who mocked him, never seeing a drop of rain, building a ship in the desert because God said
to. The doctrine of obedience lived out in the hardest possible circumstances.
**Key scriptures:** Moses 8:13–30, Genesis 6–9, Hebrews 11:7, 1 Peter 3:20

### T4-03 — Abraham — The Man Who Went Without Knowing Where
**Slug:** `Abraham`
**Why:** AbrahamCovenant-Blood exists as an apologetics page. This is the full character
treatment. The call to leave everything, Lot's choice, the covenant, the binding of Isaac
as the greatest type of Christ in Genesis. "By faith Abraham obeyed."
**Key scriptures:** Genesis 12–22, Abraham 1–3, Hebrews 11:8–19, D&C 132:29–32

### T4-04 — Joseph of Egypt — The Most Complete Type of Christ in the Old Testament
**Slug:** `Joseph-Egypt`
**Why:** Sold by his brothers for silver, falsely accused, imprisoned, then raised to save
the very people who rejected him. The parallels to Christ are staggering in number and
precision — more complete than any other OT type. One of the most powerful pages on the site.
**Key scriptures:** Genesis 37–50, 2 Nephi 3:4–15

### T4-05 — Moses — Face to Face With God
**Slug:** `Moses`
**Why:** MosesLastWords exists. The full Moses page doesn't. Pharaoh's palace, the burning
bush, the plagues, the Red Sea, Sinai, forty years in the wilderness, never entering the
promised land. The pattern of deliverance. The LDS understanding of Moses as a type of Christ.
**Key scriptures:** Moses 1:1–42, Exodus 3–14, Hebrews 11:24–29

### T4-06 — Deborah — Judge, Prophet, and Commander
**Slug:** `Deborah`
**Why:** One of the most remarkable women in scripture — simultaneously a judge, a prophetess,
and a military commander who led Israel to victory against a vastly superior army. Her story
demolishes the idea that women in ancient Israel were merely background figures.
**Key scriptures:** Judges 4–5

### T4-07 — Ruth — Covenant Loyalty Across Every Boundary
**Slug:** `Ruth`
**Why:** A Moabite woman who chose covenant loyalty to her mother-in-law's God over cultural
comfort. The concept of hesed (covenant love) embodied. Her place in the genealogy of Christ.
Boaz as kinsman-redeemer type. One of the most beautiful covenant stories in scripture.
**Key scriptures:** Ruth 1–4, Matthew 1:5

### T4-08 — Hannah — Prayer, Covenant, and the Impossible Gift
**Slug:** `Hannah`
**Why:** Barren, mocked, desperate — she poured out her soul before God so intensely the
priest thought she was drunk. God gave her Samuel, who became the greatest prophet since
Moses. Her prayer in 1 Samuel 2 is one of the most prophetic hymns in the OT. Mary's
Magnificat echoes it almost word for word.
**Key scriptures:** 1 Samuel 1–2, Luke 1:46–55

### T4-09 — Elisha — The Double Portion
**Slug:** `Elisha`
**Why:** He asked for a double portion of Elijah's spirit and received it — performing twice
as many recorded miracles as his master. The invisible army of angels, healing Naaman the
leper, raising the dead, multiplying food. Every miracle points to doctrine.
**Key scriptures:** 2 Kings 2–7, 13

### T4-10 — Isaiah — He Saw Christ's Mission 700 Years Early
**Slug:** `Isaiah-Prophet`
**Why:** Why does the Book of Mormon quote Isaiah more than any other OT book? Because Nephi
said "Isaiah spake concerning all the house of Israel." The servant songs, the Messiah
prophecies, the Millennium — Isaiah is the most prophetically dense book in the OT.
**Key scriptures:** Isaiah 6 (his call), Isaiah 53 (the Suffering Servant), Isaiah 61 (Christ
quoted at Nazareth), 2 Nephi 25:4–7

### T4-11 — Jeremiah — Called Before Birth, Right About Everything
**Slug:** `Jeremiah`
**Why:** Foreordained before birth, forbidden to marry, thrown into a pit, watched Jerusalem
fall exactly as he predicted, bought a field as the city burned because God said the land
would be restored. Rejected by everyone, vindicated by history. One of the most poignant
prophetic lives in scripture.
**Key scriptures:** Jeremiah 1:5, Jeremiah 32:1–15, Lamentations 3:22–23

### T4-12 — Ezekiel — The Prophet of Visions
**Slug:** `Ezekiel`
**Why:** The valley of dry bones, the vision of the heavenly chariot (merkabah), the detailed
new temple, the two sticks — Ezekiel is one of the most visionary and doctrinally rich
prophets. His prophecies of restoration are LDS doctrine made plain.
**Key scriptures:** Ezekiel 1, 36–37, 40–48

### T4-13 — Daniel — Covenant Integrity in the Court of the Enemy
**Slug:** `Daniel`
**Why:** CFM treatment exists. Full character page needed. A man who refused to compromise
his covenant identity in Babylon's most powerful court. His visions of the last days. The
four kingdoms and the stone cut without hands. An extraordinary life.
**Key scriptures:** Daniel 1–7, Daniel 12

### T4-14 — The Brother of Jared — Faith So Perfect He Pierced the Veil
**Slug:** `Brother-Of-Jared`
**Why:** He asked God to touch 16 small stones with His finger to give them light — and when
Christ reached through the veil to do it, the Brother of Jared saw His finger and was struck
with terror. His faith had made the veil transparent. The most direct encounter with the
pre-mortal Christ in all scripture.
**Key scriptures:** Ether 2–3

### T4-15 — Captain Moroni — The Righteous Warrior
**Slug:** `Captain-Moroni`
**Why:** Mormon's declaration — "If all men had been and were and ever would be like unto
Moroni, behold, the very powers of hell would have been shaken forever" — is one of the
most striking character endorsements in scripture. A military leader whose first act was
to kneel in prayer and whose second was to make a flag about liberty.
**Key scriptures:** Alma 43–62

### T4-16 — Samuel the Lamanite — Preached From the Wall
**Slug:** `Samuel-Lamanite`
**Why:** An outsider — a Lamanite in a Nephite city — stood on the city wall and declared
five years of national repentance to people who were literally shooting arrows at him. He
prophesied the exact signs of Christ's birth and death. And they couldn't kill him.
**Key scriptures:** Helaman 13–15

### T4-17 — Alma the Younger — Three Days in Darkness
**Slug:** `Alma-Younger`
**Why:** The most dramatic conversion in the Book of Mormon — an angel, three days of
unconsciousness as Alma experienced a kind of death and rebirth, and then fifty years of
the most powerful missionary work in scriptural history. The experiential doctrine of
being "born again" on its most vivid terms.
**Key scriptures:** Mosiah 27, Alma 36

### T4-18 — Abinadi — Died for the Truth, Saved a Nation
**Slug:** `Abinadi`
**Why:** Disguised himself to return to a city that had tried to kill him, delivered his
message, was burned alive — and his one convert (Alma) started a movement that converted
tens of thousands. The measure of a prophetic life is not the crowd it gathers but the
truth it plants.
**Key scriptures:** Mosiah 11–17

### T4-19 — Ammon — The Greatest Missionary in the Book of Mormon
**Slug:** `Ammon`
**Why:** Refused to be king, became a servant, defended the king's flocks by cutting off
the arms of attackers, and then used the king's astonishment to teach the gospel. The
most creative, courageous missionary in scripture. His joy at the end of the mission is
one of the most moving passages in the Book of Mormon.
**Key scriptures:** Alma 17–19, Alma 26

### T4-20 — King Benjamin — The Sermon That Changed a Nation
**Slug:** `King-Benjamin`
**Why:** An entire people fell to the ground and experienced a mighty change of heart when
King Benjamin delivered this address. His teachings on service, the natural man, and the
name of Christ are among the doctrinal peaks of the Book of Mormon.
**Key scriptures:** Mosiah 2–5

### T4-21 — Lehi — The Father Who Led His Family Into the Unknown
**Slug:** `Lehi`
**Why:** Left everything — wealth, home, city, friends — because God told him Jerusalem
would fall. Most people laughed at him. His family rebelled. He pressed on. His dying
testimony to his children in 2 Nephi 1–4 is one of the most powerful patriarchal blessings
in scripture.
**Key scriptures:** 1 Nephi 1–2, 2 Nephi 1–4

### T4-22 — Nephi — I Will Go and Do
**Slug:** `Nephi`
**Why:** The foundational character of the Book of Mormon — young, faithful, willing. "I
will go and do the things which the Lord hath commanded, for I know that the Lord giveth
no commandments unto the children of men, save he shall prepare a way." His life is a
template for covenant discipleship.
**Key scriptures:** 1 Nephi 1–18, 2 Nephi 31–33

### T4-23 — Solomon — Wisdom, Apostasy, and the Cost of Compromise
**Slug:** `Solomon`
**Why:** The wisest man who ever lived, given everything by God — and he threw it away
through slow, incremental spiritual compromise. His story is the warning every person in
a position of success and blessing needs. The Temple he built. The idolatry that followed.
**Key scriptures:** 1 Kings 3–11

### T4-24 — Job — Suffering, Sovereignty, and the Voice From the Whirlwind
**Slug:** `Job`
**Why:** The oldest book in the OT, and the most direct treatment of theodicy in scripture.
Job's three friends represent the wrong theology of suffering. God's answer from the
whirlwind is not an explanation — it is a revelation. Job sees God. That is the answer.
**Key scriptures:** Job 1–2, 38–42, Job 19:25–27

### T4-25 — Hosea — God's Marriage Covenant Made Personal
**Slug:** `Hosea`
**Why:** A prophet commanded to marry a harlot as a living metaphor of God's covenant
relationship with faithless Israel. One of the most personally costly prophetic assignments
in scripture. The doctrine of hesed (covenant love that endures despite betrayal).
**Key scriptures:** Hosea 1–3, 11, 14

### T4-26 — Gideon — The Least of His Father's House
**Slug:** `Gideon`
**Why:** God reduced his army from 32,000 to 300 — deliberately — so no one could claim
the victory was Israel's. Gideon's story is the doctrine of divine sufficiency: God uses
weakness, not strength, to accomplish the impossible. Connects directly to Ether 12:27.
**Key scriptures:** Judges 6–8

### T4-27 — Peter — From Fisherman to Rock
**Slug:** `Peter`
**Why:** The transformation of Peter — from impulsive denier to fearless apostolic witness
whose shadow healed the sick — is the most dramatic character arc in the New Testament.
The doctrine of conversion lived out. Keys given, lost, restored.
**Key scriptures:** Matthew 16:13–19, Luke 22:32, Acts 2–5, John 21

### T4-28 — Paul — The Most Unlikely Apostle
**Slug:** `Paul`
**Why:** The man who held coats while Stephen was stoned became the greatest missionary in
Christian history. His conversion on the Damascus road, his extraordinary theological
insight, his imprisonment, and his unbroken testimony. Conversion as transformation, not
just decision.
**Key scriptures:** Acts 9, Galatians 1:11–24, Philippians 4:11–13, 2 Timothy 4:6–8

---

## TIER 5 — Doctrinal Deep Dives

### T5-01 — The Resurrection — What Kind of Body?
**Slug:** `Resurrection-Detail`
**Why:** Found-Resurrection exists but likely surface. Paul's treatment in 1 Corinthians 15
is the most complete scriptural treatment of what a resurrected body is: celestial, terrestrial,
telestial — these are Paul's words. The LDS understanding of a perfected, tangible, immortal
body distinct from vague "spiritual" concepts.
**Key scriptures:** 1 Corinthians 15:35–44, Alma 11:43–44, D&C 88:27–32, D&C 130:22

### T5-02 — The Urim and Thummim — Oracle of God
**Slug:** `Urim-Thummim`
**Why:** From Exodus through Revelation, God provided instruments of revelation to His
prophets. What the Urim and Thummim are, how they functioned in ancient Israel, their
connection to Joseph Smith's interpreters and seer stone, and what they tell us about
how revelation works.
**Key scriptures:** Exodus 28:30, Leviticus 8:8, D&C 17:1, Revelation 2:17

### T5-03 — The Liahona — A Compass Driven by Faith
**Slug:** `Liahona`
**Why:** Not just a navigation device. The Liahona functioned only by faith and diligence —
when the Lehites murmured, it stopped working. Alma 37 turns it into an explicit type of
the Holy Ghost, the word of Christ, and the covenant path. A beautiful doctrinal object lesson.
**Key scriptures:** 1 Nephi 16:10, 28–29; Alma 37:38–47

### T5-04 — Consecrated Oil — The Doctrine Behind the Practice
**Slug:** `Consecrated-Oil`
**Why:** Anointing with oil has deep Levitical roots, NT confirmation in James 5, and active
LDS practice today. Why oil? What does consecration mean? How priesthood blessings work
and what they are. Connects to healing, the laying on of hands, and ordinances.
**Key scriptures:** James 5:14–15, Leviticus 8:12, Mark 6:13, D&C 42:43–44

### T5-05 — The Law of Moses — Why Israel Needed It and What It Pointed To
**Slug:** `Law-Of-Moses`
**Why:** Christ did not abolish the law — He fulfilled it. But what was the law for? LDS
doctrine teaches it was a "schoolmaster" pointing to Christ through every ordinance, feast,
and sacrifice. The tabernacle, the Day of Atonement, the Passover — every detail pointed
forward to Christ.
**Key scriptures:** Galatians 3:24, 2 Nephi 11:4, Mosiah 13:29–30, 3 Nephi 15:2–5

### T5-06 — The Tabernacle — Every Detail a Type of Christ
**Slug:** `Tabernacle`
**Why:** The ark, the veil, the altar of incense, the menorah, the laver — every furnishing
of the Tabernacle is a teaching about Christ and the plan of salvation. This page treats
the Tabernacle as sacred architecture — a visual scripture.
**Key scriptures:** Exodus 25–30, Hebrews 8–9, Mosiah 13:31

### T5-07 — The Passover as the Most Complete Type of Christ
**Slug:** `Passover-Type`
**Why:** CFM W13 has a treatment. A full doctrine page should stand alone. The blood on
the doorposts, the lamb without blemish, the hyssop, the unleavened bread, "not a bone
shall be broken" — every detail was fulfilled in the crucifixion. The most complete
pre-figuration of the Atonement in the Old Testament.
**Key scriptures:** Exodus 12, John 19:36, 1 Corinthians 5:7, John 1:29

### T5-08 — Celestial, Terrestrial, and Telestial Bodies — Paul's Three Glories
**Slug:** `Three-Bodies`
**Why:** Paul's 1 Corinthians 15 uses these exact three words — and most Christians read
right past them. LDS doctrine is not innovation; it is a careful reading of Paul. What
each degree of body is, what qualifies for each, and how this answers every question about
divine justice.
**Key scriptures:** 1 Corinthians 15:40–42, D&C 76:70, 78, 98

### T5-09 — General Conference — Ongoing Revelation to a Living Church
**Slug:** `General-Conference`
**Why:** Twice a year, God speaks through living prophets and apostles to the entire Church.
The doctrinal basis for why this matters, what authority these words carry, and how to
receive and apply them.
**Key scriptures:** D&C 1:38, Amos 3:7, D&C 21:4–5

### T5-10 — The Name of the Church — Why "Of Jesus Christ"
**Slug:** `Name-Of-Church`
**Why:** 3 Nephi 27 — Christ specifically rebukes naming the Church after men and insists it
bear His name. The history of why the Church's name matters, what it declares, and why every
other church name is theologically problematic.
**Key scriptures:** 3 Nephi 27:3–8, D&C 115:3–4

### T5-11 — Healing the Sick — Priesthood Blessings and How They Work
**Slug:** `Healing-Sick`
**Why:** Priesthood blessings for healing are a living practice in the Church with deep
scriptural roots. What they are, how they are performed, the doctrinal basis, and what
faith and healing actually mean in LDS doctrine.
**Key scriptures:** James 5:14–15, Mark 6:13, D&C 42:43–48, Acts 3:1–10

---

## PROPOSED MENU STRUCTURE

### Primary Navigation (8 categories)

```
Foundations
Eternal Plan
Apologetics
The Restoration
Biblical Characters
Covenant Doctrine
End Times
Interesting Doctrines
```
*(Come Follow Me and Articles of Faith already exist as separate sections)*

---

### Foundations
*What every new investigator and member needs to know — doctrines of the basic gospel*

**Already exists:**
Who Is God / Who Is Jesus Christ / The Holy Ghost / Plan of Salvation / Pre-Existence /
Why Are We Here / The Gospel / Grace / Baptism / Repentance / The Sacrament / Scriptures /
The Priesthood / Prophets / The Atonement / Faith / Sin / Eternal Progression / Satan /
Resurrection / The Commandments / Forgiveness / Tithing / Organized Church

**New pages to add to this menu:**
- The Spirit World (T1-01)
- Eternal Marriage (T1-08)
- The Family (T1-09)
- Word of Wisdom (T1-04)
- Fasting (T1-05)
- Prayer (T1-06)
- The Sabbath (T1-07)
- Spiritual Gifts (T1-13)
- Obedience (T1-15)
- The Temple (T1-16)
- Missionary Work (T1-19)
- Apostle — What Is One? (T1-14)
- Patriarchal Blessings (T1-12)
- Free Agency (T2-19)
- The Doctrine of Christ (T2-27)

---

### Eternal Plan
*The destination — where the plan leads*

**New pages:**
- The Three Degrees of Glory (T1-02)
- Exaltation — Becoming Like God (T1-03)
- Outer Darkness (T1-10)
- The Celestial Kingdom (T2-12)
- Salvation for the Dead (T2-09)
- The Gathering of Israel (T1-17)
- Zion (T1-18)
- The New Jerusalem (T2-10)
- The Millennium (T2-11)
- Calling and Election Made Sure (T2-20)
- The Sealing Power (T2-21)
- Law of Consecration (T1-11)

---

### Apologetics
*Honest answers to the real questions people bring*

**Already exists:**
Apostasy / Trinity / FaithAlone / Sola Fide / BofM-Racist / DNA-BofM / BofM-Horses /
BofM-Stylometry / Baptism-ForDead / Evolution / Priesthood-Restrictions / Polygamy /
AbrahamCovenant-Blood / Gates-Of-Hell / Bethlehem / Called-Of-God

**New pages:**
- BofM Evidences Overview (T3-01)
- Book of Abraham (T3-02)
- Joseph Smith — Prophet or Fraud? (T3-03)
- The First Vision (T3-04)
- Mountain Meadows Massacre (T3-05)
- BofM and Isaiah (T3-06)
- Nahom, Bountiful, and the Arabian Journey (T3-10)
- Chiasmus and the Book of Mormon (T3-11)
- The Witnesses of the Book of Mormon (T3-09)
- Masonry and the Temple (T3-08)
- Can We Trust the Bible? (T3-12)
- Great and Abominable Church (T2-23)
- Anti-Christs of the Book of Mormon (T2-22)

---

### The Restoration
*How the ancient church was re-established*

**New pages:**
- The First Vision (T3-04)
- Joseph Smith — Prophet (T3-03)
- Restoration of the Aaronic Priesthood *(new page on the event)*
- Restoration of the Melchizedek Priesthood *(new page on the event)*
- The Coming Forth of the Book of Mormon
- The Witnesses (T3-09)
- The Book of Abraham (T3-02)
- General Conference — Ongoing Revelation (T5-09)

---

### Biblical Characters

**Old Testament (already exists):**
Adam / Cain / Eve / Abigail / Esther / Elijah / Aaron / MosesLastWords

**Old Testament (new):**
Enoch (T4-01) / Noah (T4-02) / Abraham (T4-03) / Joseph of Egypt (T4-04) /
Moses (T4-05) / Deborah (T4-06) / Ruth (T4-07) / Hannah (T4-08) /
Elisha (T4-09) / Isaiah (T4-10) / Jeremiah (T4-11) / Ezekiel (T4-12) /
Daniel (T4-13) / Solomon (T4-23) / Job (T4-24) / Hosea (T4-25) / Gideon (T4-26)

**New Testament (new):**
Peter (T4-27) / Paul (T4-28)

**Book of Mormon (new):**
Brother of Jared (T4-14) / Captain Moroni (T4-15) / Samuel the Lamanite (T4-16) /
Alma the Younger (T4-17) / Abinadi (T4-18) / Ammon (T4-19) /
King Benjamin (T4-20) / Lehi (T4-21) / Nephi (T4-22)

---

### Covenant Doctrine
*The binding agreements that structure salvation*

**Already exists:**
Covenant-Defined / AbrahamCovenant-Blood / Blood-Oath / Baptism-ForDead / Priesthood-Keys

**New pages:**
- Eternal Marriage (T1-08)
- Sealing Power (T2-21)
- Law of Consecration (T1-11)
- The Passover as Type of Christ (T5-07)
- The Tabernacle (T5-06)
- Law of Moses (T5-05)

---

### End Times
*What scripture says about how it ends*

**Already exists:**
Signs-of-the-Times / Apostasy / Baptism-ForDead

**New pages:**
- Adam-ondi-Ahman (T2-02)
- The Gathering of Israel (T1-17)
- New Jerusalem (T2-10)
- The Millennium (T2-11)
- The Three Degrees of Glory (T1-02)
- Final Judgment *(brief doctrinal page)*
- Translated Beings (T2-04)

---

### Interesting Doctrines
*The deep, the unusual, the profound — doctrines most Christians have never heard*

**New pages (all of them):**
- Council in Heaven (T2-01)
- Adam-ondi-Ahman (T2-02)
- Intelligences (T2-03)
- Translated Beings (T2-04)
- City of Enoch (T2-05)
- Kolob (T2-06)
- King Follett Discourse (T2-07)
- Sons of Perdition (T2-08)
- Mysteries of God (T2-13)
- Elias, Elijah, and John (T2-14)
- Dead Sea Scrolls and LDS Doctrine (T2-15)
- Kabbalah and LDS Doctrine (T2-16)
- Blood Atonement (T2-17)
- The Two Sticks — Ezekiel 37 (T2-18)
- Calling and Election Made Sure (T2-20)
- The Sealing Power (T2-21)
- Foreordination (T2-24)
- The Veil of Forgetfulness (T2-25)
- Why God Allows Suffering (T2-26)
- Anti-Christs of the Book of Mormon (T2-22)
- Great and Abominable Church (T2-23)
- Urim and Thummim (T5-02)
- The Liahona (T5-03)
- Resurrection — What Kind of Body? (T5-01)
- Masonry and the Temple (T3-08)

---

## CONTENT CREATION PRIORITIES

### Do These First — Highest Impact

1. **Found-SpiritWorld** (T1-01) — #1 searched topic after a death
2. **Found-ThreeGlories** (T1-02) — anchor doctrine; replaces heaven/hell binary
3. **Found-Exaltation** (T1-03) — the whole point of the plan
4. **Found-Temple** (T1-16) — largest single gap in the foundational content
5. **Found-EternalMarriage** (T1-08) — most-asked LDS distinctive
6. **Council-In-Heaven** (T2-01) — makes the site distinctive immediately
7. **Joseph-Egypt** (T4-04) — best OT character page; most complete Christ type
8. **Salvation-For-Dead** (T2-09) — most compelling doctrine for non-members
9. **First-Vision** (T3-04) — foundational apologetics gap
10. **Enoch** (T4-01) — extraordinary story most members barely know

### Do These Second — Deepen the Unique Doctrine Layer

11. **Adam-Ondi-Ahman** (T2-02)
12. **Intelligences** (T2-03)
13. **City-Of-Enoch** (T2-05)
14. **Kolob** (T2-06)
15. **King-Follett** (T2-07)
16. **Foreordination** (T2-24)
17. **The-Veil** (T2-25)
18. **Sons-Of-Perdition** (T2-08)
19. **Translated-Beings** (T2-04)
20. **Zion-Defined** (T1-18)

### Do These Third — Complete Biblical Characters

21. Abraham (T4-03)
22. Moses (T4-05)
23. Joseph of Egypt (T4-04)
24. Nephi (T4-22)
25. Elisha (T4-09)
26. Brother of Jared (T4-14)
27. Alma the Younger (T4-17)
28. Isaiah (T4-10)
29. Jeremiah (T4-11)
30. King Benjamin (T4-20)
31. Lehi (T4-21)
32. Captain Moroni (T4-15)
33. Samuel the Lamanite (T4-16)
34. Abinadi (T4-18)
35. Ammon (T4-19)
36. Noah (T4-02)
37. Deborah (T4-06)
38. Ruth (T4-07)
39. Hannah (T4-08)
40. Daniel (T4-13)
41. Ezekiel (T4-12)
42. Job (T4-24)
43. Solomon (T4-23)
44. Hosea (T4-25)
45. Gideon (T4-26)
46. Peter (T4-27)
47. Paul (T4-28)

### Do These Fourth — Complete the Apologetics Layer

48. BofM-Evidences (T3-01)
49. Book-Of-Abraham (T3-02)
50. Joseph-Smith-Prophet (T3-03)
51. Mountain-Meadows (T3-05)
52. BofM-Isaiah (T3-06)
53. BofM-Witnesses (T3-09)
54. Nahom-Bountiful (T3-10)
55. BofM-Chiasmus (T3-11)

---

## NOTES ON TONE AND APPROACH

All new content follows the voice established across the existing site:

- Speak to someone who may be a skeptic, Protestant, or honest seeker — not just members
- Use scripture as primary evidence; the text makes the argument, not tradition
- State the LDS position confidently; don't hedge or apologize
- Where a doctrine is unusual or contested, address that directly and honestly
- Where scripture in other traditions supports LDS doctrine, point to it
- Short punchy sentences mixed with explanatory paragraphs
- Gold blockquotes for primary scripture; blue blockquotes for reference lists
- No AI-ish phrases; no theatrical prose; direct, personal, grounded
- Always be LDS doctrinally accurate
- Speak to a Protestant audience — demonstrate the scriptural basis; don't assume LDS vocabulary

---

*Based on: inventory of E:\Apologetics\OrganizedReligion + cross-reference of all 155 existing
ldsdoctrines.com pages (as of 2026-05-19). Approximately 90 new pages proposed.*
