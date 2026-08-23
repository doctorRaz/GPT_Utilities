@echo off
chcp 65001 >nul

set "ARCHIVE=d:\@Developers\В работе\GPT_export\dfd92c24f5cde964830cfc5c1a28d77b1b45587294bbecc983a8fe9faf89b81a-2026-08-22-09-43-48-e90c0a83ab054f6ea422a89c893cc19d.zip"
set "OUTPUT=d:\@Developers\В работе\Reminder\Convoviz"

if not exist "%OUTPUT%" mkdir "%OUTPUT%"

convoviz --input "%ARCHIVE%" --output "%OUTPUT%" --outputs markdown

if errorlevel 1 (
    echo.
    echo ERROR: Convoviz завершился с ошибкой.
    pause
    exit /b 1
)

echo.
echo Готово.
echo Результат: "%OUTPUT%"
pause