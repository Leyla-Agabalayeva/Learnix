

import { api } from '../../api.js';
import { t } from '../../localization.js';
import { loader } from '../../components/loader.js';
import { emptyState } from '../../components/empty-state.js';
import * as format from '../../format.js';
import { startInstructorPage, showLoadError } from './common.js';
import { setupCoursePicker } from './course-picker.js';

const $ = (id) => document.getElementById(id);

async function load() {
    await setupCoursePicker(async (courseId) => {
        if (!courseId) {
            emptyState.render($('students-content'), {
                icon: '👥',
                title: t('instructor.noCoursesTitle'),
                text: t('instructor.noCoursesHint')
            });
            return;
        }

        await loadStudents(courseId);
    });
}

async function loadStudents(courseId) {
    const container = $('students-content');
    loader.skeleton(container, { count: 5, variant: 'row' });

    try {
        const students = await api.get(`/courses/${courseId}/students`);
        render(students);
    } catch (error) {
        showLoadError(container, error, () => loadStudents(courseId));
    }
}

function render(students) {
    const container = $('students-content');
    container.innerHTML = '';

    if (!students.length) {
        emptyState.render(container, {
            icon: '👥',
            title: t('instructor.noStudentsTitle'),
            text: t('instructor.noStudentsHint')
        });
        return;
    }

    const wrapper = document.createElement('div');
    wrapper.className = 'table-wrapper';

    const table = document.createElement('table');
    table.className = 'table';
    table.appendChild(buildHead());

    const body = document.createElement('tbody');

    // Сначала те, кто продвинулся дальше: преподавателю важнее видеть, кто
    // реально учится, чем алфавитный порядок.
    [...students]
        .sort((a, b) => b.progressPercentage - a.progressPercentage)
        .forEach((student) => body.appendChild(buildRow(student)));

    table.appendChild(body);
    wrapper.appendChild(table);
    container.appendChild(wrapper);
}

function buildHead() {
    const head = document.createElement('thead');
    const row = document.createElement('tr');

    const columns = [
        'instructor.colStudent', 'instructor.colEmail', 'instructor.colProgress',
        'instructor.colStatus', 'instructor.colEnrolled'
    ];

    columns.forEach((key) => {
        const cell = document.createElement('th');
        cell.scope = 'col';
        cell.textContent = t(key);
        row.appendChild(cell);
    });

    head.appendChild(row);
    return head;
}

function buildRow(student) {
    const tr = document.createElement('tr');

    // Имя с аватаром-инициалами — строка сканируется глазом быстрее.
    const nameCell = document.createElement('td');
    const name = document.createElement('div');
    name.className = 'flex items-center gap-3';

    const avatar = document.createElement('span');
    avatar.className = 'avatar';
    avatar.setAttribute('aria-hidden', 'true');
    avatar.textContent = (student.studentName ?? '?').slice(0, 1).toUpperCase();

    const label = document.createElement('span');
    label.textContent = student.studentName;

    name.append(avatar, label);
    nameCell.appendChild(name);
    tr.appendChild(nameCell);

    tr.appendChild(cell(student.studentEmail));
    tr.appendChild(buildProgressCell(student));
    tr.appendChild(buildStatusCell(student));
    tr.appendChild(cell(format.date(student.enrolledAt)));

    return tr;
}

function buildProgressCell(student) {
    const td = document.createElement('td');

    const wrapper = document.createElement('div');
    wrapper.className = 'score-cell';

    const percent = document.createElement('span');
    percent.textContent = `${format.rating(student.progressPercentage)}%`;

    const bar = document.createElement('div');
    bar.className = 'score-bar';

    const fill = document.createElement('div');
    fill.className = 'score-bar-fill';
    fill.style.width = `${Math.min(100, student.progressPercentage)}%`;
    fill.style.backgroundColor = student.status === 'Completed'
        ? 'var(--color-success)'
        : 'var(--color-accent)';

    bar.appendChild(fill);
    wrapper.append(percent, bar);
    td.appendChild(wrapper);

    return td;
}

function buildStatusCell(student) {
    const td = document.createElement('td');

    const badge = document.createElement('span');
    badge.className = {
        Completed: 'badge badge-success',
        Cancelled: 'badge badge-danger'
    }[student.status] ?? 'badge badge-primary';

    badge.textContent = t(student.status === 'Completed' ? 'student.completed' : 'student.inProgress');
    td.appendChild(badge);

    return td;
}

function cell(text) {
    const td = document.createElement('td');
    td.textContent = text ?? '';
    return td;
}

startInstructorPage(load);
