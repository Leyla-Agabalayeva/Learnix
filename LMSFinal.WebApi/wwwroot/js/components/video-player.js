/**
 * Проигрыватель видео урока.
 *
 * В seed-данных лежат обычные ссылки вида youtube.com/watch?v=ID — те, что
 * копируются из адресной строки. Вставить такую ссылку в iframe нельзя:
 * YouTube отдаёт страницу с X-Frame-Options и кадр остаётся пустым. Встраивать
 * можно только адрес /embed/ID, поэтому ссылку приходится разбирать.
 *
 * Разбор вынесен в отдельный модуль, потому что вариантов ссылки много
 * (watch?v=, youtu.be/, /shorts/, уже готовый /embed/), а инструктор вставит
 * любой из них — и урок не должен оставаться без видео из-за формата ссылки.
 */

import { t } from '../localization.js';

/**
 * Рисует видео урока в контейнере.
 *
 * @param {Element} container
 * @param {string|null} videoUrl ссылка из LessonLocalizedDto.VideoUrl
 * @param {string} title название урока — попадает в подпись кадра
 * @returns {boolean} было ли что показывать
 */
export function renderVideo(container, videoUrl, title) {
    if (!container) {
        return false;
    }

    container.innerHTML = '';

    const source = parseVideoUrl(videoUrl);

    if (!source) {
        // Урок без видео — не ошибка: текстовых уроков в курсе больше, чем
        // видеоуроков. Показываем спокойную заглушку, а не пустое место,
        // иначе выглядит как несработавшая загрузка.
        container.appendChild(buildPlaceholder());
        return false;
    }

    container.appendChild(source.kind === 'file' ? buildFile(source.url) : buildEmbed(source.url, title));
    return true;
}

/**
 * Приводит ссылку к виду, пригодному для вставки.
 * @returns {{ kind: 'embed'|'file', url: string }|null}
 */
export function parseVideoUrl(videoUrl) {
    const raw = (videoUrl ?? '').trim();

    if (!raw) {
        return null;
    }

    let url;

    try {
        // Относительные ссылки на файлы в wwwroot тоже должны работать,
        // поэтому разбираем относительно текущего адреса.
        url = new URL(raw, window.location.origin);
    } catch {
        return null;
    }

    if (url.protocol !== 'http:' && url.protocol !== 'https:') {
        return null;   // javascript: и прочее — мимо
    }

    const host = url.hostname.replace(/^www\./, '');

    if (host === 'youtube.com' || host === 'm.youtube.com' || host === 'youtube-nocookie.com') {
        const id = url.pathname.startsWith('/embed/')
            ? url.pathname.slice('/embed/'.length)
            : url.pathname.startsWith('/shorts/')
                ? url.pathname.slice('/shorts/'.length)
                : url.searchParams.get('v');

        return id ? { kind: 'embed', url: youtubeEmbed(id, url.searchParams.get('t')) } : null;
    }

    if (host === 'youtu.be') {
        const id = url.pathname.slice(1);
        return id ? { kind: 'embed', url: youtubeEmbed(id, url.searchParams.get('t')) } : null;
    }

    if (host === 'vimeo.com') {
        const id = url.pathname.split('/').filter(Boolean)[0];
        return id && /^\d+$/.test(id) ? { kind: 'embed', url: `https://player.vimeo.com/video/${id}` } : null;
    }

    if (/\.(mp4|webm|ogg|ogv|mov)$/i.test(url.pathname)) {
        return { kind: 'file', url: url.href };
    }

    return null;
}

// ---------------------------------------------------------------------------
// Внутреннее
// ---------------------------------------------------------------------------

/**
 * Домен -nocookie: тот же проигрыватель, но YouTube не ставит рекламные куки
 * до нажатия «play». Студент смотрит урок, а не соглашается на трекинг.
 */
function youtubeEmbed(id, start) {
    const clean = encodeURIComponent(id.split('/')[0]);
    const seconds = Number.parseInt(String(start ?? '').replace('s', ''), 10);

    return Number.isFinite(seconds) && seconds > 0
        ? `https://www.youtube-nocookie.com/embed/${clean}?start=${seconds}`
        : `https://www.youtube-nocookie.com/embed/${clean}`;
}

function buildEmbed(src, title) {
    const frame = document.createElement('iframe');

    frame.className = 'lesson-video-frame';
    frame.src = src;
    frame.title = title || t('lesson.videoTitle');
    frame.loading = 'lazy';
    frame.allowFullscreen = true;
    frame.setAttribute('allow', 'accelerometer; encrypted-media; picture-in-picture; fullscreen');

    // Чужая страница внутри нашей: без sandbox у неё тот же доступ к верхнему
    // окну, что у нашего кода. Разрешаем ровно то, без чего проигрыватель
    // не работает, и не даём allow-top-navigation — иначе кадр может увести
    // студента с урока на произвольный адрес.
    frame.setAttribute('sandbox', 'allow-scripts allow-same-origin allow-presentation allow-popups allow-popups-to-escape-sandbox');

    // referrerpolicy: адрес урока (а в нём — id курса) не утекает на YouTube.
    frame.setAttribute('referrerpolicy', 'strict-origin-when-cross-origin');

    return frame;
}

function buildFile(src) {
    const video = document.createElement('video');

    video.className = 'lesson-video-frame';
    video.src = src;
    video.controls = true;
    video.preload = 'metadata';

    return video;
}

function buildPlaceholder() {
    const placeholder = document.createElement('div');
    placeholder.className = 'lesson-video-placeholder';

    const icon = document.createElement('span');
    icon.className = 'lesson-video-placeholder-icon';
    icon.textContent = '📖';
    icon.setAttribute('aria-hidden', 'true');

    const text = document.createElement('p');
    text.textContent = t('lesson.noVideo');

    placeholder.append(icon, text);
    return placeholder;
}
