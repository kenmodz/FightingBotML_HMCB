namespace FightingBotML;

/// <summary>
/// Trích xuất & CHUẨN HÓA (normalize) đặc trưng từ trạng thái trận đấu để đưa vào SVM.
///
/// SVM tìm siêu phẳng phân cách dựa trên khoảng cách hình học giữa các điểm dữ liệu, nên rất
/// nhạy cảm với đơn vị/thang đo của từng đặc trưng — nếu một đặc trưng có giá trị 0-100 (máu)
/// và một đặc trưng khác chỉ có giá trị 0-1 (cờ boolean), đặc trưng có thang đo lớn hơn sẽ
/// "lấn át" một cách không công bằng khi tính khoảng cách/tích vô hướng. Vì vậy mọi đặc trưng
/// đều được đưa về khoảng xấp xỉ [0, 1] trước khi huấn luyện.
/// </summary>
public static class FeatureExtractor
{
    public const int NumFeatures = 5;

    public static double[] Extract(double distance, double ownHp, double enemyHp, bool onCooldown, bool enemyJustAttacked)
    {
        return new double[]
        {
            distance / BattleEnvironment.MaxDistance,   // 0 (sat) .. 1 (xa nhat)
            ownHp / Fighter.MaxHP,                        // 0 (kiet suc) .. 1 (day mau)
            enemyHp / Fighter.MaxHP,
            onCooldown ? 1.0 : 0.0,
            enemyJustAttacked ? 1.0 : 0.0
        };
    }
}
