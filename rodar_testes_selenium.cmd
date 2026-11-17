@echo off
chcp 65001 >nul
cd /d "%~dp0"
set SELENIUM_PAUSA_MS=2000
echo === Testes funcionais com Selenium (CT11 a CT20) ===
dotnet test TrabalhoPOO2.SeleniumTests --logger "console;verbosity=normal"
pause
