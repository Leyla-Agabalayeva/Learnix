/**
 * Управление баннерами карусели на главной (админ-панель).
 *
 * Загрузка — через проводник, как обложки курсов и аватары: без ввода URL
 * руками. Порядок слайдов — по времени добавления (OrderIndex на сервере),
 * менять местами здесь не даём — для десятка баннеров это не нужно.
 */

import { api } from '../../api.js';
import { t } from '../../localization.js';
import { toast } from '../../components/toast.js';
import { loader } from '../../components/loader.js';
import { modal } from '../../components/modal.js';
import { emptyState } from '../../components/empty-state.js';
import { startAdminPage, showLoadError } from './common.js';

const $ = (id) => document.getElementById(id);

async function load() {
    const container = $('slides-content');
    loader.skeleton(container, { count: 3, variant: 'row' });

    try {
        const slides = await api.get('/hero-slides');
        render(slides);
    } catch (error) {
        showLoadError(container, error, load);
    }
}

function render(slides) {
    const container = $('slides-content');
    container.innerHTML = '';

    if (!slides.length) {
        emptyState.render(container, {
            icon: '🖼️',
            title: t('admin.noSlides'),
            action: { label: t('admin.addSlide'), onClick: () => pickFile() }
        });
        return;
    }

    slides.forEach((slide) => container.appendChild(buildCard(slide)));
}

function buildCard(slide) {
    const card = document.createElement('article');
    card.className = 'card admin-slide-card';

    const image = document.createElement('img');
    image.src = slide.imageUrl;
    image.alt = '';
    card.appendChild(image);

    const body = document.createElement('div');
    body.className = 'card-body';

    if (slide.linkUrl) {
        const link = document.createElement('p');
        link.className = 'text-sm text-muted admin-slide-link';
        link.textContent = slide.linkUrl;
        body.appendChild(link);
    }

    const deleteButton = document.createElement('button');
    deleteButton.type = 'button';
    deleteButton.className = 'btn btn-danger btn-sm w-full';
    deleteButton.textContent = t('actions.delete');
    deleteButton.addEventListener('click', () => deleteSlide(slide));
    body.appendChild(deleteButton);

    card.appendChild(body);
    return card;
}

async function deleteSlide(slide) {
    const confirmed = await modal.confirm({
        title: t('admin.deleteSlideTitle'),
        message: t('admin.deleteSlideText'),
        confirmText: t('actions.delete'),
        danger: true
    });

    if (!confirmed) {
        return;
    }

    try {
        await api.delete(`/admin/hero-slides/${slide.id}`);
        toast.success(t('admin.slideDeleted'));
        await load();
    } catch (error) {
        toast.fromApiError(error);
    }
}

function pickFile() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/png, image/jpeg, image/webp';
    input.style.display = 'none';

    input.addEventListener('change', async () => {
        const file = input.files?.[0];
        if (!file) {
            return;
        }

        const linkUrl = window.prompt(t('admin.slideLinkPrompt')) || '';

        const formData = new FormData();
        formData.append('file', file);
        if (linkUrl.trim()) {
            formData.append('linkUrl', linkUrl.trim());
        }

        try {
            await api.upload('/admin/hero-slides', formData);
            toast.success(t('admin.slideAdded'));
            await load();
        } catch (error) {
            toast.fromApiError(error);
        }
    });

    document.body.appendChild(input);
    input.click();
    input.remove();
}

function bindAddButton() {
    $('add-slide').addEventListener('click', pickFile);
}

startAdminPage(load, { init: bindAddButton });
