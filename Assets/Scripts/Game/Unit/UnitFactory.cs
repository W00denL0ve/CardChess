using UnityEngine;

/// <summary>
/// 单位工厂 — 负责将 UnitConfig 实例化为场景中的 Unit
/// </summary>
public static class UnitFactory
{
    private static GameObject unitPrefab;

    /// <summary>在游戏启动时设置 Unit 预制体引用</summary>
    public static void SetUnitPrefab(GameObject prefab) => unitPrefab = prefab;

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

        // 从 Resources 加载默认预制体（若未通过 SetUnitPrefab 设置）
        if (unitPrefab == null)
            unitPrefab = Resources.Load<GameObject>("Prefabs/Unit");

        if (unitPrefab == null)
        {
            Logger.LogError("UnitFactory.Spawn: 未找到 Unit 预制体");
            return null;
        }

        Vector3 worldPos = GridManager.Instance != null
            ? GridManager.Instance.GridToWorld(pos)
            : new Vector3(pos.x, 0, pos.y);

        GameObject go = Object.Instantiate(unitPrefab, worldPos, Quaternion.identity);
        Unit unit = go.GetComponent<Unit>();

        if (unit == null)
        {
            Logger.LogError("UnitFactory.Spawn: 预制体上未找到 Unit 组件");
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

    /// <summary>销毁并反注册单位</summary>
    public static void Despawn(Unit unit)
    {
        if (unit == null) return;
        LevelManager.Instance?.UnregisterUnit(unit);
        Object.Destroy(unit.gameObject);
    }
}
