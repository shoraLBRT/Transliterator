# План разработки конвейера таджвида

Единственный источник правды о том, что сделано и что нет. Артефакты, промпты
и doc-комментарии — вторичны; расходятся с этим файлом — прав этот файл.

Этот файл — про конвейер. Корпус примеров, тесты по нему, веб-версия,
профили пользователя, инфраструктура и решения о системе записи ведутся
в [GitHub issues](https://github.com/shoraLBRT/Transliterator/issues) — см. раздел
«Бэклог: issues и доска» ниже.

## Как пользоваться

- Чекбокс ставит **тот же PR, который реализует пункт**. Отдельным «докладным»
  коммитом отмечать нельзя: чекбокс и код должны попадать в `master` одновременно,
  иначе план разъедется с кодом ровно так, как разъехались хардкод-профиль и
  `Standard.json` до PR #1.
- Пункт считается закрытым, только если выполнены **все** критерии приёмки под ним.
  Частично сделанный пункт остаётся `[ ]`, а сделанная часть выносится соседним
  подпунктом — чтобы было видно, что именно осталось.
- Стадии нумерованы по порядку исполнения в `RulesService.ApplyTajweedRules`.
  Номер — не оформление: каждая стадия опирается на решения предыдущих, и
  переставить их местами нельзя.
- Правки в разные строки этого файла из разных веток git сливает без конфликта.
  Не переписывайте файл целиком и не переставляйте разделы — конфликтует именно это.

> Файл заведён после того, как стадии 1–5, 8, 9 и 11 уже влиты в `master`
> (PR #1, PR #3). Их чекбоксы проставлены задним числом по факту сверки с кодом,
> а не по описаниям PR. Дальше действует правило «чекбокс в том же PR».

## Бэклог: issues и доска

Всё, что не стадии конвейера, ведётся в GitHub issues: они — источник правды
о том, что сделано. Доска — <https://github.com/users/shoraLBRT/projects/6>.
Здесь только то, чего в списке issues нет: правила работы, порядок и предложения.
До переноса бэклог жил в `docs/BACKLOG.md`; его тексты целиком перенесены
в issues, у выполненных — в раздел «Как сделано».

### Как пользоваться

- **Один issue — одна ветка — один PR.** PR закрывает issue строкой `Closes #N`
  в описании, и закрытие попадает в `master` вместе с кодом — то же правило,
  что «чекбокс в том же PR» для стадий.
- Issue закрыт, только если выполнены **все** пункты «Готово, когда».
  Частично сделанный остаётся открытым с комментарием, что именно осталось.
- **Порядок** задают milestone, приоритет и строка «Зависит от» в описании issue,
  а не номер эпика. Берётся открытый issue текущей итерации с наивысшим
  приоритетом, у которого закрыты зависимости.
- **Код пункта** (`A1`, `B13`…) стоит в начале заголовка issue. На коды ссылаются
  этот файл, комментарии в коде и тесты, поэтому коды не переиспользуются и не
  перенумеровываются. Новый пункт получает следующий свободный номер своего
  эпика. Отменённый закрывается как not planned с причиной.
- **`BA`** — бизнес-анализ: issue ждёт решения владельца, в работу не брать.
- **Отзывы со страницы** (issues от `transliterator-feedback`) разбирает сессия:
  даёт код пункта, метки, приоритет и место в итерации, дописывает «Готово, когда».
  Решение, которое принимает владелец, остаётся вопросом с меткой `BA`.

| Что | Как |
|---|---|
| Итерация | milestone: «Итерация 1 · Первая страница» (закрыт), «Итерация 2 · Обратная связь и правила», «Итерация 3 · Отзывчивый интерфейс», «Итерация 4 · Отзывы 2026-11-18» |
| Приоритет | `priority:P1` — взять следующим, `P2` — нужно, но может подождать, `P3` — низкий; дублируется полем Priority на доске |
| Вид работы | `type:bug`, `type:feature`, `type:infra`, `type:docs`, `type:qa`, `BA` |
| Эпик | `epic:*` — таблица ниже |

### Эпики

| Эпик | Метка | Пункты |
|---|---|---|
| A. Корпус примеров и тесты | `epic:corpus` | [A1](https://github.com/shoraLBRT/Transliterator/issues/69) · [A2](https://github.com/shoraLBRT/Transliterator/issues/70) · [A3](https://github.com/shoraLBRT/Transliterator/issues/71) · [A4](https://github.com/shoraLBRT/Transliterator/issues/72) · [A5](https://github.com/shoraLBRT/Transliterator/issues/73) · [A6](https://github.com/shoraLBRT/Transliterator/issues/102) |
| B. Баги конвейера, найденные прогоном | `epic:pipeline` | [B1](https://github.com/shoraLBRT/Transliterator/issues/74) · [B2](https://github.com/shoraLBRT/Transliterator/issues/75) · [B3](https://github.com/shoraLBRT/Transliterator/issues/76) · [B4](https://github.com/shoraLBRT/Transliterator/issues/77) · [B5](https://github.com/shoraLBRT/Transliterator/issues/78) · [B6](https://github.com/shoraLBRT/Transliterator/issues/79) · [B7](https://github.com/shoraLBRT/Transliterator/issues/80) · [B8](https://github.com/shoraLBRT/Transliterator/issues/81) · [B9](https://github.com/shoraLBRT/Transliterator/issues/82) · [B10](https://github.com/shoraLBRT/Transliterator/issues/51) · [B11](https://github.com/shoraLBRT/Transliterator/issues/83) · [B12](https://github.com/shoraLBRT/Transliterator/issues/64) · [B13](https://github.com/shoraLBRT/Transliterator/issues/84) · [B14](https://github.com/shoraLBRT/Transliterator/issues/106) · [B15](https://github.com/shoraLBRT/Transliterator/issues/211) |
| C. Готовность ядра к браузеру | `epic:core-browser` | [C1](https://github.com/shoraLBRT/Transliterator/issues/85) · [C2](https://github.com/shoraLBRT/Transliterator/issues/86) · [C3](https://github.com/shoraLBRT/Transliterator/issues/29) |
| D. Веб-интерфейс | `epic:web` | [D1](https://github.com/shoraLBRT/Transliterator/issues/31) · [D2](https://github.com/shoraLBRT/Transliterator/issues/33) · [D3](https://github.com/shoraLBRT/Transliterator/issues/35) · [D4](https://github.com/shoraLBRT/Transliterator/issues/36) · [D5](https://github.com/shoraLBRT/Transliterator/issues/60) · [D6](https://github.com/shoraLBRT/Transliterator/issues/126) · [D7](https://github.com/shoraLBRT/Transliterator/issues/128) · [D8](https://github.com/shoraLBRT/Transliterator/issues/148) · [D9](https://github.com/shoraLBRT/Transliterator/issues/162) · [D10](https://github.com/shoraLBRT/Transliterator/issues/163) · [D11](https://github.com/shoraLBRT/Transliterator/issues/167) · [D12](https://github.com/shoraLBRT/Transliterator/issues/176) · [D13](https://github.com/shoraLBRT/Transliterator/issues/177) · [D14](https://github.com/shoraLBRT/Transliterator/issues/178) · [D15](https://github.com/shoraLBRT/Transliterator/issues/179) · [D16](https://github.com/shoraLBRT/Transliterator/issues/180) · [D17](https://github.com/shoraLBRT/Transliterator/issues/181) · [D18](https://github.com/shoraLBRT/Transliterator/issues/182) · [D19](https://github.com/shoraLBRT/Transliterator/issues/183) · [D20](https://github.com/shoraLBRT/Transliterator/issues/184) · [D21](https://github.com/shoraLBRT/Transliterator/issues/185) · [D22](https://github.com/shoraLBRT/Transliterator/issues/186) · [D23](https://github.com/shoraLBRT/Transliterator/issues/187) · [D24](https://github.com/shoraLBRT/Transliterator/issues/188) · [D25](https://github.com/shoraLBRT/Transliterator/issues/189) · [D26](https://github.com/shoraLBRT/Transliterator/issues/191) · [D27](https://github.com/shoraLBRT/Transliterator/issues/212) · [D28](https://github.com/shoraLBRT/Transliterator/issues/213) · [D29](https://github.com/shoraLBRT/Transliterator/issues/215) · [D30](https://github.com/shoraLBRT/Transliterator/issues/216) · [D31](https://github.com/shoraLBRT/Transliterator/issues/217) · [D32](https://github.com/shoraLBRT/Transliterator/issues/218) · [D33](https://github.com/shoraLBRT/Transliterator/issues/219) · [D34](https://github.com/shoraLBRT/Transliterator/issues/228) · [D35](https://github.com/shoraLBRT/Transliterator/issues/231) · [D36](https://github.com/shoraLBRT/Transliterator/issues/232) · [D37](https://github.com/shoraLBRT/Transliterator/issues/241) · [D38](https://github.com/shoraLBRT/Transliterator/issues/242) |
| E. Свои профили в браузере | `epic:custom-profiles` | [E1](https://github.com/shoraLBRT/Transliterator/issues/39) · [E2](https://github.com/shoraLBRT/Transliterator/issues/41) · [E3](https://github.com/shoraLBRT/Transliterator/issues/43) · [E4](https://github.com/shoraLBRT/Transliterator/issues/108) · [E5](https://github.com/shoraLBRT/Transliterator/issues/109) · [E6](https://github.com/shoraLBRT/Transliterator/issues/124) · [E7](https://github.com/shoraLBRT/Transliterator/issues/127) · [E8](https://github.com/shoraLBRT/Transliterator/issues/146) · [E9](https://github.com/shoraLBRT/Transliterator/issues/147) · [E10](https://github.com/shoraLBRT/Transliterator/issues/214) · [E11](https://github.com/shoraLBRT/Transliterator/issues/233) |
| F. Инфраструктура | `epic:infra` | [F1](https://github.com/shoraLBRT/Transliterator/issues/45) · [F2](https://github.com/shoraLBRT/Transliterator/issues/49) · [F3](https://github.com/shoraLBRT/Transliterator/issues/53) · [F4](https://github.com/shoraLBRT/Transliterator/issues/55) · [F5](https://github.com/shoraLBRT/Transliterator/issues/57) · [F6](https://github.com/shoraLBRT/Transliterator/issues/87) · [F7](https://github.com/shoraLBRT/Transliterator/issues/110) |
| G. Система записи: графемы `Cyrillic` | `epic:writing-system` | [G1](https://github.com/shoraLBRT/Transliterator/issues/88) · [G2](https://github.com/shoraLBRT/Transliterator/issues/92) · [G3](https://github.com/shoraLBRT/Transliterator/issues/93) · [G4](https://github.com/shoraLBRT/Transliterator/issues/94) · [G5](https://github.com/shoraLBRT/Transliterator/issues/95) · [G6](https://github.com/shoraLBRT/Transliterator/issues/96) · [G7](https://github.com/shoraLBRT/Transliterator/issues/116) |
| H. Связь с пользователями | `epic:feedback` | [H1](https://github.com/shoraLBRT/Transliterator/issues/89) · [H2](https://github.com/shoraLBRT/Transliterator/issues/90) · [H3](https://github.com/shoraLBRT/Transliterator/issues/99) · [H4](https://github.com/shoraLBRT/Transliterator/issues/103) · [H5](https://github.com/shoraLBRT/Transliterator/issues/229) |
| I. Правила таджвида для пользователя | `epic:tajweed-rules` | [I1](https://github.com/shoraLBRT/Transliterator/issues/91) · [I2](https://github.com/shoraLBRT/Transliterator/issues/107) · [I3](https://github.com/shoraLBRT/Transliterator/issues/121) · [I4](https://github.com/shoraLBRT/Transliterator/issues/122) · [I5](https://github.com/shoraLBRT/Transliterator/issues/123) · [I6](https://github.com/shoraLBRT/Transliterator/issues/125) · [I7](https://github.com/shoraLBRT/Transliterator/issues/129) · [I8](https://github.com/shoraLBRT/Transliterator/issues/144) · [I9](https://github.com/shoraLBRT/Transliterator/issues/145) · [I10](https://github.com/shoraLBRT/Transliterator/issues/164) · [I11](https://github.com/shoraLBRT/Transliterator/issues/165) · [I12](https://github.com/shoraLBRT/Transliterator/issues/166) · [I13](https://github.com/shoraLBRT/Transliterator/issues/174) · [I14](https://github.com/shoraLBRT/Transliterator/issues/190) |

Корпус (A) шёл первым, и не из вежливости. Веб-версии нужны готовые суры
для панели примеров, багфиксам нужен эталон, чтобы «починил» не значило «поменял
вывод в другую сторону». Один и тот же корпус кормит и тесты, и интерфейс —
иначе примеры на странице и примеры в тестах разъедутся.

Веб-версия (C, D) — Blazor WebAssembly: `Transliterator.Core` компилируется
в wasm и считает прямо в браузере. Бэкенда нет вообще — ни API, ни CORS,
ни хостинга, страница выкладывается статикой. Это ограничивает всё, что хочет
отправить данные наружу, — например, обратную связь: её принимает отдельная
маленькая функция на Cloudflare Workers (H3, `feedback-worker/`), а страница
остаётся статикой.

GitHub Pages — **dev-стенд** владельца: любое изменение `master` выкатывается туда
сразу (`pages.yml`), и так и должно быть — всё новое проверяется там, а не
ждёт релиза. Версии ведёт release-please по стандарту
[habze](https://github.com/shoraLBRT/habze) (с 0.1.0, решено 2026-11-13); стенд
показывает в подвале версию и коммит сборки (`v0.1.0+7e69cb6`), так что видно,
что на нём сейчас. Боевой сервер (F7) будет выкатывать только релизы `vX.Y.Z`.
Для людей страница выкатится на свой сервер и домен — это последний
шаг, когда продукт полностью готов ([F7](https://github.com/shoraLBRT/Transliterator/issues/110)). «Полностью готов» на сегодня —
закрытая итерация 2. Решено владельцем 2026-11-04.

### Итерация 2

| Порядок | Issue | Зависит от |
|---|---|---|
| 1 | [B13](https://github.com/shoraLBRT/Transliterator/issues/84) — хамза на татвиле пропадает | — |
| 2 | [I1](https://github.com/shoraLBRT/Transliterator/issues/91) — каталог правил таджвида | — (каталог в ядре, раскладка по ступеням утверждена и внесена; ждёт вычитки текстов владельцем) |
| 3 | [H1](https://github.com/shoraLBRT/Transliterator/issues/89) — кнопка обратной связи | — (отправка — ссылкой на форму issue, выбрано владельцем) |
| 4 | [H3](https://github.com/shoraLBRT/Transliterator/issues/99) — обратная связь без аккаунта GitHub | H1; функция развёрнута владельцем |
| 5 | [B14](https://github.com/shoraLBRT/Transliterator/issues/106) — мадд силя доживает до паузы | — |
| 6 | [G4](https://github.com/shoraLBRT/Transliterator/issues/94) — хамза после артикля не пишется, как и в начале слова | — |
| 7 | [H4](https://github.com/shoraLBRT/Transliterator/issues/103) — форма обратной связи без предупреждения о публичности | — |
| 8 | [I2](https://github.com/shoraLBRT/Transliterator/issues/107) — ступени таджвида на странице | I1 |
| 9 | [I3](https://github.com/shoraLBRT/Transliterator/issues/121) — пояснение к правилам в «Своих вариантах» (теперь «Неполный таджвид», I5) | — |
| 10 | [I6](https://github.com/shoraLBRT/Transliterator/issues/125) — группы в «Правилах этого чтения» плохо различимы | — |
| 11 | [E7](https://github.com/shoraLBRT/Transliterator/issues/127) — поиск графем по кириллице и варианты по-русски | — |
| 12 | [I7](https://github.com/shoraLBRT/Transliterator/issues/129) — перевод арабских терминов таджвида | — (перевод у каждого правила каталога и на странице; ждёт вычитки владельцем, как описания I1) |
| 13 | [G2](https://github.com/shoraLBRT/Transliterator/issues/92) — хамза: дефис внутри слова, ничего на конце | — (решено владельцем 2026-11-12) |
| 14 | [G7](https://github.com/shoraLBRT/Transliterator/issues/116) — счёт 5 на письме: сдвиг шкалы долготы | — (решено владельцем 2026-11-12) |
| 15 | [I4](https://github.com/shoraLBRT/Transliterator/issues/122) — отключение безопасных правил | — (решено владельцем 2026-11-12) |
| 16 | [I5](https://github.com/shoraLBRT/Transliterator/issues/123) — ступень «Неполный таджвид» | I4 (пояснение в панели описывает отключение правил) |
| 17 | [E6](https://github.com/shoraLBRT/Transliterator/issues/124) — «Профиль: Standard» на кнопке | — (решено владельцем 2026-11-12; сделано 2026-11-12) |
| 18 | [D6](https://github.com/shoraLBRT/Transliterator/issues/126) — настройки в боковой панели | E6, I5 (раскладка меняет те же кнопки; решено владельцем 2026-11-12; сделано 2026-11-12) |
| 19 | [D7](https://github.com/shoraLBRT/Transliterator/issues/128) — английский интерфейс и локализация | D6 (переводятся итоговые тексты страницы; сделано 2026-11-12, английские описания правил ждут вычитки владельцем) |
| 20 | [I8](https://github.com/shoraLBRT/Transliterator/issues/144) — два свёрнутых блока правил вместо «Правил этого чтения» | — (сделано 2026-11-14; счёт и переключатели «Неполного таджвида» — в строках своих правил) |
| 21 | [I9](https://github.com/shoraLBRT/Transliterator/issues/145) — строка правила таблицей: переключатель справа, описание под стрелкой | — (сделано 2026-11-14; стрелка описания, слева название и пример, справа счёт или переключатель; описание свёрнуто) |
| 22 | [E9](https://github.com/shoraLBRT/Transliterator/issues/147) — «Создать профиль» и таблица графем под «Настройками профиля» | — (сделано 2026-11-14; пояснения про копию нет, графемы — таблицей под свёрнутой стрелкой) |
| 23 | [D8](https://github.com/shoraLBRT/Transliterator/issues/148) — кнопка «?»: что за приложение и зачем | — (сделано 2026-11-14; пример «как пишет страница» считает конвейер, число стадий — `TransliterationService.StageCount`; тексты ждут вычитки владельцем) |
| 24 | [E8](https://github.com/shoraLBRT/Transliterator/issues/146) — встроенный профиль `Standard` — «Cyrillic» | — (решено владельцем 2026-11-14: профиль переименовывается целиком, старое имя `Standard` продолжает работать; сделано 2026-11-14) |
| 25 | [I10](https://github.com/shoraLBRT/Transliterator/issues/164) — панель «Как читать» не закрывается при выборе ступени | — (сделано 2026-11-15; панель открыта при любой ступени, вывод повторён в ней под ступенями, как в панели профиля; общий класс `panel-preview` и строки `Preview.*`) |
| 26 | [I12](https://github.com/shoraLBRT/Transliterator/issues/166) — счёт правил в заголовке блока дробью: 13/13, 0/13, N/13 | — (сделано 2026-11-15; в заголовке обоих блоков «читаемых/всего», читается невыключенное правило; диктору — `Reading.GroupCountLabel` словами; приписка `Reading.GroupOff` и строки `RuleCount.*` убраны) |
| 27 | [I11](https://github.com/shoraLBRT/Transliterator/issues/165) — пояснение у «Неполного таджвида»: где выбрать правила | — (сделано 2026-11-15; `ModeNote.Variants` заменено одной фразой: выбор — ниже, в «Правилах таджвида», слово «правила» одно; текст ждёт вычитки владельцем) |
| 28 | [D9](https://github.com/shoraLBRT/Transliterator/issues/162) — подпись «О чём проект?» у кнопки «?» | — (сделано 2026-11-15; подпись — строка `About.Toggle` (EN «What is this about?»), она же имя для диктора и `title`; заголовок панели `About.Title` остался «О приложении»; на телефоне кнопка с подписью переносится строкой под ступень и профиль) |
| 29 | [D10](https://github.com/shoraLBRT/Transliterator/issues/163) — плавные анимации панелей и вывода | — (сделано 2026-11-15; одна пара `panel-in`/`panel-out` и `--motion-duration` (0.2s) в `app.css` на все панели, форму отзыва и вывод; колонка с 64rem выезжает сбоку; уходящая панель держится `PanelClosing` столько же, уже `inert` и `aria-hidden`; с `prefers-reduced-motion` движения нет и уходящая скрыта сразу) |
| 30 | [I13](https://github.com/shoraLBRT/Transliterator/issues/174) — мадд мунфасиль в два счёта (как в арабском) в «Неполном таджвиде» | — (решено владельцем 2026-11-17: только мунфасиль, муттасиль обязателен и остаётся 4 / 5; сделано 2026-11-17; у мунфасиля счёты 2 / 4 / 5, по умолчанию 4; при двух `MaddRule` ставит счёт, а не удлиняет, и снимает четыре от знака мадды — гласная читается, как в «Без таджвида»; описание правила говорит о коротком чтении) |

Решения владельца 2026-11-12 по задачам `BA`: G2, G7, I4, I5, E6, D6 и D7 взяты
в итерацию 2, что именно решено, записано в каждом issue. Английский интерфейс (D7)
делается сейчас, а не после итерации, поэтому итерация 2 и боевой хостинг ждут и его.

Отзывы со страницы от 2026-11-12 (#144–#148) разобраны 2026-11-14: I8, I9, E9 и D8
взяты в итерацию 2. [E8](https://github.com/shoraLBRT/Transliterator/issues/146) (встроенный профиль `Standard` — «Cyrillic»)
решён владельцем 2026-11-14: профиль переименовывается целиком (вариант Б в issue),
старое имя `Standard` продолжает работать; взят в итерацию 2 и сделан 2026-11-14.
Как устроено: `Standard.json` стал `Cyrillic.json`, имена встроенных и их прежние имена —
`ProfileNames` в домене. Прежнее имя — запасной путь, а не второе имя: профиль ищется
сначала по имени как есть, и только если такого нет — по прежнему (`--profile Standard`
в CLI, `TransliterationService`). Сохранённый в браузере выбор `Standard` страница
переписывает на `Cyrillic`. Свой профиль под именем встроенного `UserProfileRepository`
больше не пропускает, а переносит под свободное имя копии («Cyrillic (копия)»), и выбор
такого профиля переезжает вместе с ним.

Отзывы со страницы от 2026-11-15 (#162–#167) разобраны 2026-11-15: I10, I11, I12, D9
и D10 взяты в итерацию 2, дубликатов нет. [D11](https://github.com/shoraLBRT/Transliterator/issues/167) (подсказка
у буквы транслита: как её читать) решён владельцем 2026-11-18 — таблица звуков в панели,
без подсказок у знаков вывода — и сделан в итерации 3.

Отзыв со страницы #174 разобран 2026-11-17 как I13 и взят в итерацию 2:
владелец решил дать короткое чтение (два счёта) только мунфасилю — муттасиль
обязателен (ваджиб), его все чтецы тянут не меньше четырёх.

### Итерация 3

Milestone «Итерация 3 · Отзывчивый интерфейс». Пункты найдены UI/UX-аудитом страницы
2026-11-17 по просьбе владельца: сделать страницу отзывчивой, с анимациями, разгрузить
тексты и довести вид до готового продукта. Порядок — от основы (токены, шапка, отклик
кнопок) к крупным изменениям раскладки и в конце — проверка доступности всего сделанного.

| Порядок | Issue | Зависит от |
|---|---|---|
| 1 | [D12](https://github.com/shoraLBRT/Transliterator/issues/176) — токены дизайна и тёмная тема | — (сделано 2026-11-17; шкалы отступов, радиусов, теней, кеглей и движения — токены в `:root`, а тёмная тема по `prefers-color-scheme` переопределяет только цвета и тени; вне этих двух блоков в `app.css` нет ни цвета, ни длительности, что проверяет `PageLayoutTests`; тёмная тема убрана в [D27](https://github.com/shoraLBRT/Transliterator/issues/212) — токены теперь только в `:root`) |
| 2 | [D13](https://github.com/shoraLBRT/Transliterator/issues/177) — шапка страницы: имя, знак и язык без наложения | — (сделано 2026-11-17; шапка `.topbar` в потоке над полем — знак «нун» и видимый h1 слева, RU/EN справа; её нижний отступ держит поле на прежних 24vh/7vh, а с 64rem она сдвигается вместе с полем, и колонка панели её не накрывает. Favicon — тот же знак в SVG со своей тёмной темой и PNG 180×180 для apple-touch-icon.) |
| 3 | [D21](https://github.com/shoraLBRT/Transliterator/issues/185) — микровзаимодействия: нажатия, наведение, переключатели | D12 (сделано 2026-11-17; все кнопки сменяют состояния одним переходом `--control-transition` и сжимаются под пальцем, ручка переключателя ходит с пружиной `--ease-spring`, строки панели появляются каскадом; блоки правил, описание правила и «Настройки профиля» сворачиваются классом `.fold` — сетка 0fr↔1fr и прозрачность, свёрнутый inert и aria-hidden) |
| 4 | [D17](https://github.com/shoraLBRT/Transliterator/issues/181) — карточка вывода: шапка, копирование сверху, номера аятов | D12 (сделано 2026-11-17; шапка карточки `.result-head` липнет к верху экрана: видимая подпись, метки ступени и профиля открывают свои панели через `PanelPicker.OpenAsync`, справа «Копировать» с галочкой-штрихом и живой подписью; вывод рисуется строками `ResultLine.Split`, номер аята — круглый знак с цифрой текстом, кнопка копирует прежний `_result`) |
| 5 | [D16](https://github.com/shoraLBRT/Transliterator/issues/180) — подсказка над полем уходит, когда текст введён | — (сделано 2026-11-17; подсказка завёрнута в `.fold` и схлопывается с текстом в поле, `page.hintPaused` гасит таймер печати и возобновляет с начала) |
| 6 | [D24](https://github.com/shoraLBRT/Transliterator/issues/188) — быстрый старт в пустом состоянии | — (сделано 2026-11-17; под полем в пустом состоянии подпись и три чипа сур 1, 112, 114 из корпуса — тот же `ChooseAsync`; блок — `.fold`, схлопывается с текстом, чипы появляются каскадом) |
| 7 | [D20](https://github.com/shoraLBRT/Transliterator/issues/184) — поле ввода: без двойной прокрутки, вставка и отмена очистки | — (сделано 2026-11-17; поле без потолка растёт по тексту, высоту пересчитывает page.resize при вводе и при смене ширины через ResizeObserver; у пустого поля кнопка «Вставить» читает буфер, а после очистки плашка внизу 5 с предлагает «Вернуть») |
| 8 | [D14](https://github.com/shoraLBRT/Transliterator/issues/178) — панели на телефоне — нижним листом | D12 (сделано 2026-11-17; уже 64rem `.picker-panel` — fixed-лист у низа экрана до 85dvh со своей прокруткой, липкой шапкой с ручкой и выездом `sheet-in`/`sheet-out`, под ним подложка `--scrim`, щелчок по ней закрывает лист и возвращает фокус на кнопку; пока панель открыта, `page.sheetOpen` ставит `<html>` класс `is-sheet-open` и снимает прокрутку страницы, с 64rem всё как прежде) |
| 9 | [D15](https://github.com/shoraLBRT/Transliterator/issues/179) — выбор суры — у своей кнопки и с поиском | D14 (сделано 2026-11-17; с 64rem список сур — выпадающий под кнопкой «Выбрать суру» (absolute, до 60dvh, прозрачная подложка закрывает щелчком мимо), страница не ставит ему `has-panel` и ничего не сдвигает, уже 64rem — лист D14; вверху поиск-combobox по номеру, имени на любом языке страницы и арабскому имени без харакатов (`SurahSearch`), ↑/↓/Enter/Escape, текущая сура отмечена галочкой, под именем — число аятов) |
| 10 | [D23](https://github.com/shoraLBRT/Transliterator/issues/187) — боковые кнопки и кнопка отзыва: подсказки и вид | D12 (сделано 2026-11-17; с 64rem кнопки сбоку — капсула вровень с верхом поля, открытая кнопка залита акцентом и отмечена чертой, а пока открыта колонка, подпись кнопки показывается подсказкой поверх колонки при наведении и фокусе; значок отзыва в углу раскрывается подписью, title у этих кнопок убран, чтобы подсказка не двоилась) |
| 11 | [D19](https://github.com/shoraLBRT/Transliterator/issues/183) — подсветка изменённого в выводе | D17 (сделано 2026-11-17; `OutputDiff` в ядре сравнивает прежний и новый вывод по словам — LCS внутри строки, строки по номеру; смена ступени, правила или профиля оборачивает изменённые слова в `mark.changed` со вспышкой `--highlight` за `--motion-highlight`, а без изменений показывает «Вывод не изменился» на 3 с) |
| 12 | [D18](https://github.com/shoraLBRT/Transliterator/issues/182) — арабский текст над строками транслитерации | D17 (сделано 2026-11-17; тумблер «С арабским» в шапке карточки вывода раскрывает над каждой строкой её арабскую строку поля (`.fold`, справа налево, тише, не выделяется мышью); пары по номеру строки даёт `ResultLine.Sources` в ядре, выбор помнится в `transliterator.arabicLines`, кнопка копирует только транслитерацию) |
| 13 | [D22](https://github.com/shoraLBRT/Transliterator/issues/186) — экран загрузки: скелет и прогресс | — (сделано 2026-11-17; в `#app` до старта — скелет на тех же классах каркаса (`.shell.boot`: шапка, подсказка, поле, чипы, кнопки сбоку) с мерцанием `--motion-shimmer`, поле встаёт на то же место; полоса и проценты — по `--blazor-load-percentage`, диктору — `progressbar` с `aria-valuenow` из `page.js`, готовая страница проявляется `shell-in`) |
| 14 | [I14](https://github.com/shoraLBRT/Transliterator/issues/190) — тексты панели «Как читать» короче | — (сделано 2026-11-17; над правилами одна короткая фраза и ссылка «Подробнее» с `aria-expanded`, длинные пояснения в `.fold` под ней; пояснения ступеней короткие и разные) |
| 15 | [D26](https://github.com/shoraLBRT/Transliterator/issues/191) — тексты страницы короче и понятнее | — (сделано 2026-11-17; «О приложении» — лид и пример сразу, остальные разделы под заголовками-кнопками на `.fold`; число стадий осталось в раскрывающемся «Как это устроено»; пояснения формы отзыва, профиля и конфликтов сокращены, ключи RU и EN совпадают) |
| 16 | [D25](https://github.com/shoraLBRT/Transliterator/issues/189) — доступность: цели, контраст, объявления | все пункты итерации 3 (сделано 2026-11-17; мелкие кнопки ловят нажатие в квадрате 44px — прозрачный `::after` по центру при прежнем виде, рамка фокуса — токен `--focus-ring` с контрастом от 4.7:1, кнопки под полем не переносят подписи, и `page.fitBar` оставляет «Вставить» значком, когда обе не помещаются; «Вывод обновлён» — своя вежливая живая область через секунду после последней правки, нижний лист держит Tab в себе, а арабские буквы в описаниях правил размечены `lang="ar"`) |
| 17 | [D11](https://github.com/shoraLBRT/Transliterator/issues/167) — таблица звуков: как читать буквы | — (решено владельцем 2026-11-18: вариант Б; сделано 2026-11-18; тексты о произношении 28 букв и хамзы — в данных ядра `Resources/Sounds.json` (`LocalizedText`, загрузчик `EmbeddedSoundTable` роняет чтение без текста на языке страницы); кнопка «Как читать буквы» в шапке вывода и ссылка в «О чём проект?» открывают панель `SoundsPicker`: сначала звуки, которых нет в русском, остальные под свёрнутым «Остальные буквы», запись буквы — графема текущего профиля) |

### Итерация 4

Milestone «Итерация 4 · Отзывы 2026-11-18». Отзывы со страницы #211–#219 разобраны
2026-11-18: коды, метки, приоритет и «Готово, когда» — в issues. Порядок — сначала
найденная причина и решённые просьбы владельца, затем пункты с `BA` (не брать, пока
владелец не выберет вариант — варианты и рекомендация в issue), в конце — проверка
анимаций после всех изменений раскладки. Вторые отзывы #228–#233 разобраны тем же
порядком 2026-11-18 — строки 10–14, третьи (#241, #242; #244 — дубликат #242) — строки 15–16.

| Порядок | Issue | Зависит от |
|---|---|---|
| 1 | [D31](https://github.com/shoraLBRT/Transliterator/issues/217) — значок вкладки: валидный `favicon.svg` и запасной PNG | — (причина найдена: `--` в XML-комментарии делает SVG невалидным; сделано 2026-11-18, кроме проверки во вкладке Chrome, Firefox и Edge на стенде — она после деплоя: комментарий `favicon.svg` без двух дефисов подряд, тест разбирает файл как XML; запасной `favicon-32.png` 32×32 в светлой теме подключён `<link rel="icon" type="image/png" sizes="32x32">` перед SVG) |
| 2 | [D27](https://github.com/shoraLBRT/Transliterator/issues/212) — без тёмной темы, страница всегда светлая | — (сделано 2026-11-18; блока `prefers-color-scheme: dark` в `app.css` нет, светлые токены прежние; `color-scheme: light` в `:root` и `<meta name="color-scheme" content="light">`, один `theme-color` без `media`; у `favicon.svg` тёмного варианта нет; `PageLayoutTests` проверяет, что тёмной темы нет) |
| 3 | [D32](https://github.com/shoraLBRT/Transliterator/issues/218) — панель настроек не сдвигает страницу | — (решено владельцем 2026-11-18, вариант «В»; сделано 2026-11-18: с 64rem `has-panel` больше не меняет `--rail-space` — шапка, поле, вывод и подвал при открытии колонки не двигаются; ширина колонки прежняя, 25rem; с 110rem колонка помещается слева от поля и ничего не накрывает, уже 110rem ложится поверх, и под ней прозрачная подложка, как у списка сур: щелчок мимо и Escape закрывают панель, фокус — на кнопку; лист D14 уже 64rem прежний; правило проверяет `PageLayoutTests`) |
| 4 | [D33](https://github.com/shoraLBRT/Transliterator/issues/219) — кнопка отзыва: подсветка и подсказка вместо текста | — (сделано 2026-11-18; с 56rem значок отзыва в углу — круг 2.75rem постоянной ширины, наведение и фокус только подсвечивают его фоном и рамкой, подпись не раскрывается; её показывает подсказка слева от значка тем же видом, что у кнопок сбоку (D23), с той же задержкой; `title` нет, имя для диктора — та же подпись, скрытая через `opacity`; поля плашки `.toast-region` прежние — подсказка не шире 9rem) |
| 5 | [D28](https://github.com/shoraLBRT/Transliterator/issues/213) — под полем меньше лишнего, пояснение в placeholder | — (сделано 2026-11-18; быстрый старт D24 — чипы, «Попробовать:», пояснение — убран и из скелета загрузки; пояснение ушло в placeholder `Composer.Placeholder` шрифтом интерфейса, у пустого поля нет `lang="ar"`; у «Выбрать суру» подсказка `Surah.Tip` видом D23/D33 через `aria-describedby`, без `title`; `Hint.7` переписан; тексты RU/EN ждут вычитки владельцем) |
| 6 | [E10](https://github.com/shoraLBRT/Transliterator/issues/214) — пояснение профиля проще | — (сделано 2026-11-18; `Profile.Caption` в RU — «Профиль — это то, какими русскими буквами пишется каждая арабская буква.», в EN — «A profile sets which letters are used to write each Arabic letter.»; ключи RU/EN прежние; тексты ждут вычитки владельцем) |
| 7 | [D29](https://github.com/shoraLBRT/Transliterator/issues/215) — «Как читать буквы» без текста, рядом с профилем | D32 (сделано 2026-11-18; в панели профиля перед «Настройки профиля» — кнопка «Как читать буквы» тем же видом, видна и при пустом поле; открывает `SoundsPicker` (`OpenFromAsync`), панель профиля закрывается; ссылка из «О чём проект?» открывает ту же таблицу; открытая из другой панели, таблица по Escape и крестику возвращает фокус на кнопку той панели сбоку, открытая кнопкой в шапке вывода — на неё, как прежде; правило проверяет `PageLayoutTests`; отдельный вход и отдельную панель звуков заменил E11) |
| 8 | [D30](https://github.com/shoraLBRT/Transliterator/issues/216) — анимации заметны (пересекается с D10, D21; не дубликат) | D27, D32, D33 (сделано 2026-11-18, кроме проверки на машине владельца — она после деплоя: причина — (1), в браузере на машине разработки `prefers-reduced-motion: reduce` включён, а блок в конце `app.css` снимал все `animation` и `transition`; теперь ходы, которые двигают, берут токены `--motion-move*`, сдвиг в `@keyframes` умножен на `--motion-travel`, и reduce обнуляет только их — панели, карточка, плашка, строки панели и вспышки проявляются и гаснут на месте; `--motion-duration` 0.25 с, `PanelClosing.Duration` 250 мс; смена настройки проявляет текст вывода (`text-in`); смена панели с 64rem — уход одной и появление другой; правило проверяет `PageLayoutTests`) |
| 9 | [B15](https://github.com/shoraLBRT/Transliterator/issues/211) — перенос строки и знаки вакфа — пауза | — (решено владельцем 2026-11-18, вариант «Б»; сделано 2026-11-18: граница на переводе строки — пауза, `WaqfRule` ставит ей `IsPause`; пауза на ۘ, ۗ и ۚ, на ۖ, ۙ, ۛ, ۜ чтение слитное; в корпусе изменился только 110:3 — `уастагъфирh` перед ۚ; тест на суру 110 из отзыва — `LineBreakTests`) |
| 10 | [H5](https://github.com/shoraLBRT/Transliterator/issues/229) — отзыв: повторная проверка «не робот» после неудачи | — (сделано 2026-11-18, кроме проверки на стенде — она после деплоя: флажок проигнорирован → повторная отправка проходит; причина — виджет Turnstile без колбэков, после проигнорированной проверки `guardReset` не возвращал флажок, а «Отправить» ждал пустой токен 10 секунд; теперь `page.guard` ставит колбэки, ошибка, истечение и пропущенный флажок ставят виджет заново (`remove` + `render`, без `reset`), три ошибки подряд — «проверка не загрузилась» и форма GitHub; «Отправить» ждёт исхода проверки, а не срока (предохранитель 2 мин), и продолжает сама после флажка; когда нужен флажок, `role="status"` говорит `Feedback.Confirm`; функция `feedback-worker/` не менялась; правило проверяет `PageLayoutTests`) |
| 11 | [D34](https://github.com/shoraLBRT/Transliterator/issues/228) — под полем без кнопки «Вставить» | — (сделано 2026-11-18; кнопки «Вставить» нет ни на широком экране, ни на телефоне, ни в скелете загрузки — у пустого поля под ним только «Выбрать суру»; ключи `Composer.Paste`, `Composer.PasteTitle`, `Composer.PasteFailed`, `Composer.PasteEmpty`, `page.canPaste`, `page.paste`, строка `.composer-note` убраны; `page.fitBar` и `.is-compact` убраны целиком — сжимать до значка больше нечего, неперенос подписей D25 остался; вставка Ctrl+V и долгим нажатием — обычная вставка поля, не менялась; «Очистить» и «Вернуть» D20 прежние; правило проверяет `PageLayoutTests`) |
| 12 | [D35](https://github.com/shoraLBRT/Transliterator/issues/231) — placeholder поля снова арабским текстом (возврат части D28) | — (сделано 2026-11-18; в placeholder снова первый аят суры 1 из корпуса с запасной строкой `FallbackPlaceholder`, арабским шрифтом справа налево, у поля всегда `lang="ar"`; видимого пояснения и всплывающей подсказки нет; `Composer.Placeholder` теперь скрытый текст — описание поля для диктора через `aria-describedby`; остальное из D28 — без быстрого старта, подсказка «Выбрать суру», `Hint.7` — на месте) |
| 13 | [D36](https://github.com/shoraLBRT/Transliterator/issues/232) — список сур шириной поля, в несколько столбцов; кнопки меньше | D34 (сделано 2026-11-18; с 64rem выпадающий список сур шириной карточки поля — отсчёт от `.composer`, подсказка D28 встаёт под карточкой против кнопки; суры — сеткой `auto-fill` (столбец не уже 12rem, на ширине поля три столбца; в листе — 14rem, от ~30rem экрана два столбца, уже — один), строки плотнее, но не ниже 44px; 7 сур видны без прокрутки; поиск липнет сверху, одна найденная сура не растягивается, длинный список прокручивается до 60% высоты; «Ничего не найдено» — строка `role="status"`, что стоит всегда, во всю ширину; стрелки идут по номерам, как прежде; «Выбрать суру» и крестик под полем, круги кнопок сбоку на ступень меньше, цели 44px через `::after`, зазор в капсуле сбоку 0.5rem; правило проверяет `PageLayoutTests`) |
| 14 | [E11](https://github.com/shoraLBRT/Transliterator/issues/233) — звучание букв в таблице графем профиля (переделка D29) | — (сделано 2026-11-18; решение владельца — вариант А: четвёртый столбец «Звучание» в таблице графем под «Настройки профиля», одной ячейкой на букву (`rowspan`, как сама буква), в строке буквы после графемы — диктор читает его вслед за ней; название, пометка «нет в русском», как произносить мельче; буква и графема — узкие столбцы, вариант 30%, звучание — остаток, текст переносится: без прокрутки вбок в 25rem и в 360px; поиск находит букву по звучанию с трёх знаков (`RuleTable.Filter(…, describe)`); `SoundsPicker` и вход в панели профиля удалены, «Как читать буквы» в шапке вывода и в «О чём проект?» открывают панель профиля с раскрытыми настройками и фокусом на их заголовке (`ProfilePicker.OpenSettingsAsync`), Escape — на кнопку профиля сбоку; правило проверяет `PageLayoutTests`) |
| 15 | [D38](https://github.com/shoraLBRT/Transliterator/issues/242) — плотнее по вертикали, метки шапки вывода в одну строку | — (сделано 2026-11-18; межстрочные — токены `--leading-arabic` 1.65 (поле, было 1.9), `--leading-output` 1.5 (вывод, было 1.75), `--leading-arabic-line` 1.45 (арабская строка D18, было 1.6), огласовки в пределах строки; поля строк вывода, воздух и черта между парами «арабская + транслит» вдвое меньше, арабская вплотную к своей строке; отступы подсказки, поля, кнопок сбоку и карточки вывода на ступень меньше; «Выбрать суру» — кегль `--text-xs`, крестик `--bar-icon-size` 1.75rem, цели 44px через `::after`; метки в шапке вывода — «Чтение: Полный таджвид», «Профиль: Cyrillic» в одну строку второй строкой шапки, значение обрезается многоточием, подпись видна (E6), имя для диктора начинается с той же подписи; на 1300px от верха до низа карточки вывода суры 110 с арабским −92px, на 360px высота страницы −119px; правило проверяет `PageLayoutTests`) |
| 16 | [D37](https://github.com/shoraLBRT/Transliterator/issues/241) — панели выходят из своих кнопок сбоку | D38 |

Вне итераций: [H2](https://github.com/shoraLBRT/Transliterator/issues/90) (другие каналы связи не нужны — формы обратной связи
хватает, подтверждено 2026-11-12) и [F7](https://github.com/shoraLBRT/Transliterator/issues/110) (боевой хостинг — последним, пока
не нужен: dev-стенд на Pages владельца устраивает, подтверждено 2026-11-12).
Вне итераций без решения владельца не стоят, но
и не запланированы: A6 (короткие суры 30-го джуза). E4, E5 и F6
сделаны по просьбе владельца вне итерации.

Интерфейс своих профилей (D2, D4, E1–E3) снят со страницы в D5 и возвращён
в [E4](https://github.com/shoraLBRT/Transliterator/issues/108) по образцу выбора суры и ступени чтения — кнопкой с панелью.
У кнопки подпись перед именем — «Профиль: Cyrillic», свой профиль своим именем
(«Профиль: Мой»), решено владельцем 2026-11-12 ([E6](https://github.com/shoraLBRT/Transliterator/issues/124), сделано); имена профилей
в E6 не менялись, встроенный кириллический назван `Cyrillic` в E8. Подпись — часть текста кнопки: она же имя кнопки для диктора; длинное
имя обрезается многоточием с конца, подпись остаётся видна. В панели — выбор профиля, «Создать профиль» одним нажатием из выбранного
([E9](https://github.com/shoraLBRT/Transliterator/issues/147)), переименование и удаление своего; поиск и таблица графем — под стрелкой
«Настройки профиля» (при открытии панели свёрнута, у только что созданного профиля раскрыта;
столбцы буква / вариант / графема одной ширины, с границами, варианты мельче и тише буквы); и повтор вывода — только
на телефоне, где панель закрывает результат, а правка должна быть видна сразу. Экспорт и импорт JSON
(E3) в ядре есть, на страницу не вынесены. Поиск графем находит букву и по тому,
как она пишется («х»), и по русскому пояснению варианта («пауз» → `waqf`);
пояснения вариантов — `RuleTable.VariantNotes` в ядре (E7).

Раскладка страницы ([D6](https://github.com/shoraLBRT/Transliterator/issues/126), решено владельцем 2026-11-12, сделано): под полем
осталась только кнопка «Выбрать суру» — сура меняет сам текст. Ступень чтения
и профиль — круглые кнопки со значками (звуковая волна — как читать, перо — как
писать), рядом с каждой подписано, что выбрано сейчас: «Полный таджвид»,
«Профиль: Cyrillic» (условие E6). С 64rem кнопки стоят столбиком у левого края,
а панель любой кнопки, и сур тоже, открывается колонкой справа от них; поле,
вывод и подвал отодвигаются на ширину колонки, подложки нет — щелчок по полю
панель не закрывает. Пока колонка открыта, подписи у кнопок спрятаны с экрана
(для диктора остаются). Уже 64rem двум колонкам нет места: кнопки стоят строкой
под полем, панель открывается под ними поверх вывода, как до D6. Все три панели
ведут себя одинаково: открыта одна (`PagePanels` в ядре), шапка с именем
и крестиком, Escape возвращает фокус на кнопку (`PanelPicker` в веб-проекте).
Выбор ступени чтения панель «Как читать» не закрывает ([I10](https://github.com/shoraLBRT/Transliterator/issues/164)):
ступени сравнивают подряд, а на телефоне вывод повторён в панели под ступенями,
как в панели профиля. Раскладку проверяет `PageLayoutTests`.

Языки страницы ([D7](https://github.com/shoraLBRT/Transliterator/issues/128), решено владельцем 2026-11-12, сделано): русский
и английский. Язык выбирается сам — сохранённый выбор, иначе первый язык браузера,
который есть у страницы (`en-GB` — английский), иначе русский: страница пишет
кириллицей, и у кого браузер на казахском или узбекском, тот скорее читает
по-русски. Переключатель «RU / EN» — в правом верхнем углу; выбор хранится
в браузере (`transliterator.language`), автоматический не записывается. С английским
интерфейсом профиль по умолчанию — `Latin`. Сохранённый выбор профиля важнее
языка: кто выбрал Cyrillic и переключился на английский, остаётся на Cyrillic;
удаление своего профиля возвращает к профилю языка и стирает выбор — это не выбор
человека. Как устроено (решения сессии):
- Список языков один — `PageLanguages` в домене: код, имя на самом языке и профиль
  по умолчанию. Третий язык — строка там, его файлы `.resx` и его тексты в данных;
  чего не хватает, скажут тесты.
- Строки интерфейса — ресурсы `.resx`, как принято в .NET и Blazor: страница —
  `Transliterator.Web/Localization/Strings.resx` через `IStringLocalizer<Strings>`,
  тексты ядра, которые видит человек (пояснения вариантов графем E7, сообщения
  редактора профилей), — `Transliterator.Core/Localization/CoreText.resx`. Русский —
  нейтральный язык сборок, остальные — спутниковые сборки (`*.en.resx`).
- Тексты данных — рядом с данными, объектом «язык: текст» (`LocalizedText`):
  названия, переводы `Translation` и описания правил и имена стадий в каталоге,
  имена сур в корпусе (`Name` вместо `RussianName`). Каталог и корпус без текста на
  каком-нибудь языке страницы не загружаются.
- Язык ставится до запуска приложения, а смена — запись выбора и перезагрузка:
  так в Blazor WebAssembly принято, спутниковые сборки грузятся под культуру
  старта. Текст поля переживает перезагрузку (`sessionStorage`). Сборка грузит
  данные всех культур (`BlazorWebAssemblyLoadAllGlobalizationData`): язык страницы
  выбирается уже после загрузки. «Загрузка…» и полоса ошибки видны раньше
  ресурсов — их тексты в `page.js`.
- Тест падает, если у строки нет перевода (`PageLocalizationTests`: ключи всех
  языков совпадают, ключ, который берёт разметка, есть в ресурсах, русского текста
  в разметке нет), и если у правила или суры нет текста на языке страницы.
Английские тексты написаны сессией; учебные описания правил каталога вычитывает
владелец, как русские (I1, I7). Не переводятся: сама транслитерация, сообщение
обратной связи, которое становится issue (его читает владелец), форма issue
на GitHub, сообщения разбора JSON профиля (`ProfileJson`, импорта на странице нет;
перечень недостающих ключей из него попадает в сообщение о несостоявшейся копии)
и пояснения к обязательным ключам (`RequiredProfileKeys`) — на странице их нет.

### Предложения, не взятые в работу

Не задачи. Записаны, чтобы не всплывали заново. Становятся issues, когда их
планируют в итерацию.

- **Подсветка сработавших правил.** Показывать, какое правило таджвида сработало
  на каком месте, — учебная ценность выше, чем у самой транслитерации. Требует,
  чтобы сегменты помнили происхождение: сейчас они этого не хранят.
- **Аудио.** Сопоставление транслитерации с чтением. Ломает «минималистичную
  страничку», требует прав на записи чтецов.
- **Полный Коран в корпусе.** 114 сур — это мегабайты в wasm-сборке. Нужна либо
  ленивая подгрузка, либо внешний источник текста. Пока корпус — это примеры
  и тесты, а не библиотека.
- **Другие целевые языки.** Латиница профилем проверена задачей F4 (`Latin`);
  МФА и другие языки — нет.
- **Выбор правил таджвида.** Включать и выключать отдельные правила. Например,
  удлинение гласной на паузе в конце строки (мадд арид, B11): кому-то нужна
  запись чтения нараспев, кому-то слово как в словаре. Записано по просьбе
  владельца как возможная доработка. Правила уже отдельные стадии конвейера
  (`RulesService`), но выключить можно не каждую. Стадии опираются друг на друга:
  васля, артикль и идгам меняют то, что видят следующие, и отключение такой стадии
  ломает чтение, а не упрощает его. Безопасные кандидаты — длительность мадда
  и кальканя: они только помечают звук. Выбор хранился бы рядом с профилем
  пользователя (E1). Первый шаг — каталог правил с описаниями и примерами (I1).
  Каталог есть: `Resources/TajweedRules.json` в ядре, читает его
  `EmbeddedTajweedRuleCatalog`. Две ступени — стадия конвейера и правило, которое
  она исполняет; каждый пример сверяется с конвейером тестом. Пары
  «с правилом / без правила» пока нет — вывод без правила появится вместе
  с самим отключением. **Взято в работу** (2026-11-04): владелец решил, что
  отключать нужно не правила по одному, а ступенями — полный таджвид, варианты
  внутри таджвида (счёт мадда арид и т. п.), обычное арабское чтение без таджвида.
  Раскладка утверждена владельцем в I1 и записана в каталоге: у правила ступень
  `Tier` — `Base` (есть в любом чтении) или `Tajweed` (убирается в обычном
  чтении), а у правил с выбором — допустимые счёты `Counts` и `DefaultCount`:
  мадд арид и мадд лин 2 / 4 / 6, муттасиль 4 / 5, мунфасиль 2 / 4 / 5
  (два счёта — каср, I13). Ступени
  на странице — I2: кнопка рядом с выбором суры, выбор хранится в браузере.
  Конвейер получает его как `ReadingOptions`. В обычном чтении стадии не выпадают
  целиком — каждая сама знает, что из неё основа: одинаковые буквы сливаются
  и без таджвида, разные — нет, и шадда, которой мусхаф отметил их слияние,
  снимается (`مِن رَّبِّهِمْ` → «мин роббиhим»). Счёт 5 у муттасиля
  и мунфасиля пишется четырьмя графемами, отдельно от 4 (три) и 6 (пять): шкала
  долготы сдвинута (G7, см. стадию 11), и пояснение в «Неполном таджвиде» говорит,
  почему букв не столько, сколько харакатов. В «Неполном таджвиде» у каждого правила с выбором
  счёта — описание и пример из каталога, и пример пересчитывается под выбранный
  счёт тем же профилем, что и вывод страницы (I3). Тест держит пример «чистым»:
  счёты соседних правил его не меняют. В «Правилах этого чтения» заголовок группы
  прилипает к верху панели, показывает число правил, а в обычном чтении — что
  правила таджвида сейчас не читаются (I6). У каждого правила каталога есть
  `Translation` — перевод названия, а у названия, которое уже по-русски, короткое
  пояснение: «Мадд лязим (обязательное удлинение)», «Лунный лям (лям звучит ясно)».
  Страница пишет его в скобках после названия в обоих местах, где показывает
  правило; каталог без перевода не загружается (I7). Переводы написаны сессией
  и ждут вычитки владельцем. Решено владельцем 2026-11-12 (I4): кроме ступеней,
  в «Неполном таджвиде» (так теперь называется ступень «Свои варианты», I5)
  можно отключать по одному правила, которые только помечают звук, — кальканя,
  гунна (идгам с гунной, икляб, ихфа), мадд силя. Правила, которые меняют то, что
  видят следующие стадии (вакф, васля, артикль, идгам без гунны), по одному
  не отключаются. Сделано (I4): у таких правил в каталоге `Switchable`, конвейер
  получает выключенные в `ReadingOptions.Off`, а страница хранит их в браузере
  в той же записи, что ступень и счёты (`"off":["iqlab"]`). Выключено правило —
  его место читается, как в обычном чтении, а всё остальное — как в полном
  таджвиде: `مِنۢ بَعْدِ` → «мин ба'д», `مَن يَقُولُ` → «ман йаqууль»,
  `إِنَّهُۥ كَانَ` → «иннаhу каан». Кальканя выключается двумя правилами
  каталога — сугра и кубра, — гунна тремя, как в решении. Тест на корпусе
  сравнивает сегменты с правилом и без него место в место: отличаются только
  места правила и только тем, что оно делает. Икляба в корпусе нет, его держат
  точечные тесты. Гунну ихфы и отзвук кальканя `Cyrillic` не пишет — их
  выключение видно в «Правилах этого чтения» и в своём профиле; так и сказано
  рядом с переключателями. Ихфа шафави (та же гунна у мима) в решение
  не вошла и не выключается.
  Сделано (I5): вторая ступень называется «Неполный таджвид» и на кнопке под
  полем, и в панели; пояснение под названием говорит про оба выбора — счёт
  мадда и правила, которые только помечают звук и которые можно не читать.
  Имя в коде (`ReadingMode.Variants`) и в браузере (`"mode":"variants"`)
  прежнее: сохранённый выбор не теряется. Тест `PageHintTests` держит название
  и пояснение.

## Архитектурные инварианты

Нарушение любого — повод завернуть PR. Это те самые ошибки, из-за которых
конвейер переписывали в PR #1.

- Правила работают только с `IList<Segment>`, никогда со строкой кириллицы
- Ни одной кириллической графемы в коде правил — всё через профиль
  (`"буква|вариант"`). Правило решает *что за звук*, профиль — *как это записать*
- `Harakah.Sukun` и `Harakah.None` — разные состояния, а не «пустая строка»
- Долгота хранится в харакатах (2/4/5/6), а не удвоением графемы
- Рендеринг — последняя стадия; после неё правил нет

## Стадии конвейера

Число строк этой таблицы называет страница в «О приложении» (D8): оно берётся из
`TransliterationService.StageCount`, и тест сверяет константу с этой таблицей и
с конструктором `RulesService`. Новая стадия добавляется во все три места.

| # | Стадия | Класс | Статус |
|---|--------|-------|--------|
| 1 | Нормализация орфографии | `ArabicNormalizer` | базовые случаи |
| 2 | Разбор в поток сегментов | `ArabicParser` | готово |
| 3 | Разметка пауз (вакф) | `WaqfRule` | готово |
| 4 | Хамзат аль-васль | `WaslRule` | готово |
| 5 | Лям артикля | `ArticleRule` | готово |
| 6 | Идгам однородных и близких согласных | `AssimilationRule` | готово |
| 7 | Нун сакина, танвин, мим сакина | `NasalRule` | готово |
| 8 | Тафхим и таркик | `EmphasisRule` | готово |
| 9 | Длительность мадда | `MaddRule` | готово |
| 10 | Кальканя | `QalqalahRule` | готово |
| 11 | Рендеринг по профилю | `CyrillicRenderer` | готово |

---

### Стадия 1. Нормализация орфографии — `ArabicNormalizer`

- [x] NFC, снятие татвиля, схлопывание пробелов
- [x] Сведение вариантов сукуна (U+06E1 → U+0652)
- [x] Разворачивание малых восстановительных букв (ۥ ۦ) в долготу
- [x] Знаки вакфа переживают нормализацию (сделано в PR #3)
- [x] **Сохранение знака икляба (U+06E2)** — переживает нормализацию наравне
      со знаками вакфа, а разбор читает его как сукун: другой огласовки в `مِنۢ` нет
- [x] **Восстановление надстрочного алифа в имени Аллаха** — в современной
      орфографии его не пишут (`ٱللَّهُ`), а долготу в этом слове даёт только он.
      Без него слог `لَّ` остаётся кратким, стадия 8 не узнаёт лям имени,
      а стадия 9 принимает коренную `ه` за местоименную и даёт ей мадд силя
- [x] **Перевод строки переживает схлопывание пробелов**: промежуток с ним
      становится одним `\n`, без него — одним пробелом. Для правил это та же
      граница слов и, с B15, пауза (стадия 3). Нужен и письму: аяты, подставленные панелью сур (D3), стоят
      каждый на своей строке и в выводе
- [x] **Хамза на татвиле** (`ـَٔ`, `ـًٔ`, `ـِٔ`, `ـُٔ`) — та же буква `ء`, что и хамза
      на носителе, со своей огласовкой: `يَسْـَٔلُونَ` → «йасъалююн», как `يَسْأَلُونَ`
      (B13). Так усмани пишет хамзу без носителя внутри слова. Прежде татвиль
      снимался, а его огласовка и надстрочная хамза U+0654 прирастали к предыдущей
      букве: звук пропадал, в `شَيْـًٔا` — вместе с танвином. Татвиль без хамзы
      по-прежнему только соединительная черта. В корпусе такого написания пока нет

### Стадия 2. Разбор в поток сегментов — `ArabicParser`

- [x] Носители хамзы (أ إ ؤ ئ آ ٱ) → один согласный ء со своей огласовкой
- [x] ا و ي различаются как долгая гласная / дифтонг / согласный
- [x] Алиф максура со своей огласовкой — согласная `ي`, а не звучащая `ا`:
      `وَلِىَ` → «уалий», как `وَلِيَ` (B10). Долготой максура становится раньше —
      голая после фатхи или касры и под надстрочным алифом (B4, B6)
- [x] Танвин развёрнут в «краткая гласная + нун сакин»
- [x] Надстрочный алиф, маддах, немой кружок
- [x] Артикль в современной орфографии (الحمد, а не ٱلحمد) — `DetectImlaiWasl`
- [x] Васля в современной орфографии и вне артикля, и за приросшей буквой:
      `اهْدِنَا`, `وَالْفَتْحُ`, `فَاتَّقُوا` читаются как написанные через `ٱ` (B8).
      Признак — безгласный согласный за алифом: сукун, шадда или лям артикля.
      До алифа в слове — только приставки `و ف ب ك ل` с краткой гласной, иначе
      алиф внутри слова и значит долготу. Решается на кластерах, до сборки долгот:
      за приставкой алиф иначе сразу ушёл бы в долготу её фатхи
- [x] Алиф аль-фарика после глагольного «ـوا» не даёт звука

### Стадия 3. Разметка пауз (вакф) — `WaqfRule`

- [x] Знаки вакфа доходят до модели сегментов (`WaqfMark`, `Segment.Waqf`)
- [x] Политика остановки по типам знаков задокументирована в `WaqfRule`:
      пауза на ۘ, ۗ и ۚ, на ۖ, ۙ, ۛ, ۜ чтение слитное (B15, решено владельцем 2026-11-18)
- [x] Перевод строки — пауза, как номер аята: последнее слово строки в форме
      паузы, следующая строка начинается новым высказыванием (B15)
- [x] Снятие конечной краткой огласовки, исходная сохраняется в `OriginalVowel`
- [x] Танвин на паузе: `ً` → долгая ā (мадд ивад, 2 хараката); `ٌ` и `ٍ` снимаются
      вместе с нун-сегментом
- [x] Та-марбута → /h/ на паузе (вакф снимает огласовку, рендерер берёт `"ة|waqf"`)
- [x] `WaslRule` начинает новое высказывание после паузы, а не только после номера аята
- [x] `EmphasisRule` держит ر мягкой по `OriginalVowel`, которую вакф снял (`ٱلْفَجْرِ`)
- [x] Мадд арид лис-сукун в `MaddRule`

### Стадия 4. Хамзат аль-васль — `WaslRule`

- [x] Озвучивание в начале высказывания, немота в соединении
- [x] Граница слова перед слитой васлей рендерится дефисом
- [x] Выбор огласовки: артикль → фатха, семь имён → касра, глагол → третья буква
- [x] Исключения на «привнесённую» дамму (ٱمْشُوا, ٱقْضُوا, ٱبْنُوا…)
- [x] Вспомогательная касра, когда предыдущее слово кончается сукуном

### Стадия 5. Лям артикля — `ArticleRule`

- [x] Лунный лям: сукун, дефис после
- [x] Солнечный лям: идгам — лям *становится* солнечной буквой (два сегмента)
- [x] Дефис между копиями согласного, ровно один при слиянии по васле
- [x] `ٱلَّذِينَ` — шадда прямо на ляме
- [x] **Дефис у солнечного ляма — только в начале высказывания**: `ٱلرَّحْمَٰنِ`
      → «ар-рохIмаан», `ٱللَّهُ ٱلصَّمَدُ` → «ал-лааhу-сIсIомад». В соединении
      его роль исполняет шов слова: «малики-ннаас», «бисми-лляяhи», «hууа-ллааhу»
- [x] **Приросшая буква перед артиклем** (`وَ`, `فَ`, `بِ`) — шва нет и в письме,
      и дефиса не остаётся вовсе: `وَٱلنَّاسِ` → «уаннаас». Лунный лям в той же
      позиции свой дефис сохраняет: `وَٱلْفَتْحُ` → «уаль-фатхI»

Поводов к дефису два: граница артикля с его именем («аль-хIамд») и шов слова —
пробел, съеденный слиянием, который рендерится дефисом вместо пробела
(«бисми-лляяhи»). Лунный лям свой дефис получает всегда: он звучит отдельным
закрытым слогом, и со швом слова его дефис не спорит — между ними стоит
написанное «ль», и дефисов выходит два, «робби-ль-'аалямиин». Солнечный лям
стал копией следующей буквы, между двумя дефисами не осталось ничего, и ставится
один: «малики-ннаас», а не «малики-н-наас» — иначе один звук вышел бы тремя
кусками. Уступает при этом граница артикля, а не шов, и отсюда
«ал-лааhу-сIсIомад» (112:2) против «hууа-ллааhу» (112:1): слово одно и то же,
но в первом случае артикль начинает высказывание, шва перед ним нет — и дефис
достаётся границе артикля.

Когда шва нет и в письме — перед артиклем приросшая буква, — у солнечного ляма
не остаётся ни одного дефиса, хотя спорить тут уже не с чем: «уаннаас».
Принято при сверке корпуса (A2). Дефис появляется у солнечного артикля только
там, где ему есть на что опереться — на начало высказывания или на шов слова;
опереться не на что — вывод обходится удвоением, которого без идгама не было бы.

### Стадия 6. Идгам однородных и близких согласных — `AssimilationRule`

Идгам мутаджанисайн и мутакарибайн: безгласный согласный сливается со следующим,
однородным ему или близким по месту образования. Признак берётся из письма, а не
из списка пар: мусхаф сам отмечает такое слияние шаддой на следующей букве при
безгласной предыдущей. Список пар был бы хуже — он решал бы за мусхаф там, где
мусхаф уже решил, и расходился бы с ним на спорных парах.

Стоит после артикля: шадда солнечной буквы к этому моменту уже снята и за отметку
нового слияния не сойдёт. И до стадии 7: носовые эта стадия не трогает вовсе —
слияние у них лишь одна возможность из четырёх, а по одной только шадде отличить
от него изхар, ихфу и икляб нечем.

- [x] Слияние по шадде на следующей букве при безгласной предыдущей:
      `عَبَدتُّمْ` → «'абаттум», `قَالَت طَّآئِفَةٌ` → «qоолятI-тIоооъифаh»,
      `يَلْهَث ذَّٰلِكَ` → «йальhазъ-зъаалик»
- [x] Внутри слова слияние не запрещено — в отличие от нун сакины с её изхаром
      мутлак. В `عَبَدتُّمْ` сливаются соседние буквы одного слова, и границы,
      через которую можно было бы не заглядывать, между ними нет
- [x] Без шадды слияния нет: идгам накыс в `بَسَطتَ` мусхаф не отмечает — ط
      сохраняет свой итбак, и полного слияния там не происходит
- [x] Механизм слияния общий с лямом артикля и нуном и вынесен в `Idgham`:
      первая буква становится второй, удвоение выражено двумя сегментами,
      шадда второй половины снимается, шов на стыке слов рендерится дефисом
- [x] Нун и мим сакина стадия не трогает — они целиком за стадией 7

### Стадия 7. Нун сакина, танвин, мим сакина — `NasalRule`

Стоит между 6 и 8: до артикля неизвестно, какой согласный окажется следующим
(у солнечного ляма это уже не `ل`), а эмфаза обязана считать уже слитый текст.
Отдать слияние носовых стадии 6 нельзя: шадда отмечает его и там, но изхар, ихфа
и икляб по ней от идгама неотличимы, а различает их следующая буква.
Танвин приходит сюда развёрнутым в нун-сегмент, поэтому отдельных правил для
него нет — это тот же нун сакина.

- [x] Изхар халькы (ء ه ع ح غ خ)
- [x] Идгам с гунной (ي ن م و) и без гунны (ل ر). Внутри слова идгама нет —
      изхар мутлак (`دُنْيَا`, `صِنْوَان`)
- [x] Икляб (ب) — нун переходит в мим, слова при этом не сливаются
- [x] Ихфа (остальные 15 букв)
- [x] Мим сакина: идгам мисляйн, ихфа шафави, изхар шафави
- [x] Гунна доходит до вывода, графема — из профиля (`"ن|ghunna"`)
- [x] Перепроверить стадию 8: идгам создаёт и разрушает условия эмфазы.
      Обе половины удвоения получают одно решение, и принимает его та, что несёт
      огласовку: `مِن رَّبِّهِمْ` → «мир-роббиhим», `قَالَ ٱللَّٰهُ` → «qооля-ллааh»

### Стадия 8. Тафхим и таркик — `EmphasisRule`

- [x] Буквы истиля (خ ص ض ط ظ غ ق) всегда твёрдые
- [x] ر по своей огласовке; безгласная — по предыдущей; исключение на истиля далее в слове
- [x] ر на паузе — по огласовке, которую сняла стадия вакфа
- [x] ل твёрдая только в имени Аллаха и только после фатхи или даммы
- [x] Твёрдость окрашивает **только свою** гласную. Назад она не красит:
      безгласный твёрдый согласный гласную перед собой не трогает — `نَصْرُ` →
      «насIру», `بَر` → «бар», `ٱلْفَلَقِ` → «аль-фаляq». Решено владельцем при
      сверке корпуса (A2), прежнее окрашивание назад снято багом B5. Твёрдость
      самих `ر` и ляма имени Аллаха это не меняет: они решают по огласовке,
      а не по тому, красят ли соседнюю гласную
- [x] ر после касры хамзат аль-васль (`رَبِّ ٱرْحَمْ`) — твёрдая: эта
      касра привнесена и при соединении исчезает вовсе

### Стадия 9. Длительность мадда — `MaddRule`

- [x] Мадд лязим (6) — долгая гласная перед удвоенным согласным
- [x] Мадд муттасиль и мунфасиль (4) — долгая гласная перед хамзой
- [x] Мадд арид лис-сукун (2) и мадд ивад (2) — на паузе. Счёт мадда арид выбран
      владельцем коротким (B11): дозволены 2, 4 и 6, и при двух он совпадает
      с естественным маддом — своей ветки правилу не нужно, помету берёт мадд лин
- [x] Мадд бадаль — долгая гласная после хамзы тянется два хараката:
      отдельной ветки не требует, требует лишь не удлинять её сверх двух
- [x] Мадд лин (دَيْن, خَوْف) — только на паузе и только на самом глайде;
      счёт тот же, что у мадда арид
- [x] Мадд силя (местоименная ه) — сугра в 2 хараката, кубра в 4 перед хамзой.
      Исключения: `يَرْضَهُ لَكُمْ` читается без сили, `فِيهِ مُهَانًا` — с силёй
- [x] **Силя на паузе снимается** (B14): за паузой второго движения голоса нет,
      и ه читается с сукуном — `بِهِۦ` → «биh», `أَخْلَدَهُۥ` → «ахълядаh», как
      с написанным сукуном. Снимает её стадия 3 вместе с конечной гласной: долготу
      ه дописала малая ۥ или ۦ, и обычное снятие краткой гласной её не трогало.
      Решено владельцем 2026-11-04
- [x] **Снятие долготы перед безгласным согласным соседнего слова**: `فِى ٱلْعُقَدِ` →
      «фи-ль-'уqод», а не «фии-». Хамзат аль-васль в соединении выпадает, за буквой
      мадда встаёт безгласный согласный, и из двух безгласностей подряд уступает
      долгота — التقاء الساكنين (B9, им же закрыт третий пункт B3). Только через
      границу слова: внутри слова та же встреча — мадд лязим. Безгласной считается
      и первая половина удвоения (`فِى ٱلنَّاسِ` → «фи-ннаас», `قَالُوا۟ ٱتَّخَذَ` →
      «qоолу-тт…»), но не сукун от паузы: за паузой ничего не следует, и стыка
      двух безгласных там нет. Пара к вспомогательной касре стадии 4: там слово
      кончается сукуном, уступать нечему, и стык размыкается гласной

### Стадия 10. Кальканя — `QalqalahRule`

Стоит последней среди правил: безгласность, от которой кальканя зависит, — итог
всех предыдущих стадий. Огласовку снимает вакф, первую половину удвоения создаёт
идгам, немой букву делает васля.

- [x] Буквы кальканя (ق ط ب ج د) в безгласной позиции. Первая половина удвоения
      отзвука не даёт: при идгаме буква не размыкается, а переходит в следующую
- [x] Усиленная кальканя на паузе. Признак — не снятая вакфом огласовка (как
      у мадда арид), а отсутствие следующего звучащего согласного: لَمْ يُولَدْ
      кончается написанным сукуном, и снимать вакфу там нечего
- [x] Графема из профиля: ключи `"ب|qalqalah"` и `"ب|qalqalah-strong"`, второй
      откатывается к первому. В `Standard` отзвук осознанно пуст — см. открытые
      решения ниже

### Стадия 11. Рендеринг по профилю — `CyrillicRenderer`

- [x] Поиск «вариант → базовый ключ → пусто». «Пусто» у базового ключа не молчит:
      рендерер отдаёт такие ключи списком, и сервис считает их ошибкой профиля,
      а не немой графемой (C2). Осознанно немая графема пишется в профиле
      пустой строкой
- [x] Варианты `heavy`, `soft`, `sukun`, `waqf`, `initial`
- [x] Долгота → повтор графемы (2→×2, 4→×3, 5→×4, 6→×5)
- [x] **Шкала долготы сдвинута, и каждый счёт виден** (G7, решено владельцем
      2026-11-12). Прежде шкала была 2→×2, 4→×3, 6→×4, и 5 у муттасиля
      и мунфасиля писалось как 4: выбор «5» на странице не менял текста. Теперь
      `جَآءَ` в пять счётов — «джаааа», в четыре — «джааа», а мадд лязим во всём
      корпусе на графему длиннее: «уаля-дIдIоооооллиин» (1:7). Число графем —
      не число харакатов: шкала упорядочивает долготы и различает каждый выбор,
      а не считает харакаты буквами. Это сказано в подсказке у счёта в «Неполном
      таджвиде» и в README; тест `PageHintTests` держит шкалу подсказки равной
      шкале рендерера, `ReadingTiersTests` — запись 4 и 5. `Latin` пишет долготу
      тем же повтором и сдвинулся вместе со `Standard`
- [x] Дефис не удваивается на стыке васли и артикля
- [x] Хамза между гласными пишется разделителем, а не своей графемой:
      `уа-иййаака`, `джааа-а`, `уаро-айта`. Решено при сверке корпуса (A2),
      заведено багом B7. После этого дефис значит две разные вещи — слияние
      (`-ль-`) и разрыв между гласными; различать их на письме не требуется,
      но правило, которое их ставит, должно знать, какое из двух ставит.
      Разводит их ключ: дефис слияния — структура, его ставит конвейер
      (`HyphenAfter`, шов слова), а разрыв — графема варианта `"ء|hiatus"`,
      и профиль вправе записать его иначе. Между гласными — значит огласована
      и сама хамза, и последняя звучащая буква перед ней в том же слове;
      начальная хамза остаётся за `"ء|initial"`. `Latin` задаёт вариант той же
      `ʾ`: разделитель нужен кириллице из-за диграфов, латинице — нет
- [x] **Хамза вне позиции между гласными: дефис внутри слова, ничего на конце**
      (G2, решено владельцем 2026-11-12). Прежде `Standard` писал её «ъ», и «з +
      хамза» было не отличить от `ذ` «зъ»: `جُزْءٌ` → «джузъ». Теперь внутри слова
      хамза — разрыв, и базовый ключ `"ء"` пишет его дефисом: «йу-минуун»,
      «аль-qур-аан», «кулли щай-ин qодиир». Безгласная хамза, за которой в слове
      больше ничего не звучит (немые буквы слова не продолжают), — вариант
      `"ء|final"`, и `Standard` её не пишет: «джуз», «ас-самааа», «иqро бисм».
      Каждая позиция — свой ключ (`initial`, `hiatus`, базовый, `final`), правка
      одного не трогает прочие; свой профиль без `"ء|final"` (скопированный до G2)
      откатывается к базовому `"ء"` и пишет хамзу, как писал, — оба условия
      владельца закреплены тестами `HamzaProfileKeysTests`. `Latin` варианта
      не задаёт: `ʾ` пишется и на конце: «ǧuzʾ», «as-samaaaʾ». Спор `ذ` с хамзой снят,
      и `Standard` больше не спорит ни одной графемой (E5)
- [x] **Хамза после артикля пишется как начальная** — то есть никак в `Standard`:
      `ٱلْأَرْضِ` → «аль-ардI», как `أَرْضٌ` → «ардI»; `ٱلْءَاخِرَةِ` → «аль-аахъироh» (G4).
      Артикль не делает начальную хамзу имени срединной. Ключ тот же,
      `"ء|initial"`: профиль, который пишет приступ, получит его в обоих местах.
      Имя за артиклем рендерер узнаёт по дефису лунного ляма — его ставит только
      стадия 5, а перед хамзой лям всегда лунный. Решено владельцем 2026-11-04
- [x] Граница слов, пришедшаяся на перевод строки, пишется переводом строки,
      а не пробелом (D3). С B15 перевод строки — пауза, и шва через него нет:
      `هُوَ` и `ٱللَّهُ` на разных строках дают «hуу» и «ал-лааhу» на двух строках.
      Пустые строки не сохраняются
- [x] Мягкий лям пишется мягко перед каждой гласной, которую кириллица
      различает: «ль», «ля», «лю» (`حَبْلٌ` → «хIаблюм», B12). Стадия 8 помечала
      мягкой любую гласную ляма и раньше, но в `Standard` не было `"ُ|soft"`,
      и перед даммой рендерер откатывался к твёрдому «у». Долгая — «люю»,
      как «ляя»

---

## Открытые решения

Не «чинить» молча — сначала согласовать. Это выбор системы записи, а не баг.
Каждое решение ведётся issue эпика G с меткой `BA`; там же примеры вывода
и позиция владельца.

Открытых решений сейчас нет.

Решено владельцем 2026-11-12:

- [G2](https://github.com/shoraLBRT/Transliterator/issues/92) — **хамза вне позиции между гласными: дефис внутри слова, ничего
  на конце** — вместо `ъ`, который спорил с диграфами (`зъ`/`хъ`/`гъ`: «з + хамза»
  неотличимо от `ذ`): «йу-минуун», «аль-qур-аан», «джуз», «ас-самааа». Конец
  слова — новый вариант `"ء|final"`. Условие владельца: свой профиль должен
  по-прежнему менять запись хамзы, и правка одного варианта не ломает другие —
  каждая позиция (`"ء|initial"`, `"ء|hiatus"`, внутри слова — базовый `"ء"`,
  `"ء|final"`) задаётся в профиле отдельно, а свой профиль без нового варианта
  откатывается к базовому ключу, как и раньше: кто писал хамзу по-своему, так
  и пишет. Сделано, см. стадию 11. Ожидания корпуса от решения не изменились —
  в корпусе хамза стоит только в начале слова и между гласными; изменённые
  ожидания точечных тестов владелец сверяет по PR
- [G7](https://github.com/shoraLBRT/Transliterator/issues/116) — **шкала долготы сдвигается, чтобы каждый счёт был виден:**
  2 → две графемы, 4 → три, 5 → четыре, 6 → пять. Меняется и запись мадда лязим
  во всём корпусе. Число букв на письме — не число харакатов: шкала только
  упорядочивает долготы. Это нужно объяснить там, где счёт выбирают (подсказка
  в «Неполном таджвиде») и в README, — иначе тот, кто учит таджвид, решит, что
  запись ошибается. Букву на каждый харакат (6 → шесть) владелец обсуждал;
  выбран сдвиг шкалы. Сделано, см. стадию 11; изменённые ожидания корпуса
  (мадд лязим в 1:7) и каталога владелец сверяет по PR
- I4, I5, E6, D6, D7 — решения о странице, не о системе записи; записаны в issues
  и в таблице итерации 2

Решено владельцем 2026-11-04:

- [G3](https://github.com/shoraLBRT/Transliterator/issues/93) — **`ع` остаётся `'`**: владельца устраивает, иначе — в своём профиле
- [G4](https://github.com/shoraLBRT/Transliterator/issues/94) — **хамза в начале слова не пишется — и после артикля тоже**:
  `ٱلْأَرْضِ` → «аль-ардI». Сделано, см. стадию 11
- [G5](https://github.com/shoraLBRT/Transliterator/issues/95) — **`ق` остаётся `q`**; «къ» — в своём профиле. О графеме, которая
  спорит с другой, профиль предупреждает с примером (E5, сделано): «къ» у `ق`
  складывается из «к» у `ك` и «ъ» у `ء`, и страница говорит об этом у буквы,
  а профиль всё равно сохраняется
- [G1](https://github.com/shoraLBRT/Transliterator/issues/88), [G6](https://github.com/shoraLBRT/Transliterator/issues/96) — **гунне и отзвуку кальканя своя графема не нужна**:
  всего, что звучит, на письме не обозначить. Профиль вправе задать своё

## Не в конвейере

- [x] `TransliterationService`: `UpdateRuleAsync`, `GetAvailableProfilesAsync`,
      `GetRulesAsync`. `GetRulesAsync` отдаёт копию словаря: репозиторий раздаёт
      профили из кеша, и правка возвращённого молча меняла бы профиль всем.
      `UpdateRuleAsync` не проверяет ключ по алфавиту — вариантов правил больше,
      чем букв, и их набор задаёт профиль, а не код. Заодно починено то, без чего
      это не работает: `GetAllProfilesAsync` искала профиль по имени файла вместе
      с расширением (`Standard.json.json`), а запись профиля экранировала арабицу
      и кириллицу в `\uXXXX`
- [x] Профили, кроме `Standard`: `Latin` — латиница с академической диакритикой
      (`ṯ ǧ ḥ ḫ ḏ š ṣ ḍ ṭ ẓ ġ`, `ʾ` и `ʿ`). Ни одно правило про латиницу не знает:
      конвейер тот же, различается только письмо. Варианты, которых этой системе
      записи не нужно (`ل|sukun`, огласовка после мягкого ляма), не заданы вовсе —
      рендерер откатывается к базовому ключу, и это проверено тестом. Долгота
      по-прежнему повтором графемы, а не макроном: макрон не различает три
      длительности мадда. Гунна и отзвук кальканя не пишутся — как в `Standard`,
      это открытое решение проекта, а не выбор профиля. Заодно снят хардкод
      `Standard.json` в обоих csproj: профили перечисляются маской, иначе новый
      файл не доезжает до выходной папки. Инварианты профиля закреплены тестами —
      базовый ключ на месте у каждого, имя варианта известно рендереру
- [x] **Обязательный набор ключей профиля** ([F6](https://github.com/shoraLBRT/Transliterator/issues/87)), утверждён
      владельцем 2026-11-09. Обязательны 30 букв, три огласовки и `ة|waqf`:
      без базового ключа звук пропадает, без `ة|waqf` та-марбута на паузе
      читается /t/ вместо /h/. Цифры необязательны — пишутся как есть; прочие
      варианты необязательны, у каждого записано, что будет без него
      (`RequiredProfileKeys`). Встроенные профили проверяются по набору, а не
      друг по другу; копия и импорт без обязательного ключа отвергаются
      с перечнем недостающих
- [x] Два предупреждения CS0168 в `JsonProfileRepository`: пойманное исключение
      теперь доходит до лога целиком, а не только его текст
- [x] **README сведены с кодом** (`README.md`, `README.ru.md`). Оба показывали
      профиль как хардкодированный C#-словарь с ключами носителей хамзы, танвина
      и надстрочного алифа: такого профиля нет с PR #1, а эти знаки до профиля
      и не доходят — их разбирают стадии 1 и 2. Теперь описаны формат ключа
      `"буква|вариант"`, выбор профиля вторым аргументом CLI и все десять стадий,
      а примеры вывода получены прогоном CLI, а не набраны руками
