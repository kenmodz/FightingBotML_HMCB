namespace FightingBotML;

/// <summary>
/// Đối thủ "kịch bản" (rule-based / scripted AI) — KHÔNG dùng học máy, chỉ dùng luật if-else
/// có yếu tố ngẫu nhiên. Đây là đối thủ để (1) huấn luyện bot Q-Learning qua nhiều trận,
/// và (2) làm mốc đánh giá (baseline) xem bot đã học tốt tới đâu.
///
/// Việc so sánh Bot học máy vs Đối thủ kịch bản chính là "bài toán cụ thể" cần giải quyết:
/// bot có tự học được chiến thuật (giữ khoảng cách, phòng thủ khi yếu, dứt điểm khi có cơ hội)
/// tốt hơn một đối thủ lập trình luật cứng hay không.
/// </summary>
public static class ScriptedOpponent
{
    public static ActionType ChooseAction(int distance, int ownHp, int enemyHp, Random rng)
    {
        bool inRange = distance <= BattleEnvironment.AttackRange;
        bool lowHp = ownHp <= 30;

        double roll = rng.NextDouble();

        if (!inRange)
        {
            // Ở xa: phần lớn thời gian áp sát, thỉnh thoảng giữ khoảng cách nếu đang yếu
            if (lowHp)
                return roll < 0.5 ? ActionType.MoveForward : ActionType.MoveBackward;
            return roll < 0.75 ? ActionType.MoveForward : ActionType.MoveBackward;
        }

        if (lowHp)
        {
            // Đang yếu: thiên về phòng thủ
            if (roll < 0.40) return ActionType.Block;
            if (roll < 0.70) return ActionType.Dodge;
            if (roll < 0.85) return ActionType.MoveBackward;
            return ActionType.Attack;
        }

        // Máu ổn: thiên về tấn công nhưng vẫn có phòng thủ để không dễ đoán
        if (roll < 0.55) return ActionType.Attack;
        if (roll < 0.75) return ActionType.Block;
        if (roll < 0.90) return ActionType.Dodge;
        return ActionType.MoveBackward;
    }
}
