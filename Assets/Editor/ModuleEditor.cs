using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Module))]
public class ModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Module module = (Module)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Auto Setup Module"))
            AutoSetup(module);
    }

    private void AutoSetup(Module module)
    {
        // Assign collider
        if (!module.TryGetComponent<BoxCollider2D>(out var collider))
            collider = module.gameObject.AddComponent<BoxCollider2D>();

        SerializedObject so = new(module);
        so.FindProperty("_collider").objectReferenceValue = collider;

        // Assign doors
        Door[] doors = module.GetComponentsInChildren<Door>();

        SerializedProperty doorsList = so.FindProperty("_doors");
        doorsList.arraySize = doors.Length;

        for (int i = 0; i < doors.Length; i++)
        {
            Door door = doors[i];
            doorsList.GetArrayElementAtIndex(i).objectReferenceValue = door;

            // Setup doors
            SetDoorSide(door);
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(module);
    }

    private void SetDoorSide(Door door)
    {
        Vector3 localPos = door.transform.localPosition;

        if (Mathf.Abs(localPos.x) > Mathf.Abs(localPos.y))
            SetPrivateSide(door, localPos.x > 0 ? DoorSide.Right : DoorSide.Left);
        else
            SetPrivateSide(door, localPos.y > 0 ? DoorSide.Top : DoorSide.Bottom);

        EditorUtility.SetDirty(door);
    }

    private void SetPrivateSide(Door door, DoorSide side)
    {
        SerializedObject so = new(door);
        so.FindProperty("_side").enumValueIndex = (int)side;
        so.ApplyModifiedProperties();
    }
}