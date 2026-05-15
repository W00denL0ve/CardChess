using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Unit/UnitConfig")]
public class UnitConfig : ScriptableObject
{
    public string unitId;
    public string unitName;
    public Occupation occupation;
    public Sprite icon;
    public List<AttributeInitData> initialAttributes = new();
    public List<Buff> innateBuffs;
}
