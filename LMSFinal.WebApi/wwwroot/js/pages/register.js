/**
 * Страница регистрации.
 *
 * API возвращает готовый JWT прямо в ответе на регистрацию, поэтому отдельный
 * вход после неё не нужен — пользователь сразу попадает на свой дашборд.
 */

import { initLocalization, t, applyTranslations } from '../localization.js';
import { register, isAuthenticated, getHomeUrlForRole } from '../auth.js';
import { flushPending } from '../components/wishlist.js';
import { toast } from '../components/toast.js';
import { renderLanguageSwitcher } from '../components/language-switcher.js';
import { loader } from '../components/loader.js';
import { showFormErrors, clearFormErrors } from './form-errors.js';

const form = () => document.getElementById('register-form');

async function bootstrap() {
    await initLocalization();

    if (isAuthenticated()) {
        window.location.replace(getHomeUrlForRole());
        return;
    }

    prefillRoleFromQuery();
    renderLanguageSwitcher(document.getElementById('language-switcher'));

    bindRoleToggle();
    bindBioCounter();
    bindForm();
}

const currentRole = () => form().querySelector('input[name="role"]:checked').value;

/**
 * Поля профессионального профиля нужны только преподавателю. Скрытый fieldset
 * ещё и disabled: браузер не проверяет и не отправляет то, что недоступно,
 * поэтому студент не упирается в обязательные поля, которых не видит.
 */
function syncInstructorFields() {
    const isInstructor = currentRole() === 'Instructor';
    const section = document.getElementById('instructor-fields');

    section.classList.toggle('hidden', !isInstructor);
    section.disabled = !isInstructor;
    document.querySelector('.auth-box').classList.toggle('is-wide', isInstructor);
}

function bindRoleToggle() {
    form().querySelectorAll('input[name="role"]').forEach((radio) => {
        radio.addEventListener('change', syncInstructorFields);
    });

    // Роль могла прийти из ?role=Instructor — подстраиваем форму под неё сразу.
    syncInstructorFields();
}

function bindBioCounter() {
    const bio = document.getElementById('bio');
    const counter = document.getElementById('bio-count');

    bio.addEventListener('input', () => {
        counter.textContent = String(bio.value.length);
    });
}

/** Значения полей преподавателя; пустые необязательные уходят как null, а не как пустая строка. */
function readInstructorFields() {
    const text = (id) => document.getElementById(id).value.trim() || null;
    const years = document.getElementById('yearsOfExperience').value;

    return {
        professionalTitle: text('professionalTitle'),
        specialization: text('specialization'),
        yearsOfExperience: years === '' ? null : Number(years),
        educationLevel: text('educationLevel'),
        organization: text('organization'),
        bio: text('bio'),
        profileUrl: text('profileUrl')
    };
}

/**
 * Кнопка «Стать преподавателем» на лендинге ведёт сюда — удобно сразу выбрать
 * нужную роль, чтобы пользователь не искал переключатель глазами.
 */
function prefillRoleFromQuery() {
    const role = new URLSearchParams(window.location.search).get('role');

    if (role === 'Instructor' || role === 'Student') {
        const option = form().querySelector(`input[name="role"][value="${role}"]`);
        if (option) {
            option.checked = true;
        }
    }
}

function bindForm() {
    form().addEventListener('submit', async (event) => {
        event.preventDefault();

        const submitButton = document.getElementById('submit');

        const payload = {
            firstName: document.getElementById('firstName').value.trim(),
            lastName: document.getElementById('lastName').value.trim(),
            email: document.getElementById('email').value.trim(),
            password: document.getElementById('password').value,
            role: currentRole(),
            ...(currentRole() === 'Instructor' ? readInstructorFields() : {})
        };

        clearFormErrors(form());
        hideFormError();
        loader.button(submitButton, true);

        try {
            await register(payload);

            // Гость мог прийти сюда с формы входа, куда его привело сердечко, —
            // отложенный курс должен попасть в избранное и после регистрации.
            await flushPending();

            toast.success(t('auth.registerSuccess'));
            window.location.href = getHomeUrlForRole();
        } catch (error) {
            // 422 — не прошли правила FluentValidation, ошибки раскладываются по полям.
            // 400 — Identity отклонил пароль или email уже занят: это общая ошибка формы.
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
