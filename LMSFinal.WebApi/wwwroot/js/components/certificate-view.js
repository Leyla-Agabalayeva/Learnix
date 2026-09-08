/**
 * Визуальный сертификат — общий для страницы студента и публичной проверки.
 *
 * Почему один компонент на две страницы: работодатель, набравший номер, должен
 * увидеть РОВНО ТО ЖЕ, что напечатано на PDF у студента. Если бы проверка
 * показывала сухую сводку «номер верный, курс такой-то», а сертификат выглядел
 * иначе, сверять их глазами было бы нечем — а именно в этом смысл проверки.
 *
 * Вёрстка повторяет QuestPdfCertificateGenerator: тот же порядок строк, те же
 * подписи. Расхождение между PDF и экраном сразу бросалось бы в глаза.
 */

import { t } from '../localization.js';
import * as format from '../format.js';

// qrcode.min.js — обычный UMD-скрипт (не ES-модуль), грузим его лениво тегом
// <script> при первом сертификате на странице, а не заранее на каждой из
// вообще всех страниц сайта.
let qrScriptPromise = null;
function loadQrScript() {
    if (window.QRCode) {
        return Promise.resolve();
    }

    if (!qrScriptPromise) {
        qrScriptPromise = new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = '/js/vendor/qrcode.min.js';
            script.onload = () => resolve();
            script.onerror = () => reject(new Error('Не удалось загрузить qrcode.min.js'));
            document.head.appendChild(script);
        });
    }

    return qrScriptPromise;
}

/**
 * Собирает разметку сертификата.
 *
 * @param {object} certificate ответ /certificates/{id} или /certificates/verify/{number}
 * @returns {HTMLElement}
 */
export function buildCertificate(certificate) {
    const sheet = document.createElement('article');
    sheet.className = 'certificate-sheet';

    // Рамка нарисована отдельным элементом, а не border на самом листе:
    // ей нужен собственный отступ внутрь, как на бумажных дипломах.
    const frame = document.createElement('div');
    frame.className = 'certificate-frame';
    frame.setAttribute('aria-hidden', 'true');

    const inner = document.createElement('div');
    inner.className = 'certificate-inner';

    inner.append(
        buildSeal(),
        line('certificate-heading', t('certificate.heading')),
        line('certificate-label', t('certificate.certifies')),
        line('certificate-name', certificate.studentName),
        line('certificate-label', t('certificate.completed')),
        line('certificate-course', certificate.courseTitle),
        buildMeta(certificate),
        buildFooter(certificate)
    );

    sheet.append(frame, inner);
    return sheet;
}

// ---------------------------------------------------------------------------
// Части
// ---------------------------------------------------------------------------

function buildSeal() {
    const seal = document.createElement('div');
    seal.className = 'certificate-seal';
    // Печать декоративна: смысл несут строки ниже, и скринридеру она не нужна.
    seal.setAttribute('aria-hidden', 'true');
    seal.textContent = '🏆';
    return seal;
}

function buildMeta(certificate) {
    const meta = document.createElement('dl');
    meta.className = 'certificate-meta';

    appendPair(meta, t('details.instructorLabel'), certificate.instructorName);
    appendPair(meta, t('certificate.completionDate'), format.date(certificate.completionDate));
    appendPair(meta, t('student.issuedOn'), format.date(certificate.issuedAt));

    return meta;
}

function buildFooter(certificate) {
    const footer = document.createElement('div');
    footer.className = 'certificate-footer';

    // Класс НЕ certificate-number: он уже занят карточкой в списке сертификатов
    // (student.css, Phase 22), где номер оформлен серой плашкой. Из-за совпадения
    // имён один и тот же лист выглядел по-разному на двух страницах — там, где
    // student.css подключён, и там, где нет.
    const number = document.createElement('p');
    number.className = 'certificate-id';

    const numberLabel = document.createElement('span');
    numberLabel.className = 'certificate-id-label';
    numberLabel.textContent = t('student.certificateNumber');

    // Номер — единственное, что человек будет перепечатывать вручную,
    // поэтому он моноширинный: так не путаются 0 и O, 1 и l.
    const numberValue = document.createElement('span');
    numberValue.className = 'certificate-id-value';
    numberValue.textContent = certificate.certificateNumber;

    number.append(numberLabel, numberValue);

    const hint = document.createElement('p');
    hint.className = 'certificate-verify-hint';
    hint.textContent = t('certificate.verifyHint');

    footer.append(number, buildQrCode(certificate), hint);
    return footer;
}

/**
 * QR ведёт на ту же страницу проверки, что и ссылка «Скопировать ссылку» —
 * то же самое, что печатается в PDF (QuestPdfCertificateGenerator), чтобы
 * скан с экрана и скан с распечатки давали один результат.
 */
function buildQrCode(certificate) {
    const wrapper = document.createElement('div');
    wrapper.className = 'certificate-qr';

    const number = encodeURIComponent(certificate.certificateNumber ?? '');
    const url = `${window.location.origin}/pages/verify-certificate.html?number=${number}`;

    loadQrScript()
        .then(() => {
            // eslint-disable-next-line no-new
            new window.QRCode(wrapper, { text: url, width: 84, height: 84, correctLevel: window.QRCode.CorrectLevel.M });
        })
        .catch(() => {
            // Без QR сертификат всё равно рабочий: номер и ссылку можно скопировать вручную.
            wrapper.remove();
        });

    return wrapper;
}

// ---------------------------------------------------------------------------
// Мелочи
// ---------------------------------------------------------------------------

function line(className, text) {
    const element = document.createElement('p');
    element.className = className;
    element.textContent = text;
    return element;
}

function appendPair(list, label, value) {
    const dt = document.createElement('dt');
    dt.textContent = label;

    const dd = document.createElement('dd');
    dd.textContent = value;

    list.append(dt, dd);
}
