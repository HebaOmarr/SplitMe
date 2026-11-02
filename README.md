# SplitMate

Track shared money between friends: log who paid for whom, settle up, and see who owes whom.

ASP.NET Core MVC (.NET 10) · EF Core + SQL Server · Identity · Bootstrap

## Run locally

Needs SQL Server LocalDB (installed with Visual Studio).

```powershell
cd "src/SplitMate.Web"
dotnet run
```

Open http://localhost:5034, register, create a group, and share the join code (or join link) with friends.

The local database `SplitMate` is created automatically on LocalDB (`appsettings.json`).
Login keys are stored in the database too, so nobody gets logged out on restart or redeploy.

## Tests

```powershell
dotnet test
```

## Database changes

After changing a model:

```powershell
cd "src/SplitMate.Web"
dotnet ef migrations add <Name> -o Data/Migrations
```

Migrations are applied automatically when the app starts (locally and on the server).

## Deploy for free: MonsterASP.NET (Visual Studio)

The data lives in MonsterASP's free SQL Server database, not in the website folder, so deploys never delete it.

1. In the MonsterASP control panel, open **Databases** and create a free **MSSQL** database.
   Copy its server, database name, user and password.
2. Open `src/SplitMate.Web/appsettings.Production.json` and replace the `PASTE_...` values.
   This file is git-ignored so your password never ends up in git; Visual Studio still publishes it.
3. In the control panel, download the **Web Deploy publish profile** for your website.
4. In Visual Studio: right-click **SplitMate.Web** → **Publish** → **Import profile** → pick the downloaded file → **Publish**.
5. Open the site. Tables are created on the first request.

Redeploying is just step 4 again. Your data stays.

Backups: use the backup option for the database in the MonsterASP control panel.

## Alternative: Azure App Service (F1 free tier)

You need a SQL database too (Azure SQL free offer). Set the connection string in
*Settings → Environment variables → Connection strings* with the name `Default`, then publish from Visual Studio.

Note: Hostinger's free/shared plans can't run ASP.NET Core; only their paid VPS plans can.
