# Sửa test đánh Wanted Boss đông người (19/11/2026)

Branch `ro-fantasy-heroes`. Boss: Mire Hydra Queen, Sunken Reliquary 58x24, boss tile (45,12).

## Nguyên nhân bot "kẹt"

Lỗi không nằm ở map, mà ở chỗ bot chết:
- Bot và hero chết dưới tay boss, rồi hồi sinh ở điểm đầu map (3,3), sát cổng sang Emberfen. Bot đi thẳng về boss nên đâm vào tường. Lệnh né kẹt cho bot rẽ ngẫu nhiên, nên 12 bot đi lạc qua cổng sang `emberfenMarsh` (zone.log trận cũ).
- Hero của cửa sổ (lv 60, vũ khí khởi đầu, 818 HP) chết sau khoảng 1 phút. Camera theo hero về chỗ hồi sinh, nên trông như cả đám bị kẹt (`docs/reference/mire-hydra-queen-v2/crowd-before-hero-respawned-at-entrance.png`).
- Trận cũ: 15 phút máu boss chỉ giảm 4 %, chỉ 20–35/39 bot ở gần boss.

## Đã đổi

1. **Bot xuất hiện quanh boss**
   - `ZenoniaSwarm --free-ring <map> --center TX,TY --radius 3,6 --count N --skip TX,TY` đọc rows thật của map, tính cả chân prop (swarm biên dịch kèm `Assets/Scripts/Maps` như ZenoniaNet.Tests). Lệnh in N ô đi được, nối được tới boss, cách boss 3–6 ô, rải đều theo góc, không trùng ô.
   - Script tạo nhân vật đúng các ô đó (`dev-boost --x --y`). Hero của người dùng đứng ở ô (45,14), 2 ô phía nam boss, ô này không phát cho bot.
2. **`-GodBots`**: bot bất tử, mặc định BẬT (`-GodBots $false` để tắt).
   - Zone có cờ mới `--wanted-god-bots PREFIX[,PREFIX]`: hero có tên bắt đầu bằng PREFIX được `HeroController.God` (không nhận sát thương).
   - Zone chỉ nhận cờ khi đủ cả 4 điều kiện: bản build không phải release; có `--wanted-boss` (boss test cố định); mỗi prefix dài từ 2 ký tự; không có `--api-url` hoặc server-api chạy ngay trên máy này (loopback).
   - Server thật gọi server-api ở máy khác nên luôn từ chối cờ, và ghi lỗi vào log. Script dừng nếu không thấy dòng "GOD BOTS on".
   - Code: `NetLaunchArgs.WantedGodBotsAllowed` / `IsWantedGodBot`, `ServerHeroFactory.Build`, log trong `ZoneServer.ApplyWantedBossOverride`.
3. **`-GodHero`**: cờ riêng, mặc định TẮT. Hero của người dùng cũng bất tử, để camera không rời trận.
4. **Bot đi theo đường khi quay về boss**: thêm `--map <id>` cho swarm. Swarm tính bản đồ khoảng cách BFS từ ô boss, bot đi theo từng ô thay vì lao thẳng vào tường (`GoalStep`).
5. **Map**: không đổi. Lối vào 2 ô và sân boss không có ô chặn sai: khi bot không chết thì 39/39 bot ở cạnh boss suốt trận.

## Chạy thử

- Build mới: `D:\temp\player-hydra-god2\Zenonia.exe`, dựng từ bản sao `D:\temp\zu-copy-hydra`. Ổ D còn trống 202 GB.
- Trận đang chạy dùng `-GodHero`. Trong `swarm.log`: 39/39 bot gần boss ở mọi mốc. Máu boss 99 → 98 → 97 → 96 → 95 % sau 32 → 63 → 93 → 123 → 152 s, tức khoảng 1 % mỗi 30 s (trận cũ: 4 % trong 15 phút).
- Không bot nào đi qua cổng sang map khác.
- Ảnh:
  - `docs/reference/mire-hydra-queen-v2/crowd-39-god-bots-ring-start.png` (đầu trận);
  - `crowd-39-god-bots-3min.png` (phút thứ 3).
- `dotnet test ZenoniaNet.Tests`: 455/455.

Lệnh chạy lại (cần `DOTNET_ROOT=C:\Users\korri\.dotnet` và dotnet SDK 10 đứng đầu PATH):
```
.\Tools\wanted-boss-crowd-playtest.ps1 -Exe D:\temp\player-hydra-god2\Zenonia.exe -Boss mireHydraQueen [-GodHero] [-HpScale 30]
.\Tools\wanted-boss-crowd-playtest.ps1 -Stop
```

## Chưa xử lý / ghi chú

- Bảng máu boss trên client đứng ở `65,535 / 49,500,000 (0.1%)` sau vài đòn, trong khi snapshot server báo 95 %. Đây là giới hạn ushort trong hit event, script đã ghi chú từ trước. Lỗi có sẵn, chưa sửa.
- HpScale 150 với Mire Hydra Queen (49,5 triệu HP) cần khoảng 50 phút mới hạ được. Muốn xem cảnh boss chết thì dùng `-HpScale 30`.
- Bot bất tử nên không còn thấy bot chết. Muốn test cảnh bot chết thì chạy với `-GodBots $false`.

## Câu hỏi
- Có sửa luôn lỗi ushort của bảng máu boss không?
