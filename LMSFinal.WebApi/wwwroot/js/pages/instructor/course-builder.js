/**
 * Course Builder — конструктор курса (разделы 6 и 28 ТЗ).
 *
 * Три вещи, которые здесь важны:
 *
 * 1. **Порядок сохраняется одним запросом на весь список.** После перетаскивания
 *    уходит PUT /api/modules/reorder со всеми модулями сразу, а не по одному
 *    запросу на элемент. Иначе при быстром перетаскивании нескольких подряд
 *    запросы пришли бы вразнобой, и на сервере остался бы неверный порядок.
 *
 * 2. **Оптимистичный порядок с откатом.** DOM переставляется сразу — ждать
 *    ответа сервера, глядя на «прыгающий» список, невыносимо. Если запрос
 *    упал, список перезагружается с сервера и возвращается как было.
 *
 * 3. **Модули и уроки перетаскиваются независимо.** У API два разных эндпоинта
 *    reorder, и оба требуют, чтобы все элементы принадлежали одному родителю
 *    (иначе 409). Поэтому каждый список уроков — свой sortable-контейнер.
 */

import { api } from '../../api.js';
import { getApiLanguage, t } from '../../localization.js';
import { toast } from '../../components/toast.js';
import { loader } from '../../components/loader.js';
import { modal } from '../../components/modal.js';
import { emptyState } from '../../components/empty-state.js';
import { makeSortable, createDragHandle } from '../../components/drag-list.js';
import { createTranslationTabs } from '../../components/translation-tabs.js';
import * as format from '../../format.js';
import { startInstructorPage, showLoadError } from './common.js';

const $ = (id) => document.getElementById(id);

const courseId = new URLSearchParams(window.location.search).get('id');

let course = null;
let modules = [];

// ---------------------------------------------------------------------------
// Загрузка
// ---------------------------------------------------------------------------

async function load() {
    if (!courseId) {
        emptyState.render($('modules'), {
            icon: '🔍',
            title: t('states.notFound'),
            action: { label: t('nav.myCourses'), href: '/pages/instructor/courses.html' }
        });
        return;
    }

    loader.block($('modules'));

    try {
        // Модули и уроки запрашиваются БЕЗ ?lang= — намеренно.
        //
        // Локализованный ответ несёт только один язык, а PUT /api/modules/{id}
        // заменяет переводы целиком (Translations.Clear() на сервере). Открыв
        // форму без остальных языков и сохранив её, автор безвозвратно стёр бы
        // переводы, которых просто не видел. Поэтому конструктор всегда работает
        // с полным массивом Translations.
        const [courseData, moduleList] = await Promise.all([
            api.get(`/courses/${courseId}`, { query: { lang: getApiLanguage() } }),
            api.get(`/courses/${courseId}/modules`)
        ]);

        course = courseData;
        modules = [...moduleList].sort((a, b) => a.orderIndex - b.orderIndex);

        // Уроки грузятся для всех модулей сразу, а не по клику: конструктор
        // без содержимого бесполезен, а модулей у курса единицы.
        await Promise.all(modules.map(async (module) => {
            module.lessons = await api.get(`/modules/${module.id}/lessons`);
            module.lessons.sort((a, b) => a.orderIndex - b.orderIndex);
        }));

        renderCourseHeader();
        renderModules();
    } catch (error) {
        showLoadError($('modules'), error, load);
    }
}

// ---------------------------------------------------------------------------
// Шапка курса и смена статуса
// ---------------------------------------------------------------------------

function renderCourseHeader() {
    $('course-title').textContent = course.title;
    $('preview-link').href = `/pages/course-details.html?id=${course.id}`;

    const badge = $('course-status-badge');
    badge.className = {
        Published: 'badge badge-success',
        Archived: 'badge badge-warning'
    }[course.status] ?? 'badge badge-neutral';
    badge.textContent = t(`course.status${course.status}`);

    const lessonCount = modules.reduce((sum, module) => sum + (module.lessons?.length ?? 0), 0);
    $('course-summary').textContent =
        `${format.plural(modules.length, 'units.module')} · ${format.plural(lessonCount, 'units.lesson')}`;

    renderStatusActions();
}

