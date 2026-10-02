using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HutongGames.PlayMaker;
using On;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class DialogueCleanup
{
	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static hook_SetConversation _003C0_003E__SetConversation;

		public static CameraCallback _003C1_003E__BeforeCamera;
	}

	private static readonly Dictionary<Renderer, bool> hidden = new Dictionary<Renderer, bool>();

	private static readonly HashSet<Transform> roots = new HashSet<Transform>();

	private static bool closing;

	internal static void Install()
	{
		object obj = _003C_003EO._003C0_003E__SetConversation;
		if (obj == null)
		{
			hook_SetConversation val = SetConversation;
			_003C_003EO._003C0_003E__SetConversation = val;
			obj = (object)val;
		}
		DialogueBox.SetConversation += (hook_SetConversation)obj;
		CameraCallback onPreCull = Camera.onPreCull;
		object obj2 = _003C_003EO._003C1_003E__BeforeCamera;
		if (obj2 == null)
		{
			CameraCallback val2 = BeforeCamera;
			_003C_003EO._003C1_003E__BeforeCamera = val2;
			obj2 = (object)val2;
		}
		Camera.onPreCull = (CameraCallback)Delegate.Combine((Delegate?)(object)onPreCull, (Delegate?)obj2);
	}

	internal static void Uninstall()
	{
		object obj = _003C_003EO._003C0_003E__SetConversation;
		if (obj == null)
		{
			hook_SetConversation val = SetConversation;
			_003C_003EO._003C0_003E__SetConversation = val;
			obj = (object)val;
		}
		DialogueBox.SetConversation -= (hook_SetConversation)obj;
		CameraCallback onPreCull = Camera.onPreCull;
		object obj2 = _003C_003EO._003C1_003E__BeforeCamera;
		if (obj2 == null)
		{
			CameraCallback val2 = BeforeCamera;
			_003C_003EO._003C1_003E__BeforeCamera = val2;
			obj2 = (object)val2;
		}
		Camera.onPreCull = (CameraCallback)Delegate.Remove((Delegate?)(object)onPreCull, (Delegate?)obj2);
		Restore();
	}

	private static Transform Root(DialogueBox box)
	{
		Transform parent = ((Component)box).transform.parent;
		while (Object.op_Implicit((Object)(object)parent))
		{
			if (((Object)parent).name.IndexOf("DialogueManager", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return parent;
			}
			parent = parent.parent;
		}
		if (!Object.op_Implicit((Object)(object)((Component)box).transform.parent))
		{
			return ((Component)box).transform;
		}
		return ((Component)box).transform.parent;
	}

	private static void SetConversation(orig_SetConversation orig, DialogueBox self, string conversation, string sheet)
	{
		if (!closing)
		{
			Restore();
		}
		orig.Invoke(self, conversation, sheet);
	}

	internal static void Close()
	{
		if (closing)
		{
			return;
		}
		closing = true;
		try
		{
			DialogueBox[] array = Object.FindObjectsOfType<DialogueBox>();
			foreach (DialogueBox val in array)
			{
				if (!Object.op_Implicit((Object)(object)val) || !((Component)val).gameObject.activeInHierarchy)
				{
					continue;
				}
				roots.Add(Root(val));
				try
				{
					val.HideText();
					PlayMakerFSM val2 = PlayMakerFSM.FindFsmOnGameObject(((Component)val).gameObject, "Dialogue Page Control");
					if (!Object.op_Implicit((Object)(object)val2))
					{
						continue;
					}
					string text = ((((Object)val).name == "Text YN") ? "Hero Damaged" : "Pause");
					FsmState[] states = val2.Fsm.States;
					for (int j = 0; j < states.Length; j++)
					{
						if (states[j].Name == text)
						{
							val2.Fsm.SetState(text);
							break;
						}
					}
				}
				catch (Exception ex)
				{
					Diagnostics.Throttled("DIALOGUE cancel", ex);
				}
			}
			foreach (Transform root in roots)
			{
				if (!Object.op_Implicit((Object)(object)root))
				{
					continue;
				}
				PlayMakerFSM[] componentsInChildren = ((Component)root).GetComponentsInChildren<PlayMakerFSM>(true);
				foreach (PlayMakerFSM val3 in componentsInChildren)
				{
					if (!Object.op_Implicit((Object)(object)val3) || val3.Fsm == null)
					{
						continue;
					}
					bool flag = false;
					FsmState[] states = val3.Fsm.States;
					foreach (FsmState val4 in states)
					{
						if (val4.Transitions == null)
						{
							continue;
						}
						FsmTransition[] transitions = val4.Transitions;
						for (int k = 0; k < transitions.Length; k++)
						{
							if (transitions[k].EventName == "BOX DOWN")
							{
								flag = true;
							}
						}
					}
					if (flag)
					{
						val3.SendEvent("BOX DOWN");
					}
				}
				Hide(root);
			}
		}
		finally
		{
			closing = false;
		}
	}

	private static void Hide(Transform root)
	{
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
		foreach (Transform root in roots)
		{
			if (Object.op_Implicit((Object)(object)root))
			{
				Hide(root);
			}
		}
	}

	internal static void Restore()
	{
		foreach (KeyValuePair<Renderer, bool> item in hidden)
		{
			if (Object.op_Implicit((Object)(object)item.Key))
			{
				item.Key.forceRenderingOff = item.Value;
			}
		}
		hidden.Clear();
		roots.Clear();
	}
}
