using System;
using UnityEngine;

namespace KO.HollowKnight8;

public sealed class PvpSpark : MonoBehaviour
{
	private Mesh mesh;

	private Vector3[] vertices = new Vector3[24];

	private Color[] colors = new Color[24];

	private Color tint;

	private float age;

	internal void Init(Material material, Color color)
	{
		tint = color;
		mesh = new Mesh();
		int[] array = new int[24];
		for (int i = 0; i < 24; i++)
		{
			array[i] = i;
		}
		mesh.vertices = vertices;
		mesh.triangles = array;
		base.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		MeshRenderer meshRenderer = base.gameObject.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = material;
		meshRenderer.sortingOrder = 1000;
	}

	private void Update()
	{
		age += Time.deltaTime;
		if (age >= 0.18f)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		float num = age / 0.18f;
		for (int i = 0; i < 8; i++)
		{
			float f = (float)i * (float)Math.PI / 4f;
			Vector3 vector = new Vector3(Mathf.Cos(f), Mathf.Sin(f));
			Vector3 vector2 = new Vector3(0f - vector.y, vector.x) * 0.04f * (1f - num);
			vertices[i * 3] = vector * (0.08f + num * 0.4f) + vector2;
			vertices[i * 3 + 1] = vector * (0.08f + num * 0.4f) - vector2;
			vertices[i * 3 + 2] = vector * (0.55f + num * 0.5f);
			Color color = tint;
			color.a = 1f - num;
			colors[i * 3] = (colors[i * 3 + 1] = (colors[i * 3 + 2] = color));
		}
		mesh.vertices = vertices;
		mesh.colors = colors;
		mesh.RecalculateBounds();
	}

	private void OnDestroy()
	{
		if ((bool)mesh)
		{
			UnityEngine.Object.Destroy(mesh);
		}
	}
}
