/// <summary>
/// 回合阶段状态接口，定义了每个阶段需要实现的基本属性和方法
/// </summary>
public interface ITurnState : IState
{
    public TurnPhase phaseName { get; }
}