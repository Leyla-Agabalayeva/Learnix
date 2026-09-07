/**
 * Дашборд преподавателя (раздел 27 ТЗ).
 *
 * Все пять цифр приходят из /api/instructor/dashboard-stats — их считает сервер
 * по реальным записям и отзывам. На клиенте ничего не досчитывается, иначе цифры
 * здесь и в аналитике могли бы разойтись.
 */

import { api } from '../../api.js';
import { getApiLanguage, t } from '../../localization.js';
import { getUser } from '../../auth.js';
import { loader } from '../../components/loader.js';
import { emptyState } from '../../components/empty-state.js';
import * as format from '../../format.js';
import { startInstructorPage, showLoadError, buildStat } from './common.js';
import { buildInstructorCourseCard } from './courses.js';

const $ = (id) => document.getElementById(id);

async function load() {
    $('greeting').textContent = t('instructor.welcome', { name: getUser()?.firstName ?? '' });

    loader.skeleton($('recent-courses'), { count: 3 });

    try {
        const [stats, courses] = await Promise.all([
            api.get('/instructor/dashboard-stats'),
            api.get('/courses/my', { query: { lang: getApiLanguage() } })
        ]);

        renderStats(stats);
        renderRecent(courses);
    } catch (error) {
        $('stats').innerHTML = '';
        showLoadError($('recent-courses'), error, load);
    }
}

function renderStats(stats) {
    const container = $('stats');
    container.innerHTML = '';

    container.append(
        buildStat(t('instructor.statCourses'), format.number(stats.totalCourses)),
        buildStat(t('instructor.statStudents'), format.number(stats.totalStudents)),
        buildStat(t('instructor.statEnrollments'), format.number(stats.totalEnrollments)),
        // Рейтинг показываем только если отзывы есть: «0.0 ⭐» читается как плохая
        // оценка, хотя оценок просто нет.
        buildStat(t('instructor.statRating'),
            stats.averageRating > 0 ? `${format.rating(stats.averageRating)} ⭐` : '—'),
        buildStat(t('instructor.statCompletion'), `${format.rating(stats.averageCompletionRate)}%`)
    );
}

function renderRecent(courses) {
    const container = $('recent-courses');
    container.innerHTML = '';

    if (!courses.length) {
        emptyState.render(container, {
            icon: '📚',
            title: t('instructor.noCoursesTitle'),
            text: t('instructor.noCoursesHint'),
            action: { label: t('nav.createCourse'), href: '/pages/instructor/create-course.html' }
        });
        return;
    }

    courses.slice(0, 3).forEach((course) => container.appendChild(buildInstructorCourseCard(course)));
}

startInstructorPage(load);
