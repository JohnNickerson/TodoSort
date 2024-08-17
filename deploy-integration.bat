Rem TodoSort build and deploy script for integration.
cd CLI
dotnet build --configuration Release
cd ..\WpfGui
dotnet build --configuration Release
cd ..

rem 1. Copy all in one.
robocopy "%OneDrive%\Projects\Code\TodoSort\CLI\bin\Release\net8.0" "%OneDrive%\Toolkit\TodoSort\Integration" /e /purge /v
robocopy "%OneDrive%\Projects\Code\TodoSort\WpfGui\bin\Release\net8.0-windows" "%OneDrive%\Toolkit\TodoSort\Integration\GUI" /e /purge /v
dir /s/b "%OneDrive%\Toolkit\TodoSort\Integration"

rem 2. Pause to show success or failure.
Pause