-- ============================================================
--  SELECT cheat sheet — SQL Server əsasları
--  SQL Server-in sorğunu İCRA ETDİYİ ardıcıllıq
--  sizin sorğunu YAZDIĞINIZ ardıcıllıqdan fərqlidir.
--  "Niyə WHERE daxilində alias istifadə edə bilmirəm?"
--  sualının səbəbi əsasən budur.
-- ============================================================

-- Siz yazırsınız:       SQL Server icra edir:
--   SELECT                5. SELECT
--   FROM                  1. FROM / JOIN
--   WHERE                 2. WHERE
--   GROUP BY              3. GROUP BY
--   HAVING                4. HAVING
--   ORDER BY              6. ORDER BY

-- WHERE sətirləri qruplaşdırmadan ƏVVƏL filtrdən keçirir,
-- HAVING isə qruplaşdırmadan SONRA qrupları filtrdən keçirir.
SELECT  c.CategoryId, COUNT(*) AS CourseCount
FROM    Courses AS c
WHERE   c.Status = 'Published'      -- saymadan əvvəl qaralamaları çıxarır
GROUP BY c.CategoryId
HAVING  COUNT(*) >= 2               -- qruplaşdırmadan sonra kiçik kateqoriyaları çıxarır
ORDER BY CourseCount DESC;

-- LEFT JOIN + IS NULL — ikinci cədvəldə uyğun sətri olmayan
-- sətirləri tapmaq üçün istifadə olunur.
SELECT  u.Email
FROM    AspNetUsers AS u
LEFT JOIN Enrollments AS e ON e.StudentId = u.Id
WHERE   e.Id IS NULL;               -- ümumiyyətlə qeydiyyatı olmayan tələbələr

-- Window funksiyası sətirləri birləşdirmədən (itirmədən)
-- hesablama aparmağa imkan verir.
SELECT  e.CourseId,
        e.ProgressPercentage,
        AVG(e.ProgressPercentage) OVER (PARTITION BY e.CourseId) AS AvgOnCourse
FROM    Enrollments AS e;

-- Səhifələmə (Paging). TOP sətirləri ötürə bilmir —
-- bunun üçün OFFSET/FETCH istifadə olunur.
-- OFFSET/FETCH isə ORDER BY tələb edir.
SELECT  c.Id, c.Price
FROM    Courses AS c
ORDER BY c.CreatedAt DESC
OFFSET  20 ROWS FETCH NEXT 10 ROWS ONLY;

-- NULL heç nəyə, hətta özünə belə bərabər deyil:
-- = NULL həmişə UNKNOWN nəticəsi qaytarır.
-- Bunun əvəzinə IS NULL / IS NOT NULL istifadə edin.
SELECT COUNT(*) FROM Lessons WHERE VideoUrl IS NULL;