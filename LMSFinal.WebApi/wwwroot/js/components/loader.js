/**
 * Состояния загрузки (разделы 31 и 57 ТЗ) — «не оставлять пустые белые экраны».
 *
 * Три инструмента под три ситуации:
 *   loader.block(container)     — первая загрузка данных в блок (спиннер);
 *   loader.skeleton(container)  — то же, но показываем форму будущего контента;
 *   loader.button(btn, true)    — кнопка ждёт ответа сервера;
 *   loader.overlay()            — на весь экран, для операций, блокирующих всё.
 *
 * Скелетон лучше спиннера там, где заранее известна структура (сетка карточек,
 * строки таблицы): страница не «прыгает», когда данные приходят.
 */

import { t } from '../localization.js';

/** Спиннер с подписью внутрь контейнера. Стирает предыдущее содержимое. */
function block(container, message) {
    if (!container) {
        return;
    }

    container.innerHTML = '';

    const wrapper = document.createElement('div');
    wrapper.className = 'loader-block';

    const spinner = document.createElement('div');
    spinner.className = 'spinner';
    spinner.setAttribute('role', 'status');
    spinner.setAttribute('aria-label', t('states.loading'));

    const label = document.createElement('p');
    label.textContent = message ?? t('states.loading');

    wrapper.append(spinner, label);
    container.appendChild(wrapper);
    markBusy(container);
}

/**
 * Скелетон-заглушки.
 * @param {Element} container
 * @param {{ count?: number, variant?: 'card'|'row' }} [options]
 */
function skeleton(container, options = {}) {
    const { count = 6, variant = 'card' } = options;

    if (!container) {
        return;
    }

    container.innerHTML = '';

    for (let index = 0; index < count; index += 1) {
        container.appendChild(variant === 'card' ? skeletonCard() : skeletonRow());
    }

    markBusy(container);
}

/**
 * Помечает контейнер как «идёт загрузка» и сам снимает пометку, когда
 * содержимое заменят настоящим.
 *
 * Скелетон и спиннер — сообщение чисто визуальное: карточки-заглушки помечены
 * aria-hidden, поэтому для скринридера контейнер просто пуст, и человек не
 * понимает, идёт загрузка или данных нет вовсе. aria-busy закрывает этот пробел.
 *
 * Наблюдатель нужен потому, что заглушку убирает не загрузчик, а код страницы:
 * он присваивает container.innerHTML напрямую, и атрибут остался бы висеть
 * навсегда. Сработав один раз, наблюдатель отключается.
 */
function markBusy(container) {
    container.setAttribute('aria-busy', 'true');

    const observer = new MutationObserver(() => {
        const stillLoading = container.querySelector('.skeleton, .loader-block');

        if (!stillLoading) {
            container.removeAttribute('aria-busy');
            observer.disconnect();
        }
    });

    observer.observe(container, { childList: true });
}

function skeletonCard() {
    const card = document.createElement('div');
    card.className = 'card';
    card.setAttribute('aria-hidden', 'true');

    const thumb = document.createElement('div');
    thumb.className = 'skeleton skeleton-thumb';

    const body = document.createElement('div');
    body.className = 'card-body';
    body.innerHTML = `
        <div class="skeleton skeleton-title"></div>
        <div class="skeleton skeleton-text"></div>
        <div class="skeleton skeleton-text" style="width: 80%"></div>
    `;

    card.append(thumb, body);
    return card;
}

function skeletonRow() {
    const row = document.createElement('div');
    row.className = 'skeleton skeleton-text';
    row.style.height = '2.5rem';
    row.style.marginBottom = 'var(--space-3)';
    row.setAttribute('aria-hidden', 'true');
    return row;
}

/**
 * Переключает кнопку в состояние ожидания и обратно.
 * Кнопка блокируется, чтобы повторный клик не отправил второй запрос.
 */
function button(element, isLoading) {
    if (!element) {
        return;
    }

    element.classList.toggle('is-loading', isLoading);
    element.disabled = isLoading;
    element.setAttribute('aria-busy', String(isLoading));
}

/**
 * Полноэкранный оверлей. Возвращает функцию, которая его убирает:
 *
 *   const hide = loader.overlay();
 *   try { await doWork(); } finally { hide(); }
 */
function overlay() {
    const element = document.createElement('div');
    element.className = 'loader-overlay';
    element.setAttribute('role', 'status');
    element.setAttribute('aria-label', t('states.loading'));

    const spinner = document.createElement('div');
    spinner.className = 'spinner spinner-lg';
    element.appendChild(spinner);

    document.body.appendChild(element);

    return () => element.remove();
}

export const loader = { block, skeleton, button, overlay };
