
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDataConfig", menuName = "PLA/LevelDataConfig")]
public class LevelDataConfigSO : ScriptableObject
{


    public Vector3 CameraPosition = new Vector3(0, 120, -34);
    public Vector3 CameraRotation = new Vector3(75, 0, 0);

    public Vector3 rotateEntryPoral = new Vector3(0, 0, 0);

    public Vector3 rotateExitPoral = new Vector3(0, 0, 0);
}
