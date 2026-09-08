/**
 * Колокольчик уведомлений в навбаре.
 *
 * История приходит через уже готовый REST (GET /api/notifications) — сюда
 * попадает всё, что когда-либо создал NotificationService (сертификаты,
 * тесты, курсы). Новые уведомления добавляются в список живьём через событие
 * 'lms:notification', которое кидает notifications.js при получении пуша от
 * SignalR — так не нужно ни поллинга, ни повторной перезагрузки списка.
 */

import { t } from '../localization.js';
import { api } from '../api.js';
import * as format from '../format.js';

export function buildNotificationsBell() {
    const wrapper = document.createElement('div');
    wrapper.className = 'user-menu';

    const trigger = document.createElement('button');
    trigger.type = 'button';
    trigger.className = 'navbar-bell';
    trigger.setAttribute('aria-haspopup', 'true');
    trigger.setAttribute('aria-expanded', 'false');
    trigger.setAttribute('aria-label', t('nav.notifications'));

    const icon = document.createElement('span');
    icon.setAttribute('aria-hidden', 'true');
    icon.textContent = '🔔';
    trigger.appendChild(icon);

    const badge = document.createElement('span');
    badge.className = 'navbar-cart-badge hidden';
    trigger.appendChild(badge);

    const dropdown = document.createElement('div');
    dropdown.className = 'dropdown notifications-dropdown hidden';

    const header = document.createElement('div');
    header.className = 'dropdown-header notifications-header';

    const headerLabel = document.createElement('p');
    headerLabel.className = 'text-sm font-semibold';
    headerLabel.textContent = t('nav.notifications');

    const markAllButton = document.createElement('button');
    markAllButton.type = 'button';
    markAllButton.className = 'notifications-mark-all';
    markAllButton.textContent = t('actions.markAllRead');
    markAllButton.hidden = true;

    header.append(headerLabel, markAllButton);
    dropdown.appendChild(header);

    const list = document.createElement('div');
    list.className = 'notifications-list';
    dropdown.appendChild(list);

    wrapper.append(trigger, dropdown);

    let notifications = [];

    function paintBadge() {
        const unread = notifications.filter((n) => !n.isRead).length;
        badge.textContent = String(unread);
        badge.classList.toggle('hidden', unread === 0);
        markAllButton.hidden = unread === 0;
    }

    function renderList() {
        list.innerHTML = '';

        if (!notifications.length) {
            const empty = document.createElement('p');
            empty.className = 'text-sm text-muted notifications-empty';
            empty.textContent = t('states.noNotifications');
            list.appendChild(empty);
            return;
        }

        notifications.forEach((notification) => list.appendChild(buildItem(notification)));
    }

    function buildItem(notification) {
        // Не <button> целиком: внутри своя кнопка удаления — вложенные button
        // невалидны и конфликтуют по кликам, поэтому строка — div с ролью кнопки
        // на текстовой части, а удаление — отдельный элемент управления сбоку.
        const item = document.createElement('div');
        item.className = notification.isRead ? 'notifications-item' : 'notifications-item is-unread';

        const body = document.createElement('button');
        body.type = 'button';
        body.className = 'notifications-item-body';

        const title = document.createElement('p');
        title.className = 'notifications-item-title';
        title.textContent = notification.title;

        const message = document.createElement('p');
        message.className = 'notifications-item-message';
        message.textContent = notification.message;

        const time = document.createElement('p');
        time.className = 'notifications-item-time';
        time.textContent = format.date(notification.createdAt);

        body.append(title, message, time);

        body.addEventListener('click', async () => {
            if (notification.isRead) {
                return;
            }

            notification.isRead = true;
            paintBadge();
            item.classList.remove('is-unread');
            renderDeleteButton();

            try {
                await api.put(`/notifications/${notification.id}/read`);
            } catch {
                // Тихо: список из БД подтянется корректным при следующем открытии,
                // а поднимать тост из-за неотмеченной галочки — перебор.
            }
        });

        item.appendChild(body);

        // Удалить можно только прочитанное — так же, как ограничено на бэкенде
        // (DeleteAsync кидает ConflictException для непрочитанных).
        let deleteButton = null;
        function renderDeleteButton() {
            if (deleteButton || !notification.isRead) {
                return;
            }

            deleteButton = document.createElement('button');
            deleteButton.type = 'button';
            deleteButton.className = 'notifications-item-delete';
            deleteButton.setAttribute('aria-label', t('actions.delete'));
            deleteButton.textContent = '×';

            deleteButton.addEventListener('click', async (event) => {
                event.stopPropagation();

                item.remove();
                notifications = notifications.filter((n) => n.id !== notification.id);

                try {
                    await api.delete(`/notifications/${notification.id}`);
                } catch {
                    // Тихо: если удаление не прошло, запись просто вернётся при
                    // следующем открытии колокольчика — не стоит пугать тостом.
                }

                if (!list.children.length) {
                    renderList();
                }
            });

            item.appendChild(deleteButton);
        }

        renderDeleteButton();

        return item;
    }

    async function load() {
        try {
            notifications = await api.get('/notifications');
        } catch {
            notifications = [];
        }

        paintBadge();
        renderList();
    }

    markAllButton.addEventListener('click', async (event) => {
        event.stopPropagation();
        const unread = notifications.filter((n) => !n.isRead);

        unread.forEach((n) => { n.isRead = true; });
        paintBadge();
        renderList();

        await Promise.all(
            unread.map((n) => api.put(`/notifications/${n.id}/read`).catch(() => {}))
        );
    });

    function toggle(open) {
        dropdown.classList.toggle('hidden', !open);
        trigger.setAttribute('aria-expanded', String(open));
    }

    trigger.addEventListener('click', (event) => {
        event.stopPropagation();
        toggle(dropdown.classList.contains('hidden'));
    });

    document.addEventListener('click', () => toggle(false));
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            toggle(false);
        }
    });

    dropdown.addEventListener('click', (event) => event.stopPropagation());

    // Живой пуш от SignalR (см. notifications.js) — добавляем в начало списка
    // без похода на сервер.
    document.addEventListener('lms:notification', (event) => {
        notifications.unshift({ ...event.detail, isRead: false });
        paintBadge();
        renderList();
    });

    load();

    return wrapper;
}
