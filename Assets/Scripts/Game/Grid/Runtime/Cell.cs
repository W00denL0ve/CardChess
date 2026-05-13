using UnityEngine;
using System.Collections.Generic;

public class Cell : MonoBehaviour
{
    public int col;
    public int row;
    public int layer;
    public TerrainType terrainType;
    public int height;
    public bool isWalkable;
    public List<Effect> activeEffects;
    public Character occupyingCharacter;
}