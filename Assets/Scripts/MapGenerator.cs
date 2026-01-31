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
    private readonly List<Door> _openDoors = new();
    private readonly List<Door> _moduleConnections = new();

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

        _openDoors.Clear();
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
        ConnectModule(newModule);
        _spawnedModules.Add(newModule);
        return newModule;
    }

    private void GenerateMap()
    {
        _iterationCount = 0;
        DestroyCurrentMap();
        CreateRandomStartModule();

        while (_iterationCount < _maxModules && _openDoors.Count > 0)
        {
            _iterationCount++;

            for (int i = 0; i < _openDoors.Count; i++)
                TryAttachModuleToDoor(_openDoors[i]);
        }
    }

    private void TryAttachModuleToDoor(Door targetDoor)
    {
        bool isLastConnection = IsLastConnection();
        List<Module> candidates = new(isLastConnection ? _startEndPrefabs : _normalPrefabs);
        Shuffle(candidates);

        foreach (Module prefab in candidates)
            if (TryPlaceModule(prefab, targetDoor, isLastConnection))
                break;
    }

    private bool IsLastConnection()
    {
        return _spawnedModules.Count == _maxModules - 1 && _openDoors.Count == 1;
    }

    private bool TryPlaceModule(Module prefab, Door targetDoor, bool isLastConnection)
    {
        List<Door> compatibleDoors = prefab.GetCompatibleDoors(targetDoor.Side);
        Shuffle(compatibleDoors);

        foreach (Door prefabDoor in compatibleDoors)
        {
            // Check if new module has physical space
            Vector3 position = targetDoor.transform.position - prefabDoor.transform.localPosition;
            Vector2 size = prefab.Collider.size;
            if (!HasSpace(position, size)) continue;

            // Create module and connections
            Module newModule = CreateModule(prefab, position);

            // Rollback if module does not fit expectations
            if (ShouldRollback(isLastConnection))
            {
                RollbackModule(newModule);
                continue;
            }

            return true;
        }

        return false;
    }

    private bool ShouldRollback(bool isLastConnection)
    {
        // Exceeds the modules limit
        if (_spawnedModules.Count + _openDoors.Count > _maxModules)
            return true;

        // Prevents the map from expanding
        if (!isLastConnection && _openDoors.Count == 0)
            return true;

        // Block other open doors
        return !CanAllOpenDoorsExpand();
    }

    private void ConnectModule(Module newModule)
    {
        _moduleConnections.Clear();

        foreach (Door moduleDoor in newModule.Doors)
        {
            if (moduleDoor.IsEntranceExit) continue;

            Door matchedOpenDoor = null;

            foreach (Door otherDoor in _openDoors)
            {
                if (moduleDoor != otherDoor && 
                   (moduleDoor.transform.position - otherDoor.transform.position).sqrMagnitude < 0.01f)
                {
                    matchedOpenDoor = otherDoor;
                    break;
                }
            }

            if (matchedOpenDoor != null)
            {
                moduleDoor.IsConnected = true;
                matchedOpenDoor.IsConnected = true;
                _openDoors.Remove(matchedOpenDoor);
                _moduleConnections.Add(matchedOpenDoor);
            }
            else _openDoors.Add(moduleDoor);
        }
    }

    private void RollbackModule(Module module)
    {
        // Remove from open doors the module doors that aren't connected
        foreach (Door moduleDoor in module.Doors)
            if (!moduleDoor.IsEntranceExit && !moduleDoor.IsConnected)
                _openDoors.Remove(moduleDoor);

        // Add back to open doors the modules that are connected to this module
        foreach (Door otherDoor in _moduleConnections)
        {
            otherDoor.IsConnected = false;
            _openDoors.Add(otherDoor);
        }

        // Remove module
        _spawnedModules.Remove(module);
        DestroyImmediate(module.gameObject);
    }

    private bool CanAllOpenDoorsExpand()
    {
        // Guarantee at least that all open doors have enough space to connect with a 1x1 module
        foreach (Door door in _openDoors)
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

    private bool HasSpace(Vector3 position, Vector2 size)
    {
        foreach (Module spawnedModule in _spawnedModules)
        {
            Vector2 otherPos = spawnedModule.transform.position;
            Vector2 otherSize = spawnedModule.Collider.size;

            float halfX = size.x * 0.5f;
            float halfY = size.y * 0.5f;
            float otherHalfX = otherSize.x * 0.5f;
            float otherHalfY = otherSize.y * 0.5f;

            if (position.x - halfX < otherPos.x + otherHalfX &&
                position.x + halfX > otherPos.x - otherHalfX &&
                position.y - halfY < otherPos.y + otherHalfY &&
                position.y + halfY > otherPos.y - otherHalfY)
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