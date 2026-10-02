/// <summary>
/// Component nằm trong một <see cref="LayerBase"/> và muốn tự tạm dừng công việc chạy nền
/// (tween lặp vô hạn, coroutine, vòng lặp UniTask, particle...) khi layer bị che hoặc ẩn.
///
/// Đây là cách thay thế cho <c>gameObject.SetActive(false)</c> cả layer. SetActive giết luôn
/// coroutine đang chạy (không phải tạm dừng), bắn lại toàn bộ OnEnable/OnDisable của cây con
/// — ví dụ <c>PackTimeCounter.OnEnable</c> sẽ chạy lại <c>SetupStarterPackData</c> và kéo theo
/// một lần ghi save — và bản thân việc bật/tắt cả hierarchy đã là chi phí đáng kể trên máy yếu.
///
/// <see cref="LayerBase"/> quét các handler trong cây con một lần rồi cache lại. Object sinh ra
/// động sau đó (item lấy từ pool) thì tự gọi <see cref="LayerBase.RegisterVisibilityHandler"/>
/// và <see cref="LayerBase.UnregisterVisibilityHandler"/>.
///
/// Handler phải chịu được việc bị gọi lại nhiều lần với cùng một giá trị và phải mặc định
/// coi mình là đang hiển thị, để component vẫn chạy đúng khi không nằm trong layer nào.
/// </summary>
public interface ILayerVisibilityHandler
{
    void OnLayerVisibilityChanged(bool visible);
}
