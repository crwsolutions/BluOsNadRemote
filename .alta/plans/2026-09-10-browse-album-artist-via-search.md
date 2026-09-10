# Browse "Go to album/artist": legacy browseKeys vervangen

- Status: Executed (2026-09-10) — **afgeweken van oorspronkelijk zoek-flow-ontwerp** (zie Deviation hieronder). Niet committen totdat de user akkoord is.
- Plan file: `.alta/plans/2026-09-10-browse-album-artist-via-search.md`
- Created: 2026-09-10
- Task: De Player- en Queue-action-sheet items "Blijf naar album/artiest bladeren" werken niet meer (firmware antwoordt 400 + lege body op de handmatig opgebouwde legacy browseKeys); vervangen door een zoek-flow die uitsluitend gedocumenteerde BluOS-CI-API endpoints en speler-geleverde browseKeys gebruikt, inclusief robuuste foutafhandeling, Debug-logging en tests voor zowel album als artiest.
- Git: niet genegeerd → planfile hoort bij de implementatie, maar **niet committen** (user wil eerst handmatig testen; alleen committen op expliciet verzoek).

## Objective

- Doel: "GoToAlbum"/"GoToArtist" (PlayerViewModel én QueueViewModel) werken weer: van de current track (of queue-item) naar de album- of artiest-pagina in de Browse-tab, uitsluitend via (a) gedocumenteerde endpoints en (b) browseKeys die de speler zelf levert. Extra roundtrips zijn geaccepteerd (user).
- Doel: lege/speler-fout-antwoorden geven een begrijpelijke melding, niet `XmlException: Root element is missing` → "Kon geen browsers ophalen" + disconnect.
- Doel: genoeg `Debug.WriteLine` om de flow tijdens handmatig testen te volgen (DEBUG-build logt daarnaast al alle requests/responses via `BluPlayer.Log`/`DebugTextWriter`).
- Niet-doelen:
  - Geen use van niet-publieke/ondocumenteerde endpoints (direct `/Albums?…`, `Tidal:MG/…` keys bouwen).
  - Geen `GetNodeSongNode`-equivalent (momenteel onbruikt; zie Open decisions).
  - Geen aanpassing aan overige browse-functies (search, next, context menu, presets).
  - Geen commit (user testt eerst).

## Context and evidence

Geconstateerd op de live speler `http://192.168.107.241:11000` (alle requests read-only `/Status`, `/Browse`, `/Playlist`):

- Legacy-key faalt: `GET /Browse?key=%2fAlbums%3fservice%3dTidal%26albumid%3d256734465` → **HTTP 400, lege body** → `XmlException: Root element is missing` in `BluChannel.SendRequest` (`src/BluOsNadRemote.Blu4Net/Channel/BluChannel.cs:74`) → catch in `BrowseViewModel.LoadDataAsync` → `Title = AppResources.NoBrowsers` + `Disconnect()`.
- App-code is níét de oorzaak: request-constructie is identiek vóór/ná de XmlReader-migratie (`c0dea6d`); de vorm is gewoon door de firmware gedeprecateerd.
- Speler-leverde keys wél ok: `/Browse?key=Tidal%3aMG%2fTidal-Album%3falbumid%3d…` → tracks. Maar per user **niet toegestaan** (niet-publiek API-oppervlak).
- Gedocumenteerde zoek-flow (CI-API v1.7 §7.2, `searchKey`/`browseKey` zijn speler-geleverd → mag doorgeven):
  1. Root `/Browse` → `<browse type="menu">` (geen searchKey) met o.a. item `browseKey="Tidal:"` (documenteerde second-level key, doc-voorbeeld `key=Tidal%3A`).
  2. `/Browse?key=Tidal%3aSearch&q=Sting` → top-level met 4 links: `Tidal:Artist/%2FArtists%3Fexpr=…`, `Tidal:Album/%2FAlbums%3Fexpr=…`, `Tidal:Song/…`, `Tidal:Playlist/…` (allemaal `type="link"`).
  3. `Tidal:Album/…Albums…` opvolgen → `<browse type="albums" nextKey="…start=30">` met items; **elk item bevat het id in zijn `browseKey`** (`Tidal:MG/Tidal-Album?albumid=256734465`) en ook in `playURL`/`contextMenuKey` (`…&albumid=256734465&…`).
  4. **ID matching is deterministisch**: het gespeelde album (status `albumid=256734465`) staat exact zo in de resultaten; naammatching is níét (duplex-edities `256734465` vs `35716872` vs `252518893`; en bij zoeken op de artiest stond het album buiten pagina 1 → `nextKey`-paginering is verplicht).
