using System.Collections.Generic;

/// <summary>
/// 静态工具类，实现对一个属性值的所有修饰器计算
/// </summary>
public static class AttributeCulculator
{
    /// <summary>
    /// 工具方法：计算经过修饰器修饰的最终值
    /// </summary>
    /// <param name="base_or">施方基础值</param>
    /// <param name="modifier_or">调用者传入正确的施方修饰器</param>
    /// <param name="base_ed">受方基础值</param>
    /// <param name="modifier_ed">调用者传入正确的受方修饰器</param>
    /// <returns></returns>
    public static int CulculateFinalValue(float base_or, List<Modifier> modifier_or, float base_ed = 0, List<Modifier> modifier_ed = null)
    {
        float addSum1 = base_or;
        float addSum2 = base_ed;
        float multiplySum1 = 1;
        float multiplySum2 = 1;
        float finalMultiplySum = 1;
        float finalAddSum = 0;
        if (modifier_or != null)
        {
            foreach (var mod in modifier_or)
            {
                switch (mod.type)
                {
                    case ModifierType.Add:
                        addSum1 += mod.value;
                        break;
                    case ModifierType.Multiply:
                        multiplySum1 *= mod.value;
                        break;
                    case ModifierType.FinalAdd:
                        finalAddSum += mod.value;
                        break;
                    case ModifierType.FinalMultiply:
                        finalMultiplySum *= mod.value;
                        break;
                }
            }
        }
        if (modifier_ed != null)
        {
            foreach (var mod in modifier_ed)
            {
                switch (mod.type)
                {
                    case ModifierType.Add:
                        addSum2 += mod.value;
                        break;
                    case ModifierType.Multiply:
                        multiplySum2 *= mod.value;
                        break;
                    case ModifierType.FinalAdd:
                        finalAddSum += mod.value;
                        break;
                    case ModifierType.FinalMultiply:
                        finalMultiplySum *= mod.value;
                        break;
                }
            }
        }
        // 计算公式：（基础值1加算值的和 * 乘算值1的积 - 基础值2加算值的和 * 乘算值2的积）* 最终乘算的积 + 最终加算的和
        return (int)((addSum1 * multiplySum1 - addSum2 * multiplySum2) * finalMultiplySum + finalAddSum);
    }
}