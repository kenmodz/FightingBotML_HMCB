"""
evaluate.py
-----------
Đánh giá mô hình AI Bot đã huấn luyện trên tập test.csv.
In ra classification report và ma trận nhầm lẫn (confusion matrix).
"""

import os
import joblib
import pandas as pd
from sklearn.metrics import classification_report, confusion_matrix, accuracy_score

from train import NUMERIC_FEATURES, CATEGORICAL_FEATURE, TARGET, transform

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
DATASET_DIR = os.path.join(BASE_DIR, "..", "dataset")
APP_DIR = os.path.join(BASE_DIR, "..", "app")


def main():
    print("Đang tải mô hình và bộ tiền xử lý...")
    model = joblib.load(os.path.join(APP_DIR, "model.pkl"))
    preprocessor = joblib.load(os.path.join(APP_DIR, "preprocessor.pkl"))

    print("Đang tải tập test...")
    test_df = pd.read_csv(os.path.join(DATASET_DIR, "test.csv"))

    X_test = transform(test_df, preprocessor)
    y_test = preprocessor["label_encoder"].transform(test_df[TARGET])

    y_pred = model.predict(X_test)

    acc = accuracy_score(y_test, y_pred)
    print(f"\n=== Độ chính xác trên tập test: {acc:.4f} ===\n")

    labels = preprocessor["label_encoder"].classes_
    print("=== Classification Report ===")
    print(classification_report(
        y_test, y_pred,
        target_names=labels,
        zero_division=0
    ))

    print("=== Confusion Matrix ===")
    cm = confusion_matrix(y_test, y_pred)
    cm_df = pd.DataFrame(cm, index=labels, columns=labels)
    print(cm_df)

    cm_path = os.path.join(BASE_DIR, "confusion_matrix.csv")
    cm_df.to_csv(cm_path)
    print(f"\nĐã lưu ma trận nhầm lẫn tại: {cm_path}")


if __name__ == "__main__":
    main()
