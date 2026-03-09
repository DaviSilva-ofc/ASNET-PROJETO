@echo off
echo Dropping database...
dotnet ef database drop --force

echo.
echo Deleting Migrations folder...
if exist Migrations rd /s /q Migrations

echo.
echo Creating InitialCreate migration...
dotnet ef migrations add InitialCreate

echo.
echo Updating database...
dotnet ef database update

echo.
echo Database reset successfully!
