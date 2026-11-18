# Brief: vẽ art nhân vật nhiều lớp bằng ChatGPT (cho agent)

Work context: D:\zenoniaClone (branch `unity`). Reports: D:\zenoniaClone\plans\reports\. Môi trường: Windows 11, Git Bash + PowerShell, Python (PIL/numpy/scipy), Chrome extension (Claude in Chrome) đăng nhập ChatGPT tài khoản Tay ("Tân Korris").

## Bắt buộc đọc trước
- `CLAUDE.md`
- `docs/character-appearance-layered-spec.md`: quy cách và 7 prompt mẫu.
- `docs/sprite-rules.md`: các mục "Quy tắc art lấy từ ChatGPT", "Quy tắc tư thế cầm", và các dòng Sổ lỗi về lấy ảnh ChatGPT (DOM ảo hoá, ảnh cũ nạp lại, A/B, tab).

## Chat và tài khoản
- Chat Tay: https://chatgpt.com/c/6afe1ebe-b668-83ec-8233-c010a9354ef0 (đã có thân gốc nam + nữ + ảnh đi đầu tiên).
- Luôn đọc tên ở `/api/auth/session` = "Tân Korris" trước khi gửi.
- Hết lượt vẽ: chuyển sang tài khoản Tan Pham (Plus) (được phép), mở chat mới, kiểm tra tên. Không dùng tài khoản khác.
- Mỗi lượt chỉ một agent dùng ChatGPT.

## Cách làm trong Chrome
1. `tabs_context_mcp createIfEmpty`, mở URL chat, chờ 5 s.
2. Dán các hàm dưới vào trang bằng `javascript_tool` (mất khi tải lại trang thì dán lại).
3. `find` "Attach photos file input" lấy ref, `file_upload` ảnh mẫu (chỉ file trong D:\zenoniaClone).
4. Gọi `await __send(prompt, soẢnhĐínhKèm)`. Kết quả phải là `[true, …]`.
5. Chờ bằng nhiều lệnh JS ngắn (mỗi lệnh dưới 40 s): `for(i<8){ if(__ready()) break; await sleep 4500 }` rồi `await __grab('<tên>')`.
   - `__grab` trả nhiều ảnh, hoặc cờ A/B là true: tự xem cả 2, bấm nút "Image N is better" của ảnh đúng.
6. Chép file từ `C:/Users/korri/Downloads/<tên>-nK.png` vào `Assets/ArtLibrary/hero-layered/src/<tên>-chatgpt-1.png` (vẽ lại thì -2, -3).
7. MỞ ẢNH XEM (Read) trước khi nhận. Kiểm tra:
   - đúng hướng, đúng số frame, đầu trọc (ảnh thân);
   - áo chỉ hồng tím, không dính màu lạ;
   - cỡ đầu không to hơn ảnh đứng;
   - không hình nào chạm hình bên cạnh.
   Sai thì gửi lại trong cùng chat, ghi rõ chỗ sai.

```js
window.__sizes = window.__sizes || new Set();
window.__send = async (P, needImgs) => { const ed=[...document.querySelectorAll('form [contenteditable="true"]')].pop(); ed.focus(); document.execCommand('selectAll'); document.execCommand('delete'); document.execCommand('insertText', false, P); await new Promise(r=>setTimeout(r,1500)); const imgs=document.querySelectorAll('form img').length; const ok = ed.innerText.length===P.length && imgs>=needImgs && document.querySelectorAll('form circle').length===0 && location.href.includes('6afe1ebe'); if(ok){ document.querySelector('form button[aria-label="Send"]').click(); } await new Promise(r=>setTimeout(r,3000)); return [ok, imgs, location.href]; };
window.__bottom = async () => { [...document.querySelectorAll('*')].filter(e=>{const s=getComputedStyle(e);return /(auto|scroll)/.test(s.overflowY) && e.scrollHeight>e.clientHeight+50;}).forEach(e=>e.scrollTop=e.scrollHeight); await new Promise(r=>setTimeout(r,1500)); };
window.__ready = () => { const t=document.body.innerText.slice(-400); return t.includes('Response complete') && !/Creating image|\d+%/.test(t); };
window.__grab = async (base) => { await window.__bottom(); const gen=[...document.querySelectorAll('main img')].filter(i=>i.alt.startsWith('Generated') && i.naturalWidth>500); const out=[]; let k=0; for(const im of gen){ const b=await fetch(im.src).then(r=>r.blob()); if(window.__sizes.has(b.size)) continue; window.__sizes.add(b.size); k++; const a=document.createElement('a'); a.href=URL.createObjectURL(b); a.download= base+'-n'+k+'.png'; document.body.appendChild(a); a.click(); out.push(b.size); await new Promise(r=>setTimeout(r,1200)); } return [out, document.body.innerText.includes('Which image do you like more')]; };
```

**Lưu ý về `__grab`:**
- Lần đầu sau khi mở trang, `__grab` sẽ tải về các ảnh cũ của chat. Ngay sau khi dán hàm, chạy `await __grab('old')` một lần để đánh dấu chúng, rồi xoá các file `old-*` trong Downloads.
- Ảnh tải về trùng cỡ byte với một ảnh đã có tức là ảnh cũ: bỏ.

## Prompt
Chép đúng mẫu trong spec, Prompt 3-7, thay `<…>`.

**Tên file nguồn:**
- `body-<male|female>-walk-<a>--<b>-chatgpt-N.png`
- `body-<g>-attack<1|2|3>-<a>--<b>-chatgpt-N.png`
- Ảnh 1 hàng thì `b` = `none`.
- Cặp hướng: (down, down-right), (right, up-right), (up, none).
- Ảnh 1 hàng (hướng up): viết "1 hàng x 4 cột ở giữa ảnh".

**Ảnh mẫu đính kèm (đã có trong `Assets/ArtLibrary/hero-layered/ref/`):**
- Thân: `body-<g>-idle-<a>-<b>.png`; với hướng up là `body-<g>-idle-up-up.png`.
- Tóc và mũ: `bald-head-<male|female>-5dir.png`.

Sau mỗi ảnh thân, chạy `python Tools/build-hero-layered-sheets.py write <gender>` rồi `check <gender>`, và mở `docs/reference/hero-layered-sheets/<gender>-check.png` xem cỡ, chân, vạch đỏ đỉnh sọ.

## Commit
- Sau mỗi 3-4 ảnh: `git add Assets/ArtLibrary/hero-layered` rồi commit kiểu `feat(art): layered hero <gì>`, không nhắc AI.
- Không push. Không sửa code game.
- Không đụng `Assets/Scripts`, `Assets/Resources`, các worktree khác.

## Báo cáo
Ghi `plans/reports/layered-hero-art-<đợt>-261119.md`:
- ảnh nào đã nhận, ảnh nào vẽ lại và vì sao;
- lỗi còn thấy;
- số lượt vẽ đã dùng;
- câu hỏi.

Mỗi lỗi mới về art ChatGPT ghi thêm 1 dòng vào Sổ lỗi đầu `docs/sprite-rules.md`.
