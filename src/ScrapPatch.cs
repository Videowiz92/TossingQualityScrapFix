using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace TossingQualityScrapFix;

/// <summary>
/// ZetDropifact.DropItem(body, inventory, ItemIndex index, float angle, bool scrap) removes the (Quality) item from the inventory but
/// picks the droplet from the vanilla Items.ScrapWhite/Green/Red/Yellow indexes, so the Quality tier is lost.
/// A transpiler redirects its CreateDroplet(ItemIndex, Vector3, float) call to <see cref="CreateDropletForScrap"/>, which swaps in the
/// scrap of the same Quality tier as the scrapped item and then calls the original CreateDroplet.
/// </summary>
internal static class ScrapPatch
{
	private static MethodInfo _createDropletInfo;
	private static Action<ItemIndex, Vector3, float> _createDroplet;

	public static void Apply(Harmony harmony, Type dropifact)
	{
		if (dropifact == null)
		{
			Plugin.Log.LogWarning("ZetArtifacts type ZetDropifact not found; Quality scrap fix not applied.");
			return;
		}

		MethodInfo dropItem = AccessTools.Method(dropifact, "DropItem",
			new[] { typeof(CharacterBody), typeof(Inventory), typeof(ItemIndex), typeof(float), typeof(bool) });
		_createDropletInfo = AccessTools.Method(dropifact, "CreateDroplet",
			new[] { typeof(ItemIndex), typeof(Vector3), typeof(float) });
		if (dropItem == null || _createDropletInfo == null)
		{
			Plugin.Log.LogWarning("ZetDropifact.DropItem / CreateDroplet(ItemIndex, ...) not found; Quality scrap fix not applied. ZetArtifacts may have changed.");
			return;
		}

		_createDroplet = (Action<ItemIndex, Vector3, float>)Delegate.CreateDelegate(typeof(Action<ItemIndex, Vector3, float>), _createDropletInfo);
		harmony.Patch(dropItem, transpiler: new HarmonyMethod(typeof(ScrapPatch), nameof(Transpiler)));
	}

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo replacement = AccessTools.Method(typeof(ScrapPatch), nameof(CreateDropletForScrap));
		int replaced = 0;
		foreach (CodeInstruction instruction in instructions)
		{
			if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) && instruction.operand as MethodInfo == _createDropletInfo)
			{
				// stack already holds (droplet index, position, angle); add the scrapped item (arg 2) and the scrap flag (arg 4)
				yield return new CodeInstruction(OpCodes.Ldarg_2);
				yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)4);
				yield return new CodeInstruction(OpCodes.Call, replacement);
				replaced++;
			}
			else
			{
				yield return instruction;
			}
		}

		if (replaced == 0)
		{
			Plugin.Log.LogWarning("ZetDropifact.DropItem no longer calls CreateDroplet(ItemIndex, ...); Quality scrap fix has no effect. ZetArtifacts may have changed.");
		}
		else
		{
			Plugin.Log.LogInfo("Quality scrap fix applied to the Artifact of Tossing.");
		}
	}

	private static void CreateDropletForScrap(ItemIndex dropIndex, Vector3 position, float angle, ItemIndex scrappedItem, bool scrap)
	{
		if (scrap && Plugin.FixQualityScrap.Value)
		{
			try
			{
				dropIndex = QualityBridge.MapScrap(scrappedItem, dropIndex);
			}
			catch (Exception ex)
			{
				Plugin.Log.LogWarning("Quality scrap remap failed; dropping vanilla scrap: " + ex.Message);
			}
		}

		_createDroplet(dropIndex, position, angle);
	}
}
