using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableObjects/VersionData", menuName = "VersionDataSO")]
public class VersionDataSO : ScriptableObject
{
    public string BundleVersion;
    public int BundleVersionCode;
}
