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
            case SpawnUnitAction spawn:
                ExecuteSpawnUnit(spawn);
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
    //  单位生成
    // ====================================================================

    /// <summary>当前局使用的 RunConfig（由 GameManager 在新局开始时设置）</summary>
    public static RunConfig CurrentRunConfig { get; set; }

    /// <summary>当前关卡在全局 Run 中的索引（由 GameManager 维护）</summary>
    public static int GlobalStageIndex { get; set; }

    /// <summary>
    /// 在指定格子生成单位，支持地形条件检查和占用时回退搜索
    /// </summary>
    private static void ExecuteSpawnUnit(SpawnUnitAction action)
    {
        Faction? factionOverride = action.useConfigFaction ? null : action.factionOverride;

        for (int i = 0; i < action.count; i++)
        {
            // 从 SpawnGroup 随机抽取，兜底到 fallbackUnitConfig
            UnitConfig config = ResolveUnitConfig(action);
            if (config == null)
            {
                Logger.LogWarning("[TurnAction] SpawnUnitAction 无法解析 UnitConfig（spawnGroup 和 fallbackUnitConfig 均为空）");
                return;
            }

            Vector2Int targetPos = FindSpawnPosition(action);
            if (targetPos.x < 0)
            {
                Logger.LogWarning($"[TurnAction] 无法为 {config.unitId} 找到可用出生点");
                return;
            }
            UnitFactory.Spawn(config, targetPos, factionOverride);
        }
    }

    /// <summary>解析实际生成的 UnitConfig</summary>
    private static UnitConfig ResolveUnitConfig(SpawnUnitAction action)
    {
        if (action.spawnGroup != null)
        {
            float difficulty = GetCurrentDifficulty();
            return action.spawnGroup.PickUnit(difficulty, CurrentRunConfig);
        }
        return action.fallbackUnitConfig;
    }

    /// <summary>获取当前综合难度值</summary>
    private static float GetCurrentDifficulty()
    {
        // RunConfig 的 CalculateDifficulty 需要 mapId，当前暂无地图系统，暂回 0
        return CurrentRunConfig?.CalculateDifficulty(GlobalStageIndex, "") ?? 0f;
    }

    /// <summary>
    /// 寻找一个可用的出生格子：符合地形条件且未被占用
    /// </summary>
    private static Vector2Int FindSpawnPosition(SpawnUnitAction action)
    {
        Vector2Int coord = action.coord;
        Cell origin = GridManager.Instance?.GetCell(coord.x, coord.y);

        // 1. 检查原始坐标是否可用
        if (IsCellValidForSpawn(origin, action.allowedTerrains))
            return coord;

        // 2. 原始不可用且 searchRange=0 → 放弃
        if (action.searchRange <= 0)
            return new Vector2Int(-1, -1);

        // 3. 在外层搜索最近的可用格子（曼哈顿半径递增）
        int w = GridManager.Instance.CurrentLevel.width;
        int h = GridManager.Instance.CurrentLevel.height;

        for (int r = 1; r <= action.searchRange; r++)
        {
            // 遍历曼哈顿距离 == r 的所有格子
            for (int dx = -r; dx <= r; dx++)
            {
                int dy = r - Mathf.Abs(dx);
                // dy 和 -dy 是上下对称的两个点
                foreach (int sy in new int[] { dy, -dy })
                {
                    // 当 dy == 0 时，上下对称会重复同一个点，用 continue 跳过
                    if (dy == 0 && sy == -dy) continue;

                    int nx = coord.x + dx;
                    int ny = coord.y + sy;

                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                    Cell candidate = GridManager.Instance.GetCell(nx, ny);
                    if (IsCellValidForSpawn(candidate, action.allowedTerrains))
                        return new Vector2Int(nx, ny);
                }
            }
        }

        return new Vector2Int(-1, -1);
    }

    /// <summary>判断格子是否可以生成单位</summary>
    private static bool IsCellValidForSpawn(Cell cell, List<TerrainType> allowedTerrains)
    {
        if (cell == null) return false;
        if (cell.OccupyingUnit != null) return false;
        if (allowedTerrains != null && allowedTerrains.Count > 0 && !allowedTerrains.Contains(cell.terrainType))
            return false;
        return true;
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
