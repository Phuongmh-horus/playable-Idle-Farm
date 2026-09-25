using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Draws a circular farm plot split into radial sectors in the Scene view.
/// The plot is drawn on the local XY plane for 2D scenes.
/// </summary>
public sealed class CircularFarmPlotLayoutHelper : MonoBehaviour
{
    [SerializeField] private CircularFarmPlotLayoutConfigSO config;

    public CircularFarmPlotLayoutConfigSO Config
    {
        get
        {
            if (config == null && ConfigHolder.Instance != null)
            {
                config = ConfigHolder.Instance.CircularFarmPlotLayoutConfigSO;
            }
            return config;
        }
        set => config = value;
    }

    public int RingCount => Config != null ? Config.RingCount : 0;
    public int SectorCount => Config != null ? Config.sectorCount : 0;

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (Config == null) return;

        DrawSpacing();
        DrawCircles();
        DrawDividers();
        DrawPlotPoints();
    }

    private void DrawCircles()
    {
        int count = Config.RingCount;
        if (count == 0) return;

        for (int ringIndex = 0; ringIndex < count; ringIndex++)
        {
            DrawCircle(GetRingOuterRadius(ringIndex), Config.circleColor);
            if (HasGap(ringIndex))
            {
                DrawCircle(GetRingOuterRadius(ringIndex) + GetGapAfter(ringIndex), Config.circleColor);
            }
        }
    }

    private void DrawCircle(float ringRadius, Color color)
    {
        Gizmos.color = color;
        int segments = Config.circleSegments;
        for (int i = 0; i < segments; i++)
        {
            Vector3 from = GetPoint(i / (float)segments * 360f, ringRadius);
            Vector3 to = GetPoint((i + 1) / (float)segments * 360f, ringRadius);
            Gizmos.DrawLine(from, to);
        }
    }

    private void DrawSpacing()
    {
        int count = Config.RingCount;
        if (count == 0) return;

        for (int ringIndex = 0; ringIndex < count; ringIndex++)
        {
            if (!HasGap(ringIndex))
            {
                continue;
            }

            float innerRadius = GetRingOuterRadius(ringIndex);
            float outerRadius = innerRadius + GetGapAfter(ringIndex);
#if UNITY_EDITOR
            DrawSpacingFill(innerRadius, outerRadius);
#endif
        }
    }

#if UNITY_EDITOR
    private void DrawSpacingFill(float innerRadius, float outerRadius)
    {
        Handles.color = Config.spacingColor;
        int segments = Config.circleSegments;
        for (int i = 0; i < segments; i++)
        {
            float fromAngle = i / (float)segments * 360f;
            float toAngle = (i + 1) / (float)segments * 360f;
            Vector3[] quad = GetSpacingQuad(fromAngle, toAngle, innerRadius, outerRadius);
            Handles.DrawAAConvexPolygon(quad);
        }
    }

    private Vector3[] GetSpacingQuad(
        float fromAngle,
        float toAngle,
        float innerRadius,
        float outerRadius)
    {
        return new[]
        {
            GetPoint(fromAngle, innerRadius),
            GetPoint(toAngle, innerRadius),
            GetPoint(toAngle, outerRadius),
            GetPoint(fromAngle, outerRadius)
        };
    }
