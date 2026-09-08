/**
 * Навбар (раздел 55 ТЗ).
 *
 * Одна разметка на все страницы: каждая страница пишет только
 * <div id="navbar" data-active="courses"></div>, а содержимое собирается здесь.
 * Иначе шапку пришлось бы копировать в два десятка HTML-файлов и править
 * каждый раз во всех сразу.
 *
 * Что делает:
 *   - показывает разные пункты меню для гостя, студента и преподавателя;
 *   - подсвечивает текущий раздел;
 *   - переключатель языка AZ / EN / RU (раздел 19 ТЗ);
 *   - меню пользователя с выходом;
 *   - гамбургер на мобильных.
 */

import { t, onLanguageChange } from '../localization.js';
import { createLanguageSwitcher } from './language-switcher.js';
import { createThemeToggle } from './theme-toggle.js';
import { isAuthenticated, isAdmin, isInstructor, isStudent, getFullName, getInitials, getAvatarUrl, getUser, logout } from '../auth.js';
import * as cart from './cart.js';
import { buildNotificationsBell } from './notifications-bell.js';

/** Пункты меню для каждой роли: ключ перевода, адрес и id для подсветки. */
function getLinks() {
    if (isAdmin()) {
        return [
            { id: 'dashboard', key: 'nav.dashboard', href: '/pages/admin/dashboard.html' },
            { id: 'users', key: 'nav.users', href: '/pages/admin/users.html' },
            { id: 'admin-courses', key: 'nav.courses', href: '/pages/admin/courses.html' },
            { id: 'categories', key: 'nav.categories', href: '/pages/admin/categories.html' }
        ];
    }

    if (isInstructor()) {
        return [
            { id: 'dashboard', key: 'nav.dashboard', href: '/pages/instructor/dashboard.html' },
            { id: 'my-courses', key: 'nav.myCourses', href: '/pages/instructor/courses.html' },
            { id: 'analytics', key: 'nav.analytics', href: '/pages/instructor/analytics.html' },
            { id: 'courses', key: 'nav.courses', href: '/pages/courses.html' }
        ];
    }

    if (isStudent()) {
        return [
            { id: 'dashboard', key: 'nav.dashboard', href: '/pages/student/dashboard.html' },
            { id: 'my-courses', key: 'nav.myCourses', href: '/pages/student/my-courses.html' },
            { id: 'courses', key: 'nav.courses', href: '/pages/courses.html' },
            { id: 'grades', key: 'nav.gradeBook', href: '/pages/student/grades.html' }
        ];
    }

    return [
        { id: 'home', key: 'nav.home', href: '/' },
        { id: 'courses', key: 'nav.courses', href: '/pages/courses.html' },
        { id: 'verify', key: 'nav.verifyCertificate', href: '/pages/verify-certificate.html' }
    ];
}

/** Пункты выпадающего меню пользователя. */
function getUserMenuLinks() {
    if (isAdmin()) {
        return [];
    }

    if (isInstructor()) {
        return [
            { key: 'nav.createCourse', href: '/pages/instructor/create-course.html' },
            { key: 'nav.reviews', href: '/pages/instructor/reviews.html' },
            { key: 'nav.profile', href: '/pages/instructor/profile.html' }
        ];
    }

    return [
        { key: 'nav.wishlist', href: '/pages/student/wishlist.html' },
        { key: 'nav.certificates', href: '/pages/student/certificates.html' },
        { key: 'nav.profile', href: '/pages/student/profile.html' }
    ];
}

/**
 * Отрисовывает навбар в контейнер #navbar (или в переданный элемент).
 * data-active на контейнере задаёт, какой пункт подсветить.
 */
export function renderNavbar(container = document.getElementById('navbar')) {
    if (!container) {
        return;
    }

    const activeId = container.dataset.active ?? '';

    container.innerHTML = '';
    container.appendChild(build(activeId));

    // При смене языка перерисовываем шапку целиком: проще и надёжнее,
    // чем точечно обновлять десяток подписей.
    if (!container.dataset.bound) {
        container.dataset.bound = 'true';
        onLanguageChange(() => renderNavbar(container));

        // Страница профиля шлёт это событие после сохранения (см. profile.js) —
        // иначе новое имя или аватар появились бы в шапке только после
        // перезахода, хотя сама страница профиля уже показывает актуальные данные.
        document.addEventListener('lms:profile-updated', () => renderNavbar(container));
    }
}

