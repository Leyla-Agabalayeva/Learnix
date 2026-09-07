/**
 * Страница урока — то место, где студент проводит основное время.
 *
 * Адрес страницы принимает две формы:
 *   /pages/lesson.html?courseId=X            — «продолжить обучение»: сервер
 *                                              сам решит, какой урок открыть;
 *   /pages/lesson.html?courseId=X&lessonId=Y — конкретный урок.
 *
 * Первую форму уже используют кабинет студента и страница курса, поэтому
 * ломать её нельзя: без lessonId страница спрашивает /courses/{id}/continue.
 *
 * Переход между уроками НЕ перезагружает страницу: меняется содержимое
 * и адрес (history.pushState). Перезагрузка каждый раз заново тянула бы
 * программу курса и прогресс — те же данные ради одного урока.
 */

import { api } from '../api.js';
import { requireAuth, ROLES } from '../auth.js';
import { onLanguageChange, getApiLanguage, t } from '../localization.js';
import { onReady } from '../ready.js';
import { toast } from '../components/toast.js';
import { loader } from '../components/loader.js';
import { emptyState } from '../components/empty-state.js';
import { mountQuiz } from '../components/quiz.js';
import { renderVideo } from '../components/video-player.js';
import { setHtml } from '../sanitize.js';
import { duration } from '../format.js';

const $ = (id) => document.getElementById(id);

/** Всё состояние страницы в одном объекте — иначе оно расползается по замыканиям. */
const state = {
    courseId: null,
    lessonId: null,
    course: null,          // CourseLocalizedDto вместе с curriculum
    lesson: null,          // LessonLocalizedDto текущего урока
    completed: new Set(),  // Id пройденных уроков
    flat: [],              // уроки всех модулей одним списком — для «назад/вперёд»
    quiz: null             // управление тестом, если он есть у урока
};

/** Смена языка во время теста откладывается — см. startLanguageWatcher. */
let languageChangePending = false;

/** Первая отрисовка урока на этой странице — фокус на заголовок не переводим. */
let isFirstRender = true;

onReady(async () => {
    if (!requireAuth(ROLES.STUDENT)) {
        return;
    }

    const params = new URLSearchParams(window.location.search);
    state.courseId = params.get('courseId');
    state.lessonId = params.get('lessonId');

    if (!state.courseId) {
        showFatal(t('lesson.noCourse'), null, { title: t('states.notFound'), icon: '🧭' });
        return;
    }

    await loadCourse();
    startLanguageWatcher();

    // Кнопки «назад»/«вперёд» браузера: адрес меняем через pushState,
    // и без этого обработчика они уводили бы со страницы целиком.
    window.addEventListener('popstate', () => {
        const lessonId = new URLSearchParams(window.location.search).get('lessonId');

        if (lessonId && lessonId !== state.lessonId) {
            openLesson(lessonId, { history: 'none' });
        }
    });
});

// ---------------------------------------------------------------------------
// Загрузка
// ---------------------------------------------------------------------------

async function loadCourse() {
    loader.block($('lesson-content'), t('states.loading'));

    try {
        // Курс и прогресс запрашиваются вместе: программа нужна для панели,
        // прогресс — для галочек, и ни один не зависит от другого.
        const [course, progress] = await Promise.all([
            api.get(`/courses/${state.courseId}`, { query: { lang: getApiLanguage() } }),
            api.get(`/courses/${state.courseId}/progress`)
        ]);

        state.course = course;
        applyProgress(progress);

        state.flat = course.curriculum.flatMap((module) =>
            module.lessons.map((lesson) => ({ ...lesson, moduleId: module.id, moduleTitle: module.title })));

        renderCourseHeader();
        renderOutline();

        if (state.flat.length === 0) {
            emptyState.render($('lesson-content'), {
                icon: '📭',
                title: t('lesson.emptyCourse'),
                text: t('lesson.emptyCourseHint'),
                action: { label: t('actions.goToCourse'), href: `/pages/course-details.html?id=${state.courseId}` }
            });
            return;
        }

        // Урок из адреса открываем как есть; выбранный сервером — вписываем
        // в адрес, чтобы обновление страницы и «Назад» вели туда же.
        const fromUrl = state.lessonId;
        await openLesson(fromUrl ?? (await resolveStartingLesson()), { history: fromUrl ? 'none' : 'replace' });
    } catch (error) {
        // 403 — студент не записан на курс. Уводить его на страницу входа
        // (это делает api.js только для 401) бессмысленно, он уже вошёл;
        // правильный ответ — отправить на страницу курса записываться.
        if (error.status === 403) {
            showFatal(t('lesson.notEnrolled'), {
                label: t('actions.goToCourse'),
                href: `/pages/course-details.html?id=${state.courseId}`
            });
            return;
        }

        emptyState.error($('lesson-content'), {
            message: error.isNetworkError ? t('states.networkError') : error.message,
            onRetry: loadCourse
        });
    }
}

