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

	private readonly Vector2[] positions = new Vector2[8];

	private readonly Vector2[] previousPositions = new Vector2[8];

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
		if ((bool)renderer)
		{
			renderer.enabled = false;
		}
		int num = 0;
		foreach (PlayerSlot player in session.Players)
		{
			if (player.Alive && player.Ready && !TransitionVote.Holding(player) && num < 8)
			{
				positions[num++] = player.Hero.transform.position;
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
		float num2 = Mathf.Clamp((source ? Mathf.Abs(source.transform.lossyScale.x) : 5.5f) / 5.5f, 0.12f, 1.6f);
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
		float num3 = (((bool)source && source.gameObject.activeInHierarchy) ? Mathf.Clamp01(source.color.a) : 0f);
		float num4 = 7f * num2;
		float num5 = 14f * num2;
		Matrix4x4 projectionMatrix = camera.projectionMatrix;
		Matrix4x4 localToWorldMatrix = camera.transform.localToWorldMatrix;
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
			float num6 = Vector3.Dot(new Vector3(0f, 0f, 0f) - camera.transform.position, camera.transform.forward);
			float z = Mathf.Max(camera.nearClipPlane + 0.1f, Mathf.Min(5f, num6 * 0.5f));
			Vector3 vector = camera.ViewportToWorldPoint(new Vector3(-0.01f, -0.01f, z));
			Vector3 vector2 = (camera.ViewportToWorldPoint(new Vector3(1.01f, -0.01f, z)) - vector) / 48f;
			Vector3 vector3 = (camera.ViewportToWorldPoint(new Vector3(-0.01f, 1.01f, z)) - vector) / 28f;
			Vector2 vector4 = camera.ViewportToWorldPoint(new Vector3(-0.01f, -0.01f, num6));
			Vector2 vector5 = ((Vector2)camera.ViewportToWorldPoint(new Vector3(1.01f, -0.01f, num6)) - vector4) / 48f;
			Vector2 vector6 = ((Vector2)camera.ViewportToWorldPoint(new Vector3(-0.01f, 1.01f, num6)) - vector4) / 28f;
			int num7 = 0;
			for (int j = 0; j <= 28; j++)
			{
				for (int k = 0; k <= 48; k++)
				{
					vertices[num7] = vector + k * vector2 + j * vector3;
					Vector2 vector7 = vector4 + k * vector5 + j * vector6;
					float num8 = float.MaxValue;
					for (int l = 0; l < num; l++)
					{
						num8 = Mathf.Min(num8, (vector7 - positions[l]).sqrMagnitude);
					}
					colors[num7++] = new Color(0f, 0f, 0f, (float)VisibilityRules.Alpha(num8, num4, num5, num3));
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
		int m = session.Primary.Hero.gameObject.layer;
		if ((camera.cullingMask & (1 << m)) == 0)
		{
			for (m = 0; m < 31 && (camera.cullingMask & (1 << m)) == 0; m++)
			{
			}
		}
		root.layer = Mathf.Min(31, m);
		if ((bool)source)
		{
			renderer.sortingLayerID = source.sortingLayerID;
			renderer.sortingOrder = source.sortingOrder;
		}
		renderer.enabled = num3 > 0.001f;
		Status = "holes=" + num + " inner=" + num4 + " outer=" + num5 + " opacity=" + num3;
	}

	private bool Prepare()
	{
		if ((bool)root)
		{
			return true;
		}
		Shader shader = Shader.Find("Sprites/Default");
		if (!shader)
		{
			return false;
		}
		root = new GameObject("Local8 Shared Visibility");
		UnityEngine.Object.DontDestroyOnLoad(root);
		renderer = root.AddComponent<MeshRenderer>();
		renderer.enabled = false;
		renderer.shadowCastingMode = ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		material = new Material(shader)
		{
			name = "Local8 shared darkness",
			mainTexture = Texture2D.whiteTexture
		};
		renderer.sharedMaterial = material;
		mesh = new Mesh
		{
			name = "Local8 visibility holes"
		};
		mesh.MarkDynamic();
		root.AddComponent<MeshFilter>().sharedMesh = mesh;
		vertices = new Vector3[1421];
		colors = new Color[vertices.Length];
		Vector2[] uv = new Vector2[vertices.Length];
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
		if ((bool)renderer)
		{
			renderer.enabled = false;
		}
	}

	public void Dispose()
	{
		if ((bool)root)
		{
			UnityEngine.Object.Destroy(root);
		}
		if ((bool)mesh)
		{
			UnityEngine.Object.Destroy(mesh);
		}
		if ((bool)material)
		{
			UnityEngine.Object.Destroy(material);
		}
		root = null;
		mesh = null;
		renderer = null;
		material = null;
		built = false;
	}
}