function renderStatusActions() {
    const container = $('status-actions');
    container.innerHTML = '';

    if (course.status === 'Draft') {
        const hint = document.createElement('span');
        hint.className = 'text-sm text-muted';
        hint.textContent = t('builder.publishHint');
        container.appendChild(hint);
        container.appendChild(actionButton('actions.publish', 'btn-primary', 'publish'));
    }

    if (course.status === 'Published') {
        container.appendChild(actionButton('actions.unpublish', 'btn-secondary', 'unpublish'));
        container.appendChild(actionButton('actions.archive', 'btn-secondary', 'archive'));
    }

    if (course.status === 'Archived') {
        container.appendChild(actionButton('actions.publish', 'btn-primary', 'publish'));
    }
}

function actionButton(labelKey, style, action) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = `btn ${style} btn-sm`;
    button.textContent = t(labelKey);
    button.addEventListener('click', () => changeStatus(action, button));
    return button;
}

async function changeStatus(action, button) {
    loader.button(button, true);

    try {
        course = await api.post(`/courses/${course.id}/${action}`, undefined, {
            query: { lang: getApiLanguage() }
        });

        toast.success(t({
            publish: 'builder.coursePublished',
            unpublish: 'builder.courseUnpublished',
            archive: 'builder.courseArchived'
        }[action]));

        // Перезагружаем целиком: сервер мог отклонить публикацию, и статус
        // должен прийти от него, а не быть выставлен локально.
        await load();
    } catch (error) {
        // 409 при публикации — курс не готов (например, нет названия).
        toast.fromApiError(error);
        loader.button(button, false);
    }
}

// ---------------------------------------------------------------------------
// Модули
// ---------------------------------------------------------------------------

function renderModules() {
    const container = $('modules');
    container.innerHTML = '';

    if (!modules.length) {
        emptyState.render(container, {
            icon: '🧱',
            title: t('builder.noModules'),
            text: t('builder.noModulesHint'),
            action: { label: t('actions.addModule'), onClick: () => openModuleForm() }
        });
        return;
    }

    modules.forEach((module, index) => container.appendChild(buildModule(module, index)));

    makeSortable(container, {
        itemSelector: '.builder-module',
        handleSelector: '.drag-handle',
        onReorder: saveModuleOrder
    });
}

function buildModule(module, index) {
    const element = document.createElement('section');
    element.className = 'builder-module';
    element.dataset.id = module.id;
    element.draggable = true;

    const head = document.createElement('div');
    head.className = 'builder-module-head';

    const handle = createDragHandle(t('builder.dragHandleLabel'));

    const number = document.createElement('span');
    number.className = 'builder-module-index';
    number.textContent = `${index + 1}.`;

    const title = document.createElement('span');
    title.className = 'builder-module-title';
    title.textContent = pickText(module.translations, 'title');

    const meta = document.createElement('span');
    meta.className = 'module-meta';
    meta.textContent = format.plural(module.lessons?.length ?? 0, 'units.lesson');

    const actions = document.createElement('div');
    actions.className = 'builder-actions';
    actions.append(
        iconButton(t('actions.edit'), '✎', () => openModuleForm(module)),
        iconButton(t('actions.delete'), '🗑', () => deleteModule(module))
    );

    head.append(handle, number, title, meta, actions);
    element.appendChild(head);
    element.appendChild(buildLessons(module));

    const footer = document.createElement('div');
    footer.className = 'builder-module-footer';

    const addLesson = document.createElement('button');
    addLesson.type = 'button';
    addLesson.className = 'btn btn-ghost btn-sm';
    addLesson.textContent = `+ ${t('actions.addLesson')}`;
    addLesson.addEventListener('click', () => openLessonForm(module));

    footer.appendChild(addLesson);
    element.appendChild(footer);

    return element;
}

async function saveModuleOrder(orderedIds) {
    try {
        await api.put('/modules/reorder', {
            items: orderedIds.map((id, index) => ({ moduleId: id, orderIndex: index }))
        });

        // Держим локальный порядок в соответствии с сохранённым, иначе
        // следующая перерисовка вернула бы старые номера.
        modules.sort((a, b) => orderedIds.indexOf(a.id) - orderedIds.indexOf(b.id));
        renumberModules();

        toast.success(t('builder.orderSaved'));
    } catch (error) {
        toast.fromApiError(error);
        // Откат: перезагружаем порядок с сервера.
        await load();
    }
}

