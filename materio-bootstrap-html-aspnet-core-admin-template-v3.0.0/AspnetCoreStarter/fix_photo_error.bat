@echo off
echo Killing locking process...
taskkill /F /IM AspnetCoreStarter.exe /T

echo.
echo Adding migration MakeProfilePhotoPathNullable...
dotnet ef migrations add MakeProfilePhotoPathNullable

echo.
echo Updating database...
dotnet ef database update

echo Done.
