using System;
using UnityEngine;

public class ActionNode : BTNode
{
    private readonly Func<NodeResult> action;
    public ActionNode(Func<NodeResult> action) => this.action = action;
    public override NodeResult Tick() => action();
}