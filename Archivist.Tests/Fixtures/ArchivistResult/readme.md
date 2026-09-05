# Фикстуры Archivist

В этом каталоге находятся реальные Markdown-файлы, полученные после 
экспорта ChatGPT и обработанные Archivist.

## Полученный YAML

не соответствует ожидаемому

`title` корректно обрамлен одинарными кавычками
`create_time`, `update_time` развалилось отображение... серилизовать как текст?

`aliases` не было в исходном YAML, пишем только то что реально прочитали и добавляем результат парсинга conversation_id
`conversation_i_d` неверное имя поля, должно быть `conversation_id`, сохранять как текст, должно быть обрамление кавычками 
`conversation_id: "6a9aa4d8-d72c-83ed-9763-ebab4c8a85e8"`


```
---
title: 'Тест < > : " / \ | ? * — все запрещённые символы'
aliases: []
tags:
- chatgptexporter
- cgr
- yandex
create_time:
  date_time: 2026-09-04T11:01:12.9980000
  utc_date_time: 2026-09-04T11:01:12.9980000Z
  local_date_time: 2026-09-04T14:01:12.9980000+03:00
  date: 2026-09-04T00:00:00.0000000
  day: 4
  day_of_week: Friday
  day_of_year: 247
  hour: 11
  millisecond: 998
  minute: 1
  month: 9
  offset: 00:00:00
  second: 12
  ticks: 639241164729980000
  utc_ticks: 639241164729980000
  time_of_day: 11:01:12.9980000
  year: 2026
update_time:
  date_time: 2026-09-04T11:02:02.1000000
  utc_date_time: 2026-09-04T11:02:02.1000000Z
  local_date_time: 2026-09-04T14:02:02.1000000+03:00
  date: 2026-09-04T00:00:00.0000000
  day: 4
  day_of_week: Friday
  day_of_year: 247
  hour: 11
  millisecond: 100
  minute: 2
  month: 9
  offset: 00:00:00
  second: 2
  ticks: 639241165221000000
  utc_ticks: 639241165221000000
  time_of_day: 11:02:02.1000000
  year: 2026
date_export: 2026-09-05T16-22-30
chat_link: https://chatgpt.com/c/6a9aa4d8-d72c-83ed-9763-ebab4c8a85e8
conversation_i_d: 6a9aa4d8-d72c-83ed-9763-ebab4c8a85e8
---
```


все то же что и выше, но `title` не обрамлен никак, пока оставим такое поведение
```
---
title: Установка DeepSeek R1
aliases: []
tags:
- chatgptexporter
- cgr
- yandex
create_time:
  date_time: 2026-09-02T09:34:25.7940000
  utc_date_time: 2026-09-02T09:34:25.7940000Z
  local_date_time: 2026-09-02T12:34:25.7940000+03:00
  date: 2026-09-02T00:00:00.0000000
  day: 2
  day_of_week: Wednesday
  day_of_year: 245
  hour: 9
  millisecond: 794
  minute: 34
  month: 9
  offset: 00:00:00
  second: 25
  ticks: 639239384657940000
  utc_ticks: 639239384657940000
  time_of_day: 09:34:25.7940000
  year: 2026
update_time:
  date_time: 2026-09-02T10:39:52.9630000
  utc_date_time: 2026-09-02T10:39:52.9630000Z
  local_date_time: 2026-09-02T13:39:52.9630000+03:00
  date: 2026-09-02T00:00:00.0000000
  day: 2
  day_of_week: Wednesday
  day_of_year: 245
  hour: 10
  millisecond: 963
  minute: 39
  month: 9
  offset: 00:00:00
  second: 52
  ticks: 639239423929630000
  utc_ticks: 639239423929630000
  time_of_day: 10:39:52.9630000
  year: 2026
date_export: 2026-09-05T16-22-30
chat_link: https://chatgpt.com/c/6a97ed9f-893c-83eb-8f7e-ab8c4a803bcd
conversation_i_d: 6a97ed9f-893c-83eb-8f7e-ab8c4a803bcd
---
```

Развалилось поределение путей источников
приоритет, чинить сохранение `create_time`, `update_time`, 

исправить название `conversation_i_d`->`conversation_id`, как текст