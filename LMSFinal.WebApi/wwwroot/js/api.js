/**
 * Единый HTTP-клиент (раздел 53 ТЗ).
 *
 * Ни одна страница не должна вызывать fetch() напрямую. Всё идёт через этот
 * модуль, потому что здесь в одном месте собрано то, что иначе копировалось бы
 * в каждый файл:
 *   - базовый адрес и сборка query-строки;
 *   - заголовок Authorization: Bearer <token>;
 *   - распаковка ApiResponse<T> — наружу отдаётся сразу data;
 *   - превращение ошибок API в исключение ApiError с полями status/message/errors;
 *   - единая реакция на 401 (сессия истекла) и на недоступный сервер.
 *
 * Фронтенд и API живут на одном origin (wwwroot отдаёт то же приложение),
 * поэтому CORS не нужен и никакой предварительный OPTIONS-запрос не уходит.
 */

import { getToken, handleUnauthorized } from './auth.js';
import { t } from './localization.js';

const BASE_URL = '/api';

/**
 * Ошибка, которую бросает клиент. Страницы ловят её и решают, что показать:
 * тост, подсветку полей формы или пустое состояние.
 */
export class ApiError extends Error {
    /**
     * @param {number} status HTTP-код
     * @param {string} message человекочитаемое сообщение из ApiResponse.message
     * @param {Record<string, string[]>|null} errors ошибки по полям (для 422)
     */
    constructor(status, message, errors = null) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
        this.errors = errors;
    }

    /** 422 — не прошла валидация, у ошибки есть разбивка по полям формы. */
    get isValidationError() {
        return this.status === 422 && this.errors !== null;
    }

    /** 0 — до сервера не достучались (он не запущен, нет сети). */
    get isNetworkError() {
        return this.status === 0;
    }
}

// ---------------------------------------------------------------------------
// Публичные методы
// ---------------------------------------------------------------------------

export const api = {
    get: (path, options) => request('GET', path, options),
    post: (path, body, options) => request('POST', path, { ...options, body }),
    put: (path, body, options) => request('PUT', path, { ...options, body }),
    patch: (path, body, options) => request('PATCH', path, { ...options, body }),
    delete: (path, options) => request('DELETE', path, options),

    /**
     * Скачивание файла (PDF сертификата). Возвращает Blob, а не JSON,
     * поэтому идёт мимо общей распаковки ApiResponse.
     */
    async download(path, options = {}) {
        const response = await send('GET', path, options);

        if (!response.ok) {
            throw await toApiError(response);
        }

        return response.blob();
    },

    /**
     * Загрузка файла (аватар). Отдельно от обычных запросов: тело — FormData,
     * а не JSON, и Content-Type нельзя ставить руками — браузер сам добавляет
     * его вместе с boundary, без этого сервер не сможет разобрать multipart.
     */
    async upload(path, formData) {
        const token = getToken();
        const headers = { Accept: 'application/json' };
        if (token) {
            headers.Authorization = `Bearer ${token}`;
        }

        let response;
        try {
            response = await fetch(BASE_URL + path, { method: 'POST', headers, body: formData });
        } catch {
            throw new ApiError(0, 'network');
        }

        if (!response.ok) {
            throw await toApiError(response);
        }

        const payload = await readJson(response);
        return payload && typeof payload === 'object' && 'data' in payload
            ? payload.data
            : payload;
    },

    /** Сохраняет Blob как файл — браузерного API для этого нет, приходится через ссылку. */
    saveBlob(blob, fileName) {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();

        // Освобождаем память: иначе Blob висит до перезагрузки страницы.
        URL.revokeObjectURL(url);
    }
};

// ---------------------------------------------------------------------------
// Внутреннее
// ---------------------------------------------------------------------------

/**
 * @param {string} method
 * @param {string} path путь после /api, например "/courses/my"
 * @param {{ body?: unknown, query?: Record<string, unknown>, anonymous?: boolean }} [options]
 *        anonymous: не подставлять токен (публичные эндпоинты каталога)
 */
