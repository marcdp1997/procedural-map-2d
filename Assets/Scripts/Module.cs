using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Module : MonoBehaviour
{
    private Door[] _doors;
    private BoxCollider2D _collider;

    private void Awake()
    {
        _doors = GetComponentsInChildren<Door>();
        _collider = GetComponent<BoxCollider2D>();

        foreach (Door door in _doors)
            door.Initialize(_collider.bounds.size);
    }
}
