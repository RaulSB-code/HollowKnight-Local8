using System;
using System.Collections.Generic;
using System.Reflection;

namespace KO.HollowKnight8;

internal static class Reflect
{
	private static readonly Dictionary<string, FieldInfo> Fields = new Dictionary<string, FieldInfo>();

	private static readonly Dictionary<string, MethodInfo> Methods = new Dictionary<string, MethodInfo>();

	internal static FieldInfo Field(Type type, string name)
	{
		string key = type.FullName + ":" + name;
		if (!Fields.TryGetValue(key, out var value))
		{
			value = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			Fields[key] = value;
		}
		return value;
	}

	internal static MethodInfo Method(Type type, string name)
	{
		string key = type.FullName + ":" + name;
		if (!Methods.TryGetValue(key, out var value))
		{
			value = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			Methods[key] = value;
		}
		return value;
	}

	internal static void Set(object obj, string name, object value)
	{
		FieldInfo fieldInfo = Field(obj.GetType(), name);
		if (fieldInfo != null)
		{
			fieldInfo.SetValue(obj, value);
		}
	}

	internal static T Get<T>(object obj, string name, T fallback = default(T))
	{
		if (obj == null)
		{
			return fallback;
		}
		FieldInfo fieldInfo = Field(obj.GetType(), name);
		if (!(fieldInfo == null))
		{
			return (T)fieldInfo.GetValue(obj);
		}
		return fallback;
	}

	internal static object Call(object obj, string name, params object[] args)
	{
		MethodInfo methodInfo = Method(obj.GetType(), name);
		if (methodInfo == null)
		{
			throw new MissingMethodException(obj.GetType().FullName, name);
		}
		return methodInfo.Invoke(obj, args);
	}
}
