/**
 * Очистка HTML перед вставкой в страницу.
 *
 * Содержимое урока (LessonTranslation.Content) — это HTML, который пишет
 * инструктор: абзацы, списки, примеры кода. Вставлять его через textContent
 * нельзя, иначе студент увидит теги вместо форматирования.
 *
 * Но innerHTML без очистки — это XSS. Инструктор в этой системе не админ:
 * зарегистрироваться им может кто угодно (см. страницу регистрации), а токен
 * студента лежит в localStorage. Один <script> в тексте урока — и чужая сессия
 * уходит на сторонний сервер. Поэтому содержимое проходит через белый список.
 *
 * Разбор идёт через DOMParser, а не регулярными выражениями: браузер разбирает
 * ту же строку той же логикой, что и при вставке, поэтому обмануть очистку
 * кривой вложенностью или экзотическим экранированием не получится.
 */

/** Теги, которые нужны для оформления текста урока. Всё остальное разворачивается. */
const ALLOWED_TAGS = new Set([
    'P', 'BR', 'HR',
    'STRONG', 'B', 'EM', 'I', 'U', 'S', 'MARK', 'SMALL', 'SUB', 'SUP',
    'H2', 'H3', 'H4', 'H5', 'H6',
    'UL', 'OL', 'LI', 'DL', 'DT', 'DD',
    'BLOCKQUOTE', 'PRE', 'CODE', 'KBD', 'SAMP',
    'TABLE', 'THEAD', 'TBODY', 'TFOOT', 'TR', 'TH', 'TD', 'CAPTION',
    'A', 'IMG', 'FIGURE', 'FIGCAPTION', 'SPAN', 'DIV'
]);

/** Атрибуты по тегам. Ни style, ни class: оформление задаёт страница, а не автор. */
const ALLOWED_ATTRIBUTES = {
    A: ['href', 'title'],
    IMG: ['src', 'alt', 'width', 'height'],
    TH: ['colspan', 'rowspan', 'scope'],
    TD: ['colspan', 'rowspan']
};

/** Теги, которые удаляются вместе с содержимым — разворачивать тут нечего. */
const DROP_ENTIRELY = new Set(['SCRIPT', 'STYLE', 'IFRAME', 'OBJECT', 'EMBED', 'FORM', 'INPUT', 'BUTTON', 'TEXTAREA', 'SELECT', 'LINK', 'META', 'BASE', 'SVG', 'MATH']);

/**
 * Возвращает очищенный HTML-фрагмент, готовый к вставке.
 *
 * @param {string} html исходная разметка
 * @returns {DocumentFragment}
 */
export function sanitizeHtml(html) {
    const fragment = document.createDocumentFragment();

    if (!html) {
        return fragment;
    }

    // Разбираем в отдельном документе: изображения и прочие ресурсы там
    // не загружаются, поэтому <img src="...onerror"> не успевает сработать.
    const parsed = new DOMParser().parseFromString(String(html), 'text/html');

    clean(parsed.body);

    while (parsed.body.firstChild) {
        fragment.appendChild(parsed.body.firstChild);
    }

    return fragment;
}

/** Вставляет очищенный HTML в контейнер, заменяя прежнее содержимое. */
export function setHtml(container, html) {
    if (!container) {
        return;
    }

    container.innerHTML = '';
    container.appendChild(sanitizeHtml(html));
}

// ---------------------------------------------------------------------------
// Внутреннее
// ---------------------------------------------------------------------------

function clean(root) {
    // Обходим потомков копией списка: узлы по ходу дела удаляются и заменяются,
    // и живая коллекция childNodes сбилась бы прямо во время перебора.
    [...root.childNodes].forEach((node) => {
        if (node.nodeType === Node.TEXT_NODE) {
            return;
        }

        if (node.nodeType !== Node.ELEMENT_NODE) {
            node.remove();   // комментарии, CDATA — в тексте урока им делать нечего
            return;
        }

        if (DROP_ENTIRELY.has(node.tagName)) {
            node.remove();
            return;
        }

        // Чистим потомков до того, как решать судьбу самого узла: если узел
        // придётся разворачивать, его дети уже будут безопасны.
        clean(node);

        if (!ALLOWED_TAGS.has(node.tagName)) {
            unwrap(node);
            return;
        }

        cleanAttributes(node);
    });
}

function cleanAttributes(element) {
    const allowed = ALLOWED_ATTRIBUTES[element.tagName] ?? [];

    [...element.attributes].forEach((attribute) => {
        const name = attribute.name.toLowerCase();

        // on* ловится и белым списком, но проверка явная: это самый частый вектор,
        // и её стоит видеть в коде, а не выводить из отсутствия в другом месте.
        if (name.startsWith('on') || !allowed.includes(name)) {
            element.removeAttribute(attribute.name);
            return;
        }

        if ((name === 'href' || name === 'src') && !isSafeUrl(attribute.value)) {
            element.removeAttribute(attribute.name);
        }
    });

    // Внешние ссылки открываем в новой вкладке — студент не должен терять урок,
    // кликнув по ссылке на документацию. rel обязателен: без noopener открытая
    // страница получает доступ к window.opener и может подменить нашу вкладку.
    //
    // Внутренние ссылки трогать не надо: новая вкладка на каждый переход по
    // своему же сайту — это десяток вкладок за урок и сломанная кнопка «Назад».
    const href = element.tagName === 'A' ? element.getAttribute('href') : null;

    if (href && isExternalUrl(href)) {
        element.setAttribute('target', '_blank');
        element.setAttribute('rel', 'noopener noreferrer');
    }
}

/**
 * Пропускаем только те схемы, по которым нельзя выполнить код.
 * javascript: и data: отсекаются: data:text/html выполняется как страница.
 */
function isSafeUrl(value) {
    const url = String(value).trim();

    // Относительные пути и якоря — свои, они безопасны по определению.
    if (url.startsWith('/') || url.startsWith('#') || url.startsWith('./') || url.startsWith('../')) {
        return true;
    }

    return /^(https?|mailto):/i.test(url);
}

/** Ведёт ли ссылка на другой сайт. mailto: тоже уводит из приложения. */
function isExternalUrl(value) {
    const url = String(value).trim();

    if (url.startsWith('#')) {
        return false;   // якорь внутри самого урока
    }

    if (/^mailto:/i.test(url)) {
        return true;
    }

    try {
        return new URL(url, window.location.origin).origin !== window.location.origin;
    } catch {
        return false;
    }
}

/** Убирает тег, оставляя его содержимое: текст урока не должен пропадать. */
function unwrap(element) {
    const parent = element.parentNode;

    if (!parent) {
        return;
    }

    while (element.firstChild) {
        parent.insertBefore(element.firstChild, element);
    }

    element.remove();
}