- Artiest-flow bevestigd: `Tidal:Artist/…Artists…?expr=Sting` → items `browseKey="Tidal:MG/Tidal-Artist?artistid=17356"` + `contextMenuKey` met `artistid=17356` (geen playURL bij artist-items; 4× "Sting"-items met andere ids in dezelfde lijst → id-match vereist).
- `/Playlist` bevat géén `browseKey` per song (alleen de bestaande velden) → queue-route levert géén browseKey; ook hier zoeken.
- `StatusResponse` parseert al `album`/`artist` (elementen), maar `PlayerMedia` exposeert ze niet; `PlayQueueSong` heeft wél `Artist`/`Album` (`<art>`/`<alb>`).
- Bestaande infrastructuur: `MusicContentNode.Search/ResolveNext/Resolve` dekken paginering + speler-key-resolve al; `MusicContentEntry.Resolve()` lost een item op via diens eigen `browseKey`. `MusicContentEntry` exposeert `PlayURL` maar níét `BrowseKey`/`ContextMenuKey` (intern velden).
- Testpatroon: xUnit v3 + `HttpListener`-mock in `src/BluOsNadRemote.Blu4Net.Tests/MusicContentNodeRefreshTests.cs` (lokaal, geen hardware nodig); `Fixture.CreateReader`/`Fixture.Reader` voor XML.

## Assumptions and open decisions

- Aannames:
  - `{service}:` (bv. `Tidal:`) is een geldige second-level browse-key voor doorzoekbare services (documenteerde conventie; staat ook letterlijk in de doc-voorbeelden). Fout bij niet-toepasselijke services → duidelijke melding (zie onder), geen crash.
  - Het juiste id staat altijd in de `browseKey` (of als fallback `playURL`/`contextMenuKey`) van het matchende item; geobserveerd bij alle album- én artist-items.
  - De category-link in het zoek-menus herkennen via de on-escaped browseKey bevat `/Albums?` resp. `/Artists?` (de legacy-endpoint-path zit als tweede component in de speler-key; we *bouwen* geen key, we *kiezen* alleen tussen door de speler geleverde links). Fallback: geen link gevonden → duidelijke exception.
  - `Debug.WriteLine`-flow logging blijft in de code (user wil ze voor handmatig testen).
- Opgeloste decisions (user bevestigde defaults, 2026-09-10):
  - `MusicBrowserExtension.cs` **volledig verwijderen** — alle 3 methods bouwen gedeprecateerde keys; `GetNodeSongNode` is onbruikt. Als de song-variant later nodig is: zelfde zoek-patroon met `songid`-match.
  - Menu-item **verbergen wanneer naam ontbreekt** (id wél) — `HasMoreMenu` vereist id én naam; anders is de feature per definitie niet uitvoerbaar.

## Deviation (2026-09-10, user-bevestigd)

- **Geen zoek-flow.** Handmatige test (Colin) toonde dat de zoek-flow functioneel onvoldoende is: bij "Shape of You" staat het gespeelde album (`albumid=68883383`, Ed Sheeran-single) **niet** in de ~180 op relevantie gerangschikte zoekresultaten → id-matching vindt het niet. De zoek-flow is dus afgekeurd.
- **Eindoplossing (Option A):** via het **gedocumenteerde `/Browse`-endpoint** met de key in het **speler-eigen formaat** dat de speler zelf op album/artist-items levert:
  - album: `{service}:MG/{service}-Album?albumid={id}` (bv. `Tidal:MG/Tidal-Album?albumid=68883383`)
  - artiest: `{service}:MG/{service}-Artist?artistid={id}` (bv. `Tidal:MG/Tidal-Artist?artistid=3995478`)
  - Beeld verifieerd op de live speler: de keys leveren de juiste album-tracks resp. artiest-disco-menu; een onbekende id levert een geldige maar **lege** `<browse>` (geen exception). De niet-gebruikte `/Albums`/`/Artists`-direct endpoints (ondocumenteerd) worden **niet** gebruikt.
- **Gevolg voor scope:** alle zoek-flow-scaffolding viel af → `AlbumName`/`ArtistName` (Player/Queue/Browse), `PlayerMedia.Album`/`Artist`, de id-matcher + `MusicContentEntry`-intern-keys, `BrowseFindReason`, en de 3 nieuwe resx-keys (`AlbumNotFound`/`ArtistNotFound`/`ServiceNotSearchable`) zijn **niet** toegevoegd/teruggezet. Menu-items blijven op `id != null` (naam is niet langer nodig). Geen fallback opzoek-flow (user: niet nodig).
- **Behouden:** lege-body-guard in `BluChannel` (400+empty → `BluChannelException` met HTTP status); `MusicBrowserExtension.cs` verwijderd; `BrowseViewModel`-catch doet bij `BluChannelException` géén `Disconnect()` (speler antwoordde wél → content-issue, verbinding intact); `Debug.WriteLine`-stappen; docs-notitie.

