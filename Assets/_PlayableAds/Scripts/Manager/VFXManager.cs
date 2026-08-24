using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoSingleton<VFXManager>
{
    [SerializeField]
    private List<GameObject> vfxWinGames;

    void OnEnable()
    {
        GameEventBus.OnWinGame += PlayWinGameVFX;
    }
    void OnDisable()
    {
        GameEventBus.OnWinGame -= PlayWinGameVFX;
    }

    public void PlayWinGameVFX()
    {
        Debug.Log("--------Playing win game VFX--------");
        foreach (GameObject vfxWinGame in vfxWinGames)
        {
            if (vfxWinGame != null)
            {           
                foreach (Transform child in vfxWinGame.transform)
                {
                    child.GetComponent<ParticleSystem>().Play();
                    // Debug.Log($"Playing win game VFX: {child.name}");
                }
            }
        }
    }




}
