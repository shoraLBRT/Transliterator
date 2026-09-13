// Обёртка над localStorage для LocalStorageStore. Только то, чего нельзя вызвать
// из .NET напрямую: перечень ключей и запись, которая сообщает, почему не удалась.
window.transliteratorStorage = {
    keys: () => Object.keys(window.localStorage),

    // null — записано. "quota" — не хватило места (имя ошибки у браузеров разное).
    // Иначе — имя ошибки: например, SecurityError, когда хранение запрещено.
    set: (key, value) => {
        try {
            window.localStorage.setItem(key, value);
            return null;
        } catch (e) {
            const quota = e && (e.name === "QuotaExceededError"
                || e.name === "NS_ERROR_DOM_QUOTA_REACHED"
                || e.code === 22
                || e.code === 1014);
            return quota ? "quota" : String((e && e.name) || e);
        }
    }
};
