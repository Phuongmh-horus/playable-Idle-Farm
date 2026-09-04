using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandCreative : MonoBehaviour
{
    
    [SerializeField] private Sprite normalHandnormalHandSprite;
    [SerializeField] private Sprite clickHandSprite;

    private Image m_handImage;

    private void Awake()
    {
        m_handImage = GetComponent<Image>();
    }
    void Start()
    {
        /// Hide the cursor
        Cursor.visible = false;
    }

    void OnDisable()
    {
        /// Show the cursor
        Cursor.visible = true;
    }
    void OnEnable()
    {
        /// Hide the cursor
        Cursor.visible = false;
    }
    public void SetNormalHandSprite()
    {
        m_handImage.sprite = normalHandnormalHandSprite;
    }
    public void SetClickHandSprite()
    {
        m_handImage.sprite = clickHandSprite;
    }

    protected void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SetClickHandSprite();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            SetNormalHandSprite();
        }
        m_handImage.transform.position = Input.mousePosition;

    }

}
