using System;
using System.Collections.Generic;
using UnityEngine;

// references:
// https://www.youtube.com/watch?v=mZfyt03LDH4

public class AStarManager : MonoBehaviour {
    private Astar astar;
    private readonly Queue<PathRequest> pending = new Queue<PathRequest>();

    [SerializeField] private int maxRequestsPerFrame = 2;
    // or a time budget: [SerializeField] private float maxMillisecondsPerFrame = 2f;

    private void Awake() => astar = new Astar(Grid.Instance);

    public void RequestPath(PathRequest request, Action<PathResult, List<Vector3>> onComplete)
    {
        // enqueue with callback
    }

    private void Update()
    {
        // dequeue up to maxRequestsPerFrame, call astar.ComputePath,
        // handle PROCESSING by re-enqueueing, COMPLETE/IMPOSSIBLE by firing callback
    }
}
