/**
 * Страница курса (раздел 9 ТЗ).
 *
 * Ключевой момент — кнопка действия. Что на ней написано, решает СЕРВЕР
 * через поле IsEnrolled в ответе, а не фронтенд по содержимому localStorage.
 * Подменить состояние в браузере и «стать записанным» невозможно: запись
 * проверяется по таблице Enrollments.
 *
 *   гость            → «Войдите, чтобы записаться»
 *   студент, не записан → «Записаться на курс»
 *   студент, записан    → «Продолжить обучение»
 *   преподаватель       → кнопки нет, курс открыт для просмотра
 */

import { api } from '../api.js';
import { getApiLanguage, onLanguageChange, t } from '../localization.js';
import { isAuthenticated, isStudent, getUser } from '../auth.js';
import { toast } from '../components/toast.js';
import { loader } from '../components/loader.js';
import { emptyState } from '../components/empty-state.js';
import * as format from '../format.js';
import { onReady } from '../ready.js';
import * as cart from '../components/cart.js';

const $ = (id) => document.getElementById(id);

/** Id курса берётся из query-строки: /pages/course-details.html?id=… */
const courseId = new URLSearchParams(window.location.search).get('id');

let course = null;

async function load() {
    if (!courseId) {
        showNotFound();
        return;
    }

    loader.block($('page-state'));
    $('main').classList.add('hidden');

    try {
        // Запрос НЕ анонимный: если пользователь вошёл, сервер по токену
        // определит, записан ли он, и вернёт IsEnrolled.
        course = await api.get(`/courses/${courseId}`, {
            query: { lang: getApiLanguage() },
            anonymous: !isAuthenticated()
        });

        render();
        loadReviews();
    } catch (error) {
        if (error?.status === 404) {
            showNotFound();
        } else {
            emptyState.error($('page-state'), {
                message: error?.isNetworkError ? t('states.networkError') : error?.message,
                onRetry: load
            });
        }
    }
}

function showNotFound() {
    emptyState.render($('page-state'), {
        icon: '🔍',
        title: t('states.notFound'),
        text: t('states.noCoursesHint'),
        action: { label: t('details.backToCatalogue'), href: '/pages/courses.html' }
    });
}

// ---------------------------------------------------------------------------
// Отрисовка
// ---------------------------------------------------------------------------

function render() {
    $('page-state').innerHTML = '';
    $('main').classList.remove('hidden');

    document.title = `${course.title} — Learnix`;

    $('course-category').textContent = course.categoryName;
    $('course-level').textContent = format.level(course.level);
    $('course-title').textContent = course.title;
    $('course-tagline').textContent = course.shortDescription;
    $('course-description').textContent = course.description;

    $('enrolled-badge').classList.toggle('hidden', !course.isEnrolled);

    renderMeta();
    renderThumbnail();
    renderFacts();
    renderLearnList();
    renderCurriculum();
    renderEnrollAction();
}

function renderMeta() {
    // Курс без отзывов не показывает «0.0 ★» — ноль звёзд читается как плохая
    // оценка, хотя оценок просто нет.
    $('meta-rating').textContent = course.reviewCount > 0
        ? `⭐ ${format.rating(course.averageRating)} (${format.plural(course.reviewCount, 'units.review')})`
        : t('course.noReviews');

    $('meta-students').textContent = `👥 ${format.plural(course.enrollmentCount, 'units.student')}`;
    $('meta-duration').textContent = `⏱ ${format.duration(course.durationMinutes)}`;
    $('meta-instructor').textContent = `👤 ${course.instructorName}`;
}

function renderThumbnail() {
    const container = $('course-thumb');
    container.innerHTML = '';

    if (!course.thumbnailUrl) {
        return;
    }

    const image = document.createElement('img');
    image.src = course.thumbnailUrl;
    image.alt = '';
    image.addEventListener('error', () => image.remove());
    container.appendChild(image);
}

function renderFacts() {
    $('course-price').textContent = format.price(course.price);
    $('fact-lessons').textContent = format.number(course.lessonCount);
    $('fact-modules').textContent = format.number(course.curriculum.length);
    $('fact-duration').textContent = format.duration(course.durationMinutes);
    $('fact-level').textContent = format.level(course.level);
}

function renderLearnList() {
    const list = $('learn-list');
    const items = course.whatYouWillLearn ?? [];

    // Секцию целиком прячем, если пунктов нет: пустой заголовок выглядит как недоделка.
    $('learn-section').classList.toggle('hidden', items.length === 0);
    list.innerHTML = '';

    items.forEach((text) => {
        const item = document.createElement('li');
        item.className = 'learn-item';

        const check = document.createElement('span');
        check.className = 'learn-check';
        check.setAttribute('aria-hidden', 'true');
        check.textContent = '✓';

        const label = document.createElement('span');
        label.textContent = text;

        item.append(check, label);
        list.appendChild(item);
    });
}

