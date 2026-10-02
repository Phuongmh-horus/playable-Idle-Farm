using Cysharp.Threading.Tasks;
using MemoryPack;
using System;
using System.IO;
using System.Threading;
using UnityEngine;

/// <summary>
/// Kết quả load dữ liệu local. Phân biệt rõ 3 trạng thái, vì gộp chúng lại
/// (trả về null cho cả 3) chính là nguyên nhân gây wipe data của user cũ.
/// </summary>
public enum LoadDataStatus
{
    /// <summary>Đọc thành công.</summary>
    Success = 0,

    /// <summary>Máy này chưa từng có data — user mới thật sự. An toàn để tạo account mới.</summary>
    NoDataEverSaved = 1,

    /// <summary>Máy này ĐÃ TỪNG có data nhưng lần này đọc không được (I/O lỗi, file hỏng,
    /// storage đầy, file biến mất...). TUYỆT ĐỐI không được xoá gì hoặc tạo account mới đè lên.</summary>
    Failed = 2,
}

public static class LocalDatabaseManager
{
    #region Player

    private static string _playerDataPath = $"{Application.persistentDataPath}/playerData_FN.dat";
    private static string _playerDataTmpPath = $"{Application.persistentDataPath}/playerData_FN.dat.tmp";
    private static string _playerDataBakPath = $"{Application.persistentDataPath}/playerData_FN.dat.bak";

    /// <summary>
    /// Cờ "máy này đã từng lưu data thành công". Nằm trên FILE (không phải PlayerPrefs) vì
    /// PlayerPrefs/SharedPreferences có thể mất trắng khi máy tắt đột ngột, và vì luồng khởi động
    /// từng gọi PlayerPrefs.DeleteAll(). Còn file này = user cũ = cấm tạo account mới đè lên.
    /// </summary>
    private static string _playerDataStampPath = $"{Application.persistentDataPath}/playerData_FN.stamp";

    private static readonly SemaphoreSlim _playerFilelock = new(1, 1);

    /// <summary>Trạng thái của lần <see cref="LoadPlayerData"/> gần nhất.</summary>
    public static LoadDataStatus LastLoadStatus { get; private set; } = LoadDataStatus.NoDataEverSaved;

    /// <summary>
    /// Ảnh chụp gọn trạng thái các file save + dung lượng trống, đính kèm vào log chẩn đoán.
    /// Đây là dữ liệu quyết định để biết user rơi vào nhánh lỗi nào:
    /// mất .dat mà còn .bak là một nguyên nhân khác hẳn với đầy bộ nhớ.
    /// </summary>
    public static string GetStorageSnapshot()
    {
        try
        {
            return $"dat={SizeOf(_playerDataPath)},bak={SizeOf(_playerDataBakPath)}," +
                   $"tmp={SizeOf(_playerDataTmpPath)},stamp={(File.Exists(_playerDataStampPath) ? 1 : 0)}," +
                   $"freeMB={FreeSpaceMb()}";
        }
        catch (Exception e)
        {
            return $"snapshot_error={e.GetType().Name}";
        }
    }

    /// <summary>Kích thước file, -1 nghĩa là không tồn tại.</summary>
    private static long SizeOf(string path)
    {
        try
        {
            return File.Exists(path) ? new FileInfo(path).Length : -1;
        }
        catch
        {
            return -2;
        }
    }

    /// <summary>Dung lượng trống (MB), -1 nếu nền tảng không cho đọc.</summary>
    private static long FreeSpaceMb()
    {
        try
        {
            var root = Path.GetPathRoot(Application.persistentDataPath);
            if (string.IsNullOrEmpty(root)) return -1;
            return new DriveInfo(root).AvailableFreeSpace / (1024 * 1024);
        }
        catch
        {
            // DriveInfo không đáng tin trên Android/IL2CPP; thiếu số này cũng không sao.
            return -1;
        }
    }

