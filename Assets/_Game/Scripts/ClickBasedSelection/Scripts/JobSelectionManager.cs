using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Small runtime selection service for the playable. Selectables register when enabled,
/// then a tap is tested against their authored bounds. This deliberately avoids Jobs,
/// NativeCollections and scene-wide Find calls: the playable only has a small number of
/// interactive plots and upgrade controls.
/// </summary>
public class JobSelectionManager : MonoSingleton<JobSelectionManager>
{
    [SerializeField] private StaticSelectionBaker baker;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float dragThreshold = 20f;

    public static event Action OnNothingClicked;

    private readonly Dictionary<int, StaticSelectable> _selectables = new Dictionary<int, StaticSelectable>();
    private StaticSelectable _currentSelected;
    private bool _pressStartedValid;
    private Vector2 _pressScreenPosition;
    private bool _suppressAutoFocusDeselect;

    public bool HasCurrentSelection
    {
        get { return _currentSelected != null; }
    }

    protected override void Awake()
    {
        base.Awake();

        if (baker != null)
            baker.RegisterConfiguredSelectables(this);

        StaticSelectable.RegisterActiveSelectables(this);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _pressStartedValid = CanStartWorldPress();
            _pressScreenPosition = Input.mousePosition;
            if (!_pressStartedValid)
                DeselectCurrent();
            return;
        }

        if (!Input.GetMouseButtonUp(0) || !_pressStartedValid)
            return;

        _pressStartedValid = false;
        if (Vector2.Distance(_pressScreenPosition, Input.mousePosition) > dragThreshold)
            return;

        if (IsCameraMoving() || IsPointerOverUI() || mainCamera == null)
            return;

        ProcessTap(mainCamera.ScreenPointToRay(Input.mousePosition));
    }

    private bool CanStartWorldPress()
    {
        if (PlayableScenarioRuntime.CurrentScenario != null && PlayableScenarioRuntime.CurrentScenario.scenario == PlayableScenarioId.Scenario124)
        {
            return false;
        }

        return (LunaUIManager.Instance == null || !LunaUIManager.Instance.IsGameplayInputBlocked)
            && !IsCameraMoving() && !IsPointerOverUI() && mainCamera != null;
    }

    private static bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }

    private static bool IsCameraMoving()
    {
        return false;
    }

    private void ProcessTap(Ray ray)
    {
        StaticSelectable best = null;
        float bestDistance = float.MaxValue;
        int bestPriority = int.MinValue;

        foreach (KeyValuePair<int, StaticSelectable> pair in _selectables)
        {
            StaticSelectable selectable = pair.Value;
            float distance;
            if (selectable == null || !selectable.selectable || !selectable.TryRaycast(ray, out distance))
                continue;

            if (selectable.priority > bestPriority ||
                (selectable.priority == bestPriority && distance < bestDistance))
            {
                best = selectable;
                bestDistance = distance;
                bestPriority = selectable.priority;
            }
        }

        if (best == null)
        {
            DeselectCurrent();
            if (OnNothingClicked != null)
                OnNothingClicked();
            if (GameEventBus.OnNothingClicked != null)
                GameEventBus.OnNothingClicked();
            return;
        }

        best.NotifyTapped();
        SelectOrDeselect(best);
    }

    private void SelectOrDeselect(StaticSelectable selectable)
    {
        if (_currentSelected == selectable)
        {
            DeselectCurrent();
            return;
        }

        if (_currentSelected != null)
            _currentSelected.DeselectInstant();

        _currentSelected = selectable;
        _suppressAutoFocusDeselect = true;
        _currentSelected.Select();
        _suppressAutoFocusDeselect = false;
    }

    public void RegisterSelectable(StaticSelectable selectable)
    {
        if (selectable != null)
            _selectables[selectable.GetInstanceID()] = selectable;
    }

    public void UnregisterSelectable(StaticSelectable selectable)
    {
        if (selectable == null)
            return;

        _selectables.Remove(selectable.GetInstanceID());
        if (_currentSelected == selectable)
            _currentSelected = null;
    }

    public void DeselectCurrent()
    {
        if (_currentSelected == null)
            return;

        _currentSelected.Deselect();
        _currentSelected = null;
    }

    public void SelectFromUI(StaticSelectable selectable)
    {
        if (selectable != null && selectable.selectable)
            SelectOrDeselect(selectable);
    }

    private void OnDestroy()
    {
        OnNothingClicked = null;
    }
}
