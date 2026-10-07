using System;
using System.Collections.Generic;
using System.Reflection;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

internal sealed class PlayerContext : IDisposable
{
	[ThreadStatic]
	internal static PlayerSlot Current;

	[ThreadStatic]
	internal static bool TargetingEnemy;

	private static readonly FieldInfo HeroInstance = Reflect.Field(typeof(HeroController), "_instance");

	private static readonly FieldInfo ManagerHero = Reflect.Field(typeof(GameManager), "<hero_ctrl>k__BackingField");

	private static readonly FieldInfo InputHero = Reflect.Field(typeof(InputHandler), "heroCtrl");

	private static readonly FieldInfo InputState = Reflect.Field(typeof(InputHandler), "cState");

	private static readonly Stack<PlayerContext> pool = new Stack<PlayerContext>();

	private PlayerSlot previous;

	private PlayerSlot player;

	private bool previousEnemy;

	private bool lightweight;

	private HeroController oldHero;

	private HeroController oldManagerHero;

	private HeroActions oldActions;

	private HeroController oldInputHero;

	private HeroControllerStates oldInputState;

	private float oldInputX;

	private float oldInputY;

	private InputHandler input;

	private GameManager gm;

	private readonly List<KeyValuePair<FsmGameObject, GameObject>> globals = new List<KeyValuePair<FsmGameObject, GameObject>>(8);

	private readonly List<KeyValuePair<FsmObject, UnityEngine.Object>> globalObjects = new List<KeyValuePair<FsmObject, UnityEngine.Object>>(4);

	private bool disposed;

	internal static PlayerContext Enter(PlayerSlot p, bool enemy = false)
	{
		if (p == null || !p.Hero || (Current == p && TargetingEnemy == enemy) || Plugin.Self == null || Plugin.Self.Session == null)
		{
			return null;
		}
		CoopSession session = Plugin.Self.Session;
		GameManager instance = GameManager.instance;
		if (!instance || session.Data == null || p.Retiring)
		{
			return null;
		}
		bool simple = !enemy && Current == null && p == session.Primary && (bool)instance && instance.hero_ctrl == p.Hero && HeroInstance.GetValue(null) == p.Hero && (bool)instance.inputHandler && instance.inputHandler.inputActions == p.Actions && InputHero.GetValue(instance.inputHandler) == p.Hero && InputState.GetValue(instance.inputHandler) == p.Hero.cState;
		PlayerContext obj = ((pool.Count > 0) ? pool.Pop() : new PlayerContext());
		obj.Begin(p, enemy, simple);
		return obj;
	}

	private PlayerContext()
	{
	}

	private void Begin(PlayerSlot p, bool enemy, bool simple)
	{
		disposed = false;
		CoopSession session = Plugin.Self.Session;
		gm = GameManager.instance;
		player = p;
		previous = Current;
		previousEnemy = TargetingEnemy;
		lightweight = simple;
		if (simple)
		{
			Current = p;
			return;
		}
		globals.Clear();
		globalObjects.Clear();
		PlayerData data = session.Data;
		if (previous != null)
		{
			previous.Capture(data);
			previous.HeroBoxInactive = HeroBox.inactive;
		}
		else if (session.Primary != null)
		{
			session.Primary.Capture(data);
			session.Primary.HeroBoxInactive = HeroBox.inactive;
		}
		TargetingEnemy = enemy;
		Current = p;
		p.Apply(data);
		HeroBox.inactive = p.HeroBoxInactive;
		oldHero = (HeroController)HeroInstance.GetValue(null);
		oldManagerHero = gm.hero_ctrl;
		HeroInstance.SetValue(null, p.Hero);
		ManagerHero.SetValue(gm, p.Hero);
		input = gm.inputHandler;
		if (input != null)
		{
			oldActions = input.inputActions;
			oldInputX = input.inputX;
			oldInputY = input.inputY;
			oldInputHero = (HeroController)InputHero.GetValue(input);
			oldInputState = (HeroControllerStates)InputState.GetValue(input);
			input.inputActions = p.Actions;
			InputHero.SetValue(input, p.Hero);
			InputState.SetValue(input, p.Hero.cState);
			if (p.Actions != null)
			{
				input.inputX = p.Actions.right.Value - p.Actions.left.Value;
				input.inputY = p.Actions.up.Value - p.Actions.down.Value;
			}
		}
		FsmGameObject[] gameObjectVariables = FsmVariables.GlobalVariables.GameObjectVariables;
		foreach (FsmGameObject fsmGameObject in gameObjectVariables)
		{
			if (fsmGameObject != null && (bool)fsmGameObject.Value)
			{
				PlayerSlot playerSlot = session.Resolve(fsmGameObject.Value.transform);
				if (playerSlot != null && playerSlot != p)
				{
					globals.Add(new KeyValuePair<FsmGameObject, GameObject>(fsmGameObject, fsmGameObject.Value));
					fsmGameObject.Value = session.Remap(fsmGameObject.Value, playerSlot, p);
				}
			}
		}
		FsmObject[] objectVariables = FsmVariables.GlobalVariables.ObjectVariables;
		foreach (FsmObject fsmObject in objectVariables)
		{
			if (fsmObject == null || !fsmObject.Value)
			{
				continue;
			}
			Component component = fsmObject.Value as Component;
			if (!component)
			{
				continue;
			}
			PlayerSlot playerSlot2 = session.Resolve(component);
			if (playerSlot2 != null && playerSlot2 != p)
			{
				UnityEngine.Object @object = Hooks.Mapped(component, p);
				if ((bool)@object)
				{
					globalObjects.Add(new KeyValuePair<FsmObject, UnityEngine.Object>(fsmObject, fsmObject.Value));
					fsmObject.Value = @object;
				}
			}
		}
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}
		disposed = true;
		CoopSession session = Plugin.Self.Session;
		try
		{
			if (session != null && session.Data != null)
			{
				player.Capture(session.Data);
				player.HeroBoxInactive = HeroBox.inactive;
			}
		}
		finally
		{
			Current = previous;
			TargetingEnemy = previousEnemy;
			try
			{
				PlayerSlot playerSlot = previous ?? session?.Primary;
				if (!lightweight && playerSlot != null && session.Data != null)
				{
					playerSlot.Apply(session.Data);
					HeroBox.inactive = playerSlot.HeroBoxInactive;
				}
			}
			finally
			{
				if (!lightweight)
				{
					HeroInstance.SetValue(null, oldHero);
					if ((bool)gm)
					{
						ManagerHero.SetValue(gm, oldManagerHero);
					}
					if ((bool)input)
					{
						input.inputActions = oldActions;
						input.inputX = oldInputX;
						input.inputY = oldInputY;
						InputHero.SetValue(input, oldInputHero);
						InputState.SetValue(input, oldInputState);
					}
					foreach (KeyValuePair<FsmGameObject, GameObject> global in globals)
					{
						global.Key.Value = global.Value;
					}
					foreach (KeyValuePair<FsmObject, UnityEngine.Object> globalObject in globalObjects)
					{
						globalObject.Key.Value = globalObject.Value;
					}
				}
				globals.Clear();
				globalObjects.Clear();
				previous = (player = null);
				oldHero = (oldManagerHero = (oldInputHero = null));
				oldInputState = null;
				oldActions = null;
				input = null;
				gm = null;
				if (pool.Count < 64)
				{
					pool.Push(this);
				}
			}
		}
	}
}
