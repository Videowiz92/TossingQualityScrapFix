# TossingQualityScrapFix

A small side-loaded fix for **[ZetArtifacts](https://thunderstore.io/package/William758/ZetArtifacts/)**' *Artifact of Tossing*, for players who also use the
**Quality** (ItemQualities) mod. It patches ZetArtifacts in memory; no ZetArtifacts file is changed, and removing this mod restores the original behaviour.

## The problem
With the Artifact of Tossing you can scrap an item from the inventory (RMB, or LeftAlt + RMB depending on your ZetArtifacts config).
If the item has a Quality tier (e.g. an *Epic* Soldier's Syringe), the scrap that drops is plain vanilla scrap, so the quality is lost.

## What this does
The scrap now matches the item's Quality tier: scrapping an Epic white item drops Epic White Scrap, a Legendary green drops Legendary Green Scrap, and so on
(white, green, red and boss scrap). Items without a Quality tier behave exactly as before. Lunar scrap has no Quality variant in the Quality mod, so it stays as-is.

| Config (`com.videowiz92.tossingqualityscrapfix.cfg`) | Default | |
|---|---|---|
| `FixQualityScrap` | on | Turn the fix off without uninstalling. |

## Requirements and multiplayer
- Requires ZetArtifacts (written against 1.6.6) and [Quality](https://thunderstore.io/package/Goorakh/Quality/) (0.8.5); both install automatically as dependencies.
  The code treats Quality as optional, so if you remove it manually this mod simply does nothing.
- The item is scrapped on the host, so **the host needs this mod**. Clients do not.
- If a future ZetArtifacts changes the code this patches, the fix logs a warning and turns itself off instead of breaking anything.

## For the ZetArtifacts author
This is a standalone fix so ZetArtifacts itself did not have to change. If you would like to build it in, the change is a few lines in
`ZetDropifact.DropItem`; see the source repository's `docs/UPSTREAM.md`. Feel free to use it, and once it is in ZetArtifacts this mod can be retired.

## Source
https://github.com/Videowiz92/TossingQualityScrapFix