function renderCurriculum() {
    const container = $('curriculum');
    container.innerHTML = '';

    const modules = course.curriculum ?? [];

    $('curriculum-summary').textContent = t('details.curriculumSummary', {
        modules: format.plural(modules.length, 'units.module'),
        lessons: format.plural(course.lessonCount, 'units.lesson'),
        duration: format.duration(course.durationMinutes)
    });

    if (modules.length === 0) {
        emptyState.render(container, { icon: '📭', title: t('states.empty') });
        return;
    }

    modules.forEach((module, index) => {
        // <details> даёт раскрытие и клавиатурную доступность без JavaScript.
        const details = document.createElement('details');
        details.className = 'module';
        // Первый модуль открыт: сразу видно, из чего состоит курс.
        details.open = index === 0;

        const summary = document.createElement('summary');
        summary.className = 'module-head';

        const left = document.createElement('span');
        left.className = 'module-head-left';

        const chevron = document.createElement('span');
        chevron.className = 'module-chevron';
        chevron.setAttribute('aria-hidden', 'true');
        chevron.textContent = '›';

        const title = document.createElement('span');
        title.textContent = `${index + 1}. ${module.title}`;

        left.append(chevron, title);

        const meta = document.createElement('span');
        meta.className = 'module-meta';
        meta.textContent = `${format.plural(module.lessonCount, 'units.lesson')} · ${format.duration(module.durationMinutes)}`;

        summary.append(left, meta);
        details.appendChild(summary);

        module.lessons.forEach((lesson) => details.appendChild(buildLessonRow(lesson)));
        container.appendChild(details);
    });
}

function buildLessonRow(lesson) {
    const row = document.createElement('div');
    row.className = 'lesson-row';

    const icon = document.createElement('span');
    icon.className = 'lesson-icon';
    icon.setAttribute('aria-hidden', 'true');
    // Замок для незаписанных — сразу понятно, что материал за записью.
    icon.textContent = course.isEnrolled ? '▶' : '🔒';

    const title = document.createElement('span');
    title.className = 'lesson-title';
    title.textContent = lesson.title;

    row.append(icon, title);

    if (lesson.hasQuiz) {
        const quiz = document.createElement('span');
        quiz.className = 'badge badge-neutral';
        quiz.textContent = t('details.quizBadge');
        row.appendChild(quiz);
    }

    const duration = document.createElement('span');
    duration.className = 'lesson-duration';
    duration.textContent = format.duration(lesson.durationMinutes);
    row.appendChild(duration);

    return row;
}

/** Кнопка действия — зависит от того, что вернул сервер. */
function renderEnrollAction() {
    const container = $('enroll-action');
    container.innerHTML = '';

    if (course.isEnrolled) {
        const link = document.createElement('a');
        link.className = 'btn btn-primary btn-block btn-lg';
        link.href = `/pages/lesson.html?courseId=${course.id}`;
        link.textContent = t('actions.continueLearning');
        container.appendChild(link);
        return;
    }

    if (!isAuthenticated()) {
        const link = document.createElement('a');
        link.className = 'btn btn-primary btn-block btn-lg';
        // returnUrl вернёт пользователя на эту же страницу после входа.
        link.href = `/pages/login.html?returnUrl=${encodeURIComponent(window.location.pathname + window.location.search)}`;
        link.textContent = t('details.loginToEnroll');
        container.appendChild(link);
        return;
    }

    // Преподаватель и админ на курс не записываются — кнопку не показываем.
    if (!isStudent()) {
        return;
    }

    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'btn btn-primary btn-block btn-lg';
    button.textContent = t('actions.enroll');
    button.addEventListener('click', () => enroll(button));
    container.appendChild(button);

    container.appendChild(buildCartButton());
}

function buildCartButton() {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'btn btn-secondary btn-block mt-2';

    const paint = () => {
        const inCart = cart.has(course.id);
        button.textContent = t(inCart ? 'cart.inCart' : 'cart.addToCart');
        button.classList.toggle('is-active', inCart);
    };

    button.addEventListener('click', () => {
        if (cart.has(course.id)) {
            cart.remove(course.id);
        } else {
            cart.add(course.id);
        }
        paint();
    });

    paint();
    return button;
}

async function enroll(button) {
    loader.button(button, true);

    try {
        await api.post(`/courses/${course.id}/enroll`);
        toast.success(t('details.enrollSuccess'));

        // Перезагружаем курс, а не правим флаг локально: сервер вернёт актуальное
        // состояние, и число студентов на странице тоже обновится.
        await load();
    } catch (error) {
        toast.fromApiError(error);
        loader.button(button, false);
    }
}

