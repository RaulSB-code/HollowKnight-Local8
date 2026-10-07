using System;
using System.Collections.Generic;
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
			if ((bool)input)
			{
				input.inputActions = previous;
				input.inputX = x;
				input.inputY = y;
			}
		}
	}

	internal static HeroActions view;

	private static PlayerAction neutral;

	private static readonly Dictionary<Fsm, bool> controllers = new Dictionary<Fsm, bool>();

	private static readonly Dictionary<Renderer, bool> hidden = new Dictionary<Renderer, bool>();

	private static readonly List<Target> targets = new List<Target>();

	private static Fsm selection;

	private static FsmState selectionState;

	private static bool selecting;

	internal static void Install()
	{
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(BeforeCamera));
	}

	internal static void Uninstall()
	{
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(BeforeCamera));
		Release();
	}

	internal static bool Controls(Fsm f)
	{
		if (!Charms.NativeMenuOpen || f == null || !f.GameObject || !InteractionRouter.IsUi(f))
		{
			return false;
		}
		if (controllers.TryGetValue(f, out var value))
		{
			return value;
		}
		GameObject nativePane = Charms.NativePane;
		value = (bool)nativePane && f.GameObject.transform.IsChildOf(nativePane.transform);
		if (!value && f.States != null)
		{
			FsmState[] states = f.States;
			foreach (FsmState fsmState in states)
			{
				if (fsmState.Actions == null)
				{
					continue;
				}
				FsmStateAction[] actions = fsmState.Actions;
				foreach (FsmStateAction fsmStateAction in actions)
				{
					if (fsmStateAction is GetCharmNum || fsmStateAction is SelectCharmBackboard)
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
		if (!Controls(f))
		{
			MenuInputRecovery.Capture();
			return null;
		}
		CoopSession session = Plugin.Self.Session;
		InputHandler inputHandler = GameManager.instance.inputHandler;
		if (session.Primary == null || session.Primary.Actions == null || !inputHandler)
		{
			MenuInputRecovery.Capture();
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
		view.jump = ((Platform.Current.GetMenuAction(menuSubmitInput: false, menuCancelInput: false, jumpInput: true, attackInput: false, castInput: false) == Platform.MenuActions.Submit) ? neutral : actions.jump);
		view.cast = ((Platform.Current.GetMenuAction(menuSubmitInput: false, menuCancelInput: false, jumpInput: false, attackInput: false, castInput: true) == Platform.MenuActions.Submit) ? neutral : actions.cast);
		InputScope result = new InputScope(inputHandler);
		MenuInputRecovery.Capture();
		return result;
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
		value.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
		value.GameObject.Value = board.gameObject;
	}

	internal static void Select(InvCharmBackboard board)
	{
		if (!board || selecting)
		{
			return;
		}
		if (selection == null || !selection.GameObject)
		{
			int num = -1;
			PlayMakerFSM[] array = UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>();
			foreach (PlayMakerFSM playMakerFSM in array)
			{
				if (!playMakerFSM || !Controls(playMakerFSM.Fsm))
				{
					continue;
				}
				FsmState[] states = playMakerFSM.Fsm.States;
				foreach (FsmState fsmState in states)
				{
					bool flag = false;
					int num2 = 0;
					if (fsmState.Actions == null)
					{
						continue;
					}
					FsmStateAction[] actions = fsmState.Actions;
					foreach (FsmStateAction fsmStateAction in actions)
					{
						if (fsmStateAction is GetCharmNum)
						{
							flag = true;
							num2 += 2;
						}
						if (fsmStateAction is SelectCharmBackboard)
						{
							num2 += 3;
						}
						if (fsmStateAction is GetCharmString || fsmStateAction is GetCharmNumString)
						{
							num2++;
						}
					}
					if (flag && num2 > num)
					{
						selection = playMakerFSM.Fsm;
						selectionState = fsmState;
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
				if (obj is GetCharmNum getCharmNum)
				{
					SetTarget(getCharmNum.target, board);
				}
				if (obj is SelectCharmBackboard selectCharmBackboard)
				{
					SetTarget(selectCharmBackboard.target, board);
				}
				if (obj is GetCharmString getCharmString)
				{
					SetTarget(getCharmString.target, board);
				}
				if (obj is GetCharmNumString getCharmNumString)
				{
					SetTarget(getCharmNumString.target, board);
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
		if (!root)
		{
			return;
		}
		Renderer[] componentsInChildren = root.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren)
		{
			if ((bool)renderer)
			{
				if (!hidden.ContainsKey(renderer))
				{
					hidden[renderer] = renderer.forceRenderingOff;
				}
				renderer.forceRenderingOff = true;
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
		if (!nativePane)
		{
			return;
		}
		Transform[] componentsInChildren = nativePane.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren)
		{
			if ((bool)transform && CursorName(transform.name))
			{
				Hide(transform);
			}
		}
		foreach (KeyValuePair<Fsm, bool> controller in controllers)
		{
			if (!controller.Value || !controller.Key.GameObject)
			{
				continue;
			}
			FsmGameObject[] gameObjectVariables = controller.Key.Variables.GameObjectVariables;
			foreach (FsmGameObject fsmGameObject in gameObjectVariables)
			{
				if (fsmGameObject != null && (bool)fsmGameObject.Value && CursorName(fsmGameObject.Name))
				{
					Hide(fsmGameObject.Value.transform);
				}
			}
		}
		foreach (KeyValuePair<Renderer, bool> item in hidden)
		{
			if ((bool)item.Key)
			{
				item.Key.forceRenderingOff = true;
			}
		}
	}

	private static void Restore()
	{
		foreach (KeyValuePair<Renderer, bool> item in hidden)
		{
			if ((bool)item.Key)
			{
				item.Key.forceRenderingOff = item.Value;
			}
		}
		hidden.Clear();
	}

	internal static void Release()
	{
		MenuInputRecovery.Capture();
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
			view.Destroy();
			view = null;
			neutral = null;
		}
	}
}
