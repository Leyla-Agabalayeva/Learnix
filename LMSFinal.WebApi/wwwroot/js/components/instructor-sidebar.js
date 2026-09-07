/**
 * Боковое меню кабинета преподавателя (раздел 27 ТЗ).
 *
 * Устроено так же, как student-sidebar: одна разметка на шесть страниц,
 * каждая пишет только <div id="sidebar" data-active="analytics"></div>.
 */

import { t, onLanguageChange } from '../localization.js';

const LINKS = [
    { id: 'dashboard',  key: 'nav.dashboard',    href: '/pages/instructor/dashboard.html',  icon: '🏠' },
    { id: 'my-courses', key: 'nav.myCourses',    href: '/pages/instructor/courses.html',    icon: '📚' },
    { id: 'analytics',  key: 'nav.analytics',    href: '/pages/instructor/analytics.html',  icon: '📈' },
    { id: 'students',   key: 'nav.students',     href: '/pages/instructor/students.html',   icon: '👥' },
    { id: 'reviews',    key: 'nav.reviews',      href: '/pages/instructor/reviews.html',    icon: '💬' },
    { id: 'profile',    key: 'nav.profile',      href: '/pages/instructor/profile.html',    icon: '👤' }
];

export function renderInstructorSidebar(container = document.getElementById('sidebar')) {
    if (!container) {
        return;
    }

    const activeId = container.dataset.active ?? '';

    container.innerHTML = '';
    container.className = 'sidebar';

    LINKS.forEach((link) => {
        const anchor = document.createElement('a');
        anchor.className = link.id === activeId ? 'sidebar-link is-active' : 'sidebar-link';
        anchor.href = link.href;

        if (link.id === activeId) {
            anchor.setAttribute('aria-current', 'page');
        }

        const icon = document.createElement('span');
        icon.setAttribute('aria-hidden', 'true');
        icon.textContent = link.icon;

        const label = document.createElement('span');
        label.textContent = t(link.key);

        anchor.append(icon, label);
        container.appendChild(anchor);
    });

    if (!container.dataset.bound) {
        container.dataset.bound = 'true';
        onLanguageChange(() => renderInstructorSidebar(container));
    }
}
