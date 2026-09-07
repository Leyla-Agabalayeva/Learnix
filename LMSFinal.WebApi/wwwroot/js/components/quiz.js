/**
 * Прохождение теста на странице урока.
 *
 * Три состояния в одном компоненте: вступление (что за тест, прошлые попытки),
 * прохождение (вопросы и таймер) и результат. Разделять их на три модуля нет
 * смысла — они делят одни и те же данные и переключаются одной перерисовкой.
 *
 * Правило, которое здесь важнее остальных: балл считает сервер.
 * Флаг isCorrect у вариантов ответа приходит равным null (QuizService.MapQuiz
 * скрывает его от всех, кроме владельца курса), поэтому проверить ответы
 * на клиенте физически нечем — и это правильно: иначе правильные ответы
 * лежали бы прямо в ответе API, и «сдать» тест можно было бы через DevTools.
 */

import { api } from '../api.js';
import { t, getApiLanguage } from '../localization.js';
import { toast } from './toast.js';
import { loader } from './loader.js';
import { modal } from './modal.js';
import { plural } from '../format.js';

/**
 * Монтирует тест в контейнер.
 *
 * @param {Element} container
 * @param {string} quizId
 * @param {{ onPassed?: () => void, onAttemptEnd?: () => void }} [options]
 *        onPassed — тест сдан: страница обновляет прогресс и боковую панель.
 *        onAttemptEnd — попытка завершена любым способом (ответил или отменил).
 * @returns {{ isInProgress: () => boolean, isPassed: () => boolean,
 *             refresh: () => void, destroy: () => void }}
 */
