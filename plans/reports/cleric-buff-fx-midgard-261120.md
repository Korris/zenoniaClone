# Cleric: làm lại FX 3 skill buff kiểu Midgard Senki / RO3 + Heal luôn hồi cả bản thân - 20/11/2026

Yêu cầu: "làm lại hiệu ứng skill buff máu, skill buff dam và buff defense, skill buff máu cho buff chính bản thân mình luôn".
Ánh xạ (SkillTable.Cleric): buff máu = Heal (idx 0, LowestAlly 6 ô), buff dam = Spirit Blessing (idx 1, AtkPct 15 %, 4 ô, 12 s), buff defense = Divine Aegis (idx 2, DefPct 15 %, 4 ô, 12 s).
Hướng nhìn: 8 quy tắc + bảng skill ở `docs/reference/cleric-fx-ro3/midgard-senki-vs-cleric-fx-chatgpt.md`, lớp Divine Aegis ở `ro3-midgard-fx-observation-chatgpt.md`. Giữ nguyên map.

## Heal hồi cả người cast (logic dùng chung với zone server)

- `Rpg/Skills/LowestAllyHealRule.cs`: mục tiêu = đồng minh bị thương có % máu thấp nhất (tính cả người cast); không ai bị thương thì mục tiêu = người cast (hết "Heal: no target in range"); mục tiêu là người khác thì người cast được hồi thêm.
- `SkillAim.LowestAlly` (SkillContext.cs) dùng luật đó; `SkillSupport.Collect` thêm người cast cho hiệu ứng LowestAlly. Mô tả skill: "Heal yourself and the lowest-HP ally within 6 tiles". Số skill KHÔNG đổi.
- Test `LowestAllyHealRuleTests` (5). ZenoniaSkills 479 pass, ZenoniaNet 455 pass.
- **Phải build lại zone server Linux** khi deploy (luật mục tiêu chạy ở authority).
- Bot AutoBattle vẫn chỉ Heal khi máu < 50 % (không đổi). Heal giờ không bao giờ bị từ chối: máu đầy vẫn tốn MP nếu bấm.

## FX (không sprite: quad shader + Particle System + TrailRenderer)

- Shader `LightBlob`: thêm `_Arc` (cung sáng chạy quanh vòng, ~30 % chiều dài, cũng làm sáng lớp alpha để tắt Bloom vẫn thấy) và `_Hollow` (mặt khiên rỗng giữa). Mặc định không đổi gì cho FX khác.
- `ClericLightLook`: bộ vật liệu mới `Buff*` / `Aegis*`: teal #35B69F / #48C9AE / dải #299C91 (alpha 0,55-0,8, phần cộng thấp), quầng mint #B3FFE5 0,22, kem vàng #FFE6A0 cho thập tự / cột / chớp / spark. Divine Light và đánh thường giữ bộ jade cũ.
- `ClericLightFx`: `.Arc()` cho lớp vòng; bậc đám đông `DimFor`: mình / lên mình 100 %, party 70 % (60 % hạt), người khác 35 % (25 % hạt).
- Heal (700 ms, hồi ở 0 ms như SkillTable): quầng mint + vòng teal có cung sáng chạy; cột + thập tự kem SAU hero; chớp kem nhỏ (12 đv, 0,6, sau hero, ~một nửa bản cũ, không phủ mặt); spark kem hai bên thân + hạt teal. Vẽ trên mục tiêu VÀ trên người cast (bản nhỏ hơn).
- Spirit Blessing (mốc ClericBuffFx 620 / 110 / 180-460 ms): quầng + 2 cung rune teal quay chậm + vòng teal quay ngược; sáng kem nhỏ trên tay; hạt teal + spark kem xoắn lên; dòng sáng đầu kem đuôi teal tới đồng minh; nhận buff = vòng teal + cột / thập tự / chớp kem sau người; sao kem duy trì 12 s cạnh vai.
- Divine Aegis (mới, `ClericAegisLightFx.cs`, thay frame xanh): viền khiên teal elip quanh người (ngoài silhouette, alpha ~0,6), mặt khiên rỗng giữa 0,2, 3 rune kem trên viền; cung sáng chạy 1 vòng khi dựng khiên (420 ms) và 180 ms + spark khi người đeo mất máu (đỡ đòn, đọc theo HP nên client cũng thấy); duy trì thì chỉ sáng mờ đều. Người cast: vòng teal + quầng dưới chân, sáng kem trên tay, dòng sáng tới đồng minh.
- `--cleric-fx legacy` giữ: Aegis frame vẽ cũ, Blessing chỉ tư thế, Heal / Divine Light thủ tục cũ.

## Build

- Exe: `D:/temp/player-cleric-buff/Zenonia.exe` (từ bản sao `D:/temp/zu-copy-cleric`).

## Chưa làm / chưa xem

- Người dùng tự thử trong game: KHÔNG chụp dải frame, không chụp đám đông, không đo FPS, không kiểm tắt Bloom (coordinator dặn bỏ qua). Ảnh "trước" (FX lượt 1) đã chụp một phần ở `D:/temp/cbfx/before-frost/` (Blessing, Aegis, Heal trên tuyết), không đưa vào docs.
- Online với server Linux (luật Heal mới), điện thoại, 2.5D.

## Câu hỏi

1. Heal khi cả nhóm đầy máu vẫn cast (tốn MP) lên chính mình: giữ vậy hay chặn khi chính mình cũng đầy máu?
2. Viền khiên Divine Aegis đứng TRƯỚC hero (chỉ quanh viền, giữa trong suốt): thấy vướng thì chuyển nửa trên ra sau hero?
3. Divine Light và đánh thường có chuyển sang bộ màu teal / kem mới cho đồng bộ không?
