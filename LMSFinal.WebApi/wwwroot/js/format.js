/**
 * Форматирование значений для интерфейса.
 *
 * Вынесено отдельно, потому что цена, длительность и дата встречаются на каждой
 * второй странице, и без общего модуля они неизбежно начали бы форматироваться
 * по-разному: где-то «49.99», где-то «49,99 $», где-то «$49.9».
 *
 * Всё, что зависит от языка, берёт текущий язык из localization.js — поэтому
 * при переключении AZ / EN / RU меняются и разделители чисел, и названия месяцев.
 */

import { t, getLanguage } from './localization.js';

/** BCP 47 — код локали для Intl. */
function locale() {
    switch (getLanguage()) {
        case 'az': return 'az-AZ';
        case 'ru': return 'ru-RU';
        default: return 'en-US';
    }
}

/** Цена: 0 показывается словом «Бесплатно», а не «$0.00». */
export function price(value) {
    if (!value || Number(value) === 0) {
        return t('course.free');
    }

    return new Intl.NumberFormat(locale(), {
        style: 'currency',
        currency: 'USD',
        maximumFractionDigits: 2
    }).format(value);
}

/** Длительность: 58 → «58 мин», 145 → «2 ч 25 мин». */
export function duration(minutes) {
    const total = Number(minutes) || 0;

    if (total < 60) {
        return `${total} ${t('units.minShort')}`;
    }

    const hours = Math.floor(total / 60);
    const rest = total % 60;

    return rest === 0
        ? `${hours} ${t('units.hourShort')}`
        : `${hours} ${t('units.hourShort')} ${rest} ${t('units.minShort')}`;
}

/** Разделители разрядов: 1245 → «1 245». */
export function number(value) {
    return new Intl.NumberFormat(locale()).format(Number(value) || 0);
}

/**
 * Существительное в правильной форме множественного числа.
 *
 *   plural(1, 'units.lesson')  → «1 урок»
 *   plural(3, 'units.lesson')  → «3 урока»
 *   plural(43, 'units.lesson') → «43 урока»  (в русском 43 → few)
 *
 * Формы берутся из словаря, а нужную выбирает Intl.PluralRules — по правилам
 * конкретного языка, а не по самодельному «если 1, иначе множественное».
 * В русском форм три, в английском две, в азербайджанском после числительного
 * существительное вообще не меняется — всё это описано в locales, а не в коде.
 */
export function plural(count, key) {
    const value = Number(count) || 0;
    const forms = t(key);

    // Ключ не найден или задан строкой — показываем как есть, ничего не ломая.
    if (!forms || typeof forms !== 'object') {
        return `${number(value)} ${forms ?? key}`;
    }

    const rule = new Intl.PluralRules(locale()).select(value);
    const word = forms[rule] ?? forms.other ?? forms.one ?? '';

    return `${number(value)} ${word}`;
}

// В браузерах со «small-icu» сборкой Intl нет полных данных для az-AZ, и
// { month: 'long' } вместо названия месяца отдаёт «M07» и подобное — это
// ограничение самого движка, а не наш формат. Проще прописать азербайджанские
// названия месяцев вручную, чем тащить полифилл Intl ради одной локали.
const AZ_MONTHS = [
    'yanvar', 'fevral', 'mart', 'aprel', 'may', 'iyun',
    'iyul', 'avqust', 'sentyabr', 'oktyabr', 'noyabr', 'dekabr'
];

export function date(value) {
    if (!value) {
        return '';
    }

    const parsed = new Date(value);

    if (getLanguage() === 'az') {
        return `${parsed.getDate()} ${AZ_MONTHS[parsed.getMonth()]} ${parsed.getFullYear()}`;
    }

    return new Intl.DateTimeFormat(locale(), {
        day: 'numeric', month: 'long', year: 'numeric'
    }).format(parsed);
}

/** Рейтинг с одним знаком: 5 → «5.0». */
export function rating(value) {
    return (Number(value) || 0).toFixed(1);
}

/**
 * Звёзды рейтинга. Возвращает строку из пяти символов, где закрашено
 * столько, сколько получилось после округления.
 */
export function stars(value) {
    const filled = Math.round(Number(value) || 0);
    return { filled, empty: 5 - filled };
}

/** Level и Status приходят с API строками enum — переводим их через словарь. */
export function level(value) {
    return t(`course.level${value}`);
}

export function status(value) {
    return t(`course.status${value}`);
}
