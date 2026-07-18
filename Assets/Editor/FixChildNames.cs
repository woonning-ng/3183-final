using UnityEditor;
using UnityEngine;

public class FixChildNames : EditorWindow
{
    [MenuItem("Tools/Fix Group Child Names")]
    static void Fix()
    {
        GameObject content = Selection.activeGameObject;
        if (content == null)
        {
            Debug.LogWarning("Select your 'Content' object first.");
            return;
        }

        string[] suffixes = { "Label", "MIN", "MAX", "MAx", "CURR", "Slider" };
        int renamedCount = 0;

        // loop through every group (Hunger, Energy, Safety, etc.)
        foreach (Transform group in content.transform)
        {
            string groupName = group.name; // e.g. "Energy", "SafetyPriority"

            foreach (Transform child in group)
            {
                foreach (string suffix in suffixes)
                {
                    if (child.name.EndsWith(suffix))
                    {
                        string newName = "Human" + groupName + (suffix == "MAx" ? "MAX" : suffix);
                        Undo.RecordObject(child.gameObject, "Fix Child Name");
                        child.gameObject.name = newName;
                        renamedCount++;
                        break; // stop checking other suffixes once matched
                    }
                }
            }
        }

        Debug.Log($"Renamed {renamedCount} children.");
    }
}