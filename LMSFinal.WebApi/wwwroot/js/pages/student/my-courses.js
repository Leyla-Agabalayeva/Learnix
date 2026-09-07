/**
 * «Мои курсы» — все записи студента с фильтром по статусу.
 *
 * Фильтрация здесь клиентская, и это осознанно: список курсов одного студента
 * — это единицы записей, они уже пришли одним запросом. Городить ради них
 * серверную фильтрацию с пагинацией (как в каталоге) было бы лишним.
 */

import { api } from '../../api.js';
import { getApiLanguage, t } from '../../localization.js';
import { loader } from '../../components/loader.js';
import { emptyState } from '../../components/empty-state.js';
import { startStudentPage, showLoadError } from './common.js';
import { buildEnrolledCard } from './dashboard.js';

const $ = (id) => document.getElementById(id);

let enrollments = [];
let activeStatus = '';

async function load() {
    loader.skeleton($('course-grid'), { count: 6 });

    try {
        enrollments = await api.get('/enrollments/my', { query: { lang: getApiLanguage() } });
        render();
    } catch (error) {
        showLoadError($('course-grid'), error, load);
    }
}

function render() {
    const container = $('course-grid');
    container.innerHTML = '';

    const visible = activeStatus
        ? enrollments.filter((enrollment) => enrollment.status === activeStatus)
        : enrollments;

    if (!visible.length) {
        emptyState.render(container, {
            icon: '📚',
            title: t('student.noCoursesTitle'),
            text: t('student.noCoursesHint'),
            action: { label: t('actions.explore'), href: '/pages/courses.html' }
        });
        return;
    }

    visible.forEach((enrollment) => container.appendChild(buildEnrolledCard(enrollment)));
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

// bindTabs — в init: он должен выполниться один раз, а load повторяется
// при каждой смене языка.
startStudentPage(load, { init: bindTabs });
