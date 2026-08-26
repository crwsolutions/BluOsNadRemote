# Code vs. BluOS Custom Integration API v1.7 — verschillen

- Referentie: `docs/BluOS-Custom-Integration-API_v1.7.md` (v1.7, 2025-04-09)
- Onderwerp: `src/BluOsNadRemote.Blu4Net` (handgeschreven `XmlReader`-deserializers na de migratie van 2026-08-25)
- Beslissing: **waar doc en code divergeren is de code leidend.** Nieuwe velden uit de doc zijn *niet* toegevoegd; de parsers skippen onbekende elementen en attributen (de doc zelf zegt: "Undocumented responses should be ignored" / "Other elements may be present and should be ignored").
- Scope: alleen deserialisatie van response-XML. Geen serialisatie, geen nieuwe endpoints.

## 1. /Status (doc 2.1)

### In doc v1.7, níet in de code
| Element / attribuut | In doc-tabel? | Opmerking |
|---|---|---|
| `syncStat` | ja | Signaal dat /SyncStatus is veranderd; bruikbaar om dubbel long-pollen te vermijden |
| `mute` | ja | Mute-state (1 = gemute) |
| `muteDb` | ja | Unmuted volume in dB bij mute |
| `muteVolume` | ja | Unmuted volume (0–100) bij mute |
| `name` | ja | Titel track (duplicatie van `title1`) |
| `sleep` | ja | Minuten resterend tot sleep-timer; in voorbeeld leeg (`<sleep/>`) |
| `canMovePlayback` | ja | `true` indien content naar andere player verplaatst kan worden |
| `stationImage` | ja | Afbeelding van radio-station |
| `twoline_title1` / `twoline_title2` | ja | Twee-rijns now-playing-metadata |
| `groupName` | ja | Naam van de group (alleen bij primary) |
| `groupVolume` | ja | Volume van de group (alleen bij primary) |
| `alarmsecondsremaining` | ja | Seconden resterend bij alarm-playback |
| `notifyurl` | ja | URL voor pop-up notificatie |
| `battery` | ja | Sub-element met `level`/`charging`/`icon` |
| `action` (rij in tabel) | ja | Verwijst naar sectie 4.8; code leest `<actions>/<action>` (zie conflict hieronder re `state`) |
| `cursor`, `fn`, `indexing`, `mid`, `mode`, `sid` | nee (alleen in voorbeeld) | Alleen in het doc-voorbeeld, niet in de tabel; parser slaat ze over |
| `state`-attribuut op `<action>` (bv. `state="-1"`) | voorbeeld 4.8 | `StatusResponse.Action` leest alleen `icon`/`name`/`notification`/`text`/`url` |

### In de code, níet in doc v1.7 (elementen op `<status>`)
`songid`, `trackstationid`, `artistid`, `albumid`, `is_preset`, `preset_name`.
Let op: `songid`/`artistid`/`albumid` wél gedocumenteerd als attributen van `<song>` in /Playlist (5.1); op `<status>` zelf staat ze in v1.7 nergens.

## 2. /SyncStatus (doc 2.2)

### In doc v1.7, níet in de code
| Element / attribuut | In doc-tabel? | Opmerking |
|---|---|---|
| `icon` | ja | Player-icon URL (root-attribuut) |
| `id` | ja | Player IP:port (root-attribuut) |
| `initialized` | ja | `true` indien player al opgezet is |
| `schemaVersion` | ja | Software schema versie |
| `group` | ja | Groepsnaam |
| `syncStat` | ja | Zie `syncStat` bij /Status |
| `model` | ja | Player model id (code leest alleen `modelName`) |
| `mute` / `muteDb` / `muteVolume` | ja | Zie /Status |
| `reconnecting` (attribuut op `<master>`) | ja | `true` indien reconnecten met primary |
| `outlevel` | nee (alleen in voorbeeld) | Parser slaat over |

### Conflict: fixed group (zone)
- **Doc:** `zoneMaster` en `zoneSlave` als boolese *root-attributen*; `zone` = naam fixed group.
- **Code (leidend):** `zoneController`-attribuut (root), `zone`-attribuut, `zoneUngroup`-attribuut (ungroup-URL) én een `zoneSlave`-*element* met attributen `id`, `port`, `zoneSlave` (bool), `channelName`, `name`, `model`, `modelName`.
- `channelName` (root en op `zoneSlave`) staat in v1.7 nergens (komt uit de oudere NAD CI API v1.0).

## 3. /Volume (doc 3.1–3.5)

