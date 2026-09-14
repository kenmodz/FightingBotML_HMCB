# Dockerfile - FightingBotML
# Image dùng chung cho cả "app" (REST API) và "backend" (WebSocket realtime).
# Lệnh khởi động cụ thể được chỉ định riêng trong docker-compose.yml.

FROM python:3.11-slim

WORKDIR /workspace

# Cài đặt các gói hệ thống cần thiết (build tools cho một số thư viện ML)
RUN apt-get update && apt-get install -y --no-install-recommends \
        build-essential \
    && rm -rf /var/lib/apt/lists/*

# Cài đặt thư viện Python trước để tận dụng cache layer của Docker
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy toàn bộ mã nguồn dự án
COPY app/ ./app/
COPY backend/ ./backend/
COPY dataset/ ./dataset/
COPY training/ ./training/

# Nếu model.pkl / preprocessor.pkl chưa tồn tại, huấn luyện ngay lúc build
# (bỏ qua bước này nếu bạn đã copy sẵn model đã huấn luyện vào app/)
RUN if [ ! -f app/model.pkl ]; then \
        cd dataset && python generate_dataset.py && cd .. && \
        cd training && python train.py && cd ..; \
    fi

EXPOSE 8000 8001

# Mặc định chạy REST API (app/main.py); docker-compose sẽ override cho service backend
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