## Design notes (oorspronkelijk, door Deviation hierboven ingetrokken)

- **Nieuwe protocol-methode in `MusicBrowser`** (Blu4Net, geen MAUI):
  - `FindAlbumNode(string service, string albumName, string albumId)` / `FindArtistNode(string service, string artistName, string artistId)`; gedeelde private helper:
    1. `serviceNode = BrowseContent($"{service}:")` → `searchKey = serviceNode.IsSearchable ? … : throw` (niet doorzoekbaar → `BluChannelException` met duidelijke melding).
    2. `searchNode = BrowseContent(searchKey, name)`.
    3. Category-link kiezen: eerste `searchNode.Entries`-item waarvan `Uri.UnescapeDataString(BrowseKey)` `/Albums?` resp. `/Artists?` bevat.
    4. Pagineren: `entry.Resolve()`, dan `node.ResolveNext()` zolang `HasNext`; op elke pagina zoeken naar item waarvan het query-deel van `BrowseKey` (fallback `PlayURL`, dan `ContextMenuKey`; via `HttpUtility.ParseQueryString`) `albumid=`/`artistid=` == target bevat.
    5. Match → `entry.Resolve()` (speler-browseKey, documenteerde weg) = de album/artist-pagina. Geen match na alle pagina's → `BluChannelException` ("not found").
    6. `Debug.WriteLine`-stappen (prefix `[MusicBrowser]`): service-key, searchKey, query, pagina's doorzocht, gevonden key.
  - `MusicContentEntry`: intern leesbare `ContextMenuKey` toevoegen (zelfde assembly; `InternalsVisibleTo` dekt tests). Publiek API blijft schoon.
