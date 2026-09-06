---
title: Исправление YAML теста
aliases: []
tags:
- chatgptexporter
- cgr
- yandex
create_time:
  date_time: 2026-09-04T13:00:42.2130000
  utc_date_time: 2026-09-04T13:00:42.2130000Z
  local_date_time: 2026-09-04T16:00:42.2130000+03:00
  date: 2026-09-04T00:00:00.0000000
  day: 4
  day_of_week: Friday
  day_of_year: 247
  hour: 13
  millisecond: 213
  minute: 0
  month: 9
  offset: 00:00:00
  second: 42
  ticks: 639241236422130000
  utc_ticks: 639241236422130000
  time_of_day: 13:00:42.2130000
  year: 2026
update_time:
  date_time: 2026-09-05T05:57:35.1770000
  utc_date_time: 2026-09-05T05:57:35.1770000Z
  local_date_time: 2026-09-05T08:57:35.1770000+03:00
  date: 2026-09-05T00:00:00.0000000
  day: 5
  day_of_week: Saturday
  day_of_year: 248
  hour: 5
  millisecond: 177
  minute: 57
  month: 9
  offset: 00:00:00
  second: 35
  ticks: 639241846551770000
  utc_ticks: 639241846551770000
  time_of_day: 05:57:35.1770000
  year: 2026
date_export: 2026-09-05T16-22-30
chat_link: https://chatgpt.com/c/6a9ac0b2-7a54-83eb-8e30-3148b78ad02d
conversation_i_d: 6a9ac0b2-7a54-83eb-8e30-3148b78ad02d
---
# Исправление YAML теста

#### You:
<time datetime="2026-09-04T13:00:41.859Z" title="04.09.2026, 16:00:41">16:00</time>

Archivist yaml парсер падает на 
title: "Тест < > : " / \ | ? * — все запрещённые символы"

#### ChatGPT:
<time datetime="2026-09-04T13:00:47.019Z" title="04.09.2026, 16:00:47">16:00</time>

Да. Здесь проблема именно в тестовом YAML:

