# VeronaShop

This project uses EF Core with SQL Server. Use the dotnet-ef tool to create migrations:

```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate -p VeronaShop -s VeronaShop
dotnet ef database update -p VeronaShop -s VeronaShop
```
