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
        CreateRandomStartModule();
        ExpandMapUntilLimit();
    }

    private void CreateRandomStartModule()
    {
        List<Module> candidates = new(_startEndPrefabs);
        Shuffle(candidates);
        CreateModule(candidates[0], _startPosition);
    }

    private Module CreateModule(Module prefab, Vector3 position)
    {
        Module newModule = Instantiate(prefab, position, Quaternion.identity, _mapParent);
        _spawnedModules.Add(newModule);
        return newModule;
    }

    private void ExpandMapUntilLimit()
    {
        for (int i = 0; i < _maxModules; i++)
        {
            List<Door> openDoors = GetOpenDoors();

            foreach (Door door in openDoors)
            {
                TryAttachModuleToDoor(door, openDoors.Count);
            }
        }
    }

    private void TryAttachModuleToDoor(Door targetDoor, int openDoorCount)
    {
        bool isLastConnection = IsLastConnection(openDoorCount);
        List<Module> candidates = new(isLastConnection ? _startEndPrefabs : _normalPrefabs);
        Shuffle(candidates);

        foreach (Module prefab in candidates)
        {
            if (TryPlaceModule(prefab, targetDoor, isLastConnection))
                return;
        }
    }

    private bool IsLastConnection(int numOpenDoors)
    {
        return _spawnedModules.Count == _maxModules - 1 && numOpenDoors == 1;
    }

    private bool TryPlaceModule(Module prefab, Door targetDoor, bool isLastConnection)
    {
        List<Door> compatibleDoors = prefab.GetCompatibleDoors(targetDoor.Side);
        Shuffle(compatibleDoors);

        foreach (Door prefabDoor in compatibleDoors)
        {
            // Check if new module has physical space
            Vector3 position = targetDoor.transform.position - prefabDoor.transform.localPosition;
            if (!HasSpace(position, prefab.Collider.size))
                continue;

            // Create module
            Module newModule = CreateModule(prefab, position);
            List<(Door, Door)> connections = ConnectModule(newModule);
            List<Door> openDoors = GetOpenDoors();

            // Rollback if module does not fit expectations
            if (ShouldRollback(openDoors, isLastConnection))
            {
                RollbackModule(newModule, connections);
                continue;
            }

            return true;
        }

        return false;
    }

    private bool ShouldRollback(List<Door> openDoors, bool isLastConnection)
    {
        // Exceeds the modules limit
        if (_spawnedModules.Count + openDoors.Count > _maxModules)
            return true;

        // Prevents the map from expanding
        if (!isLastConnection && openDoors.Count == 0)
            return true;

        // Block other open doors
        return !CanAllOpenDoorsExpand(openDoors);
    }

    private List<(Door, Door)> ConnectModule(Module newModule)
    {
        List<(Door, Door)> newConnections = new();

        foreach (Door door in GetOpenDoors())
        {
            Vector3 worldPos = door.transform.position;
            Door sharedDoor = newModule.Doors.Find(x => x != door && x.transform.position == worldPos);

            if (sharedDoor != null)
            {
                sharedDoor.IsConnected = true;
                door.IsConnected = true;
                newConnections.Add((sharedDoor, door));
            }
        }

        return newConnections;
    }

    private void RollbackModule(Module module, List<(Door, Door)> connections)
    {
        foreach ((Door a, Door b) in connections)
        {
            a.IsConnected = false;
            b.IsConnected = false;
        }

        _spawnedModules.Remove(module);
        Destroy(module.gameObject);
    }

    private bool CanAllOpenDoorsExpand(List<Door> openDoors)
    {
        // Guarantee that at least all open doors can connect with a 1x1 module
        foreach (Door door in openDoors)
        {
            Vector3 expansionDir = door.Side switch
            {
                DoorSide.Left => Vector3.left,
                DoorSide.Right => Vector3.right,
                DoorSide.Top => Vector3.up,
                DoorSide.Bottom => Vector3.down,
                _ => Vector3.zero
            };

            Vector2 candidatePos = door.transform.position + expansionDir * 0.5f;

            if (!HasSpace(candidatePos, Vector2.one)) 
                return false;
        }

        return true;
    }

    private bool HasSpace(Vector3 position, Vector3 size)
    {
        Bounds bounds = new()
        {
            center = position,
            size = size * 0.95f
        };

        foreach (Module module in _spawnedModules)
            if (bounds.Intersects(module.Collider.bounds))
                return false;

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
            foreach (Door door in module.GetUnconnectedDoors())
                totalUnconnectedDoors.Add(door);

        return totalUnconnectedDoors;
    }
}