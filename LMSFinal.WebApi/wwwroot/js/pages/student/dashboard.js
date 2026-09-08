/**
 * Дашборд студента (раздел 26 ТЗ).
 *
 * Порядок блоков не случайный: первым идёт «Продолжить обучение», потому что
 * ради этого студент сюда и заходит. Статистика — вторична, недавние курсы — третьи.
 */

import { api } from '../../api.js';
import { getApiLanguage, t } from '../../localization.js';
import { getUser } from '../../auth.js';
import { loader } from '../../components/loader.js';
import { emptyState } from '../../components/empty-state.js';
import { createCourseCard } from '../../components/course-card.js';
import * as format from '../../format.js';
import { startStudentPage, showLoadError, buildStat } from './common.js';

const $ = (id) => document.getElementById(id);

async function load() {
    $('greeting').textContent = t('student.welcome', { name: getUser()?.firstName ?? '' });

    loader.block($('continue-section'));
    loader.skeleton($('recent-courses'), { count: 3 });

    try {
        // Три независимых запроса параллельно — последовательные просто
        // складывали бы задержки друг к другу.
        const [enrollments, gradeBook, certificates] = await Promise.all([
            api.get('/enrollments/my', { query: { lang: getApiLanguage() } }),
            api.get('/gradebook'),
            api.get('/certificates/my')
        ]);

        renderContinue(enrollments);
        renderStats(enrollments, gradeBook, certificates);
        renderRecent(enrollments);
    } catch (error) {
        $('stats').innerHTML = '';
        $('recent-courses').innerHTML = '';
        showLoadError($('continue-section'), error, load);
    }
}

/**
 * Блок «Продолжить обучение» — курс, который студент проходит прямо сейчас.
 * Берём активный курс с наибольшим прогрессом: скорее всего именно к нему
 * человек и хочет вернуться.
 */
function renderContinue(enrollments) {
    const container = $('continue-section');
    container.innerHTML = '';

    const active = enrollments
        .filter((enrollment) => enrollment.status === 'Active')
        .sort((a, b) => b.progressPercentage - a.progressPercentage)[0];

    if (!active) {
        emptyState.render(container, {
            icon: '🚀',
            title: t('student.continueEmpty'),
            text: t('student.continueEmptyHint'),
            action: { label: t('actions.explore'), href: '/pages/courses.html' }
        });
        return;
    }

    const card = document.createElement('div');
    card.className = 'continue-card';

    const thumb = document.createElement('div');
    thumb.className = 'continue-thumb';

    if (active.course.thumbnailUrl) {
        const image = document.createElement('img');
        image.src = active.course.thumbnailUrl;
        image.alt = '';
        image.addEventListener('error', () => image.remove());
        thumb.appendChild(image);
    }

    const body = document.createElement('div');

    const label = document.createElement('p');
    label.className = 'text-sm';
    label.style.opacity = '0.85';
    label.textContent = t('student.continueLearning');

    const title = document.createElement('h2');
    title.style.fontSize = 'var(--text-2xl)';
    title.className = 'mt-2';
    title.textContent = active.course.title;

    const progressLabel = document.createElement('p');
    progressLabel.className = 'text-sm mt-4';
    progressLabel.style.opacity = '0.85';
    progressLabel.textContent = `${t('course.progress')}: ${active.progressPercentage}%`;

    const progress = document.createElement('div');
    progress.className = 'continue-progress';
    const bar = document.createElement('div');
    bar.className = 'continue-progress-bar';
    bar.style.width = `${active.progressPercentage}%`;
    progress.appendChild(bar);

    const certificateHint = buildCertificateHint(active);

    const action = document.createElement('a');
    action.className = 'btn btn-primary mt-6';
    action.href = `/pages/lesson.html?courseId=${active.course.id}`;
    action.textContent = t('actions.continueLearning');

    body.append(label, title, progressLabel, progress, ...(certificateHint ? [certificateHint] : []), action);
    card.append(thumb, body);
    container.appendChild(card);
}

/**
 * Оценка «сколько уроков осталось» — прикидка по прогрессу и общему числу
 * уроков курса, без похода на сервер за точным списком пройденных. Реальное
 * условие выдачи сертификата (100% + все обязательные тесты) считает бэкенд —
 * здесь только мотивирующая подсказка, не гарантия.
 */
function buildCertificateHint(enrollment) {
    const lessonCount = enrollment.course.lessonCount;
    if (!lessonCount || enrollment.progressPercentage >= 100) {
        return null;
    }

    const remaining = Math.max(1, Math.ceil(lessonCount * (1 - enrollment.progressPercentage / 100)));

    const hint = document.createElement('p');
    hint.className = 'text-sm mt-2';
    hint.style.opacity = '0.85';
    hint.textContent = remaining <= 1
        ? t('student.almostCertificate')
        : t('student.lessonsUntilCertificate', { count: format.plural(remaining, 'units.lesson') });

    return hint;
}

function renderStats(enrollments, gradeBook, certificates) {
    const container = $('stats');
    container.innerHTML = '';

    const completed = enrollments.filter((enrollment) => enrollment.status === 'Completed').length;

    // Средний балл берём из Grade Book — его считает сервер по всем попыткам,
    // а не пересчитываем на клиенте по неполным данным.
    const average = gradeBook?.summary?.averageQuizScore ?? 0;

    container.append(
        buildStat(t('student.statEnrolled'), format.number(enrollments.length)),
        buildStat(t('student.statCompleted'), format.number(completed), { tone: 'success' }),
        buildStat(t('student.statCertificates'), format.number(certificates.length), { tone: 'success' }),
        buildStat(t('student.statAverage'), average > 0 ? `${format.rating(average)}%` : '—',
            { tone: average >= 60 ? 'success' : average > 0 ? 'warning' : 'neutral' })
    );
}

function renderRecent(enrollments) {
    const container = $('recent-courses');
    container.innerHTML = '';

    if (!enrollments.length) {
        emptyState.render(container, {
            icon: '📚',
            title: t('student.noCoursesTitle'),
            text: t('student.noCoursesHint'),
            action: { label: t('actions.explore'), href: '/pages/courses.html' }
        });
        return;
    }

    enrollments
        .slice(0, 3)
        .forEach((enrollment) => container.appendChild(buildEnrolledCard(enrollment)));
}

/**
 * Карточка курса из каталога плюс полоса прогресса.
 * Переиспользуем общий компонент, а не рисуем свою карточку: иначе курсы
 * на дашборде и в каталоге выглядели бы по-разному.
 */
export function buildEnrolledCard(enrollment) {
    const isCompleted = enrollment.status === 'Completed';

    const footer = document.createElement('div');
    footer.className = 'enrolled-progress';

    const label = document.createElement('div');
    label.className = 'enrolled-progress-label';

    const status = document.createElement('span');
    status.textContent = isCompleted ? t('student.completed') : t('student.inProgress');

    const percent = document.createElement('span');
    percent.textContent = `${enrollment.progressPercentage}%`;

    label.append(status, percent);

    const progress = document.createElement('div');
    progress.className = 'progress';
    const bar = document.createElement('div');
    bar.className = 'progress-bar';
    bar.style.width = `${enrollment.progressPercentage}%`;
    progress.appendChild(bar);

    const action = document.createElement('a');
    action.className = 'btn btn-primary btn-sm btn-block mt-4';
    action.href = `/pages/lesson.html?courseId=${enrollment.course.id}`;
    action.textContent = t(isCompleted ? 'actions.goToCourse' : 'actions.continueLearning');

    footer.append(label, progress, action);

    return createCourseCard(enrollment.course, { footer });
}

startStudentPage(load);