async function request(method, path, options = {}) {
    const response = await send(method, path, options);

    // 204 No Content — тело пустое, парсить нечего.
    if (response.status === 204) {
        return null;
    }

    if (!response.ok) {
        throw await toApiError(response, options);
    }

    const payload = await readJson(response);

    // Все успешные ответы обёрнуты в ApiResponse<T> (раздел 43 ТЗ) —
    // страницам нужна только полезная нагрузка.
    return payload && typeof payload === 'object' && 'data' in payload
        ? payload.data
        : payload;
}

async function send(method, path, options = {}) {
    const { body, query, anonymous = false } = options;

    const headers = { Accept: 'application/json' };

    if (body !== undefined) {
        headers['Content-Type'] = 'application/json';
    }

    if (!anonymous) {
        const token = getToken();
        if (token) {
            headers.Authorization = `Bearer ${token}`;
        }
    }

    try {
        return await fetch(BASE_URL + path + buildQuery(query), {
            method,
            headers,
            body: body === undefined ? undefined : JSON.stringify(body)
        });
    } catch {
        // fetch отклоняется только при сетевой ошибке: HTTP-коды 4xx/5xx
        // сюда не попадают, их обрабатывает вызывающий код.
        throw new ApiError(0, 'network');
    }
}

/**
 * Строит query-строку, выбрасывая пустые значения — иначе в адресе
 * появлялись бы бессмысленные "?searchTerm=&level=".
 */
function buildQuery(query) {
    if (!query) {
        return '';
    }

    const params = new URLSearchParams();

    Object.entries(query).forEach(([key, value]) => {
        if (value === undefined || value === null || value === '') {
            return;
        }

        if (Array.isArray(value)) {
            value.forEach((item) => params.append(key, String(item)));
        } else {
            params.append(key, String(value));
        }
    });

    const queryString = params.toString();
    return queryString ? `?${queryString}` : '';
}

async function readJson(response) {
    const text = await response.text();

    if (!text) {
        return null;
    }

    try {
        return JSON.parse(text);
    } catch {
        return null;
    }
}

/**
 * Превращает неуспешный ответ в ApiError.
 * Формат ошибки задан GlobalExceptionMiddleware:
 *   { success: false, message: "...", errors: { "title": ["Title is required"] } }
 */
async function toApiError(response, options = {}) {
    const payload = await readJson(response);

    const message = payload?.message || defaultMessageFor(response.status);
    const errors = payload?.errors ?? null;

    // 401 обрабатывается централизованно: токен протух или его нет — чистим сессию
    // и уводим на страницу входа. Иначе каждая страница дублировала бы эту логику.
    //
    // Но для анонимных запросов этого делать НЕЛЬЗЯ: 401 от /auth/login означает
    // «неверный пароль», а не «сессия истекла». Без этой проверки неудачная попытка
    // входа перебрасывала бы пользователя на страницу входа вместо того, чтобы
    // показать ошибку прямо в форме.
    if (response.status === 401 && !options.anonymous) {
        handleUnauthorized();
    }

    return new ApiError(response.status, message, errors);
}

/**
 * Запасное сообщение, когда сервер не прислал своего (например, ответ без тела).
 *
 * Раньше эти строки были зашиты по-русски прямо здесь: азербайджанский или
 * английский интерфейс на любой такой ошибке показывал русский текст.
 *
 * Словарь к этому моменту уже загружен: app.js ждёт initLocalization до того,
 * как страницы начнут обращаться к API.
 */
function defaultMessageFor(status) {
    switch (status) {
        case 400: return t('errors.badRequest');
        case 401: return t('errors.unauthorized');
        case 403: return t('errors.forbidden');
        case 404: return t('errors.notFound');
        case 409: return t('errors.conflict');
        case 422: return t('errors.validation');
        default:  return t('errors.server');
    }
}
