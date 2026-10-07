using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using On;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class DialogueCleanup
{
	private static readonly Dictionary<Renderer, bool> hidden = new Dictionary<Renderer, bool>();

	private static readonly HashSet<Transform> roots = new HashSet<Transform>();

	private static bool closing;

	internal static void Install()
	{
		On.DialogueBox.SetConversation += SetConversation;
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(BeforeCamera));
	}

	internal static void Uninstall()
	{
		On.DialogueBox.SetConversation -= SetConversation;
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(BeforeCamera));
		Restore();
	}

	private static Transform Root(DialogueBox box)
	{
		Transform parent = box.transform.parent;
		while ((bool)parent)
		{
			if (parent.name.IndexOf("DialogueManager", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return parent;
			}
			parent = parent.parent;
		}
		if (!box.transform.parent)
		{
			return box.transform;
		}
		return box.transform.parent;
	}

	private static void SetConversation(On.DialogueBox.orig_SetConversation orig, DialogueBox self, string conversation, string sheet)
	{
		if (!closing)
		{
			Restore();
		}
		orig(self, conversation, sheet);
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
			DialogueBox[] array = UnityEngine.Object.FindObjectsOfType<DialogueBox>();
			foreach (DialogueBox dialogueBox in array)
			{
				if (!dialogueBox || !dialogueBox.gameObject.activeInHierarchy)
				{
					continue;
				}
				roots.Add(Root(dialogueBox));
				try
				{
					dialogueBox.HideText();
					PlayMakerFSM playMakerFSM = PlayMakerFSM.FindFsmOnGameObject(dialogueBox.gameObject, "Dialogue Page Control");
					if (!playMakerFSM)
					{
						continue;
					}
					string text = ((dialogueBox.name == "Text YN") ? "Hero Damaged" : "Pause");
					FsmState[] states = playMakerFSM.Fsm.States;
					for (int j = 0; j < states.Length; j++)
					{
						if (states[j].Name == text)
						{
							playMakerFSM.Fsm.SetState(text);
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
				if (!root)
				{
					continue;
				}
				PlayMakerFSM[] componentsInChildren = root.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
				foreach (PlayMakerFSM playMakerFSM2 in componentsInChildren)
				{
					if (!playMakerFSM2 || playMakerFSM2.Fsm == null)
					{
						continue;
					}
					bool flag = false;
					FsmState[] states = playMakerFSM2.Fsm.States;
					foreach (FsmState fsmState in states)
					{
						if (fsmState.Transitions == null)
						{
							continue;
						}
						FsmTransition[] transitions = fsmState.Transitions;
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
						playMakerFSM2.SendEvent("BOX DOWN");
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
		foreach (Transform root in roots)
		{
			if ((bool)root)
			{
				Hide(root);
			}
		}
	}

	internal static void Restore()
	{
		foreach (KeyValuePair<Renderer, bool> item in hidden)
		{
			if ((bool)item.Key)
			{
				item.Key.forceRenderingOff = item.Value;
			}
		}
		hidden.Clear();
		roots.Clear();
	}
}
