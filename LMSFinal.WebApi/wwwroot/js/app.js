/**
 * Точка входа для каждой страницы.
 *
 * Подключается один раз:
 *   <script type="module" src="/js/app.js"></script>
 *
 * Делает три вещи, нужные вообще везде:
 *   1. грузит словарь и переводит разметку;
 *   2. рисует навбар;
 *   3. отдаёт компоненты в window.LMS — чтобы простые страницы могли
 *      обойтись обычным <script> без импортов.
 *
 * Страницы со своей логикой импортируют модули напрямую:
 *   import { api } from '/js/api.js';
 */

import { initLocalization, t, applyTranslations, setLanguage, getLanguage, getApiLanguage } from './localization.js';
import { renderNavbar } from './components/navbar.js';
import { renderAnnouncementBanner } from './components/announcement-banner.js';
import { api, ApiError } from './api.js';
import * as auth from './auth.js';
import { toast } from './components/toast.js';
import { modal } from './components/modal.js';
import { loader } from './components/loader.js';
import { emptyState } from './components/empty-state.js';
import { markReady } from './ready.js';
import { initTheme } from './theme.js';

/**
 * Общий фасад. ES-модули изолированы, и без такого моста инлайновый скрипт
 * на странице не смог бы позвать toast или modal.
 */
window.LMS = {
    api, ApiError, auth,
    toast, modal, loader, emptyState,
    t, applyTranslations, setLanguage, getLanguage, getApiLanguage
};

async function bootstrap() {
    initTheme();

    try {
        await initLocalization();
    } catch (error) {
        // Без словаря страница всё равно должна открыться — просто с ключами
        // вместо подписей. Ронять весь интерфейс из-за перевода нельзя.
        console.error('Не удалось загрузить локализацию:', error);
    }

    renderAnnouncementBanner();
    renderNavbar();

    // Сигнал странице: инфраструктура готова, можно грузить данные.
    // markReady запоминает состояние, поэтому скрипт страницы получит сигнал
    // даже если подписался позже — см. ready.js.
    markReady();
}

// DOM может быть ещё не готов, если скрипт подключён в <head>.
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bootstrap);
} else {
    bootstrap();
}

