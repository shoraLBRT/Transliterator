# Функция обратной связи

Маленькая функция на Cloudflare Workers (H3, issue #99). Кнопка обратной связи
на странице присылает ей сообщение, она проверяет «не робот» (Cloudflare
Turnstile) и заводит issue в этом репозитории от имени GitHub App. Аккаунт GitHub
человеку не нужен, а ключ GitHub живёт только в секретах функции — на страницу
он не попадает.

Пока функция не развёрнута и её адрес не прописан в настройках страницы, кнопка
работает по-старому: открывает заполненную форму issue на GitHub (H1).

```
страница ──POST {title, body, token}──▶ функция ──siteverify──▶ Turnstile
                                           └──JWT → токен установки → POST /issues──▶ GitHub
```

Что функция проверяет, прежде чем завести issue:

- **Origin** — запрос пришёл с выложенной страницы или с локального запуска
  (`ALLOWED_ORIGINS` в `wrangler.toml`). Чужому сайту она не отвечает;
- **лимит** — не больше пяти сообщений в минуту с одного адреса;
- **длину** — заголовок до 120 знаков, текст до 12 000 (те же числа в ядре,
  `FeedbackMessage`, их сверяет тест);
- **Turnstile** — токен проверки подтверждён Cloudflare;
- **упоминания** `@кто-то` обезврежены: issue не рассылает уведомлений.

Зависимостей нет: обычный JavaScript и Web Crypto. Тесты — встроенный в Node
раннер, они идут в CI:

```bash
node --test
```

## Развёртывание

Всё бесплатно: Workers и Turnstile на бесплатном тарифе Cloudflare с запасом
покрывают такой сайт. Понадобятся аккаунт Cloudflare, ваш аккаунт GitHub и Node.js.
Команды запускаются из папки `feedback-worker`.

### 1. Turnstile — проверка «не робот»

1. В панели Cloudflare: **Turnstile → Add widget**.
2. Имя — любое, например `Transliterator`. Hostnames: `shoralbrt.github.io`
   и `localhost`. Режим — **Managed**.
3. Сохранить. Появятся два ключа: **Site Key** (публичный, пойдёт на страницу)
   и **Secret Key** (секретный, пойдёт в функцию).

### 2. GitHub App — право заводить issue

1. GitHub: **Settings → Developer settings → GitHub Apps → New GitHub App**.
2. Заполнить:
   - **GitHub App name** — например `Transliterator Feedback`; это имя будет
     автором issue (`transliterator-feedback[bot]`);
   - **Homepage URL** — `https://shoralbrt.github.io/Transliterator/`;
   - **Webhook** — снять галочку **Active**: события приложению не нужны;
   - **Repository permissions → Issues** — **Read and write**. Остальное
     не трогать (Metadata: Read-only выставится само);
   - **Where can this GitHub App be installed?** — **Only on this account**.
3. **Create GitHub App**. На открывшейся странице записать **App ID**.
4. Там же внизу — **Generate a private key**: скачается файл `.pem`. Перегонять его
   не нужно, функция понимает формат GitHub как есть. Файл никуда не коммитить.
5. Слева **Install App → Install → Only select repositories → Transliterator**.

### 3. Функция

1. Вписать App ID в `wrangler.toml`: `GITHUB_APP_ID = "123456"`. Он не секрет,
   его можно закоммитить.
2. Войти в Cloudflare и развернуть:

   ```bash
   npx wrangler@4 login
   npx wrangler@4 deploy
   ```

   В конце wrangler напечатает адрес функции, вида
   `https://transliterator-feedback.<ваш-поддомен>.workers.dev`.
3. Положить секреты. Ключ GitHub App — содержимым файла:

   ```bash
   npx wrangler@4 secret put GITHUB_APP_PRIVATE_KEY < путь/к/ключу.pem
   ```

   В PowerShell перенаправления `<` нет, там так:

   ```powershell
   Get-Content путь\к\ключу.pem -Raw | npx wrangler@4 secret put GITHUB_APP_PRIVATE_KEY
   ```

   Секрет Turnstile — вставить, когда команда спросит:

   ```bash
   npx wrangler@4 secret put TURNSTILE_SECRET
   ```

### 4. Страница

В `Transliterator.Web/wwwroot/appsettings.json` вписать адрес функции и Site Key
из шага 1 — оба публичные, их видно в коде любой страницы, которая ими
пользуется:

```json
{
  "Feedback": {
    "Endpoint": "https://transliterator-feedback.<ваш-поддомен>.workers.dev/",
    "TurnstileSiteKey": "0x4AAAAAAA..."
  }
}
```

После влития в `master` страница выложится сама. Проверка: открыть страницу,
отправить пробное сообщение — в репозитории появится issue от бота. Пробное
issue потом закрыть.

## Если что-то не так

- `npx wrangler@4 tail` показывает запросы к функции и ошибки в реальном времени.
- Панель пишет «сервер ответил ошибкой» — функция не смогла завести issue.
  Чаще всего: приложение не установлено на репозиторий, у него нет права Issues,
  не тот App ID или ключ.
- «Проверка „не робот“ не прошла» — не тот `TURNSTILE_SECRET` или в виджете
  Turnstile не указан домен страницы.
- «Нет связи с сервером» — неверный `Endpoint` на странице или страница открыта
  не с адреса из `ALLOWED_ORIGINS`.

## Локальный запуск

`npx wrangler@4 dev` поднимает функцию на `http://localhost:8787`. Секреты для неё
берутся из файла `.dev.vars` (он в `.gitignore`). У Turnstile есть тестовые ключи,
которые всегда проходят: Site Key `1x00000000000000000000AA`, Secret Key
`1x0000000000000000000000000000000AA`. Чтобы страница слала в локальную функцию,
положите рядом с `appsettings.json` файл `appsettings.Development.json` с этим
адресом и тестовым ключом — его читает только локальный запуск страницы.
