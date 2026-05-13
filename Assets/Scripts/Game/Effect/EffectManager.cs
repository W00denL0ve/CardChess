using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ExecuteCardEffects(CardData card)
    {
        //todo
    }

    public void ExecuteEffect(Effect effect, CardData card)
    {
        //todo
    }
}