/** Обновляет только номера — полная перерисовка сбросила бы фокус с ручки. */
function renumberModules() {
    $('modules').querySelectorAll('.builder-module').forEach((element, index) => {
        const number = element.querySelector('.builder-module-index');
        if (number) {
            number.textContent = `${index + 1}.`;
        }
    });
}

async function deleteModule(module) {
    const confirmed = await modal.confirm({
        title: t('builder.deleteModuleTitle'),
        message: t('builder.deleteModuleText'),
        confirmText: t('actions.delete'),
        danger: true
    });

    if (!confirmed) {
        return;
    }

    try {
        await api.delete(`/modules/${module.id}`);
        toast.success(t('builder.moduleDeleted'));
        await load();
    } catch (error) {
        toast.fromApiError(error);
    }
}

function openModuleForm(module = null) {
    const container = document.createElement('div');

    const tabs = createTranslationTabs(container, [
        { name: 'title', labelKey: 'builder.fieldModuleTitle', required: true },
        { name: 'description', labelKey: 'builder.fieldModuleDescription', type: 'textarea' }
    ], toInitialValues(module?.translations, ['title', 'description']));

    openFormModal({
        title: t(module ? 'builder.editModule' : 'builder.newModule'),
        content: container,
        validate: () => tabs.validate(),
        submit: async () => {
            const payload = { translations: tabs.getValues() };

            if (module) {
                await api.put(`/modules/${module.id}`, payload);
                toast.success(t('builder.moduleUpdated'));
            } else {
                await api.post(`/courses/${courseId}/modules`, payload);
                toast.success(t('builder.moduleCreated'));
            }
        }
    });
}

// ---------------------------------------------------------------------------
// Уроки
// ---------------------------------------------------------------------------

function buildLessons(module) {
    const container = document.createElement('div');
    container.className = 'builder-lessons';
    container.dataset.moduleId = module.id;

    const lessons = module.lessons ?? [];

    if (!lessons.length) {
        const empty = document.createElement('p');
        empty.className = 'builder-empty-lessons';
        empty.textContent = t('builder.noLessons');
        container.appendChild(empty);
        return container;
    }

    lessons.forEach((lesson) => container.appendChild(buildLesson(module, lesson)));

    // Свой sortable на каждый модуль: эндпоинт reorder уроков требует,
    // чтобы все уроки в запросе принадлежали одному модулю (иначе 409).
    makeSortable(container, {
        itemSelector: '.builder-lesson',
        handleSelector: '.drag-handle',
        onReorder: (orderedIds) => saveLessonOrder(module, orderedIds)
    });

    return container;
}

function buildLesson(module, lesson) {
    const element = document.createElement('div');
    element.className = 'builder-lesson';
    element.dataset.id = lesson.id;
    element.draggable = true;

    const handle = createDragHandle(t('builder.dragHandleLabel'));

    const title = document.createElement('span');
    title.className = 'builder-lesson-title';
    title.textContent = pickText(lesson.translations, 'title');

    const status = document.createElement('span');
    status.className = lesson.isPublished ? 'badge badge-success' : 'badge badge-neutral';
    status.textContent = t(lesson.isPublished ? 'builder.lessonPublished' : 'builder.lessonDraft');

    const meta = document.createElement('span');
    meta.className = 'builder-lesson-meta';
    meta.textContent = format.duration(lesson.durationMinutes);

    const actions = document.createElement('div');
    actions.className = 'builder-actions';
    actions.append(
        iconButton(t('builder.materials'), '📎', () => openMaterialsForm(lesson)),
        iconButton(lesson.quizId ? t('builder.editQuiz') : t('builder.addQuiz'), '📝', () => openQuizForm(module, lesson)),
        iconButton(t('actions.edit'), '✎', () => openLessonForm(module, lesson)),
        iconButton(t('actions.delete'), '🗑', () => deleteLesson(lesson))
    );

    element.append(handle, title, status, meta, actions);
    return element;
}

