"""
generate_dataset.py
--------------------
Sinh dữ liệu mô phỏng cho bài toán "AI Bot cho game đối kháng".

Mỗi dòng dữ liệu mô tả một "khung hình quyết định" (decision frame) trong trận đấu:
  - Trạng thái của bot và đối thủ (HP, khoảng cách, vị trí, cooldown...)
  - Hành động gần nhất của đối thủ
  - Nhãn (label) là hành động mà một "chuyên gia" (rule-based expert) sẽ chọn
    trong tình huống đó.

Ý tưởng: dùng một bộ luật (heuristic) hợp lý để đóng vai "người chơi giỏi",
từ đó sinh dữ liệu có nhãn. Mô hình ML sau này sẽ học lại chính sách này
(và có thể tổng quát hoá tốt hơn nhờ nhiễu ngẫu nhiên được thêm vào).

Các hành động (action) có thể có:
    idle, move_left, move_right, jump, punch, kick, block, special
"""

import numpy as np
import pandas as pd

RNG = np.random.default_rng(42)

ACTIONS = ["idle", "move_left", "move_right", "jump", "punch", "kick", "block", "special"]
ENEMY_ACTIONS = ["idle", "move_left", "move_right", "jump", "punch", "kick", "block", "special"]

ARENA_WIDTH = 10.0  # chiều rộng sàn đấu (đơn vị game)


def expert_policy(bot_hp, enemy_hp, distance, bot_x, enemy_x,
                   bot_cooldown, enemy_last_action, time_left):
    """
    Bộ luật mô phỏng một 'chuyên gia' chơi đối kháng.
    Trả về hành động hợp lý nhất cho tình huống hiện tại.
    """
    # 1. Nếu đang trong cooldown (vừa ra đòn) -> ưu tiên phòng thủ / né
    if bot_cooldown > 0:
        if distance < 1.2 and enemy_last_action in ("punch", "kick", "special"):
            return "block"
        return "idle"

    # 2. Nếu đối thủ vừa tung đòn và mình ở gần -> block
    if distance < 1.5 and enemy_last_action in ("punch", "kick", "special"):
        return "block" if RNG.random() < 0.75 else "jump"

    # 3. Nếu máu mình thấp và máu địch cao -> chơi phòng thủ, giữ khoảng cách
    if bot_hp < 30 and enemy_hp > 50:
        if distance < 2.0:
            return "move_left" if bot_x > enemy_x else "move_right"
        return "block" if RNG.random() < 0.4 else "idle"

    # 4. Nếu máu địch thấp -> dồn ép tấn công
    if enemy_hp < 25:
        if distance > 1.5:
            return "move_right" if bot_x < enemy_x else "move_left"
        return "special" if enemy_hp < 12 else ("kick" if RNG.random() < 0.5 else "punch")

    # 5. Ở khoảng cách xa -> tiến lại gần hoặc nhảy vào
    if distance > 3.0:
        approach = "move_right" if bot_x < enemy_x else "move_left"
        return "jump" if RNG.random() < 0.15 else approach

    # 6. Ở tầm trung -> áp sát dần
    if 1.6 <= distance <= 3.0:
        approach = "move_right" if bot_x < enemy_x else "move_left"
        return approach if RNG.random() < 0.7 else "jump"

    # 7. Ở tầm gần (trong tầm đánh) -> tấn công đa dạng
    if distance <= 1.6:
        roll = RNG.random()
        if roll < 0.40:
            return "punch"
        elif roll < 0.70:
            return "kick"
        elif roll < 0.85:
            return "block"
        elif roll < 0.95:
            return "special"
        else:
            return "jump"

    return "idle"


