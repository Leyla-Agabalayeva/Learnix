/**
 * Общая обвязка страниц кабинета преподавателя.
 *
 * Зеркало student/common.js, но с ролью Instructor. Разделены намеренно:
 * у кабинетов разные меню и разные роли, а попытка свести их в один
 * «универсальный» модуль с флагами читалась бы хуже, чем два коротких файла.
 */

import { requireAuth, ROLES } from '../../auth.js';
import { onLanguageChange, t } from '../../localization.js';
import { renderInstructorSidebar } from '../../components/instructor-sidebar.js';
import { emptyState } from '../../components/empty-state.js';
import { onReady } from '../../ready.js';

/**
 * @param {() => Promise<void>} load загрузка данных — повторяется при смене языка
 * @param {{ init?: () => void, reloadOnLanguageChange?: boolean }} [options]
 *        init: разовая настройка (обработчики) — выполняется один раз.
 */
export function startInstructorPage(load, { init, reloadOnLanguageChange = true } = {}) {
    onReady(async () => {
        // Роль проверяется и здесь, и на сервере. Здесь — чтобы студент не увидел
        // пустую страницу с ошибками; на сервере — чтобы он не увидел данные.
        if (!requireAuth(ROLES.INSTRUCTOR)) {
            return;
        }

        renderInstructorSidebar();

        await load();
        init?.();

        if (reloadOnLanguageChange) {
            onLanguageChange(load);
        }
    });
}

export function showLoadError(container, error, retry) {
    emptyState.error(container, {
        message: error?.isNetworkError ? t('states.networkError') : error?.message,
        onRetry: retry
    });
}

/**
 * @param {string} label
 * @param {string} value
 * @param {{ tone?: 'success'|'warning'|'danger'|'neutral' }} [options]
 */
export function buildStat(label, value, { tone } = {}) {
    const stat = document.createElement('div');
    stat.className = tone ? `stat stat-${tone}` : 'stat';

    const body = document.createElement('div');
    body.className = 'stat-body';

    const valueElement = document.createElement('p');
    valueElement.className = 'stat-value';
    valueElement.textContent = value;

    const labelElement = document.createElement('p');
    labelElement.className = 'stat-label';
    labelElement.textContent = label;

    body.append(valueElement, labelElement);
    stat.appendChild(body);
    return stat;
}

/** Бейдж статуса курса — одинаковый на всех страницах преподавателя. */
export function buildStatusBadge(status) {
    const badge = document.createElement('span');

    badge.className = {
        Published: 'badge badge-success',
        Archived: 'badge badge-warning'
    }[status] ?? 'badge badge-neutral';

    badge.textContent = t(`course.status${status}`);
    return badge;
}
