# Discovery migreren van mDNS (Zeroconf) naar LSDP (CI-API v1.7, appendix 13.1)

- Status: Approved (2026-09-13, Colin) — klaar voor executie in Default mode.
- Plan file: `.alta/plans/2026-09-13-lsdp-discovery.md`
- Created: 2026-09-13
- Task: Vervang de huidige mDNS-gebaseerde discovery (`ZeroconfTemp`/Zeroconf) door LSDP conform `docs/BluOS-Custom-Integration-API_v1.7.md` §13.1 (UDP broadcast, port 11430, class 0x0001) en verwijder de mDNS-stack volledig (geen fallback).
- Git: niet genegeerd → planfile hoort bij de implementatie, maar wordt alleen gecommittet op expliciet verzoek van Colin.

## Objective

- Doel: `BluEnvironment.ResolveEndpointsAsync` ontdekt BluOS-spelers uitsluitend via LSDP: per actieve IPv4-interface een UDP-broadcast-query (port 11430), Announce-packets parsen, elke host verifiëren via `BluPlayer.Connect` (reachability + speler-naam), en daarna merge in `EndpointRepository` — exact zoals nu.
- Doel: `src/ZeroconfTemp` volledig verwijderen **alsof het nooit heeft bestaan** (map, projectreferentie, solution entry, én alle vermeldingen in docs/AGENTS.md; geverifieerd: geen referenties in CI (`.github`), `.gitignore`, `global.json` of elders buiten `src/`).
- Niet-doelen:
  - Geen mDNS-fallback (user: "niets meer").
  - Geen implementatie van de *adverteerders*-kant van LSDP (Announce/Delete uitsturen, 57 s-announce-timer, 7-packets-op-startup) — de app is alleen een query-client.
  - Geen wijziging in connect-flow, UI, settings, ViewModels, DI of permissiebestanden.
  - Geen commit tenzij Colin dat expliciet vraagt.

## Context and evidence

- Huidige discovery: `src/BluOsNadRemote.Blu4Net/BluEnvironment.cs` — `ZeroConfTemp.ZeroconfResolver.ResolveAsync("_musc._tcp.local.", 5s)` met retry tot 3× (`MaxScanAttempts`); per host `BluPlayer.Connect` voor naam + reachability. `BluPlayerService.DiscoverAsync` (`src/BluOsNadRemote.App/Services/BluPlayerService.cs:70`) merge via `EndpointRepository.MergeEndpoints` (dedup op `Uri`).
- LSDP-spec (doc §13.1, regels 1915–2033): UDP broadcast naar/van port **11430** (IANA-Lenbrook); header `Length(1)=6` + magic **`"LSDP"`** + `Version(1)=1`; Query **`0x51`** ("Q", count + class-ids); Announce **`0x41`** ("A": Node ID + IPv4-address (len 4) + records met TXT); Delete **`0x44`** ("D"); alle multi-byte waarden **big-endian**; class **`0x0001`** = BluOS Player (mDNS-equivalent `_musc._tcp`); query-response delay 0–750 ms; Note 2: één node mag splitsen over meerdere Announce-messageblocks.
- `BluOsNadRemote.Blu4Net.csproj` targett `net10.0` zonder MAUI-referentie; `ZeroconfTemp` wordt alleen door `Blu4Net` gerefenced (csproj regel 13).
- `ZeroconfTemp` staat ook in de root **`BluOsNadRemote.slnx`** (regel 25 — slnx/XML-opmaak, géén .sln). Het App-csproj refereert alleen `Blu4Net`.
- Enige code-usage van de `Zeroconf`-namespace: `BluEnvironment.cs` (`using Zeroconf;` + `IZeroconfHost`).
- Permissies (geen wijzigingen nodig, geverifieerd): iOS `Info.plist` heeft al `NSLocalNetworkUsageDescription` + `NSBonjourServices`; Android `AndroidManifest.xml` heeft `INTERNET` + `ACCESS_NETWORK_STATE` (+ `UsesCleartextTraffic = true` in `MainApplication.cs` voor de latere HTTP-op-11000). De iOS Local Network prompt wordt getriggerd door het eerste lokale socketgebruik (UDP-broadcast telt mee).
- Tests: xUnit v3 in `src/BluOsNadRemote.Blu4Net.Tests`; `InternalsVisibleTo` staat al → internal parse-methods zijn testbaar zonder sockets.
- `docs/Code-vs-API-v1.7-diffs.md` §10 (regel 162) noteert nu: "13.1 Lenbrook Service Discovery (LSDP) — code gebruikt mDNS via `Zeroconf`"; `AGENTS.md` noemt `ZeroconfTemp` in de project structuur ("zeroconf via `ZeroconfTemp`").

