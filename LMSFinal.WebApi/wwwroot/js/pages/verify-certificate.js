/**
 * Публичная проверка сертификата — раздел 17 ТЗ.
 *
 * Единственная страница приложения, которая работает БЕЗ входа: её адресат —
 * работодатель, у которого аккаунта здесь нет и не будет. Поэтому все запросы
 * идут с { anonymous: true } — иначе api.js подставил бы токен случайного
 * залогиненного пользователя, а на 401 увёл бы на страницу входа.
 */

import { api } from '../api.js';
import { t, onLanguageChange } from '../localization.js';
import { loader } from '../components/loader.js';
import { buildCertificate } from '../components/certificate-view.js';
import { onReady } from '../ready.js';

const $ = (id) => document.getElementById(id);

const state = {
    /** Последний проверенный номер — по нему перерисовываем при смене языка. */
    number: '',
    certificate: null,
    notFound: false
};

// ---------------------------------------------------------------------------
// Проверка
// ---------------------------------------------------------------------------

async function verify(number) {
    const cleaned = normalize(number);

    if (!cleaned) {
        return;
    }

    state.number = cleaned;
    state.certificate = null;
    state.notFound = false;

    loader.block($('result'), t('states.loading'));
    loader.button($('submit'), true);

    try {
        state.certificate = await api.get(`/certificates/verify/${encodeURIComponent(cleaned)}`, {
            anonymous: true
        });
        render();
    } catch (error) {
        if (error?.status === 404) {
            state.notFound = true;
            render();
        } else {
            renderError(error);
        }
    } finally {
        loader.button($('submit'), false);
    }
}

/**
 * Приводит номер к каноническому виду.
 *
 * Номер переписывают с бумаги или копируют из письма, поэтому в него попадают
 * лишние пробелы, а регистр набирают как придётся. Сервер ищет по точному
 * совпадению, и «lms-2026-000001» не нашлось бы — хотя человек ввёл правильно.
 */
function normalize(value) {
    return String(value ?? '').trim().toUpperCase().replace(/\s+/g, '');
}

// ---------------------------------------------------------------------------
// Отрисовка
// ---------------------------------------------------------------------------

function render() {
    const container = $('result');
    container.innerHTML = '';

    if (state.notFound) {
        container.appendChild(buildVerdict(false));
        container.appendChild(buildNotFoundHint());
        return;
    }

    if (!state.certificate) {
        return;
    }

    container.appendChild(buildVerdict(true));
    container.appendChild(buildCertificate(state.certificate));
}

function buildVerdict(isValid) {
    const verdict = document.createElement('p');
    verdict.className = `verify-verdict ${isValid ? 'is-valid' : 'is-invalid'}`;

    // Значок дублирует смысл цвета: вердикт должен читаться и при дальтонизме,
    // и в чёрно-белой распечатке.
    const icon = document.createElement('span');
    icon.className = 'verify-verdict-icon';
    icon.setAttribute('aria-hidden', 'true');
    icon.textContent = isValid ? '✓' : '✕';

    const text = document.createElement('span');
    text.textContent = isValid
        ? t('verify.valid', { number: state.number })
        : t('verify.invalid', { number: state.number });

    verdict.append(icon, text);
    return verdict;
}

function buildNotFoundHint() {
    const hint = document.createElement('p');
    hint.className = 'text-muted text-center';
    hint.textContent = t('verify.invalidHint');
    return hint;
}

function renderError(error) {
    const container = $('result');
    container.innerHTML = '';

    // Сетевую ошибку и сбой сервера нельзя показывать как «сертификат поддельный»:
    // это разные вещи, и второе — обвинение, которого мы не проверяли.
    const message = document.createElement('p');
    message.className = 'text-center text-muted';
    message.textContent = error?.isNetworkError ? t('states.networkError') : t('states.error');

    const retry = document.createElement('button');
    retry.type = 'button';
    retry.className = 'btn btn-secondary btn-sm mt-3';
    retry.textContent = t('actions.retry');
    retry.addEventListener('click', () => verify(state.number));

    const wrapper = document.createElement('div');
    wrapper.className = 'text-center';
    wrapper.append(message, retry);

    container.appendChild(wrapper);
}

// ---------------------------------------------------------------------------
// Запуск
// ---------------------------------------------------------------------------

function bindForm() {
    $('verify-form').addEventListener('submit', (event) => {
        event.preventDefault();
        verify($('certificate-number').value);
    });
}

onReady(() => {
    bindForm();

    // Номер может прийти в адресе — так работает ссылка «Проверить» из кабинета
    // и та, которую студент отправляет работодателю. Проверяем сразу, без клика.
    const fromUrl = new URLSearchParams(window.location.search).get('number');

    if (fromUrl) {
        $('certificate-number').value = normalize(fromUrl);
        verify(fromUrl);
    }

    // Данные сертификата от языка не зависят, но подписи и вердикт — да.
    onLanguageChange(() => {
        if (state.certificate || state.notFound) {
            render();
        }
    });
});
