namespace FightingBotML;

/// <summary>
/// SVM nguyên bản chỉ giải quyết bài toán phân loại NHỊ PHÂN (2 lớp). Bài toán của ta có 5 lớp
/// (5 hành động: Attack, Block, Dodge, MoveForward, MoveBackward), nên cần mở rộng bằng chiến
/// lược ONE-VS-REST (OvR, còn gọi là One-vs-All):
///
///   Với mỗi hành động c trong 5 hành động, huấn luyện MỘT SVM nhị phân riêng, có nhiệm vụ trả
///   lời câu hỏi: "trạng thái này CÓ PHẢI nên chọn hành động c hay không?" (nhãn +1 nếu đúng là
///   hành động c, -1 nếu là 1 trong 4 hành động còn lại).
///
///   Khi dự đoán cho một trạng thái mới, ta hỏi cả 5 bộ phân loại, mỗi bộ trả về một điểm số
///   quyết định (decision score); hành động được chọn là hành động có bộ phân loại tự tin nhất
///   (điểm số cao nhất) — không nhất thiết bộ đó phải "thắng" với điểm dương, ta chỉ cần nó có
///   điểm cao hơn 4 bộ còn lại (argmax).
/// </summary>
public class MultiClassSvm
{
    public const int NumClasses = 5; // = so luong ActionType

    private readonly LinearSvm[] _classifiers;

    public MultiClassSvm(double lambda = 0.01)
    {
        _classifiers = new LinearSvm[NumClasses];
        for (int c = 0; c < NumClasses; c++)
            _classifiers[c] = new LinearSvm { Lambda = lambda };
    }

    public void Train(List<DatasetGenerator.Sample> data, int epochs)
    {
        for (int c = 0; c < NumClasses; c++)
        {
            var binaryData = data
                .Select(s => (s.Features, Label: s.Label == c ? 1 : -1))
                .ToList();

            _classifiers[c].Train(binaryData, epochs);
        }
    }

    public ActionType PredictAction(double[] features)
    {
        int bestClass = 0;
        double bestScore = double.NegativeInfinity;

        for (int c = 0; c < NumClasses; c++)
        {
            double score = _classifiers[c].Decision(features);
            if (score > bestScore)
            {
                bestScore = score;
                bestClass = c;
            }
        }

        return (ActionType)bestClass;
    }

    public double Evaluate(List<DatasetGenerator.Sample> testData)
    {
        if (testData.Count == 0) return 0.0;

        int correct = 0;
        foreach (var sample in testData)
        {
            if ((int)PredictAction(sample.Features) == sample.Label)
                correct++;
        }
        return (double)correct / testData.Count;
    }

    /// <summary>Xuất trọng số của cả 5 bộ phân loại ra CSV để đưa vào báo cáo / phân tích.</summary>
    public void ExportWeights(string path)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("HanhDong,TrongSo(dac_trung_cuoi_cung_la_bias)");
        for (int c = 0; c < NumClasses; c++)
        {
            string weightsStr = string.Join(";", _classifiers[c].Weights.Select(w => w.ToString("F4")));
            writer.WriteLine($"{(ActionType)c},{weightsStr}");
        }
    }
}
