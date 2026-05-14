using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 返回施法者的所有友方单位（忽略锚点）
/// 注意：需要场景中存在 LevelManager 并提供 GetAlliesOf 方法
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/AllAllies")]
public class AllAlliesSelector : TargetSelector
{
    public override List<ITarget> GetTargets(EffectContext context)
    {
        var casterUnit = context.caster?.GetComponent<Unit>();
        if (casterUnit == null) return new List<ITarget>();

        if (LevelManager.Instance != null)
        {
            var allies = LevelManager.Instance.GetAlliesOf(casterUnit);
            return allies.Select(a => new UnitTarget(a)).Cast<ITarget>().ToList();
        }

        return new List<ITarget>();
    }
}
