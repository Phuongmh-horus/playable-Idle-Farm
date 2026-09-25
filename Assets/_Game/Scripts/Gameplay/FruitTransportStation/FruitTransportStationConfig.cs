using UnityEngine;

[CreateAssetMenu(fileName = "FruitTransportStationConfig", menuName = "Farm/FruitTransportStationConfig")]
public class FruitTransportStationConfig : ScriptableObject
{
    public float DepartureDuration = 0.5f;
    public float ReturnDuration = 0.5f;
    public Vector3 CapacityPopupOffset = new Vector3(0, 1.8f, 0);
}