### In doc v1.7, níet in de code
| Attribuut | Waar in doc | Opmerking |
|---|---|---|
| `offsetDb` | 3.1-voorbeeld, tabellen 3.2/3.3/3.4 | Offset in dB |
| `muteDb` | tabellen 3.1/3.4 | Unmuted volume in dB |
| `muteVolume` | tabellen 3.1/3.4 | Unmuted volume (0–100) |

### In de code, níet in doc
Geen.

### Niet geïmplementeerd (non-goal)
Volume zetten via `abs_db` / relatieve `db`-delta / `tell_slaves` (3.1–3.3). Code ondersteunt alleen `level` en `mute`.

## 4. /Play, /Pause, /Stop, /Skip, /Back, /Shuffle, /Repeat (doc 4.1–4.7)

- Response-structuren komen overeen (`<state>`, `<id>`, `<playlist>`).
- Doc 4.1 noemt ook `/Play?url=<encodedStreamURL>` (custom stream) — er is geen methode die deze aanroep bouwt; `BluChannel.PlayURL` roept een verkregen `playURL` direct aan (dat is de aanbevolen weg uit doc 7.1 "A URI that may be invoked directly").
- Doc 4.1/4.4/4.5: `id` wordt samen met `seek` gestuurd (`/Play?seek=seconds&id=trackid`); `BluChannel.PlayByID` stuurt alleen `id`.

## 5. /Action — streaming radio acties (doc 4.8)

### Conflict: ban
- **Doc:** ban geeft `<love skip="1">0</love>` terug.
- **Code (leidend):** dispatch kent beide root-elementen: `love` (`LoveActionResponse`) én `ban` (`BanActionResponse`). Beiden blijven.

### Overig
- `response` (notificatie-tekst), `skip`, `back` komen overeen.
- `<action>`-elementen in /Status hebben in doc-voorbeeld 4.8 een `state`-attribuut (`state="-1"`) die de code niet leest (zie §1).

## 6. /Playlist (doc 5.1)

### In doc v1.7, níet in de code
| Element / attribuut | Opmerking |
|---|---|
| `id` (attribuut op `<playlist>`) | Unieke queue id; in alle doc-voorbeelden aanwezig (`id="1054"`); `PlaylistResponse` heeft er geen veld voor |
| `fn` (sub-element van `<song>`) | In listing-voorbeeld; parser slaat het over |

### Doc-interne inconsistentie
Het "play queue status"-voorbeeld in 5.1 toont `<length>`, `<id>`, `<name>`, `<modified>` als **kind-elementen**; het listing-voorbeeld en de echte firmware gebruiken **attributen** (zoals de code leest). Code is leidend.

### In de code, níet in doc v1.7 (attributen op `<song>`)
`trackstationid`, `similarstationid` (ongedocumenteerd; `songid`/`artistid`/`albumid` wél gedocumenteerd in 5.1).

## 7. /Presets (doc 6.1)

### In doc v1.7, níet in de code
| Attribuut | Opmerking |
|---|---|
| `prid` (op `<presets>`) | Unieke preset-id; cache-invalideatie-signaal (matcht `<prid>` in /Status) |

### In de code, níet in doc v1.7
| Attribuut | Opmerking |
|---|---|
| `volume` (op `<preset>`) | Opgeslagen volume van het preset; afwezig → velddefault `-1` |

### Niet geïmplementeerd (non-goal)
Preset-stappen `/Preset?id=+1` / `?id=-1` (6.2); `LoadPreset(int id)` doet alleen expliciete id's.

## 8. /Browse (doc 7.1)

### In doc v1.7, níet in de code
| Element / attribuut | In doc-tabel? | Opmerking |
|---|---|---|
| `parentKey` (root en `<category>`) | ja | Alternatieve back-navigatie |
| `type` (root) | ja | `menu` / `contextMenu` / `albums` / … |
| `sid` (root) | nee (alleen in voorbeelden) | Service id |
| `service` (root) | nee (alleen in voorbeelden) | Service id (duplicatie van `serviceName`) |
| `inputType` (op `<item>`) | nee (alleen in top-level-voorbeeld, bv. `spdif`) | Type direct input |
| `<contextMenu>` (sub-element van `<item>` bij `withContextMenuItems=1`) | ja (sectie 7.1) | Code vraagt deze parameter nooit; de parser *skippt* een eventueel `contextMenu`-element expliciet |

### In de code, níet in doc v1.7
Geen — alle gelezen attributen van `browse`/`item`/`category` staan in de doc.

