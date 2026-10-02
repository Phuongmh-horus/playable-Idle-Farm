using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DevicesTestConfigSO", menuName = "ScriptableObjects/DevicesTestConfigSO", order = 1)]
public class DevicesTestConfigSO : ScriptableObject
{
    public List<string> AdIds = new ();
    public List<string> DeviceIds = new ();
}
