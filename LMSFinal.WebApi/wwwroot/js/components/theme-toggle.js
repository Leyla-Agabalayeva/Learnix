/**
 * Кнопка переключения светлой/тёмной темы. По структуре — как
 * language-switcher.js: сама следит за состоянием и перекрашивается.
 */

import { getTheme, toggleTheme, onThemeChange } from '../theme.js';

export function createThemeToggle() {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'theme-toggle';
    button.setAttribute('aria-label', 'Переключить тему');

    const paint = () => {
        const isDark = getTheme() === 'dark';
        button.textContent = isDark ? '☀️' : '🌙';
        button.setAttribute('aria-pressed', String(isDark));
    };

    button.addEventListener('click', toggleTheme);

    paint();
    onThemeChange(paint);

    return button;
}
