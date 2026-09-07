/**
 * Логика лендинга (раздел 24 ТЗ).
 *
 * Все блоки наполняются из API — категории, курсы и цифры статистики.
 * Захардкоженных данных на странице нет (раздел 68 ТЗ).
 *
 * Публичные эндпоинты вызываются с anonymous: true — гость на главной не должен
 * получать 401 просто потому, что у него нет токена.
 */

import { api } from '../api.js';
import { getApiLanguage, onLanguageChange, t } from '../localization.js';
import { loader } from '../components/loader.js';
import { emptyState } from '../components/empty-state.js';
import { renderCourseCards } from '../components/course-card.js';
import { ensureLoaded as loadWishlist } from '../components/wishlist.js';
import { renderCarousel } from '../components/carousel.js';
import * as format from '../format.js';
import { onReady } from '../ready.js';

/** Иконки категорий по slug: в базе хранится IconUrl, но файлов иконок в проекте нет. */
const CATEGORY_ICONS = {
    programming: '💻',
    backend: '⚙️',
    frontend: '🎨',
    'web-development': '🌐',
    database: '🗄️',
    ai: '🤖',
    cybersecurity: '🔐',
    design: '✏️',
    business: '📊',
    languages: '🗣️'
};

const elements = {
    categories: () => document.getElementById('categories'),
    featured: () => document.getElementById('featured-courses'),
    popular: () => document.getElementById('popular-courses'),
    stats: () => document.getElementById('platform-stats')
};

async function loadAll() {
    // Секции грузятся параллельно: они независимы, и последовательные запросы
    // просто складывали бы задержки друг к другу.
    await Promise.all([loadCategories(), loadCourses(), loadCarousel()]);
}

async function loadCarousel() {
    try {
        const slides = await api.get('/hero-slides', { anonymous: true });
        renderCarousel(document.getElementById('hero-carousel'), slides);
    } catch {
        // Баннер — не критичный контент: если не загрузился, просто прячем секцию.
        document.getElementById('hero-carousel')?.classList.add('hidden');
    }
}

async function loadCategories() {
    const container = elements.categories();
    loader.skeleton(container, { count: 6, variant: 'row' });

    try {
        const categories = await api.get('/categories', {
            query: { lang: getApiLanguage() },
            anonymous: true
        });

        if (!categories.length) {
            emptyState.render(container, { icon: '🗂️', title: t('states.empty') });
            return;
        }

        container.innerHTML = '';
        categories.forEach((category) => container.appendChild(buildCategoryTile(category)));

        updateStat('categories', categories.length);
    } catch (error) {
        emptyState.error(container, { message: errorMessage(error), onRetry: loadCategories });
    }
}

function buildCategoryTile(category) {
    const tile = document.createElement('a');
    tile.className = 'category-tile';
    // Каталог откроется уже с примененным фильтром по категории (Phase 21).
    tile.href = `/pages/courses.html?categoryId=${category.id}`;

    const icon = document.createElement('span');
    icon.className = 'category-tile-icon';
    icon.setAttribute('aria-hidden', 'true');
    icon.textContent = CATEGORY_ICONS[category.slug] ?? '📁';

    const name = document.createElement('span');
    name.className = 'category-tile-name';
    name.textContent = category.name;

    tile.append(icon, name);
    return tile;
}

async function loadCourses() {
    const featured = elements.featured();
    const popular = elements.popular();

    loader.skeleton(featured, { count: 3 });
    loader.skeleton(popular, { count: 3 });

    try {
        // Один запрос на всю страницу: сортировки по популярности в API пока нет,
        // поэтому берём каталог целиком и раскладываем на два блока здесь.
        // Когда в Phase 21 появится параметр sortBy, это место станет двумя
        // запросами с разной сортировкой и без клиентской логики.
        const page = await api.get('/courses', {
            query: { pageSize: 50, lang: getApiLanguage() },
            anonymous: true
        });

        const courses = page.items ?? [];

        if (!courses.length) {
            const empty = { icon: '📚', title: t('states.noCourses'), text: t('states.noCoursesHint') };
            emptyState.render(featured, empty);
            emptyState.render(popular, empty);
            return;
        }

        // «Рекомендуемые» — с лучшим рейтингом среди тех, у кого отзывы вообще есть.
        const byRating = [...courses]
            .filter((course) => course.reviewCount > 0)
            .sort((a, b) => b.averageRating - a.averageRating || b.reviewCount - a.reviewCount);

        // «Популярные» — по числу записавшихся.
        const byStudents = [...courses].sort((a, b) => b.enrollmentCount - a.enrollmentCount);

        renderCourseCards(featured, (byRating.length ? byRating : courses).slice(0, 3), { wishlist: true });
        renderCourseCards(popular, byStudents.slice(0, 3), { wishlist: true });

        updatePlatformStats(courses);
    } catch (error) {
        const retry = { message: errorMessage(error), onRetry: loadCourses };
        emptyState.error(featured, retry);
        emptyState.error(popular, retry);
    }
}

/**
 * Цифры под hero считаются по реально полученному каталогу.
 * Отдельного эндпоинта «статистика платформы» в API нет, а выдумывать числа нельзя —
 * поэтому показываем только то, что действительно можно посчитать.
 */
function updatePlatformStats(courses) {
    const students = courses.reduce((sum, course) => sum + course.enrollmentCount, 0);
    const lessons = courses.reduce((sum, course) => sum + course.lessonCount, 0);
    const instructors = new Set(courses.map((course) => course.instructorName).filter(Boolean)).size;

    updateStat('courses', courses.length);
    updateStat('students', students);
    updateStat('lessons', lessons);
    updateStat('instructors', instructors);
}

function updateStat(name, value) {
    const element = elements.stats()?.querySelector(`[data-stat="${name}"]`);

    if (element) {
        element.textContent = format.number(value);
    }
}

function errorMessage(error) {
    return error?.isNetworkError ? t('states.networkError') : error?.message;
}

onReady(() => {
    // Избранное — до карточек, см. комментарий в courses.js.
    loadWishlist().then(loadAll);

    // Контент курсов и категорий приходит с сервера уже переведённым (?lang=),
    // поэтому при смене языка блоки нужно перезапросить, а не просто перерисовать.
    onLanguageChange(loadAll);
});
