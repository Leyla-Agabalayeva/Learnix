/**
 * Ведомость студента — F6 (раздел 14 ТЗ).
 *
 * Все агрегаты приходят из /api/gradebook: средний балл, число сданных тестов
 * и процент завершения курсов считает сервер. На клиенте ничего не пересчитывается —
 * иначе цифры на дашборде и в ведомости могли бы разойтись.
 */

import { api } from '../../api.js';
import { t } from '../../localization.js';
import { loader } from '../../components/loader.js';
import { emptyState } from '../../components/empty-state.js';
import * as format from '../../format.js';
import { startStudentPage, showLoadError, buildStat } from './common.js';

const $ = (id) => document.getElementById(id);

async function load() {
    loader.skeleton($('grades-content'), { count: 5, variant: 'row' });

    try {
        const gradeBook = await api.get('/gradebook');
        renderSummary(gradeBook.summary);
        renderRows(gradeBook.rows);
    } catch (error) {
        $('summary').innerHTML = '';
        showLoadError($('grades-content'), error, load);
    }
}

function renderSummary(summary) {
    const container = $('summary');
    container.innerHTML = '';

    container.append(
        buildStat(t('student.statAverage'),
            summary.averageQuizScore > 0 ? `${format.rating(summary.averageQuizScore)}%` : '—',
            { tone: summary.averageQuizScore >= 60 ? 'success' : summary.averageQuizScore > 0 ? 'warning' : 'neutral' }),
        buildStat(t('student.passed'),
            `${format.number(summary.passedQuizzes)} / ${format.number(summary.completedQuizzes)}`,
            { tone: 'success' }),
        buildStat(t('course.progress'),
            `${format.rating(summary.averageCourseCompletion)}%`),
        buildStat(t('student.statCompleted'),
            `${format.number(summary.completedCoursesCount)} / ${format.number(summary.enrolledCoursesCount)}`,
            { tone: 'success' })
    );
}

function renderRows(rows) {
    const container = $('grades-content');
    container.innerHTML = '';

    if (!rows.length) {
        emptyState.render(container, {
            icon: '📊',
            title: t('student.gradesEmpty'),
            text: t('student.gradesEmptyHint'),
            action: { label: t('actions.explore'), href: '/pages/courses.html' }
        });
        return;
    }

    // Таблица в обёртке со скроллом: на телефоне она не ломает страницу,
    // а прокручивается внутри своего блока.
    const wrapper = document.createElement('div');
    wrapper.className = 'table-wrapper';

    const table = document.createElement('table');
    table.className = 'table';

    table.appendChild(buildHead());

    const body = document.createElement('tbody');
    // Свежие результаты сверху — так же, как в любом журнале оценок.
    [...rows]
        .sort((a, b) => new Date(b.completedAt ?? 0) - new Date(a.completedAt ?? 0))
        .forEach((row) => body.appendChild(buildRow(row)));

    table.appendChild(body);
    wrapper.appendChild(table);
    container.appendChild(wrapper);
}

function buildHead() {
    const head = document.createElement('thead');
    const row = document.createElement('tr');

    ['student.colCourse', 'student.colQuiz', 'student.colScore', 'student.colResult', 'student.colDate']
        .forEach((key) => {
            const cell = document.createElement('th');
            cell.scope = 'col';
            cell.textContent = t(key);
            row.appendChild(cell);
        });

    head.appendChild(row);
    return head;
}

function buildRow(row) {
    const tr = document.createElement('tr');

    tr.appendChild(cell(row.courseTitle));
    tr.appendChild(cell(row.quizTitle));

    // Балл с миниатюрной полосой: число само по себе читается хуже,
    // чем число рядом с визуальной шкалой.
    const scoreCell = document.createElement('td');
    const score = document.createElement('div');
    score.className = 'score-cell';

    const value = document.createElement('span');
    value.textContent = `${format.rating(row.percentage)}%`;

    const bar = document.createElement('div');
    bar.className = 'score-bar';
    const fill = document.createElement('div');
    fill.className = 'score-bar-fill';
    fill.style.width = `${Math.min(100, row.percentage)}%`;
    fill.style.backgroundColor = row.passed ? 'var(--color-success)' : 'var(--color-danger)';
    bar.appendChild(fill);

    score.append(value, bar);
    scoreCell.appendChild(score);
    tr.appendChild(scoreCell);

    const resultCell = document.createElement('td');
    const badge = document.createElement('span');
    badge.className = row.passed ? 'badge badge-success' : 'badge badge-danger';
    badge.textContent = t(row.passed ? 'student.passed' : 'student.failed');
    resultCell.appendChild(badge);
    tr.appendChild(resultCell);

    tr.appendChild(cell(format.date(row.completedAt)));

    return tr;
}

function cell(text) {
    const td = document.createElement('td');
    td.textContent = text ?? '';
    return td;
}

// Ведомость не зависит от языка контента: названия курсов приходят так,
// как их вернул сервер, поэтому перезапрашивать при смене языка нечего —
// но подписи колонок перерисовать надо.
startStudentPage(load);
