/**
 * Показ ошибок валидации, пришедших с сервера.
 *
 * Бэкенд возвращает 422 со словарём «поле → список сообщений»:
 *
 *   { "success": false, "message": "Validation failed",
 *     "errors": { "email": ["Email is required"], "password": ["Too short"] } }
 *
 * Этот модуль раскладывает такой словарь по полям формы.
 *
 * Почему тексты ошибок не дублируются на фронтенде: правила уже описаны
 * в FluentValidation на сервере. Если продублировать их здесь, два набора
 * правил неизбежно разойдутся — и пользователь получит подсказку, которая
 * не совпадает с тем, что реально проверяет API.
 *
 * Браузерная валидация (required, type="email") остаётся как быстрая
 * подсказка, но решающее слово всегда за сервером.
 */

/**
 * @param {HTMLFormElement} form
 * @param {Record<string, string[]>} errors ключи в camelCase, как их отдаёт API
 */
export function showFormErrors(form, errors) {
    clearFormErrors(form);

    if (!errors) {
        return;
    }

    let firstInvalid = null;

    Object.entries(errors).forEach(([field, messages]) => {
        const input = form.querySelector(`[name="${field}"]`);

        if (!input) {
            return;
        }

        input.classList.add('is-invalid');
        input.setAttribute('aria-invalid', 'true');

        const errorElement = document.getElementById(`${input.id}-error`);

        if (errorElement) {
            errorElement.textContent = Array.isArray(messages) ? messages.join(' ') : String(messages);
            errorElement.classList.remove('hidden');
        }

        firstInvalid ??= input;
    });

    // Фокус на первое проблемное поле: пользователю не приходится искать,
    // где именно ошибка, а скринридер сразу зачитает её текст.
    firstInvalid?.focus();
}

/** @param {HTMLFormElement} form */
export function clearFormErrors(form) {
    form.querySelectorAll('.is-invalid').forEach((input) => {
        input.classList.remove('is-invalid');
        input.removeAttribute('aria-invalid');
    });

    form.querySelectorAll('.field-error').forEach((element) => {
        element.textContent = '';
        element.classList.add('hidden');
    });
}
