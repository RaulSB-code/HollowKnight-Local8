using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace KO.HollowKnight8;

internal sealed class SharedVisibility : IDisposable
{
	private const int Columns = 80;

	private const int Rows = 46;

	private GameObject root;

	private Mesh mesh;

	private MeshRenderer renderer;

	private Material material;

	private Vector3[] vertices;

	private Color[] colors;

	private readonly Vector2[] positions = (Vector2[])(object)new Vector2[8];

	private readonly Vector2[] previousPositions = (Vector2[])(object)new Vector2[8];

	private int previousCount;

	private float stableRadius = 1f;

	private float previousRadius;

	private float previousOpacity;

	private Matrix4x4 previousProjection;

	private Matrix4x4 previousCamera;

	private bool built;

	internal string Status = "pending";

	internal void Render(Camera camera, CoopSession session, SpriteRenderer source, bool softDarkness)
	{
		if (Object.op_Implicit((Object)(object)renderer))
		{
			((Renderer)renderer).enabled = false;
		}
		int num = 0;
		foreach (PlayerSlot player in session.Players)
		{
			if (player.Alive && player.Ready && !TransitionVote.Holding(player) && num < 8)
			{
				positions[num++] = Vector2.op_Implicit(((Component)player.Hero).transform.position);
			}
		}
		if (num == 0 || !softDarkness)
		{
			Status = (softDarkness ? "no living players" : "clear visibility");
			return;
		}
		if (!Prepare())
		{
			Status = "shader unavailable; clear visibility";
			return;
		}
		float num2 = Mathf.Clamp((Object.op_Implicit((Object)(object)source) ? Mathf.Abs(((Component)source).transform.lossyScale.x) : 5.5f) / 5.5f, 0.12f, 1.6f);
		bool flag = false;
		foreach (PlayerSlot player2 in session.Players)
		{
			if (player2.Alive && player2.Hero.cState.focusing)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			num2 = Mathf.Max(num2, stableRadius);
		}
		else
		{
			stableRadius = num2;
		}
		float num3 = ((Object.op_Implicit((Object)(object)source) && ((Component)source).gameObject.activeInHierarchy) ? Mathf.Clamp01(source.color.a) : 0f);
		float num4 = 7f * num2;
		float num5 = 14f * num2;
		Matrix4x4 projectionMatrix = camera.projectionMatrix;
		Matrix4x4 localToWorldMatrix = ((Component)camera).transform.localToWorldMatrix;
		bool flag2 = !built || num != previousCount || num2 != previousRadius || num3 != previousOpacity || projectionMatrix != previousProjection || localToWorldMatrix != previousCamera;
		for (int i = 0; i < num; i++)
		{
			if (flag2)
			{
				break;
			}
			if (positions[i] != previousPositions[i])
			{
				flag2 = true;
			}
		}
		if (flag2)
		{
			float num6 = Vector3.Dot(new Vector3(0f, 0f, 0f) - ((Component)camera).transform.position, ((Component)camera).transform.forward);
			float num7 = Mathf.Max(camera.nearClipPlane + 0.1f, Mathf.Min(5f, num6 * 0.5f));
			Vector3 val = camera.ViewportToWorldPoint(new Vector3(-0.01f, -0.01f, num7));
			Vector3 val2 = (camera.ViewportToWorldPoint(new Vector3(1.01f, -0.01f, num7)) - val) / 48f;
			Vector3 val3 = (camera.ViewportToWorldPoint(new Vector3(-0.01f, 1.01f, num7)) - val) / 28f;
			Vector2 val4 = Vector2.op_Implicit(camera.ViewportToWorldPoint(new Vector3(-0.01f, -0.01f, num6)));
			Vector2 val5 = (Vector2.op_Implicit(camera.ViewportToWorldPoint(new Vector3(1.01f, -0.01f, num6))) - val4) / 48f;
			Vector2 val6 = (Vector2.op_Implicit(camera.ViewportToWorldPoint(new Vector3(-0.01f, 1.01f, num6))) - val4) / 28f;
			int num8 = 0;
			for (int j = 0; j <= 28; j++)
			{
				for (int k = 0; k <= 48; k++)
				{
					vertices[num8] = val + (float)k * val2 + (float)j * val3;
					Vector2 val7 = val4 + (float)k * val5 + (float)j * val6;
					float num9 = float.MaxValue;
					for (int l = 0; l < num; l++)
					{
						float num10 = num9;
						Vector2 val8 = val7 - positions[l];
						num9 = Mathf.Min(num10, ((Vector2)(ref val8)).sqrMagnitude);
					}
					colors[num8++] = new Color(0f, 0f, 0f, (float)VisibilityRules.Alpha(num9, num4, num5, num3));
				}
			}
			mesh.vertices = vertices;
			mesh.colors = colors;
			mesh.RecalculateBounds();
			previousCount = num;
			previousRadius = num2;
			previousOpacity = num3;
			previousProjection = projectionMatrix;
			previousCamera = localToWorldMatrix;
			Array.Copy(positions, previousPositions, num);
			built = true;
		}
		int m = ((Component)session.Primary.Hero).gameObject.layer;
		if ((camera.cullingMask & (1 << m)) == 0)
		{
			for (m = 0; m < 31 && (camera.cullingMask & (1 << m)) == 0; m++)
			{
			}
		}
		root.layer = Mathf.Min(31, m);
		if (Object.op_Implicit((Object)(object)source))
		{
			((Renderer)renderer).sortingLayerID = ((Renderer)source).sortingLayerID;
			((Renderer)renderer).sortingOrder = ((Renderer)source).sortingOrder;
		}
		((Renderer)renderer).enabled = num3 > 0.001f;
		Status = "holes=" + num + " inner=" + num4 + " outer=" + num5 + " opacity=" + num3;
	}

