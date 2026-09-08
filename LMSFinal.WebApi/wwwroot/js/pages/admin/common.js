/**
 * Общая обвязка страниц админ-панели. Зеркало instructor/student common.js,
 * но с ролью Admin.
 */

import { requireAuth, ROLES } from '../../auth.js';
import { onLanguageChange, t } from '../../localization.js';
import { renderAdminSidebar } from '../../components/admin-sidebar.js';
import { emptyState } from '../../components/empty-state.js';
import { onReady } from '../../ready.js';
import { connectNotifications } from '../../notifications.js';

export function startAdminPage(load, { init, reloadOnLanguageChange = true } = {}) {
    onReady(async () => {
        if (!requireAuth(ROLES.ADMIN)) {
            return;
        }

        renderAdminSidebar();
        connectNotifications();

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
 *        tone красит тонкую полоску слева по смыслу цифры (успех/предупреждение/
 *        нейтрально) — без tone плитка нейтральная.
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
