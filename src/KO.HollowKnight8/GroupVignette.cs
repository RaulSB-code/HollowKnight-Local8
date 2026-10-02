using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KO.HollowKnight8;

internal sealed class GroupVignette : IDisposable
{
	private GameObject root;

	private Mesh mesh;

	private MeshRenderer renderer;

	private Material material;

	private Material sourceMaterial;

	private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();

	private Sprite sprite;

	private Vector2[] spriteVertices;

	private Vector2[] spriteUV;

	private ushort[] spriteTriangles;

	private readonly List<Vector3> vertices = new List<Vector3>(1024);

	private readonly List<Vector2> uvs = new List<Vector2>(1024);

	private readonly List<Color> colors = new List<Color>(1024);

	private readonly List<int> triangles = new List<int>(1536);

	private readonly LightVertex[] clipA = new LightVertex[24];

	private readonly LightVertex[] clipB = new LightVertex[24];

	private readonly Vector2[] centres = (Vector2[])(object)new Vector2[8];

	private readonly Vector2[] oldCentres = (Vector2[])(object)new Vector2[8];

	private readonly LightPoint[] blendCentres = new LightPoint[8];

	private readonly double[] blendScratch = new double[8];

	private float viewWidth;

	private float viewHeight;

	private int oldCount;

	private bool built;

	private bool lastFlipX;

	private bool lastFlipY;

	private Matrix4x4 previousProjection;

	private Matrix4x4 previousCamera;

	private Matrix4x4 previousSource;

	private Color previousTint;

	private Vector2 previousPrimary;

	private SpriteRenderer hidden;

	private bool wasEnabled;

	private Camera renderingCamera;

	internal string Status = "unprepared";

