# Два бага: что именно чинить

Диагностика проведена, ниже — точные адреса и шаги. Код я не трогала.

---

# Баг 1. «Видео недоступно»

## Что нашла

Проверила все 10 роликов через YouTube oEmbed (200 = существует и разрешён
к встраиванию):

```
e2IbNHi4uCI  200      87oOfyYvvVk  404   ← битый
dK4Yb6-LxAk  200      ZX23XohBWfM  404   ← битый
fYq5PXgSsbE  200
BM4CHBmAPh4  200
7Q17ubqLfaM  200
9yeOJ0ZMUYw  200
eIHKZfgddLM  200
y17RuWkWdn8  200
```

Два ролика **не существуют** — я выдумала их идентификаторы, когда писала seed.
Это моя ошибка, извини.

## Что ты уже сделала правильно

Ты заменила два ролика на `youtu.be/M1pvp2NvO1g` и `youtu.be/fl6r-9rQjns` —
**оба рабочие (200)**, и формат `youtu.be` плеер понимает, я проверила
`video-player.js`.

Но осталось два несоответствия:

| Курс | Урок | Что не так |
|---|---|---|
| ASP.NET Core Web API | **Controllers and routing** | битый `87oOfyYvvVk` на месте; новый ролик ты поставила уроку **JWT authentication**, а не этому |
| Entity Framework Core | **Migrations** | в seed уже исправлено, но **в базе остался старый** `ZX23XohBWfM` |

## Почему база не обновилась

Seed наполняет базу **только когда она пустая**. Правка JSON на уже
наполненную базу не влияет — поэтому в браузере ты видишь старые ссылки.

## Как чинить

### Шаг 1. Доправить seed

Файл `LMSFinal.Persistence/Seed/Data/courses/02-aspnet-core-web-api.json`,
модуль 0, урок 0 — **Controllers and routing**. Найди строку:

```json
"videoUrl": "https://www.youtube.com/watch?v=87oOfyYvvVk",
```

и подставь рабочий ролик. Подобрать можно любой по теме, но **обязательно
проверь его** командой из шага 3.

### Шаг 2. Обновить базу

Проще всего — SQL, без пересоздания базы. В SSMS или через `sqlcmd`:

```sql
USE LMSFinal;

-- Migrations: ставим тот ролик, который уже лежит в seed
UPDATE Lessons
   SET VideoUrl = 'https://youtu.be/fl6r-9rQjns?si=CfMM-w-jD6VeqiHR'
 WHERE Id = '25FC1D80-4884-48F3-B646-DB3538056912';

-- Controllers and routing: подставь свою рабочую ссылку
UPDATE Lessons
   SET VideoUrl = 'ВСТАВЬ_СЮДА_РАБОЧУЮ_ССЫЛКУ'
 WHERE Id = '87D55FA3-7AD7-4488-8E16-CE2B999D7010';

-- Проверка: битых ссылок остаться не должно
SELECT lt.Title, l.VideoUrl
  FROM Lessons l
  JOIN LessonTranslations lt ON lt.LessonId = l.Id AND lt.LanguageCode = 'EN'
 WHERE l.VideoUrl LIKE '%87oOfyYvvVk%' OR l.VideoUrl LIKE '%ZX23XohBWfM%';
```

Последний запрос должен вернуть **0 строк**.

### Шаг 3. Как проверять ролик перед вставкой

Не полагайся на то, что видео открывается у тебя в браузере — важно, разрешено
ли **встраивание**. Проверка одной командой (подставь свой ID):

```bash
curl -s -o nul -w "%{http_code}\n" "https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v=ТВОЙ_ID&format=json"
```

- `200` — ролик существует и встраивается, годится
- `404` — не существует
- `401` — существует, но автор **запретил встраивание**: будет та же чёрная
  плашка «Видео недоступно»

### Если проще убрать видео

Уроки без видео показывают спокойную заглушку «Этот урок текстовый». Это
выглядит нормально и ничего не ломает:

```sql
UPDATE Lessons SET VideoUrl = NULL
 WHERE Id IN ('87D55FA3-7AD7-4488-8E16-CE2B999D7010',
              '25FC1D80-4884-48F3-B646-DB3538056912');
```

---

# Баг 2. Конспекты всегда на русском

## Что нашла

