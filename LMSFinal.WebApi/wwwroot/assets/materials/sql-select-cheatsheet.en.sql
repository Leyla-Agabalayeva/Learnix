-- ============================================================
--  SELECT cheat sheet — SQL Server Fundamentals
--  The order in which SQL Server EXECUTES a query differs from
--  the order in which you WRITE it. Almost every "why can't I
--  use my alias in WHERE" question comes from this.
-- ============================================================

-- You write:            It executes:
--   SELECT                5. SELECT
--   FROM                  1. FROM / JOIN
--   WHERE                 2. WHERE
--   GROUP BY              3. GROUP BY
--   HAVING                4. HAVING
--   ORDER BY              6. ORDER BY

-- WHERE filters rows BEFORE grouping, HAVING filters groups AFTER.
SELECT  c.CategoryId, COUNT(*) AS CourseCount
FROM    Courses AS c
WHERE   c.Status = 'Published'      -- drop drafts before counting
GROUP BY c.CategoryId
HAVING  COUNT(*) >= 2               -- drop small categories after
ORDER BY CourseCount DESC;

-- LEFT JOIN + IS NULL — rows that have no match in the second table.
SELECT  u.Email
FROM    AspNetUsers AS u
LEFT JOIN Enrollments AS e ON e.StudentId = u.Id
WHERE   e.Id IS NULL;               -- students with no enrolment at all

-- A window function aggregates WITHOUT collapsing rows.
SELECT  e.CourseId,
        e.ProgressPercentage,
        AVG(e.ProgressPercentage) OVER (PARTITION BY e.CourseId) AS AvgOnCourse
FROM    Enrollments AS e;

-- Paging. TOP cannot skip rows — only OFFSET/FETCH can,
-- and it requires ORDER BY.
SELECT  c.Id, c.Price
FROM    Courses AS c
ORDER BY c.CreatedAt DESC
OFFSET  20 ROWS FETCH NEXT 10 ROWS ONLY;

-- NULL equals nothing, not even itself: = NULL is always UNKNOWN.
-- Use IS NULL / IS NOT NULL instead.
SELECT COUNT(*) FROM Lessons WHERE VideoUrl IS NULL;
