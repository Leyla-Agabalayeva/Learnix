/**
 * Управление курсами (админ-панель) — курсы всех преподавателей сразу,
 * в отличие от instructor/courses.js, который показывает только свои.
 */

import { api } from '../../api.js';
import { t } from '../../localization.js';
import { toast } from '../../components/toast.js';
import { loader } from '../../components/loader.js';
import { modal } from '../../components/modal.js';
import { emptyState } from '../../components/empty-state.js';
import { renderPagination } from '../../components/pagination.js';
import * as format from '../../format.js';
import { startAdminPage, showLoadError } from './common.js';

const $ = (id) => document.getElementById(id);

const STATUSES = ['Draft', 'Published', 'Archived'];

let state = { searchTerm: '', status: '', page: 1 };
let searchTimer = null;

async function load() {
    await fetchAndRender();
}

async function fetchAndRender() {
    const container = $('courses-content');
    loader.skeleton(container, { count: 5, variant: 'row' });

    try {
        const result = await api.get('/admin/courses', {
            query: { searchTerm: state.searchTerm, status: state.status, page: state.page, pageSize: 20 }
        });
        render(result);
    } catch (error) {
        showLoadError(container, error, fetchAndRender);
    }
}

function render(result) {
    const container = $('courses-content');
    container.innerHTML = '';

    if (!result.items.length) {
        emptyState.render(container, { icon: '📚', title: t('admin.noCourses') });
        renderPagination($('pagination'), { page: 1, totalPages: 0, onChange: () => {} });
        return;
    }

    const wrapper = document.createElement('div');
    wrapper.className = 'table-wrapper';

    const table = document.createElement('table');
    table.className = 'table';
    table.appendChild(buildHead());

    const body = document.createElement('tbody');
    result.items.forEach((course) => body.appendChild(buildRow(course)));
    table.appendChild(body);

    wrapper.appendChild(table);
    container.appendChild(wrapper);

    renderPagination($('pagination'), {
        page: result.page,
        totalPages: result.totalPages,
        onChange: (page) => {
            state.page = page;
            fetchAndRender();
        }
    });
}

function buildHead() {
    const head = document.createElement('thead');
    const row = document.createElement('tr');

    ['', t('admin.colTitle'), t('admin.colInstructor'), t('admin.colCategory'), t('admin.colStatus'),
        t('admin.colEnrollments'), t('admin.colCreated'), t('admin.colActions')]
        .forEach((label) => {
            const th = document.createElement('th');
            th.textContent = label;
            row.appendChild(th);
        });

    head.appendChild(row);
    return head;
}

function buildRow(course) {
    const row = document.createElement('tr');

    row.appendChild(buildThumbnailCell(course));
    row.appendChild(cell(course.title));
    row.appendChild(cell(course.instructorName));
    row.appendChild(cell(course.categoryName));

    const statusCell = document.createElement('td');
    const badge = document.createElement('span');
    badge.className = {
        Published: 'badge badge-success',
        Archived: 'badge badge-warning'
    }[course.status] ?? 'badge badge-neutral';
    badge.textContent = t(`course.status${course.status}`);
    statusCell.appendChild(badge);
    row.appendChild(statusCell);

    row.appendChild(cell(format.number(course.enrollmentCount)));
    row.appendChild(cell(format.date(course.createdAt)));

    const actionsCell = document.createElement('td');
    actionsCell.className = 'flex gap-2 flex-wrap';

    const statusSelect = document.createElement('select');
    statusSelect.className = 'select select-sm';
    STATUSES.forEach((status) => {
        const option = document.createElement('option');
        option.value = status;
        option.textContent = t({
            Published: 'admin.setPublished',
            Archived: 'admin.setArchived',
            Draft: 'admin.setDraft'
        }[status]);
        option.selected = status === course.status;
        statusSelect.appendChild(option);
    });
    statusSelect.addEventListener('change', () => setStatus(course, statusSelect.value));
    actionsCell.appendChild(statusSelect);

    const deleteButton = document.createElement('button');
    deleteButton.type = 'button';
    deleteButton.className = 'btn btn-danger btn-sm';
    deleteButton.textContent = t('actions.delete');
    deleteButton.addEventListener('click', () => deleteCourse(course));
    actionsCell.appendChild(deleteButton);

    row.appendChild(actionsCell);
    return row;
}

function cell(text) {
    const td = document.createElement('td');
    td.textContent = text;
    return td;
}

/**
 * Превью обложки + кнопка загрузки нового файла. Загрузка идёт напрямую из
 * таблицы — отдельная форма редактирования курса администратору не нужна,
 * это единственное поле курса, которое можно поменять отсюда.
 */
function buildThumbnailCell(course) {
    const td = document.createElement('td');

    const preview = document.createElement('div');
    preview.className = 'admin-course-thumb';
    renderThumbnailPreview(preview, course.thumbnailUrl);
    preview.title = t('admin.changeThumbnail');
    preview.addEventListener('click', () => pickThumbnail(course, preview));

    td.appendChild(preview);
    return td;
}

function renderThumbnailPreview(preview, url) {
    preview.innerHTML = '';

    if (url) {
        const image = document.createElement('img');
        image.src = url;
        image.alt = '';
        preview.appendChild(image);
    } else {
        preview.textContent = '🖼️';
    }
}

function pickThumbnail(course, preview) {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/png, image/jpeg, image/webp, image/svg+xml';
    input.style.display = 'none';

    input.addEventListener('change', async () => {
        const file = input.files?.[0];
        if (!file) {
            return;
        }

        const formData = new FormData();
        formData.append('file', file);

        try {
            const { url } = await api.upload(`/admin/courses/${course.id}/thumbnail`, formData);
            course.thumbnailUrl = url;
            renderThumbnailPreview(preview, url);
            toast.success(t('admin.thumbnailUpdated'));
        } catch (error) {
            toast.fromApiError(error);
        }
    });

    document.body.appendChild(input);
    input.click();
    input.remove();
}

async function setStatus(course, status) {
    try {
        await api.patch(`/admin/courses/${course.id}/status`, { status });
        toast.success(t('admin.courseStatusChanged'));
        await fetchAndRender();
    } catch (error) {
        toast.fromApiError(error);
        await fetchAndRender();
    }
}

async function deleteCourse(course) {
    const confirmed = await modal.confirm({
        title: t('admin.deleteCourseTitle'),
        message: t('admin.deleteCourseText'),
        confirmText: t('actions.delete'),
        danger: true
    });

    if (!confirmed) {
        return;
    }

    try {
        await api.delete(`/admin/courses/${course.id}`);
        toast.success(t('admin.courseDeleted'));
        await fetchAndRender();
    } catch (error) {
        toast.fromApiError(error);
    }
}

function bindFilters() {
    $('search').addEventListener('input', () => {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(() => {
            state.searchTerm = $('search').value.trim();
            state.page = 1;
            fetchAndRender();
        }, 300);
    });

    const statusFilter = $('status-filter');
    statusFilter.innerHTML = '';

    const allOption = document.createElement('option');
    allOption.value = '';
    allOption.textContent = t('admin.filterAllStatuses');
    statusFilter.appendChild(allOption);

    STATUSES.forEach((status) => {
        const option = document.createElement('option');
        option.value = status;
        option.textContent = t(`course.status${status}`);
        statusFilter.appendChild(option);
    });

    statusFilter.addEventListener('change', () => {
        state.status = statusFilter.value;
        state.page = 1;
        fetchAndRender();
    });
}

startAdminPage(load, { init: bindFilters, reloadOnLanguageChange: false });