### Gedragsverschil (bewuste verbetering)
Foutrespons (root `<error>` met `<message>`/`<detail>`):
- Oude `XmlSerializer`: `null` → NullReferenceException-risico in `BrowseContent`.
- Nieuwe parser: object met lege `Items`/`Categories`-arrays (getest: `Read_ErrorRoot_ReturnsEmptyResponse`).

## 9. /AddSlave, /RemoveSlave (doc 8.1–8.4)

- Enkel-slave varianten komen overeen (`<addSlave><slave id=… port=…/></addSlave>`, respectievelijk het /SyncStatus-respons).
- **Niet geïmplementeerd (non-goal):** multi-slave `slaves`/`ports`-parameters (8.2 en 8.4).
- **In de code, níet in doc v1.7:** `channelMode`/`slaveChannelMode`/`group`-parameters voor stereo pairs (`AddSlave(address, port, createStereoPair, slaveChannel, groupName)`) — afkomstig van de oudere NAD CI API, niet in v1.7.
- **In de code, níet in doc v1.7:** `ZoneUngroup(url)` voor fixed groups (v1.7 zegt expliciet "fixed grouping … is out of scope for this document", maar beschrijft de zone-attributen wel in 2.2 — zie conflict §2).

## 10. Endpoints / functionaliteit uit de doc die níet geïmplementeerd zijn (non-goals)

| Doc-sectie | Onderdeel |
|---|---|
| 3.1–3.3 | Volume via `abs_db`, relatieve `db`-delta, `tell_slaves` |
| 4.1 | `/Play?url=<encodedStreamURL>` (custom stream via /Play) |
| 5.3 | `/Move` (track verplaatsen in queue) |
| 6.2 | Preset-stappen `id=+1` / `id=-1` |
| 8.2 / 8.4 | Multi-slave `AddSlave`/`RemoveSlave` |
| 9.1 | `/reboot` |
| 10.1 | `/Doorbell` |
| 11.1 / 11.2 | Direct input (`inputIndex`, `inputTypeIndex`, `/RadioBrowse?service=Capture`) |
| 12.1 | Bluetooth-modi (`/audiomodes?bluetoothAutoplay=…`) |
| 7.1 | `/Browse` met `withContextMenuItems=1` (parser kan het wél parsen: `contextMenu` wordt geskippt) |
| 13.1 | Lenbrook Service Discovery (LSDP) — code gebruikt mDNS via `Zeroconf` |

## 11. Extra response-type in de code (niet in CI-doc v1.7)

- `<addsong id=… count=… length=…>` — retourneert door `/Add` (zie `http/player.http`); niet in de CI-doc v1.7. `AddSongResponse` hangt mee in de `PlayURL`-dispatch.

## 12. Latente issue in de oude code (behavioral note)

Doc 6.2 belooft voor radio-presets `<state>stream</state>` als antwoord op `/Preset`. De oude `LoadPreset`-dispatch kende `state` → `StateResponse`, maar `StateResponse` is géén subklasse van `LoadedResponse` → de runtime-cast gooide een `InvalidCastException`. De nieuwe `LoadedResponse.Read` weert een `state`-root daarom expliciet af met een `InvalidOperationException` (zelfde gedragklasse, duidelijkere fout). `PlayURL` (retourneert `object`) behandelt `state` wél, zoals vóór de migratie.

## 13. Overige gedragsverschillen parser vs. XmlSerializer

| Onderwerp | XmlSerializer (oud) | XmlReader-parser (nieuw) |
|---|---|---|
| Ontbrekende string-attributen/elementen | `null` | `null` (zelfde) |
| Ontbrekende/lege value-type velden | `default` (0/false) | `0` / `false` (zelfde) |
| Lege `<song></song>` in /Status | — | `null` (`StatusResponse.Song` is `int?`, "can be empty") |
| Slechte numerieke waarde | `FormatException`/`ArgumentException` (onduidelijk) | `FormatException` mét elementnaam |
| Culture-sensitive parsing | `CurrentCulture` | Altijd `InvariantCulture` |
| Text-waarden | niet getrimd | getrimd |
| Onbekende root bij dispatch | `Exception("Encountered invalid xml")` of `InvalidCastException` | `InvalidOperationException("Encountered invalid xml root element <…>")` |
| Root-/elementnamen | case-sensitive (`[XmlRoot("SyncStatus")]` matcht `syncstatus` niet) | expliciet case-sensitive (o.a. `SyncStatus` met hoofdletter, getest) |
| `<error>`-root bij /Browse | `null` | object met lege arrays (zie §8) |
| Self-closing elementen (`<skip/>`, `<back/>`, `<item/>`) | ok | ok (met expliciete `IsEmptyElement`-guards) |
