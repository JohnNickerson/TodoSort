rem A path like one of the following four will lead to the Visual Studio executable, which can perform the build from the command line.
rem C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE
rem C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE
rem C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE
rem C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE
rem devenv /useenv /build "Release" "AcomProfileManager.sln"

Rem TodoSort deploy script for integration.
Rem Assumes Release build has already been done.

rem 1. Copy all in one.
robocopy "%DROPBOX%\Projects\Code\TodoSort\CLI\bin\Release" "%DROPBOX%\Toolkit\TodoSort\Integration" /e /purge /v
robocopy "%DROPBOX%\Projects\Code\TodoSort\WpfGui\bin\Release" "%DROPBOX%\Toolkit\TodoSort\Integration\GUI" /e /purge /v
dir /s/b "%DROPBOX%\Toolkit\TodoSort\Integration"

rem 2. Pause to show success or failure.
Pause