async function saveLessonOrder(module, orderedIds) {
    try {
        await api.put('/lessons/reorder', {
            items: orderedIds.map((id, index) => ({ lessonId: id, orderIndex: index }))
        });

        module.lessons.sort((a, b) => orderedIds.indexOf(a.id) - orderedIds.indexOf(b.id));
        toast.success(t('builder.orderSaved'));
    } catch (error) {
        toast.fromApiError(error);
        await load();
    }
}

async function deleteLesson(lesson) {
    const confirmed = await modal.confirm({
        title: t('builder.deleteLessonTitle'),
        message: t('builder.deleteLessonText'),
        confirmText: t('actions.delete'),
        danger: true
    });

    if (!confirmed) {
        return;
    }

    try {
        await api.delete(`/lessons/${lesson.id}`);
        toast.success(t('builder.lessonDeleted'));
        await load();
    } catch (error) {
        toast.fromApiError(error);
    }
}

function openLessonForm(module, lesson = null) {
    const container = document.createElement('div');

    // Поля, общие для всех языков, идут до вкладок: длительность и ссылка
    // на видео не переводятся, и повторять их на каждой вкладке было бы странно.
    const shared = document.createElement('div');

    shared.appendChild(buildInput('lesson-video', t('builder.videoUrl'), lesson?.videoUrl ?? ''));
    shared.appendChild(buildInput('lesson-duration', t('builder.lessonDuration'),
        String(lesson?.durationMinutes ?? 10), 'number'));

    const publishRow = document.createElement('div');
    publishRow.className = 'checkbox-row mb-4';

    const publishInput = document.createElement('input');
    publishInput.type = 'checkbox';
    publishInput.id = 'lesson-published';
    publishInput.checked = lesson ? lesson.isPublished : true;

    const publishLabel = document.createElement('label');
    publishLabel.htmlFor = 'lesson-published';
    publishLabel.className = 'text-sm';
    publishLabel.textContent = t('builder.publishLesson');

    publishRow.append(publishInput, publishLabel);
    shared.appendChild(publishRow);

    container.appendChild(shared);

    const tabsHost = document.createElement('div');
    container.appendChild(tabsHost);

    const tabs = createTranslationTabs(tabsHost, [
        { name: 'title', labelKey: 'builder.fieldLessonTitle', required: true },
        { name: 'description', labelKey: 'builder.fieldLessonSummary' },
        { name: 'content', labelKey: 'builder.fieldLessonContent', type: 'textarea', required: true }
    ], toInitialValues(lesson?.translations, ['title', 'description', 'content']));

    openFormModal({
        title: t(lesson ? 'builder.editLesson' : 'builder.newLesson'),
        content: container,
        size: 'lg',
        validate: () => tabs.validate(),
        submit: async () => {
            const payload = {
                videoUrl: document.getElementById('lesson-video').value.trim() || null,
                durationMinutes: Number(document.getElementById('lesson-duration').value) || 0,
                isPublished: document.getElementById('lesson-published').checked,
                translations: tabs.getValues()
            };

            if (lesson) {
                await api.put(`/lessons/${lesson.id}`, payload);
                toast.success(t('builder.lessonUpdated'));
            } else {
                await api.post(`/modules/${module.id}/lessons`, payload);
                toast.success(t('builder.lessonCreated'));
            }
        }
    });
}

// ---------------------------------------------------------------------------
// Тест (F4): один урок — максимум один тест. Вопросы и варианты ответа
// переводятся так же, как название/содержимое урока — вкладками AZ/EN/RU,
// по отдельному экземпляру createTranslationTabs на каждый вопрос и каждый
// вариант ответа (компонент не хранит общего состояния и не мешает соседям).
// ---------------------------------------------------------------------------

