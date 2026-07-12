using System.Collections.Generic;
using UnityEngine;

public class Selector : BTNode
{
    private List<BTNode> children;

    public Selector(params BTNode[] nodes)
    {
        children = new List<BTNode>(nodes);
    }

    public override NodeResult Tick()
    {
        foreach (var child in children)
        {
            NodeResult result = child.Tick();
            if (result != NodeResult.Failure)
                return result; // Success or Running stops the search
        }
        return NodeResult.Failure;
    }
}
