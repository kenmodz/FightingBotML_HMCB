// BotAIClient.cs
// ----------------
// Script mẫu (MonoBehaviour) để gắn vào GameObject của bot trong Unity.
// Kết nối tới backend FightingBotML qua WebSocket (ws://localhost:8001/ws/bot),
// gửi trạng thái trận đấu hiện tại mỗi khung hình (hoặc mỗi N khung hình)
// và nhận về hành động mà bot nên thực hiện.
//
// YÊU CẦU:
//   - Cài package "NativeWebSocket" hoặc "WebSocketSharp" vào project Unity
//     (khuyến nghị: com.endel.nativewebsocket qua Unity Package Manager - Git URL)
//   - Backend (backend/main.py) phải đang chạy: uvicorn backend.main:app --port 8001
//
// Đây là code MẪU minh hoạ luồng giao tiếp; cần điều chỉnh theo cấu trúc
// GameObject / animation / physics thực tế của game bạn.

using System;
using UnityEngine;
using NativeWebSocket; // https://github.com/endel/NativeWebSocket

[Serializable]
public class GameStatePayload
{
    public float bot_hp;
    public float enemy_hp;
    public float distance;
    public float bot_x;
    public float enemy_x;
    public int bot_cooldown;
    public string enemy_last_action;
    public float time_left;
}

[Serializable]
public class BotActionResponse
{
    public string action;
    public float confidence;
    public string error;
}

public class BotAIClient : MonoBehaviour
{
    [Header("Kết nối tới backend AI")]
    public string backendUrl = "ws://localhost:8001/ws/bot";

    [Header("Tham chiếu đối tượng trong scene")]
    public Transform botTransform;
    public Transform enemyTransform;

    [Header("Tần suất gửi trạng thái (giây)")]
    public float decisionInterval = 0.15f; // ~6-7 lần / giây, đủ nhanh cho combat

    private WebSocket _ws;
    private float _timer;
    private int _botCooldownFrames;
    private string _enemyLastAction = "idle";

    // Các script/component thực tế của bạn sẽ cung cấp các giá trị này
    private float BotHP => FighterStats.Instance.BotHP;
    private float EnemyHP => FighterStats.Instance.EnemyHP;
    private float TimeLeft => MatchTimer.Instance.SecondsRemaining;

    async void Start()
    {
        _ws = new WebSocket(backendUrl);

        _ws.OnOpen += () => Debug.Log("[BotAIClient] Đã kết nối tới backend AI.");
        _ws.OnError += (e) => Debug.LogError($"[BotAIClient] Lỗi WebSocket: {e}");
        _ws.OnClose += (e) => Debug.LogWarning("[BotAIClient] Kết nối tới backend đã đóng.");
        _ws.OnMessage += (bytes) =>
        {
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            BotActionResponse response = JsonUtility.FromJson<BotActionResponse>(json);
            if (!string.IsNullOrEmpty(response.error))
            {
                Debug.LogWarning($"[BotAIClient] Backend trả lỗi: {response.error}");
                return;
            }
            ExecuteAction(response.action, response.confidence);
        };

        await _ws.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _ws?.DispatchMessageQueue();
#endif
        _timer += Time.deltaTime;
        if (_timer >= decisionInterval)
        {
            _timer = 0f;
            SendGameState();
        }
    }

    void SendGameState()
    {
        if (_ws == null || _ws.State != WebSocketState.Open) return;

        float distance = Vector3.Distance(botTransform.position, enemyTransform.position);

        var payload = new GameStatePayload
        {
            bot_hp = BotHP,
            enemy_hp = EnemyHP,
            distance = distance,
            bot_x = botTransform.position.x,
            enemy_x = enemyTransform.position.x,
            bot_cooldown = _botCooldownFrames,
            enemy_last_action = _enemyLastAction,
            time_left = TimeLeft,
        };

        string json = JsonUtility.ToJson(payload);
        _ws.SendText(json);
    }

    // Gọi hàm này từ script theo dõi hành động của đối thủ (ví dụ EnemyController)
    public void ReportEnemyAction(string action)
    {
        _enemyLastAction = action;
    }

    void ExecuteAction(string action, float confidence)
    {
        // Ánh xạ hành động dự đoán sang animation / input thực tế của bot.
        // Thay thế phần dưới bằng logic điều khiển nhân vật thật của bạn
        // (Animator, CharacterController, Rigidbody2D, v.v.)
        switch (action)
        {
            case "move_left":
                // botController.MoveLeft();
                break;
            case "move_right":
                // botController.MoveRight();
                break;
            case "jump":
                // botController.Jump();
                break;
            case "punch":
                // botController.Punch();
                _botCooldownFrames = 3;
                break;
            case "kick":
                // botController.Kick();
                _botCooldownFrames = 3;
                break;
            case "special":
                // botController.SpecialMove();
                _botCooldownFrames = 6;
                break;
            case "block":
                // botController.Block();
                break;
            case "idle":
            default:
                // botController.Idle();
                break;
        }

        if (_botCooldownFrames > 0) _botCooldownFrames--;
    }

    private async void OnApplicationQuit()
    {
        if (_ws != null) await _ws.Close();
    }
}
