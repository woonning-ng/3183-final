using UnityEngine;

public class Sequence : BTNode
{
    private readonly BTNode[] children;
    public Sequence(params BTNode[] children) => this.children = children;

    public override NodeResult Tick()
    {
        foreach (var child in children)
        {
            NodeResult result = child.Tick();
            if (result != NodeResult.Success)
                return result; // Failure or Running stops the search
        }
        return NodeResult.Success;
    }
}
