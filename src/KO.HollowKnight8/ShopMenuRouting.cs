using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class ShopMenuRouting
{
	private sealed class Binding
	{
		internal HollowKnightInputModule Module;

		internal PlayerTwoAxisAction Move;

		internal PlayerAction Submit;

		internal PlayerAction Cancel;

		internal PlayerAction Jump;

		internal PlayerAction Attack;

		internal PlayerAction Cast;
	}

	private sealed class GenericBinding
	{
		internal InControlInputModule Module;

		internal PlayerTwoAxisAction Move;

		internal PlayerAction Submit;

		internal PlayerAction Cancel;
	}

	private static readonly List<Binding> bindings = new List<Binding>();

	private static readonly List<GenericBinding> genericBindings = new List<GenericBinding>();

	private static readonly HashSet<Fsm> menuListeners = new HashSet<Fsm>();

	private static Fsm list;

	private static Fsm confirmation;

	private static PlayerSlot owner;

	private static float nextScan;

	private static float lastInput;

	internal static PlayerSlot Buyer
	{
		get
		{
			if (owner == null || !owner.Alive)
			{
				return null;
			}
			return owner;
		}
	}

	internal static bool MenuVisible
	{
		get
		{
			if (list == null || !Object.op_Implicit((Object)(object)list.GameObject) || !list.GameObject.activeInHierarchy)
			{
				if (confirmation != null && Object.op_Implicit((Object)(object)confirmation.GameObject))
				{
					return confirmation.GameObject.activeInHierarchy;
				}
				return false;
			}
			return true;
		}
	}

	internal static PlayerSlot Resolve(Fsm f)
	{
		//IL_002b: Invalid comparison between Unknown and I4
		UIManager instance = UIManager.instance;
		GameManager instance2 = GameManager.instance;
		if ((Object.op_Implicit((Object)(object)instance2) && instance2.isPaused) || (Object.op_Implicit((Object)(object)instance) && (int)instance.uiState == 5))
		{
			return null;
		}
		if (owner == null || f == null || !Object.op_Implicit((Object)(object)f.GameObject) || !f.GameObject.activeInHierarchy || !Object.op_Implicit((Object)(object)owner.Hero) || !owner.Ready || !owner.Alive || !owner.Connected || owner.Actions == null || StagMenuRouting.HasOwner)
		{
			return null;
		}
		PlayerSlot activePlayer = InteractionRouter.ActivePlayer;
		if (activePlayer != null && activePlayer != owner)
		{
			return null;
		}
		if (f == list || f == confirmation || menuListeners.Contains(f))
		{
			return owner;
		}
		if ((Object)(object)f.GameObject.GetComponentInParent<ShopMenuStock>() != (Object)null)
		{
			return owner;
		}
		if (list != null && Object.op_Implicit((Object)(object)list.GameObject) && Object.op_Implicit((Object)(object)list.GameObject.transform.parent))
		{
			Transform parent = list.GameObject.transform.parent;
			if (((Object)parent).name.IndexOf("shop", StringComparison.OrdinalIgnoreCase) >= 0 && f.GameObject.transform.IsChildOf(parent))
			{
				return owner;
			}
		}
		if (confirmation != null && (Object)(object)f.GameObject == (Object)(object)confirmation.GameObject)
		{
			return owner;
		}
		if (list != null && ((Object)f.GameObject).name == "UI List" && Time.unscaledTime - lastInput < 4f)
		{
			return owner;
		}
		if (f.Name == "ui_list" && list != null && Time.unscaledTime - lastInput < 4f)
		{
			return owner;
		}
		return null;
	}

	internal static PlayerSlot ListenerOwner(Fsm f)
	{
		PlayerSlot playerSlot = Resolve(f);
		if (playerSlot != null || owner == null || owner.Index == 0 || !owner.Alive || !owner.Ready || f == null || !Object.op_Implicit((Object)(object)f.GameObject) || !f.GameObject.activeInHierarchy)
		{
			return playerSlot;
		}
		PlayerSlot activePlayer = InteractionRouter.ActivePlayer;
		if ((activePlayer != null && activePlayer != owner) || (activePlayer == null && Time.unscaledTime - lastInput > 2.5f))
		{
			return null;
		}
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (coopSession == null || coopSession.Resolve(f) != null)
		{
			return null;
		}
		string text = (f.Name + " " + ((Object)f.GameObject).name).ToLowerInvariant();
		bool flag = text.Contains("shop") || text.Contains("item list") || text.Contains("ui list") || (Object)(object)f.GameObject.GetComponentInParent<ShopMenuStock>() != (Object)null;
		if (!flag && list != null && Object.op_Implicit((Object)(object)list.GameObject) && Object.op_Implicit((Object)(object)list.GameObject.transform.parent))
		{
			Transform parent = list.GameObject.transform.parent;
			flag = parent.childCount < 50 && ((Object)parent).name.IndexOf("shop", StringComparison.OrdinalIgnoreCase) >= 0 && f.GameObject.transform.IsChildOf(parent);
		}
		if (!flag)
		{
			return null;
		}
		menuListeners.Add(f);
		Diagnostics.Write("SHOP menu listener P" + (owner.Index + 1) + " fsm=" + f.Name + " object=" + ((Object)f.GameObject).name);
		return owner;
	}

	internal static void Observe(Fsm f, PlayerSlot p)
	{
		//IL_0050: Invalid comparison between Unknown and I4
		UIManager instance = UIManager.instance;
		GameManager instance2 = GameManager.instance;
		if (f == null || p == null || p.Actions == null || !p.Alive || !p.Ready || StagMenuRouting.HasOwner || (Object.op_Implicit((Object)(object)instance2) && instance2.isPaused) || (Object.op_Implicit((Object)(object)instance) && (int)instance.uiState == 5))
		{
			return;
		}
		if (owner != p)
		{
			Release();
			owner = p;
			Diagnostics.Write("SHOP list owner=P" + (p.Index + 1));
		}
		if (f.Name == "ui_list")
		{
			if (confirmation != f)
			{
				Diagnostics.Write("SHOP confirmation P" + (p.Index + 1) + " object=" + ((Object)f.GameObject).name);
			}
			confirmation = f;
		}
		else
		{
			list = f;
		}
		lastInput = Time.unscaledTime;
		Bind();
	}

	internal static void Tick()
	{
		//IL_0033: Invalid comparison between Unknown and I4
		if (owner == null)
		{
			return;
		}
		UIManager instance = UIManager.instance;
		GameManager instance2 = GameManager.instance;
		if ((Object.op_Implicit((Object)(object)instance2) && instance2.isPaused) || (Object.op_Implicit((Object)(object)instance) && (int)instance.uiState == 5))
		{
			Release();
			return;
		}
		PlayerSlot activePlayer = InteractionRouter.ActivePlayer;
		if (!Object.op_Implicit((Object)(object)owner.Hero) || !owner.Ready || !owner.Alive || !owner.Connected || owner.Actions == null || StagMenuRouting.HasOwner || (activePlayer != null && activePlayer != owner) || (activePlayer == null && Time.unscaledTime - lastInput > 2.5f))
		{
			Release();
			return;
		}
		if (owner.Index > 0 && owner.Actions != null && (((OneAxisInputControl)owner.Actions.menuCancel).WasPressed || ((OneAxisInputControl)owner.Actions.cast).WasPressed || ((OneAxisInputControl)owner.Actions.attack).WasPressed))
		{
			Diagnostics.Write("SHOP cancel input P" + (owner.Index + 1) + " menu=" + ((OneAxisInputControl)owner.Actions.menuCancel).WasPressed + " cast=" + ((OneAxisInputControl)owner.Actions.cast).WasPressed + " attack=" + ((OneAxisInputControl)owner.Actions.attack).WasPressed + " list=" + (list != null && Object.op_Implicit((Object)(object)list.GameObject) && list.GameObject.activeInHierarchy) + " confirmation=" + (confirmation != null && Object.op_Implicit((Object)(object)confirmation.GameObject) && confirmation.GameObject.activeInHierarchy) + " modules=" + bindings.Count + "/" + genericBindings.Count);
		}
		Bind();
	}

	private static void Capture(HollowKnightInputModule module)
	{
		if (Object.op_Implicit((Object)(object)module) && !bindings.Exists((Binding b) => (Object)(object)b.Module == (Object)(object)module))
		{
			bindings.Add(new Binding
			{
				Module = module,
				Move = module.MoveAction,
				Submit = module.SubmitAction,
				Cancel = module.CancelAction,
				Jump = module.JumpAction,
				Attack = module.AttackAction,
				Cast = module.CastAction
			});
			Diagnostics.Write("SHOP input module=" + ((Object)((Component)module).gameObject).name);
		}
	}

	private static void Capture(InControlInputModule module)
	{
		if (Object.op_Implicit((Object)(object)module) && !genericBindings.Exists((GenericBinding b) => (Object)(object)b.Module == (Object)(object)module))
		{
			genericBindings.Add(new GenericBinding
			{
				Module = module,
				Move = module.MoveAction,
				Submit = module.SubmitAction,
				Cancel = module.CancelAction
			});
			Diagnostics.Write("SHOP generic input module=" + ((Object)((Component)module).gameObject).name);
		}
	}

	private static void Bind()
	{
		if (owner == null || owner.Actions == null)
		{
			return;
		}
		if (Time.unscaledTime >= nextScan)
		{
			nextScan = Time.unscaledTime + 0.5f;
			HollowKnightInputModule[] array = Object.FindObjectsOfType<HollowKnightInputModule>();
			for (int i = 0; i < array.Length; i++)
			{
				Capture(array[i]);
			}
			InControlInputModule[] array2 = Object.FindObjectsOfType<InControlInputModule>();
			for (int i = 0; i < array2.Length; i++)
			{
				Capture(array2[i]);
			}
			UIManager instance = UIManager.instance;
			if (Object.op_Implicit((Object)(object)instance) && Object.op_Implicit((Object)(object)instance.inputModule))
			{
				Capture(instance.inputModule);
			}
		}
		HeroActions actions = owner.Actions;
		foreach (Binding binding in bindings)
		{
			if (Object.op_Implicit((Object)(object)binding.Module))
			{
				binding.Module.MoveAction = actions.moveVector;
				binding.Module.SubmitAction = actions.menuSubmit;
				binding.Module.CancelAction = actions.menuCancel;
				binding.Module.JumpAction = actions.jump;
				binding.Module.AttackAction = actions.attack;
				binding.Module.CastAction = actions.cast;
			}
		}
		foreach (GenericBinding genericBinding in genericBindings)
		{
			if (Object.op_Implicit((Object)(object)genericBinding.Module))
			{
				genericBinding.Module.MoveAction = actions.moveVector;
				genericBinding.Module.SubmitAction = actions.menuSubmit;
				genericBinding.Module.CancelAction = actions.menuCancel;
			}
		}
	}

	private static void Release()
	{
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		HeroActions val = ((coopSession != null && coopSession.Active && coopSession.Primary != null) ? coopSession.Primary.Actions : null);
		foreach (Binding binding in bindings)
		{
			if (Object.op_Implicit((Object)(object)binding.Module))
			{
				binding.Module.MoveAction = ((val != null) ? val.moveVector : binding.Move);
				binding.Module.SubmitAction = ((val != null) ? val.menuSubmit : binding.Submit);
				binding.Module.CancelAction = ((val != null) ? val.menuCancel : binding.Cancel);
				binding.Module.JumpAction = ((val != null) ? val.jump : binding.Jump);
				binding.Module.AttackAction = ((val != null) ? val.attack : binding.Attack);
				binding.Module.CastAction = ((val != null) ? val.cast : binding.Cast);
			}
		}
		foreach (GenericBinding genericBinding in genericBindings)
		{
			if (Object.op_Implicit((Object)(object)genericBinding.Module))
			{
				genericBinding.Module.MoveAction = ((val != null) ? val.moveVector : genericBinding.Move);
				genericBinding.Module.SubmitAction = ((val != null) ? val.menuSubmit : genericBinding.Submit);
				genericBinding.Module.CancelAction = ((val != null) ? val.menuCancel : genericBinding.Cancel);
			}
		}
		if (owner != null)
		{
			Diagnostics.Write("SHOP list released P" + (owner.Index + 1));
		}
		list = (confirmation = null);
		owner = null;
		nextScan = (lastInput = 0f);
		bindings.Clear();
		genericBindings.Clear();
		menuListeners.Clear();
	}

	internal static void Reset()
	{
		Release();
	}
}
