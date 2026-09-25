using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RingLayoutConfig
{
    [Header("Plot Ring Area (Độ rộng đất trồng)")]
    [Min(0.01f)]
    [Tooltip("Độ rộng của dải đất vòng này (tính từ viền trong ra viền ngoài)")]
    public float ringWidth = 2f;

    [Tooltip("Dịch tâm đặt ô đất/cây trồng vào trong (-) hoặc ra ngoài (+)")]
    public float radiusOffset;

    [Header("Gap to Next Ring (Rãnh đệm sang vòng tiếp)")]
    [Tooltip("Bật rãnh ngăn cách giữa vòng này và vòng tiếp theo")]
    public bool hasGap;

    [Min(0.01f)]
    [Tooltip("Độ dày của rãnh đệm")]
    public float gapThickness = 0.2f;
}

[CreateAssetMenu(fileName = "CircularFarmPlotLayoutConfigSO", menuName = "Farm/CircularFarmPlotLayoutConfigSO")]
public sealed class CircularFarmPlotLayoutConfigSO : ScriptableObject
{
    [Header("Farm Geometry")]
    [Min(1)]
    public int sectorCount = 10;

    [Tooltip("Góc xoay ban đầu của đường phân chia sector (độ)")]
    public float startAngleDegrees;

    [Tooltip("Xoay tất cả các điểm plot quanh tâm nông trại (độ)")]
    public float plotPointAngleOffset;

    [Tooltip("Dịch chuyển toàn bộ plot trên mặt phẳng XY")]
    public Vector2 plotPointLocalOffset;

    [Tooltip("Xoay bản thân ô đất xung quanh trục Z của nó (độ)")]
    public float plotPointRotationOffset;

    [Header("Rings Configuration")]
    [Tooltip("Cấu hình kích thước và khoảng cách cho từng vòng từ trong ra ngoài (Index 0 = Vòng trong cùng)")]
    public List<RingLayoutConfig> rings = new List<RingLayoutConfig>();

    [Header("Gizmo Display Settings")]
    public Color circleColor = new Color(0.05f, 0.45f, 0.65f, 1f);
    public Color dividerColor = new Color(0.85f, 0.2f, 0.15f, 1f);
    public Color spacingColor = new Color(0.15f, 0.35f, 0.85f, 0.35f);

    public bool showPlotPoints = true;
    public Color plotPointColor = Color.red;
    [Min(0.01f)] public float plotPointSize = 0.15f;

    public bool showPlotPointDirections = true;
    public Color plotPointDirectionColor = Color.blue;
    [Min(0.01f)] public float plotPointDirectionLength = 0.75f;

    [Min(8)]
    public int circleSegments = 64;

    public int RingCount => rings != null ? rings.Count : 0;
}
