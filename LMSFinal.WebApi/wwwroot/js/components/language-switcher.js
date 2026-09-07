/**
 * Переключатель языка AZ / EN / RU (раздел 19 ТЗ).
 *
 * Раньше он жил внутри navbar.js. Но страницы входа и регистрации намеренно
 * без навбара — на них не должно быть меню, которое зовёт войти, — и вместе
 * с меню оттуда пропадал и переключатель. Азербайджанец, открывший ссылку
 * на страницу входа, не мог сменить язык вообще ничем.
 *
 * Поэтому компонент вынесен отдельно: навбар использует его как часть себя,
 * а страницы без навбара ставят его самостоятельно.
 */

import { t, getLanguage, getSupportedLanguages, setLanguage, onLanguageChange } from '../localization.js';

/**
 * Создаёт переключатель. Сам следит за сменой языка и перекрашивает кнопки —
 * язык может смениться и не через него (например, на другой вкладке навбара).
 *
 * @returns {HTMLElement}
 */
export function createLanguageSwitcher() {
    const switcher = document.createElement('div');
    switcher.className = 'lang-switcher';
    switcher.setAttribute('role', 'group');
    switcher.setAttribute('aria-label', t('nav.language'));

    const buttons = getSupportedLanguages().map((language) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'lang-option';
        // Код языка не переводится: «az» на любом языке остаётся «az».
        // Заглавные буквы рисует CSS (text-transform), в разметке — нижний регистр.
        button.textContent = language;
        button.dataset.language = language;

        // Смена языка не перезагружает страницу (раздел 19 ТЗ).
        button.addEventListener('click', () => setLanguage(language));

        switcher.appendChild(button);
        return button;
    });

    const paint = () => {
        const current = getLanguage();

        buttons.forEach((button) => {
            const isCurrent = button.dataset.language === current;
            button.classList.toggle('is-active', isCurrent);
            // aria-pressed, а не только класс: без него скринридер не отличит
            // выбранный язык от остальных — цвет ему недоступен.
            button.setAttribute('aria-pressed', String(isCurrent));
        });

        switcher.setAttribute('aria-label', t('nav.language'));
    };

    paint();
    onLanguageChange(paint);

    return switcher;
}

/** Ставит переключатель в контейнер, заменяя прежнее содержимое. */
export function renderLanguageSwitcher(container) {
    if (!container) {
        return;
    }

    container.innerHTML = '';
    container.appendChild(createLanguageSwitcher());
}
