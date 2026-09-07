/**
 * Избранное студента (раздел 18.1 ТЗ).
 *
 * Удаление сделано оптимистично: карточка исчезает сразу, не дожидаясь ответа
 * сервера — иначе интерфейс «залипает» на время запроса. Если запрос упадёт,
 * список перезагружается, и карточка возвращается на место.
 */

import { api } from '../../api.js';
import { getApiLanguage, t } from '../../localization.js';
import { loader } from '../../components/loader.js';
import { emptyState } from '../../components/empty-state.js';
import { createCourseCard } from '../../components/course-card.js';
import { toast } from '../../components/toast.js';
import { startStudentPage, showLoadError } from './common.js';

const $ = (id) => document.getElementById(id);

async function load() {
    loader.skeleton($('wishlist-grid'), { count: 3 });

    try {
        const items = await api.get('/wishlist', { query: { lang: getApiLanguage() } });
        render(items);
    } catch (error) {
        showLoadError($('wishlist-grid'), error, load);
    }
}

function render(items) {
    const container = $('wishlist-grid');
    container.innerHTML = '';

    if (!items.length) {
        emptyState.render(container, {
            icon: '♡',
            title: t('student.wishlistEmpty'),
            text: t('student.wishlistEmptyHint'),
            action: { label: t('actions.explore'), href: '/pages/courses.html' }
        });
        return;
    }

    items.forEach((item) => container.appendChild(buildWishlistCard(item)));
}

function buildWishlistCard(item) {
    const footer = document.createElement('div');
    footer.className = 'course-card-footer';

    const price = document.createElement('strong');
    price.className = 'course-card-price';
    price.textContent = item.course.price > 0
        ? new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(item.course.price)
        : t('course.free');

    const actions = document.createElement('div');
    actions.className = 'flex gap-2';

    const open = document.createElement('a');
    open.className = 'btn btn-primary btn-sm';
    open.href = `/pages/course-details.html?id=${item.course.id}`;
    open.textContent = t('actions.viewCourse');

    const remove = document.createElement('button');
    remove.type = 'button';
    remove.className = 'btn btn-ghost btn-sm';
    remove.setAttribute('aria-label', `${t('actions.remove')}: ${item.course.title}`);
    remove.textContent = '✕';
    remove.addEventListener('click', (event) => removeFromWishlist(item, event.currentTarget));

    actions.append(open, remove);
    footer.append(price, actions);

    return createCourseCard(item.course, { footer });
}

async function removeFromWishlist(item, button) {
    const card = button.closest('.course-card');
    card.style.display = 'none';   // оптимистичное скрытие

    try {
        await api.delete(`/wishlist/${item.course.id}`);
        toast.success(t('student.wishlistRemoved'));

        // Перезагружаем: если карточка была последней, покажется пустое состояние.
        await load();
    } catch (error) {
        card.style.display = '';    // откат
        toast.fromApiError(error);
    }
}

startStudentPage(load);
