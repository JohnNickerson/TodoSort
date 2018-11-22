rem A path like one of the following four will lead to the Visual Studio executable, which can perform the build from the command line.
rem C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE
rem C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE
rem C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE
rem C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE
rem devenv /useenv /build "Release" "AcomProfileManager.sln"

md C:\Users\John\Dropbox\Toolkit\TodoSort\Integration\GUI
move /y CLI\bin\Release\*.* C:\Users\John\Dropbox\Toolkit\TodoSort\Integration
move /y WpfGui\bin\Release\*.* C:\Users\John\Dropbox\Toolkit\TodoSort\Integration\GUI
Pause