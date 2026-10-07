using System;
using System.Collections.Generic;
using GlobalEnums;
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
			if (list == null || !list.GameObject || !list.GameObject.activeInHierarchy)
			{
				if (confirmation != null && (bool)confirmation.GameObject)
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
		UIManager instance = UIManager.instance;
		GameManager instance2 = GameManager.instance;
		if (((bool)instance2 && instance2.isPaused) || ((bool)instance && instance.uiState == UIState.PAUSED))
		{
			return null;
		}
		if (owner == null || f == null || !f.GameObject || !f.GameObject.activeInHierarchy || !owner.Hero || !owner.Ready || !owner.Alive || !owner.Connected || owner.Actions == null || StagMenuRouting.HasOwner)
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
		if (f.GameObject.GetComponentInParent<ShopMenuStock>() != null)
		{
			return owner;
		}
		if (list != null && (bool)list.GameObject && (bool)list.GameObject.transform.parent)
		{
			Transform parent = list.GameObject.transform.parent;
			if (parent.name.IndexOf("shop", StringComparison.OrdinalIgnoreCase) >= 0 && f.GameObject.transform.IsChildOf(parent))
			{
				return owner;
			}
		}
		if (confirmation != null && f.GameObject == confirmation.GameObject)
		{
			return owner;
		}
		if (list != null && f.GameObject.name == "UI List" && Time.unscaledTime - lastInput < 4f)
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
		if (playerSlot != null || owner == null || owner.Index == 0 || !owner.Alive || !owner.Ready || f == null || !f.GameObject || !f.GameObject.activeInHierarchy)
		{
			return playerSlot;
		}
		PlayerSlot activePlayer = InteractionRouter.ActivePlayer;
		if ((activePlayer != null && activePlayer != owner) || (activePlayer == null && Time.unscaledTime - lastInput > 2.5f))
		{
			return null;
		}
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (coopSession == null || coopSession.Resolve(f) != null)
		{
			return null;
		}
		string text = (f.Name + " " + f.GameObject.name).ToLowerInvariant();
		bool flag = text.Contains("shop") || text.Contains("item list") || text.Contains("ui list") || f.GameObject.GetComponentInParent<ShopMenuStock>() != null;
		if (!flag && list != null && (bool)list.GameObject && (bool)list.GameObject.transform.parent)
		{
			Transform parent = list.GameObject.transform.parent;
			flag = parent.childCount < 50 && parent.name.IndexOf("shop", StringComparison.OrdinalIgnoreCase) >= 0 && f.GameObject.transform.IsChildOf(parent);
		}
		if (!flag)
		{
			return null;
		}
		menuListeners.Add(f);
		Diagnostics.Write("SHOP menu listener P" + (owner.Index + 1) + " fsm=" + f.Name + " object=" + f.GameObject.name);
		return owner;
	}

	internal static void Observe(Fsm f, PlayerSlot p)
	{
		UIManager instance = UIManager.instance;
		GameManager instance2 = GameManager.instance;
		if (f == null || p == null || p.Actions == null || !p.Alive || !p.Ready || StagMenuRouting.HasOwner || ((bool)instance2 && instance2.isPaused) || ((bool)instance && instance.uiState == UIState.PAUSED))
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
				Diagnostics.Write("SHOP confirmation P" + (p.Index + 1) + " object=" + f.GameObject.name);
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
		if (owner == null)
		{
			return;
		}
		UIManager instance = UIManager.instance;
		GameManager instance2 = GameManager.instance;
		if (((bool)instance2 && instance2.isPaused) || ((bool)instance && instance.uiState == UIState.PAUSED))
		{
			Release();
			return;
		}
		PlayerSlot activePlayer = InteractionRouter.ActivePlayer;
		if (!owner.Hero || !owner.Ready || !owner.Alive || !owner.Connected || owner.Actions == null || StagMenuRouting.HasOwner || (activePlayer != null && activePlayer != owner) || (activePlayer == null && Time.unscaledTime - lastInput > 2.5f))
		{
			Release();
			return;
		}
		if (owner.Index > 0 && owner.Actions != null && (owner.Actions.menuCancel.WasPressed || owner.Actions.cast.WasPressed || owner.Actions.attack.WasPressed))
		{
			Diagnostics.Write("SHOP cancel input P" + (owner.Index + 1) + " menu=" + owner.Actions.menuCancel.WasPressed + " cast=" + owner.Actions.cast.WasPressed + " attack=" + owner.Actions.attack.WasPressed + " list=" + (list != null && (bool)list.GameObject && list.GameObject.activeInHierarchy) + " confirmation=" + (confirmation != null && (bool)confirmation.GameObject && confirmation.GameObject.activeInHierarchy) + " modules=" + bindings.Count + "/" + genericBindings.Count);
		}
		Bind();
	}

	private static void Capture(HollowKnightInputModule module)
	{
		if ((bool)module && !bindings.Exists((Binding b) => b.Module == module))
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
			Diagnostics.Write("SHOP input module=" + module.gameObject.name);
		}
	}

	private static void Capture(InControlInputModule module)
	{
		if ((bool)module && !genericBindings.Exists((GenericBinding b) => b.Module == module))
		{
			genericBindings.Add(new GenericBinding
			{
				Module = module,
				Move = module.MoveAction,
				Submit = module.SubmitAction,
				Cancel = module.CancelAction
			});
			Diagnostics.Write("SHOP generic input module=" + module.gameObject.name);
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
			HollowKnightInputModule[] array = UnityEngine.Object.FindObjectsOfType<HollowKnightInputModule>();
			for (int i = 0; i < array.Length; i++)
			{
				Capture(array[i]);
			}
			InControlInputModule[] array2 = UnityEngine.Object.FindObjectsOfType<InControlInputModule>();
			for (int i = 0; i < array2.Length; i++)
			{
				Capture(array2[i]);
			}
			UIManager instance = UIManager.instance;
			if ((bool)instance && (bool)instance.inputModule)
			{
				Capture(instance.inputModule);
			}
		}
		HeroActions actions = owner.Actions;
		foreach (Binding binding in bindings)
		{
			if ((bool)binding.Module)
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
			if ((bool)genericBinding.Module)
			{
				genericBinding.Module.MoveAction = actions.moveVector;
				genericBinding.Module.SubmitAction = actions.menuSubmit;
				genericBinding.Module.CancelAction = actions.menuCancel;
			}
		}
	}

	private static void Release()
	{
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		HeroActions heroActions = ((coopSession != null && coopSession.Active && coopSession.Primary != null) ? coopSession.Primary.Actions : null);
		foreach (Binding binding in bindings)
		{
			if ((bool)binding.Module)
			{
				binding.Module.MoveAction = ((heroActions != null) ? heroActions.moveVector : binding.Move);
				binding.Module.SubmitAction = ((heroActions != null) ? heroActions.menuSubmit : binding.Submit);
				binding.Module.CancelAction = ((heroActions != null) ? heroActions.menuCancel : binding.Cancel);
				binding.Module.JumpAction = ((heroActions != null) ? heroActions.jump : binding.Jump);
				binding.Module.AttackAction = ((heroActions != null) ? heroActions.attack : binding.Attack);
				binding.Module.CastAction = ((heroActions != null) ? heroActions.cast : binding.Cast);
			}
		}
		foreach (GenericBinding genericBinding in genericBindings)
		{
			if ((bool)genericBinding.Module)
			{
				genericBinding.Module.MoveAction = ((heroActions != null) ? heroActions.moveVector : genericBinding.Move);
				genericBinding.Module.SubmitAction = ((heroActions != null) ? heroActions.menuSubmit : genericBinding.Submit);
				genericBinding.Module.CancelAction = ((heroActions != null) ? heroActions.menuCancel : genericBinding.Cancel);
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
