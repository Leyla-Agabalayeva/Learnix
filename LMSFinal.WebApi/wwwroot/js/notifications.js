/**
 * Живые уведомления через SignalR — поверх уже существующего NotificationService
 * (он сохраняет уведомление в БД, здесь только доставка в реальном времени).
 *
 * signalr.min.js — обычный UMD-скрипт (не ES-модуль), поэтому подгружаем его
 * тегом <script> лениво, при первом вызове connectNotifications(), а не через
 * <script> на каждой из ~26 HTML-страниц.
 */

import { getToken } from './auth.js';
import { toast } from './components/toast.js';

let connection = null;
let loadPromise = null;

function loadSignalRScript() {
    if (window.signalR) {
        return Promise.resolve();
    }

    if (!loadPromise) {
        loadPromise = new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = '/js/vendor/signalr.min.js';
            script.onload = () => resolve();
            script.onerror = () => reject(new Error('Не удалось загрузить signalr.min.js'));
            document.head.appendChild(script);
        });
    }

    return loadPromise;
}

export async function connectNotifications() {
    const token = getToken();
    if (!token || connection) {
        return;
    }

    try {
        await loadSignalRScript();
    } catch (error) {
        console.error('SignalR:', error);
        return;
    }

    connection = new window.signalR.HubConnectionBuilder()
        .withUrl('/hubs/notifications', { accessTokenFactory: () => getToken() })
        .withAutomaticReconnect()
        .build();

    connection.on('ReceiveNotification', (notification) => {
        toast.info(notification.message, { title: notification.title });
        document.dispatchEvent(new CustomEvent('lms:notification', { detail: notification }));
    });

    connection.start().catch((error) => console.error('SignalR:', error));
}
