using UnityEngine;

/// <summary>
/// ScriptableObject chứa toàn bộ cấu hình điều khiển Camera cho CameraDragController.
/// Tạo mới thông qua: Assets → Create → Camera Settings.
/// </summary>
[CreateAssetMenu(menuName = "Camera Settings", fileName = "CameraSettings", order = 60)]
public class CameraSettingsSO : ScriptableObject
{
    // ═══════════════════════════════════════════════════════════════════
    // 1. DI CHUYỂN & KÉO (PAN / DRAG)
    // ═══════════════════════════════════════════════════════════════════

    [Header("─── Di Chuyển / Pan ───")]
    [Tooltip("Độ nhạy khi kéo camera bằng chuột hoặc ngón tay. Giá trị càng cao camera di chuyển càng nhanh.")]
    public float dragSensitivity = 1f;

    [Tooltip("Ngưỡng khoảng cách di chuyển (pixel) trước khi bắt đầu tính là thao tác kéo (Pan) thay vì click/tap.")]
    public float dragThreshold = 10f;

    [Tooltip("Bộ lọc làm mượt chuyển động kéo giữa các frame để tránh giật lag. 0 = không làm mượt, 0.9 = mượt tối đa.")]
    [Range(0f, 0.9f)] public float dragMovementSmoothing = 0.35f;

    [Tooltip("Tọa độ thế giới (World Space) nhỏ nhất (X, Y, Z) mà camera có thể di chuyển tới.")]
    public Vector3 minBounds = new Vector3(-50f, -50f, -50f);

    [Tooltip("Tọa độ thế giới (World Space) lớn nhất (X, Y, Z) mà camera có thể di chuyển tới.")]
    public Vector3 maxBounds = new Vector3(50f, 50f, 50f);

    [Tooltip("Tỉ lệ thu hẹp giới hạn di chuyển dựa theo kích thước khung nhìn camera (viewport), giúp camera không nhìn lọt ra ngoài map khi zoom xa.")]
    [Range(0f, 0.5f)] public float maxFootprintInsetRatio = 0.4f;

    // ═══════════════════════════════════════════════════════════════════
    // 2. ĐỘ NHẠY KÉO THEO MỨC ZOOM (ZOOM-ADAPTIVE)
    // ═══════════════════════════════════════════════════════════════════

    [Header("─── Độ Nhạy Kéo Theo Mức Zoom ───")]
    [Tooltip("Hệ số nhân độ nhạy kéo / quán tính khi camera đang ở mức zoom xa nhất (Normalized Zoom = 0).")]
    [Range(0.05f, 2f)] public float zoomDampeningMultiplierAtZoomOut = 1f;

    [Tooltip("Hệ số nhân độ nhạy kéo / quán tính khi camera đang ở mức zoom gần nhất (Normalized Zoom = 1).")]
    [Range(0.05f, 2f)] public float zoomDampeningMultiplierAtZoomIn = 0.5f;

    // ═══════════════════════════════════════════════════════════════════
    // 3. QUÁN TÍNH TRƯỢT (MOMENTUM / INERTIA)
    // ═══════════════════════════════════════════════════════════════════

    [Header("─── Quán Tính / Momentum ───")]
    [Tooltip("Bật/tắt hiệu ứng trượt quán tính sau khi thả tay vuốt nhanh.")]
    public bool enableMomentum = true;

    [Tooltip("Bộ lọc làm mượt vận tốc vuốt được ghi nhận mỗi frame (Low-pass filter).")]
    [Range(0f, 1f)] public float velocitySmoothing = 0.25f;

    [Tooltip("Vận tốc vuốt tối thiểu (world units/giây) để kích hoạt hiệu ứng trượt quán tính khi nhấc tay.")]
    public float minFlingSpeed = 8f;

    [Tooltip("Lực ma sát hãm vận tốc trượt mỗi giây khi đang trượt quán tính (càng cao dừng càng nhanh).")]
    public float momentumFriction = 4f;

    [Tooltip("Ngưỡng vận tốc tối thiểu (world units/giây) để dừng hẳn trượt quán tính.")]
    public float momentumStopThreshold = 0.15f;

    // ═══════════════════════════════════════════════════════════════════
    // 4. HIỆU ỨNG ĐÀN HỒI BIÊN GIỚI HẠN (RUBBER BAND)
    // ═══════════════════════════════════════════════════════════════════

    [Header("─── Đàn Hồi Biên / Rubber Band ───")]
    [Tooltip("Cho phép camera bị kéo hoặc trượt quán tính vượt ra ngoài phạm vi biên (kèm lực cản) trước khi nảy bật lại.")]
    public bool enableRubberBand = true;

