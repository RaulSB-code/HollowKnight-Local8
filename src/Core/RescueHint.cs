using System;
using TMPro;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class RescueHint
{
	internal static bool queued;

	private static bool shown;

	internal static bool distant;

	internal static float visibleTime;

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
		RescueHintVisibility.Request();
	}

	internal static void HideDistant()
	{
		if (distant)
		{
			queued = (distant = false);
			visibleTime = 0f;
			if ((bool)label)
			{
				label.enabled = false;
			}
		}
	}

	private static bool Create()
	{
		UIManager instance = UIManager.instance;
		if (!instance || !instance.UICanvas || !instance.UICanvas.gameObject.activeInHierarchy)
		{
			return false;
		}
		if (!font && Time.unscaledTime >= nextSearch)
		{
			nextSearch = Time.unscaledTime + 1f;
			TMP_FontAsset[] array = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
			foreach (TMP_FontAsset tMP_FontAsset in array)
			{
				if ((bool)tMP_FontAsset && (tMP_FontAsset.name.IndexOf("Trajan", StringComparison.OrdinalIgnoreCase) >= 0 || tMP_FontAsset.name.IndexOf("Perpetua", StringComparison.OrdinalIgnoreCase) >= 0))
				{
					font = tMP_FontAsset;
					break;
				}
			}
			if (!font)
			{
				TMP_Text[] array2 = Resources.FindObjectsOfTypeAll<TMP_Text>();
				foreach (TMP_Text tMP_Text in array2)
				{
					if ((bool)tMP_Text && (bool)tMP_Text.font && tMP_Text.gameObject.scene.IsValid())
					{
						font = tMP_Text.font;
						break;
					}
				}
			}
		}
		if (!font)
		{
			return false;
		}
		if (!label)
		{
			GameObject gameObject = new GameObject("Local8 Rescue Hint", typeof(RectTransform));
			gameObject.transform.SetParent(instance.UICanvas.transform, worldPositionStays: false);
			label = gameObject.AddComponent<TextMeshProUGUI>();
			label.font = font;
			label.fontSize = 25f;
			label.alignment = TextAlignmentOptions.Center;
			label.raycastTarget = false;
			label.enableWordWrapping = false;
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(0.05f, 0.12f);
			component.anchorMax = new Vector2(0.95f, 0.18f);
			Vector2 vector2 = (component.offsetMin = (component.offsetMax = Vector2.zero));
		}
		return true;
	}

	internal static void Tick(CoopSession s)
	{
		RescueHintVisibility.BeforeHint(s);
		if ((bool)label)
		{
			label.enabled = false;
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
				Diagnostics.Write("RESCUE hint shown native-font=" + font.name + " duration=15");
			}
			float num = (distant ? 5f : 15f);
			visibleTime += Time.unscaledDeltaTime;
			if (visibleTime >= num)
			{
				queued = (distant = false);
				UnityEngine.Object.Destroy(label.gameObject);
				label = null;
				return;
			}
			float a = Mathf.Clamp01(Mathf.Min(visibleTime / 0.5f, (num - visibleTime) / 2f));
			label.text = Charms.SanitizeLocalized("rescue.hud_hold", "LOCAL8", "MANTÉN ") + ActionKeys.ButtonLabel + " / " + ActionKeys.RescueLabel + Charms.SanitizeLocalized("rescue.hud_title", "LOCAL8", "  -  RESCATE ONÍRICO");
			label.color = new Color(0.95f, 0.93f, 0.86f, a);
			label.enabled = true;
		}
	}

	internal static void Reset()
	{
		if ((bool)label)
		{
			UnityEngine.Object.Destroy(label.gameObject);
		}
		label = null;
		font = null;
		queued = (shown = (distant = false));
		visibleTime = (nextSearch = (nextDistant = 0f));
		RescueHintVisibility.Reset();
	}
}
