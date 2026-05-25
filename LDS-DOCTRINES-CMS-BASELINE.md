# LDSDoctrine.com CMS Baseline Snapshot
*Captured: 2026-05-19*

This is the pre-build state of the CMS configuration.
Do not alter these values without recording changes here.

---

## MENU TOOLS — NOT IMPLEMENTED

`get_menu`, `add_menu_item`, `update_menu_item`, `delete_menu_item` are NOT wired up.

The site menu is stored as a JSON file in S3:
  `websites/{siteName}/{menuConfigFileName}`

It is read/written directly by the React admin UI — there are currently no Lambda
endpoints exposing menu CRUD. To enable menu management via MCP, either:
  - Add /menu endpoints to the backend Lambda
  - Give the MCP server S3 credentials via @aws-sdk/client-s3

**Implication:** Menu additions for new pages CANNOT be done via agents.
They must be done manually in the React admin, or the backend must be extended first.
The content build can proceed — pages can be created and published — but they will
not appear in site navigation until menu items are added manually.

---

## AVAILABLE LAYOUTS

As returned by get_layouts:

- Standard                ← use this for all new single-article doctrine pages
- 1, 1/3, 2/3 Grid
- 1, 1/3 Grid
- 1, 2, 3 Grid
- 1, 2/3, 1/3 Grid
- 1, 2/3 Grid
- 1, 2 Grid
- 1, 3 Grid
- 1/3 Grid
- 1/3, 1 Grid
- 1/3, 2/3 Grid
- 2, 1 Grid
- 2 Grid
- 2, 3 Grid
- 2/3, 1 Grid
- 2/3 Grid
- 3, 1 Grid
- 3, 2 Grid
- 3 Grid

---

## EXISTING KEYWORDS (5)

- Blacks in Priesthood
- Exaltation
- Heavenly Mother
- Kingdoms of Glory
- Polygamy

**Suggested new keywords to add for the content build:**
- Plan of Salvation
- Atonement
- Book of Mormon
- Priesthood
- Temple
- Eternal Marriage
- Second Coming
- Apologetics
- Biblical Characters
- Covenant

---

## EXISTING TOPICS (25)

- Articles of Faith
- Atonement of Christ
- Blacks in Priesthood
- Christian Identity
- Church History
- Church Leader's Teachings
- Coping Mechanisms
- Counseling and Support Groups
- Ecumenical Perspectives
- Exaltation
- Faith and Doubt
- Family and Community Support
- Godhead or Trinity
- Heavenly Mother
- Historical Context
- Jesus Christ
- Kingdoms of Glory
- LDS Church Resources
- Personal Revelation and Prayer
- Polygamy
- Prophets and Revelation
- Purpose of Life
- Restoration of Christ's Church
- Scriptural Study
- Scriptures
- Testimonies

**Suggested new topics to add for the content build:**
- Fall of Adam and Eve
- Spirit World
- Eternal Life
- Book of Mormon Evidences
- Covenant Doctrine
- End Times / Second Coming
- Biblical Figures
- Book of Mormon Characters
- Temple and Ordinances
- Missionary Work
