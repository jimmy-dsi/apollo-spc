pushd "%~dp0"

zig build-exe cli_main.zig -femit-bin=../bin/apollo-spc-program.exe
if errorlevel 1 goto :fail

popd
exit /b 0

:fail

popd
exit /b 1