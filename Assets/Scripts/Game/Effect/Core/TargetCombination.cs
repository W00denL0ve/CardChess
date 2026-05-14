/// <summary>
/// 目标组合模式：定义 selectorA 和 selectorB 的结果如何配对
/// </summary>
public enum TargetCombination
{
    /// <summary>笛卡尔积：对每个 a∈A, 每个 b∈B 执行一次 ApplyToPair</summary>
    CrossProduct,

    /// <summary>一一对应：A[i] 与 B[i] 配对（要求数量相等，取较小值）</summary>
    Zip,

    /// <summary>只取 A 的第一个元素，与所有 B 配对</summary>
    FirstOfA_AllB,

    /// <summary>所有 A 与 B 的第一个元素配对</summary>
    AllA_FirstOfB,
}
