/**
 * Боковое меню личного кабинета студента (раздел 26 ТЗ).
 *
 * Одна разметка на шесть страниц: каждая пишет только
 * <div id="sidebar" data-active="grades"></div>.
 * Иначе меню пришлось бы копировать в шесть HTML-файлов и править везде сразу,
 * когда появится седьмой пункт.
 */

import { t, onLanguageChange } from '../localization.js';

const LINKS = [
    { id: 'dashboard',    key: 'nav.dashboard',    href: '/pages/student/dashboard.html',    icon: '🏠' },
    { id: 'my-courses',   key: 'nav.myCourses',    href: '/pages/student/my-courses.html',   icon: '📚' },
    { id: 'wishlist',     key: 'nav.wishlist',     href: '/pages/student/wishlist.html',     icon: '♡' },
    { id: 'grades',       key: 'nav.gradeBook',    href: '/pages/student/grades.html',       icon: '📊' },
    { id: 'certificates', key: 'nav.certificates', href: '/pages/student/certificates.html', icon: '🏆' },
    { id: 'profile',      key: 'nav.profile',      href: '/pages/student/profile.html',      icon: '👤' }
];

export function renderStudentSidebar(container = document.getElementById('sidebar')) {
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
        onLanguageChange(() => renderStudentSidebar(container));
    }
}
