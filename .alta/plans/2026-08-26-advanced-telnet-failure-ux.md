# Advanced tab: vriendelijke telnet-verbinding en foutafhandeling

- Status: Approved
- Plan file: `.alta/plans/2026-08-26-advanced-telnet-failure-ux.md`
- Created: 2026-08-26
- Task: Advanced-page hangt eeuwig of toont ruwe technische fout als de telnet-verbinding (port 23) naar de NAD lukt niet; dat gebruikensvriendelijker en robuuster maken.
- Git: niet genegeerd → dit planfile moet meegewezen worden met de implementatie.

## Objective

- Doel: op de Advanced-tab **nooit** een eindeloze "Laden..." meer, nooit een ruw `exception.Message`, en altijd een duidelijke ge-localiseerde fout + eenvoudige handmatige retry (swipe). Zelfde voor de "Ping telnet" knop in Settings. Mid-session uitval (router/slaapstand) niet langer onzichtbaar laten.
- Niet-doelen:
  - Geen generieke "connectiviteit-monitor" voor de hele app.
  - Geen wijziging van de BluOS (HTTP) logica.
  - Geen nieuw pakket toevoegen (alles kan met bestaande `Telnet`/`System.Reactive` packages).
  - Geen inline error-banner/knop op de page (user koos Snackbar).
  - Geen automatisch opnieuw proberen (user koos handmatig).

## Context and evidence

- `src/BluOsNadRemote.App/Services/NadTelnetService.cs:18-30` — `Connect()` maakt alleen een `NadRemote`-object. **Echte TCP-connect pas bij eerste `ReadAsync()`** (lazy).
- `src/BluOsNadRemote.Nad4Net/NadRemote.cs:118-126` — `ReConnectAsync()` roept eerst `Dispose()` (annuleert de CTS) en bouwt dan een `Client` met de **zelfde, reeds geannuleerde token**; de eerste `Parse(await _client.ReadAsync())` heeft **geen timeout** → unreachable host = onbeperkt hangen.
- `NadRemote.cs:47` — `Timeout = 30s` bestaat alleen voor `ReadAsync(Timeout)` in de change-detection-loop; de init-read gebruikt `ReadAsync()` (no timeout).
- `NadRemote.cs:127-161` — `SetupChangeDetectionLoop()`: `.Retry(RetryDelay)` op een cold observable die een `Task.Run`-listener start; bij `OnError` stopt de stream definitief, maar de app weet het niet → stille/stale data.
- `src/BluOsNadRemote.App/ViewModels/AdvancedViewModel.cs:23-55` — `LoadDataAsync`:
  - `catch (Exception)` zet `Title = exception.Message` → **dat is de "vreemde technische foutmelding"** op de telefoon (bijv. een ruw `SocketException`-message).
  - `NadTelnetConnectResult.Message` voor "no endpoint" is hard-coded Engels (`NadTelnetService.cs:24`) ondanks resx-localisatie.
- `src/BluOsNadRemote.App/Views/AdvancedPage.xaml:12-13` — `RefreshView.Command=LoadDataCommand`, `IsRefreshing` = `{Binding IsBusy, Mode=TwoWay}`. `IsBusy` begint `false`, wordt `true` in `OnAppearing` (`BaseContentPage.cs:18-23`) → swipe-omhoog triggert `LoadData` opnieuw = bestaande handmatige retry-mechanica.
- `src/BluOsNadRemote.App/Views/BaseContentPage.cs:35-45` — `OnDisappearing` roept `Dispose()`; `App.OnSleep` ook (`App.xaml.cs:53-58`).
- Bestaand patroon voor fouten: `NoConnectionDialogService` (Snackbar `DarkOrange`, `AppResources.NoConnectionDialogMessage`) — gebruikt op Player/Queue/Presets/Browse, **niet** op Advanced.
- Existing resx strings: `NoConnect` = "Kon niet verbinden" (nl) / `Loading` = "Laden...".
- `SettingsPlayerViewModel.TelnetPingAsync` (`SettingsPlayerViewModel.cs:24-43`) toont dezelfde hang (geen timeout in `PingAsync`) én dumpt de volledige exception in `Result`.

## Decisions (bevestigd door user, 2026-08-26)

- **Timeout-duur = 3 seconden** voor telnet-connect en init-read.
- **Fout-presentatie = Snackbar**, net als de andere tabs (via `NoConnectionDialogService`-patroon). Geen inline error-banner.
- **Retry = alleen handmatig** (swipe-omhoog via bestaande RefreshView). Geen auto-retry na mislukte connect.
- **Scope = beide**: ook `SettingsPlayerViewModel.TelnetPingAsync` fixen met dezelfde timeout.
- **Plan review = Ja**; handoff naar Default/execute.

## Assumptions

