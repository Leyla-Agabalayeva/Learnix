/**
 * Выбор курса — общий для страниц «Аналитика» и «Студенты».
 *
 * Обе работают в контексте ОДНОГО курса: сводных эндпоинтов «аналитика по всем
 * курсам» и «все мои студенты» в API нет, да и по сути такие цифры смешивали бы
 * разные курсы в одну кучу — средний прогресс по десяти разным курсам ничего
 * не говорит ни об одном из них.
 *
 * Выбранный курс пишется в URL (?courseId=…), поэтому ссылку на аналитику
 * конкретного курса можно сохранить, и переход с карточки курса работает.
 */

import { api } from '../../api.js';
import { getApiLanguage, t } from '../../localization.js';

/**
 * Актуальный список курсов. Хранится на уровне модуля, а не в замыкании
 * обработчика: setupCoursePicker вызывается заново при смене языка, и слушатель,
 * захвативший старый массив, показывал бы названия на прежнем языке.
 */
let loadedCourses = [];

/**
 * @param {(courseId: string, course: object) => void} onChange
 * @returns {Promise<object[]>} загруженные курсы
 */
export async function setupCoursePicker(onChange) {
    const select = document.getElementById('course-select');
    const courses = await api.get('/courses/my', { query: { lang: getApiLanguage() } });

    loadedCourses = courses;
    select.innerHTML = '';

    if (!courses.length) {
        select.disabled = true;

        const option = document.createElement('option');
        option.textContent = t('instructor.noCoursesTitle');
        select.appendChild(option);

        onChange(null, null);
        return courses;
    }

    courses.forEach((course) => {
        const option = document.createElement('option');
        option.value = course.id;
        // Статус в подписи: преподаватель сразу видит, что смотрит черновик,
        // у которого цифры закономерно нулевые.
        option.textContent = course.status === 'Published'
            ? course.title
            : `${course.title} · ${t(`course.status${course.status}`)}`;
        select.appendChild(option);
    });

    // Курс из URL — если сюда пришли по ссылке «Аналитика» с карточки курса.
    const requested = new URLSearchParams(window.location.search).get('courseId');
    const initial = courses.find((course) => course.id === requested) ?? courses[0];

    select.value = initial.id;

    // Обработчик вешаем один раз: setupCoursePicker вызывается заново при смене
    // языка, и без этой проверки слушатели накапливались бы.
    if (!select.dataset.bound) {
        select.dataset.bound = 'true';

        select.addEventListener('change', () => {
            const course = loadedCourses.find((item) => item.id === select.value);
            syncUrl(select.value);
            onChange(select.value, course);
        });
    }

    onChange(initial.id, initial);
    return courses;
}

function syncUrl(courseId) {
    const params = new URLSearchParams(window.location.search);
    params.set('courseId', courseId);
    window.history.replaceState(null, '', `?${params.toString()}`);
}
