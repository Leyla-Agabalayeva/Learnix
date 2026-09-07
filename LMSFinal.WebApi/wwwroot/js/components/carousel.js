/**
 * Карусель баннеров на главной (вдохновение — верхний слайдер Udemy).
 *
 * Слайды — только изображения, без текста поверх: у Learnix нет ни скидок,
 * ни срочных предложений, которые обычно пишут на таких баннерах, а рисовать
 * пустой текст ради сходства с Udemy было бы неуместно. Каждый слайд может
 * вести по ссылке (LinkUrl), которую задаёт администратор при загрузке.
 */

const AUTOPLAY_MS = 6000;

/**
 * @param {HTMLElement} container
 * @param {Array<{ id: string, imageUrl: string, linkUrl?: string|null }>} slides
 */
export function renderCarousel(container, slides) {
    if (!container) {
        return;
    }

    container.innerHTML = '';

    // Один слайд или ноль — карусель (стрелки, точки) не нужна, это просто баннер.
    if (!slides.length) {
        container.classList.add('hidden');
        return;
    }

    container.classList.remove('hidden');

    const track = document.createElement('div');
    track.className = 'carousel-track';
    slides.forEach((slide) => track.appendChild(buildSlide(slide)));
    container.appendChild(track);

    if (slides.length === 1) {
        return;
    }

    let active = 0;
    let timer = null;

    const dotsWrap = document.createElement('div');
    dotsWrap.className = 'carousel-dots';
    const dots = slides.map((_, index) => buildDot(index, () => goTo(index)));
    dots.forEach((dot) => dotsWrap.appendChild(dot));

    const prev = buildArrow('‹', () => goTo(active - 1));
    prev.classList.add('carousel-arrow-prev');
    const next = buildArrow('›', () => goTo(active + 1));
    next.classList.add('carousel-arrow-next');

    container.append(prev, next, dotsWrap);

    function goTo(index) {
        active = (index + slides.length) % slides.length;
        track.style.transform = `translateX(-${active * 100}%)`;
        dots.forEach((dot, i) => dot.classList.toggle('is-active', i === active));
        resetAutoplay();
    }

    function resetAutoplay() {
        clearInterval(timer);
        timer = setInterval(() => goTo(active + 1), AUTOPLAY_MS);
    }

    dots[0].classList.add('is-active');
    resetAutoplay();

    container.addEventListener('mouseenter', () => clearInterval(timer));
    container.addEventListener('mouseleave', resetAutoplay);
}

function buildSlide(slide) {
    const wrapper = document.createElement(slide.linkUrl ? 'a' : 'div');
    wrapper.className = 'carousel-slide';

    if (slide.linkUrl) {
        wrapper.href = slide.linkUrl;
    }

    const image = document.createElement('img');
    image.src = slide.imageUrl;
    image.alt = '';
    image.loading = 'lazy';
    wrapper.appendChild(image);

    return wrapper;
}

function buildArrow(symbol, onClick) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'carousel-arrow';
    button.setAttribute('aria-label', symbol === '‹' ? 'Previous slide' : 'Next slide');
    button.textContent = symbol;
    button.addEventListener('click', onClick);
    return button;
}

function buildDot(index, onClick) {
    const dot = document.createElement('button');
    dot.type = 'button';
    dot.className = 'carousel-dot';
    dot.setAttribute('aria-label', `Slide ${index + 1}`);
    dot.addEventListener('click', onClick);
    return dot;
}
