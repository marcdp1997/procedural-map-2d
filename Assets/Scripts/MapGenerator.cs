using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private int _maxModules = 15;
    [SerializeField] private ModuleLibrary _moduleLibrary;
    [SerializeField] private Vector3 _startPosition;
    [SerializeField] private Transform _mapParent;

    private readonly List<Module> _spawnedModules = new();
    private readonly List<Module> _startEndPrefabs = new();
    private readonly List<Module> _normalPrefabs = new();

    private void Awake()
    {
        CacheModulePrefabs();
    }

    private void CacheModulePrefabs()
    {
        foreach (Module module in _moduleLibrary.Modules)
        {
            if (module.HasEntranceExit()) _startEndPrefabs.Add(module);
            else _normalPrefabs.Add(module);
        }
    }

    private void Start()
    {
        GenerateMap();
    }

    private void GenerateMap()
    {
        CreateStartModule();
        ExpandMap();
        // Add end module
    }

    private void CreateStartModule()
    {
        Module randomPrefab = _startEndPrefabs[Random.Range(0, _startEndPrefabs.Count)];
        CreateModule(randomPrefab, _startPosition);
    }

    private void ExpandMap()
    {
        int iterations = 0;
        while (iterations < _maxModules)
        {
            List<Door> unconnectedDoors = GetMapUnconnectedDoors();
            int totalModules = _spawnedModules.Count + unconnectedDoors.Count;

            if (totalModules > _maxModules) break;

            foreach (var door in unconnectedDoors)
                TryConnectDoorWithModule(door);

            iterations++;
        }
    }

    private void TryConnectDoorWithModule(Door doorToConnect)
    {
        int numMapUnconnectedDoors = GetMapUnconnectedDoors().Count;
        bool isEndModule = IsEndModule(numMapUnconnectedDoors);
        List<Module> moduleChoices = new(isEndModule ? _startEndPrefabs : _normalPrefabs);
        Shuffle(moduleChoices);

        foreach (Module prefab in moduleChoices)
        {
            List<Door> doorChoices = prefab.GetAvailableDoors(doorToConnect.Side);
            Shuffle(doorChoices);

            foreach (Door door in doorChoices)
            {
                Vector3 position = doorToConnect.transform.position - door.transform.localPosition;
                
                if (!WillExceedMaxModules(prefab.Doors.Count, numMapUnconnectedDoors, isEndModule) && 
                    !WillPreventMapFromExpanding(prefab.Doors.Count, numMapUnconnectedDoors, isEndModule) &&
                    HasSpace(position, prefab.Collider.size - Vector2.one * 0.05f))
                {
                    Module newModule = CreateModule(prefab, position);
                    newModule.Doors.Find(x => x.name == door.name).IsConnected = true;
                    doorToConnect.IsConnected = true;
                    return;
                }
            }
        }
    }

    private bool IsEndModule(int numUnconnectedDoors)
    {
        return _spawnedModules.Count == _maxModules - 1 && numUnconnectedDoors == 1;
    }

    private bool WillExceedMaxModules(int numModuleDoors, int numUnconnectedDoors, bool isEndModule)
    {
        return _spawnedModules.Count + numUnconnectedDoors + numModuleDoors - (isEndModule ? 2 : 1) > _maxModules;
    }

    private bool WillPreventMapFromExpanding(int numModuleDoors, int numUnconnectedDoors, bool isEndModule)
    {
        return _spawnedModules.Count + numUnconnectedDoors + numModuleDoors - 1 < _maxModules &&
            numModuleDoors == 1 &&
            !isEndModule;
    }

    private Module CreateModule(Module prefab, Vector3 position)
    {
        Module newModule = Instantiate(prefab, position, Quaternion.identity, _mapParent);
        _spawnedModules.Add(newModule);
        return newModule;
    }

    private bool HasSpace(Vector3 position, Vector3 size)
    {
        Bounds bounds = new()
        {
            center = position,
            size = size
        };

        foreach (Module module in _spawnedModules)
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


    private List<Door> GetMapUnconnectedDoors()
    {
        List<Door> totalUnconnectedDoors = new();

        foreach (Module module in _spawnedModules)
        {
            foreach (Door door in module.GetUnconnectedDoors())
                totalUnconnectedDoors.Add(door);
        }

        return totalUnconnectedDoors;
    }
}
