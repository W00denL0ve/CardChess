using System.Collections.Generic;
using UnityEngine;

public abstract class Effect : ScriptableObject
{
    public EffectContext effectContext;
    public abstract void Execute(EffectContext context);
}