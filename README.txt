Dodanie bazy danych:

1. Instalacja pakietów 
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design

2. Instalacja narzêdzia do migracji
dotnet tool install --global dotnet-ef

3. Stworzenie bazy danych lokalnie
4. Konfiguracja po³¹czenia do bazy jest w pliku AppDbContext.cs
4.5 W appsettings.json trzeba zmienic konfiguracje na odpowiadajaca lokalnej bazie

5. Wygeneruj schemat bazy danych na podstawie modelu:
dotnet ef migrations add InitialCreate // Ttworzenie nowej migracji - trzeba puscic po kazdej zmianie w pliku AppDbContext
dotnet ef database update //Aktualizacja schematu na bazie 