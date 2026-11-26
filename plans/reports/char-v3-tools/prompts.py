"""Generate per-class ChatGPT prompts (turnaround + idle) for the V3 redesign; writes prompt files."""
import sys, json
C = {
 'wizard':  dict(name='Mage', vn='Pháp sư nguyên tố', game='wizard', weapon='trượng phép', gender='giữ giới tính như Mage trong bảng (ảnh 1)', bg='#00FF00'),
 'mechanic':dict(name='Engineer', vn='Kỹ sư chế tạo', game='mechanic', weapon='pháo tay / súng cơ khí', gender='nam như Engineer trong bảng', bg='#FF00FF'),
 'assassin':dict(name='Rogue', vn='Sát thủ linh hoạt', game='assassin', weapon='2 dao găm', gender='NỮ', bg='#00FF00'),
 'cleric':  dict(name='Priest', vn='Tu sĩ chữa lành', game='cleric', weapon='trượng thánh', gender='NỮ', bg='#00FF00'),
 'archer':  dict(name='Ranger', vn='Cung thủ tầm xa', game='archer (class mới)', weapon='cung + ống tên', gender='nam như Ranger trong bảng', bg='#FF00FF'),
}
PROP = ("TỈ LỆ BẮT BUỘC = Knight (Warrior hiện tại trong game). Ảnh 3 là BÓNG XÁM của Knight 5 hướng: CHỈ để so tỉ lệ / chiều cao, KHÔNG chép thiết kế, áo choàng hay dáng giáp của nó. Số đo của Knight (đo trên sprite thật): "
        "đỉnh sọ (không tính tóc / mũ) tới cằm = 33 % chiều cao người; bề ngang sọ ≈ 36 % chiều cao; vai ≈ 1,0-1,1 lần bề ngang sọ; tổng ≈ 3,0 'đầu sọ' (≈ 2,7 đầu khi có tóc), "
        "chân chạm cùng một đường đáy. Nhân vật mới phải cùng chiều cao và cùng cỡ đầu như Knight (sai lệch tối đa 3 %), không cao hơn, không đầu to hơn.")
def turnaround(k):
    c=C[k]
    return (f"Thiết kế nhân vật {c['name']} ({c['vn']}) cho game Asteria (class game: {c['game']}), theo đúng 'Asteria Character Art Direction V1' ở ảnh 1; ảnh 2 là {c['name']} trong bảng đó phóng to: đó là điểm xuất phát, bạn được tinh chỉnh cho đẹp và rõ hơn theo ý bạn. Giới tính: {c['gender']}. "
            "Knight trong bảng là Warrior đã làm xong, không vẽ lại Knight. "
            + PROP +
            f" Vẽ mới hoàn toàn từ đầu một bảng turnaround giống hàng 'Brief: Warrior V3 – Knight' của ảnh 1: 5 góc Front, 3/4 Front, Side, Back, 3/4 Back, xếp một hàng, cùng chiều cao, chân trên cùng một đường đáy, có tóc, mũ / phụ kiện đầu và {c['weapon']} như trong bảng. "
            f"Nền một màu phẳng {c['bg']} (không caro, không cảnh, không chữ). Vẽ xong chấm điểm /10 theo art direction và theo tỉ lệ Knight.")
def idle(k):
    c=C[k]
    return (f"Giờ vẽ mới hoàn toàn từ đầu sheet IDLE 5 hướng của {c['name']} vừa thiết kế để cắt vào game, đúng bố cục sheet idle của Knight trong game: ảnh rộng 3:1, 5 nhân vật một hàng theo thứ tự Front, 3/4 Front, Side, 3/4 Back, Back (các góc 3/4 và Side quay đúng hướng như ảnh bóng xám); nền phẳng {c['bg']}, khoảng cách đều, không chữ, không bóng đổ. "
            "Kỹ thuật như Knight: ĐẦU TRỌC (tóc và mũ / phụ kiện đầu là lớp riêng, KHÔNG vẽ), vẫn có mặt, mắt, tai; KHÔNG cầm vũ khí, KHÔNG ống tên / phụ kiện tay (vũ khí là lớp riêng gắn vào nắm tay), hai tay buông nắm hờ cạnh người; giữ nguyên trang phục của bản turnaround. "
            + PROP +
            " Trong ảnh: đỉnh sọ ở 12 % chiều cao ảnh, bàn chân ở 86 % chiều cao ảnh (người cao 74 % chiều cao ảnh), như ảnh bóng xám. Vẽ xong tự chấm /10 (art direction + tỉ lệ so Knight).")
if __name__=='__main__':
    k, kind = sys.argv[1], sys.argv[2]
    print(json.dumps(turnaround(k) if kind=='turn' else idle(k), ensure_ascii=False))
