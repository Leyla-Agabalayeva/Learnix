-- ============================================================
--  SELECT cheat sheet — SQL Server Fundamentals
--  Порядок, в котором SQL Server ВЫПОЛНЯЕТ запрос, отличается
--  от порядка, в котором вы его ПИШЕТЕ. Отсюда почти все
--  «почему алиас не виден в WHERE».
-- ============================================================

-- Пишем так:            Выполняется так:
--   SELECT                5. SELECT
--   FROM                  1. FROM / JOIN
--   WHERE                 2. WHERE
--   GROUP BY              3. GROUP BY
--   HAVING                4. HAVING
--   ORDER BY              6. ORDER BY

-- WHERE отсекает строки ДО группировки, HAVING — группы ПОСЛЕ.
SELECT  c.CategoryId, COUNT(*) AS CourseCount
FROM    Courses AS c
WHERE   c.Status = 'Published'      -- отсеиваем черновики до подсчёта
GROUP BY c.CategoryId
HAVING  COUNT(*) >= 2               -- отсеиваем мелкие категории после
ORDER BY CourseCount DESC;

-- LEFT JOIN + IS NULL — строки, которых нет во второй таблице.
SELECT  u.Email
FROM    AspNetUsers AS u
LEFT JOIN Enrollments AS e ON e.StudentId = u.Id
WHERE   e.Id IS NULL;               -- студенты без единой записи на курс

-- Оконная функция считает, НЕ схлопывая строки.
SELECT  e.CourseId,
        e.ProgressPercentage,
        AVG(e.ProgressPercentage) OVER (PARTITION BY e.CourseId) AS AvgOnCourse
FROM    Enrollments AS e;

-- Пагинация. TOP не умеет пропускать — только OFFSET/FETCH,
-- и он требует ORDER BY.
SELECT  c.Id, c.Price
FROM    Courses AS c
ORDER BY c.CreatedAt DESC
OFFSET  20 ROWS FETCH NEXT 10 ROWS ONLY;

-- NULL не равен ничему, включая самого себя: = NULL всегда UNKNOWN.
-- Правильно  — IS NULL / IS NOT NULL.
SELECT COUNT(*) FROM Lessons WHERE VideoUrl IS NULL;
