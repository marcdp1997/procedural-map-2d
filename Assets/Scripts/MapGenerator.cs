using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private int _numModules = 15;
    [SerializeField] private ModuleLibrary _library;
    [SerializeField] private Vector3 _startPosition;
    [SerializeField] private Transform _mapParent;

    private readonly List<Module> _createdModules = new();
    private readonly List<Module> _notConnectedModules = new();

    private void Start()
    {
        GenerateMap();
    }

    private void GenerateMap()
    {
        // Create start module
        List<Module> choices = new(_library.StartEndModules);
        Shuffle(choices);

        Module startModule = CreateModule(choices[0], _startPosition);

        // Recursive expansion
        ExpandModule(startModule);
    }

    private void ExpandModule(Module module)
    {
        if (_createdModules.Count >= _numModules)
            return;

        foreach (Door door in module.GetUnconnectedDoors())
        {
            if (_createdModules.Count >= _numModules)
                return;

            Module newModule = TryConnectDoor(door);

            if (newModule != null)
            {
                ExpandModule(newModule);
            }
        }
    }

    private Module TryConnectDoor(Door doorToConnect)
    {
        List<Module> moduleChoices = new(_library.NormalModules);
        Shuffle(moduleChoices);

        foreach (Module prefab in moduleChoices)
        {
            List<Door> doorChoices = prefab.GetAvailableDoors(doorToConnect.Side);
            Shuffle(doorChoices);

            foreach (Door door in doorChoices)
            {
                Vector3 position = doorToConnect.transform.position - door.transform.localPosition;

                if (HasSpace(position, prefab.Collider.size - Vector2.one * 0.01f))
                {
                    Module newModule = CreateModule(prefab, position);
                    door.IsConnected = true;
                    doorToConnect.IsConnected = true;
                    return newModule;
                }
            }
        }

        return null;
    }

    private Module CreateModule(Module prefab, Vector3 position)
    {
        Module newModule = Instantiate(prefab, position, Quaternion.identity, _mapParent);
        _createdModules.Add(newModule);
        _notConnectedModules.Add(newModule);
        return newModule;
    }

    private bool HasSpace(Vector2 position, Vector3 size)
    {
        Bounds bounds = new()
        {
            center = position,
            size = size
        };

        foreach (Module module in _createdModules)
        {
            if (bounds.Intersects(module.Collider.bounds))
                return false;
        }

        return true;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
