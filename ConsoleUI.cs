namespace FightingBotML;

/// <summary>
/// Các hàm tiện ích để in ra console có màu sắc, giúp giao diện dòng lệnh dễ đọc hơn.
/// Toàn bộ màu sắc được reset về mặc định (Console.ResetColor) sau mỗi lần in để tránh
/// làm "lem màu" sang các dòng in tiếp theo.
/// </summary>
public static class ConsoleUI
{
    public static void WriteLine(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void Write(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }

    /// <summary>In tiêu đề chương trình với khung viền màu xanh dương (cyan).</summary>
    public static void PrintHeader(string title)
    {
        string border = new string('=', 54);
        Console.WriteLine();
        WriteLine(border, ConsoleColor.Cyan);
        WriteLine($" {title}", ConsoleColor.Cyan);
        WriteLine(border, ConsoleColor.Cyan);
    }

    /// <summary>In một mục menu: số lựa chọn màu vàng, mô tả màu trắng.</summary>
    public static void PrintMenuItem(string key, string description)
    {
        Write($"{key}. ", ConsoleColor.Yellow);
        Console.WriteLine(description);
    }

    public static void PrintPrompt(string text)
    {
        Write(text, ConsoleColor.Green);
    }

    public static void PrintError(string text) => WriteLine(text, ConsoleColor.Red);

    public static void PrintSuccess(string text) => WriteLine(text, ConsoleColor.Green);

    public static void PrintInfo(string text) => WriteLine(text, ConsoleColor.Gray);

    public static void PrintWarning(string text) => WriteLine(text, ConsoleColor.Yellow);

    /// <summary>Chọn màu theo phần trăm máu còn lại: xanh lá (khỏe) -> vàng (trung bình) -> đỏ (nguy hiểm).</summary>
    public static ConsoleColor HpColor(int hp, int maxHp)
    {
        double ratio = maxHp <= 0 ? 0 : (double)hp / maxHp;
        if (ratio > 0.6) return ConsoleColor.Green;
        if (ratio > 0.3) return ConsoleColor.Yellow;
        return ConsoleColor.Red;
    }

    /// <summary>Vẽ một thanh máu dạng [#####-----] có màu theo mức máu hiện tại.</summary>
    public static string HpBar(int hp, int maxHp, int width = 20)
    {
        int clampedHp = Math.Clamp(hp, 0, maxHp);
        int filled = maxHp <= 0 ? 0 : (int)Math.Round((double)clampedHp / maxHp * width);
        return "[" + new string('#', filled) + new string('-', width - filled) + $"] {clampedHp,3}/{maxHp}";
    }

    /// <summary>In tên đấu sĩ kèm thanh máu có màu tương ứng với lượng máu còn lại.</summary>
    public static void PrintHpLine(string name, int hp, int maxHp)
    {
        Console.Write($"  {name,-14}: ");
        WriteLine(HpBar(hp, maxHp), HpColor(hp, maxHp));
    }

    /// <summary>In kết quả trận đấu với màu phù hợp: xanh lá (thắng), đỏ (thua), vàng (hòa).</summary>
    public static void PrintOutcome(string text, ConsoleColor color)
    {
        Console.WriteLine();
        WriteLine(text, color);
    }
}
