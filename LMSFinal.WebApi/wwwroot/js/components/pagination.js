/**
 * Пагинация (раздел 18.7 ТЗ).
 *
 * Показывает не все страницы подряд, а окно вокруг текущей с многоточиями:
 *
 *   ‹  1 … 4 [5] 6 … 20  ›
 *
 * При двадцати страницах полный список кнопок не помещается на телефоне и
 * превращает низ страницы в кашу.
 */

import { t } from '../localization.js';

const WINDOW_SIZE = 1;   // сколько соседей показывать слева и справа от текущей

/**
 * @param {Element} container
 * @param {{ page: number, totalPages: number, onChange: (page: number) => void }} options
 */
export function renderPagination(container, { page, totalPages, onChange }) {
    if (!container) {
        return;
    }

    container.innerHTML = '';

    // Одна страница — пагинация только зашумляет интерфейс.
    if (totalPages <= 1) {
        return;
    }

    const nav = document.createElement('nav');
    nav.className = 'pagination';
    nav.setAttribute('aria-label', t('catalog.page', { current: page, total: totalPages }));

    nav.appendChild(buildArrow('‹', t('actions.previous'), page > 1, () => onChange(page - 1)));

    buildPageNumbers(page, totalPages).forEach((item) => {
        nav.appendChild(item === '…' ? buildEllipsis() : buildPageButton(item, item === page, onChange));
    });

    nav.appendChild(buildArrow('›', t('actions.next'), page < totalPages, () => onChange(page + 1)));

    container.appendChild(nav);
}

/** Возвращает массив вида [1, '…', 4, 5, 6, '…', 20]. */
function buildPageNumbers(page, totalPages) {
    const pages = new Set([1, totalPages]);

    for (let offset = -WINDOW_SIZE; offset <= WINDOW_SIZE; offset += 1) {
        const candidate = page + offset;
        if (candidate >= 1 && candidate <= totalPages) {
            pages.add(candidate);
        }
    }

    const sorted = [...pages].sort((a, b) => a - b);
    const result = [];

    sorted.forEach((value, index) => {
        // Разрыв больше единицы — значит между кнопками есть пропущенные страницы.
        if (index > 0 && value - sorted[index - 1] > 1) {
            result.push('…');
        }
        result.push(value);
    });

    return result;
}

function buildPageButton(pageNumber, isActive, onChange) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = isActive ? 'pagination-item is-active' : 'pagination-item';
    button.textContent = String(pageNumber);

    if (isActive) {
        button.setAttribute('aria-current', 'page');
    } else {
        button.addEventListener('click', () => onChange(pageNumber));
    }

    return button;
}

function buildArrow(symbol, label, enabled, onClick) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'pagination-item';
    button.textContent = symbol;
    button.setAttribute('aria-label', label);
    button.disabled = !enabled;

    if (enabled) {
        button.addEventListener('click', onClick);
    }

    return button;
}

function buildEllipsis() {
    const span = document.createElement('span');
    span.className = 'pagination-item';
    span.style.border = 'none';
    span.style.background = 'none';
    span.style.cursor = 'default';
    span.setAttribute('aria-hidden', 'true');
    span.textContent = '…';
    return span;
}