async function openQuizForm(module, lesson) {
    let quiz = null;

    if (lesson.quizId) {
        try {
            // Без ?lang= — конструктору нужны все переводы сразу, по той же
            // причине, что и модулям/урокам (см. комментарий в load()).
            quiz = await api.get(`/quizzes/${lesson.quizId}`);
        } catch (error) {
            toast.fromApiError(error);
            return;
        }
    }

    const container = document.createElement('div');
    const questionEntries = [];

    if (quiz) {
        const deleteRow = document.createElement('div');
        deleteRow.className = 'mb-4';

        const deleteButton = document.createElement('button');
        deleteButton.type = 'button';
        deleteButton.className = 'btn btn-danger btn-sm';
        deleteButton.textContent = t('actions.delete');
        deleteButton.addEventListener('click', () => deleteQuiz(quiz, dialog));

        deleteRow.appendChild(deleteButton);
        container.appendChild(deleteRow);
    }

    const shared = document.createElement('div');
    shared.className = 'flex gap-3';
    shared.appendChild(buildInput('quiz-passing-score', t('builder.passingScore'), String(quiz?.passingScore ?? 60), 'number'));
    shared.appendChild(buildInput('quiz-time-limit', t('builder.timeLimitMinutes'), String(quiz?.timeLimitMinutes ?? ''), 'number'));
    container.appendChild(shared);

    const quizTabsHost = document.createElement('div');
    container.appendChild(quizTabsHost);

    const quizTabs = createTranslationTabs(quizTabsHost, [
        { name: 'title', labelKey: 'builder.fieldQuizTitle', required: true },
        { name: 'description', labelKey: 'builder.fieldQuizDescription', type: 'textarea' }
    ], toInitialValues(quiz?.translations, ['title', 'description']));

    const questionsHost = document.createElement('div');
    questionsHost.className = 'mt-4';
    container.appendChild(questionsHost);

    const addQuestionButton = document.createElement('button');
    addQuestionButton.type = 'button';
    addQuestionButton.className = 'btn btn-secondary mt-2';
    addQuestionButton.textContent = t('builder.addQuestion');
    addQuestionButton.addEventListener('click', () => addQuestionEntry(null));

    container.appendChild(addQuestionButton);

    function addAnswerEntry(question, answerData) {
        const answerRoot = document.createElement('div');
        answerRoot.className = 'quiz-builder-answer mt-3';

        const row = document.createElement('div');
        row.className = 'flex gap-3 items-center justify-between';

        const correctLabel = document.createElement('label');
        correctLabel.className = 'checkbox-row';

        const correctInput = document.createElement('input');
        correctInput.type = 'checkbox';
        correctInput.checked = answerData?.isCorrect ?? false;

        const correctText = document.createElement('span');
        correctText.className = 'text-sm';
        correctText.textContent = t('builder.correctAnswer');

        correctLabel.append(correctInput, correctText);

        const removeButton = iconButton(t('builder.removeAnswer'), '🗑', () => {
            question.answers = question.answers.filter((item) => item !== entry);
            answerRoot.remove();
        });

        row.append(correctLabel, removeButton);

        const tabsHost = document.createElement('div');
        const tabs = createTranslationTabs(tabsHost, [
            { name: 'answerText', labelKey: 'builder.fieldAnswerText', required: true }
        ], toInitialValues(answerData?.translations, ['answerText']));

        answerRoot.append(row, tabsHost);
        question.answersHost.appendChild(answerRoot);

        const entry = { correctInput, tabs };
        question.answers.push(entry);
        return entry;
    }

    function addQuestionEntry(questionData) {
        const root = document.createElement('div');
        root.className = 'card mt-4';

        const body = document.createElement('div');
        body.className = 'card-body';

        const header = document.createElement('div');
        header.className = 'flex gap-3 items-center justify-between mb-3';

        const heading = document.createElement('strong');
        heading.textContent = `${t('builder.question')} ${questionEntries.length + 1}`;

        const removeQuestionButton = iconButton(t('builder.removeQuestion'), '🗑', () => {
            const index = questionEntries.indexOf(question);
            if (index >= 0) {
                questionEntries.splice(index, 1);
            }
            root.remove();
        });

        header.append(heading, removeQuestionButton);

        const fieldsRow = document.createElement('div');
        fieldsRow.className = 'flex gap-3 mb-3';

        const typeWrap = document.createElement('div');
        typeWrap.className = 'field';

        const typeLabel = document.createElement('label');
        typeLabel.className = 'field-label';
        typeLabel.textContent = t('builder.questionType');

        const typeSelect = document.createElement('select');
        typeSelect.className = 'select';

        [['SingleChoice', 'singleChoice'], ['MultipleChoice', 'multipleChoice']].forEach(([value, key]) => {
            const option = document.createElement('option');
            option.value = value;
            option.textContent = t(`builder.${key}`);
            option.selected = questionData?.questionType === value;
            typeSelect.appendChild(option);
        });

        typeWrap.append(typeLabel, typeSelect);

        const pointsField = buildInput(
            `quiz-question-points-${questionEntries.length}-${Date.now()}`,
            t('builder.points'),
            String(questionData?.points ?? 1),
            'number');

        fieldsRow.append(typeWrap, pointsField);

        const questionTabsHost = document.createElement('div');
        const questionTabs = createTranslationTabs(questionTabsHost, [
            { name: 'questionText', labelKey: 'builder.fieldQuestionText', required: true }
        ], toInitialValues(questionData?.translations, ['questionText']));

        const answersHost = document.createElement('div');

        const addAnswerButton = document.createElement('button');
        addAnswerButton.type = 'button';
        addAnswerButton.className = 'btn btn-secondary btn-sm mt-3';
        addAnswerButton.textContent = t('builder.addAnswer');
        addAnswerButton.addEventListener('click', () => addAnswerEntry(question, null));

        const question = {
            typeSelect,
            pointsInput: pointsField.querySelector('input'),
            tabs: questionTabs,
            answers: [],
            answersHost
        };

        body.append(header, fieldsRow, questionTabsHost, answersHost, addAnswerButton);
        root.appendChild(body);
        questionsHost.appendChild(root);

        const initialAnswers = questionData?.answers?.length ? questionData.answers : [null, null];
        initialAnswers.forEach((answerData) => addAnswerEntry(question, answerData));

        questionEntries.push(question);
    }

    (quiz?.questions ?? []).forEach((questionData) => addQuestionEntry(questionData));
    if (!quiz) {
        addQuestionEntry(null);
    }

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
        title: t(quiz ? 'builder.editQuiz' : 'builder.addQuiz'),
        content: container,
        footer,
        size: 'lg'
    });

    cancelButton.addEventListener('click', () => dialog.close());

    saveButton.addEventListener('click', async () => {
        const questionsValid = questionEntries.length > 0 && questionEntries.every((question) =>
            question.tabs.validate() &&
            question.answers.length >= 2 &&
            question.answers.every((answer) => answer.tabs.validate()));

        if (!quizTabs.validate() || !questionsValid) {
            toast.warning(t('builder.atLeastOneLanguage'));
            return;
        }

        loader.button(saveButton, true);

        const payload = {
            translations: quizTabs.getValues(),
            passingScore: Number($('quiz-passing-score').value) || 0,
            timeLimitMinutes: $('quiz-time-limit').value ? Number($('quiz-time-limit').value) : null,
            questions: questionEntries.map((question) => ({
                translations: question.tabs.getValues(),
                questionType: question.typeSelect.value,
                points: Number(question.pointsInput.value) || 1,
                answers: question.answers.map((answer) => ({
                    translations: answer.tabs.getValues(),
                    isCorrect: answer.correctInput.checked
                }))
            }))
        };

        try {
            if (quiz) {
                await api.put(`/quizzes/${quiz.id}`, payload);
                toast.success(t('builder.quizUpdated'));
            } else {
                await api.post('/quizzes', { ...payload, lessonId: lesson.id });
                toast.success(t('builder.quizCreated'));
            }

            dialog.close();
            await load();
        } catch (error) {
            toast.fromApiError(error);
            loader.button(saveButton, false);
        }
    });
}

