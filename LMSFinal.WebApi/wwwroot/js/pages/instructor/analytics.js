/**
 * Аналитика по курсу (раздел 18.3 ТЗ).
 *
 * Все цифры считает сервер в /api/courses/{id}/analytics, и он же проверяет
 * владение: запрос по чужому курсу вернёт 403 независимо от того, какой Id
 * подставить в адресную строку.
 */

import { api } from '../../api.js';
import { t } from '../../localization.js';
import { loader } from '../../components/loader.js';
import { emptyState } from '../../components/empty-state.js';
import * as format from '../../format.js';
import { startInstructorPage, showLoadError, buildStat } from './common.js';
import { setupCoursePicker } from './course-picker.js';

const $ = (id) => document.getElementById(id);

async function load() {
    await setupCoursePicker(async (courseId) => {
        if (!courseId) {
            emptyState.render($('analytics-content'), {
                icon: '📈',
                title: t('instructor.noCoursesTitle'),
                text: t('instructor.noCoursesHint'),
                action: { label: t('nav.createCourse'), href: '/pages/instructor/create-course.html' }
            });
            return;
        }

        await loadAnalytics(courseId);
    });
}

async function loadAnalytics(courseId) {
    const container = $('analytics-content');
    loader.block(container);

    try {
        const analytics = await api.get(`/courses/${courseId}/analytics`);
        render(analytics);
    } catch (error) {
        showLoadError(container, error, () => loadAnalytics(courseId));
    }
}

function render(analytics) {
    const container = $('analytics-content');
    container.innerHTML = '';

    container.appendChild(buildCompletionCard(analytics));

    const stats = document.createElement('div');
    stats.className = 'grid-stats';

    stats.append(
        buildStat(t('instructor.analyticsEnrolled'), format.number(analytics.studentsEnrolled)),
        buildStat(t('instructor.analyticsCompleted'), format.number(analytics.studentsCompleted)),
        buildStat(t('instructor.analyticsProgress'), `${format.rating(analytics.averageProgress)}%`),
        // Прочерк вместо нуля там, где данных нет: «0%» читается как плохой
        // результат, хотя тесты просто ещё никто не сдавал.
        buildStat(t('instructor.analyticsQuizScore'),
            analytics.averageQuizScore > 0 ? `${format.rating(analytics.averageQuizScore)}%` : '—'),
        buildStat(t('instructor.analyticsRating'),
            analytics.reviewsCount > 0 ? `${format.rating(analytics.averageRating)} ⭐` : '—'),
        buildStat(t('instructor.analyticsReviews'), format.number(analytics.reviewsCount))
    );

    container.appendChild(stats);
}

/**
 * Доля завершивших — главная цифра для автора курса: она показывает не сколько
 * человек записалось, а скольким курс оказался по силам.
 */
function buildCompletionCard(analytics) {
    const card = document.createElement('div');
    card.className = 'completion-card';

    const rate = analytics.studentsEnrolled === 0
        ? 0
        : (analytics.studentsCompleted / analytics.studentsEnrolled) * 100;

    const numbers = document.createElement('div');
    numbers.className = 'completion-numbers';

    const value = document.createElement('span');
    value.className = 'completion-value';
    value.textContent = `${format.rating(rate)}%`;

    const label = document.createElement('span');
    label.className = 'text-muted';
    label.textContent =
        `${t('instructor.completionRate')} · ${format.number(analytics.studentsCompleted)} / ${format.number(analytics.studentsEnrolled)}`;

    numbers.append(value, label);

    const progress = document.createElement('div');
    progress.className = 'progress';
    const bar = document.createElement('div');
    bar.className = 'progress-bar';
    bar.style.width = `${Math.min(100, rate)}%`;
    progress.appendChild(bar);

    card.append(numbers, progress);
    return card;
}

startInstructorPage(load);
