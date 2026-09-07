/**
 * Страница входа.
 *
 * Здесь нет ни навбара, ни app.js: страницы авторизации намеренно
 * самостоятельные — на них не должно быть меню, которое зовёт войти.
 * Поэтому локализация инициализируется вручную.
 */

import { initLocalization, t, applyTranslations } from '../localization.js';
import { login, isAuthenticated, getHomeUrlForRole } from '../auth.js';
import { toast } from '../components/toast.js';
import { renderLanguageSwitcher } from '../components/language-switcher.js';
import { loader } from '../components/loader.js';
import { showFormErrors, clearFormErrors } from './form-errors.js';
import { flushPending } from '../components/wishlist.js';

const form = () => document.getElementById('login-form');

async function bootstrap() {
    await initLocalization();

    // Уже вошёл — незачем показывать форму входа, сразу на его дашборд.
    if (isAuthenticated()) {
        window.location.replace(getHomeUrlForRole());
        return;
    }

    renderLanguageSwitcher(document.getElementById('language-switcher'));

    handleExpiredSession();
    bindForm();
    bindDemoAccounts();
}

/**
 * Если сюда привёл ответ 401 (api.js добавляет ?expired=1), объясняем причину.
 * Без этого пользователь просто оказывается на странице входа и не понимает почему.
 */
function handleExpiredSession() {
    const params = new URLSearchParams(window.location.search);

    if (params.get('expired') === '1') {
        toast.warning(t('auth.sessionExpired'));
    }

    // Сюда привело нажатие сердечка гостем — объясняем, зачем его просят войти.
    // Без этого человек оказывается на форме входа и не понимает, что случилось.
    if (params.get('wishlist') === '1') {
        toast.info(t('auth.loginToSaveCourse'));
    }
}

function bindForm() {
    form().addEventListener('submit', async (event) => {
        event.preventDefault();

        const submitButton = document.getElementById('submit');
        const email = document.getElementById('email').value.trim();
        const password = document.getElementById('password').value;

        clearFormErrors(form());
        hideFormError();

        loader.button(submitButton, true);

        try {
            await login(email, password);

            // Курс, отмеченный до входа, досылаем ДО перехода: иначе человек
            // вернётся в каталог, а того, ради чего входил, в избранном не будет.
            //
            // Тост об успехе тут не показываем: сразу за ним идёт переход, и
            // увидеть его не успеешь. Подтверждением служит закрашенное сердечко
            // на карточке, к которой пользователь возвращается.
            await flushPending();

            toast.success(t('auth.loginSuccess'));

            // Возвращаем туда, откуда пользователя увели из-за 401,
            // иначе он теряет страницу, которую пытался открыть.
            const returnUrl = new URLSearchParams(window.location.search).get('returnUrl');
            window.location.href = safeReturnUrl(returnUrl) ?? getHomeUrlForRole();
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

/**
 * Возвращаемый адрес берётся из query-строки, то есть управляется извне.
 * Разрешаем только относительные пути внутри сайта — иначе ссылка вида
 * /pages/login.html?returnUrl=https://evil.example уводила бы пользователя
 * на чужой сайт сразу после успешного входа (open redirect).
 */
function safeReturnUrl(value) {
    if (!value) {
        return null;
    }

    const decoded = decodeURIComponent(value);
    const isRelative = decoded.startsWith('/') && !decoded.startsWith('//');

    return isRelative ? decoded : null;
}

/** Подставляет демо-логин в форму, но НЕ отправляет её — пусть будет виден шаг входа. */
function bindDemoAccounts() {
    document.querySelectorAll('.demo-account').forEach((button) => {
        button.addEventListener('click', () => {
            document.getElementById('email').value = button.dataset.email;
            document.getElementById('password').value = button.dataset.password;
            document.getElementById('email').focus();
        });
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