	internal bool Render(Camera camera, Camera gameCamera, CoopSession session, SpriteRenderer source, bool light)
	{
		Restore();
		if (!Object.op_Implicit((Object)(object)source) || !Object.op_Implicit((Object)(object)source.sprite) || !Object.op_Implicit((Object)(object)((Renderer)source).sharedMaterial))
		{
			Status = "missing asset";
			return false;
		}
		if ((camera.cullingMask & (1 << ((Component)source).gameObject.layer)) == 0)
		{
			Status = "other camera layer=" + ((Component)source).gameObject.layer;
			return false;
		}
		if (!((Component)source).gameObject.activeInHierarchy || source.color.a < 0.001f)
		{
			Status = "inactive/transparent";
			return false;
		}
		bool value = ((Renderer)source).enabled;
		PlayerSlot primary = session.Primary;
		if (!value && (primary.Down || primary.Hazard))
		{
			primary.RendererStates.TryGetValue((Renderer)(object)source, out value);
		}
		if (!value)
		{
			Status = "disabled";
			return false;
		}
		Bounds bounds = ((Renderer)source).bounds;
		float num = Vector3.Dot(((Bounds)(ref bounds)).center - ((Component)camera).transform.position, ((Component)camera).transform.forward);
		if (num <= camera.nearClipPlane || num >= camera.farClipPlane)
		{
			Status = "outside depth=" + num + " near=" + camera.nearClipPlane;
			return false;
		}
		Prepare(source);
		int num2 = 0;
		Vector2 val = Vector2.op_Implicit(gameCamera.WorldToViewportPoint(((Component)primary.Hero).transform.position));
		foreach (PlayerSlot player in session.Players)
		{
			if (!player.Alive || !player.Ready || TransitionVote.Holding(player) || num2 >= 8)
			{
				continue;
			}
			Vector2 val2 = Vector2.op_Implicit(gameCamera.WorldToViewportPoint(((Component)player.Hero).transform.position));
			bool flag = false;
			for (int i = 0; i < num2; i++)
			{
				Vector2 val3 = centres[i] - val2;
				if (((Vector2)(ref val3)).sqrMagnitude < 0f)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				centres[num2++] = val2;
			}
		}
		if (num2 == 0)
		{
			Status = "no living players";
			return false;
		}
		Matrix4x4 projectionMatrix = camera.projectionMatrix;
		Matrix4x4 localToWorldMatrix = ((Component)camera).transform.localToWorldMatrix;
		Matrix4x4 localToWorldMatrix2 = ((Component)source).transform.localToWorldMatrix;
		bool flag2 = !built || oldCount != num2 || projectionMatrix != previousProjection || localToWorldMatrix != previousCamera || localToWorldMatrix2 != previousSource || source.color != previousTint || source.flipX != lastFlipX || source.flipY != lastFlipY || val != previousPrimary;
		for (int j = 0; j < num2; j++)
		{
			if (flag2)
			{
				break;
			}
			if (centres[j] != oldCentres[j])
			{
				flag2 = true;
			}
		}
		if (flag2)
		{
			vertices.Clear();
			uvs.Clear();
			colors.Clear();
			triangles.Clear();
			float num3 = Vector3.Dot(((Component)primary.Hero).transform.position - ((Component)gameCamera).transform.position, ((Component)gameCamera).transform.forward);
			Vector3 val4 = gameCamera.ViewportToWorldPoint(new Vector3(0f, 0f, num3));
			Vector3 val5 = gameCamera.ViewportToWorldPoint(new Vector3(1f, 1f, num3));
			viewWidth = Mathf.Abs(val5.x - val4.x);
			viewHeight = Mathf.Abs(val5.y - val4.y);
			for (int k = 0; k < num2; k++)
			{
				blendCentres[k] = new LightPoint(centres[k].x * viewWidth, centres[k].y * viewHeight);
			}
			for (int l = 0; l < num2; l++)
			{
				Vector2 val6 = centres[l] - val;
				for (int m = 0; m < spriteTriangles.Length; m += 3)
				{
					for (int n = 0; n < 3; n++)
					{
						int num4 = spriteTriangles[m + n];
						Vector2 val7 = spriteVertices[num4];
						if (source.flipX)
						{
							val7.x = 0f - val7.x;
						}
						if (source.flipY)
						{
							val7.y = 0f - val7.y;
						}
						Vector3 val8 = camera.WorldToViewportPoint(((Component)source).transform.TransformPoint(Vector2.op_Implicit(val7)));
						val8.x += val6.x;
						val8.y += val6.y;
						clipA[n] = new LightVertex(val8.x, val8.y, val8.z, spriteUV[num4].x, spriteUV[num4].y);
					}
					LightVertex[] a = clipA;
					LightVertex[] b = clipB;
					int num5 = 3;
					if (num5 > 0)
					{
						num5 = Clip(a, num5, b, Vector2.right, 1.01f);
						Swap(ref a, ref b);
					}
					if (num5 > 0)
					{
						num5 = Clip(a, num5, b, Vector2.left, 0.01f);
						Swap(ref a, ref b);
					}
					if (num5 > 0)
					{
						num5 = Clip(a, num5, b, Vector2.up, 1.01f);
						Swap(ref a, ref b);
					}
					if (num5 > 0)
					{
						num5 = Clip(a, num5, b, Vector2.down, 0.01f);
						Swap(ref a, ref b);
					}
					if (num5 >= 3)
					{
						for (int num6 = 1; num6 < num5 - 1; num6++)
						{
							BlendTriangle(camera, a[0], a[num6], a[num6 + 1], source.color, l, num2);
						}
					}
				}
			}
			mesh.Clear();
			mesh.SetVertices(vertices);
			mesh.SetUVs(0, uvs);
			mesh.SetColors(colors);
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateBounds();
			previousProjection = projectionMatrix;
			previousCamera = localToWorldMatrix;
			previousSource = localToWorldMatrix2;
			previousTint = source.color;
			previousPrimary = val;
			oldCount = num2;
			lastFlipX = source.flipX;
			lastFlipY = source.flipY;
			built = true;
			Array.Copy(centres, oldCentres, num2);
		}
		if ((Object)(object)sourceMaterial != (Object)(object)((Renderer)source).sharedMaterial)
		{
			CopyMaterial(((Renderer)source).sharedMaterial);
		}
		((Renderer)source).GetPropertyBlock(properties);
		properties.SetTexture("_MainTex", (Texture)(object)source.sprite.texture);
		properties.SetColor("_RendererColor", Color.white);
		properties.SetVector("_Flip", new Vector4(1f, 1f, 1f, 1f));
		((Renderer)renderer).SetPropertyBlock(properties);
		root.layer = ((Component)source).gameObject.layer;
		((Renderer)renderer).sortingLayerID = ((Renderer)source).sortingLayerID;
		((Renderer)renderer).sortingOrder = ((Renderer)source).sortingOrder;
		hidden = source;
		wasEnabled = ((Renderer)source).enabled;
		renderingCamera = camera;
		((Renderer)source).enabled = false;
		((Renderer)renderer).enabled = triangles.Count > 0;
		Status = "smooth_glows=" + num2 + " triangles=" + triangles.Count / 3;
		return true;
	}