async function deleteQuiz(quiz, dialog) {
    const confirmed = await modal.confirm({
        title: t('builder.deleteQuizTitle'),
        message: t('builder.deleteQuizText'),
        confirmText: t('actions.delete'),
        danger: true
    });

    if (!confirmed) {
        return;
    }

    try {
        await api.delete(`/quizzes/${quiz.id}`);
        toast.success(t('builder.quizDeleted'));
        dialog.close();
        await load();
    } catch (error) {
        toast.fromApiError(error);
    }
}

// ---------------------------------------------------------------------------
// Материалы урока (PDF): у ресурса есть язык, поэтому список свой на каждой
// из вкладок AZ/EN/RU — как и с квизом, отдельный экземпляр состояния
// на каждое открытие формы, ничего общего между уроками не хранится.
// ---------------------------------------------------------------------------

const MATERIAL_LANGUAGES = ['AZ', 'EN', 'RU'];

async function openMaterialsForm(lesson) {
    const container = document.createElement('div');

    const tabBar = document.createElement('div');
    tabBar.className = 'tabs';
    tabBar.setAttribute('role', 'tablist');

    const listHost = document.createElement('div');
    listHost.className = 'mt-3';

    const uploadButton = document.createElement('button');
    uploadButton.type = 'button';
    uploadButton.className = 'btn btn-secondary btn-sm mt-3';
    uploadButton.textContent = `📎 ${t('builder.uploadMaterial')}`;

    container.append(tabBar, listHost, uploadButton);

    const tabButtons = {};
    const initialLanguage = getApiLanguage();
    let activeLanguage = MATERIAL_LANGUAGES.includes(initialLanguage) ? initialLanguage : 'AZ';
    let resourcesByLanguage = { AZ: [], EN: [], RU: [] };

    MATERIAL_LANGUAGES.forEach((language) => {
        const tab = document.createElement('button');
        tab.type = 'button';
        tab.className = 'tab';
        tab.setAttribute('role', 'tab');
        tab.textContent = language;
        tab.addEventListener('click', () => {
            activeLanguage = language;
            renderTabs();
            renderList();
        });

        tabButtons[language] = tab;
        tabBar.appendChild(tab);
    });

    function renderTabs() {
        MATERIAL_LANGUAGES.forEach((language) => {
            tabButtons[language].classList.toggle('is-active', language === activeLanguage);
        });
    }

    function renderList() {
        listHost.innerHTML = '';
        const items = resourcesByLanguage[activeLanguage] ?? [];

        if (!items.length) {
            const empty = document.createElement('p');
            empty.className = 'text-sm text-muted';
            empty.textContent = t('builder.noMaterials');
            listHost.appendChild(empty);
            return;
        }

        items.forEach((resource) => {
            const row = document.createElement('div');
            row.className = 'flex gap-3 items-center justify-between mb-2';

            const link = document.createElement('a');
            link.href = resource.fileUrl;
            link.target = '_blank';
            link.rel = 'noopener';
            link.textContent = `📄 ${resource.fileName}`;

            row.append(link, iconButton(t('actions.delete'), '🗑', () => deleteMaterial(resource)));
            listHost.appendChild(row);
        });
    }

    async function loadResources() {
        loader.block(listHost);

        try {
            // Ресурс привязан к конкретному переводу урока, поэтому запрашиваем
            // отдельно на каждом языке — общего эндпоинта "все материалы сразу" нет.
            const responses = await Promise.all(
                MATERIAL_LANGUAGES.map((language) => api.get(`/lessons/${lesson.id}`, { query: { lang: language } })
                    .catch(() => null)) // у урока может не быть перевода на каком-то языке
            );

            MATERIAL_LANGUAGES.forEach((language, index) => {
                resourcesByLanguage[language] = responses[index]?.resources ?? [];
            });
        } catch (error) {
            toast.fromApiError(error);
        }

        renderTabs();
        renderList();
    }

    async function deleteMaterial(resource) {
        const confirmed = await modal.confirm({
            title: t('builder.deleteMaterialTitle'),
            message: t('builder.deleteMaterialText'),
            confirmText: t('actions.delete'),
            danger: true
        });

        if (!confirmed) {
            return;
        }

        try {
            await api.delete(`/lessons/${lesson.id}/resources/${resource.id}`);
            toast.success(t('builder.materialDeleted'));
            await loadResources();
        } catch (error) {
            toast.fromApiError(error);
        }
    }

    uploadButton.addEventListener('click', () => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'application/pdf';
        input.style.display = 'none';

        input.addEventListener('change', async () => {
            const file = input.files?.[0];
            if (!file) {
                return;
            }

            const formData = new FormData();
            formData.append('languageCode', activeLanguage);
            formData.append('file', file);

            loader.button(uploadButton, true);
            try {
                await api.upload(`/lessons/${lesson.id}/resources`, formData);
                toast.success(t('builder.materialUploaded'));
                await loadResources();
            } catch (error) {
                toast.fromApiError(error);
            } finally {
                loader.button(uploadButton, false);
            }
        });

        document.body.appendChild(input);
        input.click();
        input.remove();
    });

    const closeButton = document.createElement('button');
    closeButton.type = 'button';
    closeButton.className = 'btn btn-secondary';
    closeButton.textContent = t('actions.close');

    const dialog = modal.open({
        title: `${t('builder.materialsFor')}: ${pickText(lesson.translations, 'title')}`,
        content: container,
        footer: closeButton
    });

    closeButton.addEventListener('click', () => dialog.close());

    await loadResources();
}

