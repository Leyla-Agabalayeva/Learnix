/**
 * Профиль преподавателя.
 *
 * Форма та же, что у студента, — меняется только обвязка роли (startInstructorPage).
 * Логика редактирования не дублируется: оба профиля бьют в один эндпоинт
 * PUT /api/auth/me и используют один и тот же form-errors.js.
 *
 * Для преподавателя поле «О себе» — не украшение: биография видна студентам
 * рядом с его именем на странице курса.
 *
 * Email и роль показаны, но заблокированы: email — это логин, а роль
 * пользователь не может выдать себе сам. Сервер их и не принимает —
 * в UpdateProfileRequest этих полей просто нет.
 */

import { api } from '../../api.js';
import { t } from '../../localization.js';
import { getUser, saveSession, getToken } from '../../auth.js';
import { toast } from '../../components/toast.js';
import { modal } from '../../components/modal.js';
import { loader } from '../../components/loader.js';
import * as format from '../../format.js';
import { showFormErrors, clearFormErrors } from '../form-errors.js';
import { startInstructorPage, showLoadError } from './common.js';

const $ = (id) => document.getElementById(id);

let profile = null;

async function load() {
    try {
        profile = await api.get('/auth/me');
        render();
    } catch (error) {
        showLoadError($('profile-form').parentElement, error, load);
    }
}

function render() {
    renderAvatar();

    $('profile-name').textContent = `${profile.firstName} ${profile.lastName}`.trim();
    $('profile-email').textContent = profile.email;
    $('profile-meta').textContent =
        `${t('student.roleLabel')}: ${profile.roles.join(', ')} · ${t('student.accountCreated')} ${format.date(profile.createdAt)}`;

    $('firstName').value = profile.firstName ?? '';
    $('lastName').value = profile.lastName ?? '';
    $('bio').value = profile.bio ?? '';
    $('email').value = profile.email ?? '';
}

function renderAvatar() {
    const button = $('profile-avatar');
    button.innerHTML = '';

    const circle = document.createElement('span');
    circle.className = 'profile-avatar-circle';

    if (profile.avatarUrl) {
        const image = document.createElement('img');
        image.src = profile.avatarUrl;
        image.alt = '';
        image.addEventListener('error', () => {
            image.remove();
            circle.textContent = initials();
        });
        circle.appendChild(image);
    } else {
        circle.textContent = initials();
    }

    button.appendChild(circle);
    appendOverlay(button);
}

function appendOverlay(button) {
    const overlay = document.createElement('span');
    overlay.className = 'profile-avatar-overlay';
    overlay.setAttribute('aria-hidden', 'true');
    overlay.textContent = '📷';
    button.appendChild(overlay);
}

function initials() {
    return `${profile.firstName?.[0] ?? ''}${profile.lastName?.[0] ?? ''}`.toUpperCase() || '?';
}

function bindForm() {
    $('profile-form').addEventListener('submit', async (event) => {
        event.preventDefault();

        const form = event.currentTarget;
        const button = $('submit');

        clearFormErrors(form);
        hideFormError();
        loader.button(button, true);

        try {
            // avatarUrl намеренно не отправляем: аватар меняется отдельным
            // действием (клик по фото → загрузка файла или камера).
            profile = await api.put('/auth/me', {
                firstName: $('firstName').value.trim(),
                lastName: $('lastName').value.trim(),
                bio: $('bio').value.trim(),
                avatarUrl: profile.avatarUrl
            });

            // Имя в навбаре берётся из сохранённой сессии, а не из ответа /auth/me,
            // поэтому после сохранения её нужно обновить — иначе в шапке
            // останется старое имя до следующего входа.
            const user = getUser();
            saveSession({
                token: getToken(),
                userId: profile.userId,
                email: profile.email,
                firstName: profile.firstName,
                lastName: profile.lastName,
                roles: profile.roles,
                expiresAt: user?.expiresAt
            });

            render();
            toast.success(t('student.profileSaved'));
            document.dispatchEvent(new CustomEvent('lms:profile-updated'));
        } catch (error) {
            if (error.isValidationError) {
                showFormErrors(form, error.errors);
            } else {
                showFormError(error.isNetworkError ? t('states.networkError') : error.message);
            }
        } finally {
            loader.button(button, false);
        }
    });
}

