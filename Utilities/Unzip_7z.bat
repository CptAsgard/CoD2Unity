@echo off
for /F "delims=" %%I IN (' dir /b /s /a-d *.iwd ') DO (
    "%ProgramFiles(x86)%\7-Zip\7z.exe" x -y -o"%%~dpI" "%%I" 
)
pause