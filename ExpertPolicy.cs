namespace FightingBotML;

/// <summary>
/// "CHUYÊN GIA" gán nhãn (expert labeler) — vì SVM là học có giám sát, cần một tập dữ liệu
/// (đặc trưng, nhãn đúng) để học. Trong game thật, nhãn này có thể lấy từ log của người chơi
/// giỏi; ở đây, vì không có dữ liệu người chơi thật, ta dùng một bộ luật (rule-based) đóng vai
/// trò "chuyên gia" để xác định hành động lý tưởng cho từng tình huống, từ đó SINH RA nhãn cho
/// tập huấn luyện. Đây là kỹ thuật thường gặp gọi là "học bắt chước" (imitation learning /
/// behavior cloning): huấn luyện một mô hình để bắt chước quyết định của một chuyên gia.
///
/// Mục tiêu của SVM sau khi huấn luyện KHÔNG PHẢI là ghi nhớ lại đúng bộ luật if-else này, mà là
/// học được RANH GIỚI QUYẾT ĐỊNH (decision boundary) tổng quát từ các mẫu ví dụ, để có thể áp
/// dụng tốt cho cả những trạng thái chưa từng xuất hiện trong tập huấn luyện.
/// </summary>
public static class ExpertPolicy
{
    public static ActionType GetIdealAction(double distance, double ownHp, double enemyHp, bool onCooldown, bool enemyJustAttacked)
    {
        bool inRange = distance <= BattleEnvironment.AttackRange;
        bool ownLow = ownHp <= 30;
        bool enemyLow = enemyHp <= 30;

        // Dang yeu VA doi thu vua tan cong -> uu tien ne don de bao toan mang
        if (ownLow && enemyJustAttacked)
            return ActionType.Dodge;

        if (inRange)
        {
            if (!onCooldown)
            {
                // Con don de danh: ket lieu neu doi thu yeu, hoac tan cong khi minh dang ap dao ve mau
                if (enemyLow || ownHp >= enemyHp)
                    return ActionType.Attack;

                // Minh yeu the hon doi thu -> phong thu an toan thay vi lien don
                return ActionType.Block;
            }

            // Dang hoi chieu, khong the tan cong -> phong thu
            return enemyJustAttacked ? ActionType.Block : ActionType.Dodge;
        }

        // Chua trong tam danh
        if (ownLow)
            return ActionType.MoveBackward; // giu khoang cach an toan khi dang yeu
        return ActionType.MoveForward;      // ap sat de tao co hoi ra don khi con khoe
    }
}
