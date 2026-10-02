using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KO.HollowKnight8;

internal static class RescueHint
{
	private static bool queued;

	private static bool shown;

	private static bool distant;

	private static float visibleTime;

	private static float nextSearch;

	private static float nextDistant;

	private static TextMeshProUGUI label;

	private static TMP_FontAsset font;

	internal static void Queue()
	{
		if (!shown)
		{
			queued = true;
		}
	}

	internal static void QueueDistant()
	{
		if (!TransitionVote.ApproachingExit(((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session) && !(Time.unscaledTime < nextDistant) && !queued)
		{
			nextDistant = Time.unscaledTime + 25f;
			queued = (distant = true);
			visibleTime = 0f;
			Diagnostics.Write("RESCUE hint distant players");
		}
	}

	internal static void HideDistant()
	{
		if (distant)
		{
			queued = (distant = false);
			visibleTime = 0f;
			if (Object.op_Implicit((Object)(object)label))
			{
				((Behaviour)label).enabled = false;
			}
		}
	}

	private static bool Create()
	{
		UIManager instance = UIManager.instance;
		if (!Object.op_Implicit((Object)(object)instance) || !Object.op_Implicit((Object)(object)instance.UICanvas) || !((Component)instance.UICanvas).gameObject.activeInHierarchy)
		{
			return false;
		}
		if (!Object.op_Implicit((Object)(object)font) && Time.unscaledTime >= nextSearch)
		{
			nextSearch = Time.unscaledTime + 1f;
			TMP_FontAsset[] array = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
			foreach (TMP_FontAsset val in array)
			{
				if (Object.op_Implicit((Object)(object)val) && (((Object)val).name.IndexOf("Trajan", StringComparison.OrdinalIgnoreCase) >= 0 || ((Object)val).name.IndexOf("Perpetua", StringComparison.OrdinalIgnoreCase) >= 0))
				{
					font = val;
					break;
				}
			}
			if (!Object.op_Implicit((Object)(object)font))
			{
				TMP_Text[] array2 = Resources.FindObjectsOfTypeAll<TMP_Text>();
				foreach (TMP_Text val2 in array2)
				{
					if (Object.op_Implicit((Object)(object)val2) && Object.op_Implicit((Object)(object)val2.font))
					{
						Scene scene = ((Component)val2).gameObject.scene;
						if (((Scene)(ref scene)).IsValid())
						{
							font = val2.font;
							break;
						}
					}
				}
			}
		}
		if (!Object.op_Implicit((Object)(object)font))
		{
			return false;
		}
		if (!Object.op_Implicit((Object)(object)label))
		{
			GameObject val3 = new GameObject("Local8 Rescue Hint", new Type[1] { typeof(RectTransform) });
			val3.transform.SetParent(((Component)instance.UICanvas).transform, false);
			label = val3.AddComponent<TextMeshProUGUI>();
			((TMP_Text)label).font = font;
			((TMP_Text)label).fontSize = 25f;
			((TMP_Text)label).alignment = (TextAlignmentOptions)5;
			((Graphic)label).raycastTarget = false;
			((TMP_Text)label).enableWordWrapping = false;
			RectTransform component = val3.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(0.05f, 0.12f);
			component.anchorMax = new Vector2(0.95f, 0.18f);
			Vector2 offsetMin = (component.offsetMax = Vector2.zero);
			component.offsetMin = offsetMin;
		}
		return true;
	}

	internal static void Tick(CoopSession s)
	{
		if (Object.op_Implicit((Object)(object)label))
		{
			((Behaviour)label).enabled = false;
		}
		if (distant && TransitionVote.ApproachingExit(s))
		{
			HideDistant();
		}
		if (!queued || s == null || !s.Active || !s.Gameplay || Plugin.Self.Panel || Charms.NativeMenuOpen || PvpMatch.BlocksInput)
		{
			return;
		}
		bool flag = false;
		foreach (PlayerSlot player in s.Players)
		{
			if (player.Index > 0 && player.Ready && player.Alive)
			{
				flag = true;
			}
		}
		if (flag && Create())
		{
			if (!shown)
			{
				shown = true;
				Diagnostics.Write("RESCUE hint shown native-font=" + ((Object)font).name + " duration=15");
			}
			float num = (distant ? 7f : 15f);
			visibleTime += Time.unscaledDeltaTime;
			if (visibleTime >= num)
			{
				queued = (distant = false);
				Object.Destroy((Object)(object)((Component)label).gameObject);
				label = null;
				return;
			}
			float num2 = Mathf.Clamp01(Mathf.Min(visibleTime / 0.5f, (num - visibleTime) / 2f));
			((TMP_Text)label).text = Charms.SanitizeLocalized("rescue.hud_hold", "LOCAL8", "MANTÉN ") + ActionKeys.ButtonLabel + " / " + ActionKeys.RescueLabel + Charms.SanitizeLocalized("rescue.hud_title", "LOCAL8", "  -  RESCATE ONÍRICO");
			((Graphic)label).color = new Color(0.95f, 0.93f, 0.86f, num2);
			((Behaviour)label).enabled = true;
		}
	}

	internal static void Reset()
	{
		if (Object.op_Implicit((Object)(object)label))
		{
			Object.Destroy((Object)(object)((Component)label).gameObject);
		}
		label = null;
		font = null;
		queued = (shown = (distant = false));
		visibleTime = (nextSearch = (nextDistant = 0f));
	}
}