## Assumptions and open decisions

- Aannames (bevestigd door Colin in gesprek, 2026-09-13):
  - mDNS blijft er **niet** als fallback; discovery = LSDP-only. Oudere firmware zonder LSDP wordt niet meer gevonden — geaccepteerd.
  - De `src/ZeroconfTemp`-map wordt **nu** verwijderd (deel van "niets meer").
- Aannames (technisch, in dit plan vastgezet):
  - Query alleen class `0x0001` (BluOS Player). De app connecteert nooit naar een BluOS Server (`0x0002`).
  - Query per actieve IPv4-interface naar het **subnet-broadcast-adres** (betrouwbarder dan `255.255.255.255`; firmware doet hetzelfde per interface).
  - Speler-naam komt nog steeds uit `BluPlayer.Connect` (bestaand gedrag); LSDP-TXT-records worden geparst maar niet gebruikt (max. Debug-loggen).
  - Bestaande retry (tot 3 scans × 5 s, dus max ~15 s) blijft — zelfde UX als nu; elke scan = 1 query + 5 s luistervenster (response-delay is max 750 ms, dus ruim voldoende).
  - Node ID (MAC) wordt meegelogd maar niet als sleutel gebruikt: dedup gebeurt op IP (LSDP doc: Node ID is gelijk per interface; `EndPoint` is al Uri-keyed).

## Design notes

- **Nieuw bestand `src/BluOsNadRemote.Blu4Net/LsdpDiscoverer.cs`** (pure .NET, geen platform-code, geen MAUI):
  - Constantes: port 11430, magic `"LSDP"`, version 1, `ClassBluOsPlayer = 0x0001`, types `Q=0x51` / `A=0x41` / `D=0x44`.
  - `public static Task<IReadOnlyList<LsdpHost>> DiscoverAsync(TimeSpan? listenWindow = null)` met `public sealed record LsdpHost(string NodeId, IPAddress Address)` (altijd IPv4).
  - Interfaces: `NetworkInterface.GetAllNetworkInterfaces()` → `OperationalStatus.Up` + unicast IPv4 (prefix length via `GetIPProperties().GetIPv4Properties()`); subnet broadcast = netwerkadres | ~mask.
  - Per interface: `UdpClient(AddressFamily.InterNetwork)` gebonden op het interface-IPv4-adres, `EnableBroadcast = true`, query sturen, asynchroon luisteren tot `listenWindow` (cancellable), collect. Alle clients `Dispose`n; geen exceptions naar boven (leeg resultaat bij fout).
  - Query-packet (11 bytes): header `06 4C 53 44 50 01` + message `03 51 01 00 01`.
  - Parse (internal static, los van de socket-logica voor testbaarheid): per datagram header valideren (Length=6, magic, version); messageblocks doorlopen **op Length** (onbekende types overslaan — future-proof per spec); Announce: `NodeIdLen`+`NodeId`, `AddrLen` (verwacht 4) + IPv4, `Count` → per record `Class` (2B, big-endian) + `TxtCount` + key/value-pairs; alleen class `0x0001` behouden. Delete (`0x44`) en unicast-query (`0x52`) negeren. Dekk ook Note 2 (meerdere Announce-blocks per datagram) via de block-loop + dedup op IP.
  - `Debug.WriteLine` per gevonden node (NodeId + IP), conform projectconventie.
- **`BluEnvironment.cs` (aanpassen):**
  - `ZeroconfResolver.ResolveAsync(EndpointProtocol, DefaultScanTimeout)` → `LsdpDiscoverer.DiscoverAsync(DefaultScanTimeout)`; retry-loop en `MaxScanAttempts = 3` blijven; `DefaultEndpointPort` blijft (ook gebruikt door `App/Utils/BluOsHost.cs`).
  - `GetEndpoint`: `http://<ip>:11000` (IPv4; geen bracket-hanteering nodig — die blijft in `BluOsHost` voor handmatig ingevoerde hostnames).
  - `EndpointProtocol` (`_musc._tcp.local.`) verwijderen (enige usage is de mDNS-path).
  - `using Zeroconf;` verwijderen.
- **Zeroconf verwijderen:**
  - `src/ZeroconfTemp/`-map verwijderen (gevendored copy van Zeroconf 3.7.16; geen ander project refereert ernaar).
  - `<ProjectReference>` verwijderen uit `src/BluOsNadRemote.Blu4Net/BluOsNadRemote.Blu4Net.csproj`.
  - `<Project>`-entry verwijderen uit `BluOsNadRemote.slnx` (root).
