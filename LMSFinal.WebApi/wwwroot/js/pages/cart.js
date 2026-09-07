/**
 * Страница корзины.
 *
 * Корзина хранит только id курсов (cart.js в components) — здесь они
 * подгружаются по одному через GET /api/courses/{id}, тот же эндпоинт, что
 * и страница курса. Доступна и гостю: он может собрать корзину, а на «Оформить»
 * его отправят на вход и вернут сюда же (returnUrl), корзина тем временем
 * никуда не денется — она просто лежит в localStorage.
 */

import { api } from '../api.js';
import { getApiLanguage, t } from '../localization.js';
import { isAuthenticated } from '../auth.js';
import { loader } from '../components/loader.js';
import { emptyState } from '../components/empty-state.js';
import { toast } from '../components/toast.js';
import * as format from '../format.js';
import * as cart from '../components/cart.js';
import { onReady } from '../ready.js';

const $ = (id) => document.getElementById(id);

let courses = [];

async function load() {
    const container = $('cart-items');
    const ids = cart.getIds();

    if (!ids.length) {
        renderEmpty();
        return;
    }

    loader.skeleton(container, { count: Math.min(ids.length, 4), variant: 'row' });

    // Курс мог быть удалён/снят с публикации после того, как его добавили
    // в корзину — такие запросы просто отфильтровываем, а не роняем страницу.
    const results = await Promise.allSettled(
        ids.map((id) => api.get(`/courses/${id}`, { query: { lang: getApiLanguage() } }))
    );

    const loaded = results
        .map((result, index) => (result.status === 'fulfilled' ? result.value : null))
        .filter(Boolean);

    // Курсы, которые не удалось загрузить, тихо убираем из корзины —
    // держать в ней мёртвые ссылки незачем.
    const loadedIds = new Set(loaded.map((c) => c.id));
    ids.filter((id) => !loadedIds.has(id)).forEach((id) => cart.remove(id));

    courses = loaded;
    render();
}

function renderEmpty() {
    $('cart-summary').classList.add('hidden');
    emptyState.render($('cart-items'), {
        icon: '🛒',
        title: t('cart.empty'),
        action: { label: t('cart.browseCourses'), href: '/pages/courses.html' }
    });
}

function render() {
    if (!courses.length) {
        renderEmpty();
        return;
    }

    $('cart-summary').classList.remove('hidden');

    const container = $('cart-items');
    container.innerHTML = '';
    courses.forEach((course) => container.appendChild(buildRow(course)));

    const total = courses.reduce((sum, course) => sum + (course.price ?? 0), 0);
    $('cart-total').textContent = format.price(total);
}

function buildRow(course) {
    const row = document.createElement('article');
    row.className = 'cart-item card';

    const body = document.createElement('div');
    body.className = 'card-body cart-item-body';

    const thumb = document.createElement('div');
    thumb.className = 'cart-item-thumb';
    if (course.thumbnailUrl) {
        const image = document.createElement('img');
        image.src = course.thumbnailUrl;
        image.alt = '';
        image.addEventListener('error', () => image.remove());
        thumb.appendChild(image);
    }

    const info = document.createElement('div');
    info.className = 'cart-item-info';

    const title = document.createElement('a');
    title.className = 'cart-item-title';
    title.href = `/pages/course-details.html?id=${course.id}`;
    title.textContent = course.title;

    const instructor = document.createElement('p');
    instructor.className = 'text-sm text-muted';
    instructor.textContent = course.instructorName ?? '';

    info.append(title, instructor);

    const price = document.createElement('strong');
    price.className = 'cart-item-price';
    price.textContent = format.price(course.price);

    const removeButton = document.createElement('button');
    removeButton.type = 'button';
    removeButton.className = 'cart-item-remove';
    removeButton.setAttribute('aria-label', `${t('cart.remove')}: ${course.title}`);
    removeButton.textContent = '✕';
    removeButton.addEventListener('click', () => {
        cart.remove(course.id);
        courses = courses.filter((c) => c.id !== course.id);
        render();
    });

    body.append(thumb, info, price, removeButton);
    row.appendChild(body);
    return row;
}

async function checkout() {
    if (!isAuthenticated()) {
        const returnUrl = encodeURIComponent('/pages/cart.html');
        window.location.href = `/pages/login.html?returnUrl=${returnUrl}`;
        return;
    }

    const button = $('checkout-button');
    loader.button(button, true);

    const results = await Promise.allSettled(
        courses.map((course) => api.post(`/courses/${course.id}/enroll`))
    );

    const succeeded = results.filter((r) => r.status === 'fulfilled').length;

    // Успешно записанные курсы убираем из корзины — уже записавшийся не
    // должен дальше маячить в списке «хочу записаться».
    courses.forEach((course, index) => {
        if (results[index].status === 'fulfilled') {
            cart.remove(course.id);
        }
    });

    loader.button(button, false);

    if (succeeded === courses.length) {
        toast.success(t('cart.checkoutSuccess', { count: succeeded }));
        window.location.href = '/pages/student/my-courses.html';
    } else if (succeeded > 0) {
        toast.warning(t('cart.checkoutPartial'));
        await load();
    } else {
        toast.error(t('cart.checkoutPartial'));
        await load();
    }
}

function bindActions() {
    $('checkout-button').addEventListener('click', checkout);
}

onReady(async () => {
    bindActions();
    await load();
});
