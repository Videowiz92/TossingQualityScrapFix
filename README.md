# TossingQualityScrapFix

A side-loaded fix for [ZetArtifacts](https://thunderstore.io/package/William758/ZetArtifacts/)' **Artifact of Tossing** (Risk of Rain 2): scrapping a
[Quality](https://thunderstore.io/package/Goorakh/Quality/) item now drops the matching **Quality scrap** instead of vanilla scrap.
It patches ZetArtifacts in memory with [Harmony](https://github.com/BepInEx/HarmonyX); no ZetArtifacts file is modified.
Written against ZetArtifacts **1.6.6** and Quality **0.8.5**.

**For the ZetArtifacts author:** the change to build this into ZetArtifacts is in **[docs/UPSTREAM.md](docs/UPSTREAM.md)**.

## How it works
`ZetDropifact.DropItem` removes the Quality item from the inventory but chooses `Items.ScrapWhite/Green/Red/Yellow` (vanilla) for the droplet.
`src/ScrapPatch.cs` transpiles that method so its `CreateDroplet(ItemIndex, ...)` call goes through `QualityBridge.MapScrap`, which asks the Quality mod
(`QualityCatalog.GetQualityTier` / `GetItemIndexOfQuality`, by reflection) for the scrap of the same tier. Neither ZetArtifacts nor Quality is referenced at
compile time, so if either renames something the fix logs a warning and disables itself rather than crashing.

## Layout
```
src/            Plugin.cs (entry, config, Quality bridge), ScrapPatch.cs (Harmony transpiler)
thunderstore/   manifest, store README, changelog, icon
docs/           UPSTREAM.md - note for the ZetArtifacts author
Local.props.example   copy to Local.props and set the game / BepInEx paths
```

## Building
Requires the .NET SDK (8+).
1. Copy `Local.props.example` to `Local.props` and set `RoR2ManagedDir` and `BepInExCoreDir`.
2. `dotnet build src -c Release` -> `src/bin/Release/netstandard2.1/TossingQualityScrapFix.dll`.
3. For Thunderstore, zip that DLL with the files in `thunderstore/` (manifest.json, README.md, CHANGELOG.md, icon.png) at the root of the zip.

## Testing in game
With ZetArtifacts, Quality and this mod installed (and the Artifact of Tossing enabled):
1. Get a Quality item (e.g. an Epic white), RMB it in the inventory (LeftAlt + RMB if you enabled `AltScrap`).
2. The dropped scrap is the Epic scrap of the same colour; a normal item still drops normal scrap.
3. Set `FixQualityScrap = false`: vanilla scrap drops again.
4. Log shows `Quality scrap fix applied to the Artifact of Tossing.` at startup and no warnings.

## AI disclosure
The code and docs were written with Claude's help. The Thunderstore icon is a placeholder; replace it with your own.

## License
[MIT](LICENSE).
