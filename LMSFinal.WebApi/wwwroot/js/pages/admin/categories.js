/**
 * Управление категориями каталога (админ-панель).
 *
 * Категорий обычно немного (десяток), поэтому список грузится целиком без
 * пагинации — в отличие от пользователей и курсов.
 */

import { api } from '../../api.js';
import { t, getApiLanguage } from '../../localization.js';
import { toast } from '../../components/toast.js';
import { loader } from '../../components/loader.js';
import { modal } from '../../components/modal.js';
import { emptyState } from '../../components/empty-state.js';
import { createTranslationTabs } from '../../components/translation-tabs.js';
import { startAdminPage, showLoadError } from './common.js';

const $ = (id) => document.getElementById(id);

async function load() {
    const container = $('categories-content');
    loader.skeleton(container, { count: 4, variant: 'row' });

    try {
        const categories = await api.get('/categories');
        render(categories);
    } catch (error) {
        showLoadError(container, error, load);
    }
}

function render(categories) {
    const container = $('categories-content');
    container.innerHTML = '';

    if (!categories.length) {
        emptyState.render(container, {
            icon: '🏷️',
            title: t('admin.noCategories'),
            action: { label: t('admin.addCategory'), onClick: () => openForm() }
        });
        return;
    }

    const wrapper = document.createElement('div');
    wrapper.className = 'table-wrapper';

    const table = document.createElement('table');
    table.className = 'table';

    const head = document.createElement('thead');
    const headRow = document.createElement('tr');
    [t('admin.fieldCategoryName'), t('admin.fieldSlug'), t('admin.colActions')].forEach((label) => {
        const th = document.createElement('th');
        th.textContent = label;
        headRow.appendChild(th);
    });
    head.appendChild(headRow);
    table.appendChild(head);

    const body = document.createElement('tbody');
    categories.forEach((category) => body.appendChild(buildRow(category)));
    table.appendChild(body);

    wrapper.appendChild(table);
    container.appendChild(wrapper);
}

function buildRow(category) {
    const row = document.createElement('tr');

    const nameCell = document.createElement('td');
    nameCell.textContent = pickName(category.translations);
    row.appendChild(nameCell);

    const slugCell = document.createElement('td');
    slugCell.textContent = category.slug;
    row.appendChild(slugCell);

    const actionsCell = document.createElement('td');
    actionsCell.className = 'flex gap-2';

    const editButton = document.createElement('button');
    editButton.type = 'button';
    editButton.className = 'btn btn-secondary btn-sm';
    editButton.textContent = t('actions.edit');
    editButton.addEventListener('click', () => openForm(category));
    actionsCell.appendChild(editButton);

    const deleteButton = document.createElement('button');
    deleteButton.type = 'button';
    deleteButton.className = 'btn btn-danger btn-sm';
    deleteButton.textContent = t('actions.delete');
    deleteButton.addEventListener('click', () => deleteCategory(category));
    actionsCell.appendChild(deleteButton);

    row.appendChild(actionsCell);
    return row;
}

function pickName(translations) {
    const list = translations ?? [];
    const current = getApiLanguage();

    const match = list.find((item) => (item.languageCode ?? '').toUpperCase() === current)
        ?? list.find((item) => (item.languageCode ?? '').toUpperCase() === 'AZ')
        ?? list[0];

    return match?.name || '—';
}

function openForm(category = null) {
    const container = document.createElement('div');

    const slugField = buildInput('category-slug', t('admin.fieldSlug'), category?.slug ?? '');
    const iconField = buildInput('category-icon', t('admin.fieldIcon'), category?.iconUrl ?? '');
    container.append(slugField, iconField);

    const tabsHost = document.createElement('div');
    container.appendChild(tabsHost);

    const initial = {};
    (category?.translations ?? []).forEach((translation) => {
        initial[(translation.languageCode ?? '').toUpperCase()] = {
            name: translation.name ?? '',
            description: translation.description ?? ''
        };
    });

    const tabs = createTranslationTabs(tabsHost, [
        { name: 'name', labelKey: 'admin.fieldCategoryName', required: true },
        { name: 'description', labelKey: 'admin.fieldCategoryDescription', type: 'textarea' }
    ], initial);

    const footer = document.createDocumentFragment();

    const cancelButton = document.createElement('button');
    cancelButton.type = 'button';
    cancelButton.className = 'btn btn-secondary';
    cancelButton.textContent = t('actions.cancel');

    const saveButton = document.createElement('button');
    saveButton.type = 'button';
    saveButton.className = 'btn btn-primary';
    saveButton.textContent = t('actions.save');

    footer.append(cancelButton, saveButton);

    const dialog = modal.open({
        title: t(category ? 'admin.editCategory' : 'admin.addCategory'),
        content: container,
        footer,
        size: 'lg'
    });

    cancelButton.addEventListener('click', () => dialog.close());

    saveButton.addEventListener('click', async () => {
        if (!tabs.validate()) {
            toast.warning(t('builder.atLeastOneLanguage'));
            return;
        }

        const payload = {
            slug: document.getElementById('category-slug').value.trim(),
            iconUrl: document.getElementById('category-icon').value.trim() || null,
            translations: tabs.getValues()
        };

        loader.button(saveButton, true);

        try {
            if (category) {
                await api.put(`/categories/${category.id}`, payload);
                toast.success(t('admin.categoryUpdated'));
            } else {
                await api.post('/categories', payload);
                toast.success(t('admin.categoryCreated'));
            }

            dialog.close();
            await load();
        } catch (error) {
            toast.fromApiError(error);
            loader.button(saveButton, false);
        }
    });
}

async function deleteCategory(category) {
    const confirmed = await modal.confirm({
        title: t('admin.deleteCategoryTitle'),
        message: t('admin.deleteCategoryText'),
        confirmText: t('actions.delete'),
        danger: true
    });

    if (!confirmed) {
        return;
    }

    try {
        await api.delete(`/categories/${category.id}`);
        toast.success(t('admin.categoryDeleted'));
        await load();
    } catch (error) {
        toast.fromApiError(error);
    }
}

function buildInput(id, label, value) {
    const wrapper = document.createElement('div');
    wrapper.className = 'field';

    const labelElement = document.createElement('label');
    labelElement.className = 'field-label';
    labelElement.htmlFor = id;
    labelElement.textContent = label;

    const input = document.createElement('input');
    input.className = 'input';
    input.id = id;
    input.value = value;

    wrapper.append(labelElement, input);
    return wrapper;
}

function bindAddButton() {
    $('add-category').addEventListener('click', () => openForm());
}

startAdminPage(load, { init: bindAddButton });
