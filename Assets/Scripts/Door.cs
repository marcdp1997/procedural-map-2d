using System;
using UnityEngine;

[Serializable]
public class Door : MonoBehaviour
{
    [SerializeField] private bool _isEntranceExit;

    private DoorSide _side;

    public void Initialize(Vector3 moduleSize)
    {
        SetSide(moduleSize);
    }

    private void SetSide(Vector3 moduleSize)
    {
        float halfX = moduleSize.x * 0.5f;
        float halfY = moduleSize.y * 0.5f;
        Vector3 pos = transform.localPosition;

        if (pos.x == halfX) _side = DoorSide.Right;
        else if (pos.x == -halfX) _side = DoorSide.Left;
        else if (pos.y == halfY) _side = DoorSide.Top;
        else if (pos.y == -halfY) _side = DoorSide.Bottom;
    }
}
