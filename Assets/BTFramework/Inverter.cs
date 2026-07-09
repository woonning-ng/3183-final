using System;
using UnityEngine;

public class Inverter : BTNode
{
    private readonly Func<bool> condition;
    public Inverter(Func<bool> condition) => this.condition = condition;
    public override NodeResult Tick() => condition() ? NodeResult.Failure : NodeResult.Success;
}
