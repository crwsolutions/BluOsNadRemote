# XmlSerializer vervangen door handgeschreven XmlReader-parsers (Blu4Net)

- Status: Approved (2026-08-25, gebruiker: "Schakel naar Default en executeer")
- Plan file: `.alta/plans/2026-08-25-xmlreader-parsers.md`
- Created: 2026-08-25
- Task: Vervang runtime-XmlSerializer in `BluOsNadRemote.Blu4Net` door handgeschreven XmlReader-deserializers (één `Read(XmlReader)` per response type), voeg een xunit-testproject met fixture-golden-tests toe, en lever een rapport met de verschillen tussen de code en API-reference v1.7. Alleen deserializatie, geen serialisatie, geen runtime codegen.
- Git: niet ignored → commit dit plan-bestand met de bijbehorende implementation work.

## Objective
- XmlSerializer eruit: geen runtime-generate assemblies (trim/AOT-vriendelijk voor iOS/Android), sneller, minder machinery.
- Alleen deserializatie schrijven. Geen XmlWriter/serialisatie-code.
- Publieke API blijft identiek: `BluChannel`-methodes, alle POCO-velden/typen, `Task<LoadedResponse> LoadPreset`, `Task<ActionResponse> ActionURL`, `Task<object> PlayURL`.
- Geen dode code achterlaten: `derivedTypes`-plumbing, gedonkerde serializer-blocks in csproj, dead locals, en null-guards/Where-filters die na de migratie redundant worden, worden verwijderd.
- Nieuw: xunit-testproject met fixture-XML's + `Read`-golden-tests (beslissing gebruiker, 2026-08-25).
- Nieuw: rapport "code vs. API-reference v1.7" (`docs/Code-vs-API-v1.7-diffs.md`) met alle geconstateerde verschillen; te leveren als de implementatie klaar is.

## Context and evidence
- XmlSerializer staat alleen in `src/BluOsNadRemote.Blu4Net/Channel/BluChannel.cs` (de twee `SendRequest<T>`-overloads rond regel 71–92, incl. de `IDictionary<string, Type> derivedTypes`-dispatch en dubbele `StringReader`-pass voor polymorfe cases).
- 14 response-POCOs in `src/BluOsNadRemote.Blu4Net/Channel/*.cs`, allemaal geannotëerd met `[XmlRoot]`/`[XmlAttribute]`/`[XmlElement]`/`[XmlText]`/`[XmlInclude]`/`[XmlIgnore]`; de rest van de code (App, Nad4Net) gebruikt XmlSerializer niet.
- Polymorfe families: `LoadedResponse` (`loaded`/`state`), `ActionResponse` (`response`/`back`/`skip`/`love`/`ban`), en `PlayURL` (`loaded`/`playlist`/`state`/`addsong`, retourneert `object`).
- Long polling: `Status` (100s) en `SyncStatus` (180s) via `ILongPollingResponse.ETag`; die flow verandert niet.
- Referentie-doc: `docs/BluOS-Custom-Integration-API_v1.7.md` (v1.7, 2025-04-09, compleet: secties 1–13). Doc is niet foutloos; **waar doc en code divergeren is code leidend** (beslissing gebruiker).
- Repo heeft geen test-project; `dotnet`-build dekt alleen compilatie.

