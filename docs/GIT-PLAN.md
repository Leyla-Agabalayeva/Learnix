# План истории коммитов

История коммитов оценивается отдельным критерием. Сейчас репозитория нет вовсе,
и весь проект существует одной кучей файлов — это худший из возможных вариантов
для защиты: не видно ни порядка работы, ни того, что делалось своими руками.

Ниже — план, как разложить готовый проект на осмысленную историю.

---

## Правило номер один: секреты не попадают в первый коммит

`appsettings.Development.json` содержит строку подключения, ключ подписи JWT
и пароль администратора. **Один раз попав в историю, они останутся в ней
навсегда** — удаление файла следующим коммитом ничего не исправит, потому что
старый коммит по-прежнему содержит их. Придётся переписывать историю целиком.

Поэтому порядок жёсткий:

```
коммит 1  →  .gitignore  (и больше ничего)
коммит 2  →  всё остальное
```

`.gitignore` уже создан и исключает `appsettings.Development.json`.
Вместо него в репозиторий идёт `appsettings.Development.example.json`
с теми же ключами и пустыми значениями.

**Проверьте перед первым коммитом:**

```bash
git status --short | findstr appsettings
```

Должен быть виден только `appsettings.Development.example.json`.
Если видно `appsettings.Development.json` — останавливайтесь и разбирайтесь.

---

## Соглашение о сообщениях

Формат — Conventional Commits, он общепринятый и читается без пояснений:

```
feat:     новая возможность
fix:      исправление
refactor: переработка без смены поведения
test:     тесты
docs:     документация
chore:    настройка, зависимости
style:    оформление, без смены логики
```

Сообщение — в повелительном наклонении и по-английски: `add`, а не `added`
и не «добавил». Первая строка не длиннее ~70 символов.

---

## Список коммитов

Каждая строка — один коммит. Порядок повторяет то, как проект собирался.

### Основание

| № | Сообщение | Что входит |
|---|---|---|
| 1 | `chore: add .gitignore` | **только** `.gitignore` |
| 2 | `chore: scaffold solution with clean architecture layers` | `.sln`, семь пустых `.csproj`, ссылки между ними |
| 3 | `feat(domain): add entities and enums` | `LMSFinal.Domain/Entities`, `Enums`, `Common` |
| 4 | `feat(persistence): add DbContext and fluent configurations` | `AppDbContext`, `Configurations/*` |
| 5 | `feat(persistence): add initial migration` | `Migrations/*` |
| 6 | `feat(domain): add repository interfaces` | `Domain/Interfaces` |
| 7 | `feat(persistence): implement repositories and unit of work` | `Persistence/Repositories` |

### Аутентификация и общий каркас API

| № | Сообщение | Что входит |
|---|---|---|
| 8 | `feat(api): add ApiResponse envelope and base controller` | `Contracts/Common`, `ApiControllerBase` |
| 9 | `feat(api): add global exception middleware` | `GlobalExceptionMiddleware`, `Application/Common/Exceptions` |
| 10 | `feat(auth): add identity, jwt and role seeding` | `Infrastructure/Identity`, настройки JWT |
| 11 | `feat(auth): add register, login and profile endpoints` | `AuthController`, `AuthService`, DTO |
| 12 | `chore: add settings example without secrets` | `appsettings.Development.example.json` |

### Предметная область

| № | Сообщение | Что входит |
|---|---|---|
| 13 | `feat(courses): add course catalog with filters and sorting` | `CourseService`, `CoursesController`, `CourseRepository` |
| 14 | `feat(courses): add modules and lessons with reordering` | `ModuleService`, `LessonService`, соответствующие контроллеры |
| 15 | `feat(enrollments): add enrollment and my-courses` | `EnrollmentService`, `EnrollmentsController` |
| 16 | `feat(progress): add lesson completion and course progress` | `ProgressService`, `ProgressController` |
| 17 | `feat(quizzes): add quizzes with server-side scoring` | `QuizService`, `QuizzesController` |
| 18 | `feat(certificates): issue certificates and generate pdf` | `CertificateService`, `QuestPdfCertificateGenerator` |
| 19 | `feat(reviews): add course reviews with enrollment check` | `ReviewService`, `ReviewsController` |
| 20 | `feat(wishlist): add wishlist endpoints` | `WishlistService`, `WishlistController` |
| 21 | `feat(analytics): add instructor analytics and grade book` | `AnalyticsService`, `GradeBookService` |
| 22 | `feat(notifications): add notification service and endpoints` | `NotificationService`, `NotificationsController` |
| 23 | `feat(i18n): add translation tables and language fallback` | `*Translation`, `TranslationResolver`, параметр `?lang=` |
| 24 | `feat(validation): add fluentvalidation rules` | `Application/Validators` |