    public static async UniTask SavePlayerData(PlayerData playerData)
    {
        // Không bao giờ ghi payload null. MemoryPack serialize null thành đúng 1 byte 0xFF,
        // và khi đọc lại nó trả về null MÀ KHÔNG throw — tức là file "hợp lệ" nhưng rỗng nghĩa,
        // qua mặt mọi lớp recovery bên dưới. Đây là đường mất sạch data mà không có log nào.
        if (playerData == null)
        {
            Debug.LogError("SavePlayerData called with null playerData - refused to overwrite save file.");
            SaveDiagnostics.Report(SaveDiagnostics.StageSaveNullPayload, "null_arg",
                storage: GetStorageSnapshot());
            return;
        }

        await _playerFilelock.WaitAsync();
        try
        {
            var bytes = MemoryPackSerializer.Serialize(playerData);
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogError("SavePlayerData: serializer produced an empty buffer - refused to overwrite save file.");
                SaveDiagnostics.Report(SaveDiagnostics.StageSaveNullPayload, "empty_buffer",
                    storage: GetStorageSnapshot());
                return;
            }

            // Ghi .tmp rồi fsync xuống đĩa TRƯỚC khi swap.
            // File.WriteAllBytesAsync chỉ đẩy vào page cache của OS; nếu máy mất điện hoặc
            // kernel panic ngay sau đó thì thao tác rename có thể đã xuống đĩa còn nội dung
            // thì chưa -> .dat rỗng. Flush(true) ép fsync nên swap mới thật sự atomic.
            using (var fs = new FileStream(_playerDataTmpPath, FileMode.Create, FileAccess.Write, FileShare.None,
                       bufferSize: 4096, useAsync: true))
            {
                await fs.WriteAsync(bytes, 0, bytes.Length);
                await fs.FlushAsync();
                fs.Flush(true);
            }

            if (File.Exists(_playerDataPath))
            {
                try
                {
                    File.Replace(_playerDataTmpPath, _playerDataPath, _playerDataBakPath);
                }
                catch (Exception replaceEx)
                {
                    // File.Replace không được hỗ trợ trên một số filesystem Android (sdcard FAT/exFAT,
                    // overlay), nên nhánh này chạy thường xuyên hơn ta tưởng. Log lại để đo tỉ lệ thật.
                    Debug.LogWarning($"File.Replace failed: {replaceEx.Message}. Falling back to Copy.");
                    SaveDiagnostics.ReportException(SaveDiagnostics.StageSaveReplaceFallback, replaceEx,
                        storage: GetStorageSnapshot());

                    // Backup bản cũ trước.
                    File.Copy(_playerDataPath, _playerDataBakPath, overwrite: true);

                    // Dùng Copy(overwrite) chứ KHÔNG Delete rồi Move.
                    // Delete+Move để lại một khoảng thời gian .dat không tồn tại; nếu app bị kill
                    // đúng lúc đó (Android LMK kill app nền rất hay xảy ra) thì lần mở sau .dat
                    // mất tích và luồng cũ coi đó là "user mới" -> reset về level 1.
                    File.Copy(_playerDataTmpPath, _playerDataPath, overwrite: true);
                    File.Delete(_playerDataTmpPath);
                }
            }
            else
            {
                File.Copy(_playerDataTmpPath, _playerDataPath, overwrite: true);
                File.Delete(_playerDataTmpPath);
            }

            // Đánh dấu "máy này đã có data" sau khi ghi thành công.
            if (!File.Exists(_playerDataStampPath))
            {
                File.WriteAllText(_playerDataStampPath, "1");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"SavePlayerData error: {e}");
            SaveDiagnostics.ReportException(SaveDiagnostics.StageSaveError, e,
                storage: GetStorageSnapshot());
        }
        finally
        {
            _playerFilelock.Release();
        }
    }

    /// <summary>
    /// Đọc data theo thứ tự .dat -> .bak -> .tmp. Trả null kèm <see cref="LastLoadStatus"/>
    /// để caller phân biệt được user mới với lỗi đọc.
    /// </summary>
    public static PlayerData LoadPlayerData()
    {
        // Chụp trạng thái TRƯỚC khi đọc: các nhánh recovery bên dưới có ghi lại file,
        // chụp sau sẽ không còn thấy hiện trường lỗi.
        var snapshot = GetStorageSnapshot();

        _lastReadError = null;
        _lastReadDetail = null;

        var everSaved = File.Exists(_playerDataStampPath)
                        || File.Exists(_playerDataPath)
                        || File.Exists(_playerDataBakPath)
                        || File.Exists(_playerDataTmpPath);

        // .bak/.tmp là fallback thật sự: .dat có thể MẤT HẲN (không throw exception nào)
        // khi app bị kill giữa lúc swap file. Luồng cũ chỉ thử .bak bên trong catch nên
        // trường hợp "file biến mất" không bao giờ được cứu.
        if (TryReadFrom(_playerDataPath, "primary", out var data))
        {
            LastLoadStatus = LoadDataStatus.Success;
            SaveDiagnostics.Report(SaveDiagnostics.StageLoadOk, storage: snapshot);
            return data;
        }

        if (TryReadFrom(_playerDataBakPath, "bak", out data))
        {
            Debug.LogWarning("LoadPlayerData recovered from .bak");
            LastLoadStatus = LoadDataStatus.Success;
            SaveDiagnostics.Report(SaveDiagnostics.StageLoadRecoveredBak, storage: snapshot);
            SavePlayerData(data).Forget(); // ghi lại ngay để lần sau không phải cứu nữa
            return data;
        }

        if (TryReadFrom(_playerDataTmpPath, "tmp", out data))
        {
            Debug.LogWarning("LoadPlayerData recovered from .tmp");
            LastLoadStatus = LoadDataStatus.Success;
            SaveDiagnostics.Report(SaveDiagnostics.StageLoadRecoveredTmp, storage: snapshot);
            SavePlayerData(data).Forget();
            return data;
        }

        LastLoadStatus = everSaved ? LoadDataStatus.Failed : LoadDataStatus.NoDataEverSaved;
        if (LastLoadStatus == LoadDataStatus.Failed)
        {
            // Đây là user đang mất data. Đếm event này để biết tỉ lệ thật sau khi ship.
            Debug.LogError("LoadPlayerData FAILED but save stamp exists - refusing to reset local data.");
            SaveDiagnostics.Report(SaveDiagnostics.StageLoadFailed, _lastReadError, _lastReadDetail, snapshot);
        }
        else
        {
            SaveDiagnostics.Report(SaveDiagnostics.StageLoadNewUser, storage: snapshot);
        }

        return null;
    }

    // Lý do đọc hỏng gần nhất, để đính kèm vào event load_failed.
    private static string _lastReadError;
    private static string _lastReadDetail;

    private static bool TryReadFrom(string path, string label, out PlayerData data)
    {
        data = null;
        try
        {
            if (!File.Exists(path))
            {
                // Ghi nhận riêng: "file không tồn tại" là một nguyên nhân khác hẳn "file hỏng",
                // và chính nó mới là nhánh mà luồng cũ không bao giờ cứu được.
                if (string.IsNullOrEmpty(_lastReadError)) _lastReadError = "file_missing";
                _lastReadDetail = label;
                return false;
            }

            var bytes = File.ReadAllBytes(path);
            if (bytes.Length == 0)
            {
                Debug.LogWarning($"LoadPlayerData: {label} file is empty.");
                _lastReadError = "file_empty";
                _lastReadDetail = label;
                return false;
            }

            data = MemoryPackSerializer.Deserialize<PlayerData>(bytes);

            // Deserialize trả null mà KHÔNG throw khi payload là marker null (1 byte 0xFF).
            // Phải check tay, nếu không sẽ nuốt luôn trường hợp file hợp lệ nhưng rỗng nghĩa.
            if (data == null)
            {
                Debug.LogWarning($"LoadPlayerData: {label} deserialized to null.");
                _lastReadError = "deserialize_null";
                _lastReadDetail = label;
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadPlayerData error reading {label}: {e}");
            _lastReadError = e.GetType().Name;
            _lastReadDetail = $"{label}:{e.Message}";
            data = null;
            return false;
        }
    }

    public static PlayerData CreateNewAcc()
    {
        try
        {
            Debug.Log("CreateNewAcc");
            var playerData = new PlayerData();
            playerData.DeviceId = GameConfig.DeviceId;
            playerData.CreateTimeString = DateTime.UtcNow.ToDefaultString();
            playerData.NickName = "PZ_" + Guid.NewGuid().ToString().Substring(0, 5);
            playerData.LevelDisplay = 1;
            playerData.LevelProgress = 1;
            playerData.AvatarId = 1;
            playerData.LastLoginTimeString = DateTime.UtcNow.ToDefaultString();

            return playerData;
        }
        catch (Exception e)
        {
            Debug.LogError($"CreateNewAcc error: {e}");
            SaveDiagnostics.ReportException("create_new_acc_error", e);
        }

        // Không trả null: caller đem thẳng giá trị này đi gán/ghi đè save file.
        return new PlayerData();
    }

    /// <summary>
    /// Đổi tên các file save không đọc được thành .corrupt.&lt;timestamp&gt; thay vì xoá,
    /// rồi cho phép chơi lại từ đầu.
    ///
    /// CHỈ được gọi khi đã hỏi server và server trả lời DỨT KHOÁT là không có account
    /// (HTTP 404, không phải lỗi mạng). Nếu không có lối thoát này, user hỏng file vĩnh viễn
    /// sẽ bị chặn ghi ở mọi phiên và không bao giờ tiến triển được nữa.
    ///
    /// Giữ file lại (không xoá) để bộ phận hỗ trợ còn khả năng phục hồi thủ công.
    /// </summary>
    public static void QuarantineCorruptData()
    {
        SaveDiagnostics.Report(SaveDiagnostics.StageQuarantined, _lastReadError, _lastReadDetail,
            GetStorageSnapshot());

        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        foreach (var path in new[] { _playerDataPath, _playerDataBakPath, _playerDataTmpPath })
        {
            try
            {
                if (!File.Exists(path)) continue;
                var dest = $"{path}.corrupt.{stamp}";
                File.Copy(path, dest, overwrite: true);
                File.Delete(path);
                Debug.LogWarning($"QuarantineCorruptData: {path} -> {dest}");
            }
            catch (Exception e)
            {
                Debug.LogError($"QuarantineCorruptData error on {path}: {e}");
                SaveDiagnostics.ReportException("quarantine_error", e);
            }
        }

        LastLoadStatus = LoadDataStatus.NoDataEverSaved;
    }

    #endregion

    #region Reset Data

    /// <summary>
    /// Xóa tất cả dữ liệu lưu trữ cục bộ trong thư mục persistentDataPath.
    /// CHỈ dùng cho thao tác reset có chủ đích của user / tool dev.
    /// KHÔNG BAO GIỜ gọi hàm này từ luồng khởi động khi load data thất bại —
    /// nó xoá cả .bak, .tmp và .stamp nên biến một lỗi đọc tạm thời thành mất data vĩnh viễn.
    /// </summary>
    public static void ResetAllData()
    {
        try
        {
            var files = Directory.GetFiles(Application.persistentDataPath);
            foreach (var file in files)
                try
                {
                    File.Delete(file);
                    Debug.Log($"Đã xóa file: {file}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Không thể xóa file {file}: {ex.Message}");
                }

            Debug.Log($"Đã xóa tất cả {files.Length} file trong thư mục persistentDataPath");
        }
        catch (Exception e)
        {
            Debug.LogError($"Lỗi khi reset dữ liệu: {e}");
        }
    }

    #endregion
}