- **Niet aanraken:** `EndPoint`-model, `EndpointRepository`/`MergeEndpoints`, `BluPlayerService.DiscoverAsync`, SettingsViewModel/NoConnectionDialogService, DI-registraties, `Info.plist`, `AndroidManifest.xml`.

## Risks and challenges

- Oudere firmware zonder LSDP wordt niet meer ontdekt (geaccepteerde trade-off; Lenbrook zelf noemt broadcast betrouwbaarder dan multicast).
- Netwerken die UDP-broadcast blokkeren → geen discovery; zelfde categorie risico die Lenbrook in de doc noemt.
- iOS: Local Network prompt bij eerste discovery; eerdere weigering = stilte (zelfde als nu bij mDNS). Geen extra plist-keys nodig.
- Windows: Windows Firewall kan bij de eerste run één keer vragen (dev-machine; product-gebruikers op het LAN hebben dit niet als het pakket geïnstalleerd is).
- IPv6: LSDP draagt alleen IPv4 (Address len = 4) → alleen IPv4-interfaces queryen. (mDNS was in de praktijk ook IPv4-only.)
- Echte eindtot-eind-verificatie vereist BluOS-hardware (projectconventie: agent draait de app niet; Colin test handmatig).

## Implementation checklist

- [x] `src/BluOsNadRemote.Blu4Net/LsdpDiscoverer.cs` toevoegen: constantes, `LsdpHost`-record, `DiscoverAsync` (per-interface subnet-broadcast query + cancellable listen + dedup op IP + `Debug.WriteLine` + dispose; geen exceptions naar caller).
- [x] In dezelfde klasse: public static `Parse(ReadOnlySpan<byte> datagram)` + private `ParseAnnounceBlock` met big-endian reads; onbekende messageblocks skippen op Length; Delete/unicast-query negeren. (Deviation: `Parse` is public i.p.v. internal — `InternalsVisibleTo` dekt alleen internals en de tests zitten in een andere assembly; `BuildQueryPacket()` is internal en wordt eveneens getest.)
- [x] `BluEnvironment.cs`: Zeroconf-call vervangen door `LsdpDiscoverer.DiscoverAsync`; `EndpointProtocol` en `using Zeroconf;` verwijderen; `GetEndpoint` op IPv4; retry-loop + `BluPlayer.Connect`-verificatie behouden.
- [x] `src/BluOsNadRemote.Blu4Net/BluOsNadRemote.Blu4Net.csproj`: `ZeroconfTemp`-ProjectReference verwijderen. **Deviation (noodzakelijk):** `Blu4Net` gebruikte `System.Reactive` transitief via `ZeroconfTemp` → expliciet `<PackageReference Include="System.Reactive" Version="5.0.0" />` toegevoegd (zelfde versie die via Zeroconf kwam; anders compileerfouten in `BluPlayer`/`Channel`).
- [x] `BluOsNadRemote.slnx` (root): `ZeroconfTemp`-project entry verwijderen.
- [x] `src/ZeroconfTemp/` map van disk verwijderen (inclusief `bin/`/`obj/`; git tracet de verwijdering).
- [x] `src/BluOsNadRemote.Blu4Net.Tests/LsdpDiscovererTests.cs` toevoegen (xUnit v3): query-packet = exacte bytes; geldige Announce → (NodeId, IPv4, class 0x0001); class-filtering (0x0002/0x0005 genegeerd); onbekend message-type overslagen; meerdere messageblocks per datagram (Note 2); Delete en unicast-query genegeerd; verkeerde magic/length/version → leeg; big-endian check op class; truncated/lege block → leeg.
- [x] Docs: `docs/Code-vs-API-v1.7-diffs.md` §10 — LSDP-rij verwijderd; `AGENTS.md` — `ZeroconfTemp`-regels uit projectstructuur weg + `BluEnvironment.cs`-regel toegevoegd + EndpointRepository-notitie bijgewerkt naar LSDP.

## Verification checklist

