// Выгрузка текста файлом: из .NET скачивание не начать, а ссылке на Blob
// браузер отдаёт файл с тем именем, которое ей задано.
window.transliteratorFiles = {
    download: (fileName, text) => {
        const url = URL.createObjectURL(new Blob([text], { type: "application/json" }));
        const link = document.createElement("a");

        link.href = url;
        link.download = fileName;
        link.style.display = "none";

        document.body.appendChild(link);
        link.click();
        link.remove();

        // Сразу отзывать нельзя: часть браузеров начинает скачивание после click().
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
};