function build(activeId) {
    const nav = document.createElement('nav');
    nav.className = 'navbar';
    nav.setAttribute('aria-label', t('nav.menu'));

    const inner = document.createElement('div');
    inner.className = 'container navbar-inner';

    inner.appendChild(buildBrand());

    const links = document.createElement('ul');
    links.className = 'navbar-links';
    links.id = 'navbar-links';

    getLinks().forEach((link) => {
        const item = document.createElement('li');
        const anchor = document.createElement('a');

        anchor.className = link.id === activeId ? 'navbar-link is-active' : 'navbar-link';
        anchor.href = link.href;
        anchor.textContent = t(link.key);

        if (link.id === activeId) {
            anchor.setAttribute('aria-current', 'page');
        }

        item.appendChild(anchor);
        links.appendChild(item);
    });

    inner.appendChild(links);

    const search = buildSearch();
    if (search) {
        inner.appendChild(search);
    }

    const actions = document.createElement('div');
    actions.className = 'navbar-actions';

    const cartLink = buildCartLink(activeId);
    if (cartLink) {
        actions.appendChild(cartLink);
    }

    if (isAuthenticated()) {
        actions.appendChild(buildNotificationsBell());
    }

    actions.append(createThemeToggle(), createLanguageSwitcher(), ...buildAuthArea());
    inner.appendChild(actions);

    inner.appendChild(buildToggle(links));

    nav.appendChild(inner);
    return nav;
}

/**
 * Поиск по каталогу прямо из шапки — как у Udemy, а не только на странице
 * каталога. Отправка ведёт на /pages/courses.html?search=…, где courses.js
 * уже умеет читать этот параметр из URL при загрузке (см. readStateFromUrl).
 * Админу искать курсы незачем — у него своя таблица со своим поиском.
 */
function buildSearch() {
    if (isAdmin()) {
        return null;
    }

    const form = document.createElement('form');
    form.className = 'navbar-search';
    form.setAttribute('role', 'search');

    const input = document.createElement('input');
    input.type = 'search';
    input.name = 'search';
    input.className = 'input';
    input.placeholder = t('nav.searchPlaceholder');
    input.setAttribute('aria-label', t('nav.searchPlaceholder'));

    form.appendChild(input);

    form.addEventListener('submit', (event) => {
        event.preventDefault();
        const term = input.value.trim();
        window.location.href = term
            ? `/pages/courses.html?search=${encodeURIComponent(term)}`
            : '/pages/courses.html';
    });

    return form;
}

/** Иконка корзины с бейджем — только там, где корзина вообще имеет смысл (см. cart.js). */
function buildCartLink(activeId) {
    if (isAdmin() || isInstructor()) {
        return null;
    }

    const link = document.createElement('a');
    link.className = activeId === 'cart' ? 'navbar-cart is-active' : 'navbar-cart';
    link.href = '/pages/cart.html';
    link.setAttribute('aria-label', t('cart.title'));

    const icon = document.createElement('span');
    icon.setAttribute('aria-hidden', 'true');
    icon.textContent = '🛒';
    link.appendChild(icon);

    const badge = document.createElement('span');
    badge.className = 'navbar-cart-badge hidden';
    link.appendChild(badge);

    const paint = (ids) => {
        const total = ids.length;
        badge.textContent = String(total);
        badge.classList.toggle('hidden', total === 0);
    };

    paint(cart.getIds());
    cart.onChange(paint);

    return link;
}

function buildBrand() {
    const brand = document.createElement('a');
    brand.className = 'navbar-brand';
    brand.href = '/';

    const logo = document.createElement('span');
    logo.className = 'navbar-logo';
    logo.setAttribute('aria-hidden', 'true');
    logo.textContent = 'L';

    const name = document.createElement('span');
    // Класс нужен, чтобы на очень узких экранах спрятать текст и оставить
    // только квадрат логотипа — иначе строка навбара не помещается.
    name.className = 'navbar-brand-name';
    name.textContent = t('app.name');

    brand.append(logo, name);
    return brand;
}

function buildAuthArea() {
    if (!isAuthenticated()) {
        const login = document.createElement('a');
        login.className = 'btn btn-ghost btn-sm';
        login.href = '/pages/login.html';
        login.textContent = t('auth.login');

        const register = document.createElement('a');
        register.className = 'btn btn-primary btn-sm';
        register.href = '/pages/register.html';
        register.textContent = t('auth.register');

        return [login, register];
    }

    return [buildUserMenu()];
}
/**
 * Аватар в шапке: фото, если оно указано в профиле, иначе инициалы —
 * тот же принцип, что и на странице профиля (renderAvatar в profile.js).
 * Битая ссылка не должна оставлять пустой кружок — падаем на инициалы.
 */
