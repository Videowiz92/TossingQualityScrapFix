using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using RoR2;

namespace TossingQualityScrapFix;

[BepInPlugin(Guid, Name, Version)]
[BepInDependency(ZetGuid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(QualityBridge.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
	public const string Guid = "com.videowiz92.tossingqualityscrapfix";
	public const string Name = "TossingQualityScrapFix";
	public const string Version = "1.0.0";

	public const string ZetGuid = "com.TPDespair.ZetArtifacts";
	private const string DropifactType = "TPDespair.ZetArtifacts.ZetDropifact";

	internal static ManualLogSource Log;
	internal static ConfigEntry<bool> FixQualityScrap;

	private Harmony _harmony;

	private void Awake()
	{
		Log = Logger;
		FixQualityScrap = Config.Bind("General", "FixQualityScrap", true,
			"When true, scrapping a Quality item with the Artifact of Tossing (ZetArtifacts) drops the matching Quality scrap " +
			"(e.g. an Epic white item gives Epic White Scrap) instead of plain vanilla scrap.");

		if (!Chainloader.PluginInfos.TryGetValue(ZetGuid, out PluginInfo zet) || zet.Instance == null)
		{
			Log.LogError("ZetArtifacts is not loaded; nothing to patch.");
			enabled = false;
			return;
		}

		string version = zet.Metadata.Version.ToString();
		if (version != "1.6.6")
		{
			Log.LogWarning("ZetArtifacts " + version + " is installed but this fix was written against 1.6.6. It is skipped (with a warning) if the code it patches has changed.");
		}

		_harmony = new Harmony(Guid);
		try
		{
			ScrapPatch.Apply(_harmony, zet.Instance.GetType().Assembly.GetType(DropifactType, throwOnError: false));
		}
		catch (Exception ex)
		{
			Log.LogError("Quality scrap patch failed and was skipped: " + ex);
		}
	}

	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}
}

/// <summary>Reflection access to the Quality mod (ItemQualities), so it stays an optional dependency.</summary>
internal static class QualityBridge
{
	public const string PluginGuid = "com.Gorakh.ItemQualities";

	private static bool _resolved;
	private static MethodInfo _getTier;
	private static MethodInfo _getItemOfQuality;
	private static object _tierNone;

	public static bool Available
	{
		get
		{
			if (!_resolved)
			{
				Resolve();
			}
			return _getTier != null && _getItemOfQuality != null;
		}
	}

	private static void Resolve()
	{
		_resolved = true;
		try
		{
			if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out PluginInfo info) || info.Instance == null)
			{
				return;
			}
			Assembly asm = info.Instance.GetType().Assembly;
			Type catalog = asm.GetType("ItemQualities.QualityCatalog", throwOnError: false);
			Type tier = asm.GetType("ItemQualities.QualityTier", throwOnError: false);
			if (catalog == null || tier == null)
			{
				Plugin.Log.LogWarning("ItemQualities.QualityCatalog / QualityTier not found; Quality scrap fix disabled.");
				return;
			}
			_getTier = catalog.GetMethod("GetQualityTier", new[] { typeof(ItemIndex) });
			_getItemOfQuality = catalog.GetMethod("GetItemIndexOfQuality", new[] { typeof(ItemIndex), tier });
			_tierNone = Enum.ToObject(tier, -1);
			if (_getTier == null || _getItemOfQuality == null)
			{
				Plugin.Log.LogWarning("QualityCatalog.GetQualityTier / GetItemIndexOfQuality not found; Quality scrap fix disabled.");
				_getTier = null;
				_getItemOfQuality = null;
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning("Could not bind to the Quality mod; Quality scrap fix disabled: " + ex.Message);
			_getTier = null;
			_getItemOfQuality = null;
		}
	}

	/// <summary>Returns the Quality variant of <paramref name="scrap"/> that matches the quality of <paramref name="source"/>,
	/// or <paramref name="scrap"/> unchanged when the source is not a Quality item / that scrap has no Quality variant.</summary>
	public static ItemIndex MapScrap(ItemIndex source, ItemIndex scrap)
	{
		if (!Available)
		{
			return scrap;
		}
		object tier = _getTier.Invoke(null, new object[] { source });
		if (tier == null || tier.Equals(_tierNone))
		{
			return scrap;
		}
		object result = _getItemOfQuality.Invoke(null, new object[] { scrap, tier });
		return result is ItemIndex mapped && (int)mapped != -1 ? mapped : scrap;
	}
}
