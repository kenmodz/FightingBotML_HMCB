"""
app/main.py
-----------
FastAPI service để suy luận (inference) hành động cho AI Bot trong game
đối kháng. Đây là "bộ não" của bot: nhận trạng thái trận đấu hiện tại và
trả về hành động tiếp theo mà bot nên thực hiện.

Chạy thử cục bộ:
    uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload

Gọi thử API:
    curl -X POST http://localhost:8000/predict \
      -H "Content-Type: application/json" \
      -d '{
            "bot_hp": 80, "enemy_hp": 60, "distance": 1.2,
            "bot_x": 3.0, "enemy_x": 4.2, "bot_cooldown": 0,
            "enemy_last_action": "punch", "time_left": 45.0
          }'
"""

import os
import joblib
import numpy as np
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
MODEL_PATH = os.path.join(BASE_DIR, "model.pkl")
PREPROCESSOR_PATH = os.path.join(BASE_DIR, "preprocessor.pkl")

VALID_ENEMY_ACTIONS = [
    "idle", "move_left", "move_right", "jump", "punch", "kick", "block", "special"
]

app = FastAPI(
    title="FightingBotML API",
    description="AI Bot cho game đối kháng - dự đoán hành động tiếp theo dựa trên trạng thái trận đấu.",
    version="1.0.0",
)

# Cho phép Unity (hoặc bất kỳ client nào) gọi API từ trình duyệt/engine khác domain
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- load model & preprocessor lúc khởi động ---
model = None
preprocessor = None


@app.on_event("startup")
def load_artifacts():
    global model, preprocessor
    if not os.path.exists(MODEL_PATH) or not os.path.exists(PREPROCESSOR_PATH):
        raise RuntimeError(
            "Không tìm thấy model.pkl / preprocessor.pkl trong thư mục app/. "
            "Hãy chạy training/train.py trước để sinh ra các file này."
        )
    model = joblib.load(MODEL_PATH)
    preprocessor = joblib.load(PREPROCESSOR_PATH)
    print("Đã tải model.pkl và preprocessor.pkl thành công.")


class GameState(BaseModel):
    bot_hp: float = Field(..., ge=0, le=100, description="Máu hiện tại của bot (0-100)")
    enemy_hp: float = Field(..., ge=0, le=100, description="Máu hiện tại của đối thủ (0-100)")
    distance: float = Field(..., ge=0, description="Khoảng cách giữa bot và đối thủ")
    bot_x: float = Field(..., description="Vị trí X của bot trên sàn đấu")
    enemy_x: float = Field(..., description="Vị trí X của đối thủ trên sàn đấu")
    bot_cooldown: int = Field(..., ge=0, description="Số khung hình còn lại trước khi bot ra đòn tiếp")
    enemy_last_action: str = Field(..., description=f"Hành động gần nhất của đối thủ, một trong: {VALID_ENEMY_ACTIONS}")
    time_left: float = Field(..., ge=0, description="Thời gian còn lại của trận đấu (giây)")


class PredictionResponse(BaseModel):
    action: str
    confidence: float
    probabilities: dict


@app.get("/")
def root():
    return {
        "service": "FightingBotML",
        "status": "running",
        "endpoints": ["/predict", "/health", "/actions"],
    }


@app.get("/health")
def health():
    return {"status": "ok", "model_loaded": model is not None}


@app.get("/actions")
def list_actions():
    """Trả về danh sách các hành động mà bot có thể thực hiện."""
    if preprocessor is None:
        raise HTTPException(status_code=503, detail="Model chưa được tải.")
    return {"actions": preprocessor["label_encoder"].classes_.tolist()}


@app.post("/predict", response_model=PredictionResponse)
def predict(state: GameState):
    if model is None or preprocessor is None:
        raise HTTPException(status_code=503, detail="Model chưa được tải.")

    if state.enemy_last_action not in VALID_ENEMY_ACTIONS:
        raise HTTPException(
            status_code=400,
            detail=f"enemy_last_action không hợp lệ. Phải là một trong: {VALID_ENEMY_ACTIONS}",
        )

    numeric_features = preprocessor["numeric_features"]
    numeric_values = [[getattr(state, f) for f in numeric_features]]
    numeric_scaled = preprocessor["scaler"].transform(numeric_values)

    enemy_action_encoded = preprocessor["enemy_action_encoder"].transform(
        [state.enemy_last_action]
    ).reshape(-1, 1)

    X = np.hstack([numeric_scaled, enemy_action_encoded])

    proba = model.predict_proba(X)[0]
    classes = preprocessor["label_encoder"].classes_
    pred_idx = int(np.argmax(proba))
    action = classes[pred_idx]
    confidence = float(proba[pred_idx])

    probabilities = {cls: round(float(p), 4) for cls, p in zip(classes, proba)}

    return PredictionResponse(
        action=action,
        confidence=round(confidence, 4),
        probabilities=probabilities,
    )
