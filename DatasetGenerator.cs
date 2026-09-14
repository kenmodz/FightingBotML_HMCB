namespace FightingBotML;

/// <summary>
/// Sinh tập dữ liệu huấn luyện có nhãn (labeled dataset) cho bài toán phân loại của SVM.
///
/// Vì không có sẵn dữ liệu người chơi thật, ta LẤY MẪU NGẪU NHIÊN (Monte Carlo sampling) trên
/// toàn bộ không gian trạng thái có thể xảy ra trong trận đấu (mọi tổ hợp khoảng cách, máu, hồi
/// chiêu, đối thủ vừa tấn công hay chưa), sau đó dùng <see cref="ExpertPolicy"/> để gán "nhãn
/// đúng" cho từng mẫu. Cách lấy mẫu ngẫu nhiên đảm bảo dữ liệu huấn luyện bao phủ đều khắp không
/// gian trạng thái, thay vì chỉ tập trung vào một vài tình huống hay gặp.
/// </summary>
public static class DatasetGenerator
{
    public record Sample(double[] Features, int Label);

    public static List<Sample> Generate(int count, Random rng)
    {
        var data = new List<Sample>(count);

        for (int i = 0; i < count; i++)
        {
            double distance = rng.NextDouble() * BattleEnvironment.MaxDistance;
            double ownHp = 1 + rng.NextDouble() * (Fighter.MaxHP - 1);
            double enemyHp = 1 + rng.NextDouble() * (Fighter.MaxHP - 1);
            bool onCooldown = rng.NextDouble() < 0.3;
            bool enemyJustAttacked = rng.NextDouble() < 0.3;

            ActionType label = ExpertPolicy.GetIdealAction(distance, ownHp, enemyHp, onCooldown, enemyJustAttacked);
            double[] features = FeatureExtractor.Extract(distance, ownHp, enemyHp, onCooldown, enemyJustAttacked);

            data.Add(new Sample(features, (int)label));
        }

        return data;
    }
}
