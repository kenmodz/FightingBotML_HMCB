"""
backend/main.py
----------------
Backend server đóng vai trò cầu nối thời gian thực (real-time bridge) giữa
game Unity và mô hình AI (FightingBotML). Trong khi app/main.py cung cấp
REST API (/predict) phù hợp cho việc gọi rời rạc hoặc tích hợp bên ngoài,
backend này mở một WebSocket để Unity gửi trạng thái trận đấu mỗi khung hình
(hoặc mỗi vài khung hình) và nhận lại hành động ngay lập tức với độ trễ thấp.

Kiến trúc:

    Unity (C#, WebSocketSharp) <--WebSocket--> backend/main.py <--load--> model.pkl

Chạy thử cục bộ:
    uvicorn backend.main:app --host 0.0.0.0 --port 8001 --reload

Giao thức WebSocket (JSON), Unity gửi:
    {
        "bot_hp": 80, "enemy_hp": 60, "distance": 1.2,
        "bot_x": 3.0, "enemy_x": 4.2, "bot_cooldown": 0,
        "enemy_last_action": "punch", "time_left": 45.0
    }

Backend trả về:
    {"action": "block", "confidence": 0.82}
"""

import os
import json
import joblib
import numpy as np
from fastapi import FastAPI, WebSocket, WebSocketDisconnect

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
APP_DIR = os.path.join(BASE_DIR, "..", "app")
MODEL_PATH = os.path.join(APP_DIR, "model.pkl")
PREPROCESSOR_PATH = os.path.join(APP_DIR, "preprocessor.pkl")

REQUIRED_FIELDS = [
    "bot_hp", "enemy_hp", "distance", "bot_x", "enemy_x",
    "bot_cooldown", "enemy_last_action", "time_left",
]

app = FastAPI(title="FightingBotML Realtime Backend")

model = None
preprocessor = None


@app.on_event("startup")
def load_artifacts():
    global model, preprocessor
    if not os.path.exists(MODEL_PATH) or not os.path.exists(PREPROCESSOR_PATH):
        raise RuntimeError(
            "Không tìm thấy model.pkl / preprocessor.pkl. "
            "Hãy chạy training/train.py trước."
        )
    model = joblib.load(MODEL_PATH)
    preprocessor = joblib.load(PREPROCESSOR_PATH)
    print("[backend] Đã tải model.pkl và preprocessor.pkl.")


def predict_action(state: dict) -> dict:
    missing = [f for f in REQUIRED_FIELDS if f not in state]
    if missing:
        return {"error": f"Thiếu trường dữ liệu: {missing}"}

    numeric_features = preprocessor["numeric_features"]
    try:
        numeric_values = [[float(state[f]) for f in numeric_features]]
    except (TypeError, ValueError):
        return {"error": "Một trong các trường số (numeric) không hợp lệ."}

    numeric_scaled = preprocessor["scaler"].transform(numeric_values)

    try:
        enemy_action_encoded = preprocessor["enemy_action_encoder"].transform(
            [state["enemy_last_action"]]
        ).reshape(-1, 1)
    except ValueError:
        return {"error": f"enemy_last_action không hợp lệ: {state['enemy_last_action']}"}

    X = np.hstack([numeric_scaled, enemy_action_encoded])
    proba = model.predict_proba(X)[0]
    classes = preprocessor["label_encoder"].classes_
    pred_idx = int(np.argmax(proba))

    return {
        "action": str(classes[pred_idx]),
        "confidence": round(float(proba[pred_idx]), 4),
    }


@app.get("/health")
def health():
    return {"status": "ok", "model_loaded": model is not None}


@app.websocket("/ws/bot")
async def bot_stream(websocket: WebSocket):
    """
    Kênh WebSocket real-time: Unity kết nối vào đây, gửi trạng thái mỗi
    khung hình / mỗi tick, và nhận về hành động ngay lập tức.
    """
    await websocket.accept()
    print("[backend] Client Unity đã kết nối tới /ws/bot")
    try:
        while True:
            raw_message = await websocket.receive_text()
            try:
                state = json.loads(raw_message)
            except json.JSONDecodeError:
                await websocket.send_text(json.dumps({"error": "JSON không hợp lệ"}))
                continue

            result = predict_action(state)
            await websocket.send_text(json.dumps(result, ensure_ascii=False))
    except WebSocketDisconnect:
        print("[backend] Client Unity đã ngắt kết nối.")