// ---------------------------------------------------------------------------
// Общее
// ---------------------------------------------------------------------------

/**
 * Модальная форма с кнопками «Отмена» и «Сохранить».
 * Одна на четыре сценария (создание/редактирование модуля и урока) —
 * иначе четыре почти одинаковых блока с обработчиками.
 */
function openFormModal({ title, content, size, validate, submit }) {
    const footer = document.createDocumentFragment();

    const cancel = document.createElement('button');
    cancel.type = 'button';
    cancel.className = 'btn btn-secondary';
    cancel.textContent = t('actions.cancel');

    const save = document.createElement('button');
    save.type = 'button';
    save.className = 'btn btn-primary';
    save.textContent = t('actions.save');

    footer.append(cancel, save);

    const dialog = modal.open({ title, content, footer, size });

    cancel.addEventListener('click', () => dialog.close());

    save.addEventListener('click', async () => {
        if (validate && !validate()) {
            toast.warning(t('builder.atLeastOneLanguage'));
            return;
        }

        loader.button(save, true);

        try {
            await submit();
            dialog.close();
            await load();
        } catch (error) {
            // Модалку не закрываем: пользователь не должен терять введённое
            // из-за ошибки сервера.
            toast.fromApiError(error);
            loader.button(save, false);
        }
    });
}

