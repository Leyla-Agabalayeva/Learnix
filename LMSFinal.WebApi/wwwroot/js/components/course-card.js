/**
 * Карточка курса (раздел 25 ТЗ).
 *
 * Один компонент на лендинг, каталог, избранное и «мои курсы» — иначе карточка
 * разъехалась бы по четырём страницам и начала выглядеть по-разному.
 *
 * Все данные приходят из CourseSummaryLocalizedDto: рейтинг, число студентов и
 * имя преподавателя реальные, посчитанные на сервере. Ничего не выдумывается
 * на фронтенде (раздел 68 ТЗ — никаких fake statistics).
 */

import { t } from '../localization.js';
import * as format from '../format.js';
import { toast } from './toast.js';
import * as wishlist from './wishlist.js';

/**
 * @param {object} course CourseSummaryLocalizedDto
 * @param {{ href?: string, showStatus?: boolean, footer?: Node, wishlist?: boolean }} [options]
 *        wishlist: показывать сердечко «в избранное». Включается на страницах,
 *        где курс ещё можно отложить на потом (каталог, главная), и НЕ включается
 *        там, где это бессмысленно: в «моих курсах» студент уже учится, а на
 *        самой странице избранного для удаления есть отдельная кнопка.
 * @returns {HTMLElement}
 */
export function createCourseCard(course, options = {}) {
    const {
        href = `/pages/course-details.html?id=${course.id}`,
        showStatus = false,
        footer = null,
        wishlist: withWishlist = false
    } = options;

    const card = document.createElement('article');
    card.className = 'card card-interactive course-card';

    card.appendChild(buildThumbnail(course, showStatus, withWishlist));

    const body = document.createElement('div');
    body.className = 'card-body course-card-body';

    // Уровень + длительность
    const meta = document.createElement('div');
    meta.className = 'course-card-meta';

    const levelBadge = document.createElement('span');
    levelBadge.className = 'badge badge-primary';
    levelBadge.textContent = format.level(course.level);

    const durationLabel = document.createElement('span');
    durationLabel.className = 'text-xs text-muted';
    durationLabel.textContent =
        `${format.duration(course.durationMinutes)} · ${format.plural(course.lessonCount, 'units.lesson')}`;

    meta.append(levelBadge, durationLabel);
    body.appendChild(meta);

    // Заголовок — ссылка на страницу курса
    const title = document.createElement('h3');
    title.className = 'course-card-title';

    const titleLink = document.createElement('a');
    titleLink.href = href;
    // textContent, а не innerHTML: название приходит из базы и может содержать
    // символы вроде < или &, которые нельзя вставлять как разметку.
    titleLink.textContent = course.title;
    title.appendChild(titleLink);
    body.appendChild(title);

    const description = document.createElement('p');
    description.className = 'course-card-description';
    description.textContent = course.shortDescription ?? '';
    body.appendChild(description);

    if (course.instructorName) {
        const instructor = document.createElement('p');
        instructor.className = 'course-card-instructor';
        instructor.textContent = course.instructorName;
        body.appendChild(instructor);
    }

    body.appendChild(buildStats(course));
    body.appendChild(footer ?? buildFooter(course, href));

    card.appendChild(body);
    return card;
}

function buildThumbnail(course, showStatus, withWishlist) {
    const wrapper = document.createElement('div');
    wrapper.className = 'course-card-thumb';

    if (course.thumbnailUrl) {
        const image = document.createElement('img');
        image.src = course.thumbnailUrl;
        image.alt = '';
        image.loading = 'lazy';
        // Файлов картинок в проекте нет — если путь битый, показываем градиент,
        // а не сломанную иконку изображения. Оттенок красим только тут, ПОСЛЕ
        // подтверждённой ошибки загрузки — если применить его сразу и картинка
        // всё же загрузится, чужой hue-rotate исказил бы цвета настоящего фото.
        image.addEventListener('error', () => {
            image.remove();
            wrapper.style.filter = `hue-rotate(${hueFromId(course.id)}deg)`;
        });
        wrapper.appendChild(image);
    } else {
        // Своей картинки нет вовсе — красим градиент сразу. Каждый курс получает
        // СВОЙ оттенок (по хэшу id), а не одну и ту же заливку на весь каталог:
        // одинаковые близнецы-обложки — первое, что выдаёт шаблонность.
        wrapper.style.filter = `hue-rotate(${hueFromId(course.id)}deg)`;
    }

    const bestseller = buildBestsellerBadge(course);
    if (bestseller) {
        wrapper.appendChild(bestseller);
    }

    if (showStatus) {
        const status = document.createElement('span');
        status.className = `badge course-card-status ${statusBadgeClass(course.status)}`;
        status.textContent = format.status(course.status);
        wrapper.appendChild(status);
    }

    // Сердечко видят гость и студент; преподавателю и админу избранное не положено.
    if (withWishlist && wishlist.isAvailable()) {
        wrapper.appendChild(buildWishlistButton(course));
    }

    return wrapper;
}

/**
 * Кнопка «в избранное».
 *
 * Это <button>, а не значок с обработчиком на div: он попадает в обход по Tab,
 * срабатывает на Enter и Пробел и объявляется скринридером как кнопка.
 * aria-pressed сообщает состояние — без него незрячий пользователь услышал бы
 * одно и то же «в избранное» и до нажатия, и после.
 */
