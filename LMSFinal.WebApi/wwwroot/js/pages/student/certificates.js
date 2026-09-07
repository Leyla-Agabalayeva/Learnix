/**
 * Сертификаты студента — F8 (раздел 16 ТЗ).
 *
 * Скачивание идёт через api.download: сервер отдаёт PDF потоком, и запрос
 * должен нести заголовок Authorization. Обычная ссылка <a href> его не отправит,
 * поэтому файл забирается как Blob и сохраняется вручную.
 */

import { api } from '../../api.js';
import { t } from '../../localization.js';
import { loader } from '../../components/loader.js';
import { emptyState } from '../../components/empty-state.js';
import { toast } from '../../components/toast.js';
import * as format from '../../format.js';
import { startStudentPage, showLoadError } from './common.js';

const $ = (id) => document.getElementById(id);

async function load() {
    loader.skeleton($('certificates-list'), { count: 3, variant: 'row' });

    try {
        const certificates = await api.get('/certificates/my');
        render(certificates);
    } catch (error) {
        showLoadError($('certificates-list'), error, load);
    }
}

function render(certificates) {
    const container = $('certificates-list');
    container.innerHTML = '';

    if (!certificates.length) {
        emptyState.render(container, {
            icon: '🏆',
            title: t('states.noCertificates'),
            text: t('student.continueEmptyHint'),
            action: { label: t('actions.explore'), href: '/pages/courses.html' }
        });
        return;
    }

    certificates.forEach((certificate) => container.appendChild(buildCard(certificate)));
}

function buildCard(certificate) {
    const card = document.createElement('article');
    card.className = 'certificate-card';

    const seal = document.createElement('div');
    seal.className = 'certificate-seal';
    seal.setAttribute('aria-hidden', 'true');
    seal.textContent = '🏆';

    const body = document.createElement('div');

    const title = document.createElement('p');
    title.className = 'font-semibold';
    title.textContent = certificate.courseTitle;

    const meta = document.createElement('p');
    meta.className = 'text-sm text-muted mt-1';
    meta.textContent = `${t('details.instructorLabel')}: ${certificate.instructorName} · ${t('student.issuedOn')} ${format.date(certificate.issuedAt)}`;

    const number = document.createElement('span');
    number.className = 'certificate-number';
    number.textContent = certificate.certificateNumber;

    body.append(title, meta, number);

    const actions = document.createElement('div');
    actions.className = 'flex gap-2 flex-wrap';

    // Главное действие — открыть сам сертификат: скачивать PDF вслепую,
    // не увидев, что в нём написано, мало кому нужно.
    const view = document.createElement('a');
    view.className = 'btn btn-primary btn-sm';
    view.href = `/pages/certificate.html?id=${certificate.id}`;
    view.textContent = t('certificate.view');

    const download = document.createElement('button');
    download.type = 'button';
    download.className = 'btn btn-secondary btn-sm';
    download.textContent = t('actions.download');
    download.addEventListener('click', () => downloadPdf(certificate, download));

    // Публичная проверка по номеру — раздел 17 ТЗ. Ссылка удобна, чтобы
    // показать работодателю, что сертификат подтверждается без входа в систему.
    const verify = document.createElement('a');
    verify.className = 'btn btn-ghost btn-sm';
    verify.href = `/pages/verify-certificate.html?number=${encodeURIComponent(certificate.certificateNumber)}`;
    verify.textContent = t('actions.verify');

    actions.append(view, download, verify);
    card.append(seal, body, actions);
    return card;
}

async function downloadPdf(certificate, button) {
    loader.button(button, true);

    try {
        const blob = await api.download(`/certificates/${certificate.id}/download`);
        api.saveBlob(blob, `${certificate.certificateNumber}.pdf`);
    } catch (error) {
        toast.fromApiError(error);
    } finally {
        loader.button(button, false);
    }
}

startStudentPage(load);