/** Какой урок открыть, если в адресе только courseId. */
async function resolveStartingLesson() {
    try {
        const next = await api.get(`/courses/${state.courseId}/continue`);

        // Курс пройден целиком — continue не возвращает урок. Открываем первый:
        // студент пришёл перечитывать материал, и пустая страница вместо урока
        // выглядела бы как поломка.
        return next.lessonId ?? state.flat[0].id;
    } catch {
        return state.flat[0].id;
    }
}

/**
 * @param {string} lessonId
 * @param {{ history?: 'push'|'replace'|'none' }} [options]
 *        push    — обычный переход, добавляет запись в историю;
 *        replace — первый урок: адрес без lessonId заменяется на адрес с ним,
 *                  иначе «Назад» возвращает на адрес, который уже не совпадает
 *                  с показанным уроком;
 *        none    — переход по «Назад»/«Вперёд»: историю трогать нельзя.
 */
async function openLesson(lessonId, { history = 'push' } = {}) {
    if (!lessonId) {
        return;
    }

    state.lessonId = lessonId;
    teardownQuiz();

    if (history !== 'none') {
        const url = `${window.location.pathname}?courseId=${state.courseId}&lessonId=${lessonId}`;
        window.history[history === 'replace' ? 'replaceState' : 'pushState']({ lessonId }, '', url);
    }

    loader.block($('lesson-content'), t('states.loading'));
    markCurrentInOutline();

    try {
        state.lesson = await api.get(`/lessons/${lessonId}`, { query: { lang: getApiLanguage() } });

        // Фокус переводим только при СМЕНЕ урока. На первой отрисовке студент
        // ничего не нажимал, и рамка фокуса на заголовке выглядит как поле ввода,
        // в которое зачем-то предлагают что-то напечатать.
        renderLesson({ moveFocus: !isFirstRender });
        isFirstRender = false;
    } catch (error) {
        emptyState.error($('lesson-content'), {
            message: error.isNetworkError ? t('states.networkError') : error.message,
            onRetry: () => openLesson(lessonId, { history: 'none' })
        });
    }
}

function applyProgress(progress) {
    state.completed = new Set(progress.completedLessonIds ?? []);

    const percentage = Math.round(progress.progressPercentage);

    $('progress-bar').style.width = `${percentage}%`;
    $('progress').setAttribute('aria-valuenow', String(percentage));
    $('progress-label').textContent = t('lesson.progressOf', {
        completed: progress.completedLessons,
        total: progress.totalLessons,
        percentage
    });
}

// ---------------------------------------------------------------------------
// Отрисовка
// ---------------------------------------------------------------------------

function renderCourseHeader() {
    $('course-title').textContent = state.course.title;
    $('course-link').href = `/pages/course-details.html?id=${state.courseId}`;
    document.title = `${state.course.title} — Learnix`;
}

