@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === Testes unitarios (CT01 a CT10) ===
dotnet test TrabalhoPOO2.Tests --logger "console;verbosity=normal"
pause
