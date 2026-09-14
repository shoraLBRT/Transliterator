// Мелочи страницы, которых нет в Blazor: печать подсказки, высота поля по тексту
// и буфер обмена. Всё здесь — удобство: без этого файла страница остаётся рабочей.
window.page = (() => {
    const reducedMotion = () => window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    const TypeDelay = 45;
    const EraseDelay = 22;
    const HoldDelay = 3500; // успеть прочитать фразу
    const GapDelay = 450;

    /** Фразы печатаются и стираются по кругу; без анимации стоит первая. */
    function typewriter(element, phrases) {
        if (!element || !phrases || phrases.length === 0)
            return;

        if (reducedMotion() || phrases.length === 1) {
            element.textContent = phrases[0];
            return;
        }

        let phrase = 0;
        let length = 0;
        let erasing = false;

        element.classList.add("is-typing");

        const tick = () => {
            const text = phrases[phrase];

            if (!erasing) {
                element.textContent = text.slice(0, ++length);

                if (length === text.length) {
                    erasing = true;
                    setTimeout(tick, HoldDelay);
                } else {
                    setTimeout(tick, TypeDelay);
                }
                return;
            }

            element.textContent = text.slice(0, --length);

            if (length === 0) {
                erasing = false;
                phrase = (phrase + 1) % phrases.length;
                setTimeout(tick, GapDelay);
            } else {
                setTimeout(tick, EraseDelay);
            }
        };

        tick();
    }

    /** Поле растёт по тексту до потолка из CSS, дальше прокручивается. */
    function resize(element) {
        if (!element)
            return;

        element.style.height = "auto";
        const limit = parseFloat(getComputedStyle(element).maxHeight) || Infinity;
        element.style.height = `${Math.min(element.scrollHeight, limit)}px`;
        element.style.overflowY = element.scrollHeight > limit ? "auto" : "hidden";
    }

    function autosize(element) {
        if (!element)
            return;

        element.addEventListener("input", () => resize(element));
        window.addEventListener("resize", () => resize(element));
        resize(element);
    }

    /**
     * Буфер обмена. Clipboard API бывает недоступен: старый браузер, страница
     * во фрейме, запрет разрешения. Тогда — прежний способ через выделение
     * скрытого поля; не сработал и он — страница попросит выделить текст руками.
     */
    async function copy(text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            return copyWithSelection(text);
        }
    }

    function copyWithSelection(text) {
        const field = document.createElement("textarea");
        field.value = text;
        field.setAttribute("readonly", "");
        field.style.position = "fixed";
        field.style.opacity = "0";
        document.body.appendChild(field);
        field.select();

        try {
            return document.execCommand("copy");
        } catch {
            return false;
        } finally {
            field.remove();
        }
    }

    return { typewriter, autosize, resize, copy };
})();
