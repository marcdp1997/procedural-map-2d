using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ModuleLibrary", menuName = "Module Library")]
public class ModuleLibrary : ScriptableObject
{
    public List<Module> StartEndModules;
    public List<Module> NormalModules;
}
