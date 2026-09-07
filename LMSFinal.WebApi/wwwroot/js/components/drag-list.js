/**
 * Перетаскивание элементов списка (раздел 6 и 28 ТЗ).
 *
 * Используется дважды: для модулей курса и для уроков внутри модуля.
 *
 * Главное решение — **порядок меняется не только мышью**. Нативный HTML5
 * drag & drop работает исключительно указателем: с клавиатуры перетащить
 * элемент невозможно, а на сенсорном экране события drag браузеры генерируют
 * непоследовательно. Поэтому у каждой ручки ☰ есть равноценная клавиатурная
 * альтернатива: она получает фокус, и Ctrl+↑ / Ctrl+↓ двигают элемент.
 *
 * Порядок сохраняется на сервере одним запросом на весь список
 * (PUT /api/modules/reorder), а не по одному элементу: так порядок остаётся
 * согласованным, даже если пользователь быстро перетащил несколько подряд.
 */

const DRAGGING_CLASS = 'is-dragging';
const DROP_TARGET_CLASS = 'is-drop-target';

/**
 * @param {HTMLElement} container контейнер списка
 * @param {{
 *   itemSelector: string,
 *   handleSelector: string,
 *   onReorder: (orderedIds: string[]) => void | Promise<void>
 * }} options
 */
export function makeSortable(container, { itemSelector, handleSelector, onReorder }) {
    if (!container) {
        return;
    }

    let dragged = null;

    // --- Мышь -------------------------------------------------------------

    container.addEventListener('dragstart', (event) => {
        const item = event.target.closest(itemSelector);

        if (!item || !container.contains(item)) {
            return;
        }

        dragged = item;
        item.classList.add(DRAGGING_CLASS);

        // Без setData Firefox не начинает перетаскивание вообще.
        event.dataTransfer.effectAllowed = 'move';
        event.dataTransfer.setData('text/plain', item.dataset.id ?? '');
    });

    container.addEventListener('dragend', () => {
        dragged?.classList.remove(DRAGGING_CLASS);
        clearDropTargets(container, itemSelector);
        dragged = null;
    });

    container.addEventListener('dragover', (event) => {
        if (!dragged) {
            return;
        }

        // preventDefault обязателен: без него drop вообще не сработает.
        event.preventDefault();
        event.dataTransfer.dropEffect = 'move';

        const after = findItemAfter(container, itemSelector, event.clientY);

        clearDropTargets(container, itemSelector);

        if (after) {
            after.classList.add(DROP_TARGET_CLASS);
            container.insertBefore(dragged, after);
        } else {
            container.appendChild(dragged);
        }
    });

    container.addEventListener('drop', (event) => {
        if (!dragged) {
            return;
        }

        event.preventDefault();
        clearDropTargets(container, itemSelector);
        commit(container, itemSelector, onReorder);
    });

    // --- Клавиатура --------------------------------------------------------

    container.addEventListener('keydown', (event) => {
        const handle = event.target.closest(handleSelector);

        if (!handle) {
            return;
        }

        // Ctrl (или Cmd) + стрелка: обычные стрелки оставляем браузеру
        // для прокрутки и навигации.
        if (!(event.ctrlKey || event.metaKey) || !['ArrowUp', 'ArrowDown'].includes(event.key)) {
            return;
        }

        event.preventDefault();

        const item = handle.closest(itemSelector);
        const moved = event.key === 'ArrowUp'
            ? moveUp(container, item)
            : moveDown(container, item);

        if (moved) {
            // Фокус остаётся на ручке перемещённого элемента — иначе после
            // первого нажатия он терялся бы и продолжить было бы нечем.
            handle.focus();
            commit(container, itemSelector, onReorder);
        }
    });
}

function moveUp(container, item) {
    const previous = item.previousElementSibling;

    if (!previous) {
        return false;
    }

    container.insertBefore(item, previous);
    return true;
}

function moveDown(container, item) {
    const next = item.nextElementSibling;

    if (!next) {
        return false;
    }

    container.insertBefore(next, item);
    return true;
}

/**
 * Находит элемент, ПЕРЕД которым нужно вставить перетаскиваемый.
 * Сравнение идёт по вертикальному центру каждого элемента: как только курсор
 * оказался выше центра — вставляем перед ним.
 */
function findItemAfter(container, itemSelector, pointerY) {
    const items = [...container.querySelectorAll(itemSelector)]
        .filter((item) => !item.classList.contains(DRAGGING_CLASS));

    return items.find((item) => {
        const box = item.getBoundingClientRect();
        return pointerY < box.top + box.height / 2;
    }) ?? null;
}

function clearDropTargets(container, itemSelector) {
    container.querySelectorAll(itemSelector).forEach((item) => item.classList.remove(DROP_TARGET_CLASS));
}

/** Сообщает новый порядок наружу — вызывающий код сохраняет его на сервере. */
function commit(container, itemSelector, onReorder) {
    const orderedIds = [...container.querySelectorAll(itemSelector)]
        .map((item) => item.dataset.id)
        .filter(Boolean);

    onReorder(orderedIds);
}

/**
 * Ручка перетаскивания.
 *
 * Это <button>, а не <div>: кнопка сама попадает в порядок обхода Tab
 * и объявляется скринридером как интерактивный элемент. aria-label объясняет,
 * что делать с клавиатуры, — иначе смысл ручки понятен только зрячему
 * пользователю мыши.
 */
export function createDragHandle(label) {
    const handle = document.createElement('button');
    handle.type = 'button';
    handle.className = 'drag-handle';
    handle.setAttribute('aria-label', label);
    handle.title = label;
    handle.textContent = '☰';
    return handle;
}
