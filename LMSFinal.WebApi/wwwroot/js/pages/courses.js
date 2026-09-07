/**
 * Каталог курсов (раздел 8 ТЗ): поиск, фильтры, сортировка, пагинация.
 *
 * Два принципа, на которых держится вся страница:
 *
 * 1. **Состояние живёт в URL.** Фильтры пишутся в query-строку, а не только в
 *    переменные. Поэтому ссылку на отфильтрованный каталог можно скинуть, открыть
 *    в новой вкладке и вернуться назад кнопкой браузера — состояние восстановится.
 *    Ссылки с лендинга (`?categoryId=…`) работают по той же причине.
 *
 * 2. **Фильтрация и сортировка — на сервере.** Каталог постраничный: отсортировать
 *    12 курсов текущей страницы — не то же самое, что отсортировать весь каталог
 *    и показать первые 12.
 */

import { api } from '../api.js';
import { getApiLanguage, onLanguageChange, t } from '../localization.js';
import { loader } from '../components/loader.js';
import { emptyState } from '../components/empty-state.js';
import { renderCourseCards } from '../components/course-card.js';
import { ensureLoaded as loadWishlist } from '../components/wishlist.js';
import { renderPagination } from '../components/pagination.js';
import * as format from '../format.js';
import { onReady } from '../ready.js';

const PAGE_SIZE = 12;
const SEARCH_DEBOUNCE_MS = 350;

/** Текущее состояние каталога. Единственный источник правды для запроса. */
const state = {
    searchTerm: '',
    categoryId: '',
    level: '',
    price: '',        // '', 'free', 'paid'
    sortBy: 'Newest',
    page: 1
};

let categories = [];
let searchTimer = null;

const $ = (id) => document.getElementById(id);

// ---------------------------------------------------------------------------
// Загрузка
// ---------------------------------------------------------------------------

async function loadCategories() {
    try {
        categories = await api.get('/categories', {
            query: { lang: getApiLanguage() },
            anonymous: true
        });

        renderCategoryFilters();
    } catch {
        // Каталог должен работать и без списка категорий — фильтр просто
        // останется с одним пунктом «все».
        categories = [];
    }
}

async function loadCourses() {
    const grid = $('course-grid');
    loader.skeleton(grid, { count: 6 });

    try {
        const result = await api.get('/courses', {
            query: buildQuery(),
            anonymous: true
        });

        renderResults(result);
    } catch (error) {
        $('results-summary').textContent = '';
        $('pagination').innerHTML = '';
        emptyState.error(grid, {
            message: error?.isNetworkError ? t('states.networkError') : error?.message,
            onRetry: loadCourses
        });
    } finally {
        $('search-spinner').classList.add('hidden');
    }
}

/**
 * Превращает состояние в параметры запроса.
 * Фильтр цены на бэкенде выражен диапазоном minPrice/maxPrice, поэтому
 * «только бесплатные» — это maxPrice=0, а «только платные» — minPrice чуть больше нуля.
 */
function buildQuery() {
    const query = {
        searchTerm: state.searchTerm || undefined,
        categoryId: state.categoryId || undefined,
        level: state.level || undefined,
        sortBy: state.sortBy,
        page: state.page,
        pageSize: PAGE_SIZE,
        lang: getApiLanguage()
    };

    if (state.price === 'free') {
        query.maxPrice = 0;
    } else if (state.price === 'paid') {
        query.minPrice = 0.01;
    }

    return query;
}

// ---------------------------------------------------------------------------
// Отрисовка
// ---------------------------------------------------------------------------

