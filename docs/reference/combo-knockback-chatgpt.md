# Combo knockback: chốt với ChatGPT (2026-11-15)

Chat: https://chatgpt.com/c/6af5faea-f11c-83ec-9e09-3c13537b61a2 (tài khoản Tay, chat World Map). Hỏi bằng chữ, không vẽ.

Vấn đề người dùng báo: mọi nhát đánh thường đều đẩy lùi quái, quái bị khoá suốt combo, không đánh lại được.

## Câu trả lời của ChatGPT (tóm tắt, giữ đủ số)

Chốt chung: chỉ nhát 4 đẩy lùi và ngắt đòn quái. Nhát 1-3 có phản ứng trúng đòn rất ngắn nhưng không khoá AI và không làm chậm windup, để quái phản công được giữa combo mà nhát cuối vẫn có lực.

1. Nhát 1-3: knockback 0; hit reaction 60 ms (chỉ hình ảnh); khoá AI 0 ms; không ngắt windup, không dừng hay làm chậm đồng hồ windup, không reset đòn của quái. Lý do: 60 ms chỉ để giật hình, quái vẫn đánh trúng nếu người chơi đứng trong tầm.
2. Nhát 4, quãng đẩy thực tế (1 ô = 16 đơn vị), đều ngắt windup:

   | Class | Đơn vị | Ô |
   |---|---|---|
   | Warrior | 24 | 1,5 |
   | Fighter | 22 | 1,38 |
   | Assassin | 14 | 0,88 |
   | Wizard | 18 | 1,13 |
   | Mechanic | 20 | 1,25 |
   | Cleric | 16 | 1,0 |

   Trượt vẫn 170 ms, giảm tốc dần. Nhát 4 trúng thì huỷ đòn quái đang chuẩn bị, hết trượt quái vào recovery 300 ms; không chạy tiếp đồng hồ windup cũ. Lý do: Warrior/Fighter đánh nặng, Assassin đẩy ngắn để nối đòn, đánh xa đẩy vừa đủ tạo khoảng cách.
3. Cận chiến và đánh xa: nhát 1-3 đều không đẩy. Nhát 4 cận chiến đẩy theo hướng đánh; đánh xa đẩy ra xa tâm vụ nổ (AoE). Chỉ nhát 4 ngắt windup. Lý do: cận chiến cần giữ quái trong tầm combo, đánh xa dùng nhát cuối để tự vệ.
4. Chí mạng: nhát 1-3 vẫn không đẩy; nhát 4 đẩy như thường, BỎ nhân đôi knockback; sát thương chí mạng giữ nguyên. Lý do: chí mạng ngẫu nhiên không nên đẩy quái ra khỏi tầm hay phá nhịp combo.
5. Skill: giữ knockback riêng của từng skill. Skill có knockback được ngắt windup; skill không có knockback chỉ ngắt nếu được thiết kế hiệu ứng Interrupt.
6. Boss: không hit reaction, knockback 0, không bị ngắt windup (kể cả nhát 4 và skill knockback); windup boss luôn chạy. Chỉ skill có Interrupt mới ngắt được boss. Lý do: boss phải buộc người chơi né thay vì đứng spam combo.

Kết quả mong muốn: đứng yên spam combo thì có thể ăn đòn ở nhát 2-3, nhưng nhát 4 vẫn mạnh và đã tay.

## Đã làm theo (code)

- `ComboConfigs.cs`: nhát 1-3 mọi class knockback 0. Nhát 4 đổi số để quãng đẩy (knockback x 1,35 x 0,17 s) khớp bảng: Warrior 110 (25), Fighter 95 (22), Assassin 61, Wizard 78, Mechanic 87, Cleric 70.
- Bỏ nhân đôi knockback khi chí mạng của đánh thường (`HeroController.Combat.cs`, `ComboProjectile.cs`). Skill giữ nguyên.
- `Monster.cs`: bị đẩy lúc đang windup thì huỷ đòn, recovery 300 ms sau khi trượt (`StatusRules.KnockInterruptRecoverMs`). Áp cho mọi knockback (nhát 4 và skill). Boss vẫn không bị đẩy nên không bị ngắt.
- Hit reaction 60 ms: không thêm, chớp trắng 100 ms có sẵn đã đủ, không khoá AI.

## Hỏi tiếp (2026-11-15, người dùng: "hỏi ChatGPT đi rồi làm")

Câu hỏi: (1) nhát 4 Assassin có 2 cú đâm cách 90 ms, cú nào đẩy; (2) có thêm giật hình ~60 ms cho nhát 1-3 không, và việc quái trúng đòn bị đổi sang khung đứng (che khung tung đòn) có làm khó đọc đòn quái không.

ChatGPT chốt:
1. Assassin: chỉ cú đâm 2 đẩy, tổng 14 đơn vị (0,875 ô), trượt 170 ms, ngắt windup, hồi 300 ms. Cú 1: có sát thương, knockback 0, không ngắt windup, không trượt. Lý do: cú 1 giữ quái trong tầm, cú 2 kết thúc dứt khoát, không reset cú trượt giữa hai cú.
2. KHÔNG thêm giật hình 60 ms (dịch sprite 0 px, hitstun khoá AI 0 ms). Giữ chớp trắng 110 ms, jitter, số damage, tia lửa, hit-stop 45 ms (nhát cuối 80 ms). Lý do: giật lùi 1-2 px trùng jitter, nhiễu hình mà không tốt hơn.
3. Không được che animation báo đòn: quái đứng yên / di chuyển thì được hiện khung trúng đòn; đang windup, đang tung đòn, đang recovery thì giữ nguyên pose (chớp trắng, jitter vẫn chạy), không dừng đồng hồ windup. Bị nhát 4 ngắt windup thì chuyển sang hit reaction + knockback. Boss cũng vậy (không thay pose khi windup / attack). Lý do: windup 350 ms, bị thay bằng khung đứng 110 ms thì người chơi mất tín hiệu cảnh báo.

Đã làm: `ComboHit.KnockbackFor(laterStab)` (`ComboSystem.cs`, cú đâm đầu của bước nhiều cú = 0) dùng trong `HeroController.MeleeStrike`; `Monster.Visuals.Render` bỏ điều kiện `flash <= 0` nên pose windup / đòn / follow-through không còn bị khung đứng che. Không thêm giật hình.

## Hỏi tiếp lần 3 (2026-11-15): hero trúng đòn có huỷ cú đâm 2 của Assassin không

Code cũ: hero trúng đòn (mất máu) thì huỷ cú đâm 2 đang chờ, combo không bị ngắt. ChatGPT chốt: cú đâm 2 LUÔN ra sau 90 ms (đòn đã cam kết), kể cả khi hero mất máu đang hurt 220 ms hay đỡ bằng khiên (0 damage); vẫn gây sát thương và đẩy 14 đơn vị. Chỉ huỷ khi bị khống chế thật (stun, knockdown, chết). Combo không ngắt. Lý do: hai cú cách 90 ms là một đòn, huỷ cú 2 làm mất đòn kết thúc, cảm giác đánh hụt.

Đã làm: `HeroController.TakeDamage` bỏ `stabLeft = 0` (chỉ còn khi chết); `TakeWantedSkillHit` huỷ cú 2 khi `StatusRules.CancelsPendingStab` (đóng băng, choáng, bị đẩy lùi); test `OnlyARealDisableDropsThePendingStab`.
