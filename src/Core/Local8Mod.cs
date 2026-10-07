using System;
using System.Collections.Generic;
using Modding;
using UnityEngine;

namespace KO.HollowKnight8;

public sealed class Local8Mod : Mod, IGlobalSettings<Local8Settings>, ILocalSettings<Local8SaveData>, IMenuMod, IMod, Modding.ILogger, ITogglableMod
{
	public const string Id = "komods.hollowknight.local8";

	public const string Version = ReleaseInfo.Version;

	internal static Local8Mod Instance;

	internal static Local8Settings Settings = new Local8Settings();

	internal static Local8SaveData Save = new Local8SaveData();

	private Local8Runtime runtime;

	public bool ToggleButtonInsideMenu => false;

	public void OnLoadLocal(Local8SaveData data)
	{
		Save = data ?? new Local8SaveData();
	}

	public Local8SaveData OnSaveLocal()
	{
		if ((bool)runtime)
		{
			Charms.Save(runtime.Session);
		}
		return Save;
	}

	public Local8Mod()
		: base(ReleaseInfo.Name)
	{
		Instance = this;
	}

	public override string GetVersion()
	{
		return ReleaseInfo.Version;
	}

	public override int LoadPriority()
	{
		return 10;
	}

	public override void Initialize()
	{
		if (!runtime)
		{
			Settings.Ensure();
			GameObject gameObject = new GameObject(ReleaseInfo.Name);
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			runtime = gameObject.AddComponent<Local8Runtime>();
			runtime.Boot(this);
			Log(ReleaseInfo.Label + " cargado para Hollow Knight 1.5.78.11833 / Modding API.");
		}
	}

	public void Unload()
	{
		if ((bool)runtime)
		{
			UnityEngine.Object.Destroy(runtime.gameObject);
		}
		runtime = null;
		Hooks.Uninstall();
		Instance = null;
	}

	public void OnLoadGlobal(Local8Settings settings)
	{
		Settings = settings ?? new Local8Settings();
		Settings.Ensure();
		PvpRoundClock.Migrate();
	}

	public Local8Settings OnSaveGlobal()
	{
		if ((bool)runtime)
		{
			runtime.ExportSettings();
		}
		return Settings;
	}

	public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
	{
		KeyCode[] panelKeys = MenuInput.Choices;
		string[] values = Array.ConvertAll(panelKeys, MenuInput.Display);
		List<IMenuMod.MenuEntry> list = new List<IMenuMod.MenuEntry>();
		list.Add(new IMenuMod.MenuEntry(UiLocalization.Get("mod.hud"), new string[2]
		{
			UiLocalization.Get("hud.full"),
			UiLocalization.Get("hud.basic")
		}, UiLocalization.Get("mod.adaptive"), delegate(int i)
		{
			Settings.BasicHUD = i == 1;
			if ((bool)runtime)
			{
				runtime.BasicHud.Value = i == 1;
			}
		}, () => ((!runtime) ? Settings.BasicHUD : runtime.BasicHud.Value) ? 1 : 0));
		list.Add(new IMenuMod.MenuEntry(UiLocalization.Get("mod.labels"), new string[2]
		{
			UiLocalization.Get("common.hidden"),
			UiLocalization.Get("common.visible")
		}, UiLocalization.Get("mod.labels_tip"), delegate(int i)
		{
			Settings.ShowLabels = i == 1;
			if ((bool)runtime)
			{
				runtime.ShowLabels.Value = i == 1;
			}
		}, () => ((!runtime) ? Settings.ShowLabels : runtime.ShowLabels.Value) ? 1 : 0));
		list.Add(new IMenuMod.MenuEntry(UiLocalization.Get("mod.camera"), new string[2]
		{
			UiLocalization.Get("common.disabled"),
			UiLocalization.Get("common.enabled")
		}, UiLocalization.Get("mod.camera_tip"), delegate(int i)
		{
			Settings.ZoomByPlayerCount = i == 1;
			if ((bool)runtime)
			{
				runtime.ZoomByPlayerCount.Value = i == 1;
			}
		}, () => ((!runtime) ? Settings.ZoomByPlayerCount : runtime.ZoomByPlayerCount.Value) ? 1 : 0));
		list.Add(new IMenuMod.MenuEntry(UiLocalization.Get("mod.respawn"), new string[2]
		{
			UiLocalization.Get("common.disabled"),
			UiLocalization.Get("common.enabled")
		}, UiLocalization.Get("mod.respawn_tip"), delegate(int i)
		{
			Settings.TimedRespawn = i == 1;
			if ((bool)runtime)
			{
				runtime.TimedRespawn.Value = i == 1;
			}
		}, () => ((!runtime) ? Settings.TimedRespawn : runtime.TimedRespawn.Value) ? 1 : 0));
		list.Add(new IMenuMod.MenuEntry(UiLocalization.Get("mod.difficulty"), new string[4]
		{
			UiLocalization.Get("mod.easy"),
			UiLocalization.Get("mod.normal"),
			UiLocalization.Get("mod.hard"),
			UiLocalization.Get("mod.extreme")
		}, UiLocalization.Get("mod.damage_tip"), delegate(int i)
		{
			Settings.Difficulty = Mathf.Clamp(i, 0, 3);
			if ((bool)runtime)
			{
				runtime.Difficulty.Value = Settings.Difficulty;
			}
		}, () => (!runtime) ? Settings.Difficulty : runtime.Difficulty.Value));
		list.Add(new IMenuMod.MenuEntry(UiLocalization.Get("mod.panel_key"), values, UiLocalization.Get("mod.panel_key_tip"), delegate(int i)
		{
			MenuInput.Set(panelKeys[Mathf.Clamp(i, 0, panelKeys.Length - 1)]);
		}, () => Mathf.Max(0, Array.IndexOf(panelKeys, MenuInput.Key))));
		return list;
	}
}
