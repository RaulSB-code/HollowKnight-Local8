using System;
using System.Collections.Generic;
using Modding;
using UnityEngine;

namespace KO.HollowKnight8;

public sealed class Local8Mod : Mod, IGlobalSettings<Local8Settings>, ILocalSettings<Local8SaveData>, IMenuMod, IMod, ILogger, ITogglableMod
{
	public const string Id = "komods.hollowknight.local8";

	public const string Version = "0.3.47-bg111xx";

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
		if (Object.op_Implicit((Object)(object)runtime))
		{
			Charms.Save(runtime.Session);
		}
		return Save;
	}

	public Local8Mod()
		: base("Hollow Knight 8-Player Co-op")
	{
		Instance = this;
	}

	public override string GetVersion()
	{
		return "0.3.47-bg111xx";
	}

	public override int LoadPriority()
	{
		return 10;
	}

	public override void Initialize()
	{
		if (!Object.op_Implicit((Object)(object)runtime))
		{
			Settings.Ensure();
			GameObject val = new GameObject("Hollow Knight 8-Player Co-op");
			Object.DontDestroyOnLoad((Object)(object)val);
			runtime = val.AddComponent<Local8Runtime>();
			runtime.Boot(this);
			((Loggable)this).Log("Hollow Knight 8-Player Co-op 0.3.47-bg111xx cargado para Hollow Knight 1.5.78.11833 / Modding API.");
		}
	}

	public void Unload()
	{
		if (Object.op_Implicit((Object)(object)runtime))
		{
			Object.Destroy((Object)(object)((Component)runtime).gameObject);
		}
		runtime = null;
		Hooks.Uninstall();
		Instance = null;
	}

	public void OnLoadGlobal(Local8Settings settings)
	{
		Settings = settings ?? new Local8Settings();
		Settings.Ensure();
	}

	public Local8Settings OnSaveGlobal()
	{
		if (Object.op_Implicit((Object)(object)runtime))
		{
			runtime.ExportSettings();
		}
		return Settings;
	}

	public List<MenuEntry> GetMenuData(MenuEntry? toggleButtonEntry)
	{
		KeyCode[] panelKeys = MenuInput.Choices;
		string[] array = Array.ConvertAll(panelKeys, MenuInput.Display);
		List<MenuEntry> list = new List<MenuEntry>();
		list.Add(new MenuEntry("Interfaz 8-Player Co-op", new string[2] { "Mascaras y alma", "Basica" }, "Se adapta automaticamente al numero de jugadores.", (Action<int>)delegate(int i)
		{
			Settings.BasicHUD = i == 1;
			if (Object.op_Implicit((Object)(object)runtime))
			{
				runtime.BasicHud.Value = i == 1;
			}
		}, (Func<int>)(() => ((!Object.op_Implicit((Object)(object)runtime)) ? Settings.BasicHUD : runtime.BasicHud.Value) ? 1 : 0)));
		list.Add(new MenuEntry("Etiquetas P1-P8", new string[2] { "Ocultas", "Visibles" }, "Muestra la etiqueta sobre cada caballero.", (Action<int>)delegate(int i)
		{
			Settings.ShowLabels = i == 1;
			if (Object.op_Implicit((Object)(object)runtime))
			{
				runtime.ShowLabels.Value = i == 1;
			}
		}, (Func<int>)(() => ((!Object.op_Implicit((Object)(object)runtime)) ? Settings.ShowLabels : runtime.ShowLabels.Value) ? 1 : 0)));
		list.Add(new MenuEntry("Camara por jugadores", new string[2] { "Desactivada", "Activada" }, "Amplia el limite de zoom segun el numero de jugadores.", (Action<int>)delegate(int i)
		{
			Settings.ZoomByPlayerCount = i == 1;
			if (Object.op_Implicit((Object)(object)runtime))
			{
				runtime.ZoomByPlayerCount.Value = i == 1;
			}
		}, (Func<int>)(() => ((!Object.op_Implicit((Object)(object)runtime)) ? Settings.ZoomByPlayerCount : runtime.ZoomByPlayerCount.Value) ? 1 : 0)));
		list.Add(new MenuEntry("Reaparicion temporal", new string[2] { "Desactivada", "Activada" }, "Alternativa opcional a la reanimacion por Focus.", (Action<int>)delegate(int i)
		{
			Settings.TimedRespawn = i == 1;
			if (Object.op_Implicit((Object)(object)runtime))
			{
				runtime.TimedRespawn.Value = i == 1;
			}
		}, (Func<int>)(() => ((!Object.op_Implicit((Object)(object)runtime)) ? Settings.TimedRespawn : runtime.TimedRespawn.Value) ? 1 : 0)));
		list.Add(new MenuEntry("Dificultad 8-Player Co-op", new string[4] { "Easy", "Normal", "Hard", "Extreme" }, "Ajusta el dano del grupo a la cantidad de jugadores.", (Action<int>)delegate(int i)
		{
			Settings.Difficulty = Mathf.Clamp(i, 0, 3);
			if (Object.op_Implicit((Object)(object)runtime))
			{
				runtime.Difficulty.Value = Settings.Difficulty;
			}
		}, (Func<int>)(() => (!Object.op_Implicit((Object)(object)runtime)) ? Settings.Difficulty : runtime.Difficulty.Value)));
		list.Add(new MenuEntry("Tecla del panel", array, "Elige la tecla para abrir 8-Player Co-op. Puedes asignar otra tecla o boton desde el panel.", (Action<int>)delegate(int i)
		{
			MenuInput.Set(panelKeys[Mathf.Clamp(i, 0, panelKeys.Length - 1)]);
		}, (Func<int>)(() => Mathf.Max(0, Array.IndexOf(panelKeys, MenuInput.Key)))));
		return list;
	}
}
