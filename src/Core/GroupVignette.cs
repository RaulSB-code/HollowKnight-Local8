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

	private readonly Vector2[] centres = new Vector2[8];

	private readonly Vector2[] oldCentres = new Vector2[8];

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
		if (!source || !source.sprite || !source.sharedMaterial)
		{
			Status = "missing asset";
			return false;
		}
		if ((camera.cullingMask & (1 << source.gameObject.layer)) == 0)
		{
			Status = "other camera layer=" + source.gameObject.layer;
			return false;
		}
		if (!source.gameObject.activeInHierarchy || source.color.a < 0.001f)
		{
			Status = "inactive/transparent";
			return false;
		}
		bool value = source.enabled;
		PlayerSlot primary = session.Primary;
		if (!value && (primary.Down || primary.Hazard))
		{
			primary.RendererStates.TryGetValue(source, out value);
		}
		if (!value)
		{
			Status = "disabled";
			return false;
		}
		float num = Vector3.Dot(source.bounds.center - camera.transform.position, camera.transform.forward);
		if (num <= camera.nearClipPlane || num >= camera.farClipPlane)
		{
			Status = "outside depth=" + num + " near=" + camera.nearClipPlane;
			return false;
		}
		Prepare(source);
		int num2 = 0;
		Vector2 vector = gameCamera.WorldToViewportPoint(primary.Hero.transform.position);
		foreach (PlayerSlot player in session.Players)
		{
			if (!player.Alive || !player.Ready || TransitionVote.Holding(player) || num2 >= 8)
			{
				continue;
			}
			Vector2 vector2 = gameCamera.WorldToViewportPoint(player.Hero.transform.position);
			bool flag = false;
			for (int i = 0; i < num2; i++)
			{
				if ((centres[i] - vector2).sqrMagnitude < 0f)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				centres[num2++] = vector2;
			}
		}
		if (num2 == 0)
		{
			Status = "no living players";
			return false;
		}
		Matrix4x4 projectionMatrix = camera.projectionMatrix;
		Matrix4x4 localToWorldMatrix = camera.transform.localToWorldMatrix;
		Matrix4x4 localToWorldMatrix2 = source.transform.localToWorldMatrix;
		bool flag2 = !built || oldCount != num2 || projectionMatrix != previousProjection || localToWorldMatrix != previousCamera || localToWorldMatrix2 != previousSource || source.color != previousTint || source.flipX != lastFlipX || source.flipY != lastFlipY || vector != previousPrimary;
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
			float z = Vector3.Dot(primary.Hero.transform.position - gameCamera.transform.position, gameCamera.transform.forward);
			Vector3 vector3 = gameCamera.ViewportToWorldPoint(new Vector3(0f, 0f, z));
			Vector3 vector4 = gameCamera.ViewportToWorldPoint(new Vector3(1f, 1f, z));
			viewWidth = Mathf.Abs(vector4.x - vector3.x);
			viewHeight = Mathf.Abs(vector4.y - vector3.y);
			for (int k = 0; k < num2; k++)
			{
				blendCentres[k] = new LightPoint(centres[k].x * viewWidth, centres[k].y * viewHeight);
			}
			for (int l = 0; l < num2; l++)
			{
				Vector2 vector5 = centres[l] - vector;
				for (int m = 0; m < spriteTriangles.Length; m += 3)
				{
					for (int n = 0; n < 3; n++)
					{
						int num3 = spriteTriangles[m + n];
						Vector2 vector6 = spriteVertices[num3];
						if (source.flipX)
						{
							vector6.x = 0f - vector6.x;
						}
						if (source.flipY)
						{
							vector6.y = 0f - vector6.y;
						}
						Vector3 vector7 = camera.WorldToViewportPoint(source.transform.TransformPoint(vector6));
						vector7.x += vector5.x;
						vector7.y += vector5.y;
						clipA[n] = new LightVertex(vector7.x, vector7.y, vector7.z, spriteUV[num3].x, spriteUV[num3].y);
					}
					LightVertex[] a = clipA;
					LightVertex[] b = clipB;
					int num4 = 3;
					if (num4 > 0)
					{
						num4 = Clip(a, num4, b, Vector2.right, 1.01f);
						Swap(ref a, ref b);
					}
					if (num4 > 0)
					{
						num4 = Clip(a, num4, b, Vector2.left, 0.01f);
						Swap(ref a, ref b);
					}
					if (num4 > 0)
					{
						num4 = Clip(a, num4, b, Vector2.up, 1.01f);
						Swap(ref a, ref b);
					}
					if (num4 > 0)
					{
						num4 = Clip(a, num4, b, Vector2.down, 0.01f);
						Swap(ref a, ref b);
					}
					if (num4 >= 3)
					{
						for (int num5 = 1; num5 < num4 - 1; num5++)
						{
							BlendTriangle(camera, a[0], a[num5], a[num5 + 1], source.color, l, num2);
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
			previousPrimary = vector;
			oldCount = num2;
			lastFlipX = source.flipX;
			lastFlipY = source.flipY;
			built = true;
			Array.Copy(centres, oldCentres, num2);
		}
		if (sourceMaterial != source.sharedMaterial)
		{
			CopyMaterial(source.sharedMaterial);
		}
		source.GetPropertyBlock(properties);
		properties.SetTexture("_MainTex", source.sprite.texture);
		properties.SetColor("_RendererColor", Color.white);
		properties.SetVector("_Flip", new Vector4(1f, 1f, 1f, 1f));
		renderer.SetPropertyBlock(properties);
		root.layer = source.gameObject.layer;
		renderer.sortingLayerID = source.sortingLayerID;
		renderer.sortingOrder = source.sortingOrder;
		hidden = source;
		wasEnabled = source.enabled;
		renderingCamera = camera;
		source.enabled = false;
		renderer.enabled = triangles.Count > 0;
		Status = "smooth_glows=" + num2 + " triangles=" + triangles.Count / 3;
		return true;
	}

	private void BlendTriangle(Camera camera, LightVertex a, LightVertex b, LightVertex c, Color tint, int player, int count)
	{
		int count2 = vertices.Count;
		Vector3 vector = camera.ViewportToWorldPoint(new Vector3(a.X, a.Y, a.Z));
		Vector3 vector2 = camera.ViewportToWorldPoint(new Vector3(b.X, b.Y, b.Z));
		Vector3 vector3 = camera.ViewportToWorldPoint(new Vector3(c.X, c.Y, c.Z));
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
				vertices.Add(vector * num3 + vector2 * num + vector3 * num2);
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
		if (sprite != source.sprite)
		{
			sprite = source.sprite;
			spriteVertices = sprite.vertices;
			spriteUV = sprite.uv;
			spriteTriangles = sprite.triangles;
			built = false;
		}
		if (!root)
		{
			root = new GameObject("Local8 Group Visibility");
			UnityEngine.Object.DontDestroyOnLoad(root);
			renderer = root.AddComponent<MeshRenderer>();
			renderer.enabled = false;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			mesh = new Mesh
			{
				name = "Local8 blended light atlas",
				indexFormat = IndexFormat.UInt32
			};
			mesh.MarkDynamic();
			root.AddComponent<MeshFilter>().sharedMesh = mesh;
			CopyMaterial(source.sharedMaterial);
		}
	}

	private void CopyMaterial(Material source)
	{
		if ((bool)material)
		{
			UnityEngine.Object.Destroy(material);
		}
		material = new Material(source)
		{
			name = "Local8 Group Light"
		};
		sourceMaterial = source;
		renderer.sharedMaterial = material;
	}

	internal void Finish(Camera camera)
	{
		if (camera == renderingCamera)
		{
			Restore();
		}
	}

	internal void Restore()
	{
		if ((bool)hidden)
		{
			hidden.enabled = wasEnabled;
		}
		hidden = null;
		renderingCamera = null;
		if ((bool)renderer)
		{
			renderer.enabled = false;
		}
	}

	public void Dispose()
	{
		Restore();
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
	}
}