function buildAvatar() {
    const avatar = document.createElement('span');
    avatar.className = 'avatar';
    avatar.setAttribute('aria-hidden', 'true');

    const avatarUrl = getAvatarUrl();

    if (avatarUrl) {
        const image = document.createElement('img');
        image.src = avatarUrl;
        image.alt = '';
        image.addEventListener('error', () => {
            image.remove();
            avatar.textContent = getInitials();
        });
        avatar.appendChild(image);
        return avatar;
    }

    avatar.textContent = getInitials();
    return avatar;
}
function buildUserMenu() {
    const wrapper = document.createElement('div');
    wrapper.className = 'user-menu';

    const trigger = document.createElement('button');
    trigger.type = 'button';
    trigger.className = 'user-trigger';
    trigger.setAttribute('aria-haspopup', 'true');
    trigger.setAttribute('aria-expanded', 'false');

    //const avatar = document.createElement('span');
    //avatar.className = 'avatar';
    //avatar.setAttribute('aria-hidden', 'true');
    //avatar.textContent = getInitials();
    const avatar = buildAvatar();

    const name = document.createElement('span');
    name.className = 'user-name';
    name.textContent = getFullName();

    trigger.append(avatar, name);

    const dropdown = document.createElement('div');
    dropdown.className = 'dropdown hidden';

    const header = document.createElement('div');
    header.className = 'dropdown-header';
    header.innerHTML = '';

    const headerLabel = document.createElement('p');
    headerLabel.className = 'text-sm text-muted';
    headerLabel.textContent = t('auth.loggedInAs');

    const headerEmail = document.createElement('p');
    headerEmail.className = 'text-sm font-semibold';
    headerEmail.textContent = getUser()?.email ?? '';

    header.append(headerLabel, headerEmail);
    dropdown.appendChild(header);

    getUserMenuLinks().forEach((link) => {
        const anchor = document.createElement('a');
        anchor.className = 'dropdown-item';
        anchor.href = link.href;
        anchor.textContent = t(link.key);
        dropdown.appendChild(anchor);
    });

    const logoutButton = document.createElement('button');
    logoutButton.type = 'button';
    logoutButton.className = 'dropdown-item is-danger';
    logoutButton.textContent = t('auth.logout');
    logoutButton.addEventListener('click', () => logout());
    dropdown.appendChild(logoutButton);

    function toggle(open) {
        dropdown.classList.toggle('hidden', !open);
        trigger.setAttribute('aria-expanded', String(open));
    }

    trigger.addEventListener('click', (event) => {
        event.stopPropagation();
        toggle(dropdown.classList.contains('hidden'));
    });

    // Клик мимо и Escape закрывают меню — ожидаемое поведение выпадашки.
    document.addEventListener('click', () => toggle(false));
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            toggle(false);
        }
    });

    dropdown.addEventListener('click', (event) => event.stopPropagation());

    wrapper.append(trigger, dropdown);
    return wrapper;
}

function buildToggle(links) {
    const toggle = document.createElement('button');
    toggle.type = 'button';
    toggle.className = 'navbar-toggle';
    toggle.setAttribute('aria-label', t('nav.menu'));
    toggle.setAttribute('aria-expanded', 'false');
    toggle.setAttribute('aria-controls', 'navbar-links');
    toggle.innerHTML = '<span aria-hidden="true">☰</span>';

    const setOpen = (open) => {
        links.classList.toggle('is-open', open);
        toggle.setAttribute('aria-expanded', String(open));
    };

    toggle.addEventListener('click', (event) => {
        // Иначе слушатель на document (ниже) тут же закроет только что открытое меню.
        event.stopPropagation();
        setOpen(!links.classList.contains('is-open'));
    });

    // Escape закрывает и ВОЗВРАЩАЕТ ФОКУС на кнопку: иначе фокус остаётся
    // внутри спрятанного меню, и следующий Tab уводит в никуда.
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape' && links.classList.contains('is-open')) {
            setOpen(false);
            toggle.focus();
        }
    });

    // Клик мимо меню закрывает — так же, как у выпадающего меню аватара.
    // Без этого закрыть меню можно было только повторным нажатием на ☰.
    document.addEventListener('click', () => setOpen(false));
    links.addEventListener('click', (event) => event.stopPropagation());

    return toggle;
}
