/**
 * Боковое меню админ-панели — устроено так же, как instructor/student-sidebar:
 * одна разметка на все страницы, каждая пишет только
 * <div id="sidebar" data-active="dashboard"></div>.
 */

import { t, onLanguageChange } from '../localization.js';

const LINKS = [
    { id: 'dashboard',  key: 'nav.dashboard',   href: '/pages/admin/dashboard.html',  icon: '🏠' },
    { id: 'users',      key: 'nav.users',       href: '/pages/admin/users.html',      icon: '👥' },
    { id: 'courses',    key: 'nav.courses',     href: '/pages/admin/courses.html',    icon: '📚' },
    { id: 'categories', key: 'nav.categories',  href: '/pages/admin/categories.html', icon: '🏷️' },
    { id: 'hero-slides', key: 'nav.heroSlides', href: '/pages/admin/hero-slides.html', icon: '🖼️' }
];

export function renderAdminSidebar(container = document.getElementById('sidebar')) {
    if (!container) {
        return;
    }

    const activeId = container.dataset.active ?? '';

    container.innerHTML = '';
    container.className = 'sidebar sidebar-admin';

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
        onLanguageChange(() => renderAdminSidebar(container));
    }
}
