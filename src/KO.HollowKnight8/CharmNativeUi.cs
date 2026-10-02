using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HutongGames.PlayMaker;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class CharmNativeUi
{
	private sealed class Target
	{
		internal FsmOwnerDefault Value;

		internal OwnerDefaultOption Option;

		internal GameObject Object;
	}

	private sealed class InputScope : IDisposable
	{
		private readonly InputHandler input;

		private readonly HeroActions previous;

		private readonly float x;

		private readonly float y;

		internal InputScope(InputHandler h)
		{
			input = h;
			previous = h.inputActions;
			x = h.inputX;
			y = h.inputY;
			h.inputActions = view;
			h.inputX = (h.inputY = 0f);
		}

		public void Dispose()
		{
			if (Object.op_Implicit((Object)(object)input))
			{
				input.inputActions = previous;
				input.inputX = x;
				input.inputY = y;
			}
		}
	}

	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static CameraCallback _003C0_003E__BeforeCamera;
	}

	private static HeroActions view;

	private static PlayerAction neutral;

	private static readonly Dictionary<Fsm, bool> controllers = new Dictionary<Fsm, bool>();

	private static readonly Dictionary<Renderer, bool> hidden = new Dictionary<Renderer, bool>();

	private static readonly List<Target> targets = new List<Target>();

	private static Fsm selection;

	private static FsmState selectionState;

	private static bool selecting;

	internal static void Install()
	{
		CameraCallback onPreCull = Camera.onPreCull;
		object obj = _003C_003EO._003C0_003E__BeforeCamera;
		if (obj == null)
		{
			CameraCallback val = BeforeCamera;
			_003C_003EO._003C0_003E__BeforeCamera = val;
			obj = (object)val;
		}
		Camera.onPreCull = (CameraCallback)Delegate.Combine((Delegate?)(object)onPreCull, (Delegate?)obj);
	}

	internal static void Uninstall()
	{
		CameraCallback onPreCull = Camera.onPreCull;
		object obj = _003C_003EO._003C0_003E__BeforeCamera;
		if (obj == null)
		{
			CameraCallback val = BeforeCamera;
			_003C_003EO._003C0_003E__BeforeCamera = val;
			obj = (object)val;
		}
		Camera.onPreCull = (CameraCallback)Delegate.Remove((Delegate?)(object)onPreCull, (Delegate?)obj);
		Release();
	}

	internal static bool Controls(Fsm f)
	{
		if (!Charms.NativeMenuOpen || f == null || !Object.op_Implicit((Object)(object)f.GameObject) || !InteractionRouter.IsUi(f))
		{
			return false;
		}
		if (controllers.TryGetValue(f, out var value))
		{
			return value;
		}
		GameObject nativePane = Charms.NativePane;
		value = Object.op_Implicit((Object)(object)nativePane) && f.GameObject.transform.IsChildOf(nativePane.transform);
		if (!value && f.States != null)
		{
			FsmState[] states = f.States;
			foreach (FsmState val in states)
			{
				if (val.Actions == null)
				{
					continue;
				}
				FsmStateAction[] actions = val.Actions;
				foreach (FsmStateAction val2 in actions)
				{
					if (val2 is GetCharmNum || val2 is SelectCharmBackboard)
					{
						value = true;
					}
				}
			}
		}
		controllers[f] = value;
		return value;
	}

	internal static IDisposable Enter(Fsm f)
	{
		//IL_00e0: Invalid comparison between Unknown and I4
		//IL_0109: Invalid comparison between Unknown and I4
		if (!Controls(f))
		{
			return null;
		}
		CoopSession session = Plugin.Self.Session;
		InputHandler inputHandler = GameManager.instance.inputHandler;
		if (session.Primary == null || session.Primary.Actions == null || !Object.op_Implicit((Object)(object)inputHandler))
		{
			return null;
		}
		if (view == null)
		{
			view = new HeroActions();
			neutral = view.menuSubmit;
		}
		HeroActions actions = session.Primary.Actions;
		view.menuCancel = actions.menuCancel;
		view.openInventory = actions.openInventory;
		view.paneLeft = actions.paneLeft;
		view.paneRight = actions.paneRight;
		view.pause = actions.pause;
		view.attack = actions.attack;
		view.jump = (((int)Platform.Current.GetMenuAction(false, false, true, false, false) == 1) ? neutral : actions.jump);
		view.cast = (((int)Platform.Current.GetMenuAction(false, false, false, false, true) == 1) ? neutral : actions.cast);
		return new InputScope(inputHandler);
	}

	private static void SetTarget(FsmOwnerDefault value, InvCharmBackboard board)
	{
		if (value == null || value.GameObject == null)
		{
			return;
		}
		bool flag = false;
		foreach (Target target in targets)
		{
			if (target.Value == value)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			targets.Add(new Target
			{
				Value = value,
				Option = value.OwnerOption,
				Object = value.GameObject.Value
			});
		}
		value.OwnerOption = (OwnerDefaultOption)1;
		value.GameObject.Value = ((Component)board).gameObject;
	}

	internal static void Select(InvCharmBackboard board)
	{
		if (!Object.op_Implicit((Object)(object)board) || selecting)
		{
			return;
		}
		if (selection == null || !Object.op_Implicit((Object)(object)selection.GameObject))
		{
			int num = -1;
			PlayMakerFSM[] array = Object.FindObjectsOfType<PlayMakerFSM>();
			foreach (PlayMakerFSM val in array)
			{
				if (!Object.op_Implicit((Object)(object)val) || !Controls(val.Fsm))
				{
					continue;
				}
				FsmState[] states = val.Fsm.States;
				foreach (FsmState val2 in states)
				{
					bool flag = false;
					int num2 = 0;
					if (val2.Actions == null)
					{
						continue;
					}
					FsmStateAction[] actions = val2.Actions;
					foreach (FsmStateAction val3 in actions)
					{
						if (val3 is GetCharmNum)
						{
							flag = true;
							num2 += 2;
						}
						if (val3 is SelectCharmBackboard)
						{
							num2 += 3;
						}
						if (val3 is GetCharmString || val3 is GetCharmNumString)
						{
							num2++;
						}
					}
					if (flag && num2 > num)
					{
						selection = val.Fsm;
						selectionState = val2;
						num = num2;
					}
				}
			}
			if (selection != null)
			{
				Diagnostics.Write("CHARMS native description fsm=" + selection.Name + " state=" + selectionState.Name);
			}
		}
		if (selection == null)
		{
			return;
		}
		selecting = true;
		try
		{
			FsmStateAction[] actions = selectionState.Actions;
			foreach (FsmStateAction obj in actions)
			{
				GetCharmNum val4 = (GetCharmNum)(object)((obj is GetCharmNum) ? obj : null);
				if (val4 != null)
				{
					SetTarget(val4.target, board);
				}
				SelectCharmBackboard val5 = (SelectCharmBackboard)(object)((obj is SelectCharmBackboard) ? obj : null);
				if (val5 != null)
				{
					SetTarget(val5.target, board);
				}
				GetCharmString val6 = (GetCharmString)(object)((obj is GetCharmString) ? obj : null);
				if (val6 != null)
				{
					SetTarget(val6.target, board);
				}
				GetCharmNumString val7 = (GetCharmNumString)(object)((obj is GetCharmNumString) ? obj : null);
				if (val7 != null)
				{
					SetTarget(val7.target, board);
				}
			}
			using (Enter(selection))
			{
				selection.SetState(selectionState.Name);
			}
		}
		catch (Exception ex)
		{
			Diagnostics.Throttled("CHARMS native description", ex);
		}
		finally
		{
			selecting = false;
		}
	}

	private static bool CursorName(string name)
	{
		if (name.IndexOf("cursor", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return name.IndexOf("selector", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}

	private static void Hide(Transform root)
	{
		if (!Object.op_Implicit((Object)(object)root))
		{
			return;
		}
		Renderer[] componentsInChildren = ((Component)root).GetComponentsInChildren<Renderer>(true);
		foreach (Renderer val in componentsInChildren)
		{
			if (Object.op_Implicit((Object)(object)val))
			{
				if (!hidden.ContainsKey(val))
				{
					hidden[val] = val.forceRenderingOff;
				}
				val.forceRenderingOff = true;
			}
		}
	}

	private static void BeforeCamera(Camera camera)
	{
		if (!Charms.NativeMenuOpen)
		{
			Restore();
			return;
		}
		GameObject nativePane = Charms.NativePane;
		if (!Object.op_Implicit((Object)(object)nativePane))
		{
			return;
		}
		Transform[] componentsInChildren = nativePane.GetComponentsInChildren<Transform>(true);
		foreach (Transform val in componentsInChildren)
		{
			if (Object.op_Implicit((Object)(object)val) && CursorName(((Object)val).name))
			{
				Hide(val);
			}
		}
		foreach (KeyValuePair<Fsm, bool> controller in controllers)
		{
			if (!controller.Value || !Object.op_Implicit((Object)(object)controller.Key.GameObject))
			{
				continue;
			}
			FsmGameObject[] gameObjectVariables = controller.Key.Variables.GameObjectVariables;
			foreach (FsmGameObject val2 in gameObjectVariables)
			{
				if (val2 != null && Object.op_Implicit((Object)(object)val2.Value) && CursorName(((NamedVariable)val2).Name))
				{
					Hide(val2.Value.transform);
				}
			}
		}
		foreach (KeyValuePair<Renderer, bool> item in hidden)
		{
			if (Object.op_Implicit((Object)(object)item.Key))
			{
				item.Key.forceRenderingOff = true;
			}
		}
	}

	private static void Restore()
	{
		foreach (KeyValuePair<Renderer, bool> item in hidden)
		{
			if (Object.op_Implicit((Object)(object)item.Key))
			{
				item.Key.forceRenderingOff = item.Value;
			}
		}
		hidden.Clear();
	}

	internal static void Release()
	{
		Restore();
		controllers.Clear();
		selection = null;
		selectionState = null;
		foreach (Target target in targets)
		{
			target.Value.OwnerOption = target.Option;
			target.Value.GameObject.Value = target.Object;
		}
		targets.Clear();
		if (view != null)
		{
			((PlayerActionSet)view).Destroy();
			view = null;
			neutral = null;
		}
	}
}
