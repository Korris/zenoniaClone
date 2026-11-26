# Archer skill icons (26/11/2026)

Cùng kiểu bộ icon RO Soft Fantasy các class khác (`../../fighter/skill-icons-src/prompt-soft-fantasy-icons.md`). Tài khoản Tay ("Tân Korris", Plus), chat "Thiết kế Ranger Asteria" (chat thiết kế Ranger, giữ context): https://chatgpt.com/c/6b0794c9-1f18-83ec-ab80-b7e41e9ca172

Ảnh gửi kèm (theo thứ tự):
1. `style-ref-existing-skill-icons.png` (icon 128 px đang dùng của Warrior / Wizard / Mechanic / Cleric, ghép bằng script từ `Sprites/classes/<class>/skills/<slug>/icon.png`)
2. `Assets/ArtLibrary/ro-fantasy-heroes/src/archer-v3/turnaround-archer-v3-chatgpt-1.png`

Thứ tự = `SkillTable.Archer.cs` index 0-7. Game dùng `Sprites/classes/archer/skills/<slug>/icon.png` 128 px (`SkillIcons.Get`, slug = đuôi `SkillDef.Id`, nên Hunter's Focus là `hunter's-focus`). Nền #FF00FF (art xanh lá).

Cắt: `py -3 Tools/build-soft-fantasy-skill-icons.py cut archer Assets/ArtLibrary/classes/archer/skill-icons-src/archer-skill-icons-chatgpt-N.png`
Xem: `py -3 Tools/build-soft-fantasy-skill-icons.py preview archer docs/reference/skill-icons-soft-fantasy/archer-after-soft-fantasy-icons.png`

Prompt lượt 1 (một dòng, gửi nguyên văn):

```
Create an image. Việc mới: 8 ICON SKILL cho class Archer (Asteria Ranger bạn đã thiết kế, ảnh 2 = turnaround). Ảnh 1 = bộ icon skill hiện có của 4 class khác trong game (Warrior đỏ, Wizard lam, Mechanic vàng mật ong, Cleric xanh ngọc): bộ Archer phải CÙNG KIỂU y hệt (mỗi icon là MỘT ĐĨA TRÒN vẽ kín, cùng cỡ đĩa, gradient nền trong đĩa, biểu tượng to rõ giữa, nét vẽ, highlight và độ sáng như ảnh 1; KHÔNG vẽ vòng viền/khung/nhẫn quanh đĩa vì game tự thêm vòng). Màu chủ đạo theo Ranger: xanh lá non / lá chanh sáng (palette Leaf), điểm vàng kim của cung; phải phân biệt rõ với bộ Cleric (xanh ngọc mint) ở ảnh 1. Biểu tượng từng icon do bạn thiết kế theo Ranger, mỗi icon khác hẳn hình dáng, đọc rõ ở 64 px. Thứ tự (lưới 4 cột x 2 hàng, đọc theo hàng): (1) Piercing Arrow: một mũi tên mạnh bay xuyên cả hàng quái; (2) Multi Shot: 5 mũi tên bắn toả hình quạt 50 độ; (3) Eagle Eye: tự buff tăng tốc đánh và công; (4) Arrow Rain: tên bắn lên cao rơi xuống thành mưa trên vùng phía trước; (5) Binding Shot: mũi tên nặng ghim và làm choáng quái đầu tiên trúng; (6) Wind Step: lướt gió nhanh một đoạn, các phát bắn sau mạnh hơn; (7) Hunter's Focus: buff tập trung tăng mạnh công và tốc đánh; (8) Starfall Volley (tuyệt kỹ): bão mũi tên ánh sao rơi trên vùng rộng. Nền ngoài phẳng một màu #FF00FF (magenta đặc, không caro, không trong suốt, không bóng đổ ra nền), 8 đĩa cùng cỡ cách nhau rộng; không dùng màu tím/hồng trong art. Không chữ, không số, không khung vuông.
```

Kết quả lượt 1: `archer-skill-icons-chatgpt-1.png` (1448 x 1086), cắt sạch.

Review trong game (ChatGPT, cùng chat, gửi ảnh thanh skill + Skill Preview): 8,7/10; giữ 1-7; ô 8 Starfall Volley 6,5/10 vì cùng bố cục với ô 4 Arrow Rain (tên rơi thẳng + vòng nổ đáy) => vẽ lại riêng ô 8 theo định hướng V2 của ChatGPT:

```
Create an image. Đồng ý, chỉ vẽ lại icon số 8 Starfall Volley theo đúng định hướng V2 bạn vừa viết (mũi tên thiên thể đầu sao bốn cánh chiếm 65-70% đường kính, bay chéo từ trên-phải xuống dưới-trái, nền xanh rừng đậm hơn, vàng kim + trắng ngà, 3-4 sao lớn và vài quỹ đạo cong, bỏ hàng tên nhỏ song song và vòng nổ dưới đáy). Giữ đúng kiểu đĩa tròn, nét vẽ, highlight của 7 icon kia trong bộ bạn vừa vẽ. Chỉ MỘT đĩa tròn ở giữa ảnh vuông, đĩa chiếm khoảng 70% bề ngang ảnh, nền ngoài phẳng một màu #FF00FF (magenta đặc, không caro, không bóng đổ ra nền), không vòng viền quanh đĩa, không màu tím/hồng trong art, không chữ.
```

Ra `archer-starfall-volley-v2-chatgpt-1.png`; `archer-skill-icons-chatgpt-2.png` = sheet lượt 1 với đĩa 8 thay bằng đĩa mới (script dán, co theo chiều cao đĩa cũ 343 px), đây là sheet đang cài.
