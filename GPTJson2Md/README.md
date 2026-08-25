# GPTJson2Md


> [!IMPORTANT]
> **Archived — development discontinued**
>
> Development discontinued in August 2026.

**GPTJson2Md** — утилита для преобразования экспорта ChatGPT из JSON в Markdown.

Разработка проекта прекращена.\
На данный момент существуют более функциональные и активно развиваемые инструменты, поэтому продолжение разработки GPTJson2Md не имеет практического смысла.

## Recommended alternatives

Если тебе нужен инструмент для работы с экспортом ChatGPT, рекомендую посмотреть:

1. **[chatgpt-exporter](https://github.com/pionxzh/chatgpt-exporter)** — импорт on-lain и работа с экспортированными архивами ChatGPT и не только.
1. **[Convoviz](https://github.com/mohamed-chs/convoviz)** — конвертация и визуализация истории ChatGPT, в том числе экспорт в Markdown.
1. **[Nexus](https://github.com/Superkikim/nexus-ai-chat-importer)** — альтернативный инструмент для работы с экспортом ChatGPT. Адаптирован для Obsidian.
1. ... если найдутся другие интересные проекты, добавлю их в список.

Эти проекты лучше соответствуют современным требованиям и продолжают развиваться.

GPTJson2Md сохранён в репозитории для исторических целей. Последняя версия остаётся доступной, но новые функции, исправления и дальнейшая разработка не планируются.




---


## GPTJson2Md описание

Минималистичный консольный инструмент для преобразования JSON-экспорта чатов ChatGPT в Markdown без искажения текста и кода.

Проект ориентирован на:
- точность
- воспроизводимость
- сохранение оригинального содержимого
- архивирование и поиск истории диалогов

---

## Возможности

- Конвертация JSON → Markdown
- Корректный парсинг сообщений ChatGPT
- Сохранение кода и спецсимволов без подмены
- YAML-header с метаданными:
  - заголовок
  - ссылка на оригинальный чат
  - проект
  - дата создания 
  - дата обновления
  - количество сообщений
  - теги
- Имя файла: `YYYY-MM-DD_Название-чата.md`
- Дата создания файла = первое сообщение
- Дата изменения файла = последнее сообщение
- Без внешних зависимостей
- Быстрое выполнение
- Интерактивный режим при отсутствии аргументов

---

## Пример YAML-header

```yaml
---
title: "API nanocad загрузчик"
project: "General"
url: "https://chat.openai.com/c/697790ce-d460-8329-8812-2dfdca7dd793"
created: "2026-01-25 12:41:10"
updated: "2026-01-25 13:02:44"
message_count: 42
tags:
  - "API"
  - "nanocad"
  - "загрузчик"
---
````

* * *

Формат имени файла
------------------

```
YYYY-MM-DD_Название-чата.md
```

Пример:

```
2026-01-25_API-nanocad.md
```

* * *

Формат сообщений
----------------

```md
## 👤 User
Date: 2026-01-22 20:28:17

Текст сообщения

---
## 🤖 Assistant
Date: 2026-01-22 20:28:21

```csharp
Console.WriteLine("Hello");
````

---

## Использование

### Запуск

```bash
GPTJson2Md.exe <input.json> <output_folder>
````

Или с именованными параметрами:

```bash
GPTJson2Md.exe --input chat.json --out output_folder
```

Сокращённо:

```bash
GPTJson2Md.exe -i chat.json -o output_folder
```

* * *

### Интерактивный режим

Если аргументы не указаны, программа запрашивает пути вручную:

```text
Input JSON (ESC = exit):
Output folder (ESC = exit):
```

*   ESC — выход
*   Валидация путей перед запуском
*   При ошибке — повторный запрос

* * *

### Поведение

*   Вход: JSON-файл экспорта ChatGPT
*   Выход: папка с `.md` файлами
*   Если папка отсутствует — создаётся автоматически
*   Дата создания файла = первое сообщение
*   Дата изменения файла = последнее сообщение
*   Имя файла формируется по дате первого сообщения

* * *

### CLI Help

```text
GPTJson2Md — ChatGPT JSON → Markdown converter

Usage:
  GPTJso2nMd.exe <input.json> <output_folder>
  GPTJson2Md.exe --input file.json --out folder

Options:
  -i, --input     Input JSON path
  -o, --out       Output folder
  -h, --help      Show help

If args missing → interactive mode (ESC to exit)
```

* * *

### Пример запуска

```bash
GPTJson2Md.exe chats.json ./md
```

Результат:

*   Markdown-файлы в `./md`
*   YAML-header с метаданными
*   Исходный текст и код без искажений

* * *

Назначение
----------

*   Архивирование истории ChatGPT
*   Индексация технических обсуждений
*   Импорт в Obsidian / Logseq / Notion
*   Документирование разработки
*   Долгосрочное хранение инженерных диалогов

* * *
### Obsidian

<img width="1867" height="1605" alt="image" src="https://github.com/user-attachments/assets/742cc5de-4e75-414a-997a-c4d49a5838ac" />




