# Unity/FightingGame

Thư mục này chứa phần tích hợp phía **Unity** cho dự án FightingBotML.

> Lưu ý: đây **không phải** là một project Unity đầy đủ (không bao gồm
> `Assets/`, `ProjectSettings/`, scene, sprite, animation...) vì những file đó
> cần được tạo và chỉnh sửa trực tiếp trong Unity Editor. Thư mục này chỉ
> cung cấp **script mẫu** để bạn copy vào project Unity có sẵn (hoặc project
> mới) và kết nối nó với AI bot.

## Nội dung

- `BotAIClient.cs` — script C# (MonoBehaviour) mẫu, kết nối tới backend
  qua WebSocket (`ws://localhost:8001/ws/bot`), gửi trạng thái trận đấu mỗi
  khung hình và nhận lại hành động để điều khiển bot.

## Cách tích hợp vào project Unity của bạn

1. Tạo (hoặc mở) project Unity 2D/3D cho game đối kháng của bạn.
2. Cài package **NativeWebSocket** qua Package Manager:
   - Window → Package Manager → "+" → *Add package from git URL*
   - `https://github.com/endel/NativeWebSocket.git#upm`
3. Copy `BotAIClient.cs` vào `Assets/Scripts/` trong project của bạn.
4. Gắn script `BotAIClient` vào GameObject của bot (nhân vật do AI điều khiển).
5. Trong Inspector, gán:
   - `Bot Transform` → Transform của nhân vật bot
   - `Enemy Transform` → Transform của đối thủ (người chơi hoặc bot khác)
   - `Backend Url` → địa chỉ WebSocket của backend (mặc định `ws://localhost:8001/ws/bot`)
6. Thay các dòng `// botController.Xxx();` trong hàm `ExecuteAction()` bằng
   lệnh điều khiển nhân vật thật (Animator, Rigidbody2D, CharacterController...).
7. Chạy backend trước khi Play trong Unity:
   ```bash
   uvicorn backend.main:app --host 0.0.0.0 --port 8001
   ```

## Kiến trúc giao tiếp

```
 Unity (C#)  --WebSocket JSON-->  backend/main.py  --load-->  model.pkl
     ^                                  |
     |______________ action ___________|
```

Mỗi ~0.15s, Unity gửi một JSON mô tả trạng thái trận đấu hiện tại
(HP, khoảng cách, vị trí, cooldown, hành động gần nhất của đối thủ, thời
gian còn lại). Backend chạy mô hình đã huấn luyện và trả về hành động kèm
độ tin cậy (confidence). Unity ánh xạ hành động đó sang animation/di chuyển
tương ứng cho bot.

## Gợi ý mở rộng

- Thay RandomForest bằng mô hình Reinforcement Learning (PPO/DQN) huấn
  luyện trực tiếp trong môi trường Unity với **Unity ML-Agents** để bot
  học qua tự chơi (self-play) thay vì học theo luật chuyên gia.
- Thêm hàng đợi (buffer) N khung hình gần nhất làm input cho mô hình để
  bot "nhìn thấy" nhịp độ trận đấu (giống LSTM/Transformer trên chuỗi thời gian).
