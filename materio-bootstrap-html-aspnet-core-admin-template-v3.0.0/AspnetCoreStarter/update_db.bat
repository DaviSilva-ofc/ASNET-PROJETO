@echo off
echo Running EF Core migrations...
dotnet tool update --global dotnet-ef
dotnet ef migrations add AddGroupingColumn
dotnet ef database update
echo Done.
