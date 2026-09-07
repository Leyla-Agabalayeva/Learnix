/**
 * Отзывы по всем курсам преподавателя (раздел 27 ТЗ).
 *
 * Данные приходят из /api/instructor/reviews — эндпоинт добавлен в этой фазе.
 * Он стартует от InstructorId из токена, поэтому чужие отзывы сюда попасть
 * не могут: отдельная проверка владения не требуется по построению выборки.
 */

import { api } from '../../api.js';
import { getApiLanguage, t } from '../../localization.js';
import { loader } from '../../components/loader.js';
import { emptyState } from '../../components/empty-state.js';
import * as format from '../../format.js';
import { startInstructorPage, showLoadError } from './common.js';

const $ = (id) => document.getElementById(id);

async function load() {
    loader.skeleton($('reviews-list'), { count: 3, variant: 'row' });

    try {
        const reviews = await api.get('/instructor/reviews', { query: { lang: getApiLanguage() } });
        render(reviews);
    } catch (error) {
        $('rating-overview').innerHTML = '';
        showLoadError($('reviews-list'), error, load);
    }
}

function render(reviews) {
    const overview = $('rating-overview');
    const list = $('reviews-list');

    overview.innerHTML = '';
    list.innerHTML = '';

    if (!reviews.length) {
        emptyState.render(list, {
            icon: '💬',
            title: t('instructor.noReviewsTitle'),
            text: t('instructor.noReviewsHint')
        });
        return;
    }

    overview.appendChild(buildOverview(reviews));
    reviews.forEach((review) => list.appendChild(buildReview(review)));
}

function buildOverview(reviews) {
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

function buildReview(review) {
    const item = document.createElement('article');
    item.className = 'instructor-review';

    const head = document.createElement('div');
    head.className = 'instructor-review-head';

    const author = document.createElement('div');
    author.className = 'instructor-review-author';

    const avatar = document.createElement('span');
    avatar.className = 'avatar';
    avatar.setAttribute('aria-hidden', 'true');
    avatar.textContent = (review.studentName ?? '?').slice(0, 1).toUpperCase();

    const info = document.createElement('div');

    const name = document.createElement('p');
    name.className = 'review-author';
    name.textContent = review.studentName;

    const meta = document.createElement('p');
    meta.className = 'review-date';
    const { filled, empty } = format.stars(review.rating);
    meta.innerHTML = `<span class="rating-stars">${'★'.repeat(filled)}<span class="star-empty">${'★'.repeat(empty)}</span></span> · `;
    meta.append(document.createTextNode(format.date(review.createdAt)));

    info.append(name, meta);
    author.append(avatar, info);

    // Название курса — ссылка: из ленты отзывов удобно сразу открыть курс.
    const courseLink = document.createElement('a');
    courseLink.className = 'instructor-review-course';
    courseLink.href = `/pages/course-details.html?id=${review.courseId}`;
    courseLink.textContent = review.courseTitle;

    head.append(author, courseLink);
    item.appendChild(head);

    if (review.comment) {
        const text = document.createElement('p');
        text.className = 'review-text';
        // textContent: отзыв пишет студент, вставлять его как HTML нельзя.
        text.textContent = review.comment;
        item.appendChild(text);
    }

    return item;
}

startInstructorPage(load);