// ---------------------------------------------------------------------------
// Отзывы
// ---------------------------------------------------------------------------

async function loadReviews() {
    const container = $('reviews');
    loader.skeleton(container, { count: 2, variant: 'row' });

    try {
        const reviews = await api.get(`/courses/${courseId}/reviews`, { anonymous: true });
        renderReviews(reviews);
    } catch {
        // Отзывы — не главное на странице: если они не загрузились, курс всё равно
        // должен читаться. Показываем пустой блок вместо ошибки во весь экран.
        renderReviews([]);
    }
}

function renderReviews(reviews) {
    const overview = $('rating-overview');
    const container = $('reviews');

    overview.innerHTML = '';
    container.innerHTML = '';

    const myUserId = getUser()?.userId;
    const myReview = isStudent() ? reviews.find((r) => r.studentId === myUserId) : null;

    // Форма для НОВОГО отзыва — только пока своего отзыва ещё нет. Как только
    // он появится, писать/менять его можно прямо в своей карточке в списке,
    // как у любого другого комментария — отдельная форма сверху больше не нужна.
    renderNewReviewPrompt(myReview);

    if (!reviews.length) {
        emptyState.render(container, {
            icon: '💬',
            title: t('details.noReviewsTitle'),
            text: t('details.noReviewsText')
        });
        return;
    }

    overview.appendChild(buildRatingOverview(reviews));
    reviews.forEach((review) => container.appendChild(buildReview(review, review === myReview)));
}

// ---------------------------------------------------------------------------
// Форма отзыва — доступна только записанным студентам (сервер и так это
// проверит, но незаписанному кнопка «Отправить» вернула бы 403 без пользы).
// ---------------------------------------------------------------------------

function renderNewReviewPrompt(myReview) {
    const container = $('review-form-container');
    container.innerHTML = '';

    if (myReview || !isStudent() || !course.isEnrolled) {
        return;
    }

    const wrapper = document.createElement('div');
    wrapper.className = 'review-form';

    const heading = document.createElement('p');
    heading.className = 'font-semibold';
    heading.textContent = t('details.writeReview');
    wrapper.appendChild(heading);

    wrapper.appendChild(buildReviewEditor(null, {
        onCancel: null,
        onSaved: loadReviews
    }));

    container.appendChild(wrapper);
}

/**
 * Звёзды + textarea + кнопки — общий кусок и для формы нового отзыва,
 * и для инлайн-редактирования уже существующего прямо в его карточке.
 */
function buildReviewEditor(review, { onCancel, onSaved }) {
    const editor = document.createElement('div');
    editor.className = 'review-editor';

    const ratingLabel = document.createElement('p');
    ratingLabel.className = 'text-sm text-muted';
    ratingLabel.textContent = t('details.yourRating');
    editor.appendChild(ratingLabel);

    let selectedRating = review?.rating ?? 0;
    const starsRow = document.createElement('div');
    starsRow.className = 'review-form-stars';

    const starButtons = [1, 2, 3, 4, 5].map((value) => {
        const star = document.createElement('button');
        star.type = 'button';
        star.className = 'review-form-star';
        star.setAttribute('aria-label', String(value));
        star.textContent = '★';
        star.addEventListener('click', () => {
            selectedRating = value;
            paintStars();
        });
        starsRow.appendChild(star);
        return star;
    });

    function paintStars() {
        starButtons.forEach((star, index) => {
            star.classList.toggle('is-filled', index < selectedRating);
        });
    }
    paintStars();

    editor.appendChild(starsRow);

    const textarea = document.createElement('textarea');
    textarea.className = 'input mt-2';
    textarea.rows = 3;
    textarea.placeholder = t('details.reviewCommentPlaceholder');
    textarea.value = review?.comment ?? '';
    editor.appendChild(textarea);

    const actions = document.createElement('div');
    actions.className = 'review-form-actions mt-2';

    const submitButton = document.createElement('button');
    submitButton.type = 'button';
    submitButton.className = 'btn btn-primary btn-sm';
    submitButton.textContent = t('details.submitReview');
    submitButton.addEventListener('click', async () => {
        if (!selectedRating) {
            toast.warning(t('details.reviewNeedsRating'));
            return;
        }

        loader.button(submitButton, true);

        try {
            const payload = { rating: selectedRating, comment: textarea.value.trim() || null };

            if (review) {
                await api.put(`/reviews/${review.id}`, payload);
                toast.success(t('details.reviewUpdated'));
            } else {
                await api.post(`/courses/${courseId}/reviews`, payload);
                toast.success(t('details.reviewSubmitted'));
            }

            await onSaved();
        } catch (error) {
            toast.fromApiError(error);
            loader.button(submitButton, false);
        }
    });
    actions.appendChild(submitButton);

    if (onCancel) {
        const cancelButton = document.createElement('button');
        cancelButton.type = 'button';
        cancelButton.className = 'btn btn-ghost btn-sm';
        cancelButton.textContent = t('actions.cancel');
        cancelButton.addEventListener('click', onCancel);
        actions.appendChild(cancelButton);
    }

    editor.appendChild(actions);
    return editor;
}