### Pre-scan: code vs. doc v1.7 (vondsten tijdens planning, te verifiëren/vervollendigen bij de implementatie)
- **`/Status` (doc 2.1) — in doc, níet in code:** `syncStat`, `mute`, `muteDb`, `muteVolume`, `name`, `sleep`, `canMovePlayback`, `stationImage`, `twoline_title1/2`, `groupName`/`groupVolume`, `action`-attribuut op `<action>` (`state="-1"` in voorbeeld 4.8).
- **`/SyncStatus` (doc 2.2) — in doc, níet in code:** `reconnecting` (attribuut op `<master>`), `mute`/`muteDb`/`muteVolume`, `icon`, `id`, `initialized`, `schemaVersion`, `group`, `syncStat`. **Conflict:** doc beschrijft `zoneMaster`/`zoneSlave` als boolese attributen; code leest `zoneController`-attribuut én een `zoneSlave`-*element* (met `channelName`, `model`, `modelName` — niet in doc). Code leidend.
- **`/Volume` (doc 3.x) — in doc, níet in code:** `muteDb`, `muteVolume`, `offsetDb`.
- **`/Playlist` (doc 5.1) — in doc, níet in code:** `fn` sub-element van `<song>`. (Code heeft extra, ongedocumenteerd: `trackstationid`, `similarstationid`.)
- **`/Presets` (doc 6.1) — in doc, níet in code:** root-attribuut `prid` (nuttig: cache-invalideatie-signaal). Code heeft extra, ongedocumenteerd: `volume` op `<preset>`.
- **`/Browse` (doc 7.1) — in doc, níet in code:** `parentKey`, `type`, `sid` op root; `parentKey` op `<category>`; `inputType` op `<item>` (staat in voorbeeld, niet in de tabel). Code vraagt nooit `withContextMenuItems=1`, maar de `Read` moet onbekende elementen (o.a. een eventueel `<contextMenu>`) `Skip()`-pen.
- **`/Action` (doc 4.8) — conflict:** doc zegt dat ban `<love skip="1">0</love>` retourneert; code kent óók root `ban` (`BanActionResponse`). Beide behouden.
- **In code, níet in CI-doc v1.7 (toegestaan, "ongedocumenteerde elementen negeren"):** o.a. `songid`/`trackstationid`/`artistid`/`albumid`/`is_preset`/`preset_name` op `<status>` (deels wél in doc-tabel), root `addsong` (komt van `/Add`, niet in CI-doc).
- **Endpoints/functionaliteit in doc, níet geïmplementeerd (non-goals, alleen rapporteren):** `/Move`, volume via `abs_db`/`db`-delta/`tell_slaves`, preset stap `+1`/`-1`, multi-slave `AddSlave`/`RemoveSlave`, `Reboot`, `Doorbell`, `Direct Input` (`inputIndex`/`inputTypeIndex`), `Bluetooth`-modi, `withContextMenuItems`.
- **Gedragsverschil (bewust kleine verbetering):** `/Browse`-foutrespons (root `<error>`): oude `XmlSerializer` → `null` (NRE-risico in `BrowseContent`); nieuwe parser → object met lege `Items`.

## Assumptions and open decisions
- **Besloten (2026-08-25):** xunit-testproject wél toevoegen, met exact deze packages: `coverlet.collector` 6.0.4, `Microsoft.NET.Test.Sdk` 18.0.1, `xunit.v3` 3.2.1, `xunit.runner.visualstudio` 3.1.5.
- **Besloten (2026-08-25):** na de implementatie een rapport met code-vs-doc-v1.7-verschillen opleveren (`docs/Code-vs-API-v1.7-diffs.md`) — geen velden toevoegen, alleen documenteren.
- Aanneem: publieke return types blijven (`Task<LoadedResponse>`, `Task<ActionResponse>`, `Task<object>`) — App-side krijgt geen API-break.
- Aanneem: géén nieuwe velden uit de doc toevoegen (bv. `syncStat`, `mute*`, `prid` op `<presets>`, `parentKey` op `<browse>`). De parsers skippen onbekende elementen/attributen; code is leidend.
- Aanneem: ActionURL houdt zowel root `love` als `ban` in de dispatch (zie pre-scan).

