// Custom Node + BT framework based on course materials and research (links below)
// https://www.academia.edu/94885456/Behavior_Trees_for_Modelling_Artificial_Intelligence_in_Games_A_Tutorial
// https://ktmarine1999.medium.com/behavior-trees-in-unity-20a738b5508c
// https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter06_The_Behavior_Tree_Starter_Kit.pdf
// https://www.researchgate.net/publication/319463746_Behavior_Trees_in_Robotics_and_AI_An_Introduction - existence of Running
// https://arxiv.org/pdf/2106.15227 - why there is only Tick() - Stateless/Asynchronous nodes

// something something inline state changes during evaluation, allowing for easier debugging
// therefore only tick(), no onstart() or onend()

using UnityEngine;

// Three results: Success - action has been completed successfully
//                Failure - action was not completed successfully
//                Running - action still in progress, let it resume execution for this frame
public enum NodeResult { Success, Failure, Running }

public abstract class BTNode
{
    // Tick/Update()
    public abstract NodeResult Tick();
}

