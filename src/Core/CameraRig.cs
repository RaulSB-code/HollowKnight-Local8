using System;
using System.Reflection;
using UnityEngine;

namespace KO.HollowKnight8;

internal sealed class CameraRig
{
	private static readonly FieldInfo ActiveLock = typeof(CameraController).GetField("currentLockArea", BindingFlags.Instance | BindingFlags.NonPublic);

	internal Camera cam;

	private tk2dCamera tkCam;

	private float baseTkZoom = 1f;

	private float nextLog;

	private float baseSize;

	private float baseFov;

	private float baseHalf;

	internal float currentHalf;

	internal float zoomVelocity;

	internal Vector3 position;

	internal Vector3 velocity;

	internal Vector3 nativePosition;

	private bool nativeSaved;

	internal bool initialized;

	private bool godhomeCaptured;

	private Vector2 godhomeCenter;

	private float godhomeHalf;

	internal void Prepare(CameraController ctrl)
	{
		Camera camera = ctrl.cam ?? Camera.main;
		if (!camera)
		{
			return;
		}
		if (initialized && cam == camera)
		{
			if (nativeSaved && (ctrl.mode == CameraController.CameraMode.FOLLOWING || ctrl.mode == CameraController.CameraMode.LOCKED))
			{
				ctrl.transform.position = nativePosition;
			}
			if ((bool)tkCam)
			{
				tkCam.ZoomFactor = baseTkZoom;
				tkCam.UpdateCameraMatrix();
			}
			else
			{
				cam.orthographicSize = baseSize;
				cam.fieldOfView = baseFov;
			}
			return;
		}
		Reset();
		cam = camera;
		tkCam = (GameCameras.instance ? GameCameras.instance.tk2dCam : null);
		baseTkZoom = (tkCam ? Mathf.Max(0.01f, tkCam.ZoomFactor) : 1f);
		baseSize = cam.orthographicSize;
		baseFov = cam.fieldOfView;
		baseHalf = (cam.orthographic ? baseSize : (Mathf.Abs(cam.transform.position.z) * Mathf.Tan(baseFov * ((float)Math.PI / 180f) * 0.5f)));
		if (baseHalf < 1f)
		{
			baseHalf = 8.3f;
		}
		currentHalf = baseHalf;
		position = ctrl.transform.position;
		initialized = true;
		Diagnostics.Write("CAMERA init tk2d=" + (tkCam != null) + " ortho=" + cam.orthographic + " size=" + baseSize + " fov=" + baseFov + " z=" + cam.transform.position.z + " half=" + baseHalf + " zoom=" + baseTkZoom);
	}

	internal void Apply(CameraController ctrl, CoopSession session)
	{
		//Discarded unreachable code: IL_0396, IL_0729
		if (!initialized || !cam)
		{
			CameraPresenceGuard.After(this, ctrl, session);
			return;
		}
		nativePosition = ctrl.transform.position;
		nativeSaved = true;
		if (!Plugin.Self.ZoomByPlayerCount.Value)
		{
			RestoreLens();
			position = ctrl.transform.position;
			velocity = Vector3.zero;
			CameraPresenceGuard.After(this, ctrl, session);
			return;
		}
		GroupBounds groupBounds = default(GroupBounds);
		foreach (PlayerSlot player in session.Players)
		{
			if (CameraPresenceGuard.Framing(player) && player.Ready)
			{
				Bounds bounds = DuelGround.Body(player);
				groupBounds.AddBox(bounds.min.x - 0.15f, bounds.max.x + 0.15f, bounds.min.y - 0.2f, bounds.max.y + 0.65f);
			}
		}
		int count = groupBounds.Count;
		float num = (float)groupBounds.MinX;
		float num2 = (float)groupBounds.MaxX;
		float num3 = (float)groupBounds.MinY;
		float num4 = (float)groupBounds.MaxY;
		if (count < 2 && session.Players.Count < 2)
		{
			RestoreLens();
			position = ctrl.transform.position;
			CameraPresenceGuard.After(this, ctrl, session);
			return;
		}
		if (count == 0)
		{
			CameraPresenceGuard.After(this, ctrl, session);
			return;
		}
		if (ctrl.mode == CameraController.CameraMode.FROZEN || ctrl.mode == CameraController.CameraMode.FADEOUT || ctrl.mode == CameraController.CameraMode.PANNING)
		{
			Log(ctrl, "mode=" + ctrl.mode, count, baseHalf, 0f, 0f);
			SetLens(currentHalf);
			position = ctrl.transform.position;
			CameraPresenceGuard.After(this, ctrl, session);
			return;
		}
		float num5 = 0.5f;
		float num6 = 0.25f;
		float num7 = ctrl.sceneWidth - 0.5f;
		float num8 = ctrl.sceneHeight - 0.25f;
		if (ctrl.sceneWidth <= 0f || ctrl.sceneHeight <= 0f)
		{
			Log(ctrl, "missing-bounds", count, baseHalf, num7 - num5, num8 - num6);
			RestoreLens();
			CameraPresenceGuard.After(this, ctrl, session);
			return;
		}
		if (ctrl.mode == CameraController.CameraMode.LOCKED)
		{
			num6 = Mathf.Max(num6, Mathf.Min(ctrl.yLockMin - baseHalf, num3 - 2f));
		}
		float num9 = Mathf.Min((num8 - num6) * 0.5f, (num7 - num5) * 0.5f / Mathf.Max(0.1f, cam.aspect));
		if (TryGodhomeFrame(ctrl, session, num5, num7, num6, num8, count, num, num2, num3, num4))
		{
			CameraPresenceGuard.After(this, ctrl, session);
			return;
		}
		if (num9 < baseHalf)
		{
			SetLens(currentHalf);
			position = Vector3.SmoothDamp(position, ctrl.transform.position, ref velocity, Plugin.Self.CameraSmooth.Value);
			position.z = nativePosition.z;
			ctrl.transform.position = position;
			CameraPresenceGuard.After(this, ctrl, session);
			return;
		}
		float value = Plugin.Self.ZoomLimit.Value;
		value = Mathf.Min(2f, value + (float)Mathf.Max(0, count - 1) * Plugin.Self.ZoomPerPlayer.Value);
		float num10 = (num2 - num + 2f * Mathf.Max(2.8f, Plugin.Self.SideMargin.Value)) / (2f * Mathf.Max(0.1f, cam.aspect));
		value = Mathf.Max(value, num10 / baseHalf);
		float a = (float)FrameMath.HalfHeight(num, num2, num3, num4, cam.aspect, baseHalf, value, num7 - num5, num8 - num6, Plugin.Self.SideMargin.Value);
		float num11 = num3 - 1.2f;
		float num12 = num4 + 3.2f;
		float b = Mathf.Min(num9, Mathf.Max((num12 - num11) * 0.5f, num10));
		a = Mathf.Max(a, b);
		if (Mathf.Abs(a - currentHalf) < 0.12f)
		{
			a = currentHalf;
		}
		float smoothTime = Plugin.Self.CameraSmooth.Value * ((a > currentHalf) ? 0.65f : 1.8f);
		currentHalf = Mathf.SmoothDamp(currentHalf, a, ref zoomVelocity, smoothTime);
		currentHalf = Mathf.Max(currentHalf, b);
		SetLens(currentHalf);
		Vector3 target = new Vector3((float)groupBounds.X, (float)groupBounds.Y + 1f, nativePosition.z);
		target.x = (float)FrameMath.Center(target.x, num5, num7, currentHalf * cam.aspect);
		target.y = (float)FrameMath.Center(target.y, num6, num8, currentHalf);
		float num13 = 0f;
		foreach (PlayerSlot player2 in session.Players)
		{
			if (CameraPresenceGuard.Framing(player2) && player2.Ready)
			{
				Rigidbody2D component = player2.Hero.GetComponent<Rigidbody2D>();
				if ((bool)component)
				{
					num13 = Mathf.Max(num13, component.velocity.magnitude);
				}
			}
		}
		if (num13 < 4f && new Vector2(target.x - position.x, target.y - position.y).sqrMagnitude < 0.09f)
		{
			target.x = position.x;
			target.y = position.y;
			velocity.x = (velocity.y = 0f);
		}
		float smoothTime2 = Mathf.Max(0.12f, Plugin.Self.CameraSmooth.Value * ((num13 > 15f) ? 0.55f : 1.3f));
		position = Vector3.SmoothDamp(position, target, ref velocity, smoothTime2);
		position.z = nativePosition.z;
		ctrl.transform.position = position;
		if (count > 1 && (num < position.x - currentHalf * cam.aspect + 0.2f || num2 > position.x + currentHalf * cam.aspect - 0.2f || num3 < position.y - currentHalf + 0.2f || num4 > position.y + currentHalf - 0.2f) && !TransitionVote.ApproachingExit(session))
		{
			RescueHint.QueueDistant();
		}
		Log(ctrl, "group", count, currentHalf, num7 - num5, num8 - num6);
		CameraPresenceGuard.After(this, ctrl, session);
	}

	private bool TryGodhomeFrame(CameraController ctrl, CoopSession session, float left, float right, float bottom, float top, int count, float minX, float maxX, float minY, float maxY)
	{
		BossSceneController instance = BossSceneController.Instance;
		if (!instance || !instance.HasTransitionedIn || !GameManager.instance || !GameManager.instance.GetSceneNameString().StartsWith("GG_", StringComparison.Ordinal))
		{
			return false;
		}
		if (!godhomeCaptured)
		{
			float num = 0f;
			float num2 = 0f;
			float a = baseHalf;
			bool flag = false;
			string text = "entrance";
			CameraLockArea cameraLockArea = ((ActiveLock != null) ? (ActiveLock.GetValue(ctrl) as CameraLockArea) : null);
			Collider2D collider2D = (cameraLockArea ? cameraLockArea.GetComponent<Collider2D>() : null);
			if ((bool)collider2D && collider2D.enabled)
			{
				Bounds bounds = collider2D.bounds;
				bool flag2 = false;
				foreach (PlayerSlot player in session.Players)
				{
					if (CameraPresenceGuard.Framing(player) && player.Ready && (bool)player.Hero && player.Hero.transform.position.x >= bounds.min.x - 3f && player.Hero.transform.position.x <= bounds.max.x + 3f && player.Hero.transform.position.y >= bounds.min.y - 3f && player.Hero.transform.position.y <= bounds.max.y + 3f)
					{
						flag2 = true;
					}
				}
				if (flag2 && bounds.size.x >= 15f && bounds.size.x <= 85f && bounds.size.y >= 6f && bounds.size.y <= 46f)
				{
					num = bounds.center.x;
					num2 = bounds.center.y;
					if (ctrl.mode == CameraController.CameraMode.LOCKED && ctrl.yLockMax >= ctrl.yLockMin && ctrl.yLockMax - ctrl.yLockMin < 5f)
					{
						num2 = (ctrl.yLockMin + ctrl.yLockMax) * 0.5f;
					}
					float num3 = bounds.min.x;
					float num4 = bounds.max.x;
					float num5 = ctrl.xLockMax - ctrl.xLockMin;
					if (ctrl.mode == CameraController.CameraMode.LOCKED && num5 >= 0f && num5 <= 65f && Mathf.Abs((ctrl.xLockMin + ctrl.xLockMax) * 0.5f - num) < 35f)
					{
						num3 = Mathf.Min(num3, ctrl.xLockMin - baseHalf * cam.aspect * 0.8f);
						num4 = Mathf.Max(num4, ctrl.xLockMax + baseHalf * cam.aspect * 0.8f);
					}
					num = (num3 + num4) * 0.5f;
					a = Mathf.Max(baseHalf, Mathf.Max((num4 - num3 + 2.5f) / (2f * Mathf.Max(0.1f, cam.aspect)), (bounds.size.y + 2f) * 0.5f));
					flag = true;
					text = "lock-collider";
				}
			}
			if (!flag && ctrl.mode == CameraController.CameraMode.LOCKED && ctrl.xLockMax >= ctrl.xLockMin && ctrl.xLockMax - ctrl.xLockMin <= 55f && ctrl.yLockMax >= ctrl.yLockMin && ctrl.yLockMax - ctrl.yLockMin <= 22f)
			{
				num = (ctrl.xLockMin + ctrl.xLockMax) * 0.5f;
				num2 = (ctrl.yLockMin + ctrl.yLockMax) * 0.5f;
				a = Mathf.Max(baseHalf, Mathf.Max(baseHalf + (ctrl.xLockMax - ctrl.xLockMin) / (2f * Mathf.Max(0.1f, cam.aspect)), baseHalf + (ctrl.yLockMax - ctrl.yLockMin) * 0.5f));
				flag = true;
				text = "camera-lock";
			}
			if (!flag && instance.bosses != null)
			{
				float num6 = float.MaxValue;
				float num7 = float.MinValue;
				float num8 = float.MaxValue;
				float num9 = float.MinValue;
				Vector3 vector = (instance.heroSpawn ? instance.heroSpawn.position : nativePosition);
				num6 = (num7 = vector.x);
				num8 = (num9 = vector.y);
				int num10 = 0;
				HealthManager[] bosses = instance.bosses;
				foreach (HealthManager healthManager in bosses)
				{
					if ((bool)healthManager && Mathf.Abs(healthManager.transform.position.x - vector.x) < 48f && Mathf.Abs(healthManager.transform.position.y - vector.y) < 30f)
					{
						Vector3 vector2 = healthManager.transform.position;
						num6 = Mathf.Min(num6, vector2.x);
						num7 = Mathf.Max(num7, vector2.x);
						num8 = Mathf.Min(num8, vector2.y);
						num9 = Mathf.Max(num9, vector2.y);
						num10++;
					}
				}
				if (num10 > 0)
				{
					num = (num6 + num7) * 0.5f;
					num2 = (num8 + num9) * 0.5f + 1.5f;
					a = Mathf.Max(baseHalf * 1.2f, Mathf.Max((num7 - num6 + 12f) / (2f * Mathf.Max(0.1f, cam.aspect)), (num9 - num8 + 9f) * 0.5f));
					flag = true;
					text = "boss-spawns";
				}
			}
			if (!flag)
			{
				num = nativePosition.x;
				num2 = nativePosition.y;
				a = baseHalf * 1.2f;
			}
			float num11 = Mathf.Min((top - bottom) * 0.5f, (right - left) * 0.5f / Mathf.Max(0.1f, cam.aspect));
			godhomeHalf = GodhomeCameraTuning.InitialHalf(Mathf.Min(a, Mathf.Max(baseHalf, Mathf.Min(baseHalf * 1.55f, num11 * 1.25f))), baseHalf, ctrl);
			godhomeCenter = new Vector2((float)FrameMath.Center(num, left, right, godhomeHalf * cam.aspect), (float)FrameMath.Center(num2, bottom, top, godhomeHalf));
			godhomeCaptured = true;
			string[] array = new string[22];
			array[0] = "CAMERA Godhome arena fixed center=";
			Vector2 vector3 = godhomeCenter;
			array[1] = vector3.ToString();
			array[2] = " half=";
			array[3] = godhomeHalf.ToString();
			array[4] = " source=";
			array[5] = text;
			array[6] = " lock=";
			array[7] = (cameraLockArea ? cameraLockArea.name : "none");
			array[8] = " lockX=";
			array[9] = ctrl.xLockMin.ToString();
			array[10] = "..";
			array[11] = ctrl.xLockMax.ToString();
			array[12] = " lockY=";
			array[13] = ctrl.yLockMin.ToString();
			array[14] = "..";
			array[15] = ctrl.yLockMax.ToString();
			array[16] = " scene=";
			array[17] = ctrl.sceneWidth.ToString();
			array[18] = "x";
			array[19] = ctrl.sceneHeight.ToString();
			array[20] = " zone=";
			array[21] = (collider2D ? collider2D.bounds.ToString() : "none");
			Diagnostics.Write(string.Concat(array));
		}
		float a2 = Mathf.Max(Mathf.Abs(minX - godhomeCenter.x), Mathf.Abs(maxX - godhomeCenter.x)) / Mathf.Max(0.1f, cam.aspect) + 1.2f;
		a2 = Mathf.Max(a2, Mathf.Max(Mathf.Abs(minY - godhomeCenter.y), Mathf.Abs(maxY - godhomeCenter.y)) + 1.4f);
		if (a2 > godhomeHalf + 0.4f)
		{
			float num12 = Mathf.Min((top - bottom) * 0.5f, (right - left) * 0.5f / Mathf.Max(0.1f, cam.aspect));
			godhomeHalf = Mathf.Min(Mathf.Max(baseHalf, Mathf.Min(baseHalf * 1.65f, num12 * 1.35f)), a2);
		}
		float num13 = godhomeHalf * cam.aspect - 1.2f;
		float num14 = godhomeHalf - 1.4f;
		if (maxX - minX <= 2f * num13 && (minX < godhomeCenter.x - num13 || maxX > godhomeCenter.x + num13))
		{
			godhomeCenter.x = Mathf.Clamp(godhomeCenter.x, maxX - num13, minX + num13);
		}
		if (maxY - minY <= 2f * num14 && (minY < godhomeCenter.y - num14 || maxY > godhomeCenter.y + num14))
		{
			godhomeCenter.y = Mathf.Clamp(godhomeCenter.y, maxY - num14, minY + num14);
		}
		godhomeCenter.x = (float)FrameMath.Center(godhomeCenter.x, left, right, godhomeHalf * cam.aspect);
		godhomeCenter.y = (float)FrameMath.Center(godhomeCenter.y, bottom, top, godhomeHalf);
		currentHalf = Mathf.SmoothDamp(currentHalf, godhomeHalf, ref zoomVelocity, 0.35f);
		if (Mathf.Abs(currentHalf - godhomeHalf) < 0.025f)
		{
			currentHalf = godhomeHalf;
			zoomVelocity = 0f;
		}
		SetLens(currentHalf);
		Vector3 vector4 = new Vector3(godhomeCenter.x, godhomeCenter.y, nativePosition.z);
		position = Vector3.SmoothDamp(position, vector4, ref velocity, 0.32f);
		if ((position - vector4).sqrMagnitude < 0.001f)
		{
			position = vector4;
			velocity = Vector3.zero;
		}
		position.z = nativePosition.z;
		ctrl.transform.position = position;
		Log(ctrl, "Godhome-fixed", count, currentHalf, right - left, top - bottom);
		return true;
	}

	internal void SetLens(float half)
	{
		if ((bool)tkCam)
		{
			float num = half / Mathf.Max(0.01f, baseHalf);
			if (cam.orthographic)
			{
				tkCam.ZoomFactor = baseTkZoom / num;
			}
			else
			{
				float b = 2f * Mathf.Atan(Mathf.Tan(baseFov * ((float)Math.PI / 180f) * 0.5f) * num) * 57.29578f;
				tkCam.ZoomFactor = baseTkZoom * baseFov / Mathf.Max(0.01f, b);
			}
			tkCam.UpdateCameraMatrix();
		}
		else if (cam.orthographic)
		{
			cam.orthographicSize = half;
		}
		else
		{
			cam.fieldOfView = 2f * Mathf.Atan(half / Mathf.Max(0.01f, Mathf.Abs(cam.transform.position.z))) * 57.29578f;
		}
	}

	private void Log(CameraController ctrl, string reason, int count, float half, float width, float height)
	{
		if (!(Time.unscaledTime < nextLog))
		{
			nextLog = Time.unscaledTime + 5f;
			string[] obj = new string[22]
			{
				"CAMERA ",
				reason,
				" players=",
				count.ToString(),
				" half=",
				half.ToString(),
				" base=",
				baseHalf.ToString(),
				" room=",
				width.ToString(),
				"x",
				height.ToString(),
				" mode=",
				ctrl.mode.ToString(),
				" pos=",
				ctrl.transform.position.ToString(),
				" native=",
				null,
				null,
				null,
				null,
				null
			};
			Vector3 vector = nativePosition;
			obj[17] = vector.ToString();
			obj[18] = " lockMin=";
			obj[19] = ctrl.yLockMin.ToString();
			obj[20] = " tkZoom=";
			obj[21] = (tkCam ? tkCam.ZoomFactor : 0f).ToString();
			Diagnostics.Write(string.Concat(obj));
		}
	}

	private void RestoreLens()
	{
		if ((bool)tkCam)
		{
			tkCam.ZoomFactor = baseTkZoom;
		}
		if ((bool)cam)
		{
			cam.orthographicSize = baseSize;
			cam.fieldOfView = baseFov;
		}
		currentHalf = baseHalf;
	}

	internal void Reset()
	{
		RestoreLens();
		cam = null;
		tkCam = null;
		initialized = false;
		nativeSaved = false;
		godhomeCaptured = false;
		velocity = Vector3.zero;
		zoomVelocity = 0f;
		nextLog = 0f;
		CameraPresenceGuard.Reset();
	}
}