export function mountQuiz(container, quizId, { onPassed, onAttemptEnd } = {}) {
    const state = {
        view: 'loading',
        quiz: null,
        attempts: [],
        // Выбранные ответы храним здесь, а не читаем из DOM при отправке:
        // при перерисовке (смена языка интерфейса, возврат из результата)
        // разметка создаётся заново, и отметки в ней бы потерялись.
        selections: new Map(),
        result: null,
        deadline: null
    };

    let timerId = null;
    let destroyed = false;

    load();

    return {
        isInProgress: () => state.view === 'running',

        // Сдан ли тест — по последней попытке или по любой из прошлых.
        // Страница урока спрашивает об этом перед автопереходом, и спрашивать
        // должна компонент, а не разметку: пока тест грузится, разметки ещё нет.
        isPassed: () => state.attempts.some((attempt) => attempt.passed),

        refresh: () => render(),
        destroy() {
            destroyed = true;
            stopTimer();
            container.innerHTML = '';
        }
    };

    // -----------------------------------------------------------------------
    // Данные
    // -----------------------------------------------------------------------

    async function load() {
        render();

        try {
            // Тест и прошлые попытки — независимые запросы, ждать их по очереди
            // незачем.
            const [quiz, attempts] = await Promise.all([
                api.get(`/quizzes/${quizId}`, { query: { lang: getApiLanguage() } }),
                api.get(`/quizzes/${quizId}/results`)
            ]);

            if (destroyed) {
                return;
            }

            state.quiz = quiz;
            state.attempts = Array.isArray(attempts) ? attempts : [];
            state.view = 'intro';
        } catch (error) {
            if (destroyed) {
                return;
            }

            state.view = 'error';
            state.error = error;
        }

        render();
    }

    async function submit() {
        const answers = state.quiz.questions.map((question) => ({
            questionId: question.id,
            selectedAnswerIds: [...(state.selections.get(question.id) ?? [])]
        }));

        const button = container.querySelector('[data-quiz-submit]');
        loader.button(button, true);

        try {
            const result = await api.post(`/quizzes/${quizId}/submit`, { answers });

            stopTimer();
            state.result = result;
            state.attempts = [result, ...state.attempts];
            state.view = 'result';
            render();
            onAttemptEnd?.();

            if (result.passed) {
                onPassed?.();
            }
        } catch (error) {
            loader.button(button, false);
            toast.fromApiError(error);
        }
    }

    // -----------------------------------------------------------------------
    // Отрисовка
    // -----------------------------------------------------------------------

    function render() {
        if (destroyed) {
            return;
        }

        container.innerHTML = '';

        const views = {
            loading: renderLoading,
            error: renderError,
            intro: renderIntro,
            running: renderRunning,
            result: renderResult
        };

        container.appendChild(views[state.view]());
    }

    function renderLoading() {
        const box = document.createElement('div');
        box.className = 'quiz-card';
        loader.block(box, t('states.loading'));
        return box;
    }

    function renderError() {
        const box = document.createElement('div');
        box.className = 'quiz-card quiz-card-error';

        const text = document.createElement('p');
        text.textContent = state.error?.isNetworkError ? t('states.networkError') : state.error?.message;

        const retry = document.createElement('button');
        retry.type = 'button';
        retry.className = 'btn btn-secondary mt-2';
        retry.textContent = t('actions.retry');
        retry.addEventListener('click', () => {
            state.view = 'loading';
            load();
        });

        box.append(text, retry);
        return box;
    }

    function renderIntro() {
        const box = document.createElement('div');
        box.className = 'quiz-card';

        box.appendChild(buildHeader());

        if (state.quiz.description) {
            const description = document.createElement('p');
            description.className = 'quiz-description';
            description.textContent = state.quiz.description;
            box.appendChild(description);
        }

        box.appendChild(buildFacts());

        const best = bestAttempt();

        if (best) {
            box.appendChild(buildPreviousResult(best));
        }

        const start = document.createElement('button');
        start.type = 'button';
        start.className = 'btn btn-primary';
        // Кнопка называется по-разному не ради красоты: «Начать» на тесте,
        // который уже сдан, выглядит так, будто прошлый результат сотрётся.
        start.textContent = state.attempts.length ? t('lesson.quizRetake') : t('lesson.quizStart');
        start.addEventListener('click', beginAttempt);

        box.appendChild(start);
        return box;
    }

    function renderRunning() {
        const box = document.createElement('div');
        box.className = 'quiz-card';

        box.appendChild(buildHeader());

        if (state.deadline) {
            box.appendChild(buildTimer());
        }

        const form = document.createElement('form');
        form.className = 'quiz-questions';
        form.noValidate = true;

        state.quiz.questions.forEach((question, index) => {
            form.appendChild(buildQuestion(question, index));
        });

        const actions = document.createElement('div');
        actions.className = 'quiz-actions';

        const submitButton = document.createElement('button');
        submitButton.type = 'submit';
        submitButton.className = 'btn btn-primary';
        submitButton.dataset.quizSubmit = 'true';
        submitButton.textContent = t('lesson.quizSubmit');

        const cancel = document.createElement('button');
        cancel.type = 'button';
        cancel.className = 'btn btn-ghost';
        cancel.textContent = t('actions.cancel');
        cancel.addEventListener('click', cancelAttempt);

        actions.append(submitButton, cancel);
        form.appendChild(actions);

        form.addEventListener('submit', async (event) => {
            event.preventDefault();

            // Отправлять тест с пропусками можно — это осознанное решение:
            // пропущенный вопрос просто не даёт баллов, и запрещать отправку
            // значило бы держать студента на странице против его воли.
            // Но предупредить стоит: чаще пропуск — это невнимательность.
            const unanswered = countUnanswered();

            if (unanswered > 0) {
                const confirmed = await modal.confirm({
                    title: t('lesson.quizUnansweredTitle'),
                    message: t('lesson.quizUnansweredText', { questions: plural(unanswered, 'units.question') }),
                    confirmText: t('lesson.quizSubmit')
                });

                if (!confirmed) {
                    return;
                }
            }

            await submit();
        });

        box.appendChild(form);
        return box;
    }

    function renderResult() {
        const box = document.createElement('div');
        box.className = `quiz-card quiz-result ${state.result.passed ? 'is-passed' : 'is-failed'}`;

        const icon = document.createElement('p');
        icon.className = 'quiz-result-icon';
        icon.textContent = state.result.passed ? '🎉' : '📚';
        icon.setAttribute('aria-hidden', 'true');

        const verdict = document.createElement('h3');
        verdict.className = 'quiz-result-verdict';
        verdict.textContent = state.result.passed ? t('lesson.quizPassed') : t('lesson.quizFailed');

        const score = document.createElement('p');
        score.className = 'quiz-result-score';
        score.textContent = `${formatPercentage(state.result.percentage)}%`;

        // Результат объявляется вслух: студент, работающий со скринридером,
        // иначе не узнает, что после нажатия «Ответить» что-то изменилось.
        box.setAttribute('role', 'status');
        box.setAttribute('aria-live', 'polite');

        const details = document.createElement('dl');
        details.className = 'quiz-result-details';

        addDetail(details, t('lesson.quizCorrect'), `${state.result.correctAnswers} / ${state.result.totalQuestions}`);
        addDetail(details, t('lesson.quizPoints'), String(state.result.score));
        addDetail(details, t('lesson.quizPassingScore'), `${state.quiz.passingScore}%`);

        box.append(icon, verdict, score, details);

        // Разбора ошибок здесь нет намеренно: сервер возвращает только балл,
        // а какие именно вопросы провалены — не сообщает. Показывать это
        // означало бы считать правильность на клиенте, то есть отдать
        // правильные ответы браузеру. Вместо разбора — повторная попытка.
        const hint = document.createElement('p');
        hint.className = 'quiz-result-hint';
        hint.textContent = state.result.passed ? t('lesson.quizPassedHint') : t('lesson.quizFailedHint');

        const again = document.createElement('button');
        again.type = 'button';
        again.className = state.result.passed ? 'btn btn-secondary' : 'btn btn-primary';
        again.textContent = t('lesson.quizRetake');
        again.addEventListener('click', beginAttempt);

        box.append(hint, again);
        return box;
    }

    // -----------------------------------------------------------------------
    // Кусочки разметки
    // -----------------------------------------------------------------------

    function buildHeader() {
        const header = document.createElement('div');
        header.className = 'quiz-header';

        const badge = document.createElement('span');
        badge.className = 'badge badge-primary';
        badge.textContent = t('lesson.quizLabel');

        const title = document.createElement('h2');
        title.className = 'quiz-title';
        title.textContent = state.quiz.title;

        header.append(badge, title);
        return header;
    }

    function buildFacts() {
        const list = document.createElement('ul');
        list.className = 'quiz-facts';

        addFact(list, '❓', plural(state.quiz.questions.length, 'units.question'));
        addFact(list, '🎯', t('lesson.quizPassingScoreShort', { score: state.quiz.passingScore }));

        if (state.quiz.timeLimitMinutes) {
            addFact(list, '⏱️', t('lesson.quizTimeLimit', { minutes: state.quiz.timeLimitMinutes }));
        }

        return list;
    }

    function buildPreviousResult(best) {
        const box = document.createElement('p');
        box.className = `quiz-previous ${best.passed ? 'is-passed' : 'is-failed'}`;

        box.textContent = t('lesson.quizBestResult', {
            percentage: formatPercentage(best.percentage),
            verdict: best.passed ? t('student.passed') : t('student.failed')
        });

        return box;
    }

    function buildQuestion(question, index) {
        const fieldset = document.createElement('fieldset');
        fieldset.className = 'quiz-question';

        const legend = document.createElement('legend');
        legend.className = 'quiz-question-text';
        legend.textContent = `${index + 1}. ${question.questionText}`;

        fieldset.appendChild(legend);

        const isMultiple = question.questionType === 'MultipleChoice';

        if (isMultiple) {
            const hint = document.createElement('p');
            hint.className = 'quiz-question-hint';
            hint.textContent = t('lesson.quizMultipleHint');
            fieldset.appendChild(hint);
        }

        const selected = state.selections.get(question.id) ?? new Set();

        question.answers.forEach((answer) => {
            const option = document.createElement('label');
            option.className = 'quiz-option';

            const input = document.createElement('input');
            input.type = isMultiple ? 'checkbox' : 'radio';
            input.name = `q-${question.id}`;
            input.value = answer.id;
            input.checked = selected.has(answer.id);

            input.addEventListener('change', () => {
                const current = state.selections.get(question.id) ?? new Set();

                if (isMultiple) {
                    input.checked ? current.add(answer.id) : current.delete(answer.id);
                } else {
                    // Один вариант: переключатель уже снял отметку с соседей
                    // в разметке, но в состоянии её надо заменить целиком.
                    current.clear();
                    current.add(answer.id);
                }

                state.selections.set(question.id, current);
                option.closest('.quiz-question')?.classList.remove('is-unanswered');
            });

            const text = document.createElement('span');
            text.textContent = answer.answerText;

            option.append(input, text);
            fieldset.appendChild(option);
        });

        return fieldset;
    }

    function buildTimer() {
        const box = document.createElement('p');
        box.className = 'quiz-timer';
        box.dataset.quizTimer = 'true';

        // aria-live здесь НЕ ставим: секундная стрелка, объявляемая вслух
        // каждую секунду, делает страницу неслушаемой. Об истечении времени
        // студент узнаёт из тоста и из появившегося результата.
        box.setAttribute('role', 'timer');

        updateTimerText(box);
        return box;
    }

    // -----------------------------------------------------------------------
    // Попытка
    // -----------------------------------------------------------------------

    function beginAttempt() {
        state.selections = new Map();
        state.result = null;
        state.view = 'running';

        state.deadline = state.quiz.timeLimitMinutes
            ? Date.now() + state.quiz.timeLimitMinutes * 60_000
            : null;

        render();
        startTimer();

        // Тест длиннее экрана: без прокрутки студент не увидит, что он начался.
        container.scrollIntoView({ behavior: prefersReducedMotion() ? 'auto' : 'smooth', block: 'start' });
    }

    async function cancelAttempt() {
        const confirmed = await modal.confirm({
            title: t('lesson.quizCancelTitle'),
            message: t('lesson.quizCancelText'),
            confirmText: t('lesson.quizCancelConfirm'),
            danger: true
        });

        if (!confirmed) {
            return;
        }

        stopTimer();
        state.view = 'intro';
        render();
        onAttemptEnd?.();
    }

    function countUnanswered() {
        let count = 0;

        state.quiz.questions.forEach((question) => {
            const selected = state.selections.get(question.id);

            if (!selected || selected.size === 0) {
                count += 1;
                container.querySelector(`[name="q-${question.id}"]`)
                    ?.closest('.quiz-question')
                    ?.classList.add('is-unanswered');
            }
        });

        return count;
    }

    function startTimer() {
        stopTimer();

        if (!state.deadline) {
            return;
        }

        timerId = window.setInterval(() => {
            const box = container.querySelector('[data-quiz-timer]');

            if (!box) {
                stopTimer();   // страницу перерисовали — интервал больше некому обновлять
                return;
            }

            if (updateTimerText(box) <= 0) {
                stopTimer();
                toast.warning(t('lesson.quizTimeUp'));

                // Время вышло — отправляем то, что успели отметить. Молча
                // выбросить ответы было бы хуже: они всё равно дают баллы.
                submit();
            }
        }, 1000);
    }

    function stopTimer() {
        if (timerId !== null) {
            window.clearInterval(timerId);
            timerId = null;
        }
    }

    /** @returns {number} сколько секунд осталось */
    function updateTimerText(box) {
        const left = Math.max(0, Math.round((state.deadline - Date.now()) / 1000));
        const minutes = Math.floor(left / 60);
        const seconds = left % 60;

        box.textContent = `⏱️ ${minutes}:${String(seconds).padStart(2, '0')}`;
        box.classList.toggle('is-urgent', left <= 30);

        return left;
    }

    // -----------------------------------------------------------------------
    // Мелочи
    // -----------------------------------------------------------------------

    function bestAttempt() {
        return state.attempts.reduce(
            (best, attempt) => (best === null || attempt.percentage > best.percentage ? attempt : best),
            null);
    }
}

function addFact(list, icon, text) {
    const item = document.createElement('li');

    const iconElement = document.createElement('span');
    iconElement.setAttribute('aria-hidden', 'true');
    iconElement.textContent = icon;

    item.append(iconElement, document.createTextNode(` ${text}`));
    list.appendChild(item);
}

function addDetail(list, label, value) {
    const term = document.createElement('dt');
    term.textContent = label;

    const definition = document.createElement('dd');
    definition.textContent = value;

    list.append(term, definition);
}

/** 66.67 → «66.67», 80 → «80»: хвост «.00» в проценте только мешает. */
function formatPercentage(value) {
    return Number.isInteger(value) ? String(value) : String(Math.round(value * 100) / 100);
}

function prefersReducedMotion() {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}
