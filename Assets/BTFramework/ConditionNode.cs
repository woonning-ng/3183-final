using System;

public class ConditionNode : BTNode
{
    private readonly Func<bool> condition;
    public ConditionNode(Func<bool> condition) => this.condition = condition;
    public override NodeResult Tick() => condition() ? NodeResult.Success : NodeResult.Failure;
}