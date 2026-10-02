using System.Collections.Generic;
using HutongGames.PlayMaker;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class StagMenuRouting
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

	private static readonly List<Binding> bindings = new List<Binding>();

	private static readonly HashSet<Fsm> sources = new HashSet<Fsm>();

	private static PlayerSlot candidate;

	private static PlayerSlot owner;

	private static float candidateAt;

	private static float nextModuleScan;

	private static Fsm menu;

	private static string lastEvent;

	internal static bool HasOwner => Valid(owner);

	internal static void NoteInteraction(PlayerSlot p)
	{
		if (p != null)
		{
			if (p.Index == 0 && owner != null && owner != p)
			{
				Release();
			}
			if (candidate != p)
			{
				Diagnostics.Write("STAG request P" + (p.Index + 1));
			}
			candidate = p;
			candidateAt = Time.unscaledTime;
		}
	}

	private static bool Valid(PlayerSlot p)
	{
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (coopSession != null && coopSession.Active && p != null && coopSession.Players.Contains(p) && Object.op_Implicit((Object)(object)p.Hero) && p.Ready && p.Alive && p.Connected)
		{
			return p.Actions != null;
		}
		return false;
	}

	private static bool MenuInput(Fsm f)
	{
		if (f == null || !Object.op_Implicit((Object)(object)f.GameObject))
		{
			return false;
		}
		FsmState activeState = f.ActiveState;
		if (activeState == null || activeState.Actions == null)
		{
			return false;
		}
		FsmStateAction[] actions = activeState.Actions;
		foreach (FsmStateAction val in actions)
		{
			if (val != null)
			{
				switch (((object)val).GetType().Name)
				{
				case "ListenForUp":
				case "ListenForDown":
				case "ListenForMenuActions":
				case "ListenForMenuSubmit":
				case "ListenForMenuCancel":
					return true;
				}
			}
		}
		return false;
	}

	private static bool Ui(Fsm f)
	{
		if (InteractionRouter.IsUi(f))
		{
			return true;
		}
		UIManager instance = UIManager.instance;
		if (Object.op_Implicit((Object)(object)instance) && Object.op_Implicit((Object)(object)instance.UICanvas) && Object.op_Implicit((Object)(object)f.GameObject))
		{
			return f.GameObject.transform.IsChildOf(((Component)instance.UICanvas).transform);
		}
		return false;
	}

	internal static PlayerSlot Resolve(Fsm f)
	{
		if (!Valid(owner) || f == null)
		{
			return null;
		}
		if (f == menu || sources.Contains(f))
		{
			return owner;
		}
		if (!Ui(f) || !MenuInput(f))
		{
			return null;
		}
		return owner;
	}

	internal static void Observe(Fsm f, FsmEvent evt, FsmEventData data)
	{
		GameCameras instance = GameCameras.instance;
		if (!Object.op_Implicit((Object)(object)instance) || !Object.op_Implicit((Object)(object)instance.openStagFSM) || f != instance.openStagFSM.Fsm || evt == null)
		{
			return;
		}
		menu = f;
		string text = evt.Name ?? "";
		switch (text)
		{
		case "DESPAWN":
		case "CLOSE":
		case "CLOSE MENU":
		case "CANCEL":
		case "BACK":
		case "DESTROY":
			Release();
			return;
		}
		if (!Valid(candidate) || Time.unscaledTime - candidateAt > 30f)
		{
			return;
		}
		if (text != lastEvent)
		{
			Diagnostics.Write("STAG pending P" + (candidate.Index + 1) + " event=" + text);
			lastEvent = text;
		}
		Fsm val = data?.SentByFsm;
		if (owner != null && val != null && val != menu && Object.op_Implicit((Object)(object)val.GameObject) && Plugin.Self.Session.Resolve(val) == null)
		{
			switch (text)
			{
			case "Dirtmouth":
			case "UI SELECTION MADE":
			case "STAG MENU DOWN":
				if (sources.Add(val))
				{
					Diagnostics.Write("STAG list source=" + val.Name + " object=" + ((Object)val.GameObject).name);
				}
				break;
			}
		}
		if (((val != null && Object.op_Implicit((Object)(object)val.GameObject) && (val.Name + " " + ((Object)val.GameObject).name).ToLowerInvariant().Contains("stag")) || !(text != "OPEN") || !(text != "OPEN MENU") || !(text != "STAG MENU") || !(text != "OPEN STAG") || !(text != "SPAWN")) && owner != candidate)
		{
			owner = candidate;
			Diagnostics.Write("STAG menu owner=P" + (owner.Index + 1) + " source=" + text);
			ShopMenuRouting.Reset();
			Bind();
		}
	}

	internal static void AfterEvent(Fsm f, FsmEvent evt)
	{
		if (owner != null && f == menu && evt != null && evt.Name == "STAG MENU DOWN")
		{
			Release();
		}
	}

	internal static void Tick()
	{
		if (owner != null)
		{
			if (!Valid(owner) || !Object.op_Implicit((Object)(object)GameCameras.instance) || !Object.op_Implicit((Object)(object)GameCameras.instance.openStagFSM) || GameCameras.instance.openStagFSM.Fsm != menu || !Object.op_Implicit((Object)(object)menu.GameObject) || !menu.GameObject.activeInHierarchy)
			{
				Release();
			}
			else
			{
				Bind();
			}
		}
	}

	private static void Bind()
	{
		if (!Valid(owner))
		{
			return;
		}
		UIManager ui = UIManager.instance;
		if (Time.unscaledTime >= nextModuleScan)
		{
			nextModuleScan = Time.unscaledTime + 0.5f;
			HollowKnightInputModule[] array = Object.FindObjectsOfType<HollowKnightInputModule>();
			foreach (HollowKnightInputModule module in array)
			{
				if (Object.op_Implicit((Object)(object)module) && bindings.Find((Binding b) => (Object)(object)b.Module == (Object)(object)module) == null)
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
					Diagnostics.Write("STAG input module=" + ((Object)((Component)module).gameObject).name);
				}
			}
			if (Object.op_Implicit((Object)(object)ui) && Object.op_Implicit((Object)(object)ui.inputModule) && bindings.Find((Binding b) => (Object)(object)b.Module == (Object)(object)ui.inputModule) == null)
			{
				HollowKnightInputModule inputModule = ui.inputModule;
				bindings.Add(new Binding
				{
					Module = inputModule,
					Move = inputModule.MoveAction,
					Submit = inputModule.SubmitAction,
					Cancel = inputModule.CancelAction,
					Jump = inputModule.JumpAction,
					Attack = inputModule.AttackAction,
					Cast = inputModule.CastAction
				});
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
		if (owner != null)
		{
			Diagnostics.Write("STAG menu released P" + (owner.Index + 1));
		}
		bindings.Clear();
		sources.Clear();
		owner = null;
		menu = null;
		lastEvent = null;
		candidate = null;
		candidateAt = 0f;
		nextModuleScan = 0f;
	}

	internal static void Reset()
	{
		Release();
		candidate = null;
		candidateAt = 0f;
	}
}
