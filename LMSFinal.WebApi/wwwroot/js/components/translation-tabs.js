/**
 * Редактор мультиязычного контента: вкладки AZ / EN / RU.
 *
 * Используется во всех формах Course Builder — курс, модуль, урок. Везде
 * структура одна: набор полей, который заполняется отдельно для каждого языка,
 * а на сервер уходит массивом Translations.
 *
 * Почему вкладки, а не три копии формы подряд: при трёх языках и четырёх полях
 * форма растянулась бы на двенадцать полей, и автор потерялся бы в ней.
 * Вкладки показывают один язык за раз, а точка на вкладке отмечает, что
 * в этом языке уже что-то заполнено.
 */

import { t, getLanguage } from '../localization.js';

const LANGUAGES = ['AZ', 'EN', 'RU'];

/**
 * @param {HTMLElement} container
 * @param {Array<{ name: string, labelKey: string, type?: 'input'|'textarea', required?: boolean }>} fields
 * @param {Record<string, Record<string, string>>} [initial] значения: { AZ: { title: '…' } }
 * @returns {{ getValues: () => object[], setValues: (v: object) => void, validate: () => boolean }}
 */
export function createTranslationTabs(container, fields, initial = {}) {
    container.innerHTML = '';
    container.className = 'translation-tabs';

    const values = {};
    LANGUAGES.forEach((language) => {
        values[language] = { ...(initial[language] ?? {}) };
    });

    const tabBar = document.createElement('div');
    tabBar.className = 'tabs';
    tabBar.setAttribute('role', 'tablist');

    const panels = document.createElement('div');

    // Активной делаем вкладку языка интерфейса: автор скорее всего начнёт
    // заполнять на том языке, на котором сам работает.
    const current = getLanguage().toUpperCase();
    let activeLanguage = LANGUAGES.includes(current) ? current : 'AZ';

    const tabButtons = {};
    const panelElements = {};

    LANGUAGES.forEach((language) => {
        const tab = document.createElement('button');
        tab.type = 'button';
        tab.className = 'tab';
        tab.setAttribute('role', 'tab');
        tab.dataset.language = language;
        tab.addEventListener('click', () => activate(language));

        tabButtons[language] = tab;
        tabBar.appendChild(tab);

        const panel = document.createElement('div');
        panel.className = 'translation-panel';

        fields.forEach((field) => {
            panel.appendChild(buildField(language, field));
        });

        panelElements[language] = panel;
        panels.appendChild(panel);
    });

    container.append(tabBar, panels);
    activate(activeLanguage);

    function buildField(language, field) {
        const wrapper = document.createElement('div');
        wrapper.className = 'field';

        const id = `tr-${language}-${field.name}`;

        const label = document.createElement('label');
        label.className = 'field-label';
        label.htmlFor = id;
        label.textContent = t(field.labelKey);

        if (field.required) {
            const star = document.createElement('span');
            star.className = 'required';
            star.textContent = ' *';
            label.appendChild(star);
        }

        const control = field.type === 'textarea'
            ? document.createElement('textarea')
            : document.createElement('input');

        control.className = field.type === 'textarea' ? 'textarea' : 'input';
        control.id = id;
        control.value = values[language][field.name] ?? '';

        control.addEventListener('input', () => {
            values[language][field.name] = control.value;
            updateTabMarks();
        });

        const error = document.createElement('p');
        error.className = 'field-error hidden';
        error.dataset.errorFor = `${language}.${field.name}`;

        wrapper.append(label, control, error);
        return wrapper;
    }

    function activate(language) {
        activeLanguage = language;

        LANGUAGES.forEach((item) => {
            const isActive = item === language;
            tabButtons[item].classList.toggle('is-active', isActive);
            tabButtons[item].setAttribute('aria-selected', String(isActive));
            panelElements[item].classList.toggle('hidden', !isActive);
        });
    }

    /** Точка на вкладке = в этом языке что-то заполнено. */
    function updateTabMarks() {
        LANGUAGES.forEach((language) => {
            const filled = fields.some((field) => (values[language][field.name] ?? '').trim().length > 0);
            tabButtons[language].textContent = language;

            if (filled) {
                const dot = document.createElement('span');
                dot.className = 'tab-dot';
                dot.setAttribute('aria-hidden', 'true');
                dot.textContent = '•';
                tabButtons[language].appendChild(dot);
            }
        });
    }

    updateTabMarks();

    return {
        /**
         * Массив переводов для отправки на сервер.
         * Языки, где не заполнено обязательное поле, отбрасываются: пустой
         * перевод хуже отсутствующего — на нём сломается fallback, и студент
         * увидит пустое название вместо текста на другом языке.
         */
        getValues() {
            return LANGUAGES
                .filter((language) => requiredFilled(language))
                .map((language) => ({
                    languageCode: language,
                    ...Object.fromEntries(fields.map((field) => [
                        field.name,
                        values[language][field.name] ?? ''
                    ]))
                }));
        },

        /** Хотя бы один язык должен быть заполнен целиком. */
        validate() {
            const ok = LANGUAGES.some((language) => requiredFilled(language));

            if (!ok) {
                // Подсвечиваем вкладку языка интерфейса — с неё удобнее начать.
                activate(activeLanguage);
            }

            return ok;
        },

        get activeLanguage() {
            return activeLanguage;
        }
    };

    function requiredFilled(language) {
        return fields
            .filter((field) => field.required)
            .every((field) => (values[language][field.name] ?? '').trim().length > 0);
    }
}
