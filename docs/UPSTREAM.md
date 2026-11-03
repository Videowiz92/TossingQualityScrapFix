# Note for the ZetArtifacts author: building the Quality scrap fix into ZetArtifacts

Hello! This small mod exists only to fix one thing in the **Artifact of Tossing** so ZetArtifacts did not have to change. If you would rather
have it in ZetArtifacts itself, this is the whole change, and this mod can then be retired. Take it, change it, or ignore it as you like.

## The problem

With the Quality mod (`com.Gorakh.ItemQualities`), a Quality item is a **separate ItemIndex** (e.g. "Soldier's Syringe (Epic)") with its own stack in
`inventory.permanentItemStacks`. In `ZetDropifact.DropItem(CharacterBody, Inventory, ItemIndex index, float angle, bool scrap)` the scrap is chosen
purely from the tier of that item:

```csharp
case 0: val = Items.ScrapWhite.itemIndex; break;
case 1: val = Items.ScrapGreen.itemIndex; break;
case 2: val = Items.ScrapRed.itemIndex;   break;
case 4: val = Items.ScrapYellow.itemIndex; break;
```

These are the **vanilla** scrap indexes, so the Quality tier of the removed item is lost. The Quality mod has Quality variants of those scraps
(`ScrapWhite/Green/Red/Yellow` item groups), and its own scrapper does the same mapping (`QualityCatalog.GetScrapIndexForPickup`).

## The fix

After `val` is chosen in the `if (scrap)` block, map it to the scrap of the same Quality tier as `index`:

```csharp
// after the switch, before `if ((int)val == -1) return false;`
val = QualityCompat.ToQualityScrap(index, val);
```

with a soft-dependency helper (so ZetArtifacts still works without Quality). Either reference `ItemQualities.dll` and guard with
`Chainloader.PluginInfos.ContainsKey("com.Gorakh.ItemQualities")`, or use reflection as this mod does:

```csharp
internal static class QualityCompat
{
    private static bool _resolved;
    private static MethodInfo _getTier, _getItemOfQuality;
    private static object _none;

    public static ItemIndex ToQualityScrap(ItemIndex source, ItemIndex scrap)
    {
        if (!_resolved) Resolve();
        if (_getTier == null) return scrap;

        object tier = _getTier.Invoke(null, new object[] { source });   // QualityTier, -1 == None
        if (tier == null || tier.Equals(_none)) return scrap;           // not a Quality item

        // returns `scrap` itself when that scrap has no Quality variant (e.g. lunar scrap)
        object mapped = _getItemOfQuality.Invoke(null, new object[] { scrap, tier });
        return mapped is ItemIndex i && (int)i != -1 ? i : scrap;
    }

    private static void Resolve()
    {
        _resolved = true;
        if (!Chainloader.PluginInfos.TryGetValue("com.Gorakh.ItemQualities", out var info) || info.Instance == null) return;
        var asm = info.Instance.GetType().Assembly;
        var catalog = asm.GetType("ItemQualities.QualityCatalog");
        var tierType = asm.GetType("ItemQualities.QualityTier");
        _getTier = catalog?.GetMethod("GetQualityTier", new[] { typeof(ItemIndex) });
        _getItemOfQuality = catalog?.GetMethod("GetItemIndexOfQuality", new[] { typeof(ItemIndex), tierType });
        _none = tierType == null ? null : Enum.ToObject(tierType, -1);
    }
}
```

If you reference ItemQualities directly instead, it is simply:

```csharp
var tier = QualityCatalog.GetQualityTier(index);
if (tier != QualityTier.None)
    val = QualityCatalog.GetItemIndexOfQuality(val, tier);
```

Add `[BepInDependency("com.Gorakh.ItemQualities", BepInDependency.DependencyFlags.SoftDependency)]` so Quality loads first.

## Other things worth knowing

- **Equipment** is not affected: this concerns item scrapping only.
- The item-count check (`GetRealItemCount`) and `RemoveItemPermanent(index, 1)` already use the Quality ItemIndex, so only the scrap choice needed changing.
- `IsScrapable` / `ValidDropRequest` already work with Quality items (their `ItemDef.tier` matches the base item), which is why scrapping works at all.
- Lunar scrap (`ScrapLunar`) has no Quality variant in the Quality mod, so `GetItemIndexOfQuality` returns it unchanged, which is the right fallback.
- The scrap happens on the server (`HandleClientRequest` / host click), so only the host needs the fix.

## How this mod applies it (for reference)

`src/ScrapPatch.cs` uses a Harmony transpiler on `ZetDropifact.DropItem` that redirects its `CreateDroplet(ItemIndex, Vector3, float)` call to a method
which maps the scrap through `QualityBridge.MapScrap(scrappedItem, scrapIndex)` when `scrap == true`, then calls the original `CreateDroplet`.
