using UnityEngine;

namespace LbsMap;

/// <summary>
/// 替代原 IL 中显式调用的 UnityEngine.Object.op_Implicit（C# 不允许显式调用转换运算符）。
/// 语义与 Unity 的隐式 bool 转换一致：对象是否存活/非 null。
/// </summary>
internal static class UnityNull
{
	public static bool IsAlive(UnityEngine.Object value)
	{
		return value != null;
	}
}
