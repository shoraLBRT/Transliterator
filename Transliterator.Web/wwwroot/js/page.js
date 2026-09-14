// Мелочи страницы, которых нет в Blazor: печать подсказки, высота поля по тексту
// и буфер обмена. Всё здесь — удобство: без этого файла страница остаётся рабочей.
window.page = (() => {
    const TypeDelay = 45;
    const EraseDelay = 22;
    const HoldDelay = 3500; // успеть прочитать фразу
    const GapDelay = 450;
    const PausedDelay = 500;

    /**
     * Фразы печатаются и стираются по кругу.
     *
     * prefers-reduced-motion здесь не спрашивается. Печать меняет текст на месте
     * и ничего не двигает по экрану, а флаг включается далеко не только
     * по просьбе человека: в Windows его ставит выключенный параметр «Показывать
     * анимацию», и из-за этой проверки подсказка у владельца не менялась вовсе.
     *
     * Пока в поле есть текст, подсказка замирает на дописанной фразе: человек
     * уже работает, и меняющаяся строка над полем только отвлекала бы.
     */
    function typewriter(element, phrases, input) {
        if (!element || !phrases || phrases.length === 0)
            return;

        if (phrases.length === 1) {
            element.textContent = phrases[0];
            return;
        }

        const busy = () => !!input && input.value.trim() !== "";

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
                    setTimeout(hold, HoldDelay);
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

        // Дописанная фраза стоит, пока поле занято; стирается, когда оно опустело.
        const hold = () => setTimeout(busy() ? hold : tick, busy() ? PausedDelay : 0);

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
