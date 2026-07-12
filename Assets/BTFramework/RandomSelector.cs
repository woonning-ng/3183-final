using System.Collections.Generic;
using UnityEngine;

public class RandomSelector : BTNode
{
    private List<BTNode> children;

    public RandomSelector(params BTNode[] nodes)
    {
        children = new List<BTNode>(nodes);
    }

    public override NodeResult Tick()
    {
        var shuffled = new List<BTNode>(children);
        Shuffle(shuffled);

        foreach (var child in shuffled)
        {
            var result = child.Tick();
            if (result != NodeResult.Failure)
                return result; // Running or Success — stop here
        }

        return NodeResult.Failure; // everything failed
    }

    private void Shuffle(List<BTNode> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}