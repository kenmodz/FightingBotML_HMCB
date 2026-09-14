namespace FightingBotML;

/// <summary>
/// Đại diện cho một đấu sĩ trong trận đánh (có thể là Bot học máy hoặc đối thủ kịch bản).
/// </summary>
public class Fighter
{
    public string Name { get; }
    public int HP { get; set; }
    public const int MaxHP = 100;

    public Fighter(string name)
    {
        Name = name;
        HP = MaxHP;
    }

    public bool IsAlive => HP > 0;

    public void ResetHP() => HP = MaxHP;
}