async function deleteReview(review) {
    if (!window.confirm(t('details.deleteReviewConfirm'))) {
        return;
    }

    try {
        await api.delete(`/reviews/${review.id}`);
        toast.success(t('details.reviewDeleted'));
        await loadReviews();
    } catch (error) {
        toast.fromApiError(error);
    }
}

function buildRatingOverview(reviews) {
    const average = reviews.reduce((sum, review) => sum + review.rating, 0) / reviews.length;
    const { filled, empty } = format.stars(average);

    const box = document.createElement('div');
    box.className = 'rating-overview';

    const value = document.createElement('div');
    value.className = 'rating-big';
    value.textContent = format.rating(average);

    const right = document.createElement('div');

    const stars = document.createElement('p');
    stars.className = 'rating-stars';
    stars.style.fontSize = 'var(--text-xl)';
    stars.innerHTML = '★'.repeat(filled) + `<span class="star-empty">${'★'.repeat(empty)}</span>`;

    const count = document.createElement('p');
    count.className = 'text-sm text-muted mt-2';
    count.textContent = format.plural(reviews.length, 'units.review');

    right.append(stars, count);
    box.append(value, right);
    return box;
}

function buildReview(review, isMine = false) {
    const item = document.createElement('article');
    item.className = isMine ? 'review is-mine' : 'review';

    const head = document.createElement('div');
    head.className = 'review-head';

    const avatar = document.createElement('span');
    avatar.className = 'avatar';
    avatar.setAttribute('aria-hidden', 'true');
    avatar.textContent = (review.studentName ?? '?').slice(0, 1).toUpperCase();

    const info = document.createElement('div');

    const author = document.createElement('p');
    author.className = 'review-author';
    author.textContent = review.studentName;

    const meta = document.createElement('p');
    meta.className = 'review-date';
    const { filled, empty } = format.stars(review.rating);
    meta.innerHTML = `<span class="rating-stars">${'★'.repeat(filled)}<span class="star-empty">${'★'.repeat(empty)}</span></span> · `;
    meta.append(document.createTextNode(format.date(review.createdAt)));

    info.append(author, meta);
    head.append(avatar, info);
    item.appendChild(head);

    const body = document.createElement('div');
    body.className = 'review-body';

    if (review.comment) {
        const text = document.createElement('p');
        text.className = 'review-text';
        // textContent: отзыв пишет пользователь, вставлять его как HTML нельзя.
        text.textContent = review.comment;
        body.appendChild(text);
    }

    if (review.instructorReply) {
        const reply = document.createElement('div');
        reply.className = 'instructor-reply';

        const label = document.createElement('p');
        label.className = 'instructor-reply-label';
        label.textContent = t('details.instructorReply');
        reply.appendChild(label);

        const text = document.createElement('p');
        text.className = 'instructor-reply-text';
        text.textContent = review.instructorReply;
        reply.appendChild(text);

        body.appendChild(reply);
    }

    item.appendChild(body);

    // «Изменить»/«Удалить» — только под своей карточкой, как обычные действия
    // под комментарием, а не отдельная форма где-то ещё на странице.
    if (isMine) {
        const actions = document.createElement('div');
        actions.className = 'review-own-actions';

        const editLink = document.createElement('button');
        editLink.type = 'button';
        editLink.className = 'review-own-action';
        editLink.textContent = t('actions.edit');
        editLink.addEventListener('click', () => {
            body.innerHTML = '';
            actions.classList.add('hidden');

            body.appendChild(buildReviewEditor(review, {
                onSaved: loadReviews,
                onCancel: () => {
                    body.innerHTML = '';
                    if (review.comment) {
                        const text = document.createElement('p');
                        text.className = 'review-text';
                        text.textContent = review.comment;
                        body.appendChild(text);
                    }
                    actions.classList.remove('hidden');
                }
            }));
        });
        actions.appendChild(editLink);

        const deleteLink = document.createElement('button');
        deleteLink.type = 'button';
        deleteLink.className = 'review-own-action is-danger';
        deleteLink.textContent = t('actions.delete');
        deleteLink.addEventListener('click', () => deleteReview(review));
        actions.appendChild(deleteLink);

        item.appendChild(actions);
    }

    return item;
}

onReady(() => {
    load();

    // Контент курса приходит переведённым с сервера — при смене языка
    // страницу нужно перезапросить целиком.
    onLanguageChange(load);
});