function renderOutline() {
    const body = $('outline-body');
    body.innerHTML = '';

    state.course.curriculum.forEach((module, moduleIndex) => {
        const section = document.createElement('section');
        section.className = 'outline-module';

        const heading = document.createElement('h3');
        heading.className = 'outline-module-title';
        heading.textContent = `${moduleIndex + 1}. ${module.title}`;

        const list = document.createElement('ol');
        list.className = 'outline-lessons';

        module.lessons.forEach((lesson) => {
            list.appendChild(buildOutlineItem(lesson));
        });

        section.append(heading, list);
        body.appendChild(section);
    });

    markCurrentInOutline();
}

function buildOutlineItem(lesson) {
    const item = document.createElement('li');
    item.className = 'outline-lesson';
    item.dataset.lessonId = lesson.id;

    // Кнопка, а не ссылка: переход не перезагружает страницу, а меняет
    // содержимое. Ссылка обещала бы навигацию, которой не происходит.
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'outline-lesson-button';
    button.addEventListener('click', () => openLesson(lesson.id));

    const check = document.createElement('span');
    check.className = 'outline-check';
    check.setAttribute('aria-hidden', 'true');

    const title = document.createElement('span');
    title.className = 'outline-lesson-title';
    title.textContent = lesson.title;

    const meta = document.createElement('span');
    meta.className = 'outline-lesson-meta';
    meta.textContent = duration(lesson.durationMinutes);

    if (lesson.hasQuiz) {
        const quizMark = document.createElement('span');
        quizMark.className = 'outline-quiz';
        quizMark.textContent = '📝';
        quizMark.title = t('lesson.quizLabel');
        meta.appendChild(quizMark);
    }

    button.append(check, title, meta);
    item.appendChild(button);
    return item;
}

/**
 * Подсветка текущего урока и галочки пройденных.
 * Отдельной функцией, потому что вызывается и при переходе между уроками,
 * и после «отметить пройденным» — перерисовывать всю панель ради двух
 * классов было бы расточительно, а главное — сбросило бы прокрутку панели.
 */
function markCurrentInOutline() {
    $('outline-body').querySelectorAll('.outline-lesson').forEach((item) => {
        const isCurrent = item.dataset.lessonId === state.lessonId;
        const isDone = state.completed.has(item.dataset.lessonId);

        item.classList.toggle('is-current', isCurrent);
        item.classList.toggle('is-completed', isDone);

        const button = item.querySelector('.outline-lesson-button');
        button.setAttribute('aria-current', isCurrent ? 'true' : 'false');

        // Галочку и слово «пройден» видит зрячий; скринридеру нужно то же
        // самое словами, иначе список уроков звучит одинаково весь.
        const check = item.querySelector('.outline-check');
        check.textContent = isDone ? '✓' : '';

        let status = button.querySelector('.sr-only');

        if (!status) {
            status = document.createElement('span');
            status.className = 'sr-only';
            button.appendChild(status);
        }

        status.textContent = isDone ? t('lesson.statusCompleted') : t('lesson.statusNotCompleted');
    });
}

