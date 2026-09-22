

import { api } from '../../api.js';
import { getApiLanguage, t } from '../../localization.js';
import { loader } from '../../components/loader.js';
import { emptyState } from '../../components/empty-state.js';
import { toast } from '../../components/toast.js';
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

    const replyContainer = document.createElement('div');
    replyContainer.className = 'instructor-reply-container';
    item.appendChild(replyContainer);
    renderReply(replyContainer, review);

    return item;
}

/**
 * Ответ преподавателя — один на отзыв. Показываем либо уже сохранённый текст
 * с кнопкой «Изменить», либо кнопку «Ответить», открывающую textarea прямо
 * на месте (без перехода на отдельную страницу).
 */
function renderReply(container, review) {
    container.innerHTML = '';

    if (review.instructorReply) {
        const box = document.createElement('div');
        box.className = 'instructor-reply';

        const label = document.createElement('p');
        label.className = 'instructor-reply-label';
        label.textContent = t('instructor.yourReply');
        box.appendChild(label);

        const text = document.createElement('p');
        text.className = 'instructor-reply-text';
        text.textContent = review.instructorReply;
        box.appendChild(text);

        const editLink = document.createElement('button');
        editLink.type = 'button';
        editLink.className = 'instructor-reply-edit';
        editLink.textContent = t('actions.edit');
        editLink.addEventListener('click', () => renderReplyEditor(container, review));
        box.appendChild(editLink);

        container.appendChild(box);
        return;
    }

    const replyButton = document.createElement('button');
    replyButton.type = 'button';
    replyButton.className = 'btn btn-ghost btn-sm mt-2';
    replyButton.textContent = t('instructor.writeReply');
    replyButton.addEventListener('click', () => renderReplyEditor(container, review));
    container.appendChild(replyButton);
}

function renderReplyEditor(container, review) {
    container.innerHTML = '';

    const editor = document.createElement('div');
    editor.className = 'instructor-reply-editor';

    const textarea = document.createElement('textarea');
    textarea.className = 'input';
    textarea.rows = 2;
    textarea.placeholder = t('instructor.replyPlaceholder');
    textarea.value = review.instructorReply ?? '';
    editor.appendChild(textarea);

    const actions = document.createElement('div');
    actions.className = 'review-form-actions mt-2';

    const submitButton = document.createElement('button');
    submitButton.type = 'button';
    submitButton.className = 'btn btn-primary btn-sm';
    submitButton.textContent = t('instructor.submitReply');
    submitButton.addEventListener('click', async () => {
        const reply = textarea.value.trim();
        if (!reply) {
            return;
        }

        loader.button(submitButton, true);

        try {
            const updated = await api.put(`/reviews/${review.id}/reply`, { reply }, { query: { lang: getApiLanguage() } });
            review.instructorReply = updated.instructorReply;
            review.instructorRepliedAt = updated.instructorRepliedAt;
            toast.success(t('instructor.replySubmitted'));
            renderReply(container, review);
        } catch (error) {
            toast.fromApiError(error);
            loader.button(submitButton, false);
        }
    });
    actions.appendChild(submitButton);

    const cancelButton = document.createElement('button');
    cancelButton.type = 'button';
    cancelButton.className = 'btn btn-ghost btn-sm';
    cancelButton.textContent = t('actions.cancel');
    cancelButton.addEventListener('click', () => renderReply(container, review));
    actions.appendChild(cancelButton);

    editor.appendChild(actions);
    container.appendChild(editor);
}

startInstructorPage(load);
