pushd "%~dp0"

if not exist "..\bin\" mkdir ..\bin

zig build-lib -dynamic lib_api.zig -O ReleaseFast -femit-bin=../bin/apollo.dll
if errorlevel 1 goto :fail

popd
exit /b 0

:fail

popd
exit /b 1