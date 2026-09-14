namespace FightingBotML;

/// <summary>
/// Không gian hành động (Action Space) của bài toán.
/// Mỗi lượt (turn), một đấu sĩ chỉ được chọn ĐÚNG 1 hành động trong 5 hành động này.
/// </summary>
public enum ActionType
{
    Attack = 0,       // Tấn công: gây sát thương nếu trong tầm đánh và không bị block/dodge
    Block = 1,        // Đỡ đòn: giảm mạnh sát thương nhận vào nếu bị tấn công
    Dodge = 2,        // Né đòn: tránh hoàn toàn sát thương nếu bị tấn công, nhưng không gây sát thương
    MoveForward = 3,  // Tiến lại gần đối thủ để vào tầm đánh
    MoveBackward = 4  // Lùi ra xa để tránh bị áp sát
}
