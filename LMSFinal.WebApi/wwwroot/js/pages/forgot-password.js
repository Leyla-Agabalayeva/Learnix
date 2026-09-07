/**
 * «Забыли пароль?» — первый шаг восстановления.
 *
 * Сервер всегда отвечает одним и тем же нейтральным сообщением, даже если
 * такого email нет в системе (см. AuthController.ForgotPassword) — поэтому
 * здесь нет ветки "email не найден", есть только "письмо отправлено".
 */

import { initLocalization, t, applyTranslations } from '../localization.js';
import { api } from '../api.js';
import { renderLanguageSwitcher } from '../components/language-switcher.js';
import { loader } from '../components/loader.js';
import { showFormErrors, clearFormErrors } from './form-errors.js';

const form = () => document.getElementById('forgot-password-form');

async function bootstrap() {
    await initLocalization();
    renderLanguageSwitcher(document.getElementById('language-switcher'));
    bindForm();
}

function bindForm() {
    form().addEventListener('submit', async (event) => {
        event.preventDefault();

        const submitButton = document.getElementById('submit');
        const email = document.getElementById('email').value.trim();

        clearFormErrors(form());
        hideFormError();
        loader.button(submitButton, true);

        try {
            // api.js распаковывает ApiResponse и отдаёт наружу только data — здесь оно
            // всегда null (эндпоинт ничего не возвращает), поэтому текст берём из словаря,
            // а не из ответа сервера.
            await api.post('/auth/forgot-password', { email }, { anonymous: true });
            showSuccess(t('auth.forgotPasswordSuccess'));
        } catch (error) {
            if (error.isValidationError) {
                showFormErrors(form(), error.errors);
            } else {
                showFormError(error.isNetworkError ? t('states.networkError') : error.message);
            }
        } finally {
            loader.button(submitButton, false);
        }
    });
}

/** Прячем форму целиком — просить email второй раз незачем, письмо уже в пути. */
function showSuccess(message) {
    form().classList.add('hidden');

    const box = document.getElementById('form-success');
    box.textContent = message;
    box.classList.remove('hidden');
    box.classList.add('is-visible');
}

function showFormError(message) {
    const box = document.getElementById('form-error');
    box.textContent = message;
    box.classList.add('is-visible');
}

function hideFormError() {
    const box = document.getElementById('form-error');
    box.textContent = '';
    box.classList.remove('is-visible');
}

document.addEventListener('DOMContentLoaded', () => {
    bootstrap().then(() => applyTranslations());
});
