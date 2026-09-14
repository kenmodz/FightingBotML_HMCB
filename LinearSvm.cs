namespace FightingBotML;

/// <summary>
/// SVM TUYẾN TÍNH NHỊ PHÂN (Linear Binary Support Vector Machine).
///
/// Ý tưởng cốt lõi của SVM: tìm một SIÊU PHẲNG (hyperplane) w·x + b = 0 phân tách hai lớp dữ
/// liệu (nhãn +1 và -1) sao cho LỀ (margin) — khoảng cách từ siêu phẳng đến các điểm dữ liệu gần
/// nhất của mỗi lớp (các "vector hỗ trợ" - support vectors) — là LỚN NHẤT có thể. Lề càng lớn,
/// mô hình càng có khả năng tổng quát hóa tốt cho dữ liệu mới chưa từng thấy.
///
/// Bài toán tối ưu (dạng "soft-margin" cho phép một số điểm bị phân loại sai/nằm trong lề):
///
///     minimize    (lambda/2) * ||w||^2  +  (1/n) * Σ max(0, 1 - y_i * (w·x_i + b))
///
///   - Số hạng (lambda/2)*||w||^2 : số hạng chính quy hóa (regularization), giúp lề rộng ra
///     và tránh overfitting. lambda càng lớn thì mô hình càng đơn giản (lề rộng, chấp nhận
///     nhiều lỗi hơn), lambda càng nhỏ thì mô hình càng cố "khớp" sát dữ liệu huấn luyện.
///     (Đây tương đương nghịch đảo của tham số C thường thấy trong các thư viện SVM khác.)
///   - Số hạng còn lại là HÀM MẤT MÁT HINGE LOSS: max(0, 1 - y*(w·x+b)) — bằng 0 nếu điểm dữ
///     liệu được phân loại đúng VÀ nằm ngoài lề; lớn hơn 0 nếu điểm bị phân loại sai hoặc nằm
///     trong vùng lề.
///
/// Cài đặt dưới đây dùng thuật toán PEGASOS (Shalev-Shwartz và cộng sự, 2007) — một dạng
/// Stochastic Sub-Gradient Descent: ở mỗi bước, chọn ngẫu nhiên 1 mẫu dữ liệu, tính gradient
/// của hàm mất mát trên riêng mẫu đó, rồi cập nhật trọng số w với tốc độ học giảm dần theo thời
/// gian (eta_t = 1 / (lambda * t)). Cách này đơn giản, không cần giải bài toán đối ngẫu
/// (dual problem) như SVM cổ điển, phù hợp để tự cài đặt từ đầu (from scratch) mà không cần
/// thư viện ngoài.
///
/// Đây là SVM với KERNEL TUYẾN TÍNH (linear kernel) — chỉ tìm được ranh giới quyết định là một
/// đường/mặt phẳng thẳng. Xem mục "Ưu điểm - Nhược điểm" trong README để biết hạn chế của lựa
/// chọn này so với kernel phi tuyến (RBF, polynomial).
/// </summary>
public class LinearSvm
{
    private double[] _weights = Array.Empty<double>();

    /// <summary>Hệ số chính quy hóa (regularization). Tương đương nghịch đảo của tham số C.</summary>
    public double Lambda { get; set; } = 0.01;

    public double[] Weights => _weights;

    /// <summary>
    /// Huấn luyện SVM nhị phân. Nhãn y phải là +1 hoặc -1.
    /// Trọng số bias (b) được cài đặt bằng cách thêm một đặc trưng hằng số = 1 vào cuối vector
    /// đặc trưng (kỹ thuật "augmented feature vector"), giúp đơn giản hóa code (b chính là
    /// trọng số cuối cùng của w).
    /// </summary>
    public void Train(List<(double[] Features, int Label)> data, int epochs)
    {
        if (data.Count == 0) return;

        int numWeights = data[0].Features.Length + 1; // +1 cho bias
        _weights = new double[numWeights];

        var rng = new Random(7);
        int t = 0;

        for (int epoch = 0; epoch < epochs; epoch++)
        {
            var shuffled = data.OrderBy(_ => rng.Next()).ToList();

            foreach (var (features, label) in shuffled)
            {
                t++;
                double eta = 1.0 / (Lambda * t);
                double[] xAug = Augment(features);
                double score = Dot(_weights, xAug);
                bool marginViolated = label * score < 1;

                for (int i = 0; i < _weights.Length; i++)
                {
                    double shrink = (1 - eta * Lambda) * _weights[i];
                    _weights[i] = marginViolated ? shrink + eta * label * xAug[i] : shrink;
                }
            }
        }
    }

    /// <summary>Giá trị hàm quyết định w·x + b. Dấu của giá trị này quyết định lớp dự đoán,
    /// độ lớn cho biết mức độ "tự tin" (khoảng cách tới siêu phẳng).</summary>
    public double Decision(double[] features) => Dot(_weights, Augment(features));

    public int PredictLabel(double[] features) => Decision(features) >= 0 ? 1 : -1;

    private static double[] Augment(double[] x)
    {
        var result = new double[x.Length + 1];
        Array.Copy(x, result, x.Length);
        result[x.Length] = 1.0; // dac trung hang so cho bias
        return result;
    }

    private static double Dot(double[] a, double[] b)
    {
        double sum = 0;
        for (int i = 0; i < a.Length; i++) sum += a[i] * b[i];
        return sum;
    }
}