## Design notes
- Per response-POCO: `internal static T Read(XmlReader reader)`, reader staat op de root `StartElement`; nested types (`Song`, `Item`, `Category`, `Preset`, `Action`, `Master`, `Slave`, `ZoneSlave`, `PlaylistLoadedResponse`, `AddSongResponse`, …) krijgen ook een `Read`. Publieke surface groeit niet (internal).
- Eén klein shared-helperbestand `src/BluOsNadRemote.Blu4Net/Channel/XmlReaderExtensions.cs` met extension methods: `Attr(r, name)` (→ `null` indien afwezig), `ReadText(r)` (getrimd, `""` indien leeg), `ReadInt(r)` (`CultureInfo.InvariantCulture`, afwezig/leeg → `0`), `ReadIntOrNullOrThrow(r)` (voor `StatusResponse.Song` `int?`: leeg/whitespace → `null`), `ReadDouble(r)`.
- Onbekende elementen: `reader.Skip()` — doc 1.7 zegt expliciet dat ongedocumenteerde elementen aanwezig kunnen zijn en genegeerd moeten worden.
- Root-/elementnamen case-sensitive matchen (o.a. `SyncStatus` met hoofdletter).
- `BluChannel`: de private `SendRequest<T>`-overloads nemen `Func<XmlReader, T> deserialize` in plaats van `IDictionary<string, Type> derivedTypes`; de `derivedTypes`-overloads verdwijnen. Alle call-sites (~20) krijgen expliciet `X.Read` mee.
- Polymorfe dispatch: `internal static LoadedResponse Read(XmlReader)` op `LoadedResponse` en `internal static ActionResponse Read(XmlReader)` op `ActionResponse` (switch op rootnaam; onbekende root → throw, zoals nu). `PlayURL`-dispatch als private static local function in `BluChannel` (geen nieuw bestand, geen basis type).
- Gedrag dat overgenomen moet worden van XmlSerializer: ontbrekende string → `null`; ontbrekend/leeg value type → default (`0`/`false`); slechte numeriek → exception (helper gooit `FormatException` mét elementnaam); text-waarden getrimd.
- Testproject: `src/BluOsNadRemote.Blu4Net.Tests/` (net10.0, xunit.v3), fixture-XML's in `TestData/` (embedded of `LoadFrom`), één test per fixture per type; toevoegen aan `BluOsNadRemote.slnx`.
- Alternatieven afgewezen: source generator (te veel machinery voor 14 stabiele types), XDocument (bouwt volledige tree, 5–10x traag), build-time XmlSerializer codegen (blijft XmlSerializer; gebruiker wil er vanaf).

## Risks and challenges
- Subtiele gedragsverschillen (lege elementen, whitespace, culture-sensitive parsing) → mitigeren met fixtures uit echte captures + golden-tests.
- `/Browse` fout-respons (root `<error>`): bewust kleine verbetering (zie pre-scan); vermelden in commit-message.
- Lege `<item></item>` (TuneIn-special case): de XmlReader-loop slaat lege elementen vanzelf over → de `Where(element => element.Text != null)`-filter in `BrowseContent` wordt dode code en verdwijnt.
- Doc vs. code conflicts (bv. `zoneSlave` element vs. attribuut, `ban` vs. `love skip="1"`): code blijft leidend; conflicts expliciet in het diff-rapport.
- App-bindings: POCO-velden zijn publiek en worden door ViewModels gebruikt — NIET hernoemen, alleen attributen eraf.

