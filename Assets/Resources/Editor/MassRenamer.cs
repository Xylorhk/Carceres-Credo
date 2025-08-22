using UnityEngine;
using UnityEditor;
using System.Linq;

public class MassRenamer : EditorWindow
{
    private string baseName = "Object";

    [MenuItem("Tools/Mass Renamer")]
    public static void ShowWindow()
    {
        GetWindow<MassRenamer>("Mass Renamer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Mass Rename Selected Objects", EditorStyles.boldLabel);

        baseName = EditorGUILayout.TextField("Base Name", baseName);

        if (GUILayout.Button("Rename"))
        {
            RenameSelectedObjects();
        }
    }

    private void RenameSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select objects in the Hierarchy to rename.", "OK");
            return;
        }

        // Sort by hierarchy order
        selectedObjects = selectedObjects
            .OrderBy(obj => obj.transform.GetSiblingIndex())
            .ToArray();

        for (int i = 0; i < selectedObjects.Length; i++)
        {
            string newName = $"{baseName} {i + 1:00}";
            Undo.RecordObject(selectedObjects[i], "Rename Objects");
            selectedObjects[i].name = newName;
            EditorUtility.SetDirty(selectedObjects[i]);
        }
    }
}