function renderResults(result) {
    const grid = $('course-grid');
    const items = result.items ?? [];

    $('results-summary').textContent = items.length === 0
        ? ''
        : (result.totalCount === 1
            ? t('catalog.resultsOne')
            : t('catalog.resultsFound', { count: format.number(result.totalCount) }));

    if (items.length === 0) {
        // Пустой результат объясняем и даём выход — иначе выглядит как поломка.
        emptyState.render(grid, {
            icon: '🔍',
            title: t('states.noCourses'),
            text: t('states.noCoursesHint'),
            action: hasActiveFilters() ? { label: t('actions.clearFilters'), onClick: clearFilters } : null
        });
        $('pagination').innerHTML = '';
        return;
    }

    renderCourseCards(grid, items, { wishlist: true });

    renderPagination($('pagination'), {
        page: result.page,
        totalPages: result.totalPages,
        onChange: (page) => {
            state.page = page;
            syncUrl();
            loadCourses();
            // Иначе после переключения страницы пользователь остаётся внизу
            // и видит новый список «с середины».
            document.getElementById('main').scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    });
}

function renderCategoryFilters() {
    const container = $('category-filters');

    // Первый пункт «все категории» уже лежит в разметке — дописываем остальные.
    container.querySelectorAll('.filter-option:not(:first-child)').forEach((element) => element.remove());

    categories.forEach((category) => {
        const label = document.createElement('label');
        label.className = 'filter-option';

        const input = document.createElement('input');
        input.type = 'radio';
        input.name = 'categoryId';
        input.value = category.id;
        input.checked = state.categoryId === category.id;

        const text = document.createElement('span');
        text.textContent = category.name;

        label.append(input, text);
        container.appendChild(label);
    });
}

/** Чипы активных фильтров: видно, что сузило выдачу, и каждый снимается отдельно. */
function renderActiveFilters() {
    const container = $('active-filters');
    container.innerHTML = '';

    const chips = [];

    if (state.searchTerm) {
        chips.push({ label: `“${state.searchTerm}”`, clear: () => { state.searchTerm = ''; $('search').value = ''; } });
    }

    if (state.categoryId) {
        const category = categories.find((item) => item.id === state.categoryId);
        chips.push({ label: category?.name ?? t('catalog.category'), clear: () => { state.categoryId = ''; } });
    }

    if (state.level) {
        chips.push({ label: format.level(state.level), clear: () => { state.level = ''; } });
    }

    if (state.price) {
        chips.push({
            label: t(state.price === 'free' ? 'catalog.priceFree' : 'catalog.pricePaid'),
            clear: () => { state.price = ''; }
        });
    }

    chips.forEach((chip) => container.appendChild(buildChip(chip)));
}

function buildChip({ label, clear }) {
    const element = document.createElement('span');
    element.className = 'filter-chip';

    const text = document.createElement('span');
    text.textContent = label;

    const button = document.createElement('button');
    button.type = 'button';
    button.setAttribute('aria-label', `${t('actions.delete')}: ${label}`);
    button.textContent = '×';
    button.addEventListener('click', () => {
        clear();
        state.page = 1;
        syncInputs();
        applyChanges();
    });

    element.append(text, button);
    return element;
}

// ---------------------------------------------------------------------------
// Состояние и URL
// ---------------------------------------------------------------------------

function hasActiveFilters() {
    return Boolean(state.searchTerm || state.categoryId || state.level || state.price);
}

function clearFilters() {
    state.searchTerm = '';
    state.categoryId = '';
    state.level = '';
    state.price = '';
    state.page = 1;

    $('search').value = '';
    syncInputs();
    applyChanges();
}

/** Читает состояние из query-строки — для ссылок с лендинга и кнопки «назад». */
function readStateFromUrl() {
    const params = new URLSearchParams(window.location.search);

    state.searchTerm = params.get('search') ?? '';
    state.categoryId = params.get('categoryId') ?? '';
    state.level = params.get('level') ?? '';
    state.price = params.get('price') ?? '';
    state.sortBy = params.get('sortBy') ?? 'Newest';
    state.page = Math.max(1, Number(params.get('page')) || 1);
}

/**
 * Пишет состояние в адресную строку через replaceState: каждый символ в поиске
 * не должен добавлять запись в историю, иначе кнопка «назад» будет отматывать
 * ввод по буквам вместо возврата на предыдущую страницу.
 */
function syncUrl() {
    const params = new URLSearchParams();

    if (state.searchTerm) params.set('search', state.searchTerm);
    if (state.categoryId) params.set('categoryId', state.categoryId);
    if (state.level) params.set('level', state.level);
    if (state.price) params.set('price', state.price);
    if (state.sortBy !== 'Newest') params.set('sortBy', state.sortBy);
    if (state.page > 1) params.set('page', String(state.page));

    const query = params.toString();
    window.history.replaceState(null, '', query ? `?${query}` : window.location.pathname);
}

/** Приводит элементы формы в соответствие состоянию (после чтения URL или сброса). */
function syncInputs() {
    $('search').value = state.searchTerm;
    $('sort').value = state.sortBy;

    document.querySelectorAll('input[name="categoryId"]').forEach((input) => {
        input.checked = input.value === state.categoryId;
    });

    document.querySelectorAll('input[name="level"]').forEach((input) => {
        input.checked = input.value === state.level;
    });

    document.querySelectorAll('input[name="price"]').forEach((input) => {
        input.checked = input.value === state.price;
    });
}

function applyChanges() {
    renderActiveFilters();
    syncUrl();
    loadCourses();
}

// ---------------------------------------------------------------------------
// События
// ---------------------------------------------------------------------------

function bindEvents() {
    // Debounce: запрос уходит не на каждую букву, а через паузу после набора.
    $('search').addEventListener('input', (event) => {
        state.searchTerm = event.target.value.trim();
        state.page = 1;

        $('search-spinner').classList.remove('hidden');
        clearTimeout(searchTimer);
        searchTimer = setTimeout(applyChanges, SEARCH_DEBOUNCE_MS);
    });

    $('sort').addEventListener('change', (event) => {
        state.sortBy = event.target.value;
        state.page = 1;
        applyChanges();
    });

    // Делегирование: категории добавляются в DOM позже, отдельные обработчики
    // пришлось бы навешивать заново после каждой перерисовки.
    document.querySelector('.filters').addEventListener('change', (event) => {
        const input = event.target;

        if (!['categoryId', 'level', 'price'].includes(input.name)) {
            return;
        }

        state[input.name] = input.value;
        state.page = 1;
        applyChanges();
    });

    $('clear-filters').addEventListener('click', clearFilters);

    $('filters-toggle').addEventListener('click', (event) => {
        const isOpen = $('filters-body').classList.toggle('is-open');
        event.currentTarget.setAttribute('aria-expanded', String(isOpen));
    });
}

onReady(async () => {
    readStateFromUrl();
    bindEvents();

    // Избранное грузим ДО карточек: иначе первая отрисовка покажет пустые
    // сердечки даже на отложенных курсах, и они перекрасятся рывком.
    // Параллельно с категориями — запросы независимы.
    await Promise.all([loadCategories(), loadWishlist()]);

    syncInputs();
    renderActiveFilters();
    await loadCourses();

    // Контент курсов приходит переведённым с сервера (?lang=), поэтому при смене
    // языка список нужно перезапросить, а не просто перерисовать подписи.
    onLanguageChange(async () => {
        await loadCategories();
        syncInputs();
        renderActiveFilters();
        await loadCourses();
    });
});
