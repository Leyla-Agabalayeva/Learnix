/**
 * Локализация интерфейса — AZ / EN / RU (разделы 19 и 27 ТЗ).
 *
 * Как это работает:
 *   1. В HTML пишется не текст, а ключ:  <button data-i18n="actions.enroll"></button>
 *   2. Переводы лежат в /locales/{az,en,ru}.json и грузятся один раз через fetch.
 *   3. applyTranslations() проходит по DOM и подставляет значения.
 *   4. setLanguage() перерисовывает страницу БЕЗ перезагрузки и запоминает выбор.
 *
 * Важное архитектурное различие (раздел 20 ТЗ): здесь переводится только
 * ИНТЕРФЕЙС. Содержимое курсов переводится на уровне БД — таблицы
 * CourseTranslation / ModuleTranslation / LessonTranslation, и приходит с API
 * через параметр ?lang=. Это два независимых механизма, и их нельзя путать.
 */

const STORAGE_KEY = 'lms.language';
const DEFAULT_LANGUAGE = 'az';          // раздел 19 ТЗ: язык по умолчанию — азербайджанский
const SUPPORTED = ['az', 'en', 'ru'];

/** Загруженные словари: { az: {...}, en: {...} }. Кэш на время жизни страницы. */
const dictionaries = {};

let currentLanguage = DEFAULT_LANGUAGE;

/** Подписчики на смену языка — например, навбар, который перерисовывает меню. */
const listeners = new Set();

// ---------------------------------------------------------------------------
// Публичный API
// ---------------------------------------------------------------------------

export function getLanguage() {
    return currentLanguage;
}

export function getSupportedLanguages() {
    return [...SUPPORTED];
}

/**
 * Код языка в том виде, в котором его ждёт API: LanguageCode — это enum
 * (AZ / EN / RU), и сериализуется он строкой в верхнем регистре.
 * Отдельная функция нужна, чтобы регистр не размазывался по всем страницам.
 */
export function getApiLanguage() {
    return currentLanguage.toUpperCase();
}

/**
 * Возвращает перевод по ключу вида "nav.courses".
 * Если перевода нет — отдаёт сам ключ: на странице сразу видно, что забыли,
 * и интерфейс при этом не ломается пустотой.
 *
 * @param {string} key
 * @param {Record<string, string|number>} [params] подстановки вида {count}
 */
export function t(key, params) {
    const raw = lookup(dictionaries[currentLanguage], key)
        ?? lookup(dictionaries[DEFAULT_LANGUAGE], key)
        ?? key;

    if (!params || typeof raw !== 'string') {
        return raw;
    }

    return raw.replace(/\{(\w+)\}/g, (match, name) =>
        Object.prototype.hasOwnProperty.call(params, name) ? String(params[name]) : match);
}

/**
 * Инициализация. Вызывается один раз при старте страницы (из app.js).
 * Определяет язык, грузит словарь и переводит текущий DOM.
 */
export async function initLocalization() {
    currentLanguage = detectLanguage();
    await loadDictionary(currentLanguage);

    // Английский догружаем как запасной: если в az.json забыли ключ,
    // пользователь увидит английский текст, а не голый "nav.courses".
    if (currentLanguage !== DEFAULT_LANGUAGE) {
        loadDictionary(DEFAULT_LANGUAGE).catch(() => { /* fallback необязателен */ });
    }

    applyTranslations();
    document.documentElement.lang = currentLanguage;
}

/** Меняет язык: грузит словарь, перерисовывает страницу, сохраняет выбор. */
export async function setLanguage(language) {
    if (!SUPPORTED.includes(language) || language === currentLanguage) {
        return;
    }

    await loadDictionary(language);

    currentLanguage = language;
    localStorage.setItem(STORAGE_KEY, language);
    document.documentElement.lang = language;

    applyTranslations();
    listeners.forEach((listener) => listener(language));

    // Событие для кода, который не хочет импортировать этот модуль
    // (например, инлайновый Alpine-компонент на странице).
    document.dispatchEvent(new CustomEvent('language:changed', { detail: { language } }));
}

/** Подписка на смену языка. Возвращает функцию отписки. */
export function onLanguageChange(listener) {
    listeners.add(listener);
    return () => listeners.delete(listener);
}

/**
 * Переводит поддерево DOM. Вызывается после инициализации и после каждой
 * вставки новой разметки (список курсов, содержимое модалки и т.п.).
 *
 * Поддерживаемые атрибуты:
 *   data-i18n              — текстовое содержимое
 *   data-i18n-html         — то же, но как HTML (для текста со ссылками)
 *   data-i18n-placeholder  — placeholder поля ввода
 *   data-i18n-title        — атрибут title
 *   data-i18n-aria-label   — подпись для скринридера
 *   data-i18n-page-title   — заголовок вкладки; ставится на <title>, к переводу
 *                            автоматически добавляется « — Learnix»
 *
 * @param {ParentNode} [root=document]
 */
export function applyTranslations(root = document) {
    root.querySelectorAll('[data-i18n]').forEach((element) => {
        element.textContent = t(element.dataset.i18n);
    });

    root.querySelectorAll('[data-i18n-html]').forEach((element) => {
        element.innerHTML = t(element.dataset.i18nHtml);
    });

    root.querySelectorAll('[data-i18n-placeholder]').forEach((element) => {
        element.setAttribute('placeholder', t(element.dataset.i18nPlaceholder));
    });

    root.querySelectorAll('[data-i18n-title]').forEach((element) => {
        element.setAttribute('title', t(element.dataset.i18nTitle));
    });

    root.querySelectorAll('[data-i18n-aria-label]').forEach((element) => {
        element.setAttribute('aria-label', t(element.dataset.i18nAriaLabel));
    });

    // Заголовок вкладки. Отдельным атрибутом, а не обычным data-i18n, по двум
    // причинам: к нему всегда добавляется название приложения (иначе пришлось бы
    // держать « — Learnix» в каждом из двадцати ключей на трёх языках), и
    // страницы, которые ставят заголовок сами (урок, курс, сертификат),
    // перезаписывают его после загрузки данных.
    const pageTitle = root.querySelector?.('title[data-i18n-page-title]');

    if (pageTitle) {
        document.title = `${t(pageTitle.dataset.i18nPageTitle)} — ${t('app.name')}`;
    }
}

// ---------------------------------------------------------------------------
// Внутреннее
// ---------------------------------------------------------------------------

/**
 * Порядок определения языка: сохранённый выбор → язык браузера → AZ.
 * Явный выбор пользователя всегда важнее автоопределения.
 */
function detectLanguage() {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved && SUPPORTED.includes(saved)) {
        return saved;
    }

    const browser = (navigator.language || '').slice(0, 2).toLowerCase();
    if (SUPPORTED.includes(browser)) {
        return browser;
    }

    return DEFAULT_LANGUAGE;
}

async function loadDictionary(language) {
    if (dictionaries[language]) {
        return dictionaries[language];
    }

    const response = await fetch(`/locales/${language}.json`, { cache: 'no-cache' });

    if (!response.ok) {
        throw new Error(`Не удалось загрузить словарь /locales/${language}.json`);
    }

    dictionaries[language] = await response.json();
    return dictionaries[language];
}

/** Достаёт значение по пути "a.b.c" из вложенного объекта. */
function lookup(dictionary, key) {
    if (!dictionary) {
        return undefined;
    }

    return key.split('.').reduce(
        (value, part) => (value && typeof value === 'object' ? value[part] : undefined),
        dictionary);
}