- **Foutafhandeling in `BluChannel.SendRequest`** (Uri-pad): lege/whitespace-only body → `BluChannelException` met HTTP status in de melding (bv. "The player returned an empty response (HTTP 400)") i.p.v. `XmlReader` faal. Niet `EnsureSuccessStatusCode` (would player-`<error>`-meldingen bij niet-200 met body verliezen; die worden al correct geparsed).
- **App-layer:**
  - `PlayerMedia`: `Album`/`Artist` toevoegen (reeds geparsed in `StatusResponse`).
  - `PlayerViewModel`: `AlbumName`/`ArtistName` vullen in `UpdatePlayerMedia`; meegeven in de `//browse?…` query; `HasMoreMenu`/menu-constructie: item alleen bij id én naam.
  - `QueueViewModel`: `entry.Album`/`entry.Artist` meegeven in de query (zelfde query-naming).
  - `BrowseViewModel`: `AlbumName`/`artistName` in `ApplyQueryAttributes`; branch 3 van `LoadDataAsync` roept de nieuwe `MusicBrowser`-methods; `finally` wist ook de namen; exception-mapping: "not found" → `AppResources.AlbumNotFound`/`ArtistNotFound`, niet-doorzoekbaar → `AppResources.ServiceNotSearchable`, overig → `NoBrowsers` (en níét blind `Disconnect()` bij een browse-fout: alleen bij echte verbindingsproblemen — zie risico's). `Debug.WriteLine` vóór/na de resolve (node, entry count).
  - Resx: 3 nieuwe keys in `en-US` én `nl-NL` (+ `Designer.cs` auto-genereren).
- Alternatief afgewezen: queue-song → `browseKey` hopen (`/Playlist` levert die niet); naam-only matching (ambigue edities, pagina-1-bias); cache van search-results (scope-bloat).

## Risks and challenges

- **`BrowseViewModel`-catch doet nu `Disconnect()`** — bij een lege-body `BluChannelException` zou een transient 400 de (primaire) verbinding kwijtraken; na de fix moet de catch onderscheiden: `BluChannelException`/parse-fout → alleen `Title`, wél reconnect-proof. Let op bestaand gedrag voor echte netwerkfouten (those keep the dialog/disconnect).
- `nextKey`-loop bij grote artiesten (honderden albums) = veel pagina's; pagina-30 is de firmware-conventie; geen extra guard nodig maar wel `Debug.WriteLine` per pagina zodat de user het ziet.
- Services zonder `searchKey` (bv. `LocalMusic:`) of zonder `{service}:`-key (bv. `Spotify:` is een playURL-item) → flow faalt op stap 1/2 → duidelijke melding; menu-item blijft zichtbaar (id aanwezig) — geaccepteerd, user ziet een nette fout i.p.v. "Kon geen browsers ophalen".
- Category-link heuristic (unescape + `/Albums?`) is afgeleid van geobserveerde speler-keys; bij een exotische service kan de link afwijken → dan nette "not found"-fout, geen crash.
- `MusicBrowser` is een singleton per `BluPlayer` (root node); de nieuwe methods maken alleen korte `MusicContentNode`-ketens, die via `Parent` verwezen blijven — `HasParent=true` op de eind-pagina → GoBack doorloopt eerst de tussennodes (zoek-pagina's). UX-acceptabel (ook vandaag via `//browse` vanaf tab-root).
- Testen op Windows/`HttpListener` werkt al (bestaand patroon); geen nieuwe dependencies.

## Implementation checklist (uitgevoerd, zie Deviation)

- [x] `src/BluOsNadRemote.Blu4Net/Channel/BluChannel.cs` — lege-body-guard in `SendRequest(Uri, …)`: `BluChannelException` met HTTP status; bestaand `<error>`-gedrag ongewijzigd.
- [x] `src/BluOsNadRemote.Blu4Net/MusicBrowser.cs` — `GetAlbumNode(service, albumId)`/`GetArtistNode(service, artistId)`: één `BrowseContent` met de speler-formaat key + `[MusicBrowser]` `Debug.WriteLine`.
- [x] `src/BluOsNadRemote.App/Extensions/MusicBrowserExtension.cs` — verwijderd (incl. `GetNodeSongNode`, onbruikt).
- [x] `src/BluOsNadRemote.App/ViewModels/BrowseViewModel.cs` — branch 3 via nieuwe methods; catch: `BluChannelException` → géén `Disconnect()`; `Debug.WriteLine`.
- [x] `src/BluOsNadRemote.Blu4Net.Tests/MusicBrowserGetNodeTests.cs` — nieuw, `HttpListener`-patroon:
  - [x] album: correcte request-key (`Tidal:MG/Tidal-Album?albumid=…`), eind-node = album-tracks.
  - [x] artiest: correcte request-key (`Tidal:MG/Tidal-Artist?artistid=…`), eind-node = artiest-menu.
  - [x] onbekende id → lege node, géén exception.
  - [x] lege body (200 én 400) → `BluChannelException` met status in message (geen `XmlException`).
  - [x] `<error>`-root → `BluChannelException` met player-message.
- [x] `docs/Code-vs-API-v1.7-diffs.md` — notitie: legacy `/Albums?…`-keys gedeprecateerd door firmware (400+empty); zoek-flow afgekeurd (relevantie/cap); speler-key via `/Browse` is de oplossing.

### Niet uitgevoerd (gevolg van de Deviation, bewust)
- `MusicContentEntry` interne keys, `PlayerMedia.Album/Artist`, `PlayerViewModel`/`QueueViewModel` name-threading + id+naam menu-conditie, resx-keys `AlbumNotFound`/`ArtistNotFound`/`ServiceNotSearchable` — niet meer nodig zonder zoek-flow.

## Verification checklist

- [x] `dotnet build` zonder warnings.
- [x] `dotnet test src/BluOsNadRemote.Blu4Net.Tests` → 68/68 groen (via test-exe; op .NET 10 SDK moet de MTP-testhost rechtstreeks worden gedraaid, de oude VSTest-`dotnet test`-pad wordt afgekeurd).
- [x] Handmatig (user): Sting werkt, andere artiesten werken.
- [ ] Handmatig (user): Shape of You-geval (`albumid=68883383`) → album-pagina (de oorspronkelijke "not found"-reproducer).
- [ ] Geen commit (user testt eerst; pas op expliciet verzoek).

## Handoff notes

- De user (Colin) testt zelf op de live speler; geen hardware-test door de agent (AGENTS.md), en spaarzaam met requests aan de speler.
- Request-encoding van de nieuwe calls komt gratis goed: `BrowseContent` zet `key`/`q` in `NameValueCollection` → `parameters.ToString()` percent-encodeert (bestaand, getest gedrag).
- `MusicBrowser` root node (`BluPlayer.MusicBrowser`) is al geconnect bij `BluPlayer.Connect` (top-level browse); `GetAlbumNode` hoeft alleen de service-level browse extra te doen.
- `Accept-Language` loopt via `BluChannel.AcceptLanguage` (al configured); zoek-terms zijn service-naam in de speler-eigen spelling (`<alb>`/`<art>` resp. `status album/artist`).
- Planfile: niet committen totdat de user akkoord is na handmatige test.