function showFormError(message) {
    const box = $('form-error');
    box.textContent = message;
    box.classList.add('is-visible');
}

function hideFormError() {
    const box = $('form-error');
    box.textContent = '';
    box.classList.remove('is-visible');
}

function bindAvatarClick() {
    $('profile-avatar').addEventListener('click', openAvatarModal);
}

function openAvatarModal() {
    const buttonsRow = document.createElement('div');
    buttonsRow.className = 'flex gap-3';

    const cameraButton = document.createElement('button');
    cameraButton.type = 'button';
    cameraButton.className = 'btn btn-secondary';
    cameraButton.textContent = '📷 ' + t('student.useCamera');

    const uploadButton = document.createElement('button');
    uploadButton.type = 'button';
    uploadButton.className = 'btn btn-primary';
    uploadButton.textContent = '🖼️ ' + t('student.uploadPhoto');

    buttonsRow.append(cameraButton, uploadButton);

    const dialog = modal.open({
        title: t('student.updateAvatar'),
        content: buttonsRow
    });

    uploadButton.addEventListener('click', () => {
        dialog.close();
        openFilePicker();
    });

    cameraButton.addEventListener('click', () => {
        openCameraCapture(dialog);
    });
}

function openFilePicker() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/png, image/jpeg, image/webp, image/gif';
    input.style.display = 'none';

    input.addEventListener('change', async () => {
        const file = input.files?.[0];
        if (!file) {
            return;
        }
        await uploadAvatar(file);
    });

    document.body.appendChild(input);
    input.click();
    input.remove();
}

async function openCameraCapture(dialog) {
    let stream;
    try {
        stream = await navigator.mediaDevices.getUserMedia({ video: true });
    } catch {
        toast.error(t('student.cameraDenied'));
        return;
    }

    const video = document.createElement('video');
    video.autoplay = true;
    video.playsInline = true;
    video.srcObject = stream;
    video.style.width = '100%';
    video.style.borderRadius = 'var(--radius-lg)';

    const captureButton = document.createElement('button');
    captureButton.type = 'button';
    captureButton.className = 'btn btn-primary mt-4';
    captureButton.textContent = t('student.takePhoto');

    const wrapper = document.createElement('div');
    wrapper.append(video, captureButton);

    dialog.body.innerHTML = '';
    dialog.body.appendChild(wrapper);

    const stopStream = () => stream.getTracks().forEach((track) => track.stop());

    captureButton.addEventListener('click', async () => {
        const canvas = document.createElement('canvas');
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        canvas.getContext('2d').drawImage(video, 0, 0);

        stopStream();
        dialog.close();

        canvas.toBlob(async (blob) => {
            if (!blob) {
                return;
            }
            const file = new File([blob], 'camera-photo.jpg', { type: 'image/jpeg' });
            await uploadAvatar(file);
        }, 'image/jpeg', 0.9);
    });

    const originalClose = dialog.close;
    dialog.close = () => {
        stopStream();
        originalClose();
    };
}

async function uploadAvatar(file) {
    const formData = new FormData();
    formData.append('file', file);

    try {
        profile = await api.upload('/auth/me/avatar', formData);

        const user = getUser();
        saveSession({
            token: getToken(),
            userId: profile.userId,
            email: profile.email,
            firstName: profile.firstName,
            lastName: profile.lastName,
            roles: profile.roles,
            avatarUrl: profile.avatarUrl,
            expiresAt: user?.expiresAt
        });

        render();
        toast.success(t('student.profileSaved'));
        document.dispatchEvent(new CustomEvent('lms:profile-updated'));
    } catch (error) {
        toast.error(error.isNetworkError ? t('states.networkError') : error.message);
    }
}

// bindForm — в init: иначе после двух переключений языка форма
// отправлялась бы трижды.
startInstructorPage(load, { init: () => { bindForm(); bindAvatarClick(); } });