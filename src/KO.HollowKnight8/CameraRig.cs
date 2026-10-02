using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KO.HollowKnight8;

internal sealed class CameraRig
{
	private static readonly FieldInfo ActiveLock = typeof(CameraController).GetField("currentLockArea", BindingFlags.Instance | BindingFlags.NonPublic);

	private Camera cam;

	private tk2dCamera tkCam;

	private float baseTkZoom = 1f;

	private float nextLog;

	private float baseSize;

	private float baseFov;

	private float baseHalf;

	private float currentHalf;

	private float zoomVelocity;

	private Vector3 position;

	private Vector3 velocity;

	private Vector3 nativePosition;

	private bool nativeSaved;

	private bool initialized;

	private bool godhomeCaptured;

	private Vector2 godhomeCenter;

	private float godhomeHalf;

	internal void Prepare(CameraController ctrl)
	{
		//IL_0041: Invalid comparison between Unknown and I4
		//IL_004a: Invalid comparison between Unknown and I4
		Camera val = ctrl.cam ?? Camera.main;
		if (!Object.op_Implicit((Object)(object)val))
		{
			return;
		}
		if (initialized && (Object)(object)cam == (Object)(object)val)
		{
			if (nativeSaved && ((int)ctrl.mode == 1 || (int)ctrl.mode == 2))
			{
				((Component)ctrl).transform.position = nativePosition;
			}
			if (Object.op_Implicit((Object)(object)tkCam))
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
		cam = val;
		tkCam = (Object.op_Implicit((Object)(object)GameCameras.instance) ? GameCameras.instance.tk2dCam : null);
		baseTkZoom = (Object.op_Implicit((Object)(object)tkCam) ? Mathf.Max(0.01f, tkCam.ZoomFactor) : 1f);
		baseSize = cam.orthographicSize;
		baseFov = cam.fieldOfView;
		baseHalf = (cam.orthographic ? baseSize : (Mathf.Abs(((Component)cam).transform.position.z) * Mathf.Tan(baseFov * ((float)Math.PI / 180f) * 0.5f)));
		if (baseHalf < 1f)
		{
			baseHalf = 8.3f;
		}
		currentHalf = baseHalf;
		position = ((Component)ctrl).transform.position;
		initialized = true;
		Diagnostics.Write("CAMERA init tk2d=" + ((Object)(object)tkCam != (Object)null) + " ortho=" + cam.orthographic + " size=" + baseSize + " fov=" + baseFov + " z=" + ((Component)cam).transform.position.z + " half=" + baseHalf + " zoom=" + baseTkZoom);
	}

	internal void Apply(CameraController ctrl, CoopSession session)
	{
		//IL_016f: Invalid comparison between Unknown and I4
		//IL_0178: Invalid comparison between Unknown and I4
		//IL_0234: Invalid comparison between Unknown and I4
		if (!initialized || !Object.op_Implicit((Object)(object)cam))
		{
			return;
		}
		nativePosition = ((Component)ctrl).transform.position;
		nativeSaved = true;
		if (!Plugin.Self.ZoomByPlayerCount.Value)
		{
			RestoreLens();
			position = ((Component)ctrl).transform.position;
			velocity = Vector3.zero;
			return;
		}
		GroupBounds groupBounds = default(GroupBounds);
		foreach (PlayerSlot player in session.Players)
		{
			if (player.Alive && player.Ready)
			{
				Bounds val = DuelGround.Body(player);
				groupBounds.AddBox(((Bounds)(ref val)).min.x - 0.15f, ((Bounds)(ref val)).max.x + 0.15f, ((Bounds)(ref val)).min.y - 0.2f, ((Bounds)(ref val)).max.y + 0.65f);
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
			position = ((Component)ctrl).transform.position;
		}
		else
		{
			if (count == 0)
			{
				return;
			}
			if ((int)ctrl.mode == 0 || (int)ctrl.mode == 4 || (int)ctrl.mode == 3)
			{
				Log(ctrl, "mode=" + ((object)Unsafe.As<CameraMode, CameraMode>(ref ctrl.mode)/*cast due to .constrained prefix*/).ToString(), count, baseHalf, 0f, 0f);
				SetLens(currentHalf);
				position = ((Component)ctrl).transform.position;
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
				return;
			}
			if ((int)ctrl.mode == 2)
			{
				num6 = Mathf.Max(num6, Mathf.Min(ctrl.yLockMin - baseHalf, num3 - 2f));
			}
			float num9 = Mathf.Min((num8 - num6) * 0.5f, (num7 - num5) * 0.5f / Mathf.Max(0.1f, cam.aspect));
			if (TryGodhomeFrame(ctrl, session, num5, num7, num6, num8, count, num, num2, num3, num4))
			{
				return;
			}
			if (num9 < baseHalf)
			{
				SetLens(currentHalf);
				position = Vector3.SmoothDamp(position, ((Component)ctrl).transform.position, ref velocity, Plugin.Self.CameraSmooth.Value);
				position.z = nativePosition.z;
				((Component)ctrl).transform.position = position;
				return;
			}
			float value = Plugin.Self.ZoomLimit.Value;
			value = Mathf.Min(2f, value + (float)Mathf.Max(0, count - 1) * Plugin.Self.ZoomPerPlayer.Value);
			float num10 = (num2 - num + 2f * Mathf.Max(2.8f, Plugin.Self.SideMargin.Value)) / (2f * Mathf.Max(0.1f, cam.aspect));
			value = Mathf.Max(value, num10 / baseHalf);
			float num11 = (float)FrameMath.HalfHeight(num, num2, num3, num4, cam.aspect, baseHalf, value, num7 - num5, num8 - num6, Plugin.Self.SideMargin.Value);
			float num12 = num3 - 1.2f;
			float num13 = num4 + 3.2f;
			float num14 = Mathf.Min(num9, Mathf.Max((num13 - num12) * 0.5f, num10));
			num11 = Mathf.Max(num11, num14);
			if (Mathf.Abs(num11 - currentHalf) < 0.12f)
			{
				num11 = currentHalf;
			}
			float num15 = Plugin.Self.CameraSmooth.Value * ((num11 > currentHalf) ? 0.65f : 1.8f);
			currentHalf = Mathf.SmoothDamp(currentHalf, num11, ref zoomVelocity, num15);
			currentHalf = Mathf.Max(currentHalf, num14);
			SetLens(currentHalf);
			Vector3 val2 = default(Vector3);
			((Vector3)(ref val2))._002Ector((float)groupBounds.X, (float)groupBounds.Y + 1f, nativePosition.z);
			val2.x = (float)FrameMath.Center(val2.x, num5, num7, currentHalf * cam.aspect);
			val2.y = (float)FrameMath.Center(val2.y, num6, num8, currentHalf);
			float num16 = 0f;
			Vector2 val3;
			foreach (PlayerSlot player2 in session.Players)
			{
				if (player2.Alive && player2.Ready)
				{
					Rigidbody2D component = ((Component)player2.Hero).GetComponent<Rigidbody2D>();
					if (Object.op_Implicit((Object)(object)component))
					{
						float num17 = num16;
						val3 = component.velocity;
						num16 = Mathf.Max(num17, ((Vector2)(ref val3)).magnitude);
					}
				}
			}
			if (num16 < 4f)
			{
				val3 = new Vector2(val2.x - position.x, val2.y - position.y);
				if (((Vector2)(ref val3)).sqrMagnitude < 0.09f)
				{
					val2.x = position.x;
					val2.y = position.y;
					velocity.x = (velocity.y = 0f);
				}
			}
			float num18 = Mathf.Max(0.12f, Plugin.Self.CameraSmooth.Value * ((num16 > 15f) ? 0.55f : 1.3f));
			position = Vector3.SmoothDamp(position, val2, ref velocity, num18);
			position.z = nativePosition.z;
			((Component)ctrl).transform.position = position;
			if (count > 1 && (num < position.x - currentHalf * cam.aspect + 0.2f || num2 > position.x + currentHalf * cam.aspect - 0.2f || num3 < position.y - currentHalf + 0.2f || num4 > position.y + currentHalf - 0.2f) && !TransitionVote.ApproachingExit(session))
			{
				RescueHint.QueueDistant();
			}
			Log(ctrl, "group", count, currentHalf, num7 - num5, num8 - num6);
		}
	}

	private unsafe bool TryGodhomeFrame(CameraController ctrl, CoopSession session, float left, float right, float bottom, float top, int count, float minX, float maxX, float minY, float maxY)
	{
		//IL_03bb: Invalid comparison between Unknown and I4
		//IL_0251: Invalid comparison between Unknown and I4
		//IL_02bc: Invalid comparison between Unknown and I4
		BossSceneController instance = BossSceneController.Instance;
		if (!Object.op_Implicit((Object)(object)instance) || !instance.HasTransitionedIn || !Object.op_Implicit((Object)(object)GameManager.instance) || !GameManager.instance.GetSceneNameString().StartsWith("GG_", StringComparison.Ordinal))
		{
			return false;
		}
		if (!godhomeCaptured)
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = baseHalf;
			bool flag = false;
			string text = "entrance";
			CameraLockArea val = (CameraLockArea)((ActiveLock != null) ? /*isinst with value type is only supported in some contexts*/: null);
			Collider2D val2 = (Object.op_Implicit((Object)(object)val) ? ((Component)val).GetComponent<Collider2D>() : null);
			if (Object.op_Implicit((Object)(object)val2) && ((Behaviour)val2).enabled)
			{
				Bounds bounds = val2.bounds;
				bool flag2 = false;
				foreach (PlayerSlot player in session.Players)
				{
					if (player.Alive && player.Ready && Object.op_Implicit((Object)(object)player.Hero) && ((Component)player.Hero).transform.position.x >= ((Bounds)(ref bounds)).min.x - 3f && ((Component)player.Hero).transform.position.x <= ((Bounds)(ref bounds)).max.x + 3f && ((Component)player.Hero).transform.position.y >= ((Bounds)(ref bounds)).min.y - 3f && ((Component)player.Hero).transform.position.y <= ((Bounds)(ref bounds)).max.y + 3f)
					{
						flag2 = true;
					}
				}
				if (flag2 && ((Bounds)(ref bounds)).size.x >= 15f && ((Bounds)(ref bounds)).size.x <= 85f && ((Bounds)(ref bounds)).size.y >= 6f && ((Bounds)(ref bounds)).size.y <= 46f)
				{
					num = ((Bounds)(ref bounds)).center.x;
					num2 = ((Bounds)(ref bounds)).center.y;
					if ((int)ctrl.mode == 2 && ctrl.yLockMax >= ctrl.yLockMin && ctrl.yLockMax - ctrl.yLockMin < 5f)
					{
						num2 = (ctrl.yLockMin + ctrl.yLockMax) * 0.5f;
					}
					float num4 = ((Bounds)(ref bounds)).min.x;
					float num5 = ((Bounds)(ref bounds)).max.x;
					float num6 = ctrl.xLockMax - ctrl.xLockMin;
					if ((int)ctrl.mode == 2 && num6 >= 0f && num6 <= 65f && Mathf.Abs((ctrl.xLockMin + ctrl.xLockMax) * 0.5f - num) < 35f)
					{
						num4 = Mathf.Min(num4, ctrl.xLockMin - baseHalf * cam.aspect * 0.8f);
						num5 = Mathf.Max(num5, ctrl.xLockMax + baseHalf * cam.aspect * 0.8f);
					}
					num = (num4 + num5) * 0.5f;
					num3 = Mathf.Max(baseHalf, Mathf.Max((num5 - num4 + 2.5f) / (2f * Mathf.Max(0.1f, cam.aspect)), (((Bounds)(ref bounds)).size.y + 2f) * 0.5f));
					flag = true;
					text = "lock-collider";
				}
			}
			if (!flag && (int)ctrl.mode == 2 && ctrl.xLockMax >= ctrl.xLockMin && ctrl.xLockMax - ctrl.xLockMin <= 55f && ctrl.yLockMax >= ctrl.yLockMin && ctrl.yLockMax - ctrl.yLockMin <= 22f)
			{
				num = (ctrl.xLockMin + ctrl.xLockMax) * 0.5f;
				num2 = (ctrl.yLockMin + ctrl.yLockMax) * 0.5f;
				num3 = Mathf.Max(baseHalf, Mathf.Max(baseHalf + (ctrl.xLockMax - ctrl.xLockMin) / (2f * Mathf.Max(0.1f, cam.aspect)), baseHalf + (ctrl.yLockMax - ctrl.yLockMin) * 0.5f));
				flag = true;
				text = "camera-lock";
			}
			if (!flag && instance.bosses != null)
			{
				float num7 = float.MaxValue;
				float num8 = float.MinValue;
				float num9 = float.MaxValue;
				float num10 = float.MinValue;
				Vector3 val3 = (Object.op_Implicit((Object)(object)instance.heroSpawn) ? instance.heroSpawn.position : nativePosition);
				num7 = (num8 = val3.x);
				num9 = (num10 = val3.y);
				int num11 = 0;
				HealthManager[] bosses = instance.bosses;
				foreach (HealthManager val4 in bosses)
				{
					if (Object.op_Implicit((Object)(object)val4) && Mathf.Abs(((Component)val4).transform.position.x - val3.x) < 48f && Mathf.Abs(((Component)val4).transform.position.y - val3.y) < 30f)
					{
						Vector3 val5 = ((Component)val4).transform.position;
						num7 = Mathf.Min(num7, val5.x);
						num8 = Mathf.Max(num8, val5.x);
						num9 = Mathf.Min(num9, val5.y);
						num10 = Mathf.Max(num10, val5.y);
						num11++;
					}
				}
				if (num11 > 0)
				{
					num = (num7 + num8) * 0.5f;
					num2 = (num9 + num10) * 0.5f + 1.5f;
					num3 = Mathf.Max(baseHalf * 1.2f, Mathf.Max((num8 - num7 + 12f) / (2f * Mathf.Max(0.1f, cam.aspect)), (num10 - num9 + 9f) * 0.5f));
					flag = true;
					text = "boss-spawns";
				}
			}
			if (!flag)
			{
				num = nativePosition.x;
				num2 = nativePosition.y;
				num3 = baseHalf * 1.2f;
			}
			float num12 = Mathf.Min((top - bottom) * 0.5f, (right - left) * 0.5f / Mathf.Max(0.1f, cam.aspect));
			godhomeHalf = Mathf.Min(num3, Mathf.Max(baseHalf, Mathf.Min(baseHalf * 1.55f, num12 * 1.25f)));
			godhomeCenter = new Vector2((float)FrameMath.Center(num, left, right, godhomeHalf * cam.aspect), (float)FrameMath.Center(num2, bottom, top, godhomeHalf));
			godhomeCaptured = true;
			string[] array = new string[22];
			array[0] = "CAMERA Godhome arena fixed center=";
			Vector2 val6 = godhomeCenter;
			array[1] = ((object)(*(Vector2*)(&val6))/*cast due to .constrained prefix*/).ToString();
			array[2] = " half=";
			array[3] = godhomeHalf.ToString();
			array[4] = " source=";
			array[5] = text;
			array[6] = " lock=";
			array[7] = (Object.op_Implicit((Object)(object)val) ? ((Object)val).name : "none");
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
			array[21] = (Object.op_Implicit((Object)(object)val2) ? ((object)val2.bounds/*cast due to .constrained prefix*/).ToString() : "none");
			Diagnostics.Write(string.Concat(array));
		}
		float num13 = Mathf.Max(Mathf.Abs(minX - godhomeCenter.x), Mathf.Abs(maxX - godhomeCenter.x)) / Mathf.Max(0.1f, cam.aspect) + 1.2f;
		num13 = Mathf.Max(num13, Mathf.Max(Mathf.Abs(minY - godhomeCenter.y), Mathf.Abs(maxY - godhomeCenter.y)) + 1.4f);
		if (num13 > godhomeHalf + 0.4f)
		{
			float num14 = Mathf.Min((top - bottom) * 0.5f, (right - left) * 0.5f / Mathf.Max(0.1f, cam.aspect));
			godhomeHalf = Mathf.Min(Mathf.Max(baseHalf, Mathf.Min(baseHalf * 1.65f, num14 * 1.35f)), num13);
		}
		float num15 = godhomeHalf * cam.aspect - 1.2f;
		float num16 = godhomeHalf - 1.4f;
		if (maxX - minX <= 2f * num15 && (minX < godhomeCenter.x - num15 || maxX > godhomeCenter.x + num15))
		{
			godhomeCenter.x = Mathf.Clamp(godhomeCenter.x, maxX - num15, minX + num15);
		}
		if (maxY - minY <= 2f * num16 && (minY < godhomeCenter.y - num16 || maxY > godhomeCenter.y + num16))
		{
			godhomeCenter.y = Mathf.Clamp(godhomeCenter.y, maxY - num16, minY + num16);
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
		Vector3 val7 = default(Vector3);
		((Vector3)(ref val7))._002Ector(godhomeCenter.x, godhomeCenter.y, nativePosition.z);
		position = Vector3.SmoothDamp(position, val7, ref velocity, 0.32f);
		Vector3 val8 = position - val7;
		if (((Vector3)(ref val8)).sqrMagnitude < 0.001f)
		{
			position = val7;
			velocity = Vector3.zero;
		}
		position.z = nativePosition.z;
		((Component)ctrl).transform.position = position;
		Log(ctrl, "Godhome-fixed", count, currentHalf, right - left, top - bottom);
		return true;
	}

	private void SetLens(float half)
	{
		if (Object.op_Implicit((Object)(object)tkCam))
		{
			float num = half / Mathf.Max(0.01f, baseHalf);
			if (cam.orthographic)
			{
				tkCam.ZoomFactor = baseTkZoom / num;
			}
			else
			{
				float num2 = 2f * Mathf.Atan(Mathf.Tan(baseFov * ((float)Math.PI / 180f) * 0.5f) * num) * 57.29578f;
				tkCam.ZoomFactor = baseTkZoom * baseFov / Mathf.Max(0.01f, num2);
			}
			tkCam.UpdateCameraMatrix();
		}
		else if (cam.orthographic)
		{
			cam.orthographicSize = half;
		}
		else
		{
			cam.fieldOfView = 2f * Mathf.Atan(half / Mathf.Max(0.01f, Mathf.Abs(((Component)cam).transform.position.z))) * 57.29578f;
		}
	}

	private unsafe void Log(CameraController ctrl, string reason, int count, float half, float width, float height)
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
				((object)Unsafe.As<CameraMode, CameraMode>(ref ctrl.mode)/*cast due to .constrained prefix*/).ToString(),
				" pos=",
				((object)((Component)ctrl).transform.position/*cast due to .constrained prefix*/).ToString(),
				" native=",
				null,
				null,
				null,
				null,
				null
			};
			Vector3 val = nativePosition;
			obj[17] = ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString();
			obj[18] = " lockMin=";
			obj[19] = ctrl.yLockMin.ToString();
			obj[20] = " tkZoom=";
			obj[21] = (Object.op_Implicit((Object)(object)tkCam) ? tkCam.ZoomFactor : 0f).ToString();
			Diagnostics.Write(string.Concat(obj));
		}
	}

	private void RestoreLens()
	{
		if (Object.op_Implicit((Object)(object)tkCam))
		{
			tkCam.ZoomFactor = baseTkZoom;
		}
		if (Object.op_Implicit((Object)(object)cam))
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
	}
}
