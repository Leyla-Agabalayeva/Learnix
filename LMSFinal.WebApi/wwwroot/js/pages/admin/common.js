/**
 * Общая обвязка страниц админ-панели. Зеркало instructor/student common.js,
 * но с ролью Admin.
 */

import { requireAuth, ROLES } from '../../auth.js';
import { onLanguageChange, t } from '../../localization.js';
import { renderAdminSidebar } from '../../components/admin-sidebar.js';
import { emptyState } from '../../components/empty-state.js';
import { onReady } from '../../ready.js';

export function startAdminPage(load, { init, reloadOnLanguageChange = true } = {}) {
    onReady(async () => {
        if (!requireAuth(ROLES.ADMIN)) {
            return;
        }

        renderAdminSidebar();

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