### Качество

| № | Сообщение | Что входит |
|---|---|---|
| 25 | `docs(api): document endpoints for swagger` | `SwaggerExtensions`, фильтры, XML-комментарии контроллеров |
| 26 | `feat(seed): add demo catalog and consistent activity data` | `Persistence/Seed` целиком |
| 27 | `test: add unit tests for services` | `LMSFinal.Tests` целиком |

### Фронтенд

| № | Сообщение | Что входит |
|---|---|---|
| 28 | `feat(ui): add design tokens and base styles` | `css/tokens.css`, `base.css`, `layout.css`, `components.css` |
| 29 | `feat(ui): add api client, auth and localization modules` | `js/api.js`, `auth.js`, `localization.js`, `ready.js`, `format.js` |
| 30 | `feat(ui): add reusable components` | `js/components/*` |
| 31 | `feat(ui): add landing page and auth pages` | `index.html`, `pages/login.html`, `register.html` + скрипты |
| 32 | `feat(ui): add course catalog and course details` | `pages/courses.html`, `course-details.html` + скрипты |
| 33 | `feat(ui): add student dashboard` | `pages/student/*` |
| 34 | `feat(ui): add instructor dashboard` | `pages/instructor/*` кроме конструктора |
| 35 | `feat(ui): add course builder with drag and drop` | `course-builder.html`, `create-course.html`, `drag-list.js`, `translation-tabs.js` |
| 36 | `feat(ui): add lesson page and quiz` | `pages/lesson.html`, `js/pages/lesson.js`, `components/quiz.js`, `video-player.js`, `sanitize.js` |
| 37 | `feat(ui): add certificate and public verification pages` | `certificate.html`, `verify-certificate.html` + скрипты |
| 38 | `feat(ui): add wishlist button with guest sign-in flow` | `components/wishlist.js`, изменения в карточке курса |

### Полировка

| № | Сообщение | Что входит |
|---|---|---|
| 39 | `fix(i18n): localize api errors, page titles and auth pages` | правки Phase 27 |
| 40 | `fix(ui): fix mobile layout, tables and touch targets` | правки Phase 28 |
| 41 | `fix(a11y): fix heading order, menu keyboard support, aria-busy` | правки Phase 28 |
| 42 | `chore: add cache-control for static files` | `Program.cs` |
| 43 | `docs: add readme, erd and screenshots` | `README.md`, `docs/` |

**Итого 43 коммита.**

---

## Как это сделать

### Первый коммит — только .gitignore

```bash
cd C:\Users\Leyla\source\repos\LMSFinal
git init
git add .gitignore
git commit -m "chore: add .gitignore"
```

### Проверьте, что секреты исключены

```bash
git status --short
```

`appsettings.Development.json` в списке быть **не должно**.

### Дальше по списку

Для каждого коммита добавляйте только его файлы:

```bash
git add LMSFinal.Domain/Entities LMSFinal.Domain/Enums
git commit -m "feat(domain): add entities and enums"
```

---

## Честно о датах

Все коммиты получат сегодняшнюю дату — история будет выглядеть как работа
одного дня. Это нормально и лучше, чем её отсутствие: ценность в **структуре**,
в том, что видно последовательность и осмысленность шагов.

Подделывать даты через `GIT_COMMITTER_DATE` не советую: во-первых, это обман,
во-вторых, он легко вскрывается — у всех коммитов совпадут временные метки
создания объектов.

Если спросят, почему всё в один день, честный ответ: «проект делался по этапам,
репозиторий завёл в конце и разложил работу по коммитам» — это лучше, чем
выдуманная хронология.

---

## Что сделать с проектом на GitHub

1. Создать **приватный** репозиторий (публичный — только если преподаватель просит).
2. `git remote add origin <url>` и `git push -u origin main`.
3. Проверить на сайте, что `appsettings.Development.json` там **нет**.
4. Убедиться, что `README.md` отображается на главной и картинки видны:
   пути в нём относительные (`docs/screenshots/...`), это на GitHub работает.

---

## Если секреты всё же попали в историю

Не паникуйте, но и не делайте вид, что ничего не было:

1. **Смените ключ JWT и пароли** — они скомпрометированы независимо от того,
   что вы сделаете с историей.
2. Если ещё не пушили — проще всего `git checkout --orphan` и начать заново.
3. Если уже запушили — переписывать историю (`git filter-repo`) и делать
   force push.

Именно поэтому `.gitignore` идёт первым коммитом.
