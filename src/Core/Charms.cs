using System;
using System.Collections.Generic;
using System.Linq;
using Language;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class Charms
{
	internal static bool Editing;

	internal static int Selected;

	internal static int Cursor = 1;

	private static float openedAt;

	internal static readonly int[] NativeCursor = new int[8];

	private static readonly float[] nextMove = new float[8];

	private static readonly int[] moveDirection = new int[8];

	private static readonly bool[] submitHeld = new bool[8];

	private static readonly Rigidbody2D[] frozen = new Rigidbody2D[8];

	private static readonly bool[] wasKinematic = new bool[8];

	private static GameObject nativePane;

	private static InvCharmBackboard[] nativeBoards = new InvCharmBackboard[0];

	private static int nativeFrame = -1;

	private static bool nativeVisible;

	internal static bool wasNative;

	private static readonly Dictionary<int, Texture2D> icons = new Dictionary<int, Texture2D>();

	internal static GameObject NativePane
	{
		get
		{
			ProbeNative();
			return nativePane;
		}
	}

	internal static bool NativeMenuOpen
	{
		get
		{
			ProbeNative();
			return CharmMenuExit.NativeActive(nativeVisible);
		}
	}

	internal static InvCharmBackboard[] NativeBoards
	{
		get
		{
			ProbeNative();
			return nativeBoards;
		}
	}

	internal static void SelectedBoard(InvCharmBackboard board)
	{
		if (!wasNative)
		{
			NativeCursor[0] = board.charmNum;
		}
	}

	internal static void SelectedEquipped()
	{
	}

	private static void ProbeNative()
	{
		if (nativeFrame == Time.frameCount)
		{
			return;
		}
		nativeFrame = Time.frameCount;
		nativeVisible = false;
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		GameManager instance = GameManager.instance;
		if (coopSession == null || !coopSession.Active || coopSession.Players.Count < 2 || !instance || !instance.IsGameplayScene() || instance.IsLoadingSceneTransition || Plugin.Self.Panel)
		{
			return;
		}
		GameObject gameObject = GameObject.FindWithTag("Charms Pane");
		if (!gameObject || !gameObject.activeInHierarchy)
		{
			return;
		}
		GameCameras instance2 = GameCameras.instance;
		if (!instance2 || !instance2.hudCamera)
		{
			return;
		}
		Vector3 vector = instance2.hudCamera.WorldToViewportPoint(gameObject.transform.position);
		if (vector.z <= 0f || vector.x < 0.05f || vector.x > 0.95f || vector.y < 0.05f || vector.y > 0.95f)
		{
			return;
		}
		if (nativePane != gameObject)
		{
			nativePane = gameObject;
			nativeBoards = gameObject.GetComponentsInChildren<InvCharmBackboard>(includeInactive: true);
			if (nativeBoards.Length == 0)
			{
				nativeBoards = UnityEngine.Object.FindObjectsOfType<InvCharmBackboard>();
			}
		}
		nativeVisible = true;
	}

	private static InvCharmBackboard Board(int id)
	{
		InvCharmBackboard[] array = NativeBoards;
		foreach (InvCharmBackboard invCharmBackboard in array)
		{
			if ((bool)invCharmBackboard && invCharmBackboard.charmNum == id)
			{
				return invCharmBackboard;
			}
		}
		return null;
	}

	internal static int FirstOwned(PlayerData data)
	{
		for (int i = 1; i <= 40; i++)
		{
			if (data.GetBool("gotCharm_" + i))
			{
				return i;
			}
		}
		return 1;
	}

	internal static int Neighbor(int current, int dx, int dy, PlayerData data)
	{
		List<CharmCell> list = new List<CharmCell>(40);
		InvCharmBackboard[] array = NativeBoards;
		foreach (InvCharmBackboard invCharmBackboard in array)
		{
			if ((bool)invCharmBackboard && invCharmBackboard.charmNum >= 1 && invCharmBackboard.charmNum <= 40)
			{
				Vector3 position = invCharmBackboard.transform.position;
				list.Add(new CharmCell(invCharmBackboard.charmNum, position.x, position.y, data.GetBool("gotCharm_" + invCharmBackboard.charmNum)));
			}
		}
		return CharmGridRules.Next(current, dx, dy, list);
	}

	internal static void Freeze(CoopSession s)
	{
		foreach (PlayerSlot player in s.Players)
		{
			if (player.Index > 0 && player.Alive && player.Ready && (bool)player.Hero)
			{
				Rigidbody2D component = player.Hero.GetComponent<Rigidbody2D>();
				if ((bool)component && frozen[player.Index] != component)
				{
					Release(player.Index);
					frozen[player.Index] = component;
					wasKinematic[player.Index] = component.isKinematic;
					component.velocity = Vector2.zero;
					component.angularVelocity = 0f;
					component.isKinematic = true;
				}
			}
		}
	}

	private static void Release(int index)
	{
		Rigidbody2D rigidbody2D = frozen[index];
		if ((bool)rigidbody2D)
		{
			rigidbody2D.isKinematic = wasKinematic[index];
		}
		frozen[index] = null;
	}

	internal static void ReleaseNative()
	{
		for (int i = 1; i < 8; i++)
		{
			Release(i);
		}
		Array.Clear(submitHeld, 0, 8);
		wasNative = false;
		nativeFrame = -1;
		nativePane = null;
		nativeBoards = new InvCharmBackboard[0];
		nativeVisible = false;
		CharmNativeUi.Release();
		CharmMenuExit.OnExit();
	}

	private static void NativeInput(CoopSession s)
	{
		RoleSystem.NativeInput(s);
	}

	internal static void Load(PlayerSlot p, PlayerData data)
	{
		RoleSystem.UnlockOvercharm(data);
		Local8SaveData save = Local8Mod.Save;
		if (save.Charms == null || save.Charms.Length != 8)
		{
			save.Charms = new int[8][];
		}
		if (p.Index == 0)
		{
			p.Charms.Read(data);
			return;
		}
		p.Charms.Initialize(data, save.Charms[p.Index] ?? (data.equippedCharms ?? new List<int>()).ToArray());
		UpdateMaximum(p, data, heal: false);
	}

	internal static void Save(CoopSession s)
	{
		if (PvpCharms.Save(s) || s == null)
		{
			return;
		}
		if (Local8Mod.Save.Charms == null || Local8Mod.Save.Charms.Length != 8)
		{
			Local8Mod.Save.Charms = new int[8][];
		}
		foreach (PlayerSlot player in s.Players)
		{
			Local8Mod.Save.Charms[player.Index] = player.Charms.Ids();
		}
	}

	internal static void UpdateMaximum(PlayerSlot p, PlayerData data, bool heal)
	{
		if (!PvpArena.OverrideMaximum(p, data, heal))
		{
			int num = RoleSystem.Maximum(data.maxHealthBase + ((p.Charms.Equipped[22] && !data.brokenCharm_23) ? 2 : 0), p);
			p.Vitals.Joni = (p.Charms.Equipped[26] ? ((int)((float)num * 1.4f)) : 0);
			p.Vitals.MaxHealth = ((p.Vitals.Joni > 0) ? 1 : num);
			if (heal)
			{
				p.Vitals.Health = p.CurrentMaxHealth;
			}
			else
			{
				p.Vitals.Health = Math.Min(p.Vitals.Health, p.CurrentMaxHealth);
			}
		}
	}

	internal static bool CanEdit(PlayerSlot p)
	{
		CoopSession session = Plugin.Self.Session;
		if (p != null && session != null && p.Alive && !BossSequenceController.BoundCharms)
		{
			if (!BenchSeats.Seated(p))
			{
				return session.NearRestedBench(p);
			}
			return true;
		}
		return false;
	}

	internal static void Open(PlayerSlot p)
	{
		if (p != null)
		{
			Editing = true;
			Selected = p.Index;
			Cursor = 1;
			openedAt = Time.unscaledTime;
			Plugin.Self.SetPanel(open: true);
		}
	}

	internal static void Close()
	{
		Editing = false;
	}

	internal static void InputTick(CoopSession s)
	{
		if (NativeMenuOpen && !Plugin.Self.Panel)
		{
			NativeInput(s);
			return;
		}
		if (wasNative)
		{
			ReleaseNative();
		}
		if (Editing)
		{
			PlayerSlot playerSlot = s.Players.FirstOrDefault((PlayerSlot x) => x.Index == Selected);
			if (playerSlot == null)
			{
				Close();
				return;
			}
			HeroActions actions = playerSlot.Actions;
			if (actions == null || Time.unscaledTime - openedAt < 0.2f)
			{
				return;
			}
			if (actions.menuCancel.WasPressed || actions.openInventory.WasPressed)
			{
				Close();
				Plugin.Self.SetPanel(open: false);
				return;
			}
			if (actions.left.WasPressed)
			{
				Cursor = (Cursor + 38) % 40 + 1;
			}
			if (actions.right.WasPressed)
			{
				Cursor = Cursor % 40 + 1;
			}
			if (actions.up.WasPressed)
			{
				Cursor = (Cursor + 29) % 40 + 1;
			}
			if (actions.down.WasPressed)
			{
				Cursor = (Cursor + 9) % 40 + 1;
			}
			if (actions.menuSubmit.WasPressed)
			{
				Toggle(playerSlot, Cursor);
			}
		}
		else
		{
			if (!s.Active || !s.Gameplay || Plugin.Self.Panel)
			{
				return;
			}
			foreach (PlayerSlot player in s.Players)
			{
				if (player.Index > 0 && player.Actions != null && player.Actions.openInventory.WasPressed && player.Alive)
				{
					Open(player);
					break;
				}
			}
		}
	}

	internal static void Toggle(PlayerSlot p, int id)
	{
		CoopSession session = Plugin.Self.Session;
		PlayerData data = session.Data;
		if (!CanEdit(p))
		{
			Plugin.Self.Notice("Descansa en un banco y permanece junto a el para cambiar los amuletos.");
		}
		else
		{
			if (id < 1 || id > 40)
			{
				return;
			}
			bool flag = !p.Charms.Equipped[id - 1];
			if (flag && !data.GetBool("gotCharm_" + id))
			{
				return;
			}
			if (!p.Charms.Equipped[id - 1] && id >= 23 && id <= 25 && data.GetBool("brokenCharm_" + id))
			{
				Plugin.Self.Notice("Repara este amuleto antes de equiparlo.");
				return;
			}
			int cost = Math.Max(0, data.GetInt("charmCost_" + id));
			if (!p.Charms.Equipped[id - 1] && !CoopRules.CanEquip(p.Charms.Used, cost, data.charmSlots, data.canOvercharm))
			{
				Plugin.Self.Notice("No quedan muescas suficientes.");
				return;
			}
			using (PlayerContext.Enter(p))
			{
				data.SetBool("equippedCharm_" + id, flag);
				List<int> list = new List<int>(data.equippedCharms ?? new List<int>());
				list.RemoveAll((int value) => value == id);
				if (flag)
				{
					list.Add(id);
				}
				data.equippedCharms = list;
				data.CalculateNotchesUsed();
				data.overcharmed = data.charmSlotsFilled > data.charmSlots;
				p.Hero.CharmUpdate();
				if (id == 27 && p.Index > 0 && data.GetBool("equippedCharm_27") && data.joniHealthBlue > 0)
				{
					data.healthBlue = Math.Max(data.healthBlue, data.joniHealthBlue);
				}
				SummonRouting.Refresh(p);
				p.Capture(data);
			}
			Save(session);
			Diagnostics.Write("CHARMS P" + (p.Index + 1) + " " + (flag ? "equip=" : "remove=") + id + " loadout=" + string.Join(",", p.Charms.Ids()));
		}
	}

	internal static string Name(int id)
	{
		try
		{
			return global::Language.Language.Get("CHARM_NAME_" + id, "UI");
		}
		catch
		{
			return "Amuleto " + id;
		}
	}

	internal static string Description(int id)
	{
		try
		{
			return global::Language.Language.Get("CHARM_DESC_" + id, "UI").Replace("<br>", "\n");
		}
		catch
		{
			return "";
		}
	}

	internal static string SanitizeLocalized(string key, string sheet, string value)
	{
		if (sheet == "LOCAL8")
		{
			sheet = global::Language.Language.CurrentLanguage().ToString();
			switch (sheet)
			{
			case "ES":
				return UiLocalization.After(value, key, sheet, value);
			case "FR":
				switch (key)
				{
				case "darkness.label":
					return UiLocalization.After("Vue complète : ", key, sheet, value);
				case "darkness.tip":
					return UiLocalization.After("Salle entière ou autour des joueurs.", key, sheet, value);
				case "camera.player_lights":
					return UiLocalization.After("Lumières des joueurs : ", key, sheet, value);
				case "camera.player_lights.tip":
					return UiLocalization.After("Masque P2-P8; P1 reste éclairé.", key, sheet, value);
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return UiLocalization.After(value.Replace("PERFIL", "PROFIL").Replace("ESPACIO", "ESPACE").Replace("salto", "saut")
						.Replace("ataque", "attaque")
						.Replace("hechizo", "sort")
						.Replace("rápido", "carte rapide")
						.Replace("onírico", "aiguillon onirique"), key, sheet, value);
				}
				switch (key)
				{
				case "common.close":
					return UiLocalization.After("Fermer", key, sheet, value);
				case "tab.players":
					return UiLocalization.After("Joueurs et commandes", key, sheet, value);
				case "tab.game":
					return UiLocalization.After("Partie", key, sheet, value);
				case "tab.settings":
					return UiLocalization.After("Réglages", key, sheet, value);
				case "action.join":
					return UiLocalization.After("Rejoindre", key, sheet, value);
				case "action.gather":
					return UiLocalization.After("Rassembler", key, sheet, value);
				case "action.repair":
					return UiLocalization.After("Récupérer les joueurs", key, sheet, value);
				case "action.remove_last":
					return UiLocalization.After("Retirer le dernier", key, sheet, value);
				case "players.title":
					return UiLocalization.After("JOUEURS", key, sheet, value);
				case "keyboard.section":
					return UiLocalization.After("JOUEURS AU CLAVIER", key, sheet, value);
				case "keyboard.add":
					return UiLocalization.After("Ajouter un joueur au clavier", key, sheet, value);
				case "keyboard.new_title":
					return UiLocalization.After("NOUVEAU JOUEUR : CLAVIER", key, sheet, value);
				case "keyboard.new_intro":
					return UiLocalization.After("Configurez les commandes du nouveau joueur.", key, sheet, value);
				case "keyboard.limit":
					return UiLocalization.After("Maximum : 4 joueurs au clavier.", key, sheet, value);
				case "common.cancel":
					return UiLocalization.After("Annuler", key, sheet, value);
				case "common.back":
					return UiLocalization.After("Retour", key, sheet, value);
				case "common.reset":
					return UiLocalization.After("Réinitialiser", key, sheet, value);
				case "difficulty.title":
					return UiLocalization.After("DIFFICULTÉ", key, sheet, value);
				case "difficulty.easy":
					return UiLocalization.After("Facile · 45%", key, sheet, value);
				case "difficulty.normal":
					return UiLocalization.After("Normal · 65%", key, sheet, value);
				case "difficulty.hard":
					return UiLocalization.After("Difficile · 85%", key, sheet, value);
				case "difficulty.extreme":
					return UiLocalization.After("Extrême · 100%", key, sheet, value);
				case "difficulty.tip":
					return UiLocalization.After("Progression par joueur vivant supplémentaire : Facile 45 %, Normal 65 %, Difficile 85 %, Extrême 100 %.", key, sheet, value);
				case "camera.title":
					return UiLocalization.After("CAMÉRA", key, sheet, value);
				case "camera.shared":
					return UiLocalization.After("Caméra partagée : ", key, sheet, value);
				case "wait.label":
					return UiLocalization.After("Attendre le groupe : ", key, sheet, value);
				case "revive.title":
					return UiLocalization.After("RÉANIMATION ET EFFETS", key, sheet, value);
				case "respawn.label":
					return UiLocalization.After("Réapparition auto : ", key, sheet, value);
				case "effects.label":
					return UiLocalization.After("Effets réduits : ", key, sheet, value);
				case "shade.label":
					return UiLocalization.After("Ombre à la mort : ", key, sheet, value);
				case "common.no":
					return UiLocalization.After("NON", key, sheet, value);
				case "common.yes":
					return UiLocalization.After("OUI", key, sheet, value);
				case "advanced.menu":
					return UiLocalization.After("MENU", key, sheet, value);
				case "advanced.open":
					return UiLocalization.After("Ouvrir le menu : ", key, sheet, value);
				case "rescue.title":
					return UiLocalization.After("SAUVETAGE ONIRIQUE", key, sheet, value);
				case "duel.title":
					return UiLocalization.After("DUELS", key, sheet, value);
				case "virtual.title":
					return UiLocalization.After("MANETTES VIRTUELLES", key, sheet, value);
				case "diagnostic.title":
					return UiLocalization.After("DIAGNOSTIC", key, sheet, value);
				case "pvp.title":
					return UiLocalization.After("COMBAT ENTRE JOUEURS", key, sheet, value);
				case "pvp.coop":
					return UiLocalization.After("Coopération", key, sheet, value);
				case "pvp.friendly":
					return UiLocalization.After("Tir allié", key, sheet, value);
				case "pvp.rounds":
					return UiLocalization.After("Duels par manches", key, sheet, value);
				case "hud.recover_in":
					return UiLocalization.After("RÉAPPARITION DANS ", key, sheet, value);
				case "hud.waiting":
					return UiLocalization.After("ATTENTE", key, sheet, value);
				case "hud.waiting_bar":
					return UiLocalization.After("ATTENTE  ", key, sheet, value);
				case "hud.down":
					return UiLocalization.After("À TERRE", key, sheet, value);
				case "hud.eliminated":
					return UiLocalization.After("ÉLIMINÉ", key, sheet, value);
				case "hud.no_device":
					return UiLocalization.After("AUCUN PÉRIPHÉRIQUE", key, sheet, value);
				case "hud.returning":
					return UiLocalization.After("RETOUR", key, sheet, value);
				case "rescue.hud_hold":
					return UiLocalization.After("MAINTENIR ", key, sheet, value);
				case "rescue.hud_title":
					return UiLocalization.After("  -  SAUVETAGE ONIRIQUE", key, sheet, value);
				}
				break;
			case "DE":
				switch (key)
				{
				case "darkness.label":
					return UiLocalization.After("Volle Sicht: ", key, sheet, value);
				case "darkness.tip":
					return UiLocalization.After("Ganzer Raum oder nur um Spieler.", key, sheet, value);
				case "camera.player_lights":
					return UiLocalization.After("Spielerlichter: ", key, sheet, value);
				case "camera.player_lights.tip":
					return UiLocalization.After("P2-P8 aus; P1 bleibt beleuchtet.", key, sheet, value);
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return UiLocalization.After(value.Replace("PERFIL", "PROFIL").Replace("ESPACIO", "LEERTASTE").Replace("salto", "Sprung")
						.Replace("ataque", "Angriff")
						.Replace("hechizo", "Zauber")
						.Replace("rápido", "Schnellkarte")
						.Replace("onírico", "Traumnagel"), key, sheet, value);
				}
				switch (key)
				{
				case "common.close":
					return UiLocalization.After("Schließen", key, sheet, value);
				case "tab.players":
					return UiLocalization.After("Spieler & Steuerung", key, sheet, value);
				case "tab.game":
					return UiLocalization.After("Spiel", key, sheet, value);
				case "tab.settings":
					return UiLocalization.After("Einstellungen", key, sheet, value);
				case "action.join":
					return UiLocalization.After("Beitreten", key, sheet, value);
				case "action.gather":
					return UiLocalization.After("Sammeln", key, sheet, value);
				case "action.repair":
					return UiLocalization.After("Spieler wiederherstellen", key, sheet, value);
				case "action.remove_last":
					return UiLocalization.After("Letzten entfernen", key, sheet, value);
				case "players.title":
					return UiLocalization.After("SPIELER", key, sheet, value);
				case "keyboard.section":
					return UiLocalization.After("TASTATURSPIELER", key, sheet, value);
				case "keyboard.add":
					return UiLocalization.After("Tastaturspieler hinzufügen", key, sheet, value);
				case "keyboard.new_title":
					return UiLocalization.After("NEUER TASTATURSPIELER", key, sheet, value);
				case "keyboard.new_intro":
					return UiLocalization.After("Steuerung des neuen Spielers festlegen.", key, sheet, value);
				case "keyboard.limit":
					return UiLocalization.After("Maximal 4 Tastaturspieler.", key, sheet, value);
				case "common.cancel":
					return UiLocalization.After("Abbrechen", key, sheet, value);
				case "common.back":
					return UiLocalization.After("Zurück", key, sheet, value);
				case "common.reset":
					return UiLocalization.After("Zurücksetzen", key, sheet, value);
				case "difficulty.title":
					return UiLocalization.After("SCHWIERIGKEIT", key, sheet, value);
				case "difficulty.easy":
					return UiLocalization.After("Leicht · 45%", key, sheet, value);
				case "difficulty.normal":
					return UiLocalization.After("Normal · 65%", key, sheet, value);
				case "difficulty.hard":
					return UiLocalization.After("Schwer · 85%", key, sheet, value);
				case "difficulty.extreme":
					return UiLocalization.After("Extrem · 100%", key, sheet, value);
				case "difficulty.tip":
					return UiLocalization.After("Skalierung pro zusätzlichem lebenden Spieler: Leicht 45 %, Normal 65 %, Schwer 85 %, Extrem 100 %.", key, sheet, value);
				case "camera.title":
					return UiLocalization.After("KAMERA", key, sheet, value);
				case "camera.shared":
					return UiLocalization.After("Gemeinsame Kamera: ", key, sheet, value);
				case "wait.label":
					return UiLocalization.After("Auf Gruppe warten: ", key, sheet, value);
				case "revive.title":
					return UiLocalization.After("WIEDERBELEBUNG & EFFEKTE", key, sheet, value);
				case "respawn.label":
					return UiLocalization.After("Auto-Respawn: ", key, sheet, value);
				case "effects.label":
					return UiLocalization.After("Reduzierte Effekte: ", key, sheet, value);
				case "shade.label":
					return UiLocalization.After("Schatten beim Tod: ", key, sheet, value);
				case "common.no":
					return UiLocalization.After("NEIN", key, sheet, value);
				case "common.yes":
					return UiLocalization.After("JA", key, sheet, value);
				case "advanced.menu":
					return UiLocalization.After("MENÜ", key, sheet, value);
				case "advanced.open":
					return UiLocalization.After("Menü öffnen: ", key, sheet, value);
				case "rescue.title":
					return UiLocalization.After("TRAUMRETTUNG", key, sheet, value);
				case "duel.title":
					return UiLocalization.After("DUELLE", key, sheet, value);
				case "virtual.title":
					return UiLocalization.After("VIRTUELLE CONTROLLER", key, sheet, value);
				case "diagnostic.title":
					return UiLocalization.After("DIAGNOSE", key, sheet, value);
				case "pvp.title":
					return UiLocalization.After("SPIELERKAMPF", key, sheet, value);
				case "pvp.coop":
					return UiLocalization.After("Koop", key, sheet, value);
				case "pvp.friendly":
					return UiLocalization.After("Eigenbeschuss", key, sheet, value);
				case "pvp.rounds":
					return UiLocalization.After("Rundenduelle", key, sheet, value);
				case "hud.recover_in":
					return UiLocalization.After("RÜCKKEHR IN ", key, sheet, value);
				case "hud.waiting":
					return UiLocalization.After("WARTEN", key, sheet, value);
				case "hud.waiting_bar":
					return UiLocalization.After("WARTEN  ", key, sheet, value);
				case "hud.down":
					return UiLocalization.After("AM BODEN", key, sheet, value);
				case "hud.eliminated":
					return UiLocalization.After("AUSGESCHIEDEN", key, sheet, value);
				case "hud.no_device":
					return UiLocalization.After("KEIN GERÄT", key, sheet, value);
				case "hud.returning":
					return UiLocalization.After("RÜCKKEHR", key, sheet, value);
				case "rescue.hud_hold":
					return UiLocalization.After("HALTEN ", key, sheet, value);
				case "rescue.hud_title":
					return UiLocalization.After("  -  TRAUMRETTUNG", key, sheet, value);
				}
				break;
			case "IT":
				switch (key)
				{
				case "darkness.label":
					return UiLocalization.After("Vista completa: ", key, sheet, value);
				case "darkness.tip":
					return UiLocalization.After("Tutta la stanza o vicino ai giocatori.", key, sheet, value);
				case "camera.player_lights":
					return UiLocalization.After("Luci dei giocatori: ", key, sheet, value);
				case "camera.player_lights.tip":
					return UiLocalization.After("P2-P8 nascosti; P1 resta illuminato.", key, sheet, value);
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return UiLocalization.After(value.Replace("PERFIL", "PROFILO").Replace("ESPACIO", "SPAZIO").Replace("salto", "salto")
						.Replace("ataque", "attacco")
						.Replace("hechizo", "incantesimo")
						.Replace("rápido", "mappa rapida")
						.Replace("onírico", "aculeo onirico"), key, sheet, value);
				}
				switch (key)
				{
				case "common.close":
					return UiLocalization.After("Chiudi", key, sheet, value);
				case "tab.players":
					return UiLocalization.After("Giocatori e comandi", key, sheet, value);
				case "tab.game":
					return UiLocalization.After("Partita", key, sheet, value);
				case "tab.settings":
					return UiLocalization.After("Impostazioni", key, sheet, value);
				case "action.join":
					return UiLocalization.After("Unisciti", key, sheet, value);
				case "action.gather":
					return UiLocalization.After("Riunisci", key, sheet, value);
				case "action.repair":
					return UiLocalization.After("Recupera giocatori", key, sheet, value);
				case "action.remove_last":
					return UiLocalization.After("Rimuovi ultimo", key, sheet, value);
				case "players.title":
					return UiLocalization.After("GIOCATORI", key, sheet, value);
				case "keyboard.section":
					return UiLocalization.After("GIOCATORI DA TASTIERA", key, sheet, value);
				case "keyboard.add":
					return UiLocalization.After("Aggiungi giocatore da tastiera", key, sheet, value);
				case "keyboard.new_title":
					return UiLocalization.After("NUOVO GIOCATORE: TASTIERA", key, sheet, value);
				case "keyboard.new_intro":
					return UiLocalization.After("Configura i comandi del nuovo giocatore.", key, sheet, value);
				case "keyboard.limit":
					return UiLocalization.After("Massimo: 4 giocatori da tastiera.", key, sheet, value);
				case "common.cancel":
					return UiLocalization.After("Annulla", key, sheet, value);
				case "common.back":
					return UiLocalization.After("Indietro", key, sheet, value);
				case "common.reset":
					return UiLocalization.After("Ripristina", key, sheet, value);
				case "difficulty.title":
					return UiLocalization.After("DIFFICOLTÀ", key, sheet, value);
				case "difficulty.easy":
					return UiLocalization.After("Facile · 45%", key, sheet, value);
				case "difficulty.normal":
					return UiLocalization.After("Normale · 65%", key, sheet, value);
				case "difficulty.hard":
					return UiLocalization.After("Difficile · 85%", key, sheet, value);
				case "difficulty.extreme":
					return UiLocalization.After("Estrema · 100%", key, sheet, value);
				case "camera.title":
					return UiLocalization.After("TELECAMERA", key, sheet, value);
				case "camera.shared":
					return UiLocalization.After("Telecamera condivisa: ", key, sheet, value);
				case "wait.label":
					return UiLocalization.After("Attendi il gruppo: ", key, sheet, value);
				case "revive.title":
					return UiLocalization.After("RIANIMAZIONE ED EFFETTI", key, sheet, value);
				case "respawn.label":
					return UiLocalization.After("Respawn automatico: ", key, sheet, value);
				case "effects.label":
					return UiLocalization.After("Effetti ridotti: ", key, sheet, value);
				case "shade.label":
					return UiLocalization.After("Ombra alla morte: ", key, sheet, value);
				case "common.no":
					return UiLocalization.After("NO", key, sheet, value);
				case "common.yes":
					return UiLocalization.After("SÌ", key, sheet, value);
				case "advanced.menu":
					return UiLocalization.After("MENU", key, sheet, value);
				case "advanced.open":
					return UiLocalization.After("Apri menu: ", key, sheet, value);
				case "rescue.title":
					return UiLocalization.After("SALVATAGGIO ONIRICO", key, sheet, value);
				case "duel.title":
					return UiLocalization.After("DUELLI", key, sheet, value);
				case "virtual.title":
					return UiLocalization.After("CONTROLLER VIRTUALI", key, sheet, value);
				case "diagnostic.title":
					return UiLocalization.After("DIAGNOSTICA", key, sheet, value);
				case "pvp.title":
					return UiLocalization.After("COMBATTIMENTO TRA GIOCATORI", key, sheet, value);
				case "pvp.coop":
					return UiLocalization.After("Cooperativa", key, sheet, value);
				case "pvp.friendly":
					return UiLocalization.After("Fuoco amico", key, sheet, value);
				case "pvp.rounds":
					return UiLocalization.After("Duelli a round", key, sheet, value);
				case "hud.recover_in":
					return UiLocalization.After("RIENTRO TRA ", key, sheet, value);
				case "hud.waiting":
					return UiLocalization.After("IN ATTESA", key, sheet, value);
				case "hud.waiting_bar":
					return UiLocalization.After("IN ATTESA  ", key, sheet, value);
				case "hud.down":
					return UiLocalization.After("A TERRA", key, sheet, value);
				case "hud.eliminated":
					return UiLocalization.After("ELIMINATO", key, sheet, value);
				case "hud.no_device":
					return UiLocalization.After("NESSUN DISPOSITIVO", key, sheet, value);
				case "hud.returning":
					return UiLocalization.After("RITORNO", key, sheet, value);
				case "rescue.hud_hold":
					return UiLocalization.After("TIENI PREMUTO ", key, sheet, value);
				case "rescue.hud_title":
					return UiLocalization.After("  -  SALVATAGGIO ONIRICO", key, sheet, value);
				}
				break;
			case "PT":
				switch (key)
				{
				case "darkness.label":
					return UiLocalization.After("Visão completa: ", key, sheet, value);
				case "darkness.tip":
					return UiLocalization.After("Toda a sala ou só perto dos jogadores.", key, sheet, value);
				case "camera.player_lights":
					return UiLocalization.After("Luzes dos jogadores: ", key, sheet, value);
				case "camera.player_lights.tip":
					return UiLocalization.After("Oculta P2-P8; P1 mantém a luz.", key, sheet, value);
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return UiLocalization.After(value.Replace("PERFIL", "PERFIL").Replace("ESPACIO", "ESPAÇO").Replace("salto", "salto")
						.Replace("ataque", "ataque")
						.Replace("hechizo", "feitiço")
						.Replace("rápido", "mapa rápido")
						.Replace("onírico", "ferrão onírico"), key, sheet, value);
				}
				switch (key)
				{
				case "common.close":
					return UiLocalization.After("Fechar", key, sheet, value);
				case "tab.players":
					return UiLocalization.After("Jogadores e controlos", key, sheet, value);
				case "tab.game":
					return UiLocalization.After("Jogo", key, sheet, value);
				case "tab.settings":
					return UiLocalization.After("Definições", key, sheet, value);
				case "action.join":
					return UiLocalization.After("Entrar", key, sheet, value);
				case "action.gather":
					return UiLocalization.After("Reunir", key, sheet, value);
				case "action.repair":
					return UiLocalization.After("Recuperar jogadores", key, sheet, value);
				case "action.remove_last":
					return UiLocalization.After("Remover último", key, sheet, value);
				case "players.title":
					return UiLocalization.After("JOGADORES", key, sheet, value);
				case "keyboard.section":
					return UiLocalization.After("JOGADORES NO TECLADO", key, sheet, value);
				case "keyboard.add":
					return UiLocalization.After("Adicionar jogador no teclado", key, sheet, value);
				case "keyboard.new_title":
					return UiLocalization.After("NOVO JOGADOR: TECLADO", key, sheet, value);
				case "keyboard.new_intro":
					return UiLocalization.After("Configura os controlos do novo jogador.", key, sheet, value);
				case "keyboard.limit":
					return UiLocalization.After("Máximo: 4 jogadores no teclado.", key, sheet, value);
				case "common.cancel":
					return UiLocalization.After("Cancelar", key, sheet, value);
				case "common.back":
					return UiLocalization.After("Voltar", key, sheet, value);
				case "common.reset":
					return UiLocalization.After("Repor", key, sheet, value);
				case "difficulty.title":
					return UiLocalization.After("DIFICULDADE", key, sheet, value);
				case "difficulty.easy":
					return UiLocalization.After("Fácil · 45%", key, sheet, value);
				case "difficulty.normal":
					return UiLocalization.After("Normal · 65%", key, sheet, value);
				case "difficulty.hard":
					return UiLocalization.After("Difícil · 85%", key, sheet, value);
				case "difficulty.extreme":
					return UiLocalization.After("Extrema · 100%", key, sheet, value);
				case "camera.title":
					return UiLocalization.After("CÂMARA", key, sheet, value);
				case "camera.shared":
					return UiLocalization.After("Câmara partilhada: ", key, sheet, value);
				case "wait.label":
					return UiLocalization.After("Esperar pelo grupo: ", key, sheet, value);
				case "revive.title":
					return UiLocalization.After("REANIMAÇÃO E EFEITOS", key, sheet, value);
				case "respawn.label":
					return UiLocalization.After("Respawn automático: ", key, sheet, value);
				case "effects.label":
					return UiLocalization.After("Efeitos reduzidos: ", key, sheet, value);
				case "shade.label":
					return UiLocalization.After("Sombra ao morrer: ", key, sheet, value);
				case "common.no":
					return UiLocalization.After("NÃO", key, sheet, value);
				case "common.yes":
					return UiLocalization.After("SIM", key, sheet, value);
				case "advanced.menu":
					return UiLocalization.After("MENU", key, sheet, value);
				case "advanced.open":
					return UiLocalization.After("Abrir menu: ", key, sheet, value);
				case "rescue.title":
					return UiLocalization.After("RESGATE ONÍRICO", key, sheet, value);
				case "duel.title":
					return UiLocalization.After("DUELOS", key, sheet, value);
				case "virtual.title":
					return UiLocalization.After("COMANDOS VIRTUAIS", key, sheet, value);
				case "diagnostic.title":
					return UiLocalization.After("DIAGNÓSTICO", key, sheet, value);
				case "pvp.title":
					return UiLocalization.After("COMBATE ENTRE JOGADORES", key, sheet, value);
				case "pvp.coop":
					return UiLocalization.After("Cooperativo", key, sheet, value);
				case "pvp.friendly":
					return UiLocalization.After("Fogo amigo", key, sheet, value);
				case "pvp.rounds":
					return UiLocalization.After("Duelos por rondas", key, sheet, value);
				case "hud.recover_in":
					return UiLocalization.After("REGRESSO EM ", key, sheet, value);
				case "hud.waiting":
					return UiLocalization.After("À ESPERA", key, sheet, value);
				case "hud.waiting_bar":
					return UiLocalization.After("À ESPERA  ", key, sheet, value);
				case "hud.down":
					return UiLocalization.After("CAÍDO", key, sheet, value);
				case "hud.eliminated":
					return UiLocalization.After("ELIMINADO", key, sheet, value);
				case "hud.no_device":
					return UiLocalization.After("SEM DISPOSITIVO", key, sheet, value);
				case "hud.returning":
					return UiLocalization.After("A VOLTAR", key, sheet, value);
				case "rescue.hud_hold":
					return UiLocalization.After("MANTÉM ", key, sheet, value);
				case "rescue.hud_title":
					return UiLocalization.After("  -  RESGATE ONÍRICO", key, sheet, value);
				}
				break;
			case "RU":
				switch (key)
				{
				case "darkness.label":
					return UiLocalization.After("Полный обзор: ", key, sheet, value);
				case "darkness.tip":
					return UiLocalization.After("Вся комната или рядом с игроками.", key, sheet, value);
				case "camera.player_lights":
					return UiLocalization.After("Свет игроков: ", key, sheet, value);
				case "camera.player_lights.tip":
					return UiLocalization.After("P2–P8 скрыты; P1 как обычно.", key, sheet, value);
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return UiLocalization.After(value.Replace("PERFIL", "ПРОФИЛЬ").Replace("ESPACIO", "ПРОБЕЛ").Replace("salto", "прыжок")
						.Replace("ataque", "атака")
						.Replace("hechizo", "заклинание")
						.Replace("rápido", "быстрая карта")
						.Replace("onírico", "гвоздь грёз"), key, sheet, value);
				}
				switch (key)
				{
				case "common.close":
					return UiLocalization.After("Закрыть", key, sheet, value);
				case "tab.players":
					return UiLocalization.After("Игроки и управление", key, sheet, value);
				case "tab.game":
					return UiLocalization.After("Игра", key, sheet, value);
				case "tab.settings":
					return UiLocalization.After("Настройки", key, sheet, value);
				case "action.join":
					return UiLocalization.After("Войти", key, sheet, value);
				case "action.gather":
					return UiLocalization.After("Собрать", key, sheet, value);
				case "action.repair":
					return UiLocalization.After("Восстановить игроков", key, sheet, value);
				case "action.remove_last":
					return UiLocalization.After("Убрать последнего", key, sheet, value);
				case "players.title":
					return UiLocalization.After("ИГРОКИ", key, sheet, value);
				case "keyboard.section":
					return UiLocalization.After("ИГРОКИ НА КЛАВИАТУРЕ", key, sheet, value);
				case "keyboard.add":
					return UiLocalization.After("Добавить игрока с клавиатуры", key, sheet, value);
				case "keyboard.new_title":
					return UiLocalization.After("НОВЫЙ ИГРОК: КЛАВИАТУРА", key, sheet, value);
				case "keyboard.new_intro":
					return UiLocalization.After("Настройте управление нового игрока.", key, sheet, value);
				case "keyboard.limit":
					return UiLocalization.After("Максимум: 4 игрока на клавиатуре.", key, sheet, value);
				case "common.cancel":
					return UiLocalization.After("Отмена", key, sheet, value);
				case "common.back":
					return UiLocalization.After("Назад", key, sheet, value);
				case "common.reset":
					return UiLocalization.After("Сбросить", key, sheet, value);
				case "difficulty.title":
					return UiLocalization.After("СЛОЖНОСТЬ", key, sheet, value);
				case "difficulty.easy":
					return UiLocalization.After("Легко · 45%", key, sheet, value);
				case "difficulty.normal":
					return UiLocalization.After("Нормально · 65%", key, sheet, value);
				case "difficulty.hard":
					return UiLocalization.After("Сложно · 85%", key, sheet, value);
				case "difficulty.extreme":
					return UiLocalization.After("Экстрим · 100%", key, sheet, value);
				case "camera.title":
					return UiLocalization.After("КАМЕРА", key, sheet, value);
				case "camera.shared":
					return UiLocalization.After("Общая камера: ", key, sheet, value);
				case "wait.label":
					return UiLocalization.After("Ждать группу: ", key, sheet, value);
				case "revive.title":
					return UiLocalization.After("ВОЗРОЖДЕНИЕ И ЭФФЕКТЫ", key, sheet, value);
				case "respawn.label":
					return UiLocalization.After("Автовозрождение: ", key, sheet, value);
				case "effects.label":
					return UiLocalization.After("Меньше эффектов: ", key, sheet, value);
				case "shade.label":
					return UiLocalization.After("Тень после смерти: ", key, sheet, value);
				case "common.no":
					return UiLocalization.After("НЕТ", key, sheet, value);
				case "common.yes":
					return UiLocalization.After("ДА", key, sheet, value);
				case "advanced.menu":
					return UiLocalization.After("МЕНЮ", key, sheet, value);
				case "advanced.open":
					return UiLocalization.After("Открыть меню: ", key, sheet, value);
				case "rescue.title":
					return UiLocalization.After("СПАСЕНИЕ СНОВ", key, sheet, value);
				case "duel.title":
					return UiLocalization.After("ДУЭЛИ", key, sheet, value);
				case "virtual.title":
					return UiLocalization.After("ВИРТУАЛЬНЫЕ КОНТРОЛЛЕРЫ", key, sheet, value);
				case "diagnostic.title":
					return UiLocalization.After("ДИАГНОСТИКА", key, sheet, value);
				case "pvp.title":
					return UiLocalization.After("БОЙ МЕЖДУ ИГРОКАМИ", key, sheet, value);
				case "pvp.coop":
					return UiLocalization.After("Кооператив", key, sheet, value);
				case "pvp.friendly":
					return UiLocalization.After("Огонь по своим", key, sheet, value);
				case "pvp.rounds":
					return UiLocalization.After("Дуэли по раундам", key, sheet, value);
				case "hud.recover_in":
					return UiLocalization.After("ВОЗВРАТ ЧЕРЕЗ ", key, sheet, value);
				case "hud.waiting":
					return UiLocalization.After("ОЖИДАНИЕ", key, sheet, value);
				case "hud.waiting_bar":
					return UiLocalization.After("ОЖИДАНИЕ  ", key, sheet, value);
				case "hud.down":
					return UiLocalization.After("ПОВЕРЖЕН", key, sheet, value);
				case "hud.eliminated":
					return UiLocalization.After("ВЫБЫЛ", key, sheet, value);
				case "hud.no_device":
					return UiLocalization.After("НЕТ УСТРОЙСТВА", key, sheet, value);
				case "hud.returning":
					return UiLocalization.After("ВОЗВРАЩЕНИЕ", key, sheet, value);
				case "rescue.hud_hold":
					return UiLocalization.After("УДЕРЖИВАЙТЕ ", key, sheet, value);
				case "rescue.hud_title":
					return UiLocalization.After("  -  СПАСЕНИЕ СНОВ", key, sheet, value);
				}
				break;
			case "ZH":
				switch (key)
				{
				case "darkness.label":
					return UiLocalization.After("全图可见：", key, sheet, value);
				case "darkness.tip":
					return UiLocalization.After("全图或仅玩家附近。", key, sheet, value);
				case "camera.player_lights":
					return UiLocalization.After("玩家光环：", key, sheet, value);
				case "camera.player_lights.tip":
					return UiLocalization.After("隐藏P2-P8；P1光照不变。", key, sheet, value);
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return UiLocalization.After(value.Replace("PERFIL", "配置").Replace("ESPACIO", "空格").Replace("salto", "跳跃")
						.Replace("ataque", "攻击")
						.Replace("hechizo", "法术")
						.Replace("rápido", "快速地图")
						.Replace("onírico", "梦之钉"), key, sheet, value);
				}
				switch (key)
				{
				case "common.close":
					return UiLocalization.After("关闭", key, sheet, value);
				case "tab.players":
					return UiLocalization.After("玩家与控制", key, sheet, value);
				case "tab.game":
					return UiLocalization.After("游戏", key, sheet, value);
				case "tab.settings":
					return UiLocalization.After("设置", key, sheet, value);
				case "action.join":
					return UiLocalization.After("加入", key, sheet, value);
				case "action.gather":
					return UiLocalization.After("集合", key, sheet, value);
				case "action.repair":
					return UiLocalization.After("恢复玩家", key, sheet, value);
				case "action.remove_last":
					return UiLocalization.After("移除最后玩家", key, sheet, value);
				case "players.title":
					return UiLocalization.After("玩家", key, sheet, value);
				case "keyboard.section":
					return UiLocalization.After("键盘玩家", key, sheet, value);
				case "keyboard.add":
					return UiLocalization.After("添加键盘玩家", key, sheet, value);
				case "keyboard.new_title":
					return UiLocalization.After("新键盘玩家", key, sheet, value);
				case "keyboard.new_intro":
					return UiLocalization.After("设置新玩家的按键。", key, sheet, value);
				case "keyboard.limit":
					return UiLocalization.After("最多 4 名键盘玩家。", key, sheet, value);
				case "common.cancel":
					return UiLocalization.After("取消", key, sheet, value);
				case "common.back":
					return UiLocalization.After("返回", key, sheet, value);
				case "common.reset":
					return UiLocalization.After("重置", key, sheet, value);
				case "difficulty.title":
					return UiLocalization.After("难度", key, sheet, value);
				case "difficulty.easy":
					return UiLocalization.After("简单 · 45%", key, sheet, value);
				case "difficulty.normal":
					return UiLocalization.After("普通 · 65%", key, sheet, value);
				case "difficulty.hard":
					return UiLocalization.After("困难 · 85%", key, sheet, value);
				case "difficulty.extreme":
					return UiLocalization.After("极限 · 100%", key, sheet, value);
				case "difficulty.tip":
					return UiLocalization.After("每增加一名存活玩家的缩放：简单 45%，普通 65%，困难 85%，极限 100%。", key, sheet, value);
				case "camera.title":
					return UiLocalization.After("镜头", key, sheet, value);
				case "camera.shared":
					return UiLocalization.After("共享镜头：", key, sheet, value);
				case "wait.label":
					return UiLocalization.After("等待队友：", key, sheet, value);
				case "revive.title":
					return UiLocalization.After("复活与特效", key, sheet, value);
				case "respawn.label":
					return UiLocalization.After("自动复活：", key, sheet, value);
				case "effects.label":
					return UiLocalization.After("减少特效：", key, sheet, value);
				case "shade.label":
					return UiLocalization.After("死亡生成暗影：", key, sheet, value);
				case "common.no":
					return UiLocalization.After("否", key, sheet, value);
				case "common.yes":
					return UiLocalization.After("是", key, sheet, value);
				case "advanced.menu":
					return UiLocalization.After("菜单", key, sheet, value);
				case "advanced.open":
					return UiLocalization.After("打开菜单：", key, sheet, value);
				case "rescue.title":
					return UiLocalization.After("梦境救援", key, sheet, value);
				case "duel.title":
					return UiLocalization.After("对决", key, sheet, value);
				case "virtual.title":
					return UiLocalization.After("虚拟手柄", key, sheet, value);
				case "diagnostic.title":
					return UiLocalization.After("诊断", key, sheet, value);
				case "pvp.title":
					return UiLocalization.After("玩家对战", key, sheet, value);
				case "pvp.coop":
					return UiLocalization.After("合作", key, sheet, value);
				case "pvp.friendly":
					return UiLocalization.After("友军伤害", key, sheet, value);
				case "pvp.rounds":
					return UiLocalization.After("回合对决", key, sheet, value);
				case "hud.recover_in":
					return UiLocalization.After("复活倒计时 ", key, sheet, value);
				case "hud.waiting":
					return UiLocalization.After("等待中", key, sheet, value);
				case "hud.waiting_bar":
					return UiLocalization.After("等待中  ", key, sheet, value);
				case "hud.down":
					return UiLocalization.After("倒地", key, sheet, value);
				case "hud.eliminated":
					return UiLocalization.After("已淘汰", key, sheet, value);
				case "hud.no_device":
					return UiLocalization.After("无控制器", key, sheet, value);
				case "hud.returning":
					return UiLocalization.After("返回中", key, sheet, value);
				case "rescue.hud_hold":
					return UiLocalization.After("按住 ", key, sheet, value);
				case "rescue.hud_title":
					return UiLocalization.After("  -  梦境救援", key, sheet, value);
				}
				break;
			case "JA":
				switch (key)
				{
				case "darkness.label":
					return UiLocalization.After("全体表示：", key, sheet, value);
				case "darkness.tip":
					return UiLocalization.After("部屋全体／プレイヤー周辺のみ。", key, sheet, value);
				case "camera.player_lights":
					return UiLocalization.After("プレイヤーの光：", key, sheet, value);
				case "camera.player_lights.tip":
					return UiLocalization.After("P2～P8非表示。P1の光はそのままです。", key, sheet, value);
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return UiLocalization.After(value.Replace("PERFIL", "プロファイル").Replace("ESPACIO", "スペース").Replace("salto", "ジャンプ")
						.Replace("ataque", "攻撃")
						.Replace("hechizo", "呪文")
						.Replace("rápido", "クイックマップ")
						.Replace("onírico", "夢見の釘"), key, sheet, value);
				}
				switch (key)
				{
				case "common.close":
					return UiLocalization.After("閉じる", key, sheet, value);
				case "tab.players":
					return UiLocalization.After("プレイヤーと操作", key, sheet, value);
				case "tab.game":
					return UiLocalization.After("ゲーム", key, sheet, value);
				case "tab.settings":
					return UiLocalization.After("設定", key, sheet, value);
				case "action.join":
					return UiLocalization.After("参加", key, sheet, value);
				case "action.gather":
					return UiLocalization.After("集合", key, sheet, value);
				case "action.repair":
					return UiLocalization.After("プレイヤーを復旧", key, sheet, value);
				case "action.remove_last":
					return UiLocalization.After("最後を削除", key, sheet, value);
				case "players.title":
					return UiLocalization.After("プレイヤー", key, sheet, value);
				case "keyboard.section":
					return UiLocalization.After("キーボードプレイヤー", key, sheet, value);
				case "keyboard.add":
					return UiLocalization.After("キーボードプレイヤーを追加", key, sheet, value);
				case "keyboard.new_title":
					return UiLocalization.After("新しいキーボードプレイヤー", key, sheet, value);
				case "keyboard.new_intro":
					return UiLocalization.After("新しいプレイヤーの操作を設定します。", key, sheet, value);
				case "keyboard.limit":
					return UiLocalization.After("キーボードは最大4人です。", key, sheet, value);
				case "common.cancel":
					return UiLocalization.After("キャンセル", key, sheet, value);
				case "common.back":
					return UiLocalization.After("戻る", key, sheet, value);
				case "common.reset":
					return UiLocalization.After("リセット", key, sheet, value);
				case "difficulty.title":
					return UiLocalization.After("難易度", key, sheet, value);
				case "difficulty.easy":
					return UiLocalization.After("簡単 · 45%", key, sheet, value);
				case "difficulty.normal":
					return UiLocalization.After("普通 · 65%", key, sheet, value);
				case "difficulty.hard":
					return UiLocalization.After("難しい · 85%", key, sheet, value);
				case "difficulty.extreme":
					return UiLocalization.After("極限 · 100%", key, sheet, value);
				case "camera.title":
					return UiLocalization.After("カメラ", key, sheet, value);
				case "camera.shared":
					return UiLocalization.After("共有カメラ：", key, sheet, value);
				case "wait.label":
					return UiLocalization.After("グループを待つ：", key, sheet, value);
				case "revive.title":
					return UiLocalization.After("蘇生とエフェクト", key, sheet, value);
				case "respawn.label":
					return UiLocalization.After("自動復活：", key, sheet, value);
				case "effects.label":
					return UiLocalization.After("エフェクト軽減：", key, sheet, value);
				case "shade.label":
					return UiLocalization.After("死亡時の影：", key, sheet, value);
				case "common.no":
					return UiLocalization.After("いいえ", key, sheet, value);
				case "common.yes":
					return UiLocalization.After("はい", key, sheet, value);
				case "advanced.menu":
					return UiLocalization.After("メニュー", key, sheet, value);
				case "advanced.open":
					return UiLocalization.After("メニューを開く：", key, sheet, value);
				case "rescue.title":
					return UiLocalization.After("夢の救援", key, sheet, value);
				case "duel.title":
					return UiLocalization.After("デュエル", key, sheet, value);
				case "virtual.title":
					return UiLocalization.After("仮想コントローラー", key, sheet, value);
				case "diagnostic.title":
					return UiLocalization.After("診断", key, sheet, value);
				case "pvp.title":
					return UiLocalization.After("プレイヤー戦", key, sheet, value);
				case "pvp.coop":
					return UiLocalization.After("協力", key, sheet, value);
				case "pvp.friendly":
					return UiLocalization.After("フレンドリーファイア", key, sheet, value);
				case "pvp.rounds":
					return UiLocalization.After("ラウンド戦", key, sheet, value);
				case "hud.recover_in":
					return UiLocalization.After("復帰まで ", key, sheet, value);
				case "hud.waiting":
					return UiLocalization.After("待機中", key, sheet, value);
				case "hud.waiting_bar":
					return UiLocalization.After("待機中  ", key, sheet, value);
				case "hud.down":
					return UiLocalization.After("ダウン", key, sheet, value);
				case "hud.eliminated":
					return UiLocalization.After("脱落", key, sheet, value);
				case "hud.no_device":
					return UiLocalization.After("デバイスなし", key, sheet, value);
				case "hud.returning":
					return UiLocalization.After("復帰中", key, sheet, value);
				case "rescue.hud_hold":
					return UiLocalization.After("長押し ", key, sheet, value);
				case "rescue.hud_title":
					return UiLocalization.After("  -  夢の救援", key, sheet, value);
				}
				break;
			case "KO":
				switch (key)
				{
				case "darkness.label":
					return UiLocalization.After("전체 시야: ", key, sheet, value);
				case "darkness.tip":
					return UiLocalization.After("방 전체 또는 플레이어 주변만.", key, sheet, value);
				case "camera.player_lights":
					return UiLocalization.After("플레이어 조명: ", key, sheet, value);
				case "camera.player_lights.tip":
					return UiLocalization.After("P2-P8 숨김; P1 조명 유지.", key, sheet, value);
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return UiLocalization.After(value.Replace("PERFIL", "프로필").Replace("ESPACIO", "스페이스").Replace("salto", "점프")
						.Replace("ataque", "공격")
						.Replace("hechizo", "주문")
						.Replace("rápido", "빠른 지도")
						.Replace("onírico", "몽환의 대못"), key, sheet, value);
				}
				switch (key)
				{
				case "common.close":
					return UiLocalization.After("닫기", key, sheet, value);
				case "tab.players":
					return UiLocalization.After("플레이어 및 조작", key, sheet, value);
				case "tab.game":
					return UiLocalization.After("게임", key, sheet, value);
				case "tab.settings":
					return UiLocalization.After("설정", key, sheet, value);
				case "action.join":
					return UiLocalization.After("참가", key, sheet, value);
				case "action.gather":
					return UiLocalization.After("모으기", key, sheet, value);
				case "action.repair":
					return UiLocalization.After("플레이어 복구", key, sheet, value);
				case "action.remove_last":
					return UiLocalization.After("마지막 제거", key, sheet, value);
				case "players.title":
					return UiLocalization.After("플레이어", key, sheet, value);
				case "keyboard.section":
					return UiLocalization.After("키보드 플레이어", key, sheet, value);
				case "keyboard.add":
					return UiLocalization.After("키보드 플레이어 추가", key, sheet, value);
				case "keyboard.new_title":
					return UiLocalization.After("새 키보드 플레이어", key, sheet, value);
				case "keyboard.new_intro":
					return UiLocalization.After("새 플레이어의 키를 설정합니다.", key, sheet, value);
				case "keyboard.limit":
					return UiLocalization.After("키보드 플레이어는 최대 4명입니다.", key, sheet, value);
				case "common.cancel":
					return UiLocalization.After("취소", key, sheet, value);
				case "common.back":
					return UiLocalization.After("뒤로", key, sheet, value);
				case "common.reset":
					return UiLocalization.After("초기화", key, sheet, value);
				case "difficulty.title":
					return UiLocalization.After("난이도", key, sheet, value);
				case "difficulty.easy":
					return UiLocalization.After("쉬움 · 45%", key, sheet, value);
				case "difficulty.normal":
					return UiLocalization.After("보통 · 65%", key, sheet, value);
				case "difficulty.hard":
					return UiLocalization.After("어려움 · 85%", key, sheet, value);
				case "difficulty.extreme":
					return UiLocalization.After("극한 · 100%", key, sheet, value);
				case "camera.title":
					return UiLocalization.After("카메라", key, sheet, value);
				case "camera.shared":
					return UiLocalization.After("공유 카메라: ", key, sheet, value);
				case "wait.label":
					return UiLocalization.After("그룹 기다리기: ", key, sheet, value);
				case "revive.title":
					return UiLocalization.After("부활 및 효과", key, sheet, value);
				case "respawn.label":
					return UiLocalization.After("자동 부활: ", key, sheet, value);
				case "effects.label":
					return UiLocalization.After("효과 감소: ", key, sheet, value);
				case "shade.label":
					return UiLocalization.After("사망 시 그림자: ", key, sheet, value);
				case "common.no":
					return UiLocalization.After("아니요", key, sheet, value);
				case "common.yes":
					return UiLocalization.After("예", key, sheet, value);
				case "advanced.menu":
					return UiLocalization.After("메뉴", key, sheet, value);
				case "advanced.open":
					return UiLocalization.After("메뉴 열기: ", key, sheet, value);
				case "rescue.title":
					return UiLocalization.After("꿈 구조", key, sheet, value);
				case "duel.title":
					return UiLocalization.After("결투", key, sheet, value);
				case "virtual.title":
					return UiLocalization.After("가상 컨트롤러", key, sheet, value);
				case "diagnostic.title":
					return UiLocalization.After("진단", key, sheet, value);
				case "pvp.title":
					return UiLocalization.After("플레이어 전투", key, sheet, value);
				case "pvp.coop":
					return UiLocalization.After("협동", key, sheet, value);
				case "pvp.friendly":
					return UiLocalization.After("아군 공격", key, sheet, value);
				case "pvp.rounds":
					return UiLocalization.After("라운드 결투", key, sheet, value);
				case "hud.recover_in":
					return UiLocalization.After("복귀까지 ", key, sheet, value);
				case "hud.waiting":
					return UiLocalization.After("대기 중", key, sheet, value);
				case "hud.waiting_bar":
					return UiLocalization.After("대기 중  ", key, sheet, value);
				case "hud.down":
					return UiLocalization.After("쓰러짐", key, sheet, value);
				case "hud.eliminated":
					return UiLocalization.After("탈락", key, sheet, value);
				case "hud.no_device":
					return UiLocalization.After("장치 없음", key, sheet, value);
				case "hud.returning":
					return UiLocalization.After("복귀 중", key, sheet, value);
				case "rescue.hud_hold":
					return UiLocalization.After("길게 누르기 ", key, sheet, value);
				case "rescue.hud_title":
					return UiLocalization.After("  -  꿈 구조", key, sheet, value);
				}
				break;
			}
			switch (key)
			{
			case "darkness.label":
				return UiLocalization.After("Full view: ", key, sheet, value);
			case "darkness.tip":
				return UiLocalization.After("Whole room or only near players.", key, sheet, value);
			case "camera.player_lights":
				return UiLocalization.After("Player lights: ", key, sheet, value);
			case "camera.player_lights.tip":
				return UiLocalization.After("Hides P2-P8; P1 stays lit.", key, sheet, value);
			default:
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return UiLocalization.After(value.Replace("PERFIL", "PROFILE").Replace("ESPACIO", "SPACE").Replace("salto", "jump")
						.Replace("ataque", "attack")
						.Replace("hechizo", "spell")
						.Replace("rápido", "quick map")
						.Replace("onírico", "dream nail"), key, sheet, value);
				}
				return key switch
				{
					"common.close" => UiLocalization.After("Close", key, sheet, value), 
					"panel.toggle_hint" => UiLocalization.After(" opens and closes the panel", key, sheet, value), 
					"panel.enter_game" => UiLocalization.After("Enter a save to configure the group.", key, sheet, value), 
					"tab.players" => UiLocalization.After("Players & controls", key, sheet, value), 
					"tab.game" => UiLocalization.After("Game", key, sheet, value), 
					"tab.settings" => UiLocalization.After("Settings", key, sheet, value), 
					"action.join_f9" => UiLocalization.After("Join (F9)", key, sheet, value), 
					"action.join" => UiLocalization.After("Join", key, sheet, value), 
					"action.gather" => UiLocalization.After("Gather", key, sheet, value), 
					"rescue.hold" => UiLocalization.After("Hold ", key, sheet, value), 
					"rescue.panel_tip" => UiLocalization.After(") to travel as dream particles to another player. Cooldown: 25 s.", key, sheet, value), 
					"action.repair" => UiLocalization.After("Recover players", key, sheet, value), 
					"action.remove_last" => UiLocalization.After("Remove last", key, sheet, value), 
					"players.title" => UiLocalization.After("PLAYERS", key, sheet, value), 
					"players.disconnected" => UiLocalization.After(" (disconnected)", key, sheet, value), 
					"keyboard.section" => UiLocalization.After("KEYBOARD PLAYERS", key, sheet, value), 
					"keyboard.shared" => UiLocalization.After("Shared keyboard · max. 4 ", key, sheet, value), 
					"vjoy.detected_hint" => UiLocalization.After(" detected. You can scan again in Settings.", key, sheet, value), 
					"keyboard.add" => UiLocalization.After("Add keyboard player", key, sheet, value), 
					"appearance.player" => UiLocalization.After("CUSTOMIZE P", key, sheet, value), 
					"common.color" => UiLocalization.After("Color", key, sheet, value), 
					"common.skin" => UiLocalization.After("Skin", key, sheet, value), 
					"common.next" => UiLocalization.After("Next", key, sheet, value), 
					"charms.player" => UiLocalization.After("Charms P", key, sheet, value), 
					"labels.prefix" => UiLocalization.After("Labels: ", key, sheet, value), 
					"interface.prefix" => UiLocalization.After("HUD: ", key, sheet, value), 
					"hud.full" => UiLocalization.After("MASKS & SOUL", key, sheet, value), 
					"hud.basic" => UiLocalization.After("BASIC", key, sheet, value), 
					"keyboard.new_title" => UiLocalization.After("NEW KEYBOARD PLAYER", key, sheet, value), 
					"bind.help" => UiLocalization.After("Select an action, then press the key or button you want to use.", key, sheet, value), 
					"bind.capture" => UiLocalization.After("Press a key... Esc to cancel", key, sheet, value), 
					"rescue.title" => UiLocalization.After("DREAM RESCUE", key, sheet, value), 
					"rescue.tip" => UiLocalization.After("Hold the key to travel as dream particles toward a teammate. Cooldown: 25 s.", key, sheet, value), 
					"common.reset" => UiLocalization.After("Reset", key, sheet, value), 
					"common.back" => UiLocalization.After("Back", key, sheet, value), 
					"keyboard.new_intro" => UiLocalization.After("Configure the new player controls.", key, sheet, value), 
					"keyboard.limit" => UiLocalization.After("Maximum: 4 keyboard players.", key, sheet, value), 
					"keyboard.p1" => UiLocalization.After("P1 keeps the controls configured in Hollow Knight.", key, sheet, value), 
					"keyboard.controllers" => UiLocalization.After("Controllers keep their usual controls.", key, sheet, value), 
					"keyboard.mouse" => UiLocalization.After("The mouse remains reserved for P1.", key, sheet, value), 
					"keyboard.add_now" => UiLocalization.After("Add player", key, sheet, value), 
					"keyboard.wait" => UiLocalization.After("Wait for the loading screen or cutscene to finish.", key, sheet, value), 
					"group.full" => UiLocalization.After("The group already has 8 players.", key, sheet, value), 
					"common.cancel" => UiLocalization.After("Cancel", key, sheet, value), 
					"difficulty.title" => UiLocalization.After("DIFFICULTY", key, sheet, value), 
					"difficulty.easy" => UiLocalization.After("Easy · 45%", key, sheet, value), 
					"difficulty.normal" => UiLocalization.After("Normal · 65%", key, sheet, value), 
					"difficulty.hard" => UiLocalization.After("Hard · 85%", key, sheet, value), 
					"difficulty.extreme" => UiLocalization.After("Extreme · 100%", key, sheet, value), 
					"difficulty.tip" => UiLocalization.After("Scaling per additional living player: Easy 45%, Normal 65%, Hard 85%, Extreme 100%.", key, sheet, value), 
					"camera.title" => UiLocalization.After("CAMERA", key, sheet, value), 
					"camera.shared" => UiLocalization.After("Shared camera: ", key, sheet, value), 
					"camera.tip" => UiLocalization.After("Adjusts zoom to keep every player on screen.", key, sheet, value), 
					"camera.margin" => UiLocalization.After("Side margin: ", key, sheet, value), 
					"darkness.label" => UiLocalization.After("Full view: ", key, sheet, value), 
					"state.off_fem" => UiLocalization.After("OFF", key, sheet, value), 
					"darkness.soft" => UiLocalization.After("SOFT", key, sheet, value), 
					"darkness.tip" => UiLocalization.After("Whole room or only near players.", key, sheet, value), 
					"wait.label" => UiLocalization.After("Wait for group: ", key, sheet, value), 
					"wait.tip" => UiLocalization.After("When leaving a room, waits a few seconds so the group can follow.", key, sheet, value), 
					"revive.title" => UiLocalization.After("REVIVE & EFFECTS", key, sheet, value), 
					"respawn.label" => UiLocalization.After("Auto respawn: ", key, sheet, value), 
					"respawn.tip" => UiLocalization.After("If someone is still alive, downed players respawn after the configured time.", key, sheet, value), 
					"effects.label" => UiLocalization.After("Reduced effects: ", key, sheet, value), 
					"effects.tip" => UiLocalization.After("Reduces flashes and hit pauses when several players attack.", key, sheet, value), 
					"shade.label" => UiLocalization.After("Shade on death: ", key, sheet, value), 
					"shade.tip" => UiLocalization.After("Creates a Shade for each player when they die.", key, sheet, value), 
					"common.no" => UiLocalization.After("NO", key, sheet, value), 
					"common.yes" => UiLocalization.After("YES", key, sheet, value), 
					"advanced.menu" => UiLocalization.After("MENU", key, sheet, value), 
					"advanced.open" => UiLocalization.After("Open menu: ", key, sheet, value), 
					"bind.capture_any" => UiLocalization.After("Press a key or button... Esc to cancel", key, sheet, value), 
					"advanced.reset_tip" => UiLocalization.After("Restores F8 as the panel key.", key, sheet, value), 
					"common.keyboard" => UiLocalization.After("Keyboard: ", key, sheet, value), 
					"common.controller" => UiLocalization.After("Controller: ", key, sheet, value), 
					"rescue.controller_tip" => UiLocalization.After("Dream Rescue button for controller players.", key, sheet, value), 
					"duel.title" => UiLocalization.After("DUELS", key, sheet, value), 
					"duel.toggle" => UiLocalization.After("Start / stop PvP: ", key, sheet, value), 
					"duel.tip" => UiLocalization.After("Starts or stops duel rounds.", key, sheet, value), 
					"virtual.title" => UiLocalization.After("VIRTUAL CONTROLLERS", key, sheet, value), 
					"vjoy.label" => UiLocalization.After("vJoy / DirectInput: ", key, sheet, value), 
					"vjoy.detected" => UiLocalization.After(" detected", key, sheet, value), 
					"vjoy.inactive" => UiLocalization.After(" (reader inactive)", key, sheet, value), 
					"vjoy.search" => UiLocalization.After("Scan vJoy", key, sheet, value), 
					"diagnostic.title" => UiLocalization.After("DIAGNOSTICS", key, sheet, value), 
					"hud.loading" => UiLocalization.After("Loading HUD graphics...", key, sheet, value), 
					"pvp.title" => UiLocalization.After("PLAYER COMBAT", key, sheet, value), 
					"pvp.coop" => UiLocalization.After("Co-op", key, sheet, value), 
					"pvp.friendly" => UiLocalization.After("Friendly fire", key, sheet, value), 
					"pvp.rounds" => UiLocalization.After("Round duels", key, sheet, value), 
					"pvp.rounds_tip" => UiLocalization.After("Round-based fights with score and respawns.", key, sheet, value), 
					"pvp.friendly_tip" => UiLocalization.After("Co-op adventure with damage between players.", key, sheet, value), 
					"pvp.coop_tip" => UiLocalization.After("Co-op adventure with no damage between players.", key, sheet, value), 
					"pvp.damage" => UiLocalization.After("DAMAGE PER HIT", key, sheet, value), 
					"pvp.nail" => UiLocalization.After("Nail", key, sheet, value), 
					"pvp.spells" => UiLocalization.After("Spells & beams", key, sheet, value), 
					"pvp.nailarts" => UiLocalization.After("Nail Arts", key, sheet, value), 
					"pvp.charms" => UiLocalization.After("Charms / pets / Sharp Shadow", key, sheet, value), 
					"pvp.parry" => UiLocalization.After("Parry: ", key, sheet, value), 
					"pvp.parry_tip" => UiLocalization.After("A clash cancels both attacks and grants brief invulnerability.", key, sheet, value), 
					"pvp.charm_attacks" => UiLocalization.After("Charm attacks: ", key, sheet, value), 
					"pvp.charm_attacks_tip" => UiLocalization.After("Allows damage from pets, charms and Sharp Shadow.", key, sheet, value), 
					"pvp.soul_hit" => UiLocalization.After("Soul on hit: ", key, sheet, value), 
					"pvp.soul_hit_tip" => UiLocalization.After("Nail hits against another player restore Soul.", key, sheet, value), 
					"pvp.teams" => UiLocalization.After("TEAMS & SCORE", key, sheet, value), 
					"pvp.team" => UiLocalization.After("Team ", key, sheet, value), 
					"pvp.free" => UiLocalization.After("Free-for-all", key, sheet, value), 
					"pvp.free_tip" => UiLocalization.After("Free-for-all. Players on the same team cannot hurt each other.", key, sheet, value), 
					"pvp.kills" => UiLocalization.After("   Kills ", key, sheet, value), 
					"pvp.deaths" => UiLocalization.After("   Deaths ", key, sheet, value), 
					"pvp.parries" => UiLocalization.After("   Parries ", key, sheet, value), 
					"pvp.round_settings" => UiLocalization.After("ROUNDS", key, sheet, value), 
					"pvp.start_soul" => UiLocalization.After("Starting Soul", key, sheet, value), 
					"pvp.round_time" => UiLocalization.After("Round time (seconds)", key, sheet, value), 
					"pvp.bestof" => UiLocalization.After("Best of: ", key, sheet, value), 
					"pvp.unlimited" => UiLocalization.After("unlimited", key, sheet, value), 
					"pvp.duel_key" => UiLocalization.After("Duel key: ", key, sheet, value), 
					"bind.capture_short" => UiLocalization.After("Press a key...", key, sheet, value), 
					"pvp.duel_key_tip" => UiLocalization.After("Changes the key used to start or stop PvP.", key, sheet, value), 
					"pvp.start_rounds" => UiLocalization.After("Start rounds", key, sheet, value), 
					"pvp.end_duel" => UiLocalization.After("End duel", key, sheet, value), 
					"pvp.start_tip" => UiLocalization.After("Starts from the current positions after a 3-second countdown.", key, sheet, value), 
					"pvp.ended" => UiLocalization.After("Duel ended.", key, sheet, value), 
					"pvp.clear" => UiLocalization.After("Clear score", key, sheet, value), 
					"pvp.clear_tip" => UiLocalization.After("Resets round wins, kills and parries.", key, sheet, value), 
					"hud.recover_in" => UiLocalization.After("RECOVER IN ", key, sheet, value), 
					"hud.waiting" => UiLocalization.After("WAITING", key, sheet, value), 
					"hud.waiting_bar" => UiLocalization.After("WAITING  ", key, sheet, value), 
					"hud.down" => UiLocalization.After("DOWN", key, sheet, value), 
					"hud.eliminated" => UiLocalization.After("ELIMINATED", key, sheet, value), 
					"hud.no_device" => UiLocalization.After("NO DEVICE", key, sheet, value), 
					"hud.returning" => UiLocalization.After("RETURNING", key, sheet, value), 
					"rescue.hud_hold" => UiLocalization.After("HOLD ", key, sheet, value), 
					"rescue.hud_title" => UiLocalization.After("  -  DREAM RESCUE", key, sheet, value), 
					_ => UiLocalization.After(value, key, sheet, value), 
				};
			}
		}
		if (sheet == "UI" && key != null && (key.StartsWith("CHARM_NAME_") || key.StartsWith("CHARM_DESC_")) && value != null)
		{
			return UiLocalization.After(value.Replace('ñ', 'n').Replace('Ñ', 'N'), key, sheet, value);
		}
		return UiLocalization.After(value, key, sheet, value);
	}

	internal static Texture2D Icon(int id)
	{
		try
		{
			if (icons.TryGetValue(id, out var value) && (bool)value)
			{
				return value;
			}
			CharmIconList instance = CharmIconList.Instance;
			if (!instance)
			{
				return null;
			}
			Sprite sprite = instance.GetSprite(id);
			if (!sprite)
			{
				return null;
			}
			value = HudAssets.FromSprite(sprite);
			icons[id] = value;
			return value;
		}
		catch (Exception ex)
		{
			Diagnostics.Throttled("CHARM ICON", ex);
			return null;
		}
	}

	internal static void Reset()
	{
		Editing = false;
		ReleaseNative();
		foreach (Texture2D value in icons.Values)
		{
			if ((bool)value)
			{
				UnityEngine.Object.Destroy(value);
			}
		}
		icons.Clear();
	}
}
