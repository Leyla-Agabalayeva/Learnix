/**
 * Верхняя полоска-объявление (раздел вдохновения — зелёный баннер Udemy).
 *
 * Текст сознательно без цифр и «осталось N дней»: в проекте нет ни скидок,
 * ни реального дедлайна, а придуманная срочность — это то самое fake statistics,
 * которого правило проекта требует избегать. Закрывается один раз навсегда —
 * не должен возвращаться при каждом визите, иначе X на баннере бесполезен.
 */

import { t } from '../localization.js';

const STORAGE_KEY = 'lms.banner.dismissed';

export function renderAnnouncementBanner(container = document.getElementById('navbar')) {
    if (!container || wasDismissed()) {
        return;
    }

    const bar = document.createElement('div');
    bar.className = 'announcement-banner';
    bar.setAttribute('role', 'note');

    const text = document.createElement('span');
    text.textContent = t('banner.message');
    bar.appendChild(text);

    const close = document.createElement('button');
    close.type = 'button';
    close.className = 'announcement-banner-close';
    close.setAttribute('aria-label', t('actions.close'));
    close.textContent = '×';
    close.addEventListener('click', () => {
        dismiss();
        bar.remove();
    });
    bar.appendChild(close);

    container.before(bar);
}

function wasDismissed() {
    try {
        return localStorage.getItem(STORAGE_KEY) === 'true';
    } catch {
        return false;
    }
}

function dismiss() {
    try {
        localStorage.setItem(STORAGE_KEY, 'true');
    } catch {
        /* приватный режим — баннер просто вернётся при следующей загрузке */
    }
}