Ты права, и это тоже моя недоработка. Когда я делала материалы уроков
(Phase 25), я решила **не переводить** их и записала это в комментарии:

> «Не переводится: имя файла и его тип от языка интерфейса не зависят»

Рассуждение было неверным. В таблице `LessonResources` есть только
`FileName`, `FileUrl`, `FileType` — **колонки языка нет вообще**. Поэтому:

- в английском интерфейсе заголовок «Materials» переведён, а под ним
  файл «Асинхронность в JavaScript.md»;
- само содержимое файла тоже русское.

Все четыре файла в `wwwroot/assets/materials/` написаны по-русски.

## Вариант А — правильный (рекомендую, ~2 часа)

Сделать материалы такими же переводимыми, как всё остальное содержимое.

**1. Добавить поле в сущность** — `LMSFinal.Domain/Entities/LessonResource.cs`:

```csharp
public LanguageCode LanguageCode { get; set; }
```

**2. Настроить колонку** — `LMSFinal.Persistence/Configurations/LessonResourceConfiguration.cs`.
Возьми за образец `LessonTranslationConfiguration.cs`, строка 18:

```csharp
builder.Property(r => r.LanguageCode).HasConversion<string>().HasMaxLength(10).IsRequired();
builder.HasIndex(r => new { r.LessonId, r.LanguageCode });
```

**3. Создать миграцию:**

```bash
dotnet ef migrations add AddLanguageToLessonResources --project LMSFinal.Persistence --startup-project LMSFinal.WebApi
dotnet ef database update --project LMSFinal.Persistence --startup-project LMSFinal.WebApi
```

**4. Фильтровать при отдаче** — `LMSFinal.Application/Services/LessonService.cs`,
метод `MapLocalized`, строка **269**. Сейчас там:

```csharp
lesson.Resources
    .OrderBy(r => r.FileName)
```

Стало:

```csharp
lesson.Resources
    .Where(r => r.LanguageCode == translation.LanguageCode)
    .OrderBy(r => r.FileName)
```

Обрати внимание: фильтруем по `translation.LanguageCode`, а **не** по
запрошенному языку. Если для урока сработал запасной язык (например,
попросили RU, а есть только EN), материалы должны быть того же языка,
что и текст урока, — иначе получится урок на английском с русским конспектом.

**5. Поле в seed** — `LMSFinal.Persistence/Seed/SeedSpec.cs`, класс `ResourceSpec`:
добавь `LanguageCode`, а в `DemoDataSeeder.BuildLesson` перенеси его в сущность.

**6. Написать файлы на трёх языках.** Это самая долгая часть: 4 материала × 3 языка.
Можно сократить — сделай материалы только для двух-трёх уроков, но на всех языках.

## Вариант Б — быстрый и честный (~20 минут)

Если времени нет: оставить один материал на урок, но **сделать язык видимым**,
чтобы это не выглядело недоделкой.

1. Переименуй записи так, чтобы язык был указан явно:

```sql
UPDATE LessonResources SET FileName = N'Асинхронность в JavaScript (RU).md'
 WHERE FileUrl = '/assets/materials/js-async-notes.md';
```

2. В README, в разделе «Что осталось за рамками», допиши строку:

> **Материалы уроков не переводятся.** У `LessonResources` нет колонки языка:
> файл один на урок, независимо от выбранного языка. Текст уроков при этом
> переводится полностью.

Это не «замазать проблему»: преподаватель всё равно заметит, и лучше, если
ты назовёшь это сама и объяснишь, почему так вышло.

## Чего делать НЕ надо

Не переводи только имя файла, оставив содержимое русским. Английское имя
и русский текст внутри — хуже, чем честное «(RU)» в названии.

---

# Порядок работы

1. Сначала **баг 1** — он виден на защите сразу, а чинится за 15 минут.
2. Потом решай по времени: вариант А или Б для второго.
3. После правок база должна пройти проверку:

```sql
-- битых роликов нет
SELECT COUNT(*) FROM Lessons
 WHERE VideoUrl LIKE '%87oOfyYvvVk%' OR VideoUrl LIKE '%ZX23XohBWfM%';
```

4. Пересобрать и прогнать тесты:

```bash
dotnet build
dotnet test
```

Должно быть 0 ошибок и 97 пройденных тестов.
