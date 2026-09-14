using System.Text;
using FightingBotML;

// ===== Thiết lập để hiển thị đúng tiếng Việt (UTF-8) trên console =====
// - OutputEncoding/InputEncoding: đảm bảo cả in ra và đọc vào đều dùng UTF-8
//   (quan trọng khi người dùng gõ tiếng Việt có dấu ở các phần nhập tên/tùy chọn sau này).
// - Trên Windows, một số terminal cũ (cmd.exe) mặc định dùng code page 850/1252 nên chữ có dấu
//   sẽ hiển thị sai; đoạn try/catch dưới đây chủ động chuyển code page hệ thống sang 65001 (UTF-8)
//   nếu đang chạy trên Windows để tránh lỗi hiển thị (không ảnh hưởng gì khi chạy trên Linux/macOS).
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

if (OperatingSystem.IsWindows())
{
    try
    {
        // Chuyển code page console sang UTF-8 (65001) để glyph tiếng Việt không bị vỡ font.
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c chcp 65001",
            CreateNoWindow = true,
            UseShellExecute = false
        })?.WaitForExit();
    }
    catch
    {
        // Nếu không đổi được code page (ví dụ môi trường hạn chế quyền), chương trình vẫn
        // tiếp tục chạy bình thường với thiết lập Encoding ở trên.
    }
}

MultiClassSvm? model = null;
List<DatasetGenerator.Sample>? testSet = null;

while (true)
{
    ConsoleUI.PrintHeader("AI BOT CHO GAME ĐỐI KHÁNG 1vs1 - SUPPORT VECTOR MACHINE");
    ConsoleUI.PrintMenuItem("1", "Sinh dữ liệu (gán nhãn bởi chuyên gia luật) và huấn luyện SVM");
    ConsoleUI.PrintMenuItem("2", "Đánh giá độ chính xác trên tập kiểm tra");
    ConsoleUI.PrintMenuItem("3", "Xem Bot (SVM) thi đấu với đối thủ kịch bản");
    ConsoleUI.PrintMenuItem("4", "Người chơi tự điều khiển, đấu với Bot (SVM)");
    ConsoleUI.PrintMenuItem("5", "Xuất trọng số mô hình ra file CSV (svm_weights.csv)");
    ConsoleUI.PrintMenuItem("0", "Thoát");
    ConsoleUI.PrintPrompt("Chọn: ");

    string? choice = Console.ReadLine();
    Console.WriteLine();

    switch (choice)
    {
        case "1":
            (model, testSet) = TrainModel();
            break;
        case "2":
            if (model == null || testSet == null)
                ConsoleUI.PrintWarning("Bạn cần huấn luyện mô hình ở mục 1 trước.");
            else
                EvaluateModel(model, testSet);
            break;
        case "3":
            if (model == null)
                ConsoleUI.PrintWarning("Bạn cần huấn luyện mô hình ở mục 1 trước.");
            else
                WatchDemo(model);
            break;
        case "4":
            if (model == null)
                ConsoleUI.PrintWarning("Bạn cần huấn luyện mô hình ở mục 1 trước.");
            else
                PlayAgainstBot(model);
            break;
        case "5":
            if (model == null)
                ConsoleUI.PrintWarning("Bạn cần huấn luyện mô hình ở mục 1 trước.");
            else
            {
                model.ExportWeights("svm_weights.csv");
                ConsoleUI.PrintSuccess("Đã xuất trọng số ra file svm_weights.csv");
            }
            break;
        case "0":
            return;
        default:
            ConsoleUI.PrintError("Lựa chọn không hợp lệ.");
            break;
    }
}