## Implementation checklist
- [x] 1. Fixtures verzamelen: 26 XML-fixture's gebaseerd op de voorbeelden in `docs/BluOS-Custom-Integration-API_v1.7.md` (geen live-player-captures beschikbaar in deze sessie; `FILE_LOGGING`-flow is intact gebleven) → `src/BluOsNadRemote.Blu4Net.Tests/TestData/`.
- [x] 2. `src/BluOsNadRemote.Blu4Net/Channel/XmlReaderExtensions.cs` (nieuw): `Attr`, `AttrInt`, `AttrDouble`, `AttrBool`, `ReadRoot`, `ReadText`, `ReadInt`, `ReadIntOrNullOrThrow`, `ReadDouble`.
- [x] 3. `Read(XmlReader)` per type (allemaal in het bijbehorende bestaande bestand): `VolumeResponse`, `StateResponse`, `IdResponse`, `DeleteResponse`, `SavedResponse`, `PlaylistResponse`+`Song`, `PresetsResponse`+`Preset`, `AddSlaveResponse` (nested `Slave` uit `SyncStatusResponse.cs`), `AddSongResponse`, `StatusResponse`+`ActionsArray`+`Action`, `SyncStatusResponse`+`Master`+`Slave`+`ZoneSlave`, `BrowseContentResponse`+`Item`+`Category`, `LoadedResponse`+`PlaylistLoadedResponse` (incl. polymorfe `LoadedResponse.Read`-dispatch), `ActionResponse`+5 kinderen (incl. polymorfe `ActionResponse.Read`-dispatch).
- [x] 4. `BluChannel.cs`: `SendRequest`-plumbing herschrijven naar `Func<XmlReader, T>`; `derivedTypes`-overloads + `using System.Xml.Serialization;` verwijderd; dead local (`parameters`-splitting) in `ActionURL` verwijderd; TuneIn-`Where`-filter in `BrowseContent` verwijderd; null-guards die nu overbodig zijn verwijderen (`response.Songs ??= []` in `GetPlaylist`, `if (response.Presets == null)` in `GetPresets`, `if (response.Items == null)`-blok in `BrowseContent`) — parsers garanderen non-null arrays.
- [x] 5. Alle Channel-POCO's: `System.Xml.Serialization`-attributen en de bijbehorende `using` verwijderen.
- [x] 6. `src/BluOsNadRemote.Blu4Net/BluOsNadRemote.Blu4Net.csproj`: gedonkerde `UseXmlSerializerGenerator` / `Microsoft.XmlSerializer.Generator`-blocks verwijderen.
- [x] 7. Nieuw testproject `src/BluOsNadRemote.Blu4Net.Tests/`: net10.0, packages `coverlet.collector` 6.0.4, `Microsoft.NET.Test.Sdk` 18.0.1, `xunit.v3` 3.2.1, `xunit.runner.visualstudio` 3.1.5; ProjectReference naar Blu4Net (+ `InternalsVisibleTo`); toegevoegd aan `BluOsNadRemote.slnx`. 50 golden-tests, allemaal groen: o.a. leeg `<song></song>` → `null`, onbekend element → geskippt, `SyncStatus` case-sensitive, `<error>`-root → lege object, `contextMenu`-nested items geskippt, self-closing elementen.
- [x] 8. Diff-rapport schrijven: `docs/Code-vs-API-v1.7-diffs.md` — per endpoint/type: velden die in doc v1.7 staan maar níet in de code, velden die in de code staan maar níet in de doc (incl. conflicts zoals `zoneSlave` element vs. attribuut en `ban` vs. `love skip="1"`), en endpoints/functionaliteit uit de doc die níet geïmplementeerd zijn (non-goals). Code leidend.
- [x] 9. Samenvatting van het diff-rapport in de final report aan de gebruiker (kortste lijst + verwijzing naar het bestand).

## Verification checklist
- [x] `dotnet build BluOsNadRemote.slnx` compileert clean (Blu4Net + Tests + Nad4Net + App, 0 warnings/0 errors).
- [x] `dotnet test` groen (xunit, 50/50).
- [x] `grep -ri "XmlSerializer\|System\.Xml\.Serialization"` in `src/` (excl. `obj`/`bin`) → geen hits meer.
- [ ] Handmatige smoke-test tegen live player — **overgelaten aan gebruiker** (geen live player in deze sessie): Status/SyncStatus long polling, Play/Pause/Skip/Back, Volume/Mute, Playlist (list/length/clear/save), Presets (list/load), Browse (top-level + 2 niveaus + TuneIn lege items), PlayURL, ActionURL (back/skip/love/ban).
- [x] Self-review van `git diff`: publieke API ongewijzigd (alle `Task<…>`-signatures behouden), geen dode code, POCO-velden niet hernoemd.
- [x] Diff-rapport bestaat (`docs/Code-vs-API-v1.7-diffs.md`) en is consistent met de pre-scan hierboven.

## Handoff notes
- Lees dit plan + `docs/BluOS-Custom-Integration-API_v1.7.md`; waar doc en code afwijken: **code is leidend**.
- `Read`-methodes zijn `internal static` op publieke classes — publieke surface mag niet veranderen.
- `XmlReader.Create` met minimale settings (default; geen DTD), één pass over het XML (geen dubbele StringReader meer).
- Commit dit plan-bestand (`.alta/plans/2026-08-25-xmlreader-parsers.md`) én het diff-rapport samen met de implementation (`.alta/plans/` is nog niet gitignored).
- Het diff-rapport is een deliverable: pas klaar als het bestaat én de gebruiker er een samenvatting van krijgt.
