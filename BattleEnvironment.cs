namespace FightingBotML;

/// <summary>
/// Môi trường mô phỏng trận đấu đối kháng 1vs1 theo lượt (turn-based). Dùng để (1) chạy các
/// trận đấu minh họa/demo cho bot đã huấn luyện bằng SVM, và (2) làm luật tham chiếu khi thiết
/// kế bộ luật chuyên gia (`ExpertPolicy`) dùng để gán nhãn dữ liệu huấn luyện.
/// StepResult vẫn trả về reward (phần thưởng) dù SVM không dùng đến, để có thể tái sử dụng môi
/// trường này cho các hướng mở rộng khác (ví dụ Reinforcement Learning) trong tương lai.
/// </summary>
public class BattleEnvironment
{
    public const int MaxDistance = 10;
    public const int AttackRange = 2;      // Khoảng cách <= 2 mới đánh trúng được
    public const int MoveStep = 3;         // Mỗi lần di chuyển thay đổi khoảng cách 3 đơn vị
    public const int AttackDamage = 18;
    public const int ChipDamageOnBlock = 6; // Vẫn còn "chút" sát thương xuyên block
    public const int MaxTurnsPerMatch = 100;

    public Fighter FighterA { get; } // Bot điều khiển bằng mô hình SVM đã huấn luyện
    public Fighter FighterB { get; } // Đối thủ (kịch bản / người chơi)

    public int Distance { get; private set; }
    public int TurnCount { get; private set; }

    private int _cooldownTurnsA;
    private int _cooldownTurnsB;
    private bool _lastActionAWasAttack;
    private bool _lastActionBWasAttack;

    public BattleEnvironment(string nameA, string nameB)
    {
        FighterA = new Fighter(nameA);
        FighterB = new Fighter(nameB);
        Reset();
    }

    public void Reset()
    {
        FighterA.ResetHP();
        FighterB.ResetHP();
        Distance = 6; // Bắt đầu ở khoảng cách trung bình
        TurnCount = 0;
        _cooldownTurnsA = 0;
        _cooldownTurnsB = 0;
        _lastActionAWasAttack = false;
        _lastActionBWasAttack = false;
    }

    /// <summary>
    /// Trả về trạng thái "thô" (chưa rời rạc hóa) từ góc nhìn của FighterA, dùng làm đầu vào
    /// (feature vector) cho mô hình SVM. Khác với Q-Learning dạng bảng, SVM có thể nhận trực
    /// tiếp giá trị liên tục (khoảng cách, máu) mà không cần chia nhóm (bucket).
    /// </summary>
    public (int distance, int ownHp, int enemyHp, bool onCooldown, bool enemyJustAttacked) GetRawStateA() =>
        (Distance, FighterA.HP, FighterB.HP, _cooldownTurnsA > 0, _lastActionBWasAttack);

    public (int distance, int ownHp, int enemyHp, bool onCooldown, bool enemyJustAttacked) GetRawStateB() =>
        (Distance, FighterB.HP, FighterA.HP, _cooldownTurnsB > 0, _lastActionAWasAttack);

    /// <summary>
    /// Thực hiện một lượt đấu: cả hai đấu sĩ hành động ĐỒNG THỜI (simultaneous move),
    /// giống cơ chế các game đối kháng thời gian thực được rời rạc hóa theo khung hình (frame).
    /// </summary>
    public StepResult Step(ActionType actionA, ActionType actionB)
    {
        TurnCount++;
        double rewardA = -0.1; // phạt nhẹ mỗi lượt để khuyến khích bot kết thúc trận nhanh, dứt khoát
        double rewardB = -0.1;

        bool canAttackA = _cooldownTurnsA <= 0;
        bool canAttackB = _cooldownTurnsB <= 0;

        // 1) Xử lý di chuyển trước (thay đổi khoảng cách)
        ApplyMovement(actionA);
        ApplyMovement(actionB);

        bool inRange = Distance <= AttackRange;

        // 2) Xử lý đòn tấn công của A nhắm vào B
        if (actionA == ActionType.Attack && canAttackA)
        {
            _cooldownTurnsA = 1;
            if (inRange)
            {
                if (actionB == ActionType.Dodge)
                {
                    // Né hoàn toàn, không mất máu
                }
                else if (actionB == ActionType.Block)
                {
                    FighterB.HP -= ChipDamageOnBlock;
                    rewardA += 0.5;
                    rewardB -= 0.2;
                }
                else
                {
                    FighterB.HP -= AttackDamage;
                    rewardA += 2.0;
                    rewardB -= 2.0;
                }
            }
        }
        else if (actionA == ActionType.Attack && !canAttackA)
        {
            rewardA -= 0.3; // ra đòn khi đang hồi chiêu -> lãng phí lượt
        }

        // 3) Xử lý đòn tấn công của B nhắm vào A
        if (actionB == ActionType.Attack && canAttackB)
        {
            _cooldownTurnsB = 1;
            if (inRange)
            {
                if (actionA == ActionType.Dodge)
                {
                    // Né hoàn toàn
                }
                else if (actionA == ActionType.Block)
                {
                    FighterA.HP -= ChipDamageOnBlock;
                    rewardB += 0.5;
                    rewardA -= 0.2;
                }
                else
                {
                    FighterA.HP -= AttackDamage;
                    rewardB += 2.0;
                    rewardA -= 2.0;
                }
            }
        }
        else if (actionB == ActionType.Attack && !canAttackB)
        {
            rewardB -= 0.3;
        }

        // 4) Giảm số lượt hồi chiêu còn lại
        if (_cooldownTurnsA > 0) _cooldownTurnsA--;
        if (_cooldownTurnsB > 0) _cooldownTurnsB--;

        _lastActionAWasAttack = actionA == ActionType.Attack;
        _lastActionBWasAttack = actionB == ActionType.Attack;

        FighterA.HP = Math.Clamp(FighterA.HP, 0, Fighter.MaxHP);
        FighterB.HP = Math.Clamp(FighterB.HP, 0, Fighter.MaxHP);

        bool aDead = !FighterA.IsAlive;
        bool bDead = !FighterB.IsAlive;
        bool done = aDead || bDead || TurnCount >= MaxTurnsPerMatch;

        MatchOutcome outcome = MatchOutcome.Ongoing;
        if (aDead && bDead)
        {
            outcome = MatchOutcome.Draw;
        }
        else if (bDead)
        {
            rewardA += 10.0;
            rewardB -= 10.0;
            outcome = MatchOutcome.AWins;
        }
        else if (aDead)
        {
            rewardB += 10.0;
            rewardA -= 10.0;
            outcome = MatchOutcome.BWins;
        }
        else if (TurnCount >= MaxTurnsPerMatch)
        {
            outcome = MatchOutcome.Draw;
        }

        return new StepResult(rewardA, rewardB, done, outcome);
    }

    private void ApplyMovement(ActionType action)
    {
        if (action == ActionType.MoveForward)
            Distance = Math.Max(0, Distance - MoveStep);
        else if (action == ActionType.MoveBackward)
            Distance = Math.Min(MaxDistance, Distance + MoveStep);
    }
}

public enum MatchOutcome { Ongoing, AWins, BWins, Draw }

public readonly record struct StepResult(double RewardA, double RewardB, bool Done, MatchOutcome Outcome);
