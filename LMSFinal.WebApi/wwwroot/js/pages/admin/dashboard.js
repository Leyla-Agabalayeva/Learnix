/**
 * Дашборд администратора: сводная статистика по всей платформе,
 * без учёта того, кто автор курса или чей это аккаунт.
 */

import { api } from '../../api.js';
import { t } from '../../localization.js';
import { getUser } from '../../auth.js';
import * as format from '../../format.js';
import { startAdminPage, buildStat } from './common.js';

const $ = (id) => document.getElementById(id);

// Существующие Chart.js-инстансы — уничтожаем перед перерисовкой (смена языка,
// повторный load), иначе на canvas накладываются друг на друга старые графики.
const charts = {};

async function load() {
    $('greeting').textContent = t('admin.welcome', { name: getUser()?.firstName ?? '' });

    try {
        const stats = await api.get('/admin/dashboard-stats');
        renderStats(stats);
        renderCharts(stats);
    } catch {
        $('stats-users').innerHTML = '';
        $('stats-courses').innerHTML = '';
    }
}

function renderCharts(stats) {
    const styles = getComputedStyle(document.documentElement);
    const textColor = styles.getPropertyValue('--color-text-muted').trim();
    const gridColor = styles.getPropertyValue('--color-border').trim();
    const primary = styles.getPropertyValue('--color-primary-600').trim();
    const secondary = styles.getPropertyValue('--color-secondary-500').trim();

    renderChart('chart-signups', 'line', {
        labels: stats.signupsLast30Days.map((d) => d.date.slice(5)),
        datasets: [{
            data: stats.signupsLast30Days.map((d) => d.count),
            borderColor: primary,
            backgroundColor: `${primary}22`,
            fill: true,
            tension: 0.35,
            pointRadius: 0
        }]
    }, {
        plugins: { legend: { display: false } },
        scales: {
            x: { ticks: { color: textColor, maxRotation: 0, autoSkip: true }, grid: { display: false } },
            y: { ticks: { color: textColor, precision: 0 }, grid: { color: gridColor } }
        }
    });

    renderChart('chart-course-status', 'doughnut', {
        labels: [t('admin.statPublished'), t('admin.statDraft'), t('admin.statArchived')],
        datasets: [{
            data: [stats.publishedCourses, stats.draftCourses, stats.archivedCourses],
            backgroundColor: [primary, secondary, gridColor]
        }]
    }, { plugins: { legend: { position: 'bottom', labels: { color: textColor } } } });

    renderChart('chart-user-roles', 'doughnut', {
        labels: [t('admin.statStudents'), t('admin.statInstructors'), t('admin.statAdmins')],
        datasets: [{
            data: [stats.totalStudents, stats.totalInstructors, stats.totalAdmins],
            backgroundColor: [primary, secondary, gridColor]
        }]
    }, { plugins: { legend: { position: 'bottom', labels: { color: textColor } } } });
}

function renderChart(canvasId, type, data, options) {
    charts[canvasId]?.destroy();

    const canvas = $(canvasId);
    if (!canvas || typeof Chart === 'undefined') {
        return; // Chart.js не подгрузился (например, файл /js/vendor заблокирован) — молча пропускаем графики
    }

    charts[canvasId] = new Chart(canvas, {
        type, data,
        options: { responsive: true, maintainAspectRatio: false, ...options }
    });
}

function renderStats(stats) {
    const users = $('stats-users');
    users.innerHTML = '';
    users.append(
        buildStat(t('admin.statUsers'), format.number(stats.totalUsers)),
        buildStat(t('admin.statStudents'), format.number(stats.totalStudents), { tone: 'success' }),
        buildStat(t('admin.statInstructors'), format.number(stats.totalInstructors)),
        buildStat(t('admin.statAdmins'), format.number(stats.totalAdmins), { tone: 'neutral' })
    );

    const courses = $('stats-courses');
    courses.innerHTML = '';
    courses.append(
        buildStat(t('admin.statCourses'), format.number(stats.totalCourses)),
        buildStat(t('admin.statPublished'), format.number(stats.publishedCourses), { tone: 'success' }),
        buildStat(t('admin.statDraft'), format.number(stats.draftCourses), { tone: 'neutral' }),
        buildStat(t('admin.statArchived'), format.number(stats.archivedCourses), { tone: 'warning' }),
        buildStat(t('admin.statCategories'), format.number(stats.totalCategories)),
        buildStat(t('admin.statEnrollments'), format.number(stats.totalEnrollments), { tone: 'success' }),
        buildStat(t('admin.statCertificates'), format.number(stats.totalCertificatesIssued), { tone: 'success' })
    );
}

startAdminPage(load);
