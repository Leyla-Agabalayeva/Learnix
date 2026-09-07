/**
 * Установка нового пароля по ссылке из письма.
 *
 * Email и токен приходят в query-строке (их туда положил AuthService.ForgotPasswordAsync
 * при генерации ссылки) — форма их не запрашивает у пользователя, только новый пароль.
 */

import { initLocalization, t, applyTranslations } from '../localization.js';
import { api } from '../api.js';
import { renderLanguageSwitcher } from '../components/language-switcher.js';
import { loader } from '../components/loader.js';
import { showFormErrors, clearFormErrors } from './form-errors.js';

const form = () => document.getElementById('reset-password-form');

async function bootstrap() {
    await initLocalization();
    renderLanguageSwitcher(document.getElementById('language-switcher'));

    const params = new URLSearchParams(window.location.search);
    const email = params.get('email');
    const token = params.get('token');

    // Без обоих параметров форма всё равно ничего не сможет отправить —
    // сразу объясняем, что ссылка не та, вместо непонятной ошибки после сабмита.
    if (!email || !token) {
        showFormError(t('auth.resetPasswordInvalidLink'));
        form().querySelectorAll('input, button').forEach((el) => { el.disabled = true; });
        return;
    }

    bindForm(email, token);
}

function bindForm(email, token) {
    form().addEventListener('submit', async (event) => {
        event.preventDefault();

        const submitButton = document.getElementById('submit');
        const password = document.getElementById('password').value;
        const confirmPassword = document.getElementById('confirmPassword').value;

        clearFormErrors(form());
        hideFormError();

        if (password !== confirmPassword) {
            showFormErrors(form(), { confirmPassword: [t('auth.passwordsDontMatch')] });
            return;
        }

        loader.button(submitButton, true);

        try {
            await api.post('/auth/reset-password', { email, token, newPassword: password }, { anonymous: true });

            form().classList.add('hidden');

            const box = document.getElementById('form-success');
            box.textContent = t('auth.resetPasswordSuccess');
            box.classList.remove('hidden');
            box.classList.add('is-visible');
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
