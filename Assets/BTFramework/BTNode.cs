using UnityEngine;

public enum NodeResult { Success, Failure, Running }

public abstract class BTNode
{
    public abstract NodeResult Tick();
}