- **A1 (assume):** NAD is de enige telnet-endpoint; host komt uit `EndpointRepository.SelectedEndpoint`.
- **A2 (assume):** 3s kan op een langzaam/overbelast netwerk een valse "niet verbonden" geven; user heeft dit geweten geaccepteerd.

## Design notes

**Chosen approach (3 lagen):**

1. **Nad4Net — `NadRemote`**
   - Nieuwe async `ConnectAsync(TimeSpan? timeout = null, CancellationToken ct = default)`:
     - `Dispose()`; **nieuwe** `CancellationTokenSource`; `Client` op `TcpByteStream(_host, PORT)`;
     - init `ReadAsync` met `timeout ?? TimeSpan.FromSeconds(3)`; timeout/cancellation/`SocketException` → throw nette `NadConnectException(reason, host)` (nieuwe exceptie met `enum NadConnectReason { Timeout, Unreachable, Negotiation }`).
   - `ReConnectAsync()` gebruikt de **nieuwe** token (bug fix); de oude CTS wordt netjes weggegooid.
   - `PingAsync` krijgt optionele timeout doorgeven aan connect/init-read.
   - Change-detection loop:
     - `OnError` → interne herstart na `RetryDelay` (5s) via while-loop + `Task.Delay` met token (in plaats van `.Retry` op cold observable), zodat de verbinding zich zelf herstelt als de NAD terugkomt.
     - Nieuwe `IObservable<NadConnectionState> ConnectionStateChanged` (`enum { Connected, Reconnecting, Disconnected }`) voor eventuele UI-indicator.
   - `Parse` verhard: regels zonder `=` of met ongetal (`Main.Dirac` etc.) → skip + `Debug.WriteLine`, geen crash.

2. **App — `NadTelnetService`**
   - `Connect()` → `ConnectAsync()`:
     - geen endpoint → `NadTelnetConnectResult` met **ge-localiseerde** message (nieuw resx key `NoEndpointMessage`), geen hard-coded Engels.
     - Timeout/Unreachable → `NadTelnetConnectResult` met `reason` + host (VM bouwt er de ge-localiseerde `CouldNotConnectTelnet` ({0}=host) van).
     - Slag → `Connected`.
   - `IsConnected`-property blijft.

3. **App — `AdvancedViewModel` + `NoConnectionDialogService`**
   - `NoConnectionDialogService`: overload `ShowAsync(string? message = null)` (default = huidige generieke message) zodat de host-specifieke tekst kan meekomen.
   - `LoadDataAsync`:
     - probeert `ConnectAsync()` **allereerst** (timeout 3s) → bij falen: `IsBusy=false`, `Title = AppResources.NoConnect` (vriendelijk, ge-localiseerd), snackbar met `CouldNotConnectTelnet` + host; **nooit** ruw `exception.Message`.
     - `catch`-blokketjes: bekende types netjes, onbekende → generiek `AppResources.NoConnect`.
     - Retry blijft handmatig via de bestaande `RefreshView` (swipe-omhoog triggert `LoadDataCommand` opnieuw).
   - `Dispose()`: subscriber + disconnect + state-reset zodat de volgende keer schone state.
   - (Optioneel, klein) `ConnectionStateChanged` doorgeven naar page: kleine "herverbinden…" indicator.

**Alternatives rejected:**
- Globale health-check op interval: te zwaar, batterij/energie, niet nodig.
- `Task.WhenAny(ReadAsync, Task.Delay(...))` als "quick fix": lost alleen het hangen op, niet de ruwe fout, niet mid-session crash, niet de token-reuse bug.
- Inline error-banner + retry-knop (A): user koos Snackbar (B).
- Eigen socket-implementatie: te veel scope; `Telnet`-pakket is al gekozen.

**Compatibility/security/migration:**
- Geen API-breuk buiten `Nad4Net` (interne repo); `NadRemote` publiceert een extra observable + methode, bestaande call sites blijven werken.
- Gegevens/privacy: geen extra netwerken, geen logging van credentials.
- Rollback: alle wijzigingen in ~6 bestanden + resx; revert van commit lost het.

## Risks and challenges

- **R1 (midden):** `Telnet` 0.13.1 `Client` kan bij connect-timeout een `OperationCanceledException` of `SocketException` throwen — beide moeten netjes naar `NadConnectException` vertaald worden; exacte type per scenario bepalen tijdens implementatie.
- **R2 (midden):** `RefreshView.IsRefreshing=TwoWay` + `OnAppearing` kan dubbel-`LoadData` veroorzaken; `AllowConcurrentExecutions=true` + nieuwe connect = 2 TCP-attempts. Mitigatie: `IsBusy`-guard in `LoadDataAsync` (early return) of `AllowConcurrentExecutions=false` (verwijdert de bug-workaround-comment op regel 22).
- **R3 (midden):** 3s timeout is kort — op langzame netwerken valse "niet verbonden". Geaccepteerd (user keuze A2); swipe-retry is dan de uitweg.
- **R4 (laag):** resx-strings in 2 talen (nl + en); `Designer.cs` wordt niet handmatig aangepast (MSBuild genereert).
- **R5 (laag):** `Dispose()`-tijdens-connect: `NadConnectException`/`OperationCanceledException` — UI moet geen snackbar tonen als de pagina al verdwenen was (token check).

