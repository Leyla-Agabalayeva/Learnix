/**
 * Управление пользователями (админ-панель).
 *
 * Поиск и фильтр по роли — с debounce и через сервер (не на клиенте), потому
 * что пользователей может быть много и постранично тянуть их всех нет смысла.
 */

import { api } from '../../api.js';
import { t } from '../../localization.js';
import { getUser } from '../../auth.js';
import { toast } from '../../components/toast.js';
import { loader } from '../../components/loader.js';
import { modal } from '../../components/modal.js';
import { emptyState } from '../../components/empty-state.js';
import { renderPagination } from '../../components/pagination.js';
import * as format from '../../format.js';
import { startAdminPage, showLoadError } from './common.js';

const $ = (id) => document.getElementById(id);

const ROLES = ['Admin', 'Instructor', 'Student'];

let state = { searchTerm: '', role: '', page: 1 };
let searchTimer = null;

async function load() {
    await fetchAndRender();
}

async function fetchAndRender() {
    const container = $('users-content');
    loader.skeleton(container, { count: 5, variant: 'row' });

    try {
        const result = await api.get('/admin/users', {
            query: { searchTerm: state.searchTerm, role: state.role, page: state.page, pageSize: 20 }
        });
        render(result);
    } catch (error) {
        showLoadError(container, error, fetchAndRender);
    }
}

function render(result) {
    const container = $('users-content');
    container.innerHTML = '';

    if (!result.items.length) {
        emptyState.render(container, { icon: '👥', title: t('admin.noUsers') });
        renderPagination($('pagination'), { page: 1, totalPages: 0, onChange: () => {} });
        return;
    }

    const wrapper = document.createElement('div');
    wrapper.className = 'table-wrapper';

    const table = document.createElement('table');
    table.className = 'table';
    table.appendChild(buildHead());

    const body = document.createElement('tbody');
    result.items.forEach((user) => body.appendChild(buildRow(user)));
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

    [t('admin.colName'), t('admin.colEmail'), t('admin.colRole'), t('admin.colStatus'), t('admin.colJoined'), t('admin.colActions')]
        .forEach((label) => {
            const th = document.createElement('th');
            th.textContent = label;
            row.appendChild(th);
        });

    head.appendChild(row);
    return head;
}

function buildRow(user) {
    const row = document.createElement('tr');
    const isSelf = user.id === getUser()?.userId;

    row.appendChild(cell(`${user.firstName} ${user.lastName}`.trim()));
    row.appendChild(cell(user.email));

    const roleCell = document.createElement('td');
    roleCell.className = 'flex gap-2 flex-wrap';
    user.roles.forEach((role) => {
        const badge = document.createElement('span');
        badge.className = 'badge badge-neutral';
        badge.textContent = role;
        roleCell.appendChild(badge);
    });
    row.appendChild(roleCell);

    const statusCell = document.createElement('td');
    const statusBadge = document.createElement('span');
    statusBadge.className = user.isLocked ? 'badge badge-danger' : 'badge badge-success';
    statusBadge.textContent = t(user.isLocked ? 'admin.statusLocked' : 'admin.statusActive');
    statusCell.appendChild(statusBadge);
    row.appendChild(statusCell);

    row.appendChild(cell(format.date(user.createdAt)));

    const actionsCell = document.createElement('td');
    actionsCell.className = 'flex gap-2';

    if (!isSelf) {
        const lockButton = document.createElement('button');
        lockButton.type = 'button';
        lockButton.className = 'btn btn-secondary btn-sm';
        lockButton.textContent = t(user.isLocked ? 'admin.unlockUser' : 'admin.lockUser');
        lockButton.addEventListener('click', () => toggleLock(user));
        actionsCell.appendChild(lockButton);

        const deleteButton = document.createElement('button');
        deleteButton.type = 'button';
        deleteButton.className = 'btn btn-danger btn-sm';
        deleteButton.textContent = t('actions.delete');
        deleteButton.addEventListener('click', () => deleteUser(user));
        actionsCell.appendChild(deleteButton);
    }

    row.appendChild(actionsCell);
    return row;
}

function cell(text) {
    const td = document.createElement('td');
    td.textContent = text;
    return td;
}

async function toggleLock(user) {
    const locking = !user.isLocked;

    const confirmed = await modal.confirm({
        title: t(locking ? 'admin.lockUserTitle' : 'admin.unlockUserTitle'),
        message: t(locking ? 'admin.lockUserText' : 'admin.unlockUserText'),
        confirmText: t(locking ? 'admin.lockUser' : 'admin.unlockUser'),
        danger: locking
    });

    if (!confirmed) {
        return;
    }

    try {
        await api.patch(`/admin/users/${user.id}/lock`, { locked: locking });
        toast.success(t(locking ? 'admin.userLocked' : 'admin.userUnlocked'));
        await fetchAndRender();
    } catch (error) {
        toast.fromApiError(error);
    }
}

async function deleteUser(user) {
    const confirmed = await modal.confirm({
        title: t('admin.deleteUserTitle'),
        message: t('admin.deleteUserText'),
        confirmText: t('actions.delete'),
        danger: true
    });

    if (!confirmed) {
        return;
    }

    try {
        await api.delete(`/admin/users/${user.id}`);
        toast.success(t('admin.userDeleted'));
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

    const roleFilter = $('role-filter');
    roleFilter.innerHTML = '';

    const allOption = document.createElement('option');
    allOption.value = '';
    allOption.textContent = t('admin.filterAllRoles');
    roleFilter.appendChild(allOption);

    ROLES.forEach((role) => {
        const option = document.createElement('option');
        option.value = role;
        option.textContent = role;
        roleFilter.appendChild(option);
    });

    roleFilter.addEventListener('change', () => {
        state.role = roleFilter.value;
        state.page = 1;
        fetchAndRender();
    });
}

startAdminPage(load, { init: bindFilters, reloadOnLanguageChange: false });
