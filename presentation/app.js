function initSlides() {
  const slides = Array.from(document.querySelectorAll(".slide"));
  if (slides.length === 0) return;

  const titleEl = document.getElementById("slideTitle");
  const indexEl = document.getElementById("slideIndex");
  const prevBtn = document.getElementById("prevBtn");
  const nextBtn = document.getElementById("nextBtn");
  const timerEl = document.getElementById("timerValue");

  let elapsedSeconds = 0;
  let timerHandle = null;

  let current = 0;
  const max = slides.length - 1;

  function readHashIndex() {
    const match = window.location.hash.match(/^#slide-(\d+)$/i);
    if (!match) return 0;
    const n = Number(match[1]) - 1;
    if (Number.isNaN(n)) return 0;
    return Math.max(0, Math.min(max, n));
  }

  function writeHashIndex(idx) {
    const target = `#slide-${idx + 1}`;
    if (window.location.hash !== target) {
      history.replaceState(null, "", target);
    }
  }

  function render() {
    slides.forEach((slide, i) => {
      slide.classList.toggle("active", i === current);
    });

    const active = slides[current];
    const title = active.getAttribute("data-title") || `Slide ${current + 1}`;
    if (titleEl) titleEl.textContent = title;
    if (indexEl) indexEl.textContent = `${current + 1} / ${slides.length}`;

    if (prevBtn) prevBtn.disabled = current <= 0;
    if (nextBtn) nextBtn.disabled = current >= max;

    writeHashIndex(current);
  }

  function goTo(idx) {
    const next = Math.max(0, Math.min(max, idx));
    if (next === current) return;
    current = next;
    render();
  }

  function nextSlide() {
    goTo(current + 1);
  }

  function prevSlide() {
    goTo(current - 1);
  }

  document.addEventListener("keydown", (event) => {
    if (event.defaultPrevented) return;

    switch (event.key) {
      case "ArrowRight":
      case "PageDown":
      case " ":
        event.preventDefault();
        nextSlide();
        break;
      case "ArrowLeft":
      case "PageUp":
        event.preventDefault();
        prevSlide();
        break;
      case "Home":
        event.preventDefault();
        goTo(0);
        break;
      case "End":
        event.preventDefault();
        goTo(max);
        break;
      default:
        break;
    }
  });

  if (prevBtn) prevBtn.addEventListener("click", prevSlide);
  if (nextBtn) nextBtn.addEventListener("click", nextSlide);

  window.addEventListener("hashchange", () => {
    goTo(readHashIndex());
  });

  current = readHashIndex();
  render();
}

document.addEventListener("DOMContentLoaded", () => {
  initSlides();

  const timerEl = document.getElementById("timerValue");
  if (!timerEl) return;

  let seconds = 0;
  function tick() {
    seconds += 1;
    const mins = String(Math.floor(seconds / 60)).padStart(2, "0");
    const secs = String(seconds % 60).padStart(2, "0");
    timerEl.textContent = `${mins}:${secs}`;
  }

  setInterval(tick, 1000);
});
