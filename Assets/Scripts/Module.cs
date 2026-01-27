using System.Collections.Generic;
using UnityEngine;

public class Module : MonoBehaviour
{
    [SerializeField] private BoxCollider2D _collider;
    [SerializeField] private List<Door> _doors;

    public List<Door> Doors => _doors;
    public BoxCollider2D Collider => _collider;

    public List<Door> GetAvailableDoors(DoorSide doorToConnect)
    {
        List<Door> sideDoors = new();
        DoorSide oppositeSide = GetOppositeSide(doorToConnect);

        foreach (Door door in _doors)
        {
            if (!door.IsEntranceExit && !door.IsConnected && door.Side == oppositeSide)
                sideDoors.Add(door);
        }

        return sideDoors;
    }

    public List<Door> GetUnconnectedDoors()
    {
        List<Door> unconnectedDoors = new();

        foreach (Door door in _doors)
        {
            if (!door.IsEntranceExit && !door.IsConnected)
                unconnectedDoors.Add(door);
        }

        return unconnectedDoors;
    }

    public bool HasEntranceExit()
    {
        foreach (Door door in _doors)
        {
            if (door.IsEntranceExit)
                return true;
        }

        return false;
    }

    private DoorSide GetOppositeSide(DoorSide side)
    {
        return side switch
        {
            DoorSide.Left => DoorSide.Right,
            DoorSide.Right => DoorSide.Left,
            DoorSide.Top => DoorSide.Bottom,
            DoorSide.Bottom => DoorSide.Top,
            _ => default
        };
    }
}
