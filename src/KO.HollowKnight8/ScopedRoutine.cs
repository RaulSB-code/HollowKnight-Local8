using System;
using System.Collections;
using UnityEngine;

namespace KO.HollowKnight8;

internal sealed class ScopedRoutine : IEnumerator, IDisposable
{
	private readonly IEnumerator routine;

	private readonly PlayerSlot player;

	private readonly bool enemy;

	public object Current
	{
		get
		{
			if (routine.Current is IEnumerator r)
			{
				return new ScopedRoutine(player, r, enemy);
			}
			return routine.Current;
		}
	}

	internal ScopedRoutine(PlayerSlot p, IEnumerator r, bool targetingEnemy = false)
	{
		player = p;
		routine = r;
		enemy = targetingEnemy;
	}

	public bool MoveNext()
	{
		if (player == null || !Object.op_Implicit((Object)(object)player.Hero) || player.Retiring)
		{
			return false;
		}
		using (PlayerContext.Enter(player, enemy))
		{
			return routine.MoveNext();
		}
	}

	public void Reset()
	{
		throw new NotSupportedException();
	}

	public void Dispose()
	{
		using (PlayerContext.Enter(player, enemy))
		{
			if (routine is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}
	}
}
