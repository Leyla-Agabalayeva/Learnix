
import { api } from '../../api.js';
import { getApiLanguage, t } from '../../localization.js';
import { loader } from '../../components/loader.js';
import { emptyState } from '../../components/empty-state.js';
import { createCourseCard } from '../../components/course-card.js';
import { startInstructorPage, showLoadError, buildStatusBadge } from './common.js';

const $ = (id) => document.getElementById(id);

let courses = [];
let activeStatus = '';

async function load() {
    loader.skeleton($('course-grid'), { count: 6 });

    try {
        courses = await api.get('/courses/my', { query: { lang: getApiLanguage() } });
        render();
    } catch (error) {
        showLoadError($('course-grid'), error, load);
    }
}

function render() {
    const container = $('course-grid');
    container.innerHTML = '';

    const visible = activeStatus
        ? courses.filter((course) => course.status === activeStatus)
        : courses;

    if (!visible.length) {
        emptyState.render(container, {
            icon: '📚',
            title: t('instructor.noCoursesTitle'),
            text: t('instructor.noCoursesHint'),
            action: { label: t('nav.createCourse'), href: '/pages/instructor/create-course.html' }
        });
        return;
    }

    visible.forEach((course) => container.appendChild(buildInstructorCourseCard(course)));
}

/**
 * Карточка курса для преподавателя.
 *
 * Переиспользует общий компонент каталога, меняя только действия: вместо
 * «Смотреть курс» — «Редактировать» и «Аналитика». Статус выводится бейджем,
 * потому что для автора разница между черновиком и публикацией — главное.
 */
export function buildInstructorCourseCard(course) {
    const footer = document.createElement('div');
    footer.className = 'instructor-course-actions';

    const edit = document.createElement('a');
    edit.className = 'btn btn-secondary btn-sm';
    edit.href = `/pages/instructor/course-builder.html?id=${course.id}`;
    edit.textContent = t('actions.edit');

    const analytics = document.createElement('a');
    analytics.className = 'btn btn-primary btn-sm';
    analytics.href = `/pages/instructor/analytics.html?courseId=${course.id}`;
    analytics.textContent = t('nav.analytics');

    footer.append(edit, analytics);

    return createCourseCard(course, {
        href: `/pages/course-details.html?id=${course.id}`,
        showStatus: true,
        footer
    });
}

function bindTabs() {
    $('status-tabs').addEventListener('click', (event) => {
        const tab = event.target.closest('.tab');

        if (!tab) {
            return;
        }

        activeStatus = tab.dataset.status;

        $('status-tabs').querySelectorAll('.tab').forEach((item) => {
            const isActive = item === tab;
            item.classList.toggle('is-active', isActive);
            item.setAttribute('aria-selected', String(isActive));
        });

        render();
    });
}

// Файл используется и дашбордом (импортирует buildInstructorCourseCard),
// поэтому запуск страницы — только если на ней есть сетка курсов с вкладками.
if (document.getElementById('status-tabs')) {
    startInstructorPage(load, { init: bindTabs });
}

// buildStatusBadge переэкспортируем: страницы студентов и аналитики
// показывают тот же бейдж статуса.
export { buildStatusBadge };
