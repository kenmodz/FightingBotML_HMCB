"""
train.py
--------
Huấn luyện mô hình phân loại hành động (action classifier) cho AI Bot
trong game đối kháng.

Input đặc trưng (features):
    bot_hp, enemy_hp, distance, bot_x, enemy_x,
    bot_cooldown, enemy_last_action (categorical), time_left

Output (label):
    action  in {idle, move_left, move_right, jump, punch, kick, block, special}

Mô hình: RandomForestClassifier (đủ mạnh, nhanh, dễ triển khai, không cần GPU
và phù hợp để suy luận real-time trong game).

Kết quả huấn luyện được lưu lại thành:
    - model.pkl        : mô hình đã huấn luyện
    - preprocessor.pkl : bộ tiền xử lý (scaler + encoder) để dùng lại lúc suy luận
"""

import os
import json
import joblib
import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestClassifier
from sklearn.preprocessing import StandardScaler, LabelEncoder
from sklearn.model_selection import train_test_split

NUMERIC_FEATURES = [
    "bot_hp", "enemy_hp", "distance", "bot_x", "enemy_x",
    "bot_cooldown", "time_left",
]
CATEGORICAL_FEATURE = "enemy_last_action"
TARGET = "action"

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
DATASET_DIR = os.path.join(BASE_DIR, "..", "dataset")
APP_DIR = os.path.join(BASE_DIR, "..", "app")


def load_data():
    train_path = os.path.join(DATASET_DIR, "train.csv")
    df = pd.read_csv(train_path)
    return df


def build_preprocessor(df: pd.DataFrame):
    scaler = StandardScaler()
    scaler.fit(df[NUMERIC_FEATURES])

    enemy_action_encoder = LabelEncoder()
    enemy_action_encoder.fit(df[CATEGORICAL_FEATURE])

    label_encoder = LabelEncoder()
    label_encoder.fit(df[TARGET])

    preprocessor = {
        "scaler": scaler,
        "enemy_action_encoder": enemy_action_encoder,
        "label_encoder": label_encoder,
        "numeric_features": NUMERIC_FEATURES,
        "categorical_feature": CATEGORICAL_FEATURE,
    }
    return preprocessor


def transform(df: pd.DataFrame, preprocessor: dict):
    numeric_part = preprocessor["scaler"].transform(df[NUMERIC_FEATURES])
    categorical_part = preprocessor["enemy_action_encoder"].transform(
        df[CATEGORICAL_FEATURE]
    ).reshape(-1, 1)
    X = np.hstack([numeric_part, categorical_part])
    return X


def main():
    print("Đang tải dữ liệu huấn luyện...")
    df = load_data()

    print("Đang xây dựng bộ tiền xử lý (scaler + encoders)...")
    preprocessor = build_preprocessor(df)

    X = transform(df, preprocessor)
    y = preprocessor["label_encoder"].transform(df[TARGET])

    X_train, X_val, y_train, y_val = train_test_split(
        X, y, test_size=0.15, random_state=42, stratify=y
    )

    print("Đang huấn luyện RandomForestClassifier...")
    model = RandomForestClassifier(
        n_estimators=120,
        max_depth=10,
        min_samples_leaf=5,
        class_weight="balanced",
        n_jobs=-1,
        random_state=42,
    )
    model.fit(X_train, y_train)

    train_acc = model.score(X_train, y_train)
    val_acc = model.score(X_val, y_val)
    print(f"Độ chính xác trên tập train: {train_acc:.4f}")
    print(f"Độ chính xác trên tập validation: {val_acc:.4f}")

    # Lưu model + preprocessor vào cả training/ và app/ (để backend load trực tiếp)
    os.makedirs(APP_DIR, exist_ok=True)

    model_path = os.path.join(APP_DIR, "model.pkl")
    preproc_path = os.path.join(APP_DIR, "preprocessor.pkl")
    joblib.dump(model, model_path)
    joblib.dump(preprocessor, preproc_path)
    print(f"Đã lưu mô hình tại: {model_path}")
    print(f"Đã lưu bộ tiền xử lý tại: {preproc_path}")

    # lưu thêm feature importance để tham khảo
    importances = dict(zip(
        NUMERIC_FEATURES + [CATEGORICAL_FEATURE],
        model.feature_importances_.round(4).tolist()
    ))
    with open(os.path.join(BASE_DIR, "feature_importance.json"), "w", encoding="utf-8") as f:
        json.dump(importances, f, ensure_ascii=False, indent=2)
    print("\nMức độ quan trọng của từng đặc trưng:")
    for k, v in sorted(importances.items(), key=lambda kv: -kv[1]):
        print(f"  {k:20s}: {v}")


if __name__ == "__main__":
    main()
