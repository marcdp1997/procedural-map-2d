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
        ExpandMapUntilLimit();
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

    private void ExpandMapUntilLimit()
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
        List<Module> moduleCandidates = new(isLastConnection ? _startEndPrefabs : _normalPrefabs);
        Shuffle(moduleCandidates);

        foreach (Module prefab in moduleCandidates)
        {
            if (IsModuleGoodCandidate(prefab, targetDoor, openDoorCount, isLastConnection))
                break;
        }
    }

    private bool IsModuleGoodCandidate(Module prefab, Door targetDoor, int openDoorCount, bool isLastConnection)
    {
        List<Door> compatibleDoors = prefab.GetCompatibleDoors(targetDoor.Side);
        Shuffle(compatibleDoors);

        foreach (Door prefabDoor in compatibleDoors)
        {
            Vector3 position = targetDoor.transform.position - prefabDoor.transform.localPosition;

            if (!CanPlaceModule(prefab, position, openDoorCount, isLastConnection))
                continue;

            CreateAndConnect(prefab, prefabDoor, position);
            targetDoor.IsConnected = true;

            if (!CanAllOpenDoorsExpand())
            {
                RollbackModule();
                targetDoor.IsConnected = false;
                continue;
            }

            return true;
        }

        return false;
    }

    private Module CreateAndConnect(Module prefab, Door prefabDoor, Vector3 position)
    {
        Module newModule = CreateModule(prefab, position);
        int doorIndex = prefab.Doors.IndexOf(prefabDoor);
        newModule.Doors[doorIndex].IsConnected = true;
        return newModule;
    }

    private void RollbackModule()
    {
        int index = _spawnedModules.Count - 1;
        Module moduleToRemove = _spawnedModules[index];
        _spawnedModules.RemoveAt(index);
        Destroy(moduleToRemove.gameObject);
    }

    private bool CanAllOpenDoorsExpand()
    {
        Module lastModuleAdded = _spawnedModules[^1];
        List<(Door, Door)> newCommunications = new();

        foreach (Door door in GetOpenDoors())
        {
            Vector3 worldPos = door.transform.position;

            if (!CanDoorExpand(door, worldPos))
            {
                Door sharedDoor = lastModuleAdded.Doors.Find(x => x != door && x.transform.position == worldPos);

                if (sharedDoor != null) newCommunications.Add((door, sharedDoor));
                else return false;
            }
        }

        foreach ((Door a, Door b) in newCommunications)
        {
            a.IsConnected = true;
            b.IsConnected = true;
        }

        return true;
    }

    private bool CanDoorExpand(Door door, Vector3 doorWorldPosition)
    {
        Vector3 expansionDir = door.Side switch
        {
            DoorSide.Left => Vector3.left,
            DoorSide.Right => Vector3.right,
            DoorSide.Top => Vector3.up,
            DoorSide.Bottom => Vector3.down,
            _ => Vector3.zero
        };

        Vector2 size = Vector2.one;
        Vector2 candidatePos = doorWorldPosition + expansionDir * 0.5f;
        return HasSpace(candidatePos, size * 0.05f);
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
