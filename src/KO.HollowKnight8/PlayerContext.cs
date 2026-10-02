using System;
using System.Collections.Generic;
using System.Reflection;
using HutongGames.PlayMaker;
using InControl;
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

	private readonly List<KeyValuePair<FsmObject, Object>> globalObjects = new List<KeyValuePair<FsmObject, Object>>(4);

	private bool disposed;

	internal static PlayerContext Enter(PlayerSlot p, bool enemy = false)
	{
		if (p == null || !Object.op_Implicit((Object)(object)p.Hero) || (Current == p && TargetingEnemy == enemy) || (Object)(object)Plugin.Self == (Object)null || Plugin.Self.Session == null)
		{
			return null;
		}
		CoopSession session = Plugin.Self.Session;
		GameManager instance = GameManager.instance;
		if (!Object.op_Implicit((Object)(object)instance) || session.Data == null || p.Retiring)
		{
			return null;
		}
		bool simple = !enemy && Current == null && p == session.Primary && Object.op_Implicit((Object)(object)instance) && (Object)(object)instance.hero_ctrl == (Object)(object)p.Hero && HeroInstance.GetValue(null) == p.Hero && Object.op_Implicit((Object)(object)instance.inputHandler) && instance.inputHandler.inputActions == p.Actions && InputHero.GetValue(instance.inputHandler) == p.Hero && InputState.GetValue(instance.inputHandler) == p.Hero.cState;
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
		if ((Object)(object)input != (Object)null)
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
				input.inputX = ((OneAxisInputControl)p.Actions.right).Value - ((OneAxisInputControl)p.Actions.left).Value;
				input.inputY = ((OneAxisInputControl)p.Actions.up).Value - ((OneAxisInputControl)p.Actions.down).Value;
			}
		}
		FsmGameObject[] gameObjectVariables = FsmVariables.GlobalVariables.GameObjectVariables;
		foreach (FsmGameObject val in gameObjectVariables)
		{
			if (val != null && Object.op_Implicit((Object)(object)val.Value))
			{
				PlayerSlot playerSlot = session.Resolve(val.Value.transform);
				if (playerSlot != null && playerSlot != p)
				{
					globals.Add(new KeyValuePair<FsmGameObject, GameObject>(val, val.Value));
					val.Value = session.Remap(val.Value, playerSlot, p);
				}
			}
		}
		FsmObject[] objectVariables = FsmVariables.GlobalVariables.ObjectVariables;
		foreach (FsmObject val2 in objectVariables)
		{
			if (val2 == null || !Object.op_Implicit(val2.Value))
			{
				continue;
			}
			Object value = val2.Value;
			Component val3 = (Component)(object)((value is Component) ? value : null);
			if (!Object.op_Implicit((Object)(object)val3))
			{
				continue;
			}
			PlayerSlot playerSlot2 = session.Resolve(val3);
			if (playerSlot2 != null && playerSlot2 != p)
			{
				Object val4 = Hooks.Mapped((Object)(object)val3, p);
				if (Object.op_Implicit(val4))
				{
					globalObjects.Add(new KeyValuePair<FsmObject, Object>(val2, val2.Value));
					val2.Value = val4;
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
					if (Object.op_Implicit((Object)(object)gm))
					{
						ManagerHero.SetValue(gm, oldManagerHero);
					}
					if (Object.op_Implicit((Object)(object)input))
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
					foreach (KeyValuePair<FsmObject, Object> globalObject in globalObjects)
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
