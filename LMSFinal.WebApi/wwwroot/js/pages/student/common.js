/**
 * Общая обвязка страниц личного кабинета студента.
 *
 * Каждая из шести страниц начинается одинаково: проверить роль, нарисовать
 * боковое меню, загрузить данные, перезагрузить их при смене языка.
 * Чтобы это не повторялось шесть раз, всё собрано здесь.
 */

import { requireAuth, ROLES } from '../../auth.js';
import { onLanguageChange, t } from '../../localization.js';
import { renderStudentSidebar } from '../../components/student-sidebar.js';
import { emptyState } from '../../components/empty-state.js';
import { onReady } from '../../ready.js';

/**
 * Запускает страницу кабинета.
 *
 * @param {() => Promise<void>} load загрузка данных — вызывается при старте
 *        и повторно при смене языка.
 * @param {{ init?: () => void, reloadOnLanguageChange?: boolean }} [options]
 *        init: разовая настройка страницы — навешивание обработчиков.
 *              Вынесено отдельно от load намеренно: load вызывается заново
 *              при каждой смене языка, и если вешать слушатели там, после
 *              двух переключений форма отправлялась бы трижды.
 */
export function startStudentPage(load, { init, reloadOnLanguageChange = true } = {}) {
    onReady(async () => {
        // Страница только для студентов. Это удобство, а не защита:
        // данные всё равно закрыты ролью на сервере.
        if (!requireAuth(ROLES.STUDENT)) {
            return;
        }

        renderStudentSidebar();

        await load();
        init?.();

        if (reloadOnLanguageChange) {
            onLanguageChange(load);
        }
    });
}

/** Единообразный показ ошибки загрузки с кнопкой «Повторить». */
export function showLoadError(container, error, retry) {
    emptyState.error(container, {
        message: error?.isNetworkError ? t('states.networkError') : error?.message,
        onRetry: retry
    });
}

/** Плитка статистики — используется на дашборде и в ведомости. */
export function buildStat(label, value) {
    const stat = document.createElement('div');
    stat.className = 'stat';

    const valueElement = document.createElement('p');
    valueElement.className = 'stat-value';
    valueElement.textContent = value;

    const labelElement = document.createElement('p');
    labelElement.className = 'stat-label';
    labelElement.textContent = label;

    stat.append(valueElement, labelElement);
    return stat;
}