    [Tooltip("Khoảng cách tối đa (world units) mà camera có thể bị kéo vượt ra ngoài biên map.")]
    public float maxOverscroll = 12f;

    [Tooltip("Thời gian (giây) để camera tự động lướt mượt mà quay trở lại trong phạm vi biên map.")]
    public float rubberBandReturnDuration = 0.2f;

    // ═══════════════════════════════════════════════════════════════════
    // 5. THU PHÓNG (ZOOM)
    // ═══════════════════════════════════════════════════════════════════

    [Header("─── Thu Phóng 2D (Orthographic Zoom) ───")]
    [Tooltip("Độ nhạy khi chụm 2 ngón tay (Pinch Zoom) trên camera trực giao 2D.")]
    public float zoomSensitivity = 0.01f;

    [Tooltip("Kích thước Orthographic tối thiểu (mức zoom gần nhất trong 2D).")]
    public float minOrthographicSize = 3f;

    [Tooltip("Kích thước Orthographic tối đa (mức zoom xa nhất trong 2D).")]
    public float maxOrthographicSize = 30f;

    [Header("─── Thu Phóng 3D (Perspective Zoom) ───")]
    [Tooltip("Độ nhạy khi chụm 2 ngón tay (Pinch Zoom) trên camera phối cảnh 3D (Field of View).")]
    public float perspectiveZoomSensitivity = 0.1f;

    [Tooltip("Góc nhìn FOV tối thiểu (mức zoom gần nhất trong 3D).")]
    public float minFieldOfView = 10f;

    [Tooltip("Góc nhìn FOV tối đa (mức zoom xa nhất trong 3D).")]
    public float maxFieldOfView = 100f;

    [Header("─── Thu Phóng & Cuộn Bằng Con Lăn Chuột (PC / Editor) ───")]
    [Tooltip("Độ nhạy thu phóng khi giữ Ctrl + lăn con lăn chuột (Mouse ScrollWheel) trên PC hoặc Editor.")]
    public float scrollZoomSensitivity = 5f;

    [Tooltip("Độ nhạy cuộn camera lên/xuống khi lăn con lăn chuột (không giữ Ctrl) trên PC hoặc Editor.")]
    public float scrollPanSensitivity = 2f;

    [Tooltip("Đảo ngược hướng cuộn camera lên/xuống khi lăn con lăn chuột.")]
    public bool invertScrollPan = false;

    [Header("─── Cài Đặt Zoom Chung ───")]
    [Tooltip("Mức zoom mặc định ban đầu khi vào map (0 = xa nhất/bao quát, 1 = gần nhất/chi tiết).")]
    [Range(0f, 1f)] public float defaultNormalizedZoom = 0.5f;

    [Tooltip("Thời gian (giây) chuyển đổi zoom mượt mà khi zoom theo bước hoặc gọi qua code API.")]
    public float smoothZoomDuration = 0.3f;

    // ═══════════════════════════════════════════════════════════════════
    // 6. TỰ ĐỘNG FOCUS MỤC TIÊU (AUTO FOCUS)
    // ═══════════════════════════════════════════════════════════════════

    [Header("─── Tự Động Focus Mục Tiêu ───")]
    [Tooltip("Thời gian (giây) để camera tự động lướt mượt tới vị trí mục tiêu được chỉ định.")]
    public float autoFocusDuration = 0.35f;

    [Tooltip("Độ lệch vị trí trong không gian thế giới (World-space offset) so với mục tiêu focus (ví dụ: bù trục Z để camera nhìn mục tiêu ở tầm mắt thuận tiện trên màn hình).")]
    public Vector3 focusOffset = Vector3.zero;

    // ═══════════════════════════════════════════════════════════════════
    // 7. FOCUS VÀO WORLD POPUP (POPUP FOCUS & ZOOM)
    // ═══════════════════════════════════════════════════════════════════

    [Header("─── Focus Vào World Popup ───")]
    [Tooltip("Mức zoom normalized khi camera focus vào World Popup (0 = xa nhất, 1 = gần nhất).")]
    [Range(0f, 1f)] public float popupFocusNormalizedZoom = 0.8f;

    [Tooltip("Thời gian (giây) camera lướt và zoom vào World Popup.")]
    public float popupFocusDuration = 0.35f;

    [Tooltip("Độ lệch vị trí mục tiêu khi focus vào World Popup.")]
    public Vector3 popupFocusOffset = Vector3.zero;

    [Tooltip("Tạm thời khóa thao tác vuốt/pinch của người chơi trong lúc camera đang lướt và zoom vào popup.")]
    public bool lockInputDuringPopupFocus = true;
}