/**
 * Выбирает текст на языке интерфейса из массива переводов.
 *
 * Это ПРЕЗЕНТАЦИЯ, а не дублирование серверной логики: конструктор уже держит
 * все переводы в памяти (без них нельзя редактировать), и выбрать из них один
 * для показа в списке — задача экрана. Порядок отката тот же, что на сервере:
 * язык интерфейса → AZ → EN → первый существующий.
 */
function pickText(translations, field) {
    const list = translations ?? [];

    if (list.length === 0) {
        return '—';
    }

    const current = getApiLanguage();
    const match = list.find((item) => (item.languageCode ?? '').toUpperCase() === current)
        ?? list.find((item) => (item.languageCode ?? '').toUpperCase() === 'AZ')
        ?? list.find((item) => (item.languageCode ?? '').toUpperCase() === 'EN')
        ?? list[0];

    return match?.[field] || '—';
}

function iconButton(label, glyph, onClick) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'btn btn-ghost btn-sm';
    button.setAttribute('aria-label', label);
    button.title = label;
    button.textContent = glyph;
    button.addEventListener('click', onClick);
    return button;
}

function buildInput(id, label, value, type = 'text') {
    const wrapper = document.createElement('div');
    wrapper.className = 'field';

    const labelElement = document.createElement('label');
    labelElement.className = 'field-label';
    labelElement.htmlFor = id;
    labelElement.textContent = label;

    const input = document.createElement('input');
    input.className = 'input';
    input.id = id;
    input.type = type;
    input.value = value;

    if (type === 'number') {
        input.min = '0';
    }

    wrapper.append(labelElement, input);
    return wrapper;
}

/**
 * Массив переводов с сервера → объект { AZ: {...}, EN: {...} } для формы.
 *
 * Благодаря тому, что модули и уроки грузятся без ?lang=, форма открывается
 * со ВСЕМИ существующими языками. Это критично: сервер при сохранении
 * заменяет переводы целиком, и незаполненная вкладка означала бы удаление.
 */
function toInitialValues(translations, fields) {
    const result = {};

    (translations ?? []).forEach((translation) => {
        const language = (translation.languageCode ?? '').toUpperCase();
        result[language] = Object.fromEntries(
            fields.map((field) => [field, translation[field] ?? ''])
        );
    });

    return result;
}

startInstructorPage(load);
