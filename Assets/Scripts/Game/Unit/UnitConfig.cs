using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardChess/Units/UnitConfig")]
public class UnitConfig : ScriptableObject
{
    public string unitId;
    public string unitName;
    public Occupation occupation;
    public Faction defaultFaction = Faction.Enemy;
    public Sprite icon;
    public GameObject unitPrefab;
    public AIDeck aiDeck;

    [Header("坐标偏移（单独设置）")]
    public float yOffset;
    public float zOffset = -0.3f;
    public float xRotation = 45;

    public List<AttributeInitData> initialAttributes = new();
    public List<Buff> innateBuffs;
}