static (MultiClassSvm, List<DatasetGenerator.Sample>) TrainModel()
{
    ConsoleUI.PrintPrompt("Số mẫu dữ liệu sinh ra (gợi ý 5000): ");
    if (!int.TryParse(Console.ReadLine(), out int n) || n <= 0) n = 5000;

    ConsoleUI.PrintPrompt("Số epoch huấn luyện (gợi ý 30): ");
    if (!int.TryParse(Console.ReadLine(), out int epochs) || epochs <= 0) epochs = 30;

    var rng = new Random(42);
    var allData = DatasetGenerator.Generate(n, rng);

    // Xáo trộn và chia tập Train (80%) / Test (20%)
    var shuffled = allData.OrderBy(_ => rng.Next()).ToList();
    int splitIndex = (int)(shuffled.Count * 0.8);
    var trainSet = shuffled.Take(splitIndex).ToList();
    var testSet = shuffled.Skip(splitIndex).ToList();

    ConsoleUI.PrintInfo($"Đã sinh {allData.Count} mẫu: {trainSet.Count} mẫu huấn luyện, {testSet.Count} mẫu kiểm tra.");
    ConsoleUI.PrintInfo("Đang huấn luyện 5 bộ phân loại SVM nhị phân (One-vs-Rest)...");

    var model = new MultiClassSvm(lambda: 0.01);
    model.Train(trainSet, epochs);

    ConsoleUI.PrintSuccess("Huấn luyện xong.");
    EvaluateModel(model, testSet);

    return (model, testSet);
}

static void EvaluateModel(MultiClassSvm model, List<DatasetGenerator.Sample> testSet)
{
    double acc = model.Evaluate(testSet);
    Console.WriteLine();
    ConsoleUI.WriteLine(
        $"Độ chính xác tổng thể trên tập kiểm tra: {acc * 100:F2}% ({testSet.Count} mẫu)",
        acc >= 0.8 ? ConsoleColor.Green : acc >= 0.5 ? ConsoleColor.Yellow : ConsoleColor.Red);

    var correctByClass = new int[MultiClassSvm.NumClasses];
    var totalByClass = new int[MultiClassSvm.NumClasses];

    foreach (var sample in testSet)
    {
        int predicted = (int)model.PredictAction(sample.Features);
        totalByClass[sample.Label]++;
        if (predicted == sample.Label) correctByClass[sample.Label]++;
    }

    Console.WriteLine("Độ chính xác theo từng hành động (đánh giá riêng từng lớp):");
    for (int c = 0; c < MultiClassSvm.NumClasses; c++)
    {
        string name = ((ActionType)c).ToString();
        if (totalByClass[c] == 0)
        {
            ConsoleUI.PrintWarning($"  {name,-13}: không có mẫu nào trong tập kiểm tra");
        }
        else
        {
            double classAcc = 100.0 * correctByClass[c] / totalByClass[c];
            ConsoleColor color = classAcc >= 80 ? ConsoleColor.Green : classAcc >= 50 ? ConsoleColor.Yellow : ConsoleColor.Red;
            Console.Write($"  {name,-13}: ");
            ConsoleUI.WriteLine($"{classAcc,6:F1}%  ({correctByClass[c]}/{totalByClass[c]} mẫu)", color);
        }
    }
}

