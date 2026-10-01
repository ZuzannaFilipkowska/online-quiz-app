Dodanie bazy danych:

1. Instalacja pakietów 
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design

2. Instalacja narzędzia do migracji
dotnet tool install --global dotnet-ef

3. Stworzenie bazy danych lokalnie
4. Konfiguracja połšczenia do bazy jest w pliku AppDbContext.cs
5. W appsettings.json trzeba zmienić konfiguracje na odpowiadajaca lokalnej bazie

6. Wygeneruj schemat bazy danych na podstawie modelu:
dotnet ef migrations add InitialCreate // Tworzenie nowej migracji - trzeba wykonać po każdej zmianie w pliku AppDbContext
dotnet ef database update //Aktualizacja schematu na bazie 