function renderLesson({ moveFocus = true } = {}) {
    const root = $('lesson-content');
    root.innerHTML = '';

    const position = currentIndex();
    const lesson = state.lesson;

    // --- шапка урока
    const header = document.createElement('header');
    header.className = 'lesson-header';

    const kicker = document.createElement('p');
    kicker.className = 'lesson-kicker';
    kicker.textContent = t('lesson.positionOf', { current: position + 1, total: state.flat.length });

    const title = document.createElement('h1');
    title.className = 'lesson-title';
    title.textContent = lesson.title;

    const meta = document.createElement('p');
    meta.className = 'lesson-meta';
    meta.textContent = duration(lesson.durationMinutes);

    header.append(kicker, title, meta);
    root.appendChild(header);

    // Язык содержимого может отличаться от языка интерфейса: перевода на
    // выбранный язык может не быть, и сервер отдаёт запасной (ResolvedLanguage).
    // Молча показывать чужой язык нечестно — студент решит, что перевод такой.
    if (lesson.resolvedLanguage && lesson.resolvedLanguage !== getApiLanguage()) {
        const notice = document.createElement('p');
        notice.className = 'lesson-language-notice';
        notice.textContent = t('lesson.languageFallback', { language: lesson.resolvedLanguage });
        root.appendChild(notice);
    }

    // --- видео
    const video = document.createElement('div');
    video.className = 'lesson-video';
    renderVideo(video, lesson.videoUrl, lesson.title);
    root.appendChild(video);

    // --- текст урока
    const article = document.createElement('article');
    article.className = 'lesson-body';
    setHtml(article, lesson.content);
    root.appendChild(article);

    // --- материалы
    if (lesson.resources?.length) {
        root.appendChild(buildResources(lesson.resources));
    }

    // --- тест
    if (lesson.quizId) {
        const quizMount = document.createElement('div');
        quizMount.className = 'lesson-quiz';
        root.appendChild(quizMount);

        state.quiz = mountQuiz(quizMount, lesson.quizId, {
            // Сданный тест мог оказаться последним условием для сертификата —
            // перечитываем прогресс, чтобы полоса и галочки не отставали.
            onPassed: refreshProgress,
            onAttemptEnd: applyPendingLanguageChange
        });
    }

    root.appendChild(buildFooterNav(position));

    // Фокус переводим на заголовок: при переходе к следующему уроку он остался
    // бы на кнопке «Дальше», и человек с клавиатурой не понял бы, что страница
    // сменилась. tabindex="-1" — чтобы заголовок принимал фокус программно,
    // но не попадал в обход по Tab.
    title.tabIndex = -1;

    if (moveFocus) {
        title.focus({ preventScroll: true });
    }

    window.scrollTo({ top: 0, behavior: 'auto' });
}

function buildResources(resources) {
    const section = document.createElement('section');
    section.className = 'lesson-resources';

    const heading = document.createElement('h2');
    heading.className = 'lesson-section-title';
    heading.textContent = t('lesson.materials');

    const list = document.createElement('ul');
    list.className = 'lesson-resource-list';

    resources.forEach((resource) => {
        const item = document.createElement('li');

        const link = document.createElement('a');
        link.className = 'lesson-resource';
        link.href = resource.fileUrl;
        // download подсказывает браузеру сохранить файл, а не открывать .md
        // как текст прямо во вкладке, потеряв урок.
        link.download = resource.fileName;

        const icon = document.createElement('span');
        icon.className = 'lesson-resource-icon';
        icon.setAttribute('aria-hidden', 'true');
        icon.textContent = resourceIcon(resource.fileType);

        const name = document.createElement('span');
        name.className = 'lesson-resource-name';
        name.textContent = resource.fileName;

        const type = document.createElement('span');
        type.className = 'lesson-resource-type';
        type.textContent = (resource.fileType || '').toUpperCase();

        link.append(icon, name, type);
        item.appendChild(link);
        list.appendChild(item);
    });

    section.append(heading, list);
    return section;
}

function buildFooterNav(position) {
    const nav = document.createElement('nav');
    nav.className = 'lesson-nav';
    nav.setAttribute('aria-label', t('lesson.navLabel'));

    const previous = document.createElement('button');
    previous.type = 'button';
    previous.className = 'btn btn-secondary';
    previous.textContent = `← ${t('actions.previous')}`;
    previous.disabled = position <= 0;
    previous.addEventListener('click', () => openLesson(state.flat[position - 1].id));

    const complete = document.createElement('button');
    complete.type = 'button';
    complete.dataset.complete = 'true';
    applyCompleteButtonState(complete);
    complete.addEventListener('click', () => markComplete(complete));

    const next = document.createElement('button');
    next.type = 'button';
    next.className = 'btn btn-secondary';
    next.textContent = `${t('actions.next')} →`;
    next.disabled = position >= state.flat.length - 1;
    next.addEventListener('click', () => openLesson(state.flat[position + 1].id));

    nav.append(previous, complete, next);
    return nav;
}

function applyCompleteButtonState(button) {
    const isDone = state.completed.has(state.lessonId);

    button.className = isDone ? 'btn btn-ghost' : 'btn btn-primary';
    button.textContent = isDone ? `✓ ${t('lesson.completed')}` : t('lesson.markComplete');

    // Пройденный урок не «отмечается заново»: повторный вызов сервер стерпит
    // (он идемпотентен), но кнопка, которая ничего не меняет, только сбивает.
    button.disabled = isDone;
}

