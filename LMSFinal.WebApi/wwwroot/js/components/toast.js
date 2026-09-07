/**
 * Тост-уведомления (раздел 57 ТЗ).
 *
 *   toast.success('Курс создан');
 *   toast.error(apiError.message);
 *
 * Контейнер создаётся лениво при первом вызове, чтобы каждая страница не
 * повторяла один и тот же <div> в разметке.
 *
 * Доступность: контейнер помечен role="status" + aria-live="polite", поэтому
 * скринридер зачитывает появившийся текст, не прерывая пользователя.
 */

import { t } from '../localization.js';

const DEFAULT_DURATION = 4000;

const ICONS = {
    success: '✓',
    error: '✕',
    warning: '!',
    info: 'i'
};

let container = null;

function ensureContainer() {
    if (container && document.body.contains(container)) {
        return container;
    }

    container = document.createElement('div');
    container.className = 'toast-container';
    container.setAttribute('role', 'status');
    container.setAttribute('aria-live', 'polite');
    document.body.appendChild(container);

    return container;
}

/**
 * @param {'success'|'error'|'warning'|'info'} type
 * @param {string} message
 * @param {{ title?: string, duration?: number }} [options]
 */
function show(type, message, options = {}) {
    const { title, duration = DEFAULT_DURATION } = options;

    const element = document.createElement('div');
    element.className = `toast toast-${type}`;

    const icon = document.createElement('span');
    icon.className = 'toast-icon';
    icon.setAttribute('aria-hidden', 'true');
    icon.textContent = ICONS[type] ?? ICONS.info;

    const content = document.createElement('div');
    content.className = 'toast-content';

    if (title) {
        const titleElement = document.createElement('p');
        titleElement.className = 'toast-title';
        titleElement.textContent = title;
        content.appendChild(titleElement);
    }

    const messageElement = document.createElement('p');
    messageElement.className = title ? 'toast-message' : 'toast-title';
    // textContent, а не innerHTML: в сообщение может попасть текст от сервера
    // или введённый пользователем — вставлять его как разметку нельзя.
    messageElement.textContent = message;
    content.appendChild(messageElement);

    const closeButton = document.createElement('button');
    closeButton.className = 'toast-close';
    closeButton.type = 'button';
    closeButton.setAttribute('aria-label', t('actions.close'));
    closeButton.textContent = '×';
    closeButton.addEventListener('click', () => dismiss(element));

    element.append(icon, content, closeButton);
    ensureContainer().appendChild(element);

    if (duration > 0) {
        setTimeout(() => dismiss(element), duration);
    }

    return element;
}

function dismiss(element) {
    if (!element.isConnected || element.classList.contains('is-leaving')) {
        return;
    }

    element.classList.add('is-leaving');

    // Ждём окончания анимации ухода. Если анимации отключены системной
    // настройкой prefers-reduced-motion, событие всё равно придёт мгновенно.
    element.addEventListener('animationend', () => element.remove(), { once: true });

    // Страховка на случай, если animationend не сработает.
    setTimeout(() => element.remove(), 400);
}

export const toast = {
    success: (message, options) => show('success', message, options),
    error: (message, options) => show('error', message, options),
    warning: (message, options) => show('warning', message, options),
    info: (message, options) => show('info', message, options),

    /**
     * Готовый обработчик ошибки от API: сам подбирает текст для сетевой ошибки.
     * @param {import('../api.js').ApiError} error
     */
    fromApiError(error) {
        const message = error?.isNetworkError ? t('states.networkError') : error?.message;
        return show('error', message || t('states.error'));
    },

    dismissAll() {
        container?.querySelectorAll('.toast').forEach(dismiss);
    }
};
