

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
        buildStat(t('instructor.statEnrollments'), format.number(stats.totalEnrollments), { tone: 'success' }),
        // Рейтинг показываем только если отзывы есть: «0.0 ⭐» читается как плохая
        // оценка, хотя оценок просто нет.
        buildStat(t('instructor.statRating'),
            stats.averageRating > 0 ? `${format.rating(stats.averageRating)} ⭐` : '—',
            { tone: stats.averageRating >= 4 ? 'success' : stats.averageRating > 0 ? 'warning' : 'neutral' }),
        buildStat(t('instructor.statCompletion'), `${format.rating(stats.averageCompletionRate)}%`,
            { tone: stats.averageCompletionRate >= 50 ? 'success' : 'warning' })
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
