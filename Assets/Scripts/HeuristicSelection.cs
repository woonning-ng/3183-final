using TMPro;
using UnityEngine;

public class HeuristicSelection : MonoBehaviour
{
    //the class is used for the dropdown UI in the game to select the heuristic mode
    [Header("Heuristic Dropdown")]
    [SerializeField] private TMP_Dropdown heuristicDrop;

    public Heuristic heuristicVal = Heuristic.OCTILE;

    public void HeuristicDropDown()
    {
        switch (heuristicDrop.value)
        {
            case 0:

                heuristicVal = Heuristic.OCTILE;
                break;
            case 1:
                heuristicVal = Heuristic.CHEBYSHEV;
                break;
            case 2:
                heuristicVal = Heuristic.MANHATTAN;
                break;
            case 3:
                heuristicVal = Heuristic.EUCLIDEAN;
                break;
        }
    }

    


}
