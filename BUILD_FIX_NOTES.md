# Build Fix Notes

Applied fixes:

1. Renamed the UI theme service from `ThemeService` to `UiThemeService` to avoid the ambiguous reference with `Radzen.ThemeService`.
2. Updated dependency injection in `CharityHealth.Web/Program.cs`.
3. Updated injection in `CharityHealth.Web/Shared/MainLayout.razor`.
4. Added `@using CharityHealth.Web.Pages` to `_Imports.razor` so `RedirectToLogin` is resolved.
5. Removed the duplicate `Microsoft.AspNetCore.Components.Authorization` import warning from `_Imports.razor`.

Run:

```bash
dotnet clean
dotnet restore
dotnet build
cd CharityHealth.Web
dotnet run
```
