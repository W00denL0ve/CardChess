using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 返回施法者的所有敌人（忽略锚点）
/// 注意：需要场景中存在 LevelManager 并提供 GetEnemiesOf 方法
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/AllEnemies")]
public class AllEnemiesSelector : TargetSelector
{
    public override List<ITarget> GetTargets(EffectContext context)
    {
        var casterUnit = context.caster?.GetComponent<Unit>();
        if (casterUnit == null) return new List<ITarget>();

        // 尝试从 LevelManager 获取敌人（如果存在）
        if (LevelManager.Instance != null)
        {
            var enemies = LevelManager.Instance.GetEnemiesOf(casterUnit);
            return enemies.Select(e => new UnitTarget(e)).Cast<ITarget>().ToList();
        }

        return new List<ITarget>();
    }
}
