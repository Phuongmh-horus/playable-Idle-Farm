using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StaticSelectableButtonBase : MonoBehaviour
{
    [SerializeField] StaticSelectable StaticSelectable;

    private void OnValidate()
    {
        StaticSelectable = GetComponent<StaticSelectable>();
    }

    private void Awake()
    {
        if (StaticSelectable != null)
        {
            StaticSelectable.OnTapped += OnButtonClicked;
        }
    }

    private void OnDestroy()
    {
        if (StaticSelectable != null)
        {
            StaticSelectable.OnTapped -= OnButtonClicked;
        }
    }

    protected abstract void OnButtonClicked(StaticSelectable selectable);
}
