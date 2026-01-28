using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MapGenerator : MonoBehaviour
{
    private const int MaxAttempts = 5;

    [SerializeField] private ModuleLibrary _moduleLibrary;
    [SerializeField] [Min(2)] private int _maxModules = 15;
    [SerializeField] private bool _useRandomSeed = true;
    [SerializeField] private int _seed;
    [SerializeField] private int _currentSeed;
    [SerializeField] private int _attemptCount;
    [SerializeField] private int _iterationCount;

    private readonly List<Module> _spawnedModules = new();
    private readonly List<Module> _startEndPrefabs = new();
    private readonly List<Module> _normalPrefabs = new();

    public void StartMapGeneration()
    {
        _attemptCount = 0;

        if (_moduleLibrary == null)
        {
            Debug.LogWarning("Can't create map. Please assign a valid Module Library.");
            return;
        }

        CacheModulePrefabs();

        if (_startEndPrefabs.Count == 0)
        {
            Debug.LogWarning("Can't create map. Please add modules with an entrance/exit in the assigned Module Library.");
            return;
        }

        if (_normalPrefabs.Count == 0)
        {
            Debug.LogWarning("Can't create map. Please add modules without an entrance/exit in the assigned Module Library.");
            return;
        }

        GenerateSeed();

        while (_attemptCount < MaxAttempts)
        {
            _attemptCount++;
            Debug.Log($"Generating map with seed {_currentSeed}. Attempt number {_attemptCount}.");
            GenerateMap();

            if (_spawnedModules.Count == _maxModules)
            {
                Debug.Log($"Map generation succeed!");
                return;
            }
        }

        Debug.Log($"Map generation failed.");
    }

    private void GenerateSeed()
    {
        _currentSeed = _useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : _seed;
        Random.InitState(_currentSeed);
    }

    private void CacheModulePrefabs()
    {
        _startEndPrefabs.Clear();
        _normalPrefabs.Clear();

        foreach (Module module in _moduleLibrary.Modules)
        {
            if (module.HasEntranceExit()) _startEndPrefabs.Add(module);
            else _normalPrefabs.Add(module);
        }
    }

    private void DestroyCurrentMap()
    {
        Module[] modules = GetComponentsInChildren<Module>();
        foreach (Module module in modules)
                DestroyImmediate(module.gameObject);

        _spawnedModules.Clear();
    }

    private void CreateRandomStartModule()
    {
        List<Module> candidates = new(_startEndPrefabs);
        Shuffle(candidates);
        CreateModule(candidates[0], Vector3.zero);
    }

    private Module CreateModule(Module prefab, Vector3 position)
    {
        Module newModule = Instantiate(prefab, position, prefab.transform.rotation, transform);
        _spawnedModules.Add(newModule);
        return newModule;
    }

    private void GenerateMap()
    {
        _iterationCount = 0;
        DestroyCurrentMap();
        CreateRandomStartModule();

        List<Door> openDoors = GetOpenDoors();

        while (_iterationCount < _maxModules && openDoors.Count > 0)
        {
            _iterationCount++;

            foreach (Door door in openDoors)
                TryAttachModuleToDoor(door, openDoors.Count);

            openDoors = GetOpenDoors();
        }
    }

    private void TryAttachModuleToDoor(Door targetDoor, int openDoorCount)
    {
        bool isLastConnection = IsLastConnection(openDoorCount);
        List<Module> candidates = new(isLastConnection ? _startEndPrefabs : _normalPrefabs);
        Shuffle(candidates);

        foreach (Module prefab in candidates)
            if (TryPlaceModule(prefab, targetDoor, isLastConnection))
                break;
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

            // Create module and connections
            Module newModule = CreateModule(prefab, position);
            List<(Door, Door)> connections = ConnectModule(newModule);

            // Rollback if module does not fit expectations
            if (ShouldRollback(isLastConnection))
            {
                RollbackModule(newModule, connections);
                continue;
            }

            return true;
        }

        return false;
    }

    private bool ShouldRollback(bool isLastConnection)
    {
        List<Door> openDoors = GetOpenDoors();

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

        foreach (Door openDoor in GetOpenDoors())
        {
            foreach (Door moduleDoor in newModule.Doors)
            {
                if (moduleDoor != openDoor && 
                    moduleDoor.transform.position == openDoor.transform.position)
                {
                    moduleDoor.IsConnected = true;
                    openDoor.IsConnected = true;
                    newConnections.Add((moduleDoor, openDoor));
                }
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
        DestroyImmediate(module.gameObject);
    }

    private bool CanAllOpenDoorsExpand(List<Door> openDoors)
    {
        // Guarantee at least that all open doors can connect with a 1x1 module
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