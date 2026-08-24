
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDataConfig", menuName = "PLA/LevelDataConfig")]
public class LevelDataConfigSO : ScriptableObject
{
    public int levelData;
    public Sprite mapImageSprite;
    public Vector3 mapImagePosition;
    public Vector3 mapImageScale;
    public Sprite railImageSprite;
    public Vector3 railImagePosition;
    public Vector3 railImageScale;

    public Vector3 CameraPosition = new Vector3(0, 120, -34);
    public Vector3 CameraRotation = new Vector3(75, 0, 0);

    public Vector3 rotateEntryPoral = new Vector3(0, 0, 0);

    public Vector3 rotateExitPoral = new Vector3(0, 0, 0);
}
