using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Unit/UnitConfig")]
public class UnitConfig : ScriptableObject
{
    public string unitId;
    public string unitName;
    public Occupation occupation;
    public Faction defaultFaction = Faction.Enemy;
    public Sprite icon;
    public GameObject unitPrefab;

    [Header("坐标偏移（编辑器调整用）")]
    public float yOffset;
    public float zOffset = -0.3f;
    public float xRotation = 45;

    public List<AttributeInitData> initialAttributes = new();
    public List<Buff> innateBuffs;
}