// ---------------------------------------------------------------------------
// Действия
// ---------------------------------------------------------------------------

async function markComplete(button) {
    loader.button(button, true);

    try {
        await api.post(`/lessons/${state.lessonId}/complete`);

        state.completed.add(state.lessonId);
        markCurrentInOutline();

        await refreshProgress();

        loader.button(button, false);
        applyCompleteButtonState(button);

        advanceAfterCompletion();
    } catch (error) {
        loader.button(button, false);
        toast.fromApiError(error);
    }
}

/**
 * Автопереход к следующему уроку.
 *
 * Есть одно исключение, ради которого эта функция отдельная: если у урока
 * есть тест и он ещё не сдан, автопереход НЕ срабатывает. Иначе «отметить
 * пройденным» уносило бы студента мимо теста, который нужен для сертификата
 * (CertificateService требует сданными все квизы курса), — и он бы даже
 * не увидел, что его пропустил.
 */
function advanceAfterCompletion() {
    const position = currentIndex();
    const isLast = position >= state.flat.length - 1;

    if (state.quiz && !state.quiz.isPassed()) {
        toast.info(t('lesson.quizPending'));
        document.querySelector('.lesson-quiz')?.scrollIntoView({
            behavior: prefersReducedMotion() ? 'auto' : 'smooth',
            block: 'start'
        });
        return;
    }

    if (isLast) {
        toast.success(t('lesson.courseFinished'));
        return;
    }

    toast.success(t('lesson.completedGoingNext'));
    openLesson(state.flat[position + 1].id);
}

async function refreshProgress() {
    try {
        applyProgress(await api.get(`/courses/${state.courseId}/progress`));
        markCurrentInOutline();
    } catch {
        // Прогресс — сопровождающая информация. Урок уже отмечен пройденным,
        // и ронять на этом всю страницу было бы несоразмерно: полоса просто
        // обновится при следующем переходе.
    }
}

function teardownQuiz() {
    state.quiz?.destroy();
    state.quiz = null;
}

/**
 * Смена языка интерфейса перезагружает урок — но не посреди теста:
 * перерисовка стёрла бы отмеченные ответы и обнулила таймер.
 * Поэтому перезагрузка откладывается до конца попытки.
 */
function startLanguageWatcher() {
    onLanguageChange(async () => {
        if (state.quiz?.isInProgress()) {
            languageChangePending = true;
            toast.info(t('lesson.languageAfterQuiz'));

            // Подписи самого теста всё же обновляем: они не хранят ответов.
            state.quiz.refresh();
            return;
        }

        await reloadForLanguage();
    });

}

/** Вызывается тестом, когда попытка закончилась — ответом или отменой. */
async function applyPendingLanguageChange() {
    if (!languageChangePending) {
        return;
    }

    languageChangePending = false;
    await reloadForLanguage();
}

async function reloadForLanguage() {
    const current = state.lessonId;

    await loadCourse();

    if (current && current !== state.lessonId) {
        await openLesson(current, { history: 'none' });
    }
}

// ---------------------------------------------------------------------------
// Мелочи
// ---------------------------------------------------------------------------

function currentIndex() {
    return state.flat.findIndex((lesson) => lesson.id === state.lessonId);
}

function resourceIcon(fileType) {
    switch ((fileType || '').toLowerCase()) {
        case 'pdf': return '📕';
        case 'zip': return '🗜️';
        case 'sql': return '🗄️';
        case 'md':
        case 'txt': return '📄';
        case 'png':
        case 'jpg':
        case 'svg': return '🖼️';
        default: return '📎';
    }
}

function showFatal(message, action, { title = t('states.forbidden'), icon = '🔒' } = {}) {
    emptyState.render($('lesson-content'), { icon, title, text: message, action: action ?? null });
}

function prefersReducedMotion() {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}