function buildWishlistButton(course) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'course-card-wishlist';
    button.dataset.courseId = course.id;

    const icon = document.createElement('span');
    icon.setAttribute('aria-hidden', 'true');
    button.appendChild(icon);

    const paint = (isIn) => {
        button.classList.toggle('is-active', isIn);
        button.setAttribute('aria-pressed', String(isIn));
        // Подпись включает название курса: на странице таких кнопок дюжина,
        // и «Добавить в избранное» без уточнения ничего не различает.
        button.setAttribute('aria-label',
            `${t(isIn ? 'actions.removeFromWishlist' : 'actions.addToWishlist')}: ${course.title}`);
        button.title = t(isIn ? 'actions.removeFromWishlist' : 'actions.addToWishlist');
        icon.textContent = isIn ? '♥' : '♡';
    };

    paint(wishlist.isInWishlist(course.id));

    // Тот же курс может быть на странице дважды (на главной — и в «Рекомендуемых»,
    // и в «Популярных»). Перекрашиваем обе кнопки, а не только нажатую.
    wishlist.onChange((changedId, isIn) => {
        if (changedId === course.id) {
            paint(isIn);
        }
    });

    button.addEventListener('click', async (event) => {
        // Карточка кликабельна целиком — не даём нажатию уйти на ссылку курса.
        event.preventDefault();
        event.stopPropagation();

        // Гость: хранить отметку негде, но и терять её нельзя — запоминаем курс
        // и уводим на вход. После входа он доедет в избранное сам (flushPending),
        // и пользователь вернётся ровно на эту же страницу.
        if (wishlist.isGuest()) {
            window.location.href = wishlist.rememberPending(course.id);
            return;
        }

        button.disabled = true;

        try {
            const isIn = await wishlist.toggle(course.id);
            toast.success(t(isIn ? 'actions.addedToWishlist' : 'student.wishlistRemoved'));
        } catch (error) {
            toast.fromApiError(error);
        } finally {
            button.disabled = false;
        }
    });

    return button;
}

/**
 * Хэш id курса → угол поворота оттенка (0–359). Одна и та же карточка всегда
 * получает один и тот же цвет (детерминированно от id), но разные курсы
 * расходятся по палитре, а не повторяют один и тот же фиолетовый градиент.
 */
function hueFromId(id) {
    let hash = 0;
    for (let i = 0; i < id.length; i += 1) {
        hash = (hash * 31 + id.charCodeAt(i)) % 360;
    }
    return hash;
}

/**
 * «Лидер продаж» — не выдуманная метка, а честный вывод из реальных цифр
 * курса (раздел 68 ТЗ — никакой fake statistics): высокий рейтинг И заметное
 * число отзывов вместе. Один высокий балл на двух отзывах ничего не значит,
 * поэтому оба условия обязательны.
 */
function buildBestsellerBadge(course) {
    const qualifies = course.reviewCount >= 2 && course.averageRating >= 4.5;
    if (!qualifies) {
        return null;
    }

    const badge = document.createElement('span');
    badge.className = 'badge course-card-bestseller';
    badge.textContent = t('course.bestseller');
    return badge;
}

function statusBadgeClass(status) {
    switch (status) {
        case 'Published': return 'badge-success';
        case 'Archived': return 'badge-warning';
        default: return 'badge-neutral';
    }
}

function buildStats(course) {
    const stats = document.createElement('div');
    stats.className = 'course-card-stats';

    // Курс без отзывов честно показывает «нет отзывов», а не «0.0 ★»:
    // ноль звёзд выглядит как плохая оценка, хотя оценок просто нет.
    if (course.reviewCount > 0) {
        const { filled, empty } = format.stars(course.averageRating);

        const rating = document.createElement('span');
        rating.className = 'rating';
        rating.setAttribute('aria-label',
            `${t('course.rating')}: ${format.rating(course.averageRating)} / 5`);

        const starsElement = document.createElement('span');
        starsElement.className = 'rating-stars';
        starsElement.setAttribute('aria-hidden', 'true');
        starsElement.innerHTML = '★'.repeat(filled) + `<span class="star-empty">${'★'.repeat(empty)}</span>`;

        const value = document.createElement('span');
        value.className = 'text-muted';
        value.textContent = `${format.rating(course.averageRating)} (${course.reviewCount})`;

        rating.append(starsElement, value);
        stats.appendChild(rating);
    } else {
        const noReviews = document.createElement('span');
        noReviews.className = 'text-xs text-muted';
        noReviews.textContent = t('course.noReviews');
        stats.appendChild(noReviews);
    }

    const students = document.createElement('span');
    students.className = 'text-sm text-muted';
    students.textContent = `👥 ${format.number(course.enrollmentCount)}`;
    students.setAttribute('aria-label', `${t('course.students')}: ${course.enrollmentCount}`);
    stats.appendChild(students);

    return stats;
}

function buildFooter(course, href) {
    const footer = document.createElement('div');
    footer.className = 'course-card-footer';

    const price = document.createElement('strong');
    price.className = 'course-card-price';
    price.textContent = format.price(course.price);

    const action = document.createElement('a');
    action.className = 'btn btn-primary btn-sm';
    action.href = href;
    action.textContent = t('actions.viewCourse');

    footer.append(price, action);
    return footer;
}

/**
 * Рисует список карточек в контейнер. Возвращает число отрисованных —
 * вызывающий код по нему решает, показывать ли пустое состояние.
 */
export function renderCourseCards(container, courses, options = {}) {
    if (!container) {
        return 0;
    }

    container.innerHTML = '';

    const fragment = document.createDocumentFragment();
    courses.forEach((course) => fragment.appendChild(createCourseCard(course, options)));
    container.appendChild(fragment);

    return courses.length;
}
