using System;
using UnityEngine;

namespace KO.HollowKnight8;

public sealed class PvpSpark : MonoBehaviour
{
	private Mesh mesh;

	private Vector3[] vertices = (Vector3[])(object)new Vector3[24];

	private Color[] colors = (Color[])(object)new Color[24];

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
		((Component)this).gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		MeshRenderer obj = ((Component)this).gameObject.AddComponent<MeshRenderer>();
		((Renderer)obj).sharedMaterial = material;
		((Renderer)obj).sortingOrder = 1000;
	}

	private void Update()
	{
		age += Time.deltaTime;
		if (age >= 0.18f)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
			return;
		}
		float num = age / 0.18f;
		Vector3 val = default(Vector3);
		for (int i = 0; i < 8; i++)
		{
			float num2 = (float)i * (float)Math.PI / 4f;
			((Vector3)(ref val))._002Ector(Mathf.Cos(num2), Mathf.Sin(num2));
			Vector3 val2 = new Vector3(0f - val.y, val.x) * 0.04f * (1f - num);
			vertices[i * 3] = val * (0.08f + num * 0.4f) + val2;
			vertices[i * 3 + 1] = val * (0.08f + num * 0.4f) - val2;
			vertices[i * 3 + 2] = val * (0.55f + num * 0.5f);
			Color val3 = tint;
			val3.a = 1f - num;
			colors[i * 3] = (colors[i * 3 + 1] = (colors[i * 3 + 2] = val3));
		}
		mesh.vertices = vertices;
		mesh.colors = colors;
		mesh.RecalculateBounds();
	}

	private void OnDestroy()
	{
		if (Object.op_Implicit((Object)(object)mesh))
		{
			Object.Destroy((Object)(object)mesh);
		}
	}
}
