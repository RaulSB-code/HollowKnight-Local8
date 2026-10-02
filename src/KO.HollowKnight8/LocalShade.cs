using System;
using System.Collections;
using UnityEngine;

namespace KO.HollowKnight8;

public sealed class LocalShade : MonoBehaviour
{
	internal int OwnerIndex;

	internal int Health;

	internal int Soul;

	internal int Fireball;

	internal int Quake;

	internal int Scream;

	internal string Scene;

	internal string Zone;

	internal float SafeUntil;

	private bool defeated;

	private HealthManager health;

	private int remaining;

	private void OnEnable()
	{
		SafeUntil = Time.time + 0.8f;
	}

	private void LateUpdate()
	{
		if (defeated)
		{
			return;
		}
		if (!Object.op_Implicit((Object)(object)health))
		{
			health = ((Component)this).GetComponent<HealthManager>();
			remaining = Health;
		}
		if (Object.op_Implicit((Object)(object)health))
		{
			remaining = Math.Min(remaining, health.hp);
			if (health.hp > remaining)
			{
				health.hp = remaining;
			}
		}
	}

	internal void Defeat()
	{
		if (!defeated)
		{
			defeated = true;
			HealthManager component = ((Component)this).GetComponent<HealthManager>();
			if (Object.op_Implicit((Object)(object)component))
			{
				component.hp = 0;
				component.isDead = true;
			}
			Collider2D[] componentsInChildren = ((Component)this).GetComponentsInChildren<Collider2D>(true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				((Behaviour)componentsInChildren[i]).enabled = false;
			}
			DamageHero[] componentsInChildren2 = ((Component)this).GetComponentsInChildren<DamageHero>(true);
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				componentsInChildren2[i].damageDealt = 0;
			}
			PlayMakerFSM[] componentsInChildren3 = ((Component)this).GetComponentsInChildren<PlayMakerFSM>(true);
			for (int i = 0; i < componentsInChildren3.Length; i++)
			{
				((Behaviour)componentsInChildren3[i]).enabled = false;
			}
			Rigidbody2D component2 = ((Component)this).GetComponent<Rigidbody2D>();
			if (Object.op_Implicit((Object)(object)component2))
			{
				component2.velocity = Vector2.zero;
			}
			Diagnostics.Write("SHADE defeated P" + (OwnerIndex + 1));
			((MonoBehaviour)this).StartCoroutine(Fade());
		}
	}

	private IEnumerator Fade()
	{
		tk2dSprite[] sprites = ((Component)this).GetComponentsInChildren<tk2dSprite>(true);
		for (float t = 0f; t < 0.25f; t += Time.deltaTime)
		{
			tk2dSprite[] array = sprites;
			foreach (tk2dSprite val in array)
			{
				if (Object.op_Implicit((Object)(object)val))
				{
					Color color = ((tk2dBaseSprite)val).color;
					color.a = 1f - t / 0.25f;
					((tk2dBaseSprite)val).color = color;
				}
			}
			yield return null;
		}
		((Component)this).gameObject.SetActive(false);
		Object.Destroy((Object)(object)((Component)this).gameObject);
	}
}
