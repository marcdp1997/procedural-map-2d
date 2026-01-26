using System;
using UnityEngine;

[Serializable]
public class Door : MonoBehaviour
{
    [SerializeField] private bool _isEntranceExit;
    [SerializeField] private DoorSide _side;

    private bool _isConnected;

    public DoorSide Side => _side;
    public bool IsEntranceExit => _isEntranceExit;
    public bool IsConnected { get => _isConnected; set => _isConnected = value; }
}
