using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TestTextListener : MonoBehaviour
{
    public TextMeshProUGUI testTMP;
    // Start is called before the first frame update
    void Start()
    {
        GameEventChannel.Register<GameStartEvent>(OnGameStartText);
        GameEventChannel.Register<PhaseChangedEvent>(OnTurnPhaseChangedText);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnGameStartText(GameStartEvent evt)
    {
        testTMP.text = "游戏开始";
        Debug.Log("游戏开始");
    }

    public void OnTurnPhaseChangedText(PhaseChangedEvent evt)
    {
        int turnNumber = evt.turnNumber;
        TurnPhase oldPhase = evt.oldPhase;
        TurnPhase newPhase = evt.newPhase;
        testTMP.text = $"第{turnNumber}轮 阶段从{oldPhase}切换到{newPhase}";
        Debug.Log($"第{turnNumber}轮，阶段从{oldPhase}切换到{newPhase}");
    }

    void OnDestroy()
    {
        GameEventChannel.Unregister<GameStartEvent>(OnGameStartText);
        GameEventChannel.Unregister<PhaseChangedEvent>(OnTurnPhaseChangedText);
    }
}
