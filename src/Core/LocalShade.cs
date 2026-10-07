using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KO.HollowKnight8;

public sealed class LocalShade : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class __iterator__Fade_d__15 : IEnumerator<object>, IDisposable, IEnumerator
	{
		private int __iterator___1__state;

		private object __iterator___2__current;

		public LocalShade __iterator___4__this;

		private tk2dSprite[] __iterator__sprites_5__2;

		private float __iterator__t_5__3;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return __iterator___2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return __iterator___2__current;
			}
		}

		[DebuggerHidden]
		public __iterator__Fade_d__15(int __iterator___1__state)
		{
			this.__iterator___1__state = __iterator___1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			__iterator__sprites_5__2 = null;
			__iterator___1__state = -2;
		}

		private bool MoveNext()
		{
			int num = __iterator___1__state;
			LocalShade localShade = __iterator___4__this;
			switch (num)
			{
			default:
				return false;
			case 0:
				__iterator___1__state = -1;
				__iterator__sprites_5__2 = localShade.GetComponentsInChildren<tk2dSprite>(includeInactive: true);
				__iterator__t_5__3 = 0f;
				break;
			case 1:
				__iterator___1__state = -1;
				__iterator__t_5__3 += Time.deltaTime;
				break;
			}
			if (__iterator__t_5__3 < 0.25f)
			{
				tk2dSprite[] array = __iterator__sprites_5__2;
				foreach (tk2dSprite tk2dSprite in array)
				{
					if ((bool)tk2dSprite)
					{
						Color color = tk2dSprite.color;
						color.a = 1f - __iterator__t_5__3 / 0.25f;
						tk2dSprite.color = color;
					}
				}
				__iterator___2__current = null;
				__iterator___1__state = 1;
				return true;
			}
			localShade.gameObject.SetActive(value: false);
			UnityEngine.Object.Destroy(localShade.gameObject);
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

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
		if (!health)
		{
			health = GetComponent<HealthManager>();
			remaining = Health;
		}
		if ((bool)health)
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
			HealthManager component = GetComponent<HealthManager>();
			if ((bool)component)
			{
				component.hp = 0;
				component.isDead = true;
			}
			Collider2D[] componentsInChildren = GetComponentsInChildren<Collider2D>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = false;
			}
			DamageHero[] componentsInChildren2 = GetComponentsInChildren<DamageHero>(includeInactive: true);
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				componentsInChildren2[i].damageDealt = 0;
			}
			PlayMakerFSM[] componentsInChildren3 = GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
			for (int i = 0; i < componentsInChildren3.Length; i++)
			{
				componentsInChildren3[i].enabled = false;
			}
			Rigidbody2D component2 = GetComponent<Rigidbody2D>();
			if ((bool)component2)
			{
				component2.velocity = Vector2.zero;
			}
			Diagnostics.Write("SHADE defeated P" + (OwnerIndex + 1));
			StartCoroutine(Fade());
		}
	}

	[IteratorStateMachine(typeof(__iterator__Fade_d__15))]
	private IEnumerator Fade()
	{
		return new __iterator__Fade_d__15(0)
		{
			__iterator___4__this = this
		};
	}
}
