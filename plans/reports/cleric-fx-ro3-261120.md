# Cleric FX kiểu Ragnarok 3 (buff dam, attack, hồi máu) - 20/11/2026

Yêu cầu: "sua lai skill buff dam, attack, hoi mau cua cleric dung hieu ung nhu ragnarok 3".

## Ánh xạ skill

| Người dùng nói | Skill trong code | Trước | Sau |
|---|---|---|---|
| buff dam | Spirit Blessing (`cleric.spirit-blessing`, idx 1, +15% ATK, 4 ô, 12 s) | `ClericBuffFx` frame vẽ vàng (gcast / gapply / gloop) | `ClericBlessingLightFx` (ClericBuffFx vẫn chạy tư thế giơ tay) |
| hồi máu | Heal (`cleric.heal`, idx 0, 220% ATK, đồng minh máu thấp nhất 6 ô) | choreo sprite `skills/heal` | `ClericLightSkills.Heal` |
| attack (skill) | Divine Light (`cleric.divine-light`, idx 3, vùng tròn r2 cách 3 ô, 300% + hồi 100%) | choreo sprite `skills/divine-light` | `ClericLightSkills.DivineLight` |
| attack (đánh thường) | combo 3 đạn + quả cầu nổ r26 (`ComboConfigs.ClericHits`, `BoltStyle.HolyArrow`) | bộ sprite `cleric-v2` | `ClericAttackLightFx` (đạn, chớp phép, va chạm, nổ) |

"attack" mơ hồ nên làm cả skill đánh Divine Light lẫn đánh thường. Divine Aegis (buff thủ) KHÔNG đổi, vẫn frame xanh.

## Hướng nhìn (ChatGPT, Tan Pham, chỉ chữ)

Chat: https://chatgpt.com/c/6b001902-3eec-83ec-8da9-29241e160c1c . ChatGPT nói rõ KHÔNG xác minh được VFX thật của RO3 2025 (số hạt, màu là thiết kế riêng); RO Mobile: Blessing = ánh sáng quanh người + hạt bay lên, duy trì bằng icon; Heal = loé xanh trắng quanh mục tiêu; Magnus = trận pháp hình chữ thập dưới đất. Ba ngôn ngữ: Blessing = ánh sáng truyền đi, Heal = ánh sáng tụ vào mục tiêu, Divine Light = thánh trận nở ra; đánh thường = giọt sáng nhanh, sạch.
Màu spec: #72C9A0, #A8E4C5, #E8FFF0, #B8F0D3, trắng, vàng kem #F3E8BA phụ. Luật raid: hiệu ứng người khác alpha 45%, hạt 35%, aura duy trì 15%; đạn người ngoài chỉ có đầu, không vệt.

## Đã làm

- Không sprite frame: quad shader (`LightBlob`, `LightBeamScroll`, `LightTrail`) + Particle System (`LightParticle`, `WantedFxParticles`) + TrailRenderer. 2 mask mới `holy-cross`, `holy-rune` (`Tools/build-light-skill-textures.py gen holy-cross holy-rune`).
- `ClericLightLook.cs` vật liệu + bảng màu, cờ `--cleric-fx legacy` (về hình thủ tục cũ). `ClericLightFx.cs` bộ phát timeline (quad mặt đất / trên / sau hero, hạt hẹn giờ, Dim cho người khác, `ClearAll` khi đổi map qua `ProxySpawner.ClearForTransfer`).
- Heal 550 ms: quầng + vòng đất, cột sáng + thập tự SAU hero, chớp, hạt bay lên. Divine Light 780 ms: vòng đúng bán kính strike (r = 2 ô), rune xoay, sóng ra mép, thập tự + chớp ở tâm, hạt; không cột cao. Đồng minh được hồi trong vùng: chớp nhỏ 180 ms (`AllySpark`). Hit-stop 40 ms + rung nhẹ cho người cast như choreo cũ.
- Blessing: rune + quầng dưới chân, sáng tụ trên tay (110-500 ms), xoắn hạt, dòng sáng (đầu + ribbon) tới từng đồng minh 180-460 ms, nhận buff = vòng + cột ngắn + thập tự sau người; duy trì 12 s = 1 sao nhỏ nhịp thở cạnh vai phía lưng (ngoài viền người, không chạm mặt) + 1 hạt / 1,3 s, mờ dần 300 ms cuối.
- Đánh thường: đầu sáng ngọc + vệt ribbon ~20 đv, chớp ở pha lê gậy; trúng = chớp + thập tự nhỏ + vòng đất + 4 hạt; nổ = vòng dừng đúng r26, quầng, thập tự, 14 hạt + 6 sao. Người khác: đạn không vệt, không chớp phép, trúng chỉ 1 chớp nhỏ.
- Thời gian: mọi hình bắt đầu 0 ms = lúc cast, khớp `SkillTable` (Heal / Divine Light `atMs` 0; Blessing theo mốc của ClericBuffFx). KHÔNG đổi số skill.
- `quietFx` bật cho đạn `HolyArrow` (quái không tự loé đĩa trắng) - cờ này đi trong sự kiện MonsterHit từ server: build lại zone server khi deploy (không bắt buộc cho chơi offline).
- Xoá (git rm kèm .meta): `fx/attack-v2/` + `vfx-attack-v2.json`; frame cast/loop/impact + json của `skills/heal`, `skills/divine-light`, `skills/spirit-blessing` (giữ `icon.png`, UI dùng); `fx/clericbuff/g*` (vàng). Choreo Heal / DivineLight bỏ khỏi `SkillChoreoDefsCleric`.
- `DebugCapture.Choreo`: thêm cảnh Heal (tự trừ 50% máu hero trước khi cast) và Divine Light.

