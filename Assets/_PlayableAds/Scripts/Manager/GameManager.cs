using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoSingleton<GameManager>
{
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this);
    }

}
