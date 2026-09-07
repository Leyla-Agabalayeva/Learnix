/**
 * Тёмная тема. Работает так же, как язык (localization.js): читает и
 * сохраняет выбор в localStorage, применяет его через атрибут на <html>.
 */

const STORAGE_KEY = 'lms.theme';
const listeners = [];

function getSystemPreference() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

export function getTheme() {
    return localStorage.getItem(STORAGE_KEY) ?? getSystemPreference();
}

export function setTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem(STORAGE_KEY, theme);
    listeners.forEach((listener) => listener(theme));
}

export function toggleTheme() {
    setTheme(getTheme() === 'dark' ? 'light' : 'dark');
}

/** Вызывается один раз при загрузке страницы — до первой отрисовки. */
export function initTheme() {
    document.documentElement.setAttribute('data-theme', getTheme());
}

export function onThemeChange(listener) {
    listeners.push(listener);
}