## Kiểm tra

- `dotnet test` ZenoniaSkills 474 pass, ZenoniaNet 455 pass.
- Build từ bản sao: `C:/temp/player-cleric-fx/Zenonia.exe`.
- Chụp 60 fps (`--record-choreo spirit-blessing,heal,divine-light --zoom 60`, `--record-combo cleric --tp 13,21 --dummy-dist 48`) và xem từng khung. Lần đầu trên tuyết: lớp trắng / sữa / mint mất hình, tâm Divine Light loá trắng -> đổi lớp alpha sang ngọc bão hoà (Sổ lỗi). Sao duy trì bị tóc che -> ra ngoài viền, dải FX. Quái loé đĩa trắng -> quietFx.
- Đám đông 40 người (`wanted-boss-crowd-playtest.ps1 -Party -GodHero`, 3 bot Cleric buff + heal party, ~8 Cleric đánh thường, thư mục / cổng riêng), `--fps-log`, RTX 4050 laptop, uncapped:
  - FX mới (bản cuối, đã giảm FX người khác): 388-433 FPS, trung bình 2,31-2,57 ms, tệ nhất 6-12 ms.
  - `--cleric-fx legacy`: 435-544 FPS, trung bình 1,84-2,30 ms, tệ nhất 5-15 ms.
  - Chênh khoảng +0,3-0,5 ms / khung (2 lần chạy khác nhau, vị trí bot không giống hệt, nên chỉ là ước lượng). Bản đầu chưa giảm FX người khác: 2,6-2,9 ms.
- Ảnh: `docs/reference/cleric-fx-ro3/` (spirit-blessing-strip, heal-strip, divine-light-strip, basic-attack-strip, crowd-40-party-blessed).

## Chưa xem / còn lại

- Chưa xem trên map tối (hang, rừng) và đường đất sáng; chỉ tuyết Frost Hollow.
- Dòng sáng Blessing tới đồng minh và hiệu ứng nhận buff trên đồng minh: chỉ thấy gián tiếp trong đám đông (icon buff có, hình khó tách giữa 40 người); chưa có cảnh chụp riêng có đồng minh.
- Sao duy trì Blessing khá nhạt ở zoom mặc định (đúng spec "nhẹ" nhưng có thể khó thấy).
- Online thật với server Linux (chưa build lại: quietFx), điện thoại, WebGL, 2.5D.
- Tạo GameObject mỗi lần trúng đạn (chưa pool); nếu điện thoại chậm nên pool `ClericLightFx`.
- Không hỏi ChatGPT duyệt lại ảnh chụp.

## Câu hỏi

1. Sao duy trì Blessing nhạt: muốn rõ hơn (to / đậm) hay giữ nhẹ như RO (chủ yếu nhìn icon)?
2. Divine Aegis (buff thủ, frame xanh) có chuyển sang kiểu ánh sáng luôn không?
3. Các skill Cleric còn lại (Sanctuary, Guardian Spirit, Purification, Divine Ascension) vẫn sprite choreo: làm tiếp không?
