-- GetPageDetails
--
-- Kept in source control deliberately: this procedure is the read side of a full-record
-- upsert path, so a column missing here becomes DATA LOSS on the next save (an article read
-- back without websiteId gets rewritten with websiteId 0; without status it gets unpublished).
-- Changes to it must be reviewable in a diff.
--
-- Site scoping is enforced ONCE, at the top. Filtering only the first recordset is not enough:
-- recordsets 2-7 key off pageId alone, so a caller asking for another site's page still
-- received that page's articles, keywords, topics and layout. Blanking pageId makes every
-- subsequent `WHERE ... = pageId` fail to match, so the whole response goes empty together.
--
-- _websiteId is named with a leading underscore on purpose. A parameter called `websiteId`
-- would shadow the page column of the same name in recordset 1, making the query echo back
-- the requested site instead of the stored one — which would silently defeat every
-- cross-site check that compares a record's owner against the caller's site.

DROP PROCEDURE IF EXISTS `GetPageDetails`;

DELIMITER $$

CREATE DEFINER=`webadmin`@`%` PROCEDURE `GetPageDetails`(IN pageId INT, IN _websiteId INT)
BEGIN
    -- Scope guard. A NULL/0 site means "unscoped" — GetPageByNameAsync passes 0 when its
    -- int.TryParse fails, and must keep working.
    IF _websiteId IS NOT NULL AND _websiteId <> 0
       AND NOT EXISTS (SELECT 1 FROM page p WHERE p.id = pageId AND p.websiteId = _websiteId) THEN
        SET pageId = NULL;
    END IF;

    -- First Recordset: page information.
    -- The site condition here is redundant with the guard above, and deliberately kept: it is a
    -- second, independent check on the row that actually carries the site, so the page cannot
    -- leak even if the guard is later edited or removed.
    -- It MUST use the same NULL/0-means-unscoped semantics as the guard. A bare
    -- `AND p.websiteId = _websiteId` disagrees with the guard on the unscoped case and returns
    -- an empty page row while recordsets 2-7 still populate — the inverse of the leak.
    SELECT p.id, p.name, p.description, p.layoutid, p.style, p.websiteId, p.status
    FROM page p
    WHERE p.id = pageId
      AND (_websiteId IS NULL OR _websiteId = 0 OR p.websiteId = _websiteId);

    -- Second Recordset: keywords associated with the page
    SELECT k.value
    FROM keyword k
    JOIN page_keyword pk ON pk.keyword_id = k.id
    WHERE pk.page_id = pageId;

    -- Third Recordset: topics associated with the page
    SELECT t.value
    FROM topic t
    JOIN page_topic pt ON pt.topic_id = t.id
    WHERE pt.page_id = pageId;

    -- Fourth Recordset: articles associated with the page.
    -- articlePath / memeImagePath / articleId are aliased to the names the C# reader expects
    -- (PageDAO.GetPageDetailsAsync). memepath in particular does NOT match memeImagePath by
    -- case-insensitivity — it is a different word, and returning it unaliased made the value
    -- null, which a page save then wrote back over the stored meme image.
    SELECT a.id,
           a.name,
           a.description,
           a.articlepath AS articlePath,
           a.memepath    AS memeImagePath,
           a.sequence_no,
           a.articleid   AS articleId,
           a.status,
           a.websiteId
    FROM article a
    INNER JOIN page_article pa ON a.id = pa.article_id
    WHERE pa.page_id = pageId;

    -- Fifth Recordset: keywords associated with the articles
    SELECT DISTINCT k.value, pa.article_id
    FROM keyword k
    INNER JOIN article_keyword ak ON ak.keyword_id = k.id
    INNER JOIN page_article pa    ON ak.article_id = pa.article_id
    WHERE pa.page_id = pageId;

    -- Sixth Recordset: topics associated with the articles
    SELECT DISTINCT t.value, pa.article_id
    FROM topic t
    INNER JOIN article_topic at2 ON at2.topic_id = t.id
    INNER JOIN page_article pa   ON at2.article_id = pa.article_id
    WHERE pa.page_id = pageId;

    -- Seventh Recordset: layout associated with the page
    SELECT l.name
    FROM layout l
    INNER JOIN page p ON l.id = p.layoutid
    WHERE p.id = pageId;
END$$

DELIMITER ;
