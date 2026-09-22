

import { api } from '../../api.js';
import { getApiLanguage, t } from '../../localization.js';
import { toast } from '../../components/toast.js';
import { loader } from '../../components/loader.js';
import { createTranslationTabs } from '../../components/translation-tabs.js';
import { showFormErrors, clearFormErrors } from '../form-errors.js';
import { startInstructorPage } from './common.js';

const $ = (id) => document.getElementById(id);

let translations = null;

async function load() {
    await loadCategories();

    translations = createTranslationTabs($('translations'), [
        { name: 'title', labelKey: 'builder.fieldTitle', required: true },
        { name: 'shortDescription', labelKey: 'builder.fieldShort', type: 'textarea', required: true },
        { name: 'description', labelKey: 'builder.fieldDescription', type: 'textarea', required: true },
        { name: 'whatYouWillLearn', labelKey: 'builder.fieldLearn', type: 'textarea' }
    ]);
}

async function loadCategories() {
    const select = $('categoryId');

    try {
        const categories = await api.get('/categories', { query: { lang: getApiLanguage() } });

        select.innerHTML = '<option value="" data-i18n="builder.selectCategory"></option>';
        categories.forEach((category) => {
            const option = document.createElement('option');
            option.value = category.id;
            option.textContent = category.name;
            select.appendChild(option);
        });
    } catch (error) {
        showFormError(error.isNetworkError ? t('states.networkError') : error.message);
    }
}

function bindForm() {
    bindThumbnailUpload();

    $('course-form').addEventListener('submit', async (event) => {
        event.preventDefault();

        const form = event.currentTarget;
        const button = $('submit');

        clearFormErrors(form);
        hideFormError();

        // Проверяем до отправки: без единого заполненного языка курс не имеет
        // названия ни на одном языке, и сервер всё равно отклонит публикацию.
        if (!translations.validate()) {
            showFormError(t('builder.atLeastOneLanguage'));
            return;
        }

        loader.button(button, true);

        try {
            const course = await api.post('/courses', buildPayload());
            toast.success(t('builder.courseCreated'));

            // Сразу в конструктор: курс без модулей опубликовать нельзя,
            // поэтому логичный следующий шаг — наполнить его.
            window.location.href = `/pages/instructor/course-builder.html?id=${course.id}`;
        } catch (error) {
            if (error.isValidationError) {
                showFormErrors(form, error.errors);
            } else {
                showFormError(error.isNetworkError ? t('states.networkError') : error.message);
            }

            loader.button(button, false);
        }
    });
}

function bindThumbnailUpload() {
    $('thumbnail-upload').addEventListener('click', () => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/png, image/jpeg, image/webp, image/svg+xml';
        input.style.display = 'none';

        input.addEventListener('change', async () => {
            const file = input.files?.[0];
            if (!file) {
                return;
            }

            const formData = new FormData();
            formData.append('file', file);

            const button = $('thumbnail-upload');
            loader.button(button, true);

            try {
                const { url } = await api.upload('/courses/thumbnail', formData);
                $('thumbnailUrl').value = url;
                renderThumbnailPreview(url);
            } catch (error) {
                toast.fromApiError(error);
            } finally {
                loader.button(button, false);
            }
        });

        document.body.appendChild(input);
        input.click();
        input.remove();
    });
}

function renderThumbnailPreview(url) {
    const preview = $('thumbnail-preview');
    preview.innerHTML = '';

    const image = document.createElement('img');
    image.src = url;
    image.alt = '';
    preview.appendChild(image);
}

function buildPayload() {
    return {
        categoryId: $('categoryId').value,
        level: $('level').value,
        price: Number($('price').value) || 0,
        durationMinutes: Number($('durationMinutes').value) || 0,
        thumbnailUrl: $('thumbnailUrl').value.trim() || null,
        translations: translations.getValues().map((translation) => ({
            ...translation,
            whatYouWillLearn: (translation.whatYouWillLearn ?? '')
                .split('\n')
                .map((line) => line.trim())
                .filter(Boolean)
        }))
    };
}
function showFormError(message) {
    const box = $('form-error');
    box.textContent = message;
    box.classList.add('is-visible');
}

function hideFormError() {
    const box = $('form-error');
    box.textContent = '';
    box.classList.remove('is-visible');
}

startInstructorPage(load, { init: bindForm });
