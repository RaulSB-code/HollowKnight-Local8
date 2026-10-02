using System;
using System.Collections.Generic;
using System.Linq;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class Hud
{
	private static GUIStyle text;

	private static GUIStyle title;

	private static GUIStyle small;

	private static GUIStyle button;

	private static GUIStyle tag;

	private static Texture2D pixel;

	internal static int selected;

	private static int tab;

	private static Vector2 scroll;

	private static readonly int[] lastHealth = Enumerable.Repeat(-1, 8).ToArray();

	private static readonly Color[] palette = (Color[])(object)new Color[12]
	{
		new Color(0.82f, 0.92f, 1f),
		new Color(0.4f, 0.85f, 1f),
		new Color(1f, 0.55f, 0.45f),
		new Color(0.55f, 1f, 0.6f),
		new Color(0.95f, 0.65f, 1f),
		new Color(1f, 0.88f, 0.4f),
		new Color(0.55f, 0.62f, 1f),
		new Color(1f, 0.64f, 0.84f),
		Color.white,
		new Color(0.55f, 1f, 0.9f),
		new Color(1f, 0.35f, 0.35f),
		new Color(0.75f, 0.55f, 1f)
	};

	private static Font nativeFont;

	private static float nextFontSearch;

	private static string lastTip = "";

	private static float tipSince;

	private static void Styles()
	{
		if (text == null)
		{
			pixel = Texture2D.whiteTexture;
			GUIStyle val = new GUIStyle(GUI.skin.label)
			{
				fontSize = 15
			};
			val.normal.textColor = Color.white;
			text = val;
			title = new GUIStyle(text)
			{
				fontSize = 20,
				fontStyle = (FontStyle)1
			};
			small = new GUIStyle(text)
			{
				fontSize = 12
			};
			GUIStyle val2 = new GUIStyle(GUI.skin.button)
			{
				fontSize = 13
			};
			val2.normal.textColor = Color.white;
			button = val2;
			tag = new GUIStyle(text)
			{
				alignment = (TextAnchor)4,
				fontStyle = (FontStyle)1
			};
		}
	}

	private static void Box(Rect r, Color c)
	{
		Color color = GUI.color;
		GUI.color = c;
		GUI.DrawTexture(r, (Texture)(object)pixel);
		GUI.color = color;
	}

	private static void Icon(Texture tex, Rect rect, Color tint)
	{
		Color color = GUI.color;
		GUI.color = tint;
		GUI.DrawTexture(rect, tex, (ScaleMode)2, true);
		GUI.color = color;
	}

	internal static void Restore()
	{
		VanillaHud.Restore();
		RescueHint.Reset();
		for (int i = 0; i < 8; i++)
		{
			lastHealth[i] = -1;
		}
	}

	internal static void QueueRescueHint()
	{
		RescueHint.Queue();
	}

	internal static void ReleaseAssets()
	{
		Restore();
		HudAssets.Release();
		RoomWaitArt.Release();
	}

	private static string Status(PlayerSlot p, Local8Runtime plugin)
	{
		if (p.Faulted)
		{
			return Charms.SanitizeLocalized("hud.recover_in", "LOCAL8", "RECUPERAR EN ") + MenuInput.Label;
		}
		if (!Object.op_Implicit((Object)(object)p.Hero) || !p.Ready)
		{
			return Charms.SanitizeLocalized("hud.waiting", "LOCAL8", "ESPERANDO");
		}
		if (p.Down)
		{
			if (!PvpMatch.Running)
			{
				if (!plugin.TimedRespawn.Value)
				{
					return Charms.SanitizeLocalized("hud.down", "LOCAL8", "CAÍDO");
				}
				return Mathf.CeilToInt(Mathf.Max(0f, plugin.RespawnSeconds.Value - (Time.time - p.DownAt))) + "s";
			}
			return Charms.SanitizeLocalized("hud.eliminated", "LOCAL8", "ELIMINADO");
		}
		if (!p.Connected)
		{
			return Charms.SanitizeLocalized("hud.no_device", "LOCAL8", "SIN DISPOSITIVO");
		}
		if (CoopEnding.NeedsFocus(p))
		{
			return "FOCUS";
		}
		if (!p.Hazard)
		{
			return "";
		}
		return Charms.SanitizeLocalized("hud.returning", "LOCAL8", "VOLVIENDO");
	}

	internal static void Draw(Local8Runtime plugin)
	{
		//IL_0069: Invalid comparison between Unknown and I4
		Styles();
		CoopSession session = plugin.Session;
		float num = Mathf.Clamp((float)Screen.height / 900f, 0.65f, 1.6f);
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = Matrix4x4.Scale(new Vector3(num, num, 1f));
		float w = (float)Screen.width / num;
		float h = (float)Screen.height / num;
		try
		{
			GameManager instance = GameManager.instance;
			if ((int)Event.current.type == 7 && session.Active && session.Primary != null && Object.op_Implicit((Object)(object)instance) && instance.IsGameplayScene() && !instance.IsLoadingSceneTransition)
			{
				if (!plugin.BasicHud.Value)
				{
					HudAssets.Prepare();
				}
				foreach (PlayerSlot player in session.Players)
				{
					int num2 = player.Vitals.Health + player.Vitals.Blue;
					if (lastHealth[player.Index] >= 0 && num2 < lastHealth[player.Index])
					{
						player.HealthBeforeHit = lastHealth[player.Index];
						player.LastDamageAt = Time.unscaledTime;
						player.DamageFlashUntil = Time.unscaledTime + 0.4f;
					}
					lastHealth[player.Index] = num2;
				}
				if (plugin.BasicHud.Value || !HudAssets.Ready)
				{
					DrawBasic(plugin, w);
				}
				else
				{
					DrawMasks(plugin, w);
				}
				if (plugin.ShowLabels.Value)
				{
					DrawLabels(session, num);
				}
				DrawRevival(session, num);
				DrawPvp(session, w, num);
			}
			if (!plugin.Panel && session.Active && session.Players.Count > 1 && Object.op_Implicit((Object)(object)instance) && instance.IsGameplayScene())
			{
				NativeCharmCards(session, w, h, num);
			}
			if (!plugin.Panel && session.Active && Object.op_Implicit((Object)(object)instance) && instance.IsGameplayScene())
			{
				TransitionBar(w, h);
				JoiningBar(session, w, h);
			}
			if (plugin.Panel)
			{
				if (Charms.Editing)
				{
					CharmPanel(plugin, w, h);
				}
				else
				{
					Panel(plugin, w, h);
				}
			}
			if (plugin.Panel)
			{
				DrawTooltip(w, h);
			}
		}
		finally
		{
			GUI.matrix = matrix;
		}
	}

	private static void DrawBasic(Local8Runtime plugin, float w)
	{
		CoopSession session = plugin.Session;
		float num = Mathf.Min(210f * plugin.HudScale.Value, (w - 32f - (float)((session.Players.Count - 1) * 7)) / (float)session.Players.Count);
		float num2 = 57f * plugin.HudScale.Value;
		float num3 = num * (float)session.Players.Count + (float)(7 * (session.Players.Count - 1));
		float num4 = (w - num3) * 0.5f;
		Rect r = default(Rect);
		foreach (PlayerSlot player in session.Players)
		{
			((Rect)(ref r))._002Ector(num4 + (float)player.Index * (num + 7f), 12f, num, num2);
			bool flag = Time.unscaledTime < player.DamageFlashUntil;
			Box(r, flag ? new Color(0.35f, 0.04f, 0.04f, 0.9f) : new Color(0.035f, 0.04f, 0.075f, 0.84f));
			Box(new Rect(((Rect)(ref r)).x, ((Rect)(ref r)).y, 3f, ((Rect)(ref r)).height), player.Color);
			Rect val = new Rect(((Rect)(ref r)).x + 8f, ((Rect)(ref r)).y + 2f, ((Rect)(ref r)).width - 12f, 20f);
			string obj = "P" + (player.Index + 1) + " " + Status(player, plugin);
			GUIStyle val2 = new GUIStyle(small)
			{
				fontStyle = (FontStyle)1
			};
			val2.normal.textColor = player.Color;
			GUI.Label(val, obj, val2);
			GUI.Label(new Rect(((Rect)(ref r)).x + 8f, ((Rect)(ref r)).y + 22f, ((Rect)(ref r)).width - 12f, 20f), "VIDA " + Mathf.Max(0, player.Vitals.Health) + ((player.Vitals.Blue > 0) ? (" +" + player.Vitals.Blue) : "") + " / " + player.CurrentMaxHealth, small);
			Bar(new Rect(((Rect)(ref r)).x + 8f, ((Rect)(ref r)).yMax - 6f, ((Rect)(ref r)).width - 16f, 3f), (float)player.Vitals.Soul / (float)Mathf.Max(1, player.Vitals.MaxSoul), player.Color);
			if (player.ReviveProgress > 0f)
			{
				Bar(new Rect(((Rect)(ref r)).x + 8f, ((Rect)(ref r)).yMax - 12f, ((Rect)(ref r)).width - 16f, 3f), player.ReviveProgress, Color.white);
			}
		}
	}

	private static void Bar(Rect rect, float fraction, Color color)
	{
		Box(rect, new Color(0.12f, 0.14f, 0.19f, 0.85f));
		((Rect)(ref rect)).width = ((Rect)(ref rect)).width * Mathf.Clamp01(fraction);
		Box(rect, color);
	}

	private static void DrawMasks(Local8Runtime plugin, float w)
	{
		CoopSession session = plugin.Session;
		float num = (float)CoopRules.HudScale(session.Players.Count, w, plugin.HudScale.Value);
		float num2 = 254f * num;
		float num3 = 8f;
		float num4 = (w - ((float)session.Players.Count * num2 + (float)(session.Players.Count - 1) * num3)) * 0.5f;
		Rect rect = default(Rect);
		foreach (PlayerSlot player in session.Players)
		{
			float num5 = num4 + (float)player.Index * (num2 + num3);
			float num6 = 12f;
			Color color = player.Color;
			bool flag = Time.unscaledTime < player.DamageFlashUntil;
			num5 += (flag ? (Mathf.Sin(Time.unscaledTime * 90f) * 2f * num) : 0f);
			HudAssets.Images images = HudAssets.For(player);
			Texture2D val = ((images != null && Object.op_Implicit((Object)(object)images.Mask)) ? images.Mask : HudAssets.Mask);
			Texture2D tex = ((images != null && Object.op_Implicit((Object)(object)images.Soul)) ? images.Soul : HudAssets.Soul);
			GUIStyle val2 = new GUIStyle(small)
			{
				fontSize = Mathf.Max(10, Mathf.RoundToInt(13f * num)),
				fontStyle = (FontStyle)1
			};
			val2.normal.textColor = color;
			GUIStyle val3 = val2;
			GUI.Label(new Rect(num5 + 7f * num, num6, num2 - 14f * num, 20f), "P" + (player.Index + 1) + " " + Status(player, plugin), val3);
			((Rect)(ref rect))._002Ector(num5 + 7f * num, num6 + 24f * num, 56f * num, 56f * num);
			Icon((Texture)(object)tex, rect, new Color(0.22f, 0.25f, 0.32f));
			float num7 = Mathf.Clamp01((float)player.Vitals.Soul / (float)Mathf.Max(1, player.Vitals.MaxSoul));
			if (num7 > 0f)
			{
				GUI.BeginGroup(new Rect(((Rect)(ref rect)).x, ((Rect)(ref rect)).y + ((Rect)(ref rect)).height * (1f - num7), ((Rect)(ref rect)).width, ((Rect)(ref rect)).height * num7));
				try
				{
					Icon((Texture)(object)tex, new Rect(0f, (0f - ((Rect)(ref rect)).height) * (1f - num7), ((Rect)(ref rect)).width, ((Rect)(ref rect)).height), color);
				}
				finally
				{
					GUI.EndGroup();
				}
			}
			if (images != null && Object.op_Implicit((Object)(object)images.Frame))
			{
				Icon((Texture)(object)images.Frame, new Rect(((Rect)(ref rect)).x - 5f * num, ((Rect)(ref rect)).y - 13f * num, ((Rect)(ref rect)).width + 15f * num, ((Rect)(ref rect)).height + 23f * num), color);
			}
			int currentMaxHealth = player.CurrentMaxHealth;
			int num8 = Mathf.Max(0, player.Vitals.Blue);
			int num9 = currentMaxHealth + num8;
			int num10 = Mathf.Max(5, Mathf.Min(10, Mathf.CeilToInt((float)num9 / 2f)));
			int num11 = Mathf.CeilToInt((float)num9 / (float)num10);
			float num12 = Mathf.Min(29f * num, 58f * num / (float)Mathf.Max(2, num11));
			float num13 = Mathf.Min(27f * num, 180f * num / (float)num10);
			for (int i = 0; i < num9; i++)
			{
				bool flag2 = i >= currentMaxHealth || i < player.Vitals.Health;
				Color val4 = (Color)((!flag2) ? new Color(0.2f, 0.22f, 0.28f, 0.85f) : ((i >= currentMaxHealth) ? new Color(0.45f, 0.85f, 1f) : color));
				if (flag && flag2)
				{
					val4 = Color.Lerp(val4, Color.white, 0.75f);
				}
				Texture2D tex2 = val;
				if (images != null)
				{
					if (i >= currentMaxHealth && Object.op_Implicit((Object)(object)images.Blue))
					{
						tex2 = images.Blue;
					}
					else if (!flag2 && Object.op_Implicit((Object)(object)images.Empty))
					{
						tex2 = images.Empty;
						val4 = color;
					}
					if (!flag2 && i == player.Vitals.Health && Time.unscaledTime - player.LastDamageAt < 0.6f)
					{
						Texture2D val5 = images.Damage(Time.unscaledTime - player.LastDamageAt);
						if (Object.op_Implicit((Object)(object)val5))
						{
							tex2 = val5;
							val4 = color;
						}
					}
				}
				if (Time.unscaledTime < player.HealFlashUntil)
				{
					val4 = Color.Lerp(val4, Color.white, 0.5f);
				}
				Icon((Texture)(object)tex2, new Rect(num5 + 67f * num + (float)(i % num10) * num13, num6 + 23f * num + (float)(i / num10) * num12, num13 - 2f * num, num12 - 2f * num), val4);
			}
			if (player.Vitals.Reserve > 0)
			{
				Bar(new Rect(((Rect)(ref rect)).x, ((Rect)(ref rect)).yMax + 4f * num, ((Rect)(ref rect)).width, 3f * num), (float)player.Vitals.Reserve / 99f, color);
			}
			if (player.ReviveProgress > 0f)
			{
				Bar(new Rect(num5 + 6f * num, num6 + 99f * num, num2 - 12f * num, 3f * num), player.ReviveProgress, Color.white);
			}
		}
	}

	private static void DrawLabels(CoopSession s, float scale)
	{
		//IL_0024: Invalid comparison between Unknown and I4
		if (!Object.op_Implicit((Object)(object)Camera.main))
		{
			return;
		}
		UIManager instance = UIManager.instance;
		if (!Object.op_Implicit((Object)(object)instance) || (int)instance.uiState != 4 || GameManager.instance.isPaused || s.Data.disablePause || Plugin.Self.Panel || ShopMenuRouting.MenuVisible || PickupCard.Owner != null || Charms.NativeMenuOpen || StagMenuRouting.HasOwner || ScriptedParty.Active || CoopEnding.Active)
		{
			return;
		}
		GameCameras instance2 = GameCameras.instance;
		if (!Object.op_Implicit((Object)(object)instance2))
		{
			return;
		}
		PlayMakerFSM cameraFadeFSM = instance2.cameraFadeFSM;
		if (!Object.op_Implicit((Object)(object)cameraFadeFSM) || !(cameraFadeFSM.ActiveStateName == "Normal"))
		{
			return;
		}
		GUI.depth = 5000;
		Camera main = Camera.main;
		if (!Object.op_Implicit((Object)(object)main))
		{
			return;
		}
		Rect val2 = default(Rect);
		foreach (PlayerSlot player in s.Players)
		{
			if (Object.op_Implicit((Object)(object)player.Hero) && player.Alive && !TransitionVote.Holding(player))
			{
				Vector3 val = main.WorldToScreenPoint(((Component)player.Hero).transform.position + Vector3.up * 1.9f);
				if (!(val.z <= 0f))
				{
					((Rect)(ref val2))._002Ector(val.x / scale - 20f, ((float)Screen.height - val.y) / scale - 12f, 40f, 22f);
					Rect val3 = new Rect(((Rect)(ref val2)).x + 1f, ((Rect)(ref val2)).y + 1f, ((Rect)(ref val2)).width, ((Rect)(ref val2)).height);
					string obj = "P" + (player.Index + 1);
					GUIStyle val4 = new GUIStyle(tag);
					val4.normal.textColor = Color.black;
					GUI.Label(val3, obj, val4);
					Rect val5 = val2;
					string obj2 = "P" + (player.Index + 1);
					GUIStyle val6 = new GUIStyle(tag);
					val6.normal.textColor = player.Color;
					GUI.Label(val5, obj2, val6);
				}
			}
		}
		GUI.depth = -10000;
	}

	private static void DeviceRow(CoopSession s, InputDevice d, string name, float cw, ref float row)
	{
		InputDevice d2 = d;
		PlayerSlot playerSlot = s.Players.FirstOrDefault((PlayerSlot p) => p.Device == d2);
		GUI.Label(new Rect(5f, row, cw - 230f, 26f), name + ((playerSlot != null) ? (" - P" + (playerSlot.Index + 1)) : " - libre"), small);
		if (d != InputDevice.Null && playerSlot == null && s.Players.Count < 8 && GUI.Button(new Rect(cw - 220f, row, 105f, 29f), "Unir", button))
		{
			s.Join(d2);
		}
		bool num = s.Players.Skip(1).Any((PlayerSlot p) => p.Device == d2);
		if ((d == InputDevice.Null || !num) && GUI.Button(new Rect(cw - 110f, row, 105f, 29f), "Asignar P1", button))
		{
			s.AssignPrimary(d2);
		}
		row += 35f;
	}

	private static void Panel(Local8Runtime plugin, float w, float h)
	{
		//IL_040e: Invalid comparison between Unknown and I4
		CoopSession session = plugin.Session;
		float num = Mathf.Min(840f, w - 24f);
		float num2 = Mathf.Min(800f, h - 24f);
		float num3 = (w - num) * 0.5f;
		float num4 = (h - num2) * 0.5f;
		Box(new Rect(num3, num4, num, num2), new Color(0.035f, 0.045f, 0.075f, 0.98f));
		GUI.Label(new Rect(num3 + 20f, num4 + 15f, num - 110f, 28f), "LOCAL 8 / MODDING API 0.3.47-bg111xx", title);
		if (GUI.Button(new Rect(num3 + num - 90f, num4 + 15f, 70f, 30f), Charms.SanitizeLocalized("common.close", "LOCAL8", "Cerrar"), button))
		{
			plugin.SetPanel(open: false);
		}
		GUI.Label(new Rect(num3 + 20f, num4 + 50f, num - 40f, 28f), "Local8  /  " + MenuInput.Label + Charms.SanitizeLocalized("panel.toggle_hint", "LOCAL8", " abre y cierra el panel"), small);
		if (session.Primary == null)
		{
			GUI.Label(new Rect(num3 + 20f, num4 + 112f, num - 40f, 60f), Charms.SanitizeLocalized("panel.enter_game", "LOCAL8", "Entra en una partida para configurar el grupo."), text);
			return;
		}
		string[] array = new string[4]
		{
			Charms.SanitizeLocalized("tab.players", "LOCAL8", "Jugadores y controles"),
			Charms.SanitizeLocalized("tab.game", "LOCAL8", "Partida"),
			"PvP",
			Charms.SanitizeLocalized("tab.settings", "LOCAL8", "Ajustes")
		};
		float num5 = (num - 40f) / (float)array.Length;
		for (int i = 0; i < array.Length; i++)
		{
			if (GUI.Button(new Rect(num3 + 20f + (float)i * num5, num4 + 86f, num5 - 4f, 33f), ((tab == i) ? "> " : "") + array[i], button) && tab != i)
			{
				tab = i;
				scroll = Vector2.zero;
			}
		}
		InputDevice[] array2 = Controls.Devices().ToArray();
		Rect val = default(Rect);
		((Rect)(ref val))._002Ector(num3 + 15f, num4 + 134f, num - 30f, num2 - 207f);
		float num6 = Mathf.Max(((Rect)(ref val)).height - 10f, (float)((tab == 0) ? (613 + session.Players.Count * 34 + (array2.Length + 1) * 35) : ((tab == 1) ? 510 : ((tab == 2) ? (700 + session.Players.Count * 37) : 490))));
		scroll = GUI.BeginScrollView(val, scroll, new Rect(0f, 0f, ((Rect)(ref val)).width - 20f, num6));
		float row = 0f;
		float cw = ((Rect)(ref val)).width - 30f;
		try
		{
			if (tab == 0)
			{
				PlayerOptions(plugin, session, array2, cw, ref row);
			}
			else if (tab == 1)
			{
				GameOptions(plugin, cw, ref row);
			}
			else if (tab == 2)
			{
				PvpOptions(plugin, session, cw, ref row);
			}
			else
			{
				AdvancedOptions(plugin, session, cw, ref row);
			}
		}
		finally
		{
			GUI.EndScrollView();
		}
		float num7 = num3 + 20f;
		float num8 = num4 + num2 - 67f;
		float num9 = (num - 48f) / 4f;
		if (GUI.Button(new Rect(num7, num8, num9 - 4f, 32f), ((int)MenuInput.Key == 290) ? Charms.SanitizeLocalized("action.join", "LOCAL8", "Unir") : Charms.SanitizeLocalized("action.join_f9", "LOCAL8", "Unir (F9)"), button))
		{
			session.JoinFirstAvailable();
		}
		if (GUI.Button(new Rect(num7 + num9, num8, num9 - 4f, 32f), new GUIContent(Charms.SanitizeLocalized("action.gather", "LOCAL8", "Reunir"), Charms.SanitizeLocalized("rescue.hold", "LOCAL8", "Mantén ") + ActionKeys.ButtonLabel + " (teclado: " + ActionKeys.RescueLabel + Charms.SanitizeLocalized("rescue.panel_tip", "LOCAL8", ") para viajar como partículas oníricas hasta otro jugador. Recarga: 25 s.")), button))
		{
			session.GatherNow();
		}
		if (GUI.Button(new Rect(num7 + 2f * num9, num8, num9 - 4f, 32f), Charms.SanitizeLocalized("action.repair", "LOCAL8", "Recuperar jugadores"), button))
		{
			session.RepairPlayers();
		}
		if (session.Players.Count > 1 && GUI.Button(new Rect(num7 + 3f * num9, num8, num9 - 4f, 32f), Charms.SanitizeLocalized("action.remove_last", "LOCAL8", "Quitar último"), button))
		{
			session.RemoveLast();
		}
		GUI.Label(new Rect(num7, num8 + 37f, num - 40f, 25f), (Time.unscaledTime < plugin.MessageUntil) ? plugin.Message : "", new GUIStyle(small)
		{
			wordWrap = false,
			clipping = (TextClipping)1
		});
	}

	private static void PlayerOptions(Local8Runtime plugin, CoopSession s, InputDevice[] devices, float cw, ref float row)
	{
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("players.title", "LOCAL8", "JUGADORES"), title);
		row += 32f;
		foreach (PlayerSlot player in s.Players)
		{
			if (GUI.Button(new Rect(5f, row, 58f, 29f), ((selected == player.Index) ? "> " : "") + "P" + (player.Index + 1), button))
			{
				selected = player.Index;
			}
			Rect val = new Rect(75f, row + 4f, cw - 85f, 26f);
			string obj = player.DeviceName + (player.Connected ? "" : Charms.SanitizeLocalized("players.disconnected", "LOCAL8", " (desconectado)")) + " / " + SkinBridge.Display(player);
			GUIStyle val2 = new GUIStyle(text);
			val2.normal.textColor = player.Color;
			GUI.Label(val, obj, val2);
			row += 34f;
		}
		row += 10f;
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("keyboard.section", "LOCAL8", "JUGADORES CON TECLADO"), title);
		row += 32f;
		DeviceRow(s, InputDevice.Null, Charms.SanitizeLocalized("keyboard.shared", "LOCAL8", "Teclado compartido · máx. 4 "), cw, ref row);
		foreach (InputDevice d in devices)
		{
			DeviceRow(s, d, Controls.Label(d), cw, ref row);
		}
		GUI.Label(new Rect(5f, row, cw, 28f), "vJoy: " + Controls.VirtualCount + Charms.SanitizeLocalized("vjoy.detected_hint", "LOCAL8", " detectado(s). Puedes buscar más en Ajustes."), small);
		row += 42f;
		if (GUI.Button(new Rect(5f, row, cw - 10f, 31f), Charms.SanitizeLocalized("keyboard.add", "LOCAL8", "Añadir jugador con teclado"), button))
		{
			tab = 4;
			scroll = Vector2.zero;
		}
		row += 43f;
		selected = Mathf.Clamp(selected, 0, s.Players.Count - 1);
		PlayerSlot playerSlot = s.Players[selected];
		GUI.Label(new Rect(5f, row, cw, 27f), Charms.SanitizeLocalized("appearance.player", "LOCAL8", "PERSONALIZACIÓN P") + (selected + 1), title);
		row += 34f;
		GUI.Label(new Rect(5f, row, 145f, 24f), Charms.SanitizeLocalized("common.color", "LOCAL8", "Color"), small);
		float num = Mathf.Min(28f, (cw - 155f) / (float)palette.Length);
		for (int j = 0; j < palette.Length; j++)
		{
			Rect val3 = new Rect(155f + (float)j * num, row, num - 3f, 25f);
			Box(val3, palette[j]);
			if (GUI.Button(val3, "", GUIStyle.none))
			{
				playerSlot.Color = palette[j];
				Local8Mod.Settings.Colors[selected] = ColorUtility.ToHtmlStringRGB(palette[j]);
			}
		}
		row += 35f;
		string[] array = new string[3] { "R", "G", "B" };
		for (int k = 0; k < 3; k++)
		{
			GUI.Label(new Rect(5f, row + 4f, 30f, 22f), array[k], small);
			float num2 = k switch
			{
				1 => playerSlot.Color.g, 
				0 => playerSlot.Color.r, 
				_ => playerSlot.Color.b, 
			};
			float num3 = GUI.HorizontalSlider(new Rect(42f, row + 6f, cw - 160f, 18f), num2, 0f, 1f);
			GUI.Label(new Rect(cw - 100f, row, 95f, 23f), Mathf.RoundToInt(num3 * 255f).ToString(), small);
			if (Mathf.Abs(num3 - num2) > 0.001f)
			{
				Color color = playerSlot.Color;
				switch (k)
				{
				case 0:
					color.r = num3;
					break;
				case 1:
					color.g = num3;
					break;
				default:
					color.b = num3;
					break;
				}
				playerSlot.Color = color;
				Local8Mod.Settings.Colors[selected] = ColorUtility.ToHtmlStringRGB(color);
			}
			row += 26f;
		}
		row += 8f;
		GUI.Label(new Rect(5f, row, 145f, 24f), Charms.SanitizeLocalized("common.skin", "LOCAL8", "Skin"), small);
		if (GUI.Button(new Rect(155f, row, 35f, 29f), "<", button))
		{
			SkinBridge.Cycle(playerSlot, -1);
		}
		GUI.Label(new Rect(200f, row, cw - 320f, 26f), SkinBridge.Display(playerSlot), small);
		if (GUI.Button(new Rect(cw - 105f, row, 100f, 29f), Charms.SanitizeLocalized("common.next", "LOCAL8", "Siguiente"), button))
		{
			SkinBridge.Cycle(playerSlot, 1);
		}
		row += 44f;
		if (GUI.Button(new Rect(5f, row, cw * 0.48f, 31f), Charms.SanitizeLocalized("charms.player", "LOCAL8", "Amuletos P") + (selected + 1), button))
		{
			Charms.Open(playerSlot);
		}
		if (GUI.Button(new Rect(cw * 0.51f, row, cw * 0.48f, 31f), Charms.SanitizeLocalized("labels.prefix", "LOCAL8", "Etiquetas: ") + (plugin.ShowLabels.Value ? Charms.SanitizeLocalized("common.yes", "LOCAL8", "SÍ") : Charms.SanitizeLocalized("common.no", "LOCAL8", "NO")), button))
		{
			plugin.ShowLabels.Value = !plugin.ShowLabels.Value;
		}
		row += 39f;
		if (GUI.Button(new Rect(5f, row, cw * 0.55f, 31f), Charms.SanitizeLocalized("interface.prefix", "LOCAL8", "Interfaz: ") + (plugin.BasicHud.Value ? Charms.SanitizeLocalized("hud.basic", "LOCAL8", "BÁSICA") : Charms.SanitizeLocalized("hud.full", "LOCAL8", "MÁSCARAS Y ALMA")), button))
		{
			plugin.BasicHud.Value = !plugin.BasicHud.Value;
		}
		if (GUI.Button(new Rect(cw - 145f, row, 32f, 31f), "-", button))
		{
			plugin.HudScale.Value = Mathf.Max(0.7f, plugin.HudScale.Value - 0.1f);
		}
		GUI.Label(new Rect(cw - 110f, row + 5f, 75f, 24f), Mathf.RoundToInt(plugin.HudScale.Value * 100f) + "%", small);
		if (GUI.Button(new Rect(cw - 35f, row, 32f, 31f), "+", button))
		{
			plugin.HudScale.Value = Mathf.Min(1.5f, plugin.HudScale.Value + 0.1f);
		}
	}

	private static void GameOptions(Local8Runtime plugin, float cw, ref float row)
	{
		if (tab == 4)
		{
			if (selected >= 1 && plugin.Session.Players.Count > selected && Controls.IsKeyboard(plugin.Session.Players[selected].Device) && plugin.Session.Players[selected].Actions != null)
			{
				GUI.Label(new Rect(5f, row, cw * 0.98f, 29f), Charms.SanitizeLocalized("keyboard.new_title", "LOCAL8", "NUEVO JUGADOR: TECLADO"), title);
				row += 36f;
				GUI.Label(new Rect(5f, row, cw * 0.98f, 24f), Charms.SanitizeLocalized("bind.help", "LOCAL8", "Selecciona una acción y pulsa la tecla o botón que quieras usar."), small);
				row += 34f;
				GUI.Label(new Rect(5f, row, cw * 0.2f, 29f), plugin.Session.Players[selected].Actions.up.Name, text);
				Rect val = new Rect(cw * 0.22f, row, cw * 0.25f, 29f);
				object obj;
				if (ActionKeys.Capturing == 10)
				{
					obj = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator = plugin.Session.Players[selected].Actions.up.Bindings.GetEnumerator();
					obj = ((!enumerator.MoveNext()) ? "-" : enumerator.Current.Name);
				}
				if (GUI.Button(val, (string)obj, button))
				{
					ActionKeys.Capturing = 10;
				}
				GUI.Label(new Rect(cw * 0.52f, row, cw * 0.2f, 29f), plugin.Session.Players[selected].Actions.left.Name, text);
				Rect val2 = new Rect(cw * 0.73f, row, cw * 0.25f, 29f);
				object obj2;
				if (ActionKeys.Capturing == 12)
				{
					obj2 = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator2 = plugin.Session.Players[selected].Actions.left.Bindings.GetEnumerator();
					obj2 = ((!enumerator2.MoveNext()) ? "-" : enumerator2.Current.Name);
				}
				if (GUI.Button(val2, (string)obj2, button))
				{
					ActionKeys.Capturing = 12;
				}
				row += 36f;
				GUI.Label(new Rect(5f, row, cw * 0.2f, 29f), plugin.Session.Players[selected].Actions.down.Name, text);
				Rect val3 = new Rect(cw * 0.22f, row, cw * 0.25f, 29f);
				object obj3;
				if (ActionKeys.Capturing == 11)
				{
					obj3 = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator3 = plugin.Session.Players[selected].Actions.down.Bindings.GetEnumerator();
					obj3 = ((!enumerator3.MoveNext()) ? "-" : enumerator3.Current.Name);
				}
				if (GUI.Button(val3, (string)obj3, button))
				{
					ActionKeys.Capturing = 11;
				}
				GUI.Label(new Rect(cw * 0.52f, row, cw * 0.2f, 29f), plugin.Session.Players[selected].Actions.right.Name, text);
				Rect val4 = new Rect(cw * 0.73f, row, cw * 0.25f, 29f);
				object obj4;
				if (ActionKeys.Capturing == 13)
				{
					obj4 = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator4 = plugin.Session.Players[selected].Actions.right.Bindings.GetEnumerator();
					obj4 = ((!enumerator4.MoveNext()) ? "-" : enumerator4.Current.Name);
				}
				if (GUI.Button(val4, (string)obj4, button))
				{
					ActionKeys.Capturing = 13;
				}
				row += 36f;
				GUI.Label(new Rect(5f, row, cw * 0.2f, 29f), plugin.Session.Players[selected].Actions.jump.Name, text);
				Rect val5 = new Rect(cw * 0.22f, row, cw * 0.25f, 29f);
				object obj5;
				if (ActionKeys.Capturing == 14)
				{
					obj5 = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator5 = plugin.Session.Players[selected].Actions.jump.Bindings.GetEnumerator();
					obj5 = ((!enumerator5.MoveNext()) ? "-" : enumerator5.Current.Name);
				}
				if (GUI.Button(val5, (string)obj5, button))
				{
					ActionKeys.Capturing = 14;
				}
				GUI.Label(new Rect(cw * 0.52f, row, cw * 0.2f, 29f), plugin.Session.Players[selected].Actions.quickMap.Name, text);
				Rect val6 = new Rect(cw * 0.73f, row, cw * 0.25f, 29f);
				object obj6;
				if (ActionKeys.Capturing == 21)
				{
					obj6 = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator6 = plugin.Session.Players[selected].Actions.quickMap.Bindings.GetEnumerator();
					obj6 = ((!enumerator6.MoveNext()) ? "-" : enumerator6.Current.Name);
				}
				if (GUI.Button(val6, (string)obj6, button))
				{
					ActionKeys.Capturing = 21;
				}
				row += 36f;
				GUI.Label(new Rect(5f, row, cw * 0.2f, 29f), plugin.Session.Players[selected].Actions.attack.Name, text);
				Rect val7 = new Rect(cw * 0.22f, row, cw * 0.25f, 29f);
				object obj7;
				if (ActionKeys.Capturing == 15)
				{
					obj7 = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator7 = plugin.Session.Players[selected].Actions.attack.Bindings.GetEnumerator();
					obj7 = ((!enumerator7.MoveNext()) ? "-" : enumerator7.Current.Name);
				}
				if (GUI.Button(val7, (string)obj7, button))
				{
					ActionKeys.Capturing = 15;
				}
				GUI.Label(new Rect(cw * 0.52f, row, cw * 0.2f, 29f), plugin.Session.Players[selected].Actions.superDash.Name, text);
				Rect val8 = new Rect(cw * 0.73f, row, cw * 0.25f, 29f);
				object obj8;
				if (ActionKeys.Capturing == 20)
				{
					obj8 = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator8 = plugin.Session.Players[selected].Actions.superDash.Bindings.GetEnumerator();
					obj8 = ((!enumerator8.MoveNext()) ? "-" : enumerator8.Current.Name);
				}
				if (GUI.Button(val8, (string)obj8, button))
				{
					ActionKeys.Capturing = 20;
				}
				row += 36f;
				GUI.Label(new Rect(5f, row, cw * 0.2f, 29f), plugin.Session.Players[selected].Actions.dash.Name, text);
				Rect val9 = new Rect(cw * 0.22f, row, cw * 0.25f, 29f);
				object obj9;
				if (ActionKeys.Capturing == 16)
				{
					obj9 = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator9 = plugin.Session.Players[selected].Actions.dash.Bindings.GetEnumerator();
					obj9 = ((!enumerator9.MoveNext()) ? "-" : enumerator9.Current.Name);
				}
				if (GUI.Button(val9, (string)obj9, button))
				{
					ActionKeys.Capturing = 16;
				}
				GUI.Label(new Rect(cw * 0.52f, row, cw * 0.2f, 29f), plugin.Session.Players[selected].Actions.dreamNail.Name, text);
				Rect val10 = new Rect(cw * 0.73f, row, cw * 0.25f, 29f);
				object obj10;
				if (ActionKeys.Capturing == 19)
				{
					obj10 = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator10 = plugin.Session.Players[selected].Actions.dreamNail.Bindings.GetEnumerator();
					obj10 = ((!enumerator10.MoveNext()) ? "-" : enumerator10.Current.Name);
				}
				if (GUI.Button(val10, (string)obj10, button))
				{
					ActionKeys.Capturing = 19;
				}
				row += 36f;
				GUI.Label(new Rect(5f, row, cw * 0.2f, 29f), plugin.Session.Players[selected].Actions.cast.Name, text);
				Rect val11 = new Rect(cw * 0.22f, row, cw * 0.25f, 29f);
				object obj11;
				if (ActionKeys.Capturing == 17)
				{
					obj11 = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator11 = plugin.Session.Players[selected].Actions.cast.Bindings.GetEnumerator();
					obj11 = ((!enumerator11.MoveNext()) ? "-" : enumerator11.Current.Name);
				}
				if (GUI.Button(val11, (string)obj11, button))
				{
					ActionKeys.Capturing = 17;
				}
				GUI.Label(new Rect(cw * 0.52f, row, cw * 0.2f, 29f), plugin.Session.Players[selected].Actions.focus.Name, text);
				Rect val12 = new Rect(cw * 0.73f, row, cw * 0.25f, 29f);
				object obj12;
				if (ActionKeys.Capturing == 18)
				{
					obj12 = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator12 = plugin.Session.Players[selected].Actions.focus.Bindings.GetEnumerator();
					obj12 = ((!enumerator12.MoveNext()) ? "-" : enumerator12.Current.Name);
				}
				if (GUI.Button(val12, (string)obj12, button))
				{
					ActionKeys.Capturing = 18;
				}
				row += 36f;
				GUI.Label(new Rect(5f, row, cw * 0.2f, 29f), Charms.SanitizeLocalized("rescue.title", "LOCAL8", "RESCATE ONÍRICO"), text);
				Rect val13 = new Rect(cw * 0.22f, row, cw * 0.25f, 29f);
				object obj13;
				if (ActionKeys.Capturing == 22)
				{
					obj13 = Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar");
				}
				else
				{
					IEnumerator<BindingSource> enumerator13 = plugin.Session.Players[selected].Actions.paneRight.Bindings.GetEnumerator();
					obj13 = ((!enumerator13.MoveNext()) ? "-" : enumerator13.Current.Name);
				}
				if (GUI.Button(val13, (string)obj13, button))
				{
					ActionKeys.Capturing = 22;
				}
				row += 36f;
				GUI.Label(new Rect(5f, row, cw * 0.98f, 24f), Charms.SanitizeLocalized("rescue.tip", "LOCAL8", "Mantén la tecla para viajar como partículas oníricas hacia un compañero. Recarga: 25 s."), small);
				row += 32f;
				if (GUI.Button(new Rect(5f, row, cw * 0.48f, 31f), Charms.SanitizeLocalized("common.reset", "LOCAL8", "Restablecer"), button))
				{
					ActionKeys.Capturing = 0;
					Controls.Dispose(plugin.Session.Players[selected]);
					Controls.InitSecondary(plugin.Session.Players[selected], plugin.Session.Players[0].Actions, InputDevice.Null);
				}
				if (GUI.Button(new Rect(cw * 0.51f, row, cw * 0.47f, 31f), Charms.SanitizeLocalized("common.back", "LOCAL8", "Volver"), button))
				{
					ActionKeys.Capturing = 0;
					selected = 0;
					tab = 0;
					scroll = Vector2.zero;
				}
				return;
			}
			GUI.Label(new Rect(5f, row, cw - 10f, 29f), Charms.SanitizeLocalized("keyboard.new_title", "LOCAL8", "NUEVO JUGADOR: TECLADO"), title);
			row += 36f;
			GUI.Label(new Rect(5f, row, cw - 10f, 24f), Charms.SanitizeLocalized("keyboard.new_intro", "LOCAL8", "Configura los controles del nuevo jugador."), small);
			row += 31f;
			int num = 0;
			if (plugin.Session.Players.Count > 0)
			{
				num += (Controls.IsKeyboard(plugin.Session.Players[0].Device) ? 1 : 0);
			}
			if (plugin.Session.Players.Count > 1)
			{
				num += (Controls.IsKeyboard(plugin.Session.Players[1].Device) ? 1 : 0);
			}
			if (plugin.Session.Players.Count > 2)
			{
				num += (Controls.IsKeyboard(plugin.Session.Players[2].Device) ? 1 : 0);
			}
			if (plugin.Session.Players.Count > 3)
			{
				num += (Controls.IsKeyboard(plugin.Session.Players[3].Device) ? 1 : 0);
			}
			if (plugin.Session.Players.Count > 4)
			{
				num += (Controls.IsKeyboard(plugin.Session.Players[4].Device) ? 1 : 0);
			}
			if (plugin.Session.Players.Count > 5)
			{
				num += (Controls.IsKeyboard(plugin.Session.Players[5].Device) ? 1 : 0);
			}
			if (plugin.Session.Players.Count > 6)
			{
				num += (Controls.IsKeyboard(plugin.Session.Players[6].Device) ? 1 : 0);
			}
			if (plugin.Session.Players.Count > 7)
			{
				num += (Controls.IsKeyboard(plugin.Session.Players[7].Device) ? 1 : 0);
			}
			int num2 = num;
			switch (num2)
			{
			case 0:
				GUI.Label(new Rect(5f, row, cw - 10f, 25f), Charms.SanitizeLocalized("PERFIL 1  WASD | ESPACIO salto | Q ataque | E dash", "LOCAL8", "PERFIL 1  WASD | ESPACIO salto | Q ataque | E dash"), text);
				row += 32f;
				GUI.Label(new Rect(5f, row, cw - 10f, 24f), Charms.SanitizeLocalized("C hechizo | X focus | Z rápido | 1 onírico | 2 superdash", "LOCAL8", "C hechizo | X focus | Z rápido | 1 onírico | 2 superdash"), small);
				row += 31f;
				break;
			case 1:
				GUI.Label(new Rect(5f, row, cw - 10f, 25f), Charms.SanitizeLocalized("PERFIL 2  TEGH | V salto | Y ataque | B dash", "LOCAL8", "PERFIL 2  TEGH | V salto | Y ataque | B dash"), text);
				row += 32f;
				GUI.Label(new Rect(5f, row, cw - 10f, 24f), Charms.SanitizeLocalized("N hechizo | 7 focus | 5 rápido | R onírico | 3 superdash", "LOCAL8", "N hechizo | 7 focus | 5 rápido | R onírico | 3 superdash"), small);
				row += 31f;
				break;
			case 2:
				GUI.Label(new Rect(5f, row, cw - 10f, 25f), Charms.SanitizeLocalized("PERFIL 3  WJKL | M salto | O ataque | , dash", "LOCAL8", "PERFIL 3  WJKL | M salto | O ataque | , dash"), text);
				row += 32f;
				GUI.Label(new Rect(5f, row, cw - 10f, 24f), Charms.SanitizeLocalized(". hechizo | U focus | 0 rápido | P onírico | / superdash", "LOCAL8", ". hechizo | U focus | 0 rápido | P onírico | / superdash"), small);
				row += 31f;
				break;
			case 3:
				GUI.Label(new Rect(5f, row, cw - 10f, 25f), Charms.SanitizeLocalized("PERFIL 4  NUM 8456 | NUM0 salto | NUM7 ataque | NUM9 dash", "LOCAL8", "PERFIL 4  NUM 8456 | NUM0 salto | NUM7 ataque | NUM9 dash"), text);
				row += 32f;
				GUI.Label(new Rect(5f, row, cw - 10f, 24f), Charms.SanitizeLocalized("NUM1 hechizo | NUM+ focus | NUM* rápido | NUM3 onírico | NUM2 superdash", "LOCAL8", "NUM1 hechizo | NUM+ focus | NUM* rápido | NUM3 onírico | NUM2 superdash"), small);
				row += 31f;
				break;
			default:
				GUI.Label(new Rect(5f, row, cw - 10f, 25f), Charms.SanitizeLocalized("keyboard.limit", "LOCAL8", "Máximo: 4 jugadores con teclado."), text);
				row += 32f;
				break;
			}
			GUI.Label(new Rect(5f, row, cw - 10f, 25f), Charms.SanitizeLocalized("keyboard.p1", "LOCAL8", "P1 mantiene los controles configurados en Hollow Knight."), small);
			row += 32f;
			GUI.Label(new Rect(5f, row, cw - 10f, 25f), Charms.SanitizeLocalized("keyboard.controllers", "LOCAL8", "Los mandos mantienen sus controles habituales."), small);
			row += 32f;
			GUI.Label(new Rect(5f, row, cw - 10f, 24f), Charms.SanitizeLocalized("keyboard.mouse", "LOCAL8", "El ratón sigue reservado para P1."), small);
			row += 31f;
			GUI.Label(new Rect(5f, row, cw - 10f, 24f), Charms.SanitizeLocalized("bind.help", "LOCAL8", "Selecciona una acción y pulsa la tecla o botón que quieras usar."), small);
			row += 31f;
			if (num2 < 4)
			{
				if (plugin.Session.Players.Count < 8)
				{
					if (GUI.Button(new Rect(5f, row, cw - 10f, 31f), Charms.SanitizeLocalized("keyboard.add_now", "LOCAL8", "Añadir jugador"), button))
					{
						if (plugin.Session.CanJoin)
						{
							plugin.Session.Join(InputDevice.Null);
							selected = plugin.Session.Players.Count - 1;
							scroll = Vector2.zero;
						}
						else
						{
							GUI.Label(new Rect(5f, row, cw - 10f, 25f), Charms.SanitizeLocalized("keyboard.wait", "LOCAL8", "Espera a que termine la carga o la cinemática."), text);
							row += 32f;
						}
					}
				}
				else
				{
					GUI.Label(new Rect(5f, row, cw - 10f, 25f), Charms.SanitizeLocalized("group.full", "LOCAL8", "El grupo ya tiene 8 jugadores."), text);
					row += 32f;
				}
			}
			else
			{
				GUI.Label(new Rect(5f, row, cw - 10f, 25f), Charms.SanitizeLocalized("keyboard.limit", "LOCAL8", "Máximo: 4 jugadores con teclado."), text);
				row += 32f;
			}
			row += 39f;
			if (GUI.Button(new Rect(5f, row, cw - 10f, 31f), Charms.SanitizeLocalized("common.cancel", "LOCAL8", "Cancelar"), button))
			{
				tab = 0;
				scroll = Vector2.zero;
			}
			return;
		}
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("difficulty.title", "LOCAL8", "DIFICULTAD"), title);
		row += 34f;
		string[] array = new string[4]
		{
			Charms.SanitizeLocalized("difficulty.easy", "LOCAL8", "Fácil · 45%"),
			Charms.SanitizeLocalized("difficulty.normal", "LOCAL8", "Normal · 65%"),
			Charms.SanitizeLocalized("difficulty.hard", "LOCAL8", "Difícil · 85%"),
			Charms.SanitizeLocalized("difficulty.extreme", "LOCAL8", "Extremo · 100%")
		};
		float num3 = (cw - 10f) / 4f;
		for (int num2 = 0; num2 < 4; num2++)
		{
			if (GUI.Button(new Rect(5f + (float)num2 * num3, row, num3 - 5f, 34f), Help(((plugin.Difficulty.Value == num2) ? "> " : "") + array[num2], Charms.SanitizeLocalized("difficulty.tip", "LOCAL8", "Escalado por jugador vivo adicional: Fácil 45%, Normal 65%, Difícil 85%, Extremo 100%.")), button))
			{
				plugin.Difficulty.Value = num2;
			}
		}
		row += 53f;
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("camera.title", "LOCAL8", "CÁMARA"), title);
		row += 34f;
		if (GUI.Button(new Rect(5f, row, cw - 10f, 35f), Help(Charms.SanitizeLocalized("camera.shared", "LOCAL8", "Cámara compartida: ") + (plugin.ZoomByPlayerCount.Value ? Charms.SanitizeLocalized("common.yes", "LOCAL8", "SÍ") : Charms.SanitizeLocalized("common.no", "LOCAL8", "NO")), Charms.SanitizeLocalized("camera.tip", "LOCAL8", "Ajusta el zoom para mantener a todos los jugadores dentro de la pantalla.")), button))
		{
			plugin.ZoomByPlayerCount.Value = !plugin.ZoomByPlayerCount.Value;
		}
		row += 42f;
		GUI.Label(new Rect(5f, row + 5f, cw - 160f, 27f), Charms.SanitizeLocalized("camera.margin", "LOCAL8", "Margen lateral: ") + plugin.SideMargin.Value.ToString("0.0"), small);
		if (GUI.Button(new Rect(cw - 145f, row, 65f, 31f), "-", button))
		{
			plugin.SideMargin.Value = Mathf.Max(2.8f, plugin.SideMargin.Value - 0.5f);
		}
		if (GUI.Button(new Rect(cw - 70f, row, 65f, 31f), "+", button))
		{
			plugin.SideMargin.Value = Mathf.Min(10f, plugin.SideMargin.Value + 0.5f);
		}
		row += 43f;
		if (GUI.Button(new Rect(5f, row, cw - 10f, 35f), Help(Charms.SanitizeLocalized("darkness.label", "LOCAL8", "Visibilidad completa: ") + (plugin.GroupDarkness.Value ? Charms.SanitizeLocalized("common.no", "LOCAL8", "NO") : Charms.SanitizeLocalized("common.yes", "LOCAL8", "SÍ")), Charms.SanitizeLocalized("darkness.tip", "LOCAL8", "Muestra toda la sala; al desactivarla, solo se ve alrededor de los jugadores.")), button))
		{
			plugin.GroupDarkness.Value = !plugin.GroupDarkness.Value;
		}
		row += 42f;
		if (!plugin.GroupDarkness.Value)
		{
			if (GUI.Button(new Rect(5f, row, cw - 10f, 35f), Help(Charms.SanitizeLocalized("camera.player_lights", "LOCAL8", "Luces de jugadores: ") + ((!(plugin.GatherDistance.Value < 27.5f)) ? Charms.SanitizeLocalized("common.yes", "LOCAL8", "SÍ") : Charms.SanitizeLocalized("common.no", "LOCAL8", "NO")), Charms.SanitizeLocalized("camera.player_lights.tip", "LOCAL8", "Oculta P2-P8; P1 mantiene su luz.")), button))
			{
				plugin.GatherDistance.Value = ((!(plugin.GatherDistance.Value < 27.5f)) ? 27f : 28f);
			}
			row += 42f;
		}
		if (GUI.Button(new Rect(5f, row, cw - 10f, 35f), Help(Charms.SanitizeLocalized("wait.label", "LOCAL8", "Esperar al grupo: ") + (plugin.WaitForParty.Value ? Charms.SanitizeLocalized("common.yes", "LOCAL8", "SÍ") : Charms.SanitizeLocalized("common.no", "LOCAL8", "NO")), Charms.SanitizeLocalized("wait.tip", "LOCAL8", "Al salir de una sala, espera unos segundos para que el grupo pueda seguirte.")), button))
		{
			plugin.WaitForParty.Value = !plugin.WaitForParty.Value;
		}
		row += 42f;
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("revive.title", "LOCAL8", "REANIMACIÓN Y EFECTOS"), title);
		row += 34f;
		if (GUI.Button(new Rect(5f, row, cw * 0.48f, 35f), Help(Charms.SanitizeLocalized("respawn.label", "LOCAL8", "Reaparición automática: ") + (plugin.TimedRespawn.Value ? Charms.SanitizeLocalized("common.yes", "LOCAL8", "SÍ") : Charms.SanitizeLocalized("common.no", "LOCAL8", "NO")), Charms.SanitizeLocalized("respawn.tip", "LOCAL8", "Si queda alguien vivo, los jugadores caídos reaparecen tras el tiempo configurado.")), button))
		{
			plugin.TimedRespawn.Value = !plugin.TimedRespawn.Value;
		}
		if (GUI.Button(new Rect(cw * 0.51f, row, cw * 0.48f, 35f), Help(Charms.SanitizeLocalized("effects.label", "LOCAL8", "Efectos reducidos: ") + (plugin.ReduceHitEffects.Value ? Charms.SanitizeLocalized("common.yes", "LOCAL8", "SÍ") : Charms.SanitizeLocalized("common.no", "LOCAL8", "NO")), Charms.SanitizeLocalized("effects.tip", "LOCAL8", "Reduce destellos y pausas de impacto cuando atacan varios jugadores.")), button))
		{
			plugin.ReduceHitEffects.Value = !plugin.ReduceHitEffects.Value;
		}
		row += 47f;
		if (GUI.Button(new Rect(5f, row, cw - 10f, 35f), Help(Charms.SanitizeLocalized("shade.label", "LOCAL8", "Sombra al morir: ") + (plugin.SpawnShades.Value ? Charms.SanitizeLocalized("common.yes", "LOCAL8", "SÍ") : Charms.SanitizeLocalized("common.no", "LOCAL8", "NO")), Charms.SanitizeLocalized("shade.tip", "LOCAL8", "Crea una Sombra para cada jugador al morir.")), button))
		{
			plugin.SpawnShades.Value = !plugin.SpawnShades.Value;
		}
		row += 43f;
	}

	private static void TransitionBar(float w, float h)
	{
		if (TransitionVote.Pending)
		{
			float num = Mathf.Min(222f, w * 0.23f);
			float num2 = w - num - 23f;
			float num3 = h - 92f;
			RoomWaitArt.Draw(new Rect(num2 - 8f, num3 - 27f, num + 16f, 83f));
			Rect val = new Rect(num2, num3 - 8f, num, 22f);
			string obj = Charms.SanitizeLocalized("hud.waiting_bar", "LOCAL8", "ESPERANDO  ") + TransitionVote.Count;
			GUIStyle val2 = new GUIStyle(small)
			{
				font = RoomWaitArt.GameFont,
				alignment = (TextAnchor)4,
				fontSize = 13,
				fontStyle = (FontStyle)1
			};
			val2.normal.textColor = new Color(0.94f, 0.94f, 0.9f);
			GUI.Label(val, obj, val2);
			Box(new Rect(num2 + 28f, num3 + 23f, num - 56f, 2f), new Color(0.62f, 0.67f, 0.76f, 0.85f));
			Box(new Rect(num2 + 28f, num3 + 23f, (num - 56f) * TransitionVote.Progress, 2f), new Color(0.97f, 0.97f, 1f));
		}
	}

	private static void JoiningBar(CoopSession s, float w, float h)
	{
		PlayerSlot playerSlot = null;
		foreach (PlayerSlot player in s.Players)
		{
			if (player.Index > 0 && player.JoinVisualUntil > Time.unscaledTime)
			{
				playerSlot = player;
				break;
			}
		}
		if (playerSlot != null && !TransitionVote.Pending)
		{
			float num = Mathf.Min(206f, w * 0.23f);
			float num2 = w - num - 23f;
			float num3 = h - 89f;
			RoomWaitArt.Draw(new Rect(num2 - 8f, num3 - 24f, num + 16f, 77f));
			Rect val = new Rect(num2, num3 - 7f, num, 24f);
			string obj = "P" + (playerSlot.Index + 1) + " SE UNE";
			GUIStyle val2 = new GUIStyle(small)
			{
				font = RoomWaitArt.GameFont,
				alignment = (TextAnchor)4,
				fontSize = 14,
				fontStyle = (FontStyle)1
			};
			val2.normal.textColor = new Color(0.94f, 0.92f, 0.86f);
			GUI.Label(val, obj, val2);
			Box(new Rect(num2 + 34f, num3 + 26f, num - 68f, 2f), new Color(playerSlot.Color.r, playerSlot.Color.g, playerSlot.Color.b, 0.78f));
		}
	}

	private static void AdvancedOptions(Local8Runtime plugin, CoopSession s, float cw, ref float row)
	{
		if (tab == 4)
		{
			GameOptions(plugin, cw, ref row);
			return;
		}
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("advanced.menu", "LOCAL8", "MENÚ"), title);
		row += 35f;
		if (GUI.Button(new Rect(5f, row, cw - 120f, 33f), Help(MenuInput.Waiting ? Charms.SanitizeLocalized("bind.capture_any", "LOCAL8", "Pulsa una tecla o botón... Esc para cancelar") : (Charms.SanitizeLocalized("advanced.open", "LOCAL8", "Abrir menú: ") + MenuInput.Label), Charms.SanitizeLocalized("bind.help", "LOCAL8", "Selecciona una acción y pulsa la tecla o botón que quieras usar.")), button))
		{
			MenuInput.Waiting = true;
		}
		if (GUI.Button(new Rect(cw - 110f, row, 105f, 33f), Help(Charms.SanitizeLocalized("common.reset", "LOCAL8", "Restablecer"), Charms.SanitizeLocalized("advanced.reset_tip", "LOCAL8", "Restaura F8 como tecla del panel.")), button))
		{
			MenuInput.Set((KeyCode)289);
		}
		row += 49f;
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("rescue.title", "LOCAL8", "RESCATE ONÍRICO"), title);
		row += 33f;
		if (GUI.Button(new Rect(5f, row, cw * 0.61f - 8f, 33f), Help((ActionKeys.Capturing == 1) ? Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar") : (Charms.SanitizeLocalized("common.keyboard", "LOCAL8", "Teclado: ") + ActionKeys.RescueLabel), Charms.SanitizeLocalized("rescue.tip", "LOCAL8", "Mantén la tecla para viajar como partículas oníricas hacia un compañero. Recarga: 25 s.")), button))
		{
			ActionKeys.Capturing = 1;
			MenuInput.Waiting = false;
		}
		if (GUI.Button(new Rect(cw * 0.61f, row, cw * 0.39f - 5f, 33f), Help(Charms.SanitizeLocalized("common.controller", "LOCAL8", "Mando: ") + ActionKeys.ButtonLabel, Charms.SanitizeLocalized("rescue.controller_tip", "LOCAL8", "Botón de rescate para jugadores con mando.")), button))
		{
			Local8Mod.Settings.RescueButton = (Local8Mod.Settings.RescueButton + 1) % 4;
		}
		row += 45f;
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("duel.title", "LOCAL8", "DUELOS"), title);
		row += 33f;
		if (GUI.Button(new Rect(5f, row, cw - 10f, 33f), Help((ActionKeys.Capturing == 2) ? Charms.SanitizeLocalized("bind.capture", "LOCAL8", "Pulsa una tecla... Esc para cancelar") : (Charms.SanitizeLocalized("duel.toggle", "LOCAL8", "Iniciar / detener PvP: ") + ActionKeys.PvpLabel), Charms.SanitizeLocalized("duel.tip", "LOCAL8", "Inicia o detiene las rondas de duelo.")), button))
		{
			ActionKeys.Capturing = 2;
			MenuInput.Waiting = false;
		}
		row += 48f;
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("virtual.title", "LOCAL8", "MANDOS VIRTUALES"), title);
		row += 35f;
		GUI.Label(new Rect(5f, row + 4f, cw - 210f, 30f), Charms.SanitizeLocalized("vjoy.label", "LOCAL8", "vJoy / DirectInput: ") + Controls.VirtualCount + Charms.SanitizeLocalized("vjoy.detected", "LOCAL8", " detectados") + (Controls.NativeActive ? "" : Charms.SanitizeLocalized("vjoy.inactive", "LOCAL8", " (lector inactivo)")), small);
		if (GUI.Button(new Rect(cw - 205f, row, 200f, 30f), Charms.SanitizeLocalized("vjoy.search", "LOCAL8", "Buscar vJoy"), button))
		{
			Controls.EnableDirectInput(requested: true);
		}
		row += 54f;
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("diagnostic.title", "LOCAL8", "DIAGNÓSTICO"), title);
		row += 35f;
		DrawLogButtons(5f, row, cw);
		row += 43f;
		if (!plugin.BasicHud.Value && !HudAssets.Ready)
		{
			GUI.Label(new Rect(5f, row, cw, 45f), Charms.SanitizeLocalized("hud.loading", "LOCAL8", "Cargando los gráficos del HUD..."), small);
		}
	}

	private static void PvpOptions(Local8Runtime plugin, CoopSession session, float cw, ref float row)
	{
		Local8Settings settings = Local8Mod.Settings;
		GUI.Label(new Rect(5f, row, cw, 27f), Charms.SanitizeLocalized("pvp.title", "LOCAL8", "COMBATE ENTRE JUGADORES"), title);
		row += 36f;
		string[] array = new string[3]
		{
			Charms.SanitizeLocalized("pvp.coop", "LOCAL8", "Cooperativo"),
			Charms.SanitizeLocalized("pvp.friendly", "LOCAL8", "Fuego amigo"),
			Charms.SanitizeLocalized("pvp.rounds", "LOCAL8", "Duelos por rondas")
		};
		float num = (cw - 10f) / 3f;
		for (int i = 0; i < 3; i++)
		{
			if (GUI.Button(new Rect(5f + (float)i * num, row, num - 5f, 35f), Help(((settings.PvpMode == i) ? "> " : "") + array[i], i switch
			{
				1 => Charms.SanitizeLocalized("pvp.friendly_tip", "LOCAL8", "Aventura cooperativa con daño entre jugadores."), 
				0 => Charms.SanitizeLocalized("pvp.coop_tip", "LOCAL8", "Aventura cooperativa sin daño entre jugadores."), 
				_ => Charms.SanitizeLocalized("pvp.rounds_tip", "LOCAL8", "Combates por rondas con marcador y reaparición."), 
			}), button))
			{
				PvpMatch.SetMode(i);
			}
		}
		row += 45f;
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("pvp.damage", "LOCAL8", "DAÑO POR GOLPE"), title);
		row += 34f;
		PvpNumber(Charms.SanitizeLocalized("pvp.nail", "LOCAL8", "Aguijón"), ref settings.PvpNailDamage, 1, 5, 1, cw, ref row);
		PvpNumber(Charms.SanitizeLocalized("pvp.spells", "LOCAL8", "Hechizos y rayos"), ref settings.PvpSpellDamage, 1, 5, 1, cw, ref row);
		PvpNumber(Charms.SanitizeLocalized("pvp.nailarts", "LOCAL8", "Artes del aguijón"), ref settings.PvpArtDamage, 1, 5, 1, cw, ref row);
		PvpNumber(Charms.SanitizeLocalized("pvp.charms", "LOCAL8", "Amuletos / mascotas / Sombra Afilada"), ref settings.PvpCharmDamage, 1, 5, 1, cw, ref row);
		if (GUI.Button(new Rect(5f, row, cw - 10f, 32f), Help(Charms.SanitizeLocalized("pvp.parry", "LOCAL8", "Parry: ") + (settings.PvpParry ? Charms.SanitizeLocalized("common.yes", "LOCAL8", "SÍ") : Charms.SanitizeLocalized("common.no", "LOCAL8", "NO")), Charms.SanitizeLocalized("pvp.parry_tip", "LOCAL8", "El choque cancela ambos ataques y da una breve invulnerabilidad.")), button))
		{
			settings.PvpParry = !settings.PvpParry;
		}
		row += 38f;
		if (GUI.Button(new Rect(5f, row, cw - 10f, 32f), Help(Charms.SanitizeLocalized("pvp.charm_attacks", "LOCAL8", "Ataques de amuletos: ") + (settings.PvpCharmAttacks ? Charms.SanitizeLocalized("common.yes", "LOCAL8", "SÍ") : Charms.SanitizeLocalized("common.no", "LOCAL8", "NO")), Charms.SanitizeLocalized("pvp.charm_attacks_tip", "LOCAL8", "Permite daño con mascotas, amuletos y Sombra Afilada.")), button))
		{
			settings.PvpCharmAttacks = !settings.PvpCharmAttacks;
		}
		row += 38f;
		if (GUI.Button(new Rect(5f, row, cw - 10f, 32f), Help(Charms.SanitizeLocalized("pvp.soul_hit", "LOCAL8", "Alma al acertar: ") + (settings.PvpSoulOnHit ? Charms.SanitizeLocalized("common.yes", "LOCAL8", "SÍ") : Charms.SanitizeLocalized("common.no", "LOCAL8", "NO")), Charms.SanitizeLocalized("pvp.soul_hit_tip", "LOCAL8", "Los golpes de aguijón contra otro jugador recuperan alma.")), button))
		{
			settings.PvpSoulOnHit = !settings.PvpSoulOnHit;
		}
		row += 43f;
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("pvp.teams", "LOCAL8", "EQUIPOS Y MARCADOR"), title);
		row += 33f;
		foreach (PlayerSlot player in session.Players)
		{
			int index = player.Index;
			Rect val = new Rect(5f, row + 4f, 40f, 25f);
			string obj = "P" + (index + 1);
			GUIStyle val2 = new GUIStyle(small);
			val2.normal.textColor = player.Color;
			GUI.Label(val, obj, val2);
			if (GUI.Button(new Rect(50f, row, 110f, 30f), Help((settings.PvpTeams[index] == 0) ? Charms.SanitizeLocalized("pvp.free", "LOCAL8", "Libre") : (Charms.SanitizeLocalized("pvp.team", "LOCAL8", "Equipo ") + settings.PvpTeams[index]), Charms.SanitizeLocalized("pvp.free_tip", "LOCAL8", "Libre: todos contra todos. Los compañeros de equipo no se hacen daño.")), button))
			{
				PvpMatch.Stop(restorePosition: true);
				settings.PvpTeams[index] = (settings.PvpTeams[index] + 1) % 5;
				PvpCombat.Invalidate();
			}
			GUI.Label(new Rect(175f, row + 5f, cw - 180f, 25f), "Rondas " + PvpCombat.Wins[index] + Charms.SanitizeLocalized("pvp.kills", "LOCAL8", "   Bajas ") + PvpCombat.Kills[index] + Charms.SanitizeLocalized("pvp.deaths", "LOCAL8", "   Caídas ") + PvpCombat.Deaths[index] + Charms.SanitizeLocalized("pvp.parries", "LOCAL8", "   Parries ") + PvpCombat.Parries[index], small);
			row += 37f;
		}
		row += 8f;
		GUI.Label(new Rect(5f, row, cw, 26f), Charms.SanitizeLocalized("pvp.round_settings", "LOCAL8", "RONDAS"), title);
		row += 33f;
		PvpNumber(Charms.SanitizeLocalized("pvp.start_soul", "LOCAL8", "Alma inicial"), ref settings.DuelSoul, 0, 99, 33, cw, ref row);
		PvpNumber(Charms.SanitizeLocalized("pvp.round_time", "LOCAL8", "Tiempo por ronda (segundos)"), ref settings.DuelSeconds, 30, 600, 30, cw, ref row);
		GUI.Label(new Rect(5f, row + 5f, cw - 155f, 26f), Charms.SanitizeLocalized("pvp.bestof", "LOCAL8", "Mejor de: ") + ((settings.DuelBestOf == 0) ? Charms.SanitizeLocalized("pvp.unlimited", "LOCAL8", "sin límite") : settings.DuelBestOf.ToString()), small);
		if (GUI.Button(new Rect(cw - 145f, row, 65f, 29f), "-", button))
		{
			settings.DuelBestOf = PvpRules.SeriesLength(settings.DuelBestOf - 2);
		}
		if (GUI.Button(new Rect(cw - 70f, row, 65f, 29f), "+", button))
		{
			settings.DuelBestOf = PvpRules.SeriesLength((settings.DuelBestOf == 0) ? 1 : (settings.DuelBestOf + 2));
		}
		row += 36f;
		if (GUI.Button(new Rect(5f, row, cw - 10f, 31f), Help(Charms.SanitizeLocalized("pvp.duel_key", "LOCAL8", "Tecla de duelo: ") + ((ActionKeys.Capturing == 2) ? Charms.SanitizeLocalized("bind.capture_short", "LOCAL8", "Pulsa una tecla...") : ActionKeys.PvpLabel), Charms.SanitizeLocalized("pvp.duel_key_tip", "LOCAL8", "Cambia la tecla que inicia o detiene el PvP.")), button))
		{
			ActionKeys.Capturing = 2;
			MenuInput.Waiting = false;
		}
		row += 39f;
		if (GUI.Button(new Rect(5f, row, cw * 0.48f, 34f), Help(PvpMatch.Running ? Charms.SanitizeLocalized("pvp.end_duel", "LOCAL8", "Terminar duelo") : Charms.SanitizeLocalized("pvp.start_rounds", "LOCAL8", "Iniciar rondas"), Charms.SanitizeLocalized("pvp.start_tip", "LOCAL8", "Empieza desde las posiciones actuales tras una cuenta atrás de 3 segundos.")), button))
		{
			if (PvpMatch.Running)
			{
				PvpMatch.Stop(restorePosition: true);
				plugin.Notice(Charms.SanitizeLocalized("pvp.ended", "LOCAL8", "Duelo terminado."));
			}
			else
			{
				PvpMatch.SetMode(2);
				PvpMatch.RequestStart();
			}
		}
		if (GUI.Button(new Rect(cw * 0.51f, row, cw * 0.48f, 34f), Help(Charms.SanitizeLocalized("pvp.clear", "LOCAL8", "Borrar marcador"), Charms.SanitizeLocalized("pvp.clear_tip", "LOCAL8", "Reinicia rondas ganadas, bajas y parries.")), button))
		{
			PvpCombat.ResetScores();
		}
		row += 42f;
		if (!string.IsNullOrEmpty(PvpMatch.StartIssue))
		{
			Rect val3 = new Rect(5f, row, cw - 10f, 42f);
			string startIssue = PvpMatch.StartIssue;
			GUIStyle val4 = new GUIStyle(small)
			{
				wordWrap = true
			};
			val4.normal.textColor = new Color(1f, 0.75f, 0.65f);
			GUI.Label(val3, startIssue, val4);
			row += 46f;
		}
	}

	private static void PvpNumber(string label, ref int value, int min, int max, int step, float cw, ref float row)
	{
		GUI.Label(new Rect(5f, row + 5f, cw - 155f, 26f), label + ": " + value, small);
		if (GUI.Button(new Rect(cw - 145f, row, 65f, 29f), "-", button))
		{
			value = Mathf.Max(min, value - step);
		}
		if (GUI.Button(new Rect(cw - 70f, row, 65f, 29f), "+", button))
		{
			value = Mathf.Min(max, value + step);
		}
		row += 35f;
	}

	private static void FindNativeFont()
	{
		if (Object.op_Implicit((Object)(object)nativeFont) || !(Time.unscaledTime >= nextFontSearch))
		{
			return;
		}
		nextFontSearch = Time.unscaledTime + 5f;
		Font[] array = Resources.FindObjectsOfTypeAll<Font>();
		foreach (Font val in array)
		{
			if (((Object)val).name.IndexOf("Trajan", StringComparison.OrdinalIgnoreCase) >= 0 || ((Object)val).name.IndexOf("Perpetua", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				nativeFont = val;
				break;
			}
		}
	}

	private static void DrawPvp(CoopSession s, float w, float scale)
	{
		GameManager instance = GameManager.instance;
		if (PvpMatch.Running && !Plugin.Self.Panel && Object.op_Implicit((Object)(object)instance) && !instance.isPaused && s.Gameplay && PvpMatch.BlocksInput)
		{
			FindNativeFont();
			if (Object.op_Implicit((Object)(object)nativeFont))
			{
				GUIStyle val = new GUIStyle(tag)
				{
					font = nativeFont,
					fontStyle = (FontStyle)0,
					fontSize = 27
				};
				val.normal.textColor = new Color(0.94f, 0.91f, 0.85f);
				GUIStyle val2 = val;
				string announcement = PvpMatch.Announcement;
				Rect val3 = new Rect(1f, (float)Screen.height / scale * 0.32f + 1f, w, 42f);
				GUIStyle val4 = new GUIStyle(val2);
				val4.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
				GUI.Label(val3, announcement, val4);
				GUI.Label(new Rect(0f, (float)Screen.height / scale * 0.32f, w, 42f), announcement, val2);
			}
		}
	}

	private static GUIContent Help(string label, string tip)
	{
		return new GUIContent(label, tip);
	}

	private static void DrawTooltip(float w, float h)
	{
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_005f: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 7)
		{
			string text = GUI.tooltip ?? "";
			if (text != lastTip)
			{
				lastTip = text;
				tipSince = Time.unscaledTime;
			}
			if (text.Length != 0 && !(Time.unscaledTime - tipSince < 0.35f) && (int)Event.current.type == 7)
			{
				float num = Mathf.Min(345f, w - 24f);
				GUIStyle val = new GUIStyle(small)
				{
					wordWrap = true,
					padding = new RectOffset(12, 12, 9, 9)
				};
				float num2 = val.CalcHeight(new GUIContent(text), num);
				Vector2 mousePosition = Event.current.mousePosition;
				Rect val2 = new Rect(Mathf.Clamp(mousePosition.x + 16f, 8f, w - num - 8f), Mathf.Clamp(mousePosition.y + 20f, 8f, h - num2 - 8f), num, num2);
				Box(val2, new Color(0.035f, 0.04f, 0.06f, 0.98f));
				GUI.Label(val2, text, val);
			}
		}
	}

	private static void DrawLogButtons(float x, float y, float width)
	{
		if (GUI.Button(new Rect(x, y, width * 0.48f, 30f), "Abrir carpeta de logs", button))
		{
			Diagnostics.OpenFolder();
		}
		if (GUI.Button(new Rect(x + width * 0.5f, y, width * 0.48f, 30f), "Copiar ruta de logs", button))
		{
			Diagnostics.CopyFolder();
		}
	}

	private static void DrawRevival(CoopSession s, float scale)
	{
		Camera main = Camera.main;
		if (!Object.op_Implicit((Object)(object)main))
		{
			return;
		}
		foreach (PlayerSlot player in s.Players)
		{
			if (!player.Reviving || !Object.op_Implicit((Object)(object)player.Hero))
			{
				continue;
			}
			Vector3 val = main.WorldToScreenPoint(((Component)player.Hero).transform.position + Vector3.up * 2f);
			if (!(val.z <= 0f))
			{
				float num = val.x / scale;
				float num2 = ((float)Screen.height - val.y) / scale;
				Box(new Rect(num - 49f, num2 - 2f, 98f, 9f), new Color(0f, 0f, 0f, 0.7f));
				Bar(new Rect(num - 46f, num2, 92f, 5f), player.ReviveProgress, Color.Lerp(player.Color, Color.white, 0.4f));
				Rect val2 = new Rect(num - 60f, num2 - 23f, 120f, 20f);
				GUIStyle val3 = new GUIStyle(small)
				{
					alignment = (TextAnchor)4
				};
				val3.normal.textColor = player.Color;
				GUI.Label(val2, "Reanimando", val3);
				HudAssets.Images images = HudAssets.For(player);
				Texture2D val4 = ((images != null) ? images.Soul : HudAssets.Soul);
				if (Object.op_Implicit((Object)(object)val4))
				{
					float num3 = 16f + Mathf.Sin(Time.unscaledTime * 7f) * 2f;
					Icon((Texture)(object)val4, new Rect(num - num3 / 2f, num2 - 45f, num3, num3), new Color(player.Color.r, player.Color.g, player.Color.b, 0.5f + 0.5f * player.ReviveProgress));
				}
			}
		}
	}

	private static void NativeCharmCards(CoopSession s, float w, float h, float scale)
	{
		if (Charms.NativeMenuOpen)
		{
			if (s.Primary != null && PlayerContext.Current == null)
			{
				s.Primary.Charms.Read(s.Data);
			}
			int count = s.Players.Count;
			int num = ((count <= 3) ? count : 4);
			int num2 = (count + num - 1) / num;
			Rect r = default(Rect);
			((Rect)(ref r))._002Ector(w * 0.104f, h * 0.185f, w * 0.478f, h * 0.294f);
			Box(r, new Color(0.035f, 0.027f, 0.03f, 0.97f));
			float num3 = 4f;
			float num4 = (((Rect)(ref r)).width - num3 * (float)(num + 1)) / (float)num;
			float num5 = (((Rect)(ref r)).height - num3 * (float)(num2 + 1)) / (float)num2;
			Rect r2 = default(Rect);
			for (int i = 0; i < count; i++)
			{
				PlayerSlot p = s.Players[i];
				((Rect)(ref r2))._002Ector(((Rect)(ref r)).x + num3 + (float)(i % num) * (num4 + num3), ((Rect)(ref r)).y + num3 + (float)(i / num) * (num5 + num3), num4, num5);
				DrawCharmCard(p, s.Data, r2);
			}
			DrawNativeCharmCursors(s, scale);
		}
	}

	private static void DrawNativeCharmCursors(CoopSession s, float scale)
	{
		GameCameras instance = GameCameras.instance;
		if (!Object.op_Implicit((Object)(object)instance) || !Object.op_Implicit((Object)(object)instance.hudCamera))
		{
			return;
		}
		foreach (PlayerSlot player in s.Players)
		{
			if (!player.Alive || !player.Ready)
			{
				continue;
			}
			int num = Charms.NativeCursor[player.Index];
			InvCharmBackboard val = null;
			InvCharmBackboard[] nativeBoards = Charms.NativeBoards;
			foreach (InvCharmBackboard val2 in nativeBoards)
			{
				if (Object.op_Implicit((Object)(object)val2) && val2.charmNum == num)
				{
					val = val2;
					break;
				}
			}
			if (!Object.op_Implicit((Object)(object)val))
			{
				continue;
			}
			Vector3 val3 = instance.hudCamera.WorldToScreenPoint(((Component)val).transform.position);
			if (val3.z <= 0f)
			{
				continue;
			}
			float num2 = val3.x / scale;
			float num3 = ((float)Screen.height - val3.y) / scale;
			if (num2 < 20f || num2 > (float)Screen.width / scale - 20f || num3 < 20f || num3 > (float)Screen.height / scale - 20f)
			{
				continue;
			}
			int num4 = 0;
			foreach (PlayerSlot player2 in s.Players)
			{
				if (player2.Index < player.Index && Charms.NativeCursor[player2.Index] == num)
				{
					num4++;
				}
			}
			float num5 = (float)(58 + num4 * 9) * 0.5f;
			Color color = player.Color;
			float num6 = 2.5f;
			float num7 = 11f;
			Box(new Rect(num2 - num5, num3 - num5, num7, num6), color);
			Box(new Rect(num2 - num5, num3 - num5, num6, num7), color);
			Box(new Rect(num2 + num5 - num7, num3 - num5, num7, num6), color);
			Box(new Rect(num2 + num5 - num6, num3 - num5, num6, num7), color);
			Box(new Rect(num2 - num5, num3 + num5 - num6, num7, num6), color);
			Box(new Rect(num2 - num5, num3 + num5 - num7, num6, num7), color);
			Box(new Rect(num2 + num5 - num7, num3 + num5 - num6, num7, num6), color);
			Box(new Rect(num2 + num5 - num6, num3 + num5 - num7, num6, num7), color);
			Rect val4 = new Rect(num2 - num5, num3 - num5 - 23f, 38f, 21f);
			string obj = "P" + (player.Index + 1);
			GUIStyle val5 = new GUIStyle(tag)
			{
				fontSize = 14
			};
			val5.normal.textColor = color;
			GUI.Label(val4, obj, val5);
		}
	}

	private static void DrawCharmCard(PlayerSlot p, PlayerData data, Rect r)
	{
		Box(r, new Color(0.015f, 0.013f, 0.023f, 0.93f));
		Box(new Rect(((Rect)(ref r)).x, ((Rect)(ref r)).y, 3f, ((Rect)(ref r)).height), p.Color);
		Box(new Rect(((Rect)(ref r)).x, ((Rect)(ref r)).y, Mathf.Max(0f, ((Rect)(ref r)).width), 1f), new Color(p.Color.r, p.Color.g, p.Color.b, 0.6f));
		float num = 8f;
		float num2 = Mathf.Clamp(((Rect)(ref r)).height * 0.19f, 15f, 25f);
		float num3 = Mathf.Clamp(((Rect)(ref r)).height * 0.34f, 18f, 48f);
		Rect val = new Rect(((Rect)(ref r)).x + num, ((Rect)(ref r)).y + 2f, ((Rect)(ref r)).width - num * 2f, num2);
		string obj = "P" + (p.Index + 1);
		GUIStyle val2 = new GUIStyle(title)
		{
			fontSize = Mathf.RoundToInt(Mathf.Clamp(((Rect)(ref r)).height * 0.17f, 14f, 23f))
		};
		val2.normal.textColor = p.Color;
		GUI.Label(val, obj, val2);
		if (data.GetBool("gotCharm_" + Charms.NativeCursor[p.Index]))
		{
			Rect val3 = new Rect(((Rect)(ref r)).x + ((Rect)(ref r)).width * 0.2f, ((Rect)(ref r)).y + 5f, ((Rect)(ref r)).width * 0.75f, num2);
			string obj2 = Charms.Name(Charms.NativeCursor[p.Index]);
			GUIStyle val4 = new GUIStyle(small)
			{
				fontSize = 10,
				alignment = (TextAnchor)5,
				clipping = (TextClipping)1
			};
			val4.normal.textColor = p.Color;
			GUI.Label(val3, obj2, val4);
		}
		int[] array = p.Charms.Ids();
		int num4 = Mathf.Clamp(Mathf.FloorToInt((((Rect)(ref r)).width - num * 2f) / 36f), 4, 10);
		int num5 = Mathf.Max(1, Mathf.CeilToInt((float)array.Length / (float)num4));
		float num6 = Mathf.Max(18f, ((Rect)(ref r)).height - num2 - 48f);
		float num7 = Mathf.Min(num3, Mathf.Min((((Rect)(ref r)).width - num * 2f) / (float)num4, num6 / (float)num5));
		Rect val5 = default(Rect);
		for (int i = 0; i < array.Length; i++)
		{
			((Rect)(ref val5))._002Ector(((Rect)(ref r)).x + num + (float)(i % num4) * num7, ((Rect)(ref r)).y + num2 + 5f + (float)(i / num4) * num7, num7 - 2f, num7 - 2f);
			Texture2D val6 = Charms.Icon(array[i]);
			if (Object.op_Implicit((Object)(object)val6))
			{
				Icon((Texture)(object)val6, val5, Color.white);
			}
			else
			{
				GUI.Label(val5, array[i].ToString(), tag);
			}
		}
		float num8 = ((Rect)(ref r)).yMax - Mathf.Clamp(((Rect)(ref r)).height * 0.26f, 22f, 35f);
		int num9 = Charms.NativeCursor[p.Index];
		if (num9 >= 1 && num9 <= 40 && data.GetBool("gotCharm_" + num9) && ((Rect)(ref r)).height > 108f)
		{
			float num10 = ((Rect)(ref r)).y + num2 + 9f + (float)num5 * num7;
			float num11 = num8 - num10 - 10f;
			if (num11 > 23f)
			{
				Rect val7 = new Rect(((Rect)(ref r)).x + num, num10, ((Rect)(ref r)).width - num * 2f, num11);
				string obj3 = Charms.Description(num9);
				GUIStyle val8 = new GUIStyle(small)
				{
					fontSize = Mathf.Clamp(Mathf.RoundToInt(((Rect)(ref r)).height / 24f), 9, 12),
					wordWrap = true,
					richText = true,
					clipping = (TextClipping)1
				};
				val8.normal.textColor = new Color(0.9f, 0.88f, 0.85f);
				GUI.Label(val7, obj3, val8);
			}
		}
		if (!Charms.CanEdit(p))
		{
			Rect val9 = new Rect(((Rect)(ref r)).x + num, num8 - 31f, ((Rect)(ref r)).width - num * 2f, 18f);
			GUIStyle val10 = new GUIStyle(small)
			{
				fontSize = 10,
				clipping = (TextClipping)1
			};
			val10.normal.textColor = Color.Lerp(p.Color, Color.white, 0.5f);
			GUI.Label(val9, "El jugador no esta cerca de un banco", val10);
		}
		Rect val11 = new Rect(((Rect)(ref r)).x + num, num8 - 12f, ((Rect)(ref r)).width - num * 2f, 15f);
		string obj4 = "Muescas  " + p.Charms.Used + "/" + data.charmSlots + (p.Charms.Overcharmed ? "  SOBRECARGA" : "");
		GUIStyle val12 = new GUIStyle(small)
		{
			fontSize = 10,
			clipping = (TextClipping)1
		};
		val12.normal.textColor = new Color(0.9f, 0.9f, 0.89f);
		GUI.Label(val11, obj4, val12);
		int num12 = Mathf.Clamp(data.charmSlots, 0, 11);
		int num13 = Mathf.Max(num12, Mathf.Min(16, p.Charms.Used));
		float num14 = (((Rect)(ref r)).width - num * 2f) / (float)Mathf.Max(1, num13);
		float num15 = Mathf.Clamp(num14 * 0.57f, 4f, 13f);
		for (int j = 0; j < num13; j++)
		{
			bool num16 = j >= num12;
			bool flag = j < p.Charms.Used;
			Color c = (Color)(num16 ? new Color(0.85f, 0.48f, 0.96f, 0.95f) : (flag ? Color.Lerp(p.Color, Color.white, 0.55f) : new Color(0.66f, 0.67f, 0.74f, 0.9f)));
			float num17 = ((Rect)(ref r)).x + num + (float)j * num14 + (num14 - num15) * 0.5f;
			float num18 = ((Rect)(ref r)).yMax - num15 - 5f;
			Box(new Rect(num17, num18, num15, num15), c);
			Box(new Rect(num17 + num15 * 0.3f, num18 + num15 * 0.3f, num15 * 0.4f, num15 * 0.4f), (Color)(flag ? Color.white : new Color(0.22f, 0.23f, 0.28f, 0.9f)));
		}
	}

	private static void CharmPanel(Local8Runtime plugin, float w, float h)
	{
		CoopSession session = plugin.Session;
		PlayerSlot playerSlot = session.Players.FirstOrDefault((PlayerSlot v) => v.Index == Charms.Selected);
		if (playerSlot == null)
		{
			Charms.Close();
			return;
		}
		float num = Mathf.Min(840f, w - 24f);
		float num2 = Mathf.Min(790f, h - 24f);
		float num3 = (w - num) / 2f;
		float num4 = (h - num2) / 2f;
		Box(new Rect(num3, num4, num, num2), new Color(0.025f, 0.035f, 0.065f, 0.99f));
		Rect val = new Rect(num3 + 20f, num4 + 15f, num - 130f, 32f);
		string obj = "AMULETOS - P" + (playerSlot.Index + 1);
		GUIStyle val2 = new GUIStyle(title);
		val2.normal.textColor = playerSlot.Color;
		GUI.Label(val, obj, val2);
		if (GUI.Button(new Rect(num3 + num - 105f, num4 + 15f, 85f, 30f), "Volver", button))
		{
			Charms.Close();
		}
		float num5 = num3 + 20f;
		foreach (PlayerSlot player in session.Players)
		{
			Box(new Rect(num5, num4 + 55f, 55f, 28f), (player.Index == playerSlot.Index) ? new Color(player.Color.r, player.Color.g, player.Color.b, 0.42f) : new Color(player.Color.r, player.Color.g, player.Color.b, 0.15f));
			GUI.Label(new Rect(num5, num4 + 55f, 55f, 28f), "P" + (player.Index + 1), tag);
			num5 += 60f;
		}
		GUI.Label(new Rect(num3 + 20f, num4 + 94f, num - 40f, 44f), "Muescas: " + playerSlot.Charms.Used + " / " + session.Data.charmSlots + (playerSlot.Charms.Overcharmed ? "  SOBREHECHIZADO" : "") + "   " + (Charms.CanEdit(playerSlot) ? "Puedes cambiar tus amuletos" : "Descansa en un banco y permanece junto a el"), small);
		float num6 = Mathf.Min(72f, (num - 40f) / 10f);
		float num7 = num3 + (num - num6 * 10f) / 2f;
		float num8 = num4 + 142f;
		Rect val3 = default(Rect);
		for (int num9 = 1; num9 <= 40; num9++)
		{
			((Rect)(ref val3))._002Ector(num7 + (float)((num9 - 1) % 10) * num6, num8 + (float)((num9 - 1) / 10) * num6, num6 - 5f, num6 - 5f);
			bool flag = session.Data.GetBool("gotCharm_" + num9);
			bool flag2 = playerSlot.Charms.Equipped[num9 - 1];
			Box(val3, (num9 == Charms.Cursor) ? new Color(0.38f, 0.42f, 0.55f, 0.9f) : (flag2 ? new Color(playerSlot.Color.r, playerSlot.Color.g, playerSlot.Color.b, 0.32f) : new Color(0.13f, 0.14f, 0.18f, 0.7f)));
			if (flag)
			{
				Texture2D val4 = Charms.Icon(num9);
				if (Object.op_Implicit((Object)(object)val4))
				{
					Icon((Texture)(object)val4, new Rect(((Rect)(ref val3)).x + 5f, ((Rect)(ref val3)).y + 3f, ((Rect)(ref val3)).width - 10f, ((Rect)(ref val3)).height - 13f), (Color)(flag2 ? Color.white : new Color(0.7f, 0.7f, 0.7f)));
				}
				else
				{
					GUI.Label(val3, num9.ToString(), tag);
				}
			}
			else
			{
				GUI.Label(val3, "?", tag);
			}
			if (playerSlot.Index == 0 && GUI.Button(val3, "", GUIStyle.none))
			{
				Charms.Cursor = num9;
				if (flag)
				{
					Charms.Toggle(playerSlot, num9);
				}
			}
			if (playerSlot.Index == 0 && ((Rect)(ref val3)).Contains(Event.current.mousePosition))
			{
				Charms.Cursor = num9;
			}
			if (flag2)
			{
				GUI.Label(new Rect(((Rect)(ref val3)).x + 3f, ((Rect)(ref val3)).yMax - 15f, ((Rect)(ref val3)).width - 6f, 16f), "Equipado", new GUIStyle(small)
				{
					fontSize = 9,
					alignment = (TextAnchor)4
				});
			}
		}
		int cursor = Charms.Cursor;
		float num10 = num8 + 4f * num6 + 12f;
		bool flag3 = session.Data.GetBool("gotCharm_" + cursor);
		GUI.Label(new Rect(num3 + 20f, num10, num - 40f, 30f), flag3 ? (Charms.Name(cursor) + " - " + session.Data.GetInt("charmCost_" + cursor) + " muescas") : "Amuleto sin descubrir", text);
		if (flag3)
		{
			GUI.Label(new Rect(num3 + 20f, num10 + 34f, num - 40f, Mathf.Max(40f, num2 - (num10 - num4) - 105f)), Charms.Description(cursor), new GUIStyle(small)
			{
				wordWrap = true,
				richText = true
			});
		}
		GUI.Label(new Rect(num3 + 20f, num4 + num2 - 60f, num - 40f, 45f), (Time.unscaledTime < plugin.MessageUntil) ? plugin.Message : "Cruceta: seleccionar. Confirmar: equipar/quitar. Cancelar: cerrar. Cada P conserva su propia seleccion.", new GUIStyle(small)
		{
			wordWrap = true
		});
	}
}