## Implementation checklist

- [ ] `Nad4Net`: nieuw `NadConnectException` + `enum NadConnectReason` (bestand: `Nad4Net/NadConnectException.cs`)
- [ ] `Nad4Net/NadRemote.cs`: `ConnectAsync(timeout, ct)` toevoegen; `ReConnectAsync` token-reuse fix; init-read met 3s-default timeout
- [ ] `Nad4Net/NadRemote.cs`: `PingAsync` krijgt optionele timeout
- [ ] `Nad4Net/NadRemote.cs`: `Parse` verhard (skip malformed lines, geen crash)
- [ ] `Nad4Net/NadRemote.cs`: change-detection loop zelf-retry na 5s bij `OnError` + `ConnectionStateChanged` observable (`NadConnectionState { Connected, Reconnecting, Disconnected }`)
- [ ] `App/Services/NadTelnetService.cs`: `ConnectAsync()` met nette reasons + host; geen hard-coded Engelse message
- [ ] `App/Resources/Languages/AppResources.resx` + `AppResources.nl.resx`: nieuwe keys `NoEndpointMessage`, `CouldNotConnectTelnet` ({0}=host), `CouldNotConnect` (fallback)
- [ ] `App/Services/NoConnectionDialogService.cs`: overload `ShowAsync(string? message = null)`
- [ ] `App/ViewModels/AdvancedViewModel.cs`: `LoadDataAsync` herstructureren — connect-eerst (3s), bij falen nette Title + snackbar met host-msg, geen ruw `exception.Message`; `Dispose()` state-reset
- [ ] `App/ViewModels/SettingsPlayerViewModel.cs`: `TelnetPingAsync` geeft 3s timeout door + vriendschappelijke "Failed"-msg in plaats van volledige exception-dump
- [ ] (optioneel, alleen als laag) `AdvancedPage.xaml` + VM: kleine "herverbinden…" indicator bij `ConnectionStateChanged == Reconnecting`

## Verification checklist

- [ ] `dotnet build BluOsNadRemote.slnx` — 0 errors
- [ ] Handmatig (Android/Windows): NAD **uit** → Advanced-tab → na ~3s snackbar "Kon niet verbinden met {host}", title "Kon niet verbinden"; geen hang, geen ruwe fout
- [ ] Handmatig: swipe-omhoog → opnieuw proberen, werkt (en faalt netjes opnieuw als NAD nog uit is)
- [ ] Handmatig: NAD **aan** → Advanced-tab → data laadt (regressie)
- [ ] Handmatig: NAD uit tijdens sessie → data stopt; NAD weer aan → (zelf-retry herpakt of) swipe → werkt
- [ ] Handmatig: Settings → "Ping telnet" met NAD uit → na ~3s nette "Failed"-melding, geen hang
- [ ] Bestaande tests: `dotnet test` (als aanwezig)
- [ ] Resx: beide talen renderen (nl + en), Designer regeneratie lukt bij build
- [ ] Self-review: `git diff` — alleen verwachte bestanden, geen ruwe exception-strings in UI

## Handoff notes

- Alle decisions zijn bevestigd door de user: **3s timeout, Snackbar, handmatige retry, ook Settings-ping meenemen**. Plan is Approved.
- **Belangrijkste bestanden:** `Nad4Net/NadRemote.cs`, `Nad4Net/NadConnectException.cs` (nieuw), `App/Services/NadTelnetService.cs`, `App/Services/NoConnectionDialogService.cs`, `App/ViewModels/AdvancedViewModel.cs`, `App/ViewModels/SettingsPlayerViewModel.cs`, beide `AppResources.*.resx`.
- **Patronen:** `NoConnectionDialogService` voor snackbar-stijl; `BaseRefreshViewModel.IsBusy` voor spinner; `AppResources` voor alle strings (nooit hard-coded).
- **Bug-workaround te overwegen te verwijderen:** `AdvancedViewModel.cs:22` `[RelayCommand(AllowConcurrentExecutions = true)]` + GitHub-link comment — wordt overbodig bij `IsBusy`-guard (R2).
- **Commit:** planfile + implementatie in 1 commit (plan is niet git-ignored).
- **Testen:** NAD-uit/aan cycli zijn de belangrijkste handmatige checks.
