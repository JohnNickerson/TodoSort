rem A path like one of the following four will lead to the Visual Studio executable, which can perform the build from the command line.
rem C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE
rem C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE
rem C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE
rem C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE
rem devenv /useenv /build "Release" "AcomProfileManager.sln"

Rem TodoSort deploy script for integration.
Rem Assumes Release build has already been done.

rem 1. Ensure target directories exist.
md %DROPBOX%\Toolkit\TodoSort\Integration\GUI

rem 2. Empty out target directories.
del "%DROPBOX%\Toolkit\TodoSort\Integration\GUI\*.*"
del "%DROPBOX%\Toolkit\TodoSort\Integration\*.*"

rem 3. Move Release binaries to target directories.
move /y CLI\bin\Release\*.* %DROPBOX%\Toolkit\TodoSort\Integration
move /y WpfGui\bin\Release\*.* %DROPBOX%\Toolkit\TodoSort\Integration\GUI

rem 4. Pause to show success or failure.
Pause