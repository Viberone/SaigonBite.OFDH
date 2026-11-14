# SaigonBite OFDH

ASP.NET Core MVC (.NET 10), EF Core 10, SQL Server LocalDB.

## Requirements
- Visual Studio 2026 with workloads: "ASP.NET and web development" and "Data storage and processing"
- .NET 10 SDK (check with `dotnet --version`)

## Setup
1. Clone the repository and open `SaigonBite.OFDH.sln`.
2. In Visual Studio open View > SQL Server Object Explorer, right-click
   `(localdb)\MSSQLLocalDB` > New Query.
3. Open `Database/create.sql`, paste it, click Execute.
4. Build (Ctrl+Shift+B), then press F5.
5. Open `/Voucher`.

## Troubleshooting
- Package errors: right-click the solution > Restore NuGet Packages.
- Database not found: repeat step 3.