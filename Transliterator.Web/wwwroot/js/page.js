// Мелочи страницы, которых нет в Blazor: печать подсказки, высота поля по тексту
// и буфер обмена. Всё здесь — удобство: без этого файла страница остаётся рабочей.
window.page = (() => {
    const TypeDelay = 58;   // около 17 букв в секунду: быстрее читается как мельтешение
    const EraseDelay = 28;
    const HoldDelay = 6000; // успеть прочитать фразу, в том числе арабский пример
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

    /**
     * Проверка «не робот» перед отправкой отзыва (H3) — Cloudflare Turnstile.
     * Скрипт чужой, и грузится он только при открытии панели обратной связи:
     * кто панель не открывает, тот к Cloudflare и не обращается. В режиме
     * interaction-only виджет невидим, пока проверке хватает фона браузера.
     */
    const TurnstileScript = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";
    const TokenWait = 250;
    const TokenAttempts = 40; // десять секунд: проверка обычно укладывается в одну-две

    let turnstileLoading = null;

    function loadTurnstile() {
        if (window.turnstile)
            return Promise.resolve(window.turnstile);

        turnstileLoading ??= new Promise((resolve, reject) => {
            const script = document.createElement("script");
            script.src = TurnstileScript;
            script.async = true;
            script.onload = () => resolve(window.turnstile);
            script.onerror = () => {
                // Блокировщик рекламы или нет сети: следующая попытка грузит заново.
                turnstileLoading = null;
                script.remove();
                reject(new Error("Turnstile did not load"));
            };
            document.head.appendChild(script);
        });

        return turnstileLoading;
    }

    /** Ставит виджет в элемент и возвращает его номер. */
    async function guard(element, siteKey) {
        const turnstile = await loadTurnstile();
        return turnstile.render(element, {
            sitekey: siteKey,
            language: "ru",
            theme: "light",
            appearance: "interaction-only",
        });
    }

    /**
     * Токен проверки. Человек может нажать «Отправить» раньше, чем проверка
     * закончилась, — тогда токен дожидается, а не приходит пустым.
     */
    async function guardToken(id) {
        for (let attempt = 0; attempt < TokenAttempts; attempt++) {
            const token = window.turnstile?.getResponse(id);
            if (token)
                return token;

            await new Promise(resolve => setTimeout(resolve, TokenWait));
        }

        return "";
    }

    /** Токен одноразовый: после каждой отправки проверка начинается заново. */
    function guardReset(id) {
        window.turnstile?.reset(id);
    }

    function guardRemove(id) {
        try {
            window.turnstile?.remove(id);
        } catch {
            // Элемент уже убран со страницы — убирать нечего.
        }
    }

    return { typewriter, autosize, resize, copy, guard, guardToken, guardReset, guardRemove };
})();
