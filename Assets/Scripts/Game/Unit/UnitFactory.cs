using UnityEngine;

/// <summary>
/// 单位工厂 — 负责将 UnitConfig 实例化为场景中的 Unit
/// </summary>
public static class UnitFactory
{
    /// <summary>
    /// 生成一个单位
    /// </summary>
    public static Unit Spawn(UnitConfig config, Vector2Int pos, Faction? overrideFaction = null)
    {
        if (config == null)
        {
            Logger.LogError("UnitFactory.Spawn: config 为 null");
            return null;
        }

        if (config.unitPrefab == null)
        {
            Logger.LogError($"UnitFactory.Spawn: {config.unitId} 的 unitPrefab 为 null");
            return null;
        }

        Vector3 worldPos = GetWorldPosition(pos, config);
        Quaternion rotation = Quaternion.Euler(config.xRotation, 0f, 0f);

        GameObject go = Object.Instantiate(config.unitPrefab, worldPos, rotation);
        Unit unit = go.GetComponent<Unit>();

        if (unit == null)
        {
            Logger.LogError($"UnitFactory.Spawn: {config.unitId} 的预制体上未找到 Unit 组件");
            Object.Destroy(go);
            return null;
        }

        unit.Initialize(config, pos, overrideFaction);

        if (GridManager.Instance != null)
            GridManager.Instance.PlaceUnit(unit, pos);

        if (LevelManager.Instance != null)
            LevelManager.Instance.RegisterUnit(unit);

        Logger.Log($"[UnitFactory] 生成 {config.unitId} 于 ({pos.x},{pos.y})");
        return unit;
    }

    /// <summary>根据网格坐标和配置计算最终世界坐标（含偏移）</summary>
    public static Vector3 GetWorldPosition(Vector2Int gridPos, UnitConfig config)
    {
        Vector3 basePos = GridManager.Instance != null
            ? GridManager.Instance.GridToWorld(gridPos)
            : new Vector3(gridPos.x, 0, gridPos.y);

        if (config != null)
        {
            basePos.y += config.yOffset;
            basePos.z += config.zOffset;
        }
        return basePos;
    }

    /// <summary>销毁并反注册单位</summary>
    public static void Despawn(Unit unit)
    {
        if (unit == null) return;
        LevelManager.Instance?.UnregisterUnit(unit);
        Object.Destroy(unit.gameObject);
    }
}