def simulate_match(n_frames: int, seed_offset: int = 0) -> pd.DataFrame:
    rng = np.random.default_rng(42 + seed_offset)
    rows = []

    bot_hp, enemy_hp = 100.0, 100.0
    bot_x, enemy_x = 2.0, 8.0
    bot_cooldown = 0
    enemy_last_action = "idle"
    time_left = 99.0

    for _ in range(n_frames):
        # reset trận đấu nếu kết thúc
        if bot_hp <= 0 or enemy_hp <= 0 or time_left <= 0:
            bot_hp, enemy_hp = 100.0, 100.0
            bot_x = float(rng.uniform(0, ARENA_WIDTH / 2))
            enemy_x = float(rng.uniform(ARENA_WIDTH / 2, ARENA_WIDTH))
            bot_cooldown = 0
            enemy_last_action = "idle"
            time_left = 99.0

        distance = abs(enemy_x - bot_x)

        action = expert_policy(
            bot_hp, enemy_hp, distance, bot_x, enemy_x,
            bot_cooldown, enemy_last_action, time_left
        )

        # thêm 6% nhiễu nhãn để mô hình học tổng quát hơn thay vì học vẹt
        if rng.random() < 0.06:
            action = rng.choice(ACTIONS)

        rows.append({
            "bot_hp": round(bot_hp, 1),
            "enemy_hp": round(enemy_hp, 1),
            "distance": round(distance, 2),
            "bot_x": round(bot_x, 2),
            "enemy_x": round(enemy_x, 2),
            "bot_cooldown": bot_cooldown,
            "enemy_last_action": enemy_last_action,
            "time_left": round(time_left, 1),
            "action": action,
        })

        # --- cập nhật trạng thái giả lập cho khung hình kế tiếp ---
        if action in ("punch", "kick", "special"):
            bot_cooldown = 3 if action != "special" else 6
            if distance <= 1.6 and rng.random() < 0.6:
                dmg = {"punch": 6, "kick": 9, "special": 18}[action]
                enemy_hp = max(0.0, enemy_hp - dmg)
        else:
            bot_cooldown = max(0, bot_cooldown - 1)

        if action == "move_left":
            bot_x = max(0.0, bot_x - 0.3)
        elif action == "move_right":
            bot_x = min(ARENA_WIDTH, bot_x + 0.3)

        # đối thủ hành động ngẫu nhiên đơn giản (mô phỏng đối thủ)
        enemy_last_action = rng.choice(ENEMY_ACTIONS,
                                        p=[0.15, 0.15, 0.15, 0.1, 0.2, 0.15, 0.05, 0.05])
        if enemy_last_action in ("punch", "kick", "special") and distance <= 1.6 and rng.random() < 0.5:
            dmg = {"punch": 6, "kick": 9, "special": 18}[enemy_last_action]
            bot_hp = max(0.0, bot_hp - dmg)
        elif enemy_last_action == "move_left":
            enemy_x = max(0.0, enemy_x - 0.3)
        elif enemy_last_action == "move_right":
            enemy_x = min(ARENA_WIDTH, enemy_x + 0.3)

        time_left = max(0.0, time_left - 0.1)

    return pd.DataFrame(rows)


def main():
    full_df = simulate_match(n_frames=20000, seed_offset=0)

    # xáo trộn rồi chia train/test 80/20
    full_df = full_df.sample(frac=1.0, random_state=42).reset_index(drop=True)
    split_idx = int(len(full_df) * 0.8)
    train_df = full_df.iloc[:split_idx].reset_index(drop=True)
    test_df = full_df.iloc[split_idx:].reset_index(drop=True)

    full_df.to_csv("fighting_data.csv", index=False)
    train_df.to_csv("train.csv", index=False)
    test_df.to_csv("test.csv", index=False)

    print(f"Đã sinh {len(full_df)} dòng dữ liệu.")
    print(f"  -> fighting_data.csv (toàn bộ): {len(full_df)} dòng")
    print(f"  -> train.csv: {len(train_df)} dòng")
    print(f"  -> test.csv : {len(test_df)} dòng")
    print("\nPhân bố nhãn (action):")
    print(full_df["action"].value_counts())


if __name__ == "__main__":
    main()