static void WatchDemo(MultiClassSvm model)
{
    var env = new BattleEnvironment("Bot(SVM)", "ĐốiThủKịchBản");
    var rng = new Random();
    env.Reset();
    bool done = false;

    ConsoleUI.PrintInfo("--- Bắt đầu trận đấu minh họa (Bot dùng SVM đã huấn luyện) ---");
    while (!done)
    {
        var (distance, ownHp, enemyHp, onCooldown, enemyJustAttacked) = env.GetRawStateA();
        double[] features = FeatureExtractor.Extract(distance, ownHp, enemyHp, onCooldown, enemyJustAttacked);
        ActionType actionA = model.PredictAction(features);
        ActionType actionB = ScriptedOpponent.ChooseAction(env.Distance, env.FighterB.HP, env.FighterA.HP, rng);

        var result = env.Step(actionA, actionB);

        Console.Write($"Lượt {env.TurnCount,3}: ");
        ConsoleUI.Write($"Bot->{actionA,-12}", ConsoleColor.Cyan);
        Console.Write(" | ");
        ConsoleUI.Write($"ĐốiThủ->{actionB,-12}", ConsoleColor.Magenta);
        Console.Write($" | KhoảngCách={env.Distance,2} | ");
        ConsoleUI.Write($"HP Bot={env.FighterA.HP,3}", ConsoleUI.HpColor(env.FighterA.HP, Fighter.MaxHP));
        Console.Write(" ");
        ConsoleUI.WriteLine($"HP ĐốiThủ={env.FighterB.HP,3}", ConsoleUI.HpColor(env.FighterB.HP, Fighter.MaxHP));

        done = result.Done;
        if (done)
        {
            (string text, ConsoleColor color) = result.Outcome switch
            {
                MatchOutcome.AWins => (">>> BOT (SVM) THẮNG! <<<", ConsoleColor.Green),
                MatchOutcome.BWins => (">>> Đối thủ kịch bản thắng <<<", ConsoleColor.Red),
                _ => (">>> Hòa <<<", ConsoleColor.Yellow)
            };
            ConsoleUI.PrintOutcome(text, color);
        }
    }
}

static void PlayAgainstBot(MultiClassSvm model)
{
    var env = new BattleEnvironment("Bot(SVM)", "NgườiChơi");
    env.Reset();
    bool done = false;

    ConsoleUI.PrintInfo("Bạn điều khiển 'NgườiChơi'. Các lựa chọn hành động:");
    ConsoleUI.PrintMenuItem("1", "Attack (Tấn công)");
    ConsoleUI.PrintMenuItem("2", "Block (Đỡ đòn)");
    ConsoleUI.PrintMenuItem("3", "Dodge (Né đòn)");
    ConsoleUI.PrintMenuItem("4", "MoveForward (Tiến)");
    ConsoleUI.PrintMenuItem("5", "MoveBackward (Lùi)");

    while (!done)
    {
        Console.WriteLine();
        ConsoleUI.WriteLine($"[Lượt {env.TurnCount + 1}] Khoảng cách = {env.Distance}", ConsoleColor.DarkCyan);
        ConsoleUI.PrintHpLine("Bạn", env.FighterB.HP, Fighter.MaxHP);
        ConsoleUI.PrintHpLine("Bot", env.FighterA.HP, Fighter.MaxHP);
        ConsoleUI.PrintPrompt("Chọn hành động của bạn (1-5): ");
        string? input = Console.ReadLine();

        ActionType playerAction = input switch
        {
            "1" => ActionType.Attack,
            "2" => ActionType.Block,
            "3" => ActionType.Dodge,
            "4" => ActionType.MoveForward,
            "5" => ActionType.MoveBackward,
            _ => ActionType.Block
        };

        var (distance, ownHp, enemyHp, onCooldown, enemyJustAttacked) = env.GetRawStateA();
        double[] features = FeatureExtractor.Extract(distance, ownHp, enemyHp, onCooldown, enemyJustAttacked);
        ActionType botAction = model.PredictAction(features);

        var result = env.Step(botAction, playerAction);
        Console.Write("Bot chọn: ");
        ConsoleUI.WriteLine(botAction.ToString(), ConsoleColor.Cyan);

        done = result.Done;
        if (done)
        {
            (string text, ConsoleColor color) = result.Outcome switch
            {
                MatchOutcome.AWins => (">>> BOT THẮNG BẠN! <<<", ConsoleColor.Red),
                MatchOutcome.BWins => (">>> BẠN ĐÃ THẮNG BOT! <<<", ConsoleColor.Green),
                _ => (">>> Hòa <<<", ConsoleColor.Yellow)
            };
            ConsoleUI.PrintOutcome(text, color);
        }
    }
}
