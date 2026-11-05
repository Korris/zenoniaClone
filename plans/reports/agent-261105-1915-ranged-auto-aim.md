# Bắn thường tự ngắm quái chéo (2026-11-05)

## File đổi
- Mới `Assets/Scripts/Rpg/Skills/RangedAimRules.cs`: `AimCandidate` + `RangedAimRules.Pick / Direction`, code thuần (không Unity), test được.
- Mới `Assets/Scripts/RangedAutoAim.cs`: glue Unity, gom `Monster.All` thành candidate, gọi `Pick`, trả `dir`/`angle` mới qua `ref`.
- Sửa `Assets/Scripts/HeroController.Combat.cs` `FireBolt`: 1 dòng gọi `RangedAutoAim.Steer`, rồi dùng `v`/`angle` đã ngắm cho `NetEventBus.BoltFired`, `ComboProjectile.Fire`, `HeroFx.BoltLaunched`. Nòng (`muzzle`) và `CastTip` vẫn tính theo facing.
- Mới `Tests/dotnet/ZenoniaSkills.Tests/RangedAimRulesTests.cs`: 8 test.
- `docs/pending-tasks.md`: thêm 1 dòng mục "Xem trong game, chưa ai xem".
- Không đụng `BoltStyle.cs`, `ArcaneBoltRig.cs`, `BurstFx.cs`, `HeroClassDef.cs`, `ComboConfigs`, `SkillTable`, số liệu sát thương/tầm.

## Luật chọn mục tiêu (lúc hit bắn, mỗi phát một lần)
- Ứng viên: quái không `IsDying`.
- Nửa mặt phẳng trước: `dot(center - heroFeet, facing) >= 0` (góc với trục nhìn ≤ 90°). Code chưa có cone sẵn cho bắn thường nên dùng đúng nửa mặt phẳng.
- Tầm: `SkillGeometry.DistToBox(muzzle, Box) <= h.Bolt.Range` (đạn bay đúng `Range` đơn vị từ nòng, `life = Range/Speed`).
- Chọn gần nhất; hoà (±0,001) thì `NetId` nhỏ hơn, rồi index nhỏ hơn: tất định với cùng tập quái.
- Hướng ngắm = vector đơn vị nòng → `Monster.Center`; góc = atan2. Không có mục tiêu: giữ bắn thẳng 4 hướng. Hero không quay người.

## Đồng bộ server / client
- Đạn là hitbox bay (`ComboProjectile.Update` → `HitSomething` chỉ ở authority); client dự đoán chỉ vẽ (`PredictedImpact`). Server chạy `FireBolt` với quái thật → sát thương và `BoltFired` mang góc server; client dự đoán chạy cùng code với quái proxy từ snapshot (đúng quy tắc VFX 7) nên có thể lệch góc chút khi quái đang di chuyển; đạn dự đoán vẫn được `AdoptPredicted` nhận id và `BoltEnd` đặt điểm nổ theo server.
- Người chơi khác: `NetEventPlayer` → `PlayBoltFx(step, muzzle, angle, ...)` đã tính `dir` từ `Angle` của sự kiện (độ phân giải 360/256 ≈ 1,4°), nên đạn chéo vẽ đúng, cast tip chọn theo facing gần nhất với hướng bắn (code sẵn có).

## Protocol
- Không đổi: `BoltFired` đã có trường `Angle`. `NetProtocol.Version` giữ 14.
- Nhưng server quyết định trúng/không: phải build lại zone server để online cũng tự ngắm; client cũ với server mới vẫn vẽ đúng góc.

## Biên dịch
- Zone server là bản build Unity (không có csproj riêng); `Tests/dotnet/*` target net10.0, máy chỉ có SDK 9.0.203 nên không build/test trong repo được.
- Đã tạo bản sao csproj net9.0 trong scratchpad gồm `Assets/Scripts/Rpg/**` + test: build OK, `RangedAimRulesTests` 8/8 pass. Phần Unity (`RangedAutoAim.cs`, `HeroController.Combat.cs`) chưa biên dịch (Unity đang mở, không chạy; Editor.log chưa thấy lần biên dịch mới). Lead build sau.

## Câu hỏi chưa chốt
1. Nửa mặt phẳng 180° có rộng quá không (quái ngang hông cũng bị ngắm, đạn bay gần vuông góc với hướng nhìn)? Nếu muốn hẹp hơn chỉ cần đổi điều kiện dot trong `RangedAimRules.Pick` thành cone (vd 120°).
2. Chớp nòng/pháo Mechanic (`CastWindup`, `MechanicCannonBlastFx`) vẫn xoay theo facing vì chơi lúc windup, trước khi chọn mục tiêu; chỉ `FallbackCast`/`SecondPistol`/tracer theo góc ngắm. Cần xem trong game có chướng mắt không.
3. Có nên ưu tiên quái đang ở ngay trục nhìn hơn quái gần hơn ở bên hông? Hiện lấy gần nhất theo đúng brief.
