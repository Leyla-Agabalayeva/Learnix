/**
 * Пустые состояния и состояния ошибки (раздел 31 ТЗ).
 *
 *   emptyState.render(grid, {
 *       icon: '📚',
 *       title: t('states.noCourses'),
 *       text: t('states.noCoursesHint'),
 *       action: { label: t('actions.explore'), href: '/pages/courses.html' }
 *   });
 *
 * Почему это отдельный компонент, а не «просто div с текстом»: пустой экран —
 * самая частая ситуация в новом аккаунте, и именно по ней судят о качестве
 * интерфейса. Пустое состояние должно объяснять, что произошло, и давать
 * следующий шаг, а не показывать белое поле.
 */

import { t } from '../localization.js';

/**
 * @param {Element} container
 * @param {{
 *   icon?: string,
 *   title?: string,
 *   text?: string,
 *   action?: { label: string, href?: string, onClick?: () => void }
 * }} [options]
 */
function render(container, options = {}) {
    if (!container) {
        return;
    }

    container.innerHTML = '';
    container.appendChild(create(options));
}

function create(options = {}) {
    const {
        icon = '📭',
        title = t('states.empty'),
        text = '',
        action = null
    } = options;

    const wrapper = document.createElement('div');
    wrapper.className = 'empty-state';

    const iconElement = document.createElement('div');
    iconElement.className = 'empty-state-icon';
    iconElement.setAttribute('aria-hidden', 'true');
    iconElement.textContent = icon;

    const titleElement = document.createElement('p');
    titleElement.className = 'empty-state-title';
    titleElement.textContent = title;

    wrapper.append(iconElement, titleElement);

    if (text) {
        const textElement = document.createElement('p');
        textElement.className = 'empty-state-text';
        textElement.textContent = text;
        wrapper.appendChild(textElement);
    }

    if (action) {
        const actionElement = action.href
            ? document.createElement('a')
            : document.createElement('button');

        actionElement.className = 'btn btn-primary mt-2';
        actionElement.textContent = action.label;

        if (action.href) {
            actionElement.href = action.href;
        } else {
            actionElement.type = 'button';
            actionElement.addEventListener('click', action.onClick);
        }

        wrapper.appendChild(actionElement);
    }

    return wrapper;
}

/**
 * Состояние ошибки с кнопкой «Повторить».
 * Отличается от пустого состояния тем, что здесь виновата не пустота данных,
 * а сбой — и пользователю нужно предложить действие, а не объяснение.
 *
 * @param {Element} container
 * @param {{ message?: string, onRetry?: () => void }} [options]
 */
function error(container, options = {}) {
    const { message, onRetry } = options;

    render(container, {
        icon: '⚠️',
        title: t('states.error'),
        text: message || t('states.errorHint'),
        action: onRetry ? { label: t('actions.retry'), onClick: onRetry } : null
    });
}

export const emptyState = { render, create, error };
