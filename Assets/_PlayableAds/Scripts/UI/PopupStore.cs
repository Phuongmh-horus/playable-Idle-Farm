using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupStore : MonoBehaviour
{
    public void OpenPopupStore()
    {
        gameObject.SetActive(true);
    }
    public void ClosePopupStore()
    {
        gameObject.SetActive(false);
    }
}
