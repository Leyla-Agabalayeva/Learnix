/**
 * Аутентификация на фронтенде (разделы 54 и 51 ТЗ).
 *
 * Хранит JWT и профиль пользователя, даёт проверки ролей и защиту страниц.
 *
 * Где хранится токен и почему именно там:
 *   localStorage переживает перезагрузку и закрытие вкладки, поэтому
 *   пользователю не приходится входить заново на каждой странице.
 *   Плата — уязвимость к XSS: скрипт, внедрённый на страницу, прочитает токен.
 *   Правильное решение для продакшена — httpOnly cookie, недоступная из JS,
 *   но это требует CSRF-защиты и переделки схемы аутентификации на бэкенде.
 *   Для учебного проекта осознанно выбран localStorage; защита от XSS
 *   обеспечивается тем, что пользовательский контент нигде не вставляется
 *   через innerHTML без экранирования.
 *
 * Ключевой принцип: всё, что здесь есть, — только для УДОБСТВА интерфейса.
 * Настоящая проверка прав живёт на сервере. Спрятанная кнопка ничего не
 * защищает — эндпоинт всё равно вернёт 403, если роль не та.
 */

import { api } from './api.js';

const TOKEN_KEY = 'lms.token';
const USER_KEY = 'lms.user';

export const ROLES = {
    ADMIN: 'Admin',
    INSTRUCTOR: 'Instructor',
    STUDENT: 'Student'
};

// ---------------------------------------------------------------------------
// Хранилище сессии
// ---------------------------------------------------------------------------

export function getToken() {
    return localStorage.getItem(TOKEN_KEY);
}

/** Профиль текущего пользователя или null. */
export function getUser() {
    const raw = localStorage.getItem(USER_KEY);

    if (!raw) {
        return null;
    }

    try {
        return JSON.parse(raw);
    } catch {
        // Испорченное значение чистим, чтобы не падать на каждой странице.
        localStorage.removeItem(USER_KEY);
        return null;
    }
}

export function isAuthenticated() {
    return Boolean(getToken());
}

export function hasRole(role) {
    const user = getUser();
    return Boolean(user?.roles?.includes(role));
}

export function isStudent() {
    return hasRole(ROLES.STUDENT);
}

export function isInstructor() {
    return hasRole(ROLES.INSTRUCTOR);
}

export function isAdmin() {
    return hasRole(ROLES.ADMIN);
}

/** Инициалы для аватара: «Leyla Ağabalayeva» → «LA». */
export function getInitials() {
    const user = getUser();

    if (!user) {
        return '?';
    }

    return `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase() || '?';
}

export function getFullName() {
    const user = getUser();
    return user ? `${user.firstName} ${user.lastName}`.trim() : '';
}

/** Ссылка на аватар из кэшированной сессии, или null, если его нет / не загружен. */
export function getAvatarUrl() {
    const user = getUser();
    return user?.avatarUrl || null;
}

// ---------------------------------------------------------------------------
// Вход, регистрация, выход
// ---------------------------------------------------------------------------

/**
 * @param {string} email
 * @param {string} password
 * @returns {Promise<object>} профиль вошедшего пользователя
 */
export async function login(email, password) {
    const data = await api.post('/auth/login', { email, password }, { anonymous: true });
    saveSession(data);
    return getUser();
}

/**
 * @param {{ email: string, password: string, firstName: string, lastName: string, role: string }} form
 */
export async function register(form) {
    // API возвращает готовый токен — отдельный вход после регистрации не нужен.
    const data = await api.post('/auth/register', form, { anonymous: true });
    saveSession(data);
    return getUser();
}

/**
 * Выход. Эндпоинт на сервере дёргается для порядка (и как точка для будущего
 * отзыва refresh-токена), но результат не важен: JWT всё равно живёт на клиенте,
 * и главное — стереть его локально.
 */
export async function logout({ redirect = '/' } = {}) {
    try {
        if (isAuthenticated()) {
            await api.post('/auth/logout');
        }
    } catch {
        // Сервер недоступен — выйти всё равно обязаны.
    } finally {
        clearSession();
        window.location.href = redirect;
    }
}

/** Сохраняет ответ AuthResponse: токен отдельно, профиль отдельно. */
export function saveSession(authResponse) {
    localStorage.setItem(TOKEN_KEY, authResponse.token);
    localStorage.setItem(USER_KEY, JSON.stringify({
        userId: authResponse.userId,
        email: authResponse.email,
        firstName: authResponse.firstName,
        lastName: authResponse.lastName,
        roles: authResponse.roles ?? [],
        avatarUrl: authResponse.avatarUrl ?? null,
        expiresAt: authResponse.expiresAt
    }));

}

export function clearSession() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
}

/**
 * Вызывается из api.js при любом ответе 401.
 * Уводит на страницу входа и запоминает, куда пользователь шёл, чтобы вернуть
 * его туда же после входа.
 */
export function handleUnauthorized() {
    if (window.location.pathname.startsWith('/pages/login')) {
        return; // уже на странице входа — незачем зацикливаться
    }

    clearSession();

    const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
    window.location.href = `/pages/login.html?returnUrl=${returnUrl}&expired=1`;
}

// ---------------------------------------------------------------------------
// Защита страниц
// ---------------------------------------------------------------------------

/**
 * Ставится в начале скрипта защищённой страницы.
 * Возвращает false, если пользователя уже уводят на другую страницу, —
 * тогда остальной код инициализации выполнять не нужно.
 *
 * Ещё раз: это удобство, а не безопасность. Данные защищает сервер.
 *
 * @param {string} [role] требуемая роль
 */
export function requireAuth(role) {
    if (!isAuthenticated()) {
        handleUnauthorized();
        return false;
    }

    if (role && !hasRole(role)) {
        window.location.href = '/pages/forbidden.html';
        return false;
    }

    return true;
}

/** Куда вести пользователя после входа — зависит от роли (раздел F1 ТЗ). */
export function getHomeUrlForRole() {
    if (isAdmin()) {
        return '/pages/admin/dashboard.html';
    }

    if (isInstructor()) {
        return '/pages/instructor/dashboard.html';
    }

    if (isStudent()) {
        return '/pages/student/dashboard.html';
    }

    return '/';
}
