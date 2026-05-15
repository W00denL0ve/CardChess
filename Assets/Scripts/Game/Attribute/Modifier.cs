public enum ModifierType
{
    Add,
    Multiply,
    FinalAdd,
    FinalMultiply
}

[System.Serializable]
public class Modifier
{
    public object source;
    public float value;
    public ModifierType type;

    public Modifier(object source, float value, ModifierType type)
    {
        this.source = source;
        this.value = value;
        this.type = type;
    }
}

