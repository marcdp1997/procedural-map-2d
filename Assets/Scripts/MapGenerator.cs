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
    }

    private void CreateStartModule()
    {
        Module randomPrefab = _startEndPrefabs[Random.Range(0, _startEndPrefabs.Count)];
        CreateModule(randomPrefab, _startPosition);
    }

    private Module CreateModule(Module prefab, Vector3 position)
    {
        Module newModule = Instantiate(prefab, position, Quaternion.identity, _mapParent);
        _spawnedModules.Add(newModule);
        return newModule;
    }

    private void ExpandMap()
    {
        int iterations = 0;
        while (iterations < _maxModules)
        {
            List<Door> openDoors = GetOpenDoors();
            int totalModules = _spawnedModules.Count + openDoors.Count;

            if (totalModules > _maxModules) break;

            foreach (Door door in openDoors)
                TryAttachModuleToDoor(door);

            iterations++;
        }
    }

    private void TryAttachModuleToDoor(Door targetDoor)
    {
        int openDoorCount = GetOpenDoors().Count;
        bool isLastConnection = IsLastConnection(openDoorCount);
        List<Module> moduleChoices = new(isLastConnection ? _startEndPrefabs : _normalPrefabs);
        Shuffle(moduleChoices);

        foreach (Module prefab in moduleChoices)
        {
            List<Door> compatibleDoors = prefab.GetCompatibleDoors(targetDoor.Side);
            Shuffle(compatibleDoors);

            foreach (Door prefabDoor in compatibleDoors)
            {
                int doorIndex = prefab.Doors.IndexOf(prefabDoor);
                Vector3 position = targetDoor.transform.position - prefabDoor.transform.localPosition;
                
                if (CanPlaceModule(prefab, position, openDoorCount, isLastConnection))
                {
                    Module newModule = CreateModule(prefab, position);
                    newModule.Doors[doorIndex].IsConnected = true;
                    targetDoor.IsConnected = true;
                    return;
                }
            }
        }
    }

    private bool IsLastConnection(int numUnconnectedDoors)
    {
        return _spawnedModules.Count == _maxModules - 1 && numUnconnectedDoors == 1;
    }

    private bool CanPlaceModule(Module prefab, Vector3 position, int openDoorCount, bool isLastConnection)
    {
        int moduleDoorCount = prefab.GetUnconnectedDoors().Count;
        bool exceedsLimit = WillExceedModuleLimit(moduleDoorCount, openDoorCount);
        bool blockExpansion = WillBlockFurtherExpansion(moduleDoorCount, openDoorCount) && !isLastConnection;
        bool hasSpace = HasSpace(position, prefab.Collider.size - Vector2.one * 0.05f);

        return !exceedsLimit && !blockExpansion && hasSpace;
    }

    private bool WillExceedModuleLimit(int moduleDoorCount, int openDoorCount)
    {
        return _spawnedModules.Count + openDoorCount + moduleDoorCount - 1 > _maxModules;
    }

    private bool WillBlockFurtherExpansion(int moduleDoorCount, int openDoorCount)
    {
        return _spawnedModules.Count + openDoorCount + moduleDoorCount - 1 < _maxModules && moduleDoorCount == 1;
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


    private List<Door> GetOpenDoors()
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
