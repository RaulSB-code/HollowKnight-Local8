using Modding;

namespace KO.HollowKnight8;

internal sealed class CompatLog
{
	internal void LogInfo(object value)
	{
		if (Local8Mod.Instance != null)
		{
			((Loggable)Local8Mod.Instance).Log(value);
		}
	}

	internal void LogError(object value)
	{
		if (Local8Mod.Instance != null)
		{
			((Loggable)Local8Mod.Instance).LogError(value);
		}
	}

	internal void LogWarning(object value)
	{
		if (Local8Mod.Instance != null)
		{
			((Loggable)Local8Mod.Instance).LogWarn(value);
		}
	}
}
