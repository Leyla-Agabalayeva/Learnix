/**
 * Корзина «на запись».
 *
 * В Learnix нет платёжного шлюза — запись на курс уже бесплатна и происходит
 * в один клик (POST /courses/{id}/enroll). Корзина здесь не эмулирует оплату,
 * а даёт собрать несколько курсов и записаться на все разом — это реальное,
 * а не бутафорское действие: «Оформить» вызывает тот же enroll для каждого id.
 *
 * Целиком в localStorage, без сервера: список из id курсов, максимум пара
 * десятков позиций, синхронизировать с бэкендом здесь не за чем — в отличие
 * от избранного (wishlist.js), у которого есть свой API и он должен быть
 * виден и с телефона, и с ноутбука одного и того же студента.
 */

const STORAGE_KEY = 'lms.cart';

const listeners = new Set();

export function getIds() {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        const parsed = raw ? JSON.parse(raw) : [];
        return Array.isArray(parsed) ? parsed : [];
    } catch {
        return [];
    }
}

export function has(courseId) {
    return getIds().includes(courseId);
}

export function count() {
    return getIds().length;
}

export function add(courseId) {
    const ids = getIds();
    if (!ids.includes(courseId)) {
        ids.push(courseId);
        write(ids);
    }
}

export function remove(courseId) {
    write(getIds().filter((id) => id !== courseId));
}

export function clear() {
    write([]);
}

export function onChange(listener) {
    listeners.add(listener);
    return () => listeners.delete(listener);
}

function write(ids) {
    try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(ids));
    } catch {
        // Приватный режим — корзина просто не переживёт перезагрузку.
    }

    listeners.forEach((listener) => {
        try {
            listener(ids);
        } catch (error) {
            console.error('Ошибка обработчика корзины:', error);
        }
    });
}