	private bool Prepare()
	{
		if (Object.op_Implicit((Object)(object)root))
		{
			return true;
		}
		Shader val = Shader.Find("Sprites/Default");
		if (!Object.op_Implicit((Object)(object)val))
		{
			return false;
		}
		root = new GameObject("Local8 Shared Visibility");
		Object.DontDestroyOnLoad((Object)(object)root);
		renderer = root.AddComponent<MeshRenderer>();
		((Renderer)renderer).enabled = false;
		((Renderer)renderer).shadowCastingMode = (ShadowCastingMode)0;
		((Renderer)renderer).receiveShadows = false;
		material = new Material(val)
		{
			name = "Local8 shared darkness",
			mainTexture = (Texture)(object)Texture2D.whiteTexture
		};
		((Renderer)renderer).sharedMaterial = material;
		mesh = new Mesh
		{
			name = "Local8 visibility holes"
		};
		mesh.MarkDynamic();
		root.AddComponent<MeshFilter>().sharedMesh = mesh;
		vertices = (Vector3[])(object)new Vector3[1421];
		colors = (Color[])(object)new Color[vertices.Length];
		Vector2[] uv = (Vector2[])(object)new Vector2[vertices.Length];
		int[] array = new int[8064];
		int num = 0;
		for (int i = 0; i < 28; i++)
		{
			for (int j = 0; j < 48; j++)
			{
				int num2 = i * 49 + j;
				array[num++] = num2;
				array[num++] = num2 + 48 + 1;
				array[num++] = num2 + 1;
				array[num++] = num2 + 1;
				array[num++] = num2 + 48 + 1;
				array[num++] = num2 + 48 + 2;
			}
		}
		mesh.vertices = vertices;
		mesh.colors = colors;
		mesh.uv = uv;
		mesh.triangles = array;
		return true;
	}

	internal void Hide()
	{
		if (Object.op_Implicit((Object)(object)renderer))
		{
			((Renderer)renderer).enabled = false;
		}
	}

	public void Dispose()
	{
		if (Object.op_Implicit((Object)(object)root))
		{
			Object.Destroy((Object)(object)root);
		}
		if (Object.op_Implicit((Object)(object)mesh))
		{
			Object.Destroy((Object)(object)mesh);
		}
		if (Object.op_Implicit((Object)(object)material))
		{
			Object.Destroy((Object)(object)material);
		}
		root = null;
		mesh = null;
		renderer = null;
		material = null;
		built = false;
	}
}
