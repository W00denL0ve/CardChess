using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回合行动执行器 - 负责在运行时执行 LevelTurnData 中定义的各种行动
/// </summary>
public static class TurnActionExecutor
{
    /// <summary>
    /// 批量执行一组行动
    /// </summary>
    public static void ExecuteAll(List<TurnAction> actions)
    {
        foreach (var action in actions)
        {
            if (action == null) continue;
            Execute(action);
        }
    }

    /// <summary>
    /// 根据行动类型分派执行
    /// </summary>
    public static void Execute(TurnAction action)
    {
        switch (action)
        {
            case EnemySpawnAction spawn:
                ExecuteEnemySpawn(spawn);
                break;

            case CellChangeAction cellChange:
                ExecuteCellChange(cellChange);
                break;

            case EffectApplyAction effectApply:
                ExecuteEffectApply(effectApply);
                break;

            default:
                Logger.LogWarning($"[TurnActionExecutor] 未处理的行动类型: {action.GetType().Name}");
                break;
        }
    }

    // ====================================================================
    //  敌人生成
    // ====================================================================

    /// <summary>
    /// 在指定格子生成敌人
    /// </summary>
    private static void ExecuteEnemySpawn(EnemySpawnAction action)
    {
        Vector2Int coord = action.coord;
        Logger.Log($"[TurnAction] 在 ({coord.x},{coord.y}) 生成 {action.spawnCount} 个敌人 (ID: {action.enemyId})");

        // 获取目标格子
        Cell cell = GridManager.Instance?.GetCell(coord.x, coord.y);
        if (cell == null)
        {
            Logger.LogWarning($"[TurnAction] 格子 ({coord.x},{coord.y}) 不存在，跳过敌人生成");
            return;
        }

        // 格子已被占用则不生成
        if (cell.OccupyingUnit != null)
        {
            Logger.LogWarning($"[TurnAction] 格子 ({coord.x},{coord.y}) 已被占用，跳过敌人生成");
            return;
        }

        // TODO: 根据 enemyId 从配置表/Addressables 加载敌人 prefab 并实例化
        // 示例：
        // GameObject enemyPrefab = Resources.Load<GameObject>($"Enemies/{action.enemyId}");
        // for (int i = 0; i < action.spawnCount; i++)
        // {
        //     Vector3 worldPos = GridManager.Instance.GetWorldPosition(coord.x, coord.y);
        //     GameObject go = Object.Instantiate(enemyPrefab, worldPos, Quaternion.identity);
        //     Unit unit = go.GetComponent<Unit>();
        //     if (unit != null)
        //     {
        //         UnitConfig config = Resources.Load<UnitConfig>($"UnitConfigs/{action.enemyId}");
        //         if (config != null)
        //         {
        //             unit.Initialize(config, Faction.Enemy, coord);
        //             LevelManager.Instance.RegisterUnit(unit);
        //             GridManager.Instance.PlaceUnit(unit, coord);
        //         }
        //     }
        // }
    }

    // ====================================================================
    //  格子变化
    // ====================================================================

    /// <summary>
    /// 修改指定格子的地形属性
    /// </summary>
    private static void ExecuteCellChange(CellChangeAction action)
    {
        Vector2Int coord = action.coord;
        Logger.Log($"[TurnAction] 修改格子 ({coord.x},{coord.y})");

        GridManager grid = GridManager.Instance;
        if (grid == null) return;

        // 通过 GridManager.SetCell 的回调来修改，确保触发可视化更新
        grid.SetCell(coord.x, coord.y, 0, (cell) =>
        {
            if (action.newTerrainType.HasValue)
                cell.terrainType = action.newTerrainType.Value;

            if (action.newHeight.HasValue)
                cell.height = action.newHeight.Value;

            if (action.setWalkable.HasValue)
                cell.isWalkable = action.setWalkable.Value;
        });

        Logger.Log($"[TurnAction] 格子 ({coord.x},{coord.y}) 已更新");
    }

    // ====================================================================
    //  效果应用
    // ====================================================================

    /// <summary>
    /// 在指定格子应用 Effect
    /// </summary>
    private static void ExecuteEffectApply(EffectApplyAction action)
    {
        if (action.effectToApply == null)
        {
            Logger.LogWarning("[TurnAction] EffectApplyAction 的 effectToApply 为 null");
            return;
        }

        Vector2Int coord = action.coord;
        Logger.Log($"[TurnAction] 在 ({coord.x},{coord.y}) 应用效果: {action.effectToApply.effectName}");

        // 构建效果上下文，目标格作为被执行者
        EffectContext context = new EffectContext
        {
            executor = new CellTarget(coord),
            executed = new CellTarget(coord)
        };

        action.effectToApply.OnExecute(context);
        action.effectToApply.OnComplete(context);
    }
}
