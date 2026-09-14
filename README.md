# FightingBotML 🥊🤖

**AI Bot cho game đối kháng** — huấn luyện một mô hình machine learning để
điều khiển nhân vật trong game đối kháng (fighting game) 2D, tích hợp với
Unity qua REST API / WebSocket.

## Ý tưởng dự án

Thay vì viết bot bằng bộ luật cứng (hard-coded if/else), dự án này huấn
luyện một mô hình phân loại (classifier) học cách chọn hành động
(`idle`, `move_left`, `move_right`, `jump`, `punch`, `kick`, `block`,
`special`) dựa trên trạng thái trận đấu hiện tại:

- Máu của bot và đối thủ (`bot_hp`, `enemy_hp`)
- Khoảng cách và vị trí của hai bên (`distance`, `bot_x`, `enemy_x`)
- Trạng thái hồi chiêu của bot (`bot_cooldown`)
- Hành động gần nhất của đối thủ (`enemy_last_action`)
- Thời gian còn lại của trận đấu (`time_left`)

Dữ liệu huấn luyện ban đầu được **sinh mô phỏng** từ một bộ luật "chuyên
gia" (xem `dataset/generate_dataset.py`) kèm nhiễu ngẫu nhiên để mô hình
tổng quát hoá tốt hơn. Bạn có thể thay dữ liệu này bằng log thật thu thập
từ người chơi hoặc từ Unity ML-Agents self-play.

## Cấu trúc thư mục

```
FightingBotML/
│
├── app/                    # REST API phục vụ suy luận (FastAPI)
│   ├── main.py
│   ├── model.pkl           # mô hình đã huấn luyện (RandomForest)
│   └── preprocessor.pkl    # scaler + label encoders
│
├── backend/                # Backend WebSocket realtime cho Unity
│   └── main.py
│
├── dataset/                # Dữ liệu huấn luyện / kiểm thử
│   ├── generate_dataset.py # script sinh dữ liệu mô phỏng
│   ├── fighting_data.csv   # toàn bộ dữ liệu sinh ra
│   ├── train.csv
│   └── test.csv
│
├── training/                # Huấn luyện & đánh giá mô hình
│   ├── train.py
│   └── evaluate.py
│
├── Unity/
│   └── FightingGame/        # Script C# mẫu để tích hợp vào project Unity
│       ├── BotAIClient.cs
│       └── README.md
│
├── Dockerfile
├── docker-compose.yml
├── requirements.txt
└── README.md
```

## Cài đặt

```bash
python -m venv venv
source venv/bin/activate       # Windows: venv\Scripts\activate
pip install -r requirements.txt
```

## Quy trình huấn luyện

```bash
# 1. Sinh dữ liệu mô phỏng (bỏ qua nếu đã có sẵn CSV trong dataset/)
cd dataset
python generate_dataset.py
cd ..

# 2. Huấn luyện mô hình -> tạo app/model.pkl và app/preprocessor.pkl
cd training
python train.py

# 3. Đánh giá mô hình trên tập test
python evaluate.py
cd ..
```

Kết quả tham khảo trên dữ liệu mô phỏng mặc định: **~73% accuracy** trên
tập test, với các hành động phổ biến (`idle`, `block`, `move_right`) được
dự đoán chính xác cao (~0.76–0.96 f1-score); các hành động hiếm gặp hơn
(`special`, `kick`) có độ chính xác thấp hơn do dữ liệu mất cân bằng —
đây là điểm bạn nên cải thiện khi thay bằng dữ liệu thật.

## Chạy thử API cục bộ

```bash
# REST API (dùng cho tích hợp rời rạc, thử nghiệm, tool khác)
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload

# Backend realtime cho Unity (WebSocket)
uvicorn backend.main:app --host 0.0.0.0 --port 8001 --reload
```

Thử gọi REST API:

```bash
curl -X POST http://localhost:8000/predict \
  -H "Content-Type: application/json" \
  -d '{
        "bot_hp": 80, "enemy_hp": 60, "distance": 1.2,
        "bot_x": 3.0, "enemy_x": 4.2, "bot_cooldown": 0,
        "enemy_last_action": "punch", "time_left": 45.0
      }'
```

Kết quả mẫu:

```json
{
  "action": "block",
  "confidence": 0.82,
  "probabilities": {
    "idle": 0.02, "move_left": 0.01, "move_right": 0.03, "jump": 0.02,
    "punch": 0.04, "kick": 0.03, "block": 0.82, "special": 0.03
  }
}
```

## Chạy bằng Docker

```bash
docker compose up --build
```

- REST API: `http://localhost:8000`
- WebSocket backend: `ws://localhost:8001/ws/bot`

## Tích hợp với Unity

Xem chi tiết trong [`Unity/FightingGame/README.md`](Unity/FightingGame/README.md).
Tóm tắt: Unity kết nối tới `backend/main.py` qua WebSocket, gửi trạng thái
trận đấu mỗi khung hình, nhận về hành động và ánh xạ sang animation/di chuyển
của nhân vật bot.

```
Unity (C#, script BotAIClient.cs)
        │  WebSocket JSON (trạng thái trận đấu)
        ▼
backend/main.py  ──load──▶  app/model.pkl + app/preprocessor.pkl
        │  JSON (hành động + độ tin cậy)
        ▼
Unity thực thi hành động (di chuyển / tấn công / đỡ đòn)
```

## Kiến trúc mô hình

- **Đầu vào:** 7 đặc trưng số (chuẩn hoá bằng `StandardScaler`) + 1 đặc
  trưng phân loại (`enemy_last_action`, mã hoá bằng `LabelEncoder`).
- **Mô hình:** `RandomForestClassifier` (300 cây, `class_weight="balanced"`
  để giảm ảnh hưởng của mất cân bằng nhãn).
- **Đầu ra:** 1 trong 8 hành động, kèm phân phối xác suất cho từng hành động.

## Hướng phát triển tiếp theo

- Thay dữ liệu mô phỏng bằng log trận đấu thật (thu thập từ người chơi
  hoặc bản build Unity đã gắn logger).
- Thử nghiệm mô hình mạnh hơn: Gradient Boosting (XGBoost/LightGBM), hoặc
  mạng nơ-ron nhỏ (MLP) nếu có nhiều dữ liệu hơn.
- Chuyển sang **Reinforcement Learning** với **Unity ML-Agents**, để bot
  tự học qua self-play thay vì bắt chước luật chuyên gia — sẽ giúp bot
  vượt qua giới hạn của chính "chuyên gia" dùng để sinh dữ liệu ban đầu.
- Thêm ngữ cảnh chuỗi thời gian (N khung hình gần nhất) làm đầu vào để mô
  hình "nhìn thấy" nhịp độ trận đấu, không chỉ trạng thái tức thời.

## Giấy phép

Dự án mẫu phục vụ mục đích học tập / nghiên cứu.
