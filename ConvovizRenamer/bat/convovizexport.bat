@echo off
chcp 65001 >nul
setlocal

set "ARCHIVE_DIR=d:\@Developers\В работе\GPT_export"
set "OUTPUT=d:\@Developers\В работе\Reminder\Convoviz"
set "CONFIG=d:\setup\Convoviz\config.toml"

if not exist "%OUTPUT%" mkdir "%OUTPUT%"

rem Ищем последний изменённый ZIP в каталоге экспорта
for /f "delims=" %%F in ('dir /b /a-d /o-d "%ARCHIVE_DIR%\*.zip" 2^>nul') do (
    set "ARCHIVE=%ARCHIVE_DIR%\%%F"
    goto :archive_found
)

echo.
echo ERROR: ZIP-архивы не найдены в:
echo "%ARCHIVE_DIR%"
pause
exit /b 1

:archive_found

echo Архив: "%ARCHIVE%"
echo Выход: "%OUTPUT%"
echo.

convoviz --config "%CONFIG%" --input "%ARCHIVE%" --output "%OUTPUT%" --outputs markdown

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

endlocal