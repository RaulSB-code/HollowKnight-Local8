using System;
using System.Collections.Generic;
using System.Linq;
using InControl;
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

	private static readonly Rigidbody2D[] frozen = (Rigidbody2D[])(object)new Rigidbody2D[8];

	private static readonly bool[] wasKinematic = new bool[8];

	private static GameObject nativePane;

	private static InvCharmBackboard[] nativeBoards = (InvCharmBackboard[])(object)new InvCharmBackboard[0];

	private static int nativeFrame = -1;

	private static bool nativeVisible;

	private static bool wasNative;

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
			return nativeVisible;
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
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		GameManager instance = GameManager.instance;
		if (coopSession == null || !coopSession.Active || coopSession.Players.Count < 2 || !Object.op_Implicit((Object)(object)instance) || !instance.IsGameplayScene() || instance.IsLoadingSceneTransition || Plugin.Self.Panel)
		{
			return;
		}
		GameObject val = GameObject.FindWithTag("Charms Pane");
		if (!Object.op_Implicit((Object)(object)val) || !val.activeInHierarchy)
		{
			return;
		}
		GameCameras instance2 = GameCameras.instance;
		if (!Object.op_Implicit((Object)(object)instance2) || !Object.op_Implicit((Object)(object)instance2.hudCamera))
		{
			return;
		}
		Vector3 val2 = instance2.hudCamera.WorldToViewportPoint(val.transform.position);
		if (val2.z <= 0f || val2.x < 0.05f || val2.x > 0.95f || val2.y < 0.05f || val2.y > 0.95f)
		{
			return;
		}
		if ((Object)(object)nativePane != (Object)(object)val)
		{
			nativePane = val;
			nativeBoards = val.GetComponentsInChildren<InvCharmBackboard>(true);
			if (nativeBoards.Length == 0)
			{
				nativeBoards = Object.FindObjectsOfType<InvCharmBackboard>();
			}
		}
		nativeVisible = true;
	}

	private static InvCharmBackboard Board(int id)
	{
		InvCharmBackboard[] array = NativeBoards;
		foreach (InvCharmBackboard val in array)
		{
			if (Object.op_Implicit((Object)(object)val) && val.charmNum == id)
			{
				return val;
			}
		}
		return null;
	}

	private static int FirstOwned(PlayerData data)
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

	private static int Neighbor(int current, int dx, int dy, PlayerData data)
	{
		List<CharmCell> list = new List<CharmCell>(40);
		InvCharmBackboard[] array = NativeBoards;
		foreach (InvCharmBackboard val in array)
		{
			if (Object.op_Implicit((Object)(object)val) && val.charmNum >= 1 && val.charmNum <= 40)
			{
				Vector3 position = ((Component)val).transform.position;
				list.Add(new CharmCell(val.charmNum, position.x, position.y, data.GetBool("gotCharm_" + val.charmNum)));
			}
		}
		return CharmGridRules.Next(current, dx, dy, list);
	}

	private static void Freeze(CoopSession s)
	{
		foreach (PlayerSlot player in s.Players)
		{
			if (player.Index > 0 && player.Alive && player.Ready && Object.op_Implicit((Object)(object)player.Hero))
			{
				Rigidbody2D component = ((Component)player.Hero).GetComponent<Rigidbody2D>();
				if (Object.op_Implicit((Object)(object)component) && (Object)(object)frozen[player.Index] != (Object)(object)component)
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
		Rigidbody2D val = frozen[index];
		if (Object.op_Implicit((Object)(object)val))
		{
			val.isKinematic = wasKinematic[index];
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
		nativeBoards = (InvCharmBackboard[])(object)new InvCharmBackboard[0];
		nativeVisible = false;
		CharmNativeUi.Release();
	}

	private static void NativeInput(CoopSession s)
	{
		//IL_0242: Invalid comparison between Unknown and I4
		if (!wasNative)
		{
			wasNative = true;
			for (int i = 0; i < 8; i++)
			{
				if (i > 0 || NativeCursor[i] < 1 || NativeCursor[i] > 40)
				{
					NativeCursor[i] = FirstOwned(s.Data);
				}
				nextMove[i] = Time.unscaledTime + 0.2f;
				moveDirection[i] = 0;
				submitHeld[i] = false;
			}
		}
		Freeze(s);
		foreach (PlayerSlot player in s.Players)
		{
			if (player.Alive && player.Ready && player.Connected && player.Actions != null)
			{
				HeroActions actions = player.Actions;
				bool flag = player.Index == 0 && Controls.IsKeyboard(player.Device);
				int num = ((((OneAxisInputControl)actions.left).IsPressed || (flag && Input.GetKey((KeyCode)276))) ? (-1) : ((((OneAxisInputControl)actions.right).IsPressed || (flag && Input.GetKey((KeyCode)275))) ? 1 : ((((OneAxisInputControl)actions.up).IsPressed || (flag && Input.GetKey((KeyCode)273))) ? 2 : ((((OneAxisInputControl)actions.down).IsPressed || (flag && Input.GetKey((KeyCode)274))) ? 3 : 0))));
				if (num != 0 && (num != moveDirection[player.Index] || Time.unscaledTime >= nextMove[player.Index]))
				{
					_ = NativeCursor[player.Index];
					NativeCursor[player.Index] = Neighbor(NativeCursor[player.Index], (num == -1) ? (-1) : ((num == 1) ? 1 : 0), num switch
					{
						3 => -1, 
						2 => 1, 
						_ => 0, 
					}, s.Data);
					_ = NativeCursor[player.Index];
					nextMove[player.Index] = Time.unscaledTime + 0.16f;
				}
				moveDirection[player.Index] = num;
				bool flag2 = (int)Platform.Current.GetMenuAction(((OneAxisInputControl)actions.menuSubmit).IsPressed, ((OneAxisInputControl)actions.menuCancel).IsPressed, ((OneAxisInputControl)actions.jump).IsPressed, ((OneAxisInputControl)actions.attack).IsPressed, ((OneAxisInputControl)actions.cast).IsPressed) == 1;
				if (flag2 && !submitHeld[player.Index])
				{
					Diagnostics.Write("CHARMS submit P" + (player.Index + 1) + " id=" + NativeCursor[player.Index] + " equipped=" + (NativeCursor[player.Index] > 0 && NativeCursor[player.Index] <= 40 && player.Charms.Equipped[NativeCursor[player.Index] - 1]) + " edit=" + CanEdit(player));
					Toggle(player, NativeCursor[player.Index]);
				}
				submitHeld[player.Index] = flag2;
			}
		}
	}

	internal static void Load(PlayerSlot p, PlayerData data)
	{
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
		if (s == null)
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
		int num = data.maxHealthBase + ((p.Charms.Equipped[22] && !data.brokenCharm_23) ? 2 : 0);
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
			if (((OneAxisInputControl)actions.menuCancel).WasPressed || ((OneAxisInputControl)actions.openInventory).WasPressed)
			{
				Close();
				Plugin.Self.SetPanel(open: false);
				return;
			}
			if (((OneAxisInputControl)actions.left).WasPressed)
			{
				Cursor = (Cursor + 38) % 40 + 1;
			}
			if (((OneAxisInputControl)actions.right).WasPressed)
			{
				Cursor = Cursor % 40 + 1;
			}
			if (((OneAxisInputControl)actions.up).WasPressed)
			{
				Cursor = (Cursor + 29) % 40 + 1;
			}
			if (((OneAxisInputControl)actions.down).WasPressed)
			{
				Cursor = (Cursor + 9) % 40 + 1;
			}
			if (((OneAxisInputControl)actions.menuSubmit).WasPressed)
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
				if (player.Index > 0 && player.Actions != null && ((OneAxisInputControl)player.Actions.openInventory).WasPressed && player.Alive)
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
			return Language.Get("CHARM_NAME_" + id, "UI");
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
			return Language.Get("CHARM_DESC_" + id, "UI").Replace("<br>", "\n");
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
			sheet = ((object)Language.CurrentLanguage()).ToString();
			switch (sheet)
			{
			case "ES":
				return value;
			case "FR":
				switch (key)
				{
				case "darkness.label":
					return "Vue complète : ";
				case "darkness.tip":
					return "Salle entière ou autour des joueurs.";
				case "camera.player_lights":
					return "Lumières des joueurs : ";
				case "camera.player_lights.tip":
					return "Masque P2-P8; P1 reste éclairé.";
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return value.Replace("PERFIL", "PROFIL").Replace("ESPACIO", "ESPACE").Replace("salto", "saut")
						.Replace("ataque", "attaque")
						.Replace("hechizo", "sort")
						.Replace("rápido", "carte rapide")
						.Replace("onírico", "aiguillon onirique");
				}
				switch (key)
				{
				case "common.close":
					return "Fermer";
				case "tab.players":
					return "Joueurs et commandes";
				case "tab.game":
					return "Partie";
				case "tab.settings":
					return "Réglages";
				case "action.join":
					return "Rejoindre";
				case "action.gather":
					return "Rassembler";
				case "action.repair":
					return "Récupérer les joueurs";
				case "action.remove_last":
					return "Retirer le dernier";
				case "players.title":
					return "JOUEURS";
				case "keyboard.section":
					return "JOUEURS AU CLAVIER";
				case "keyboard.add":
					return "Ajouter un joueur au clavier";
				case "keyboard.new_title":
					return "NOUVEAU JOUEUR : CLAVIER";
				case "keyboard.new_intro":
					return "Configurez les commandes du nouveau joueur.";
				case "keyboard.limit":
					return "Maximum : 4 joueurs au clavier.";
				case "common.cancel":
					return "Annuler";
				case "common.back":
					return "Retour";
				case "common.reset":
					return "Réinitialiser";
				case "difficulty.title":
					return "DIFFICULTÉ";
				case "difficulty.easy":
					return "Facile · 45%";
				case "difficulty.normal":
					return "Normal · 65%";
				case "difficulty.hard":
					return "Difficile · 85%";
				case "difficulty.extreme":
					return "Extrême · 100%";
				case "difficulty.tip":
					return "Progression par joueur vivant supplémentaire : Facile 45 %, Normal 65 %, Difficile 85 %, Extrême 100 %.";
				case "camera.title":
					return "CAMÉRA";
				case "camera.shared":
					return "Caméra partagée : ";
				case "wait.label":
					return "Attendre le groupe : ";
				case "revive.title":
					return "RÉANIMATION ET EFFETS";
				case "respawn.label":
					return "Réapparition auto : ";
				case "effects.label":
					return "Effets réduits : ";
				case "shade.label":
					return "Ombre à la mort : ";
				case "common.no":
					return "NON";
				case "common.yes":
					return "OUI";
				case "advanced.menu":
					return "MENU";
				case "advanced.open":
					return "Ouvrir le menu : ";
				case "rescue.title":
					return "SAUVETAGE ONIRIQUE";
				case "duel.title":
					return "DUELS";
				case "virtual.title":
					return "MANETTES VIRTUELLES";
				case "diagnostic.title":
					return "DIAGNOSTIC";
				case "pvp.title":
					return "COMBAT ENTRE JOUEURS";
				case "pvp.coop":
					return "Coopération";
				case "pvp.friendly":
					return "Tir allié";
				case "pvp.rounds":
					return "Duels par manches";
				case "hud.recover_in":
					return "RÉAPPARITION DANS ";
				case "hud.waiting":
					return "ATTENTE";
				case "hud.waiting_bar":
					return "ATTENTE  ";
				case "hud.down":
					return "À TERRE";
				case "hud.eliminated":
					return "ÉLIMINÉ";
				case "hud.no_device":
					return "AUCUN PÉRIPHÉRIQUE";
				case "hud.returning":
					return "RETOUR";
				case "rescue.hud_hold":
					return "MAINTENIR ";
				case "rescue.hud_title":
					return "  -  SAUVETAGE ONIRIQUE";
				}
				break;
			case "DE":
				switch (key)
				{
				case "darkness.label":
					return "Volle Sicht: ";
				case "darkness.tip":
					return "Ganzer Raum oder nur um Spieler.";
				case "camera.player_lights":
					return "Spielerlichter: ";
				case "camera.player_lights.tip":
					return "P2-P8 aus; P1 bleibt beleuchtet.";
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return value.Replace("PERFIL", "PROFIL").Replace("ESPACIO", "LEERTASTE").Replace("salto", "Sprung")
						.Replace("ataque", "Angriff")
						.Replace("hechizo", "Zauber")
						.Replace("rápido", "Schnellkarte")
						.Replace("onírico", "Traumnagel");
				}
				switch (key)
				{
				case "common.close":
					return "Schließen";
				case "tab.players":
					return "Spieler & Steuerung";
				case "tab.game":
					return "Spiel";
				case "tab.settings":
					return "Einstellungen";
				case "action.join":
					return "Beitreten";
				case "action.gather":
					return "Sammeln";
				case "action.repair":
					return "Spieler wiederherstellen";
				case "action.remove_last":
					return "Letzten entfernen";
				case "players.title":
					return "SPIELER";
				case "keyboard.section":
					return "TASTATURSPIELER";
				case "keyboard.add":
					return "Tastaturspieler hinzufügen";
				case "keyboard.new_title":
					return "NEUER TASTATURSPIELER";
				case "keyboard.new_intro":
					return "Steuerung des neuen Spielers festlegen.";
				case "keyboard.limit":
					return "Maximal 4 Tastaturspieler.";
				case "common.cancel":
					return "Abbrechen";
				case "common.back":
					return "Zurück";
				case "common.reset":
					return "Zurücksetzen";
				case "difficulty.title":
					return "SCHWIERIGKEIT";
				case "difficulty.easy":
					return "Leicht · 45%";
				case "difficulty.normal":
					return "Normal · 65%";
				case "difficulty.hard":
					return "Schwer · 85%";
				case "difficulty.extreme":
					return "Extrem · 100%";
				case "difficulty.tip":
					return "Skalierung pro zusätzlichem lebenden Spieler: Leicht 45 %, Normal 65 %, Schwer 85 %, Extrem 100 %.";
				case "camera.title":
					return "KAMERA";
				case "camera.shared":
					return "Gemeinsame Kamera: ";
				case "wait.label":
					return "Auf Gruppe warten: ";
				case "revive.title":
					return "WIEDERBELEBUNG & EFFEKTE";
				case "respawn.label":
					return "Auto-Respawn: ";
				case "effects.label":
					return "Reduzierte Effekte: ";
				case "shade.label":
					return "Schatten beim Tod: ";
				case "common.no":
					return "NEIN";
				case "common.yes":
					return "JA";
				case "advanced.menu":
					return "MENÜ";
				case "advanced.open":
					return "Menü öffnen: ";
				case "rescue.title":
					return "TRAUMRETTUNG";
				case "duel.title":
					return "DUELLE";
				case "virtual.title":
					return "VIRTUELLE CONTROLLER";
				case "diagnostic.title":
					return "DIAGNOSE";
				case "pvp.title":
					return "SPIELERKAMPF";
				case "pvp.coop":
					return "Koop";
				case "pvp.friendly":
					return "Eigenbeschuss";
				case "pvp.rounds":
					return "Rundenduelle";
				case "hud.recover_in":
					return "RÜCKKEHR IN ";
				case "hud.waiting":
					return "WARTEN";
				case "hud.waiting_bar":
					return "WARTEN  ";
				case "hud.down":
					return "AM BODEN";
				case "hud.eliminated":
					return "AUSGESCHIEDEN";
				case "hud.no_device":
					return "KEIN GERÄT";
				case "hud.returning":
					return "RÜCKKEHR";
				case "rescue.hud_hold":
					return "HALTEN ";
				case "rescue.hud_title":
					return "  -  TRAUMRETTUNG";
				}
				break;
			case "IT":
				switch (key)
				{
				case "darkness.label":
					return "Vista completa: ";
				case "darkness.tip":
					return "Tutta la stanza o vicino ai giocatori.";
				case "camera.player_lights":
					return "Luci dei giocatori: ";
				case "camera.player_lights.tip":
					return "P2-P8 nascosti; P1 resta illuminato.";
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return value.Replace("PERFIL", "PROFILO").Replace("ESPACIO", "SPAZIO").Replace("salto", "salto")
						.Replace("ataque", "attacco")
						.Replace("hechizo", "incantesimo")
						.Replace("rápido", "mappa rapida")
						.Replace("onírico", "aculeo onirico");
				}
				switch (key)
				{
				case "common.close":
					return "Chiudi";
				case "tab.players":
					return "Giocatori e comandi";
				case "tab.game":
					return "Partita";
				case "tab.settings":
					return "Impostazioni";
				case "action.join":
					return "Unisciti";
				case "action.gather":
					return "Riunisci";
				case "action.repair":
					return "Recupera giocatori";
				case "action.remove_last":
					return "Rimuovi ultimo";
				case "players.title":
					return "GIOCATORI";
				case "keyboard.section":
					return "GIOCATORI DA TASTIERA";
				case "keyboard.add":
					return "Aggiungi giocatore da tastiera";
				case "keyboard.new_title":
					return "NUOVO GIOCATORE: TASTIERA";
				case "keyboard.new_intro":
					return "Configura i comandi del nuovo giocatore.";
				case "keyboard.limit":
					return "Massimo: 4 giocatori da tastiera.";
				case "common.cancel":
					return "Annulla";
				case "common.back":
					return "Indietro";
				case "common.reset":
					return "Ripristina";
				case "difficulty.title":
					return "DIFFICOLTÀ";
				case "difficulty.easy":
					return "Facile · 45%";
				case "difficulty.normal":
					return "Normale · 65%";
				case "difficulty.hard":
					return "Difficile · 85%";
				case "difficulty.extreme":
					return "Estrema · 100%";
				case "camera.title":
					return "TELECAMERA";
				case "camera.shared":
					return "Telecamera condivisa: ";
				case "wait.label":
					return "Attendi il gruppo: ";
				case "revive.title":
					return "RIANIMAZIONE ED EFFETTI";
				case "respawn.label":
					return "Respawn automatico: ";
				case "effects.label":
					return "Effetti ridotti: ";
				case "shade.label":
					return "Ombra alla morte: ";
				case "common.no":
					return "NO";
				case "common.yes":
					return "SÌ";
				case "advanced.menu":
					return "MENU";
				case "advanced.open":
					return "Apri menu: ";
				case "rescue.title":
					return "SALVATAGGIO ONIRICO";
				case "duel.title":
					return "DUELLI";
				case "virtual.title":
					return "CONTROLLER VIRTUALI";
				case "diagnostic.title":
					return "DIAGNOSTICA";
				case "pvp.title":
					return "COMBATTIMENTO TRA GIOCATORI";
				case "pvp.coop":
					return "Cooperativa";
				case "pvp.friendly":
					return "Fuoco amico";
				case "pvp.rounds":
					return "Duelli a round";
				case "hud.recover_in":
					return "RIENTRO TRA ";
				case "hud.waiting":
					return "IN ATTESA";
				case "hud.waiting_bar":
					return "IN ATTESA  ";
				case "hud.down":
					return "A TERRA";
				case "hud.eliminated":
					return "ELIMINATO";
				case "hud.no_device":
					return "NESSUN DISPOSITIVO";
				case "hud.returning":
					return "RITORNO";
				case "rescue.hud_hold":
					return "TIENI PREMUTO ";
				case "rescue.hud_title":
					return "  -  SALVATAGGIO ONIRICO";
				}
				break;
			case "PT":
				switch (key)
				{
				case "darkness.label":
					return "Visão completa: ";
				case "darkness.tip":
					return "Toda a sala ou só perto dos jogadores.";
				case "camera.player_lights":
					return "Luzes dos jogadores: ";
				case "camera.player_lights.tip":
					return "Oculta P2-P8; P1 mantém a luz.";
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return value.Replace("PERFIL", "PERFIL").Replace("ESPACIO", "ESPAÇO").Replace("salto", "salto")
						.Replace("ataque", "ataque")
						.Replace("hechizo", "feitiço")
						.Replace("rápido", "mapa rápido")
						.Replace("onírico", "ferrão onírico");
				}
				switch (key)
				{
				case "common.close":
					return "Fechar";
				case "tab.players":
					return "Jogadores e controlos";
				case "tab.game":
					return "Jogo";
				case "tab.settings":
					return "Definições";
				case "action.join":
					return "Entrar";
				case "action.gather":
					return "Reunir";
				case "action.repair":
					return "Recuperar jogadores";
				case "action.remove_last":
					return "Remover último";
				case "players.title":
					return "JOGADORES";
				case "keyboard.section":
					return "JOGADORES NO TECLADO";
				case "keyboard.add":
					return "Adicionar jogador no teclado";
				case "keyboard.new_title":
					return "NOVO JOGADOR: TECLADO";
				case "keyboard.new_intro":
					return "Configura os controlos do novo jogador.";
				case "keyboard.limit":
					return "Máximo: 4 jogadores no teclado.";
				case "common.cancel":
					return "Cancelar";
				case "common.back":
					return "Voltar";
				case "common.reset":
					return "Repor";
				case "difficulty.title":
					return "DIFICULDADE";
				case "difficulty.easy":
					return "Fácil · 45%";
				case "difficulty.normal":
					return "Normal · 65%";
				case "difficulty.hard":
					return "Difícil · 85%";
				case "difficulty.extreme":
					return "Extrema · 100%";
				case "camera.title":
					return "CÂMARA";
				case "camera.shared":
					return "Câmara partilhada: ";
				case "wait.label":
					return "Esperar pelo grupo: ";
				case "revive.title":
					return "REANIMAÇÃO E EFEITOS";
				case "respawn.label":
					return "Respawn automático: ";
				case "effects.label":
					return "Efeitos reduzidos: ";
				case "shade.label":
					return "Sombra ao morrer: ";
				case "common.no":
					return "NÃO";
				case "common.yes":
					return "SIM";
				case "advanced.menu":
					return "MENU";
				case "advanced.open":
					return "Abrir menu: ";
				case "rescue.title":
					return "RESGATE ONÍRICO";
				case "duel.title":
					return "DUELOS";
				case "virtual.title":
					return "COMANDOS VIRTUAIS";
				case "diagnostic.title":
					return "DIAGNÓSTICO";
				case "pvp.title":
					return "COMBATE ENTRE JOGADORES";
				case "pvp.coop":
					return "Cooperativo";
				case "pvp.friendly":
					return "Fogo amigo";
				case "pvp.rounds":
					return "Duelos por rondas";
				case "hud.recover_in":
					return "REGRESSO EM ";
				case "hud.waiting":
					return "À ESPERA";
				case "hud.waiting_bar":
					return "À ESPERA  ";
				case "hud.down":
					return "CAÍDO";
				case "hud.eliminated":
					return "ELIMINADO";
				case "hud.no_device":
					return "SEM DISPOSITIVO";
				case "hud.returning":
					return "A VOLTAR";
				case "rescue.hud_hold":
					return "MANTÉM ";
				case "rescue.hud_title":
					return "  -  RESGATE ONÍRICO";
				}
				break;
			case "RU":
				switch (key)
				{
				case "darkness.label":
					return "Полный обзор: ";
				case "darkness.tip":
					return "Вся комната или рядом с игроками.";
				case "camera.player_lights":
					return "Свет игроков: ";
				case "camera.player_lights.tip":
					return "P2–P8 скрыты; P1 как обычно.";
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return value.Replace("PERFIL", "ПРОФИЛЬ").Replace("ESPACIO", "ПРОБЕЛ").Replace("salto", "прыжок")
						.Replace("ataque", "атака")
						.Replace("hechizo", "заклинание")
						.Replace("rápido", "быстрая карта")
						.Replace("onírico", "гвоздь грёз");
				}
				switch (key)
				{
				case "common.close":
					return "Закрыть";
				case "tab.players":
					return "Игроки и управление";
				case "tab.game":
					return "Игра";
				case "tab.settings":
					return "Настройки";
				case "action.join":
					return "Войти";
				case "action.gather":
					return "Собрать";
				case "action.repair":
					return "Восстановить игроков";
				case "action.remove_last":
					return "Убрать последнего";
				case "players.title":
					return "ИГРОКИ";
				case "keyboard.section":
					return "ИГРОКИ НА КЛАВИАТУРЕ";
				case "keyboard.add":
					return "Добавить игрока с клавиатуры";
				case "keyboard.new_title":
					return "НОВЫЙ ИГРОК: КЛАВИАТУРА";
				case "keyboard.new_intro":
					return "Настройте управление нового игрока.";
				case "keyboard.limit":
					return "Максимум: 4 игрока на клавиатуре.";
				case "common.cancel":
					return "Отмена";
				case "common.back":
					return "Назад";
				case "common.reset":
					return "Сбросить";
				case "difficulty.title":
					return "СЛОЖНОСТЬ";
				case "difficulty.easy":
					return "Легко · 45%";
				case "difficulty.normal":
					return "Нормально · 65%";
				case "difficulty.hard":
					return "Сложно · 85%";
				case "difficulty.extreme":
					return "Экстрим · 100%";
				case "camera.title":
					return "КАМЕРА";
				case "camera.shared":
					return "Общая камера: ";
				case "wait.label":
					return "Ждать группу: ";
				case "revive.title":
					return "ВОЗРОЖДЕНИЕ И ЭФФЕКТЫ";
				case "respawn.label":
					return "Автовозрождение: ";
				case "effects.label":
					return "Меньше эффектов: ";
				case "shade.label":
					return "Тень после смерти: ";
				case "common.no":
					return "НЕТ";
				case "common.yes":
					return "ДА";
				case "advanced.menu":
					return "МЕНЮ";
				case "advanced.open":
					return "Открыть меню: ";
				case "rescue.title":
					return "СПАСЕНИЕ СНОВ";
				case "duel.title":
					return "ДУЭЛИ";
				case "virtual.title":
					return "ВИРТУАЛЬНЫЕ КОНТРОЛЛЕРЫ";
				case "diagnostic.title":
					return "ДИАГНОСТИКА";
				case "pvp.title":
					return "БОЙ МЕЖДУ ИГРОКАМИ";
				case "pvp.coop":
					return "Кооператив";
				case "pvp.friendly":
					return "Огонь по своим";
				case "pvp.rounds":
					return "Дуэли по раундам";
				case "hud.recover_in":
					return "ВОЗВРАТ ЧЕРЕЗ ";
				case "hud.waiting":
					return "ОЖИДАНИЕ";
				case "hud.waiting_bar":
					return "ОЖИДАНИЕ  ";
				case "hud.down":
					return "ПОВЕРЖЕН";
				case "hud.eliminated":
					return "ВЫБЫЛ";
				case "hud.no_device":
					return "НЕТ УСТРОЙСТВА";
				case "hud.returning":
					return "ВОЗВРАЩЕНИЕ";
				case "rescue.hud_hold":
					return "УДЕРЖИВАЙТЕ ";
				case "rescue.hud_title":
					return "  -  СПАСЕНИЕ СНОВ";
				}
				break;
			case "ZH":
				switch (key)
				{
				case "darkness.label":
					return "全图可见：";
				case "darkness.tip":
					return "全图或仅玩家附近。";
				case "camera.player_lights":
					return "玩家光环：";
				case "camera.player_lights.tip":
					return "隐藏P2-P8；P1光照不变。";
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return value.Replace("PERFIL", "配置").Replace("ESPACIO", "空格").Replace("salto", "跳跃")
						.Replace("ataque", "攻击")
						.Replace("hechizo", "法术")
						.Replace("rápido", "快速地图")
						.Replace("onírico", "梦之钉");
				}
				switch (key)
				{
				case "common.close":
					return "关闭";
				case "tab.players":
					return "玩家与控制";
				case "tab.game":
					return "游戏";
				case "tab.settings":
					return "设置";
				case "action.join":
					return "加入";
				case "action.gather":
					return "集合";
				case "action.repair":
					return "恢复玩家";
				case "action.remove_last":
					return "移除最后玩家";
				case "players.title":
					return "玩家";
				case "keyboard.section":
					return "键盘玩家";
				case "keyboard.add":
					return "添加键盘玩家";
				case "keyboard.new_title":
					return "新键盘玩家";
				case "keyboard.new_intro":
					return "设置新玩家的按键。";
				case "keyboard.limit":
					return "最多 4 名键盘玩家。";
				case "common.cancel":
					return "取消";
				case "common.back":
					return "返回";
				case "common.reset":
					return "重置";
				case "difficulty.title":
					return "难度";
				case "difficulty.easy":
					return "简单 · 45%";
				case "difficulty.normal":
					return "普通 · 65%";
				case "difficulty.hard":
					return "困难 · 85%";
				case "difficulty.extreme":
					return "极限 · 100%";
				case "difficulty.tip":
					return "每增加一名存活玩家的缩放：简单 45%，普通 65%，困难 85%，极限 100%。";
				case "camera.title":
					return "镜头";
				case "camera.shared":
					return "共享镜头：";
				case "wait.label":
					return "等待队友：";
				case "revive.title":
					return "复活与特效";
				case "respawn.label":
					return "自动复活：";
				case "effects.label":
					return "减少特效：";
				case "shade.label":
					return "死亡生成暗影：";
				case "common.no":
					return "否";
				case "common.yes":
					return "是";
				case "advanced.menu":
					return "菜单";
				case "advanced.open":
					return "打开菜单：";
				case "rescue.title":
					return "梦境救援";
				case "duel.title":
					return "对决";
				case "virtual.title":
					return "虚拟手柄";
				case "diagnostic.title":
					return "诊断";
				case "pvp.title":
					return "玩家对战";
				case "pvp.coop":
					return "合作";
				case "pvp.friendly":
					return "友军伤害";
				case "pvp.rounds":
					return "回合对决";
				case "hud.recover_in":
					return "复活倒计时 ";
				case "hud.waiting":
					return "等待中";
				case "hud.waiting_bar":
					return "等待中  ";
				case "hud.down":
					return "倒地";
				case "hud.eliminated":
					return "已淘汰";
				case "hud.no_device":
					return "无控制器";
				case "hud.returning":
					return "返回中";
				case "rescue.hud_hold":
					return "按住 ";
				case "rescue.hud_title":
					return "  -  梦境救援";
				}
				break;
			case "JA":
				switch (key)
				{
				case "darkness.label":
					return "全体表示：";
				case "darkness.tip":
					return "部屋全体／プレイヤー周辺のみ。";
				case "camera.player_lights":
					return "プレイヤーの光：";
				case "camera.player_lights.tip":
					return "P2～P8非表示。P1の光はそのままです。";
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return value.Replace("PERFIL", "プロファイル").Replace("ESPACIO", "スペース").Replace("salto", "ジャンプ")
						.Replace("ataque", "攻撃")
						.Replace("hechizo", "呪文")
						.Replace("rápido", "クイックマップ")
						.Replace("onírico", "夢見の釘");
				}
				switch (key)
				{
				case "common.close":
					return "閉じる";
				case "tab.players":
					return "プレイヤーと操作";
				case "tab.game":
					return "ゲーム";
				case "tab.settings":
					return "設定";
				case "action.join":
					return "参加";
				case "action.gather":
					return "集合";
				case "action.repair":
					return "プレイヤーを復旧";
				case "action.remove_last":
					return "最後を削除";
				case "players.title":
					return "プレイヤー";
				case "keyboard.section":
					return "キーボードプレイヤー";
				case "keyboard.add":
					return "キーボードプレイヤーを追加";
				case "keyboard.new_title":
					return "新しいキーボードプレイヤー";
				case "keyboard.new_intro":
					return "新しいプレイヤーの操作を設定します。";
				case "keyboard.limit":
					return "キーボードは最大4人です。";
				case "common.cancel":
					return "キャンセル";
				case "common.back":
					return "戻る";
				case "common.reset":
					return "リセット";
				case "difficulty.title":
					return "難易度";
				case "difficulty.easy":
					return "簡単 · 45%";
				case "difficulty.normal":
					return "普通 · 65%";
				case "difficulty.hard":
					return "難しい · 85%";
				case "difficulty.extreme":
					return "極限 · 100%";
				case "camera.title":
					return "カメラ";
				case "camera.shared":
					return "共有カメラ：";
				case "wait.label":
					return "グループを待つ：";
				case "revive.title":
					return "蘇生とエフェクト";
				case "respawn.label":
					return "自動復活：";
				case "effects.label":
					return "エフェクト軽減：";
				case "shade.label":
					return "死亡時の影：";
				case "common.no":
					return "いいえ";
				case "common.yes":
					return "はい";
				case "advanced.menu":
					return "メニュー";
				case "advanced.open":
					return "メニューを開く：";
				case "rescue.title":
					return "夢の救援";
				case "duel.title":
					return "デュエル";
				case "virtual.title":
					return "仮想コントローラー";
				case "diagnostic.title":
					return "診断";
				case "pvp.title":
					return "プレイヤー戦";
				case "pvp.coop":
					return "協力";
				case "pvp.friendly":
					return "フレンドリーファイア";
				case "pvp.rounds":
					return "ラウンド戦";
				case "hud.recover_in":
					return "復帰まで ";
				case "hud.waiting":
					return "待機中";
				case "hud.waiting_bar":
					return "待機中  ";
				case "hud.down":
					return "ダウン";
				case "hud.eliminated":
					return "脱落";
				case "hud.no_device":
					return "デバイスなし";
				case "hud.returning":
					return "復帰中";
				case "rescue.hud_hold":
					return "長押し ";
				case "rescue.hud_title":
					return "  -  夢の救援";
				}
				break;
			case "KO":
				switch (key)
				{
				case "darkness.label":
					return "전체 시야: ";
				case "darkness.tip":
					return "방 전체 또는 플레이어 주변만.";
				case "camera.player_lights":
					return "플레이어 조명: ";
				case "camera.player_lights.tip":
					return "P2-P8 숨김; P1 조명 유지.";
				}
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return value.Replace("PERFIL", "프로필").Replace("ESPACIO", "스페이스").Replace("salto", "점프")
						.Replace("ataque", "공격")
						.Replace("hechizo", "주문")
						.Replace("rápido", "빠른 지도")
						.Replace("onírico", "몽환의 대못");
				}
				switch (key)
				{
				case "common.close":
					return "닫기";
				case "tab.players":
					return "플레이어 및 조작";
				case "tab.game":
					return "게임";
				case "tab.settings":
					return "설정";
				case "action.join":
					return "참가";
				case "action.gather":
					return "모으기";
				case "action.repair":
					return "플레이어 복구";
				case "action.remove_last":
					return "마지막 제거";
				case "players.title":
					return "플레이어";
				case "keyboard.section":
					return "키보드 플레이어";
				case "keyboard.add":
					return "키보드 플레이어 추가";
				case "keyboard.new_title":
					return "새 키보드 플레이어";
				case "keyboard.new_intro":
					return "새 플레이어의 키를 설정합니다.";
				case "keyboard.limit":
					return "키보드 플레이어는 최대 4명입니다.";
				case "common.cancel":
					return "취소";
				case "common.back":
					return "뒤로";
				case "common.reset":
					return "초기화";
				case "difficulty.title":
					return "난이도";
				case "difficulty.easy":
					return "쉬움 · 45%";
				case "difficulty.normal":
					return "보통 · 65%";
				case "difficulty.hard":
					return "어려움 · 85%";
				case "difficulty.extreme":
					return "극한 · 100%";
				case "camera.title":
					return "카메라";
				case "camera.shared":
					return "공유 카메라: ";
				case "wait.label":
					return "그룹 기다리기: ";
				case "revive.title":
					return "부활 및 효과";
				case "respawn.label":
					return "자동 부활: ";
				case "effects.label":
					return "효과 감소: ";
				case "shade.label":
					return "사망 시 그림자: ";
				case "common.no":
					return "아니요";
				case "common.yes":
					return "예";
				case "advanced.menu":
					return "메뉴";
				case "advanced.open":
					return "메뉴 열기: ";
				case "rescue.title":
					return "꿈 구조";
				case "duel.title":
					return "결투";
				case "virtual.title":
					return "가상 컨트롤러";
				case "diagnostic.title":
					return "진단";
				case "pvp.title":
					return "플레이어 전투";
				case "pvp.coop":
					return "협동";
				case "pvp.friendly":
					return "아군 공격";
				case "pvp.rounds":
					return "라운드 결투";
				case "hud.recover_in":
					return "복귀까지 ";
				case "hud.waiting":
					return "대기 중";
				case "hud.waiting_bar":
					return "대기 중  ";
				case "hud.down":
					return "쓰러짐";
				case "hud.eliminated":
					return "탈락";
				case "hud.no_device":
					return "장치 없음";
				case "hud.returning":
					return "복귀 중";
				case "rescue.hud_hold":
					return "길게 누르기 ";
				case "rescue.hud_title":
					return "  -  꿈 구조";
				}
				break;
			}
			switch (key)
			{
			case "darkness.label":
				return "Full view: ";
			case "darkness.tip":
				return "Whole room or only near players.";
			case "camera.player_lights":
				return "Player lights: ";
			case "camera.player_lights.tip":
				return "Hides P2-P8; P1 stays lit.";
			default:
				if (key.Contains("PERFIL") || key.Contains("hechizo"))
				{
					return value.Replace("PERFIL", "PROFILE").Replace("ESPACIO", "SPACE").Replace("salto", "jump")
						.Replace("ataque", "attack")
						.Replace("hechizo", "spell")
						.Replace("rápido", "quick map")
						.Replace("onírico", "dream nail");
				}
				return key switch
				{
					"common.close" => "Close", 
					"panel.toggle_hint" => " opens and closes the panel", 
					"panel.enter_game" => "Enter a save to configure the group.", 
					"tab.players" => "Players & controls", 
					"tab.game" => "Game", 
					"tab.settings" => "Settings", 
					"action.join_f9" => "Join (F9)", 
					"action.join" => "Join", 
					"action.gather" => "Gather", 
					"rescue.hold" => "Hold ", 
					"rescue.panel_tip" => ") to travel as dream particles to another player. Cooldown: 25 s.", 
					"action.repair" => "Recover players", 
					"action.remove_last" => "Remove last", 
					"players.title" => "PLAYERS", 
					"players.disconnected" => " (disconnected)", 
					"keyboard.section" => "KEYBOARD PLAYERS", 
					"keyboard.shared" => "Shared keyboard · max. 4 ", 
					"vjoy.detected_hint" => " detected. You can scan again in Settings.", 
					"keyboard.add" => "Add keyboard player", 
					"appearance.player" => "CUSTOMIZE P", 
					"common.color" => "Color", 
					"common.skin" => "Skin", 
					"common.next" => "Next", 
					"charms.player" => "Charms P", 
					"labels.prefix" => "Labels: ", 
					"interface.prefix" => "HUD: ", 
					"hud.full" => "MASKS & SOUL", 
					"hud.basic" => "BASIC", 
					"keyboard.new_title" => "NEW KEYBOARD PLAYER", 
					"bind.help" => "Select an action, then press the key or button you want to use.", 
					"bind.capture" => "Press a key... Esc to cancel", 
					"rescue.title" => "DREAM RESCUE", 
					"rescue.tip" => "Hold the key to travel as dream particles toward a teammate. Cooldown: 25 s.", 
					"common.reset" => "Reset", 
					"common.back" => "Back", 
					"keyboard.new_intro" => "Configure the new player controls.", 
					"keyboard.limit" => "Maximum: 4 keyboard players.", 
					"keyboard.p1" => "P1 keeps the controls configured in Hollow Knight.", 
					"keyboard.controllers" => "Controllers keep their usual controls.", 
					"keyboard.mouse" => "The mouse remains reserved for P1.", 
					"keyboard.add_now" => "Add player", 
					"keyboard.wait" => "Wait for the loading screen or cutscene to finish.", 
					"group.full" => "The group already has 8 players.", 
					"common.cancel" => "Cancel", 
					"difficulty.title" => "DIFFICULTY", 
					"difficulty.easy" => "Easy · 45%", 
					"difficulty.normal" => "Normal · 65%", 
					"difficulty.hard" => "Hard · 85%", 
					"difficulty.extreme" => "Extreme · 100%", 
					"difficulty.tip" => "Scaling per additional living player: Easy 45%, Normal 65%, Hard 85%, Extreme 100%.", 
					"camera.title" => "CAMERA", 
					"camera.shared" => "Shared camera: ", 
					"camera.tip" => "Adjusts zoom to keep every player on screen.", 
					"camera.margin" => "Side margin: ", 
					"darkness.label" => "Full view: ", 
					"state.off_fem" => "OFF", 
					"darkness.soft" => "SOFT", 
					"darkness.tip" => "Whole room or only near players.", 
					"wait.label" => "Wait for group: ", 
					"wait.tip" => "When leaving a room, waits a few seconds so the group can follow.", 
					"revive.title" => "REVIVE & EFFECTS", 
					"respawn.label" => "Auto respawn: ", 
					"respawn.tip" => "If someone is still alive, downed players respawn after the configured time.", 
					"effects.label" => "Reduced effects: ", 
					"effects.tip" => "Reduces flashes and hit pauses when several players attack.", 
					"shade.label" => "Shade on death: ", 
					"shade.tip" => "Creates a Shade for each player when they die.", 
					"common.no" => "NO", 
					"common.yes" => "YES", 
					"advanced.menu" => "MENU", 
					"advanced.open" => "Open menu: ", 
					"bind.capture_any" => "Press a key or button... Esc to cancel", 
					"advanced.reset_tip" => "Restores F8 as the panel key.", 
					"common.keyboard" => "Keyboard: ", 
					"common.controller" => "Controller: ", 
					"rescue.controller_tip" => "Dream Rescue button for controller players.", 
					"duel.title" => "DUELS", 
					"duel.toggle" => "Start / stop PvP: ", 
					"duel.tip" => "Starts or stops duel rounds.", 
					"virtual.title" => "VIRTUAL CONTROLLERS", 
					"vjoy.label" => "vJoy / DirectInput: ", 
					"vjoy.detected" => " detected", 
					"vjoy.inactive" => " (reader inactive)", 
					"vjoy.search" => "Scan vJoy", 
					"diagnostic.title" => "DIAGNOSTICS", 
					"hud.loading" => "Loading HUD graphics...", 
					"pvp.title" => "PLAYER COMBAT", 
					"pvp.coop" => "Co-op", 
					"pvp.friendly" => "Friendly fire", 
					"pvp.rounds" => "Round duels", 
					"pvp.rounds_tip" => "Round-based fights with score and respawns.", 
					"pvp.friendly_tip" => "Co-op adventure with damage between players.", 
					"pvp.coop_tip" => "Co-op adventure with no damage between players.", 
					"pvp.damage" => "DAMAGE PER HIT", 
					"pvp.nail" => "Nail", 
					"pvp.spells" => "Spells & beams", 
					"pvp.nailarts" => "Nail Arts", 
					"pvp.charms" => "Charms / pets / Sharp Shadow", 
					"pvp.parry" => "Parry: ", 
					"pvp.parry_tip" => "A clash cancels both attacks and grants brief invulnerability.", 
					"pvp.charm_attacks" => "Charm attacks: ", 
					"pvp.charm_attacks_tip" => "Allows damage from pets, charms and Sharp Shadow.", 
					"pvp.soul_hit" => "Soul on hit: ", 
					"pvp.soul_hit_tip" => "Nail hits against another player restore Soul.", 
					"pvp.teams" => "TEAMS & SCORE", 
					"pvp.team" => "Team ", 
					"pvp.free" => "Free-for-all", 
					"pvp.free_tip" => "Free-for-all. Players on the same team cannot hurt each other.", 
					"pvp.kills" => "   Kills ", 
					"pvp.deaths" => "   Deaths ", 
					"pvp.parries" => "   Parries ", 
					"pvp.round_settings" => "ROUNDS", 
					"pvp.start_soul" => "Starting Soul", 
					"pvp.round_time" => "Round time (seconds)", 
					"pvp.bestof" => "Best of: ", 
					"pvp.unlimited" => "unlimited", 
					"pvp.duel_key" => "Duel key: ", 
					"bind.capture_short" => "Press a key...", 
					"pvp.duel_key_tip" => "Changes the key used to start or stop PvP.", 
					"pvp.start_rounds" => "Start rounds", 
					"pvp.end_duel" => "End duel", 
					"pvp.start_tip" => "Starts from the current positions after a 3-second countdown.", 
					"pvp.ended" => "Duel ended.", 
					"pvp.clear" => "Clear score", 
					"pvp.clear_tip" => "Resets round wins, kills and parries.", 
					"hud.recover_in" => "RECOVER IN ", 
					"hud.waiting" => "WAITING", 
					"hud.waiting_bar" => "WAITING  ", 
					"hud.down" => "DOWN", 
					"hud.eliminated" => "ELIMINATED", 
					"hud.no_device" => "NO DEVICE", 
					"hud.returning" => "RETURNING", 
					"rescue.hud_hold" => "HOLD ", 
					"rescue.hud_title" => "  -  DREAM RESCUE", 
					_ => value, 
				};
			}
		}
		if (sheet == "UI" && key != null && (key.StartsWith("CHARM_NAME_") || key.StartsWith("CHARM_DESC_")) && value != null)
		{
			return value.Replace('ñ', 'n').Replace('Ñ', 'N');
		}
		return value;
	}

	internal static Texture2D Icon(int id)
	{
		try
		{
			if (icons.TryGetValue(id, out var value) && Object.op_Implicit((Object)(object)value))
			{
				return value;
			}
			CharmIconList instance = CharmIconList.Instance;
			if (!Object.op_Implicit((Object)(object)instance))
			{
				return null;
			}
			Sprite sprite = instance.GetSprite(id);
			if (!Object.op_Implicit((Object)(object)sprite))
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
			if (Object.op_Implicit((Object)(object)value))
			{
				Object.Destroy((Object)(object)value);
			}
		}
		icons.Clear();
	}
}
