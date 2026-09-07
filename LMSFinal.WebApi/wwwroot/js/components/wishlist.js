/**
 * Избранное — состояние сердечек на карточках курсов.
 *
 * Зачем отдельный модуль, а не запрос из каждой карточки: на странице каталога
 * карточек до двенадцати, и спрашивать сервер «а этот курс в избранном?»
 * по каждой — это двенадцать запросов вместо одного. Здесь список забирается
 * ОДИН раз за загрузку страницы и держится множеством Id.
 *
 * Второе назначение — синхронизация. Один и тот же курс может встретиться
 * дважды (в «Рекомендуемых» и в «Популярных» на главной). Нажатие на одно
 * сердечко должно перекрасить оба, поэтому подписчики оповещаются централизованно.
 */

import { api } from '../api.js';
import { isAuthenticated, isStudent, isInstructor } from '../auth.js';

/**
 * Курсы, отмеченные ДО входа. Лежат в sessionStorage, а не в localStorage:
 * это намерение на один заход, и оно не должно всплыть через неделю в другой
 * вкладке. Переживает переход на страницу входа — этого достаточно.
 */
const PENDING_KEY = 'lms.wishlist.pending';

/** Id избранных курсов. null — список ещё не загружен. */
let ids = null;

/** Уже идущий запрос: при параллельных вызовах ensureLoaded ждём один и тот же. */
let loading = null;

const listeners = new Set();

/**
 * Загружает избранное один раз за жизнь страницы.
 *
 * Запрос уходит только у вошедшего студента: эндпоинт закрыт ролью
 * (WishlistController), и для гостя вернул бы 401. Гостю сердечки при этом
 * показываются — просто все пустые, состояние ему хранить негде.
 */
export async function ensureLoaded() {
    if (!isStudent()) {
        ids = new Set();
        return ids;
    }

    if (ids) {
        return ids;
    }

    if (!loading) {
        loading = api.get('/wishlist')
            .then((items) => {
                ids = new Set((items ?? []).map((item) => item.course?.id ?? item.courseId));
                return ids;
            })
            .catch(() => {
                // Упавший список избранного не должен ронять каталог: показываем
                // пустые сердечки, курсы при этом видны и кликабельны.
                ids = new Set();
                return ids;
            })
            .finally(() => {
                loading = null;
            });
    }

    return loading;
}

export function isInWishlist(courseId) {
    return ids?.has(courseId) ?? false;
}

/**
 * Показывать ли сердечко.
 *
 * Гостю — да: он должен видеть, что курс можно отложить, иначе о функции
 * вообще не узнает. По нажатию его ведёт на вход, а курс сохраняется после.
 * Преподавателю и админу — нет: избранного у них не бывает, эндпоинт закрыт
 * ролью Student, и кнопка вела бы в 403.
 */
export function isAvailable() {
    return !isAuthenticated() || isStudent();
}

/** Нажатие сердечка гостем: запомнить курс и увести на вход. */
export function isGuest() {
    return !isAuthenticated();
}

/**
 * Переключает курс в избранном.
 *
 * Состояние меняется СРАЗУ, до ответа сервера: сердечко должно откликаться
 * на нажатие мгновенно. Если запрос упал — возвращаем как было, иначе на
 * экране осталась бы отметка, которой на сервере нет.
 *
 * @returns {Promise<boolean>} итоговое состояние
 * @throws {ApiError} если сервер отказал — вызывающий показывает сообщение
 */
export async function toggle(courseId) {
    const wasIn = isInWishlist(courseId);

    apply(courseId, !wasIn);

    try {
        if (wasIn) {
            await api.delete(`/wishlist/${courseId}`);
        } else {
            await api.post(`/wishlist/${courseId}`);
        }

        return !wasIn;
    } catch (error) {
        apply(courseId, wasIn);
        throw error;
    }
}

/** Подписка на изменения — карточки перекрашивают свои сердечки. */
export function onChange(listener) {
    listeners.add(listener);
    return () => listeners.delete(listener);
}

// ---------------------------------------------------------------------------
// Отложенное намерение: сердечко нажали до входа
// ---------------------------------------------------------------------------

/** Запоминает курс и возвращает адрес страницы входа с возвратом сюда же. */
export function rememberPending(courseId) {
    const pending = readPending();

    if (!pending.includes(courseId)) {
        pending.push(courseId);
        writePending(pending);
    }

    const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
    return `/pages/login.html?returnUrl=${returnUrl}&wishlist=1`;
}

/**
 * Досылает отложенные курсы после успешного входа или регистрации.
 *
 * Вызывается со страниц входа и регистрации ДО перехода: иначе пользователь
 * уехал бы в каталог, а курс, ради которого он входил, там бы не появился.
 *
 * @returns {Promise<number>} сколько курсов добавлено
 */
export async function flushPending() {
    const pending = readPending();

    // Чистим сразу: если вошли преподавателем, добавлять некуда, но и висеть
    // намерению до следующего входа незачем.
    clearPending();

    if (!pending.length || !isStudent() || isInstructor()) {
        return 0;
    }

    // Каждый курс отдельным запросом: эндпоинт принимает по одному, а список
    // здесь длиной в одну-две позиции — до входа успевает произойти один клик.
    const results = await Promise.allSettled(
        pending.map((courseId) => api.post(`/wishlist/${courseId}`))
    );

    const added = results.filter((r) => r.status === 'fulfilled').length;

    if (added) {
        // Список мог быть уже загружен на этой странице — держим его в согласии.
        ids ??= new Set();
        pending.forEach((courseId) => ids.add(courseId));
    }

    return added;
}

function readPending() {
    try {
        const raw = sessionStorage.getItem(PENDING_KEY);
        const parsed = raw ? JSON.parse(raw) : [];
        return Array.isArray(parsed) ? parsed : [];
    } catch {
        // Приватный режим или запрет на хранилище — молча работаем без памяти.
        return [];
    }
}

function writePending(list) {
    try {
        sessionStorage.setItem(PENDING_KEY, JSON.stringify(list));
    } catch {
        /* см. readPending */
    }
}

function clearPending() {
    try {
        sessionStorage.removeItem(PENDING_KEY);
    } catch {
        /* см. readPending */
    }
}

// ---------------------------------------------------------------------------

function apply(courseId, isIn) {
    ids ??= new Set();

    if (isIn) {
        ids.add(courseId);
    } else {
        ids.delete(courseId);
    }

    listeners.forEach((listener) => {
        try {
            listener(courseId, isIn);
        } catch (error) {
            console.error('Ошибка обработчика избранного:', error);
        }
    });
}