#endif

    private void DrawDividers()
    {
        int sectors = Config.sectorCount;
        int rings = Config.RingCount;
        if (rings == 0 || sectors == 0) return;

        Gizmos.color = Config.dividerColor;
        float outerRadius = GetRingOuterRadius(rings - 1);
        for (int i = 0; i < sectors; i++)
        {
            float angle = Config.startAngleDegrees - i * 360f / sectors;
            Gizmos.DrawLine(transform.position, GetPoint(angle, outerRadius));
        }
    }

    private void DrawPlotPoints()
    {
        if (!Config.showPlotPoints)
        {
            return;
        }

        Gizmos.color = Config.plotPointColor;
        int rings = Config.RingCount;
        int sectors = Config.sectorCount;
        for (int ringIndex = 0; ringIndex < rings; ringIndex++)
        {
            for (int sectorIndex = 0; sectorIndex < sectors; sectorIndex++)
            {
                DrawPlotPoint(ringIndex, sectorIndex);
            }
        }
    }

    private void DrawPlotPoint(int ringIndex, int sectorIndex)
    {
        Vector3 position = GetPlotPointPosition(ringIndex, sectorIndex);
        Gizmos.color = Config.plotPointColor;
        Gizmos.DrawSphere(position, Config.plotPointSize);
        if (Config.showPlotPointDirections)
        {
            Gizmos.color = Config.plotPointDirectionColor;
            Gizmos.DrawRay(position, GetPlotPointForward(sectorIndex) * Config.plotPointDirectionLength);
        }
    }

    #endregion

    public float GetRingInnerRadius(int ringIndex)
    {
        if (Config == null || ringIndex <= 0)
        {
            return 0f;
        }

        float radius = 0f;
        int count = Mathf.Min(ringIndex, Config.RingCount);
        for (int i = 0; i < count; i++)
        {
            radius += GetRingWidth(i) + GetGapAfter(i);
        }
        return radius;
    }

    public float GetRingOuterRadius(int ringIndex)
    {
        if (Config == null || ringIndex < 0)
        {
            return 0f;
        }

        return GetRingInnerRadius(ringIndex) + GetRingWidth(ringIndex);
    }

    public float GetRingWidth(int ringIndex)
    {
        if (Config == null || Config.rings == null || ringIndex < 0 || ringIndex >= Config.rings.Count)
        {
            return 2f;
        }

        return Mathf.Max(0.01f, Config.rings[ringIndex].ringWidth);
    }

    public bool HasGap(int ringIndex)
    {
        return Config != null && Config.rings != null && ringIndex >= 0 && ringIndex < Config.rings.Count && Config.rings[ringIndex].hasGap;
    }

    public float GetGapAfter(int ringIndex)
    {
        if (!HasGap(ringIndex))
        {
            return 0f;
        }

        return Mathf.Max(0.01f, Config.rings[ringIndex].gapThickness);
    }

    public float GetPlotPointRadiusOffset(int ringIndex)
    {
        if (Config == null || Config.rings == null || ringIndex < 0 || ringIndex >= Config.rings.Count)
        {
            return 0f;
        }

        return Config.rings[ringIndex].radiusOffset;
    }

    public float GetPlotPointRadius(int ringIndex)
    {
        float innerRadius = GetRingInnerRadius(ringIndex);
        float width = GetRingWidth(ringIndex);
        float offset = GetPlotPointRadiusOffset(ringIndex);
        return Mathf.Max(0.01f, innerRadius + width * 0.5f + offset);
    }

    public Vector3 GetPlotPointPosition(int ringIndex, int sectorIndex)
    {
        int sectorCount = Config != null ? Config.sectorCount : 10;
        float angleStep = 360f / Mathf.Max(1, sectorCount);
        float angle = GetPlotPointAngle(sectorIndex, angleStep);
        float pointRadius = GetPlotPointRadius(ringIndex);
        Vector3 point = GetPoint(angle, pointRadius);
        Vector2 offset = Config != null ? Config.plotPointLocalOffset : Vector2.zero;
        return point + transform.TransformVector(offset);
    }

    public Vector3 GetPlotPointForward(int sectorIndex)
    {
        int sectorCount = Config != null ? Config.sectorCount : 10;
        float angle = GetPlotPointAngle(sectorIndex, 360f / Mathf.Max(1, sectorCount));
        float radians = angle * Mathf.Deg2Rad;
        Vector3 localForward = new Vector3(-Mathf.Cos(radians), -Mathf.Sin(radians), 0f);
        return transform.TransformDirection(localForward).normalized;
    }

    public Quaternion GetPlotPointRotation(int sectorIndex)
    {
        int sectorCount = Config != null ? Config.sectorCount : 10;
        float angle = GetPlotPointAngle(sectorIndex, 360f / Mathf.Max(1, sectorCount));
        float radians = angle * Mathf.Deg2Rad;
        Vector3 localForward = new Vector3(-Mathf.Cos(radians), -Mathf.Sin(radians), 0f);

        Quaternion localRotation = Quaternion.FromToRotation(Vector3.up, localForward);
        float rotOffset = Config != null ? Config.plotPointRotationOffset : 0f;
        localRotation *= Quaternion.Euler(0f, 0f, rotOffset);

        return transform.rotation * localRotation;
    }

    private float GetPlotPointAngle(int sectorIndex, float angleStep)
    {
        float startAngle = Config != null ? Config.startAngleDegrees : 0f;
        float angleOffset = Config != null ? Config.plotPointAngleOffset : 0f;
        return startAngle + angleOffset - (sectorIndex + 0.5f) * angleStep;
    }

    private Vector3 GetPoint(float angleDegrees, float ringRadius)
    {
        float angle = angleDegrees * Mathf.Deg2Rad;
        Vector3 localPoint = new Vector3(Mathf.Cos(angle) * ringRadius, Mathf.Sin(angle) * ringRadius, 0f);
        return transform.TransformPoint(localPoint);
    }
}