- [x] `dotnet build` — compileert zonder warnings (0 warnings, 0 errors; .NET 10 SDK 10.0.204).
- [x] `dotnet test src/BluOsNadRemote.Blu4Net.Tests` — groen, inclusief nieuwe `LsdpDiscovererTests`. **Deviation in testrunner:** op .NET 10 SDK is `dotnet test` via VSTest niet meer ondersteund door Microsoft.Testing.Platform (xunit.v3 4.0.0) → uitgevoerd met `dotnet src/BluOsNadRemote.Blu4Net.Tests/bin/Debug/net10.0/BluOsNadRemote.Blu4Net.Tests.dll`: 81 tests, 0 failed. (Bestaande situatie, niet door deze taak veroorzaakt; testrunner-opt-in is een apart decision.)
- [x] Exhaustieve grep over de hele repo (buiten `.git/` en dit planfile): geen enkel voorkomen meer van `Zeroconf`/`zeroconf` (case-insensitief) — ook niet in `AGENTS.md`, `docs/`, `.github/`, `.gitignore`, `global.json`.
- [ ] Handmatig (Colin, echte hardware): discovery via Settings-tab vindt de speler(s) op een Windows-apparaat; bij voorkeur ook een Android-device.
- [ ] Handmatig: app sleep/resume gedrag ongewijzigd (sockets leven alleen per discovery-run; geen permanente listeners).

## Deviaties op het plan (technisch, geverifieerd tijdens executie)

1. **Query-packet bytes:** het plan gaf `03 51 01 00 01` ("11 bytes") — de `03` was een typo. Spec §13.1 Query = `Length(1) + Type(1) + Count(1) + Class(2)` = 5 bytes → correcte bytes zijn `05 51 01 00 01` (totaal wél 11 bytes: 6 header + 5 query). Geïmplementeerd conform de spec-tabel, getest in `BuildQueryPacket_MatchesSpecifiedBytes`.
2. **`System.Reactive` PackageReference** in `Blu4Net.csproj`: `ZeroconfTemp` leverde `System.Reactive 5.0.0` transitief; na verwijdering moet `Blu4Net` dit zelf refereren (zelfde versie → geen behavioral verandering).
3. **`Parse` is public** i.p.v. internal: `InternalsVisibleTo` dekt alleen internal members van `Blu4Net`; een public parse-methode is ook een nette, geteste API voor latere tooling.

### Socket-path herziening na eerste handmatige test (Android, 2026-09-13)

Colin kon met de eerste versie geen spelers vinden op een fysiek Android-apparaat (2× `SocketException` in de debug window). mDNS/Zeroconf werkte op hetzelfde apparaat met dezelfde rechten, dus geen permissions-probleem. Twee echte bugs, beide gefixt en geverifieerd:

1. **Socket liep op een ephemeral poort** — de belangrijkste bug. Spec §13.1: LSDP gebruikt "all UDP broadcast packets **to and from** port 11430"; firmware stuurt Announce-responses *naar* 11430. `new UdpClient()` bindt op een willekeurige poort → response werd nooit ontvangen → 0 spelers. Fix: socket bindt op `0.0.0.0:11430` (wildcard-bind is op Android veilig; per-interface-bind was het bekende probleem). Query wordt ook vanaf 11430 gestuurd, dus unicast-responses komen ook goed terug.
2. **Per-interface `Bind`** — de oorspronkelijke implementatie boonde per interface op het interface-IP (`EADDRNOTAVAIL`/`EACCES` op Android). Fix: één socket, queries naar de directed broadcast per subnet **en** `255.255.255.255` (limited broadcast als fallback wanneer prefix/mask ontbreekt — op Android Wi-Fi is `PrefixLength` vaak 0; de mask wordt nu liever uit `IPv4Mask` gelezen).
3. **Diagnostics:** `[LsdpDiscoverer]`-Debug-lijnen per interface/mask, per query-send, per ontvangen datagram (met afzender + byte count) en `SocketError`-codes bij falen, zodat de volgende handmatige test direct de oorzaak toont.
4. **Verificatie:** build 0 warnings; 81/81 unit tests; lokale end-to-end simulatie (nep-speler stuurt Announce naar poort 11430) → speler ontdekt en geparst (E2E PASS).

## Handoff notes

- Solutionfile is **`BluOsNadRemote.slnx`** op de repo-root (XML slnx-formaat, géén .sln) — daar de project entry weg.
- `ZeroconfTemp` is een gevendored copy van het Zeroconf-pakket (3.7.16); geen enkel ander project of bestand refereert ernaar → map volledig verwijderen is veilig.
- `Blu4Net` is `net10.0` zonder MAUI-referentie: geen platform-code nodig; `System.Net.Sockets.UdpClient` + `System.Net.NetworkInformation` volstaat op alle drie targets.
- Permissies zijn al compleet (iOS `Info.plist` + Android manifest geverifieerd) — geen wijzigingen.
- Geen commit tenzij Colin expliciet vraagt; eventueel commit bevat dan ook dit planfile.
- Reachability + speler-naam blijven afhangen van `BluPlayer.Connect`: een dode IP in een Announce wordt gefilterd zoals nu (try/catch per host).
