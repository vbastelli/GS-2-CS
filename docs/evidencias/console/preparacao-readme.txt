dotnet ef database update --project src/AguaNaRua.Api
Build started...
Build succeeded.
info: Microsoft.EntityFrameworkCore.Migrations[20411]
      Acquiring an exclusive lock for migration application. See https://aka.ms/efcore-docs-migrations-lock for more information if this takes too long.
Acquiring an exclusive lock for migration application. See https://aka.ms/efcore-docs-migrations-lock for more information if this takes too long.
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20261118180433_Inicial'.
Applying migration '20261118180433_Inicial'.
Done.
dotnet run --project src/AguaNaRua.Api --no-launch-profile -- --seed
info: Microsoft.EntityFrameworkCore.Migrations[20411]
      Acquiring an exclusive lock for migration application. See https://aka.ms/efcore-docs-migrations-lock for more information if this takes too long.
info: Microsoft.EntityFrameworkCore.Migrations[20405]
      No migrations were applied. The database is already up to date.
