using UnityEngine;

public class RandomSelector : BTNode
{
    private readonly BTNode[] children;
    public RandomSelector(params BTNode[] children) => this.children = children;

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