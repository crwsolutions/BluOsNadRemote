# Agent Guidelines for BluOS NAD Remote

## Project Overview
BluOS NAD Remote is an open source .NET MAUI remote control for BluOS-enabled wireless hi-res music systems (Bluesound, NAD Electronics, PSB, DALI, Monitor Audio, Cyrus Audio). In addition to standard BluOS control (player, presets, browse, queue), it features an Advanced tab that sends NAD Electronics Txxx ethernet (RS232/telnet) commands. It uses CommunityToolkit.Maui/MVVM, SourceDepend for constructor injection, and supports English and Dutch.

## Build, Lint, and Test Commands

### Build Commands
```bash
dotnet build                          # Build the solution
dotnet build -c Release               # Build in Release mode
```

### Test Commands
```bash
dotnet test src/BluOsNadRemote.Blu4Net.Tests   # xUnit v3 golden/unit tests for the BluOS API layer
```
- Tests are limited to the `BluOsNadRemote.Blu4Net` library (see the [Testing](#testing) section).
- Running the app end-to-end requires real BluOS hardware on the local network — the agent should not attempt to run the app; the developer handles manual validation.

### Linting
- Rely on .NET compiler warnings and the rules in `src/.editorconfig` (e.g., `CA1816`, `CA1822`, `CA1826`, `IDE0305` are downgraded/none)
- Ensure code compiles without warnings
- Use Visual Studio or Rider for IDE-level linting

## Code Style and Conventions

### XAML Compilation
- This project uses `<MauiXamlInflator>SourceGen</MauiXamlInflator>` together with `<MauiStrictXamlCompilation>true</MauiStrictXamlCompilation>`. Therefore `x:DataType` is mandatory for all bindings. Never generate bindings without `x:DataType`, and never fall back to runtime-only bindings.

### .NET and Language Version
- Target .NET 10 exclusively (SDK pinned via `global.json`, `rollForward: disable`)
- App targets: `net10.0-android`, `net10.0-ios`, `net10.0-windows10.0.19041.0` (Windows is added conditionally on Windows dev machines)
- Use modern C# features (primary constructors, records, collection expressions); the App project sets `<LangVersion>preview</LangVersion>` (required for MVVM toolkit warnings)
- Enable nullable reference types (`<Nullable>enable</Nullable>`)

### Imports and Namespace Organization
- Use explicit `using` statements in files; `GlobalUsings.cs` only contains global usings for MVVM components, `ObservableCollection`, `Debug`, and `CallerMemberName`
- Group imports: external packages first, then project namespaces
- Follow alphabetical order within groups
- Example:
  ```csharp
  using BluOsNadRemote.App.Extensions;
  using BluOsNadRemote.App.Services;
  using BluOsNadRemote.Blu4Net;
  using System.Diagnostics.CodeAnalysis;
  using System.Globalization;
  ```

### Naming Conventions
- **Classes/Interfaces**: PascalCase (e.g., `BluPlayerService`, `PlayerViewModel`)
- **Methods/Properties**: PascalCase (e.g., `ConnectAsync`, `IsConnected`)
- **Local variables/parameters**: camelCase (e.g., `endpoint`, `culture`)
- **Private fields**: camelCase with underscore prefix (e.g., `_bluPlayerService`, `_isConnected`)
- **Async methods**: Suffix with `Async` (e.g., `ConnectAsync`, `LoadDataAsync`)

### Code Organization
- Keep methods focused and concise (< 50 lines preferred)
- Place property declarations before method declarations
- Group related functionality together
- Use file-scoped namespace declarations (`namespace BluOsNadRemote.App.Services;`)

### Comments and Documentation
- Add comments only for complex logic or non-obvious behavior
- Avoid redundant comments that restate the code
- Use `///` XML documentation for public APIs
- Prefer self-documenting code through naming

### Async/Await
- Use asynchronous APIs for all I/O operations (network, telnet, HTTP, file system).
- Asynchronous methods should return `Task` or `Task<T>`. Avoid `async void` except for framework event handlers (e.g., `OnDisappearing`).
- Prefer `async` `[RelayCommand]` methods so CommunityToolkit.MVVM generates `IAsyncRelayCommand`.
- Avoid blocking asynchronous code with `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`.
- Fire-and-forget should be rare and only used intentionally. Handle exceptions appropriately.
- Do not use `ConfigureAwait(false)` in application code. It is only appropriate for reusable libraries (`Blu4Net`, `Nad4Net`) or infrastructure code.

### Alerts and Snackbars
- Prefer CommunityToolkit.Maui `Alerts` and `Snackbar` for user dialogs (see `NoConnectionDialogService`).

## Architecture and Patterns

### MVVM Pattern
- **Views**: XAML files in `Views/` folder with minimal code-behind; all pages derive from `BaseContentPage`
- **ViewModels**: Classes in `ViewModels/` folder using CommunityToolkit.MVVM; derive from `BaseViewModel` (plain `ObservableObject`) or `BaseRefreshViewModel` (adds `IsBusy` refresh support)
- **Services**: Business logic in `Services/` folder, registered as singletons
- **Repositories**: Persistence (IPreferences-backed) in `Repositories/` folder (endpoints, culture override, theme override)
- **Models**: POCO data classes in `Models/` (e.g., `Endpoint`, `BluPlayerConnectResult`)
- **API layer**: `BluOsNadRemote.Blu4Net` (BluOS HTTP/SOVI protocol) and `BluOsNadRemote.Nad4Net` (NAD telnet) are plain class libraries that do not reference MAUI

### Dependency Injection (SourceDepend)
- This project relies on [SourceDepend](https://github.com/crwsolutions/sourcedepend) instead of manual constructor injection:
  ```csharp
  [Dependency]
  private readonly BluPlayerService _bluPlayerService;
  ```
- Fields with `[Dependency]` on a `sealed partial` (or `partial`) class are injected via a generated constructor. Continue applying this pattern in new code; do not introduce manual constructors for DI.
- Lifecycle hooks are available as `partial void PostConstruct()` (see `BluPlayerService`, `App`).
- Services are registered in `ServicesExtensions.ConfigureServices()` (`MauiProgram.cs`); keep them there.
- Cross-cutting state lives in singleton services (`BluPlayerService`, `NadTelnetService`, `LanguageService`, `ThemeService`).

### ViewModel Lifecycle
- Pages derive from `BaseContentPage`, which disposes the `BindingContext` in `OnDisappearing` (`IDisposable` / `IAsyncDisposable`) and marks `IsBusy` in `OnAppearing`.
- ViewModels that own subscriptions, timers, or sockets must implement `IDisposable` (or `IAsyncDisposable`) and clean up there (see `PlayerViewModel`).
- `App.OnSleep()` disposes tab-level ViewModels and disconnects the player; rely on this rather than re-implementing app lifecycle logic.

### Shell Navigation
- Shell-based TabBar navigation defined in `AppShell.xaml`: `PlayerPage`, `PresetsPage`, `BrowsePage`, `AdvancedPage`, `SettingsPage` (tabs); `QueuePage`, `SettingsPlayerPage`, `SettingsMorePage` (routable pages).
- Page→ViewModel registrations (and Shell routes) live in `AppConfigurationExtensions.ConfigurePages()` — register new pages there. Tab pages/ViewModels are singletons (the Settings tab is transient); `App.OnResume`/`OnSleep` manage the reconnect.
- Navigate with `Shell.Current.GoToAsync("QueuePage")` (optionally with query parameters, e.g. `?discover=true`), go back with `GoToAsync("..")`.
- Deep tab switching uses the `///{Route}` syntax (see `NoConnectionDialogService`).

### Localization
- All user-visible text must be externalized to resource files
- Resource files in `Resources/Languages/` (`AppResources.resx`, `AppResources.nl.resx`) with generated `AppResources.Designer.cs`
- Use the `TextsViewModel.Instance` singleton with indexer for resolving resource keys in C#
- Use the `TextExtension` XAML markup extension: `{lang:Text Name="Key"}` (produces a OneWay binding to the indexer, so it updates on language change)
- Resource keys follow PascalCase naming convention (e.g., `NoConnection`)
- Supported languages: English (`en-US`) and Dutch (`nl-NL`) — add both when adding a key
- `LanguageService` (singleton, `BehaviorSubject<CultureInfo>`) manages culture switching with `CultureOverrideRepository` (IPreferences-backed); subscribe via `LanguageObservable()` when your component caches localized strings
- Pass `AppResources.Culture` to the API layer where the server supports language (`Accept-Language`), e.g. `BluPlayer.Connect(uri, AppResources.Culture)`

### Theming
- `ThemeService` (singleton) resolves the theme override (`dark`/`light`) from `ThemeOverrideRepository` in `Initialize()`; otherwise follows the requested system theme
- Colors and styles in `Resources/Styles/`

## Project Structure
```
src/
├── BluOsNadRemote.App/           # .NET MAUI app (Android/iOS/Windows)
│   ├── ContentViews/             # Reusable content views (RemoteStepper, spectrum analyzer, ...)
│   ├── Controls/                 # Custom control helpers
│   ├── Extensions/               # Extension methods (player, collections, ...)
│   ├── Models/                   # App POCOs (Endpoint, connect results, ...)
│   ├── Repositories/             # EndpointRepository, CultureOverrideRepository, ThemeOverrideRepository
│   ├── Resources/
│   │   ├── Fonts/                # OpenSans, CrwMedia glyph font
│   │   ├── Images/
│   │   ├── Languages/            # AppResources.resx, AppResources.nl.resx, TextsViewModel, TextExtension
│   │   └── Styles/               # Colors.xaml, Styles.xaml
│   ├── Services/                 # BluPlayerService, NadTelnetService, LanguageService, ThemeService, ...
│   ├── Utils/
│   ├── ViewModels/               # PlayerViewModel, BrowseViewModel, ... (CommunityToolkit.MVVM + SourceDepend)
│   └── Views/                    # Pages (all derive from BaseContentPage)
│
├── BluOsNadRemote.Blu4Net/       # BluOS protocol library (plain .NET 10, no MAUI)
│   ├── Channel/                  # Hand-written XmlReader parsers for each SOVI response
│   ├── BluPlayer.cs              # Player connection, status, actions (HTTP + long polling)
│   └── MusicBrowser.cs           # Browse/presets/queue content
│
├── BluOsNadRemote.Blu4Net.Tests/ # xUnit v3 tests (golden tests over TestData/*.xml)
│
├── BluOsNadRemote.Nad4Net/       # NAD Txxx telnet (RS232) library (plain .NET 10, no MAUI)
│   ├── NadRemote.cs              # Telnet client: connect, command changes, command send
│   ├── Model/                    # Command list model
│   └── Extensions/
│
└── ZeroconfTemp/                 # Local copy of Zeroconf (iOS-specific variant required by Blu4Net)

art/font/                         # CrwMedia glyph font design source (media_font.svg + readme)
docs/                             # BluOS Custom Integration API v1.7 reference + code-vs-API diffs
http/                             # .http files for manual API experimentation
src/player.http                   # .http file for player endpoint testing
```

## Git and Version Control
- Repository: https://github.com/crwsolutions/BluOsNadRemote
- Main branch: `main`
- Conventional commits when applicable
- **Never push** unless explicitly asked

## Testing
- Unit/golden tests live only in `BluOsNadRemote.Blu4Net.Tests` (xUnit v3)
- Golden tests parse XML fixtures from `TestData/` (copied to output at build time) via `Fixture.Reader("Status.xml")`; keep fixtures and `Channel/` parsers in sync when the protocol parsing changes
- `InternalsVisibleTo` is configured so tests can access internal members of `Blu4Net`
- The MAUI app itself has no unit tests; manual testing against real hardware is the primary approach

## Common Patterns

### Controls
- Glyph-based icons use the `CrwMedia` font family (`FontImageSource FontFamily="CrwMedia"`)

### BluOS Protocol (Blu4Net)
- `BluPlayer` handles the SOVI protocol over HTTP (long polling for state changes) against port 11000
- `Channel/` contains one hand-written `XmlReader` parser per response type (e.g., `StatusResponse.Read(reader)`); the API layer was deliberately moved away from runtime `XmlSerializer` — keep new parsing in that style
- Response payloads are defined in `docs/BluOS-Custom-Integration-API_v1.7.md`; check `docs/Code-vs-API-v1.7-diffs.md` for known deviations
- Use the `.http` files (`src/player.http`, `http/bluos_app/`) to inspect live endpoints during development

### NAD Telnet (Nad4Net)
- `NadRemote` connects to the NAD Txxx on TCP port 23 and exposes `CommandChanges` (System.Reactive observable) plus command sending
- Connection failures surface as `NadConnectException` with a reason; the app layer (`NadTelnetService`) maps them to user-friendly results (see `AdvancedViewModel` / settings ping UX)

### Connection State and UX
- `EndpointRepository` stores discovered BluOS endpoints (zeroconf via `ZeroconfTemp`) and the user-selected one
- Services return typed connect-result POCOs (e.g., `BluPlayerConnectResult`, `NadTelnetConnectResult`) instead of throwing into the UI layer
- `NoConnectionDialogService` centralizes "no connection" snackbars/alerts — reuse it for connection failures
- All connection logic must be resilient to app sleep/resume (`App.OnSleep()` disconnects everything)

### Localization Example
```csharp
// C#
var message = TextsViewModel.Instance["NoConnection"];

// XAML
<Label Text="{lang:Text NoConnection}" />
```
