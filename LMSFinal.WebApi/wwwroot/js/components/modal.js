/**
 * Модальные окна и диалог подтверждения (раздел 57 ТЗ).
 *
 *   const dialog = modal.open({ title: 'Редактировать', content: form });
 *   dialog.close();
 *
 *   if (await modal.confirm({ title: 'Удалить курс?' })) { ... }
 *
 * Что здесь сделано ради доступности, а не «для галочки»:
 *   - role="dialog" + aria-modal + aria-labelledby;
 *   - фокус переводится внутрь окна, Tab не выпускает его наружу (focus trap);
 *   - Escape закрывает;
 *   - клик по подложке закрывает;
 *   - после закрытия фокус возвращается на кнопку, которая открыла окно, —
 *     иначе пользователь с клавиатуры оказывается в начале страницы.
 */

import { t, applyTranslations } from '../localization.js';

const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

let openCount = 0;

/**
 * @param {{
 *   title?: string,
 *   content?: string|Node,
 *   footer?: Node|null,
 *   size?: 'md'|'lg',
 *   closable?: boolean,
 *   onClose?: () => void
 * }} options
 */
function open(options = {}) {
    const {
        title = '',
        content = '',
        footer = null,
        size = 'md',
        closable = true,
        onClose
    } = options;

    const previouslyFocused = document.activeElement;

    const backdrop = document.createElement('div');
    backdrop.className = 'modal-backdrop';

    const dialog = document.createElement('div');
    dialog.className = size === 'lg' ? 'modal modal-lg' : 'modal';
    dialog.setAttribute('role', 'dialog');
    dialog.setAttribute('aria-modal', 'true');

    if (title) {
        const titleId = `modal-title-${Date.now()}`;
        const header = document.createElement('div');
        header.className = 'modal-header';

        const heading = document.createElement('h2');
        heading.className = 'modal-title';
        heading.id = titleId;
        heading.textContent = title;
        header.appendChild(heading);

        dialog.setAttribute('aria-labelledby', titleId);

        if (closable) {
            const closeButton = document.createElement('button');
            closeButton.type = 'button';
            closeButton.className = 'modal-close';
            closeButton.setAttribute('aria-label', t('actions.close'));
            closeButton.textContent = '×';
            closeButton.addEventListener('click', () => close());
            header.appendChild(closeButton);
        }

        dialog.appendChild(header);
    }

    const body = document.createElement('div');
    body.className = 'modal-body';

    if (typeof content === 'string') {
        const paragraph = document.createElement('p');
        paragraph.textContent = content;
        body.appendChild(paragraph);
    } else if (content instanceof Node) {
        body.appendChild(content);
    }

    dialog.appendChild(body);

    if (footer) {
        const footerElement = document.createElement('div');
        footerElement.className = 'modal-footer';
        footerElement.appendChild(footer);
        dialog.appendChild(footerElement);
    }

    backdrop.appendChild(dialog);
    document.body.appendChild(backdrop);

    // Фон не должен скроллиться под открытым окном.
    openCount += 1;
    document.body.style.overflow = 'hidden';

    applyTranslations(dialog);

    // Фокус — на первый интерактивный элемент, иначе на само окно.
    const firstFocusable = dialog.querySelector(FOCUSABLE);
    if (firstFocusable) {
        firstFocusable.focus();
    } else {
        dialog.tabIndex = -1;
        dialog.focus();
    }

    function onKeyDown(event) {
        if (event.key === 'Escape' && closable) {
            event.preventDefault();
            close();
            return;
        }

        if (event.key !== 'Tab') {
            return;
        }

        // Focus trap: замыкаем Tab в пределах окна.
        const focusable = [...dialog.querySelectorAll(FOCUSABLE)];
        if (focusable.length === 0) {
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];

        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    }

    function onBackdropClick(event) {
        // Именно target === backdrop: клик внутри окна не должен его закрывать.
        if (event.target === backdrop && closable) {
            close();
        }
    }

    document.addEventListener('keydown', onKeyDown);
    backdrop.addEventListener('mousedown', onBackdropClick);

    function close() {
        if (!backdrop.isConnected) {
            return;
        }

        document.removeEventListener('keydown', onKeyDown);
        backdrop.remove();

        openCount = Math.max(0, openCount - 1);
        if (openCount === 0) {
            document.body.style.overflow = '';
        }

        // Возвращаем фокус туда, откуда окно открыли.
        if (previouslyFocused instanceof HTMLElement) {
            previouslyFocused.focus();
        }

        onClose?.();
    }

    return { element: dialog, body, close };
}

/**
 * Диалог подтверждения для необратимых действий (раздел 57 ТЗ).
 * Возвращает Promise<boolean>, поэтому в коде читается линейно:
 *
 *   if (!await modal.confirm({ title: '...' })) return;
 */
function confirm(options = {}) {
    const {
        title = t('actions.confirm'),
        message = '',
        confirmText = t('actions.confirm'),
        cancelText = t('actions.cancel'),
        danger = false
    } = options;

    return new Promise((resolve) => {
        let settled = false;

        const footer = document.createDocumentFragment();

        const cancelButton = document.createElement('button');
        cancelButton.type = 'button';
        cancelButton.className = 'btn btn-secondary';
        cancelButton.textContent = cancelText;

        const confirmButton = document.createElement('button');
        confirmButton.type = 'button';
        confirmButton.className = danger ? 'btn btn-danger' : 'btn btn-primary';
        confirmButton.textContent = confirmText;

        footer.append(cancelButton, confirmButton);

        const dialog = open({
            title,
            content: message,
            footer,
            // Закрытие крестиком/Escape/подложкой считается отказом.
            onClose: () => {
                if (!settled) {
                    settled = true;
                    resolve(false);
                }
            }
        });

        cancelButton.addEventListener('click', () => dialog.close());

        confirmButton.addEventListener('click', () => {
            settled = true;
            dialog.close();
            resolve(true);
        });

        // Фокус на «Отмена»: случайный Enter не должен удалять курс.
        cancelButton.focus();
    });
}

export const modal = { open, confirm };
