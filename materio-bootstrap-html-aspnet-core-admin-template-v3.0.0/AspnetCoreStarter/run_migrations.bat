@echo off
echo Running: dotnet ef migrations add InitialCreate
dotnet ef migrations add InitialCreate > migration_log.txt 2>&1
echo Running: dotnet ef database update
dotnet ef database update >> migration_log.txt 2>&1
echo Done. Check migration_log.txt for results.
