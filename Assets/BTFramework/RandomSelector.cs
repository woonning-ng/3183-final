using System.Collections.Generic;
using UnityEngine;

// the random selector has memory
public class RandomSelector : BTNode
{
    private List<BTNode> children;
    private BTNode runningChild = null; // this is needed as we ran into an issue whereby
                                        // the currently running child was getting overwritten
                                        // so the random selector kept flipping between its children
                                        // very rapidly. this way the node knows which child is currently
                                        // running, if any, and will not attempt to overwrite it

    public RandomSelector(params BTNode[] nodes)
    {
        children = new List<BTNode>(nodes);
    }

    public override NodeResult Tick()
    {
        // if something was already running, stick with it
        if (runningChild != null)
        {
            var result = runningChild.Tick();
            if (result == NodeResult.Running)
                return NodeResult.Running;

            runningChild = null; // resolved — fall through to pick a new one next
            if (result == NodeResult.Success)
                return NodeResult.Success;
            // if Failure, continue on to try a fresh random pick this same tick
        }

        var shuffled = new List<BTNode>(children);
        Shuffle(shuffled);

        foreach (var child in shuffled)
        {
            var result = child.Tick();
            if (result == NodeResult.Running)
            {
                runningChild = child; // commit to it going forward
                return NodeResult.Running;
            }
            if (result == NodeResult.Success)
                return NodeResult.Success;
            // Failure -> try next in shuffled order
        }

        return NodeResult.Failure;
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