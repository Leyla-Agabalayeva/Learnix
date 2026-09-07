/**
 * Страница одного сертификата — F8 (раздел 16 ТЗ).
 *
 * Открывается по /pages/certificate.html?id=<guid> из списка сертификатов.
 * Сертификат чужой открыть нельзя: сервер отдаёт 403 (ownership-проверка
 * в CertificateService.GetByIdAsync), фронтенд лишь показывает это по-человечески.
 */

import { api } from '../api.js';
import { requireAuth, ROLES } from '../auth.js';
import { t, onLanguageChange } from '../localization.js';
import { loader } from '../components/loader.js';
import { emptyState } from '../components/empty-state.js';
import { toast } from '../components/toast.js';
import { buildCertificate } from '../components/certificate-view.js';
import { onReady } from '../ready.js';

const $ = (id) => document.getElementById(id);

const state = {
    id: new URLSearchParams(window.location.search).get('id'),
    certificate: null
};

async function load() {
    const container = $('certificate');

    if (!state.id) {
        // Прямой заход без параметра — не ошибка сервера, а неполный адрес.
        emptyState.render(container, {
            icon: '🔗',
            title: t('certificate.noIdTitle'),
            text: t('certificate.noIdText'),
            action: { label: t('nav.certificates'), href: '/pages/student/certificates.html' }
        });
        return;
    }

    loader.block(container, t('states.loading'));

    try {
        state.certificate = await api.get(`/certificates/${state.id}`);
        render();
    } catch (error) {
        renderError(container, error);
    }
}

function render() {
    const container = $('certificate');
    container.innerHTML = '';

    container.appendChild(buildCertificate(state.certificate));
    container.appendChild(buildActions());

    // Заголовок вкладки — по названию курса: у студента таких вкладок может быть
    // несколько, и «Certificate — Learnix» на всех не помогает их различить.
    document.title = `${state.certificate.courseTitle} — Learnix`;
}

function buildActions() {
    const actions = document.createElement('div');
    actions.className = 'certificate-actions';

    const download = document.createElement('button');
    download.type = 'button';
    download.className = 'btn btn-primary';
    download.textContent = t('certificate.downloadPdf');
    download.addEventListener('click', () => downloadPdf(download));

    // Ссылка на публичную проверку — то, что студент отправляет работодателю.
    const share = document.createElement('button');
    share.type = 'button';
    share.className = 'btn btn-secondary';
    share.textContent = t('certificate.copyLink');
    share.addEventListener('click', () => copyVerificationLink(share));

    const verify = document.createElement('a');
    verify.className = 'btn btn-ghost';
    verify.href = verificationUrl();
    verify.textContent = t('certificate.openVerification');

    actions.append(download, share, verify);
    return actions;
}

// ---------------------------------------------------------------------------
// Действия
// ---------------------------------------------------------------------------

async function downloadPdf(button) {
    loader.button(button, true);

    try {
        // Через api.download, а не обычной ссылкой: эндпоинт требует заголовок
        // Authorization, а <a href> его не отправит — вернулся бы 401.
        const blob = await api.download(`/certificates/${state.id}/download`);
        api.saveBlob(blob, `${state.certificate.certificateNumber}.pdf`);
        toast.success(t('certificate.downloaded'));
    } catch (error) {
        toast.fromApiError(error);
    } finally {
        loader.button(button, false);
    }
}

async function copyVerificationLink(button) {
    const url = new URL(verificationUrl(), window.location.origin).href;

    try {
        // Clipboard API есть не везде (нужен https или localhost) — если его нет,
        // молча падать нельзя: показываем ссылку, чтобы её скопировали руками.
        await navigator.clipboard.writeText(url);
        toast.success(t('certificate.linkCopied'));
    } catch {
        window.prompt(t('certificate.copyManually'), url);
    }

    button.blur();
}

function verificationUrl() {
    const number = encodeURIComponent(state.certificate?.certificateNumber ?? '');
    return `/pages/verify-certificate.html?number=${number}`;
}

// ---------------------------------------------------------------------------
// Ошибки
// ---------------------------------------------------------------------------

function renderError(container, error) {
    // 403 и 404 здесь означают одно и то же для пользователя: «этого сертификата
    // у вас нет». Разделять их в интерфейсе смысла нет, а вот подсказывать
    // «повторить» на 403 — вредно: повтор ничего не изменит.
    if (error?.status === 403 || error?.status === 404) {
        emptyState.render(container, {
            icon: '🔍',
            title: t('certificate.notYoursTitle'),
            text: t('certificate.notYoursText'),
            action: { label: t('nav.certificates'), href: '/pages/student/certificates.html' }
        });
        return;
    }

    emptyState.error(container, {
        message: error?.isNetworkError ? t('states.networkError') : error?.message,
        onRetry: load
    });
}

// ---------------------------------------------------------------------------

onReady(async () => {
    if (!requireAuth(ROLES.STUDENT)) {
        return;
    }

    await load();

    // Содержимое сертификата (имя, курс) от языка интерфейса не зависит — оно
    // уже зафиксировано в момент выдачи. Перерисовываем только ради подписей.
    onLanguageChange(() => {
        if (state.certificate) {
            render();
        } else {
            load();
        }
    });
});