	private void BlendTriangle(Camera camera, LightVertex a, LightVertex b, LightVertex c, Color tint, int player, int count)
	{
		int count2 = vertices.Count;
		Vector3 val = camera.ViewportToWorldPoint(new Vector3(a.X, a.Y, a.Z));
		Vector3 val2 = camera.ViewportToWorldPoint(new Vector3(b.X, b.Y, b.Z));
		Vector3 val3 = camera.ViewportToWorldPoint(new Vector3(c.X, c.Y, c.Z));
		bool clockwise = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X) < 0f;
		for (int i = 0; i <= 6; i++)
		{
			for (int j = 0; j <= 6 - i; j++)
			{
				float num = (float)j / 6f;
				float num2 = (float)i / 6f;
				float num3 = 1f - num - num2;
				float num4 = a.X * num3 + b.X * num + c.X * num2;
				float num5 = a.Y * num3 + b.Y * num + c.Y * num2;
				vertices.Add(val * num3 + val2 * num + val3 * num2);
				uvs.Add(new Vector2(a.U * num3 + b.U * num + c.U * num2, a.V * num3 + b.V * num + c.V * num2));
				Color item = tint;
				item.a *= (float)LightBlendRules.Weight(blendCentres, count, player, num4 * viewWidth, num5 * viewHeight, 3.5, blendScratch);
				colors.Add(item);
			}
		}
		for (int k = 0; k < 6; k++)
		{
			int num6 = count2 + k * 7 - k * (k - 1) / 2;
			int num7 = num6 + 6 - k + 1;
			for (int l = 0; l < 6 - k; l++)
			{
				AddTriangle(num6 + l, num6 + l + 1, num7 + l, clockwise);
				if (l < 6 - k - 1)
				{
					AddTriangle(num6 + l + 1, num7 + l + 1, num7 + l, clockwise);
				}
			}
		}
	}

	private void AddTriangle(int a, int b, int c, bool clockwise)
	{
		triangles.Add(a);
		triangles.Add(clockwise ? b : c);
		triangles.Add(clockwise ? c : b);
	}

	private static void Swap(ref LightVertex[] a, ref LightVertex[] b)
	{
		LightVertex[] array = a;
		a = b;
		b = array;
	}

	private static int Clip(LightVertex[] input, int count, LightVertex[] output, Vector2 normal, float limit)
	{
		return LightGeometry.Clip(input, count, output, normal.x, normal.y, limit);
	}

	private void Prepare(SpriteRenderer source)
	{
		if ((Object)(object)sprite != (Object)(object)source.sprite)
		{
			sprite = source.sprite;
			spriteVertices = sprite.vertices;
			spriteUV = sprite.uv;
			spriteTriangles = sprite.triangles;
			built = false;
		}
		if (!Object.op_Implicit((Object)(object)root))
		{
			root = new GameObject("Local8 Group Visibility");
			Object.DontDestroyOnLoad((Object)(object)root);
			renderer = root.AddComponent<MeshRenderer>();
			((Renderer)renderer).enabled = false;
			((Renderer)renderer).shadowCastingMode = (ShadowCastingMode)0;
			((Renderer)renderer).receiveShadows = false;
			mesh = new Mesh
			{
				name = "Local8 blended light atlas",
				indexFormat = (IndexFormat)1
			};
			mesh.MarkDynamic();
			root.AddComponent<MeshFilter>().sharedMesh = mesh;
			CopyMaterial(((Renderer)source).sharedMaterial);
		}
	}

	private void CopyMaterial(Material source)
	{
		if (Object.op_Implicit((Object)(object)material))
		{
			Object.Destroy((Object)(object)material);
		}
		material = new Material(source)
		{
			name = "Local8 Group Light"
		};
		sourceMaterial = source;
		((Renderer)renderer).sharedMaterial = material;
	}

	internal void Finish(Camera camera)
	{
		if ((Object)(object)camera == (Object)(object)renderingCamera)
		{
			Restore();
		}
	}

	internal void Restore()
	{
		if (Object.op_Implicit((Object)(object)hidden))
		{
			((Renderer)hidden).enabled = wasEnabled;
		}
		hidden = null;
		renderingCamera = null;
		if (Object.op_Implicit((Object)(object)renderer))
		{
			((Renderer)renderer).enabled = false;
		}
	}

	public void Dispose()
	{
		Restore();
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
	}
}
