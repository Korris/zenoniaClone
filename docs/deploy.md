# Đưa server Pocket Valor lên VPS

## Cách ngắn: pm2, không cần Docker

Dùng MongoDB bạn đã có (phải là replica set, Atlas thì luôn đạt). Máy chủ cần Node 20.12 trở lên và pm2. Cách này không có backup tự động: backup do phía MongoDB của bạn lo.

**1.** Lần đầu: điền `MONGO_URI`, `SMTP_USER`, `SMTP_PASS`, `SMTP_FROM` trong `deploy/.env.prod`.

**2.** Trên máy Windows, ra file `C:\temp\pocketvalor-server.tar.gz`. File này chứa mật khẩu thật, chỉ chép bằng `scp`:

```powershell
.\Tools\package-server.ps1
scp C:\temp\pocketvalor-server.tar.gz <user>@<IP-VPS>:/tmp/
```

Chỉ sửa API thì thêm `-ApiOnly` nếu chưa cần zone, hoặc `-SkipBuild` để dùng lại zone đã build.

**3.** Trên server, lần đầu và mỗi lần cập nhật đều chạy đúng dòng này (`/opt/pocketvalor` phải ghi được):

```bash
mkdir -p /opt/pocketvalor && cd /opt/pocketvalor && rm -rf api zone && tar xzf /tmp/pocketvalor-server.tar.gz && rm /tmp/pocketvalor-server.tar.gz && bash start.sh
```

Thành công là `pm2 list` hiện `pocketvalor-api` và `pocketvalor-zone` đều `online`. Xem lỗi: `pm2 logs`. Lần đầu chạy thêm `pm2 startup` để tự bật lại khi VPS khởi động.

**4.** Chỉ làm lần đầu: thêm site nginx ở bước nginx bên dưới, xin HTTPS bằng certbot, mở cổng UDP 9050 trên tường lửa VPS và tường lửa của nhà cung cấp.

Phần còn lại của file này là cách chạy bằng Docker, không cần đọc nếu dùng pm2.

## Cách đầy đủ: Docker

Hướng dẫn này tự đủ: làm theo thứ tự, chép từng lệnh, so với mục "Thành công là" rồi mới sang bước sau. VPS là Ubuntu đã có sẵn nginx và có thể đang chạy site khác. Các bước chỉ **thêm** một site mới, không sửa site cũ.

Trong các lệnh:

- `<IP-VPS>` là IP của VPS, `<user>` là tài khoản SSH: tự thay bằng của bạn.
- Tên miền là `pocketvalor.luckyday.io.vn`. Nếu đổi tên miền thì thay ở mọi lệnh và trong `deploy/.env.prod`.

## Những gì chưa được chạy thử

Hãy đọc trước, để khi gặp lỗi còn biết nên nghi ở đâu:

- Chưa có lần nào chạy trên VPS thật. Máy làm hướng dẫn này không bật Docker, nên image, compose thật, nginx, certbot, `mongodump`/`mongorestore` đều **chưa chạy lần nào**. Bước 9 và 12 là lần đầu kiểm tra chúng.
- Chưa từng gửi mail qua Gmail thật. Mailer mới được thử với một SMTP giả trên máy, không có STARTTLS.
- Zone server chưa từng chạy trên Linux vì chưa có bản build. Danh sách thư viện trong `deploy/zone/Dockerfile` chỉ là dự đoán.
- Người chơi nối vào zone bằng tên miền cũng chưa được thử qua mạng thật.

Nếu máy Windows đã bật Docker Desktop, có thể thử cả stack (trừ zone) trước khi thuê máy. Script dùng project riêng `zenonia-verify`, cổng cao, tự dọn sau khi xong:

```bash
bash deploy/test/verify-stack.sh
```

Thành công là dòng cuối ghi `ALL OK`.

## Cái gì chạy ở đâu

| Thành phần | Chạy ở | Mở ra ngoài |
|---|---|---|
| nginx + HTTPS (certbot) | Trên VPS, ngoài Docker | TCP 80, 443 |
| API (`server-api`) | Docker | Không, chỉ `127.0.0.1:3000` cho nginx |
| MongoDB | Docker | Không, mạng nội bộ của stack |
| Backup hằng ngày | Docker | Không |
| Zone server (Unity) | Docker | UDP 9050 |

`/internal/*` chỉ dành cho zone, nginx luôn trả 404 cho đường này.

## Bước 1. Chuẩn bị trên máy Windows

**1a. Tạo Google App Password.** Vào tài khoản Gmail sẽ dùng để gửi mail, bật Xác minh 2 bước, rồi mở <https://myaccount.google.com/apppasswords> và tạo mật khẩu mới. Google đưa 16 chữ cái. Dùng mật khẩu này, **không** dùng mật khẩu đăng nhập Gmail.

Giới hạn của Gmail cần biết:

- Khoảng **500 mail mỗi ngày**. Quá mức thì Google tạm khóa việc gửi. Mỗi tài khoản game chỉ nhận tối đa 1 mail mỗi 60 giây, nhưng nếu đông người chơi thì nên chuyển sang dịch vụ gửi mail riêng.
- Người nhận sẽ thấy **địa chỉ Gmail của bạn là người gửi**, kể cả khi `SMTP_FROM` ghi địa chỉ khác.

**1b. Điền `deploy\.env.prod`.** File này đã có sẵn `DOMAIN`, `ZONE_HOST`, và `JWT_SECRET`, `SERVICE_KEY` đã được sinh ngẫu nhiên. Bạn tự điền ba dòng còn lại:

```
SMTP_USER=dia-chi-cua-ban@gmail.com
SMTP_PASS=16chucaikhongcocach
SMTP_FROM=Pocket Valor <dia-chi-cua-ban@gmail.com>
```

**Không commit file này và không gửi nó qua chat hay mail.** `deploy/.gitignore` đã bỏ qua nó. Chỉ chép nó lên VPS bằng `scp`. Kiểm tra lại:

```powershell
git check-ignore -v deploy/.env.prod
```

Thành công là: lệnh in ra một dòng có `deploy/.gitignore`.

**1c. Đóng gói.** Chạy trong PowerShell, ở thư mục gốc repo:

```powershell
tar -czf $env:TEMP\pocketvalor-server.tgz --exclude=node_modules --exclude=.dev-data --exclude=.env --exclude=.env.prod --exclude=backups --exclude=build --exclude=.work --exclude=coverage deploy server-api
```

Thành công là: có file khoảng 80 KB và không báo lỗi. Gói này cố ý **không** chứa `.env.prod`.

**1d. Chép lên VPS.** Lệnh đầu tạo thư mục, hai lệnh sau chép file:

```powershell
ssh -t <user>@<IP-VPS> "sudo mkdir -p /opt/pocketvalor && sudo chown `$USER /opt/pocketvalor"
scp $env:TEMP\pocketvalor-server.tgz <user>@<IP-VPS>:/opt/pocketvalor/
ssh <user>@<IP-VPS> "cd /opt/pocketvalor && tar -xzf pocketvalor-server.tgz"
scp deploy\.env.prod <user>@<IP-VPS>:/opt/pocketvalor/deploy/.env.prod
```

| Nếu thấy | Làm gì |
|---|---|
| `Permission denied` khi scp | Lệnh đầu chưa chạy được, thư mục chưa thuộc về `<user>` |

## Bước 2. Kiểm tra máy (pre-flight)

Từ đây trở đi là chạy trên VPS.

```bash
ssh <user>@<IP-VPS>
cd /opt/pocketvalor/deploy
chmod 600 .env.prod
sudo bash preflight.sh
```

Script chỉ đọc, không đổi gì, không in bí mật. Nó xem Docker, nginx, cổng đang dùng, RAM, đĩa, DNS, ufw và các ô trống trong `.env.prod`. Nó có gửi một yêu cầu đến dịch vụ công khai để biết IP của máy.

Thành công là: dòng cuối ghi `Result: 0 FAIL`. Nhưng ở lần chạy đầu thì `FAIL` về Docker và DNS là bình thường, các bước 3 và 4 sẽ xử lý. Sau bước 4 hãy chạy lại, và đọc từng dòng `WARN`.

Xem luôn cổng đang có ai dùng, để biết máy đang chạy gì:

```bash
sudo ss -ltnp
sudo ss -lunp
ls -l /etc/nginx/sites-enabled
```

**Chụp hoặc lưu lại kết quả ba lệnh này.** Nếu sau này có site cũ hỏng, đây là mốc để so sánh.

| Nếu thấy | Làm gì |
|---|---|
| Cổng 3000 đang có chương trình khác dùng | Đổi `API_PORT` trong `.env.prod` (ví dụ `3100`), và dùng đúng số đó ở bước 7 |
| UDP 9050 đang có chương trình khác dùng | Đổi `ZONE_PORT` trong `.env.prod` |
| Cổng 80/443 không phải nginx giữ | Dừng lại. Tìm hiểu xem đó là gì, hướng dẫn này giả định nginx giữ hai cổng đó |
| RAM báo `FAIL` | Thuê gói lớn hơn hoặc thêm swap. Nhu cầu RAM thật của zone chưa đo được |

## Bước 3. Cài Docker

Nếu bước 2 báo Docker và compose đều `OK`, bỏ qua bước này. Docker Engine và compose plugin được cài từ apt repo chính thức của Docker.

**Việc này không đụng đến nginx hay các site đang chạy:** các gói Docker không phụ thuộc nginx, không sửa `/etc/nginx` và không chiếm cổng 80, 443. Nó có thêm mạng `docker0` và các luật iptables của riêng Docker. Lưu trạng thái trước khi cài để so sánh:

```bash
systemctl is-active nginx
sudo nginx -t
```

**3a. Thêm repo của Docker:**

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
```

Thành công là: lệnh `apt-get update` cuối có một dòng chứa `download.docker.com` và không báo lỗi.

**3b. Cài Docker Engine và compose plugin:**

```bash
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker $USER
exit
```

Lệnh `usermod` cho tài khoản này dùng Docker không cần `sudo`. Nhóm `docker` có quyền tương đương root, nên chỉ thêm tài khoản bạn tin tưởng.

**3c. SSH lại rồi kiểm tra:**

```bash
docker compose version
docker run --rm hello-world
systemctl is-active nginx
sudo nginx -t
```

Thành công là: `docker compose version` in số phiên bản, có dòng `Hello from Docker!`, nginx vẫn `active`, `nginx -t` vẫn `test is successful`, và các site cũ vẫn mở được.

| Nếu thấy | Làm gì |
|---|---|
| `permission denied ... docker.sock` | Chưa đăng nhập lại sau `usermod`. Thoát SSH rồi vào lại |
| `docker: 'compose' is not a docker command` | `sudo apt-get install -y docker-compose-plugin` |
| `Conflicting values` hoặc `Malformed` khi `apt-get update` | Đã có file repo Docker từ trước. Xem `ls /etc/apt/sources.list.d/`, đừng xóa khi chưa rõ file đó của ai |
| `containerd.io` xung đột với gói khác | Máy đã có Docker từ gói `docker.io` của Ubuntu. Dừng lại và xem `dpkg -l \| grep -E 'docker\|containerd'`. Nếu đã có container đang chạy thì đừng gỡ |
| Không có bản cho phiên bản Ubuntu này | Xem `lsb_release -a`, rồi đối chiếu danh sách được hỗ trợ ở <https://docs.docker.com/engine/install/ubuntu/> |

**Cảnh báo: cổng do Docker publish đi vòng qua ufw.** Docker tự thêm luật iptables, nên `ufw deny` **không chặn** được một cổng đã publish, và `ufw status` cũng không hiện cổng đó. Stack này chỉ publish hai cổng: API trên `127.0.0.1` và UDP của zone. Đừng thêm `ports` vào compose, và đừng bỏ `127.0.0.1:` ở dòng của API. Bước 6 có lệnh kiểm tra việc này.

## Lệnh gọi tắt `pv`

Các bước sau dùng `pv` thay cho một lệnh `docker compose` dài. Chạy một lần (kể cả khi đã bỏ qua bước 3), nó vẫn còn ở các lần SSH sau và dùng được ở mọi thư mục:

```bash
echo "alias pv='docker compose --project-directory /opt/pocketvalor/deploy --env-file /opt/pocketvalor/deploy/.env.prod -f /opt/pocketvalor/deploy/docker-compose.prod.yml'" >> ~/.bash_aliases
source ~/.bash_aliases
pv version
```

Thành công là: `pv version` in số phiên bản. Nếu SSH mới báo `pv: command not found` thì chạy lại `source ~/.bash_aliases`.

## Bước 4. Trỏ DNS

Trong trang quản lý DNS của `luckyday.io.vn`, tạo bản ghi:

| Loại | Tên | Giá trị |
|---|---|---|
| A | `pocketvalor` | `<IP-VPS>` |

Không biết IP của VPS thì chạy `curl -4 https://api.ipify.org` trên VPS.

Nếu dùng Cloudflare, **phải** đặt là "DNS only" (đám mây xám). Trò chơi đi bằng UDP, không đi qua proxy của Cloudflare được. Ngoài ra, nếu đi qua Cloudflare thì mọi người chơi sẽ mang IP của Cloudflare và cùng chịu chung một giới hạn theo IP.

Kiểm tra:

```bash
getent ahostsv4 pocketvalor.luckyday.io.vn
sudo bash preflight.sh
```

Thành công là: thấy đúng IP của VPS, và phần DNS của preflight ghi `[OK] ... points at this host`.

| Nếu thấy | Làm gì |
|---|---|
| Chưa ra IP | Đợi vài phút rồi thử lại, bản ghi mới cần thời gian |
| Ra IP khác | Sửa lại bản ghi. Nếu là IP của Cloudflare thì tắt proxy |

## Bước 5. Tường lửa

**Máy này có thể đang chạy site khác: chỉ thêm luật, không xóa luật nào, không chạy `ufw reset`.** Mọi cổng mà các site đang có cần đều phải được giữ.

```bash
sudo ufw status verbose
```

**Nếu ufw đang `active`:** chỉ thêm cổng zone, thêm 80 và 443 nếu chưa có:

```bash
sudo ufw allow 9050/udp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

**Nếu ufw đang `inactive`:** so với danh sách `ss -ltnp` đã lưu ở bước 2, rồi mở mọi cổng mà máy đang thực sự cần **trước khi** bật. Mở thiếu cổng SSH là tự khóa mình ở ngoài. Với máy chỉ có nginx và SSH cổng 22:

```bash
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 9050/udp
sudo ufw enable
```

Giữ cửa sổ SSH hiện tại, mở thêm một cửa sổ SSH mới để thử đăng nhập trước khi đóng.

Thành công là: `sudo ufw status` có cả bốn cổng, SSH mới vào được, các site cũ vẫn mở được.

Cần biết:

- Cổng do Docker publish **đi vòng qua ufw**: ufw không chặn được chúng. Vì thế API chỉ publish trên `127.0.0.1` và Mongo không publish gì. Đừng đổi điều đó trong compose.
- Nhiều nhà cung cấp VPS có thêm tường lửa ở trang quản trị. Nếu có thì mở UDP 9050 ở đó nữa.
- Đã đổi `ZONE_PORT` thì dùng số mới ở các lệnh trên.

## Bước 6. Khởi động API, Mongo, backup

```bash
pv up -d --build
pv ps
curl -s http://127.0.0.1:3000/health
```

Lần đầu mất vài phút để tải image.

Thành công là: `pv ps` có `mongo`, `api` ở trạng thái `healthy`, có `backup` ở `Up`, và `curl` in `{"ok":true}`.

Kiểm tra Docker đã publish đúng những gì, vì ufw không chặn được các cổng này:

```bash
docker ps --filter label=com.docker.compose.project=zenonia --format '{{.Names}}: {{.Ports}}'
```

Thành công là: chỉ dòng của `api` có `127.0.0.1:3000->3000/tcp` (sau bước 11, zone có thêm `9050->9050/udp`). Dòng `mongo` chỉ ghi `27017/tcp`, không có dấu `->`, tức là không publish. Nếu thấy `0.0.0.0:3000` hoặc `0.0.0.0:27017`, **hãy chạy `pv down` ngay** (dữ liệu vẫn còn) và khôi phục `docker-compose.prod.yml` từ gói gốc.

Kiểm tra cấu hình mail:

```bash
pv logs api | grep -E "SMTP"
```

Thành công là: có `SMTP on: password reset links are mailed`. Nếu có `SMTP is not configured` thì `.env.prod` còn thiếu, dòng đó ghi rõ thiếu biến nào. Sửa rồi chạy `pv up -d api`.

| Nếu thấy | Làm gì |
|---|---|
| `required variable ... is missing a value` | Biến đó trống trong `.env.prod`. Đây là cố ý: stack không chịu chạy khi thiếu |
| `port is already allocated` | Cổng đang bận: đổi `API_PORT` hoặc `ZONE_PORT` |
| `api` cứ khởi động lại | `pv logs --tail 50 api`. `Invalid configuration` cho biết biến nào sai |
| `mongo` không `healthy` | `pv logs --tail 50 mongo`. Thường do hết RAM hoặc hết đĩa |
| `curl` báo `Connection refused` | API chưa sẵn sàng hoặc `API_PORT` khác 3000: dùng đúng số đó trong URL |

## Bước 7. Thêm site vào nginx

Chỉ thêm một file mới trong `sites-available` và một symlink. Không sửa site khác, không dùng `default_server`.

```bash
cd /opt/pocketvalor
sudo apt install -y gettext-base
DOMAIN=pocketvalor.luckyday.io.vn API_PORT=3000 envsubst '${DOMAIN} ${API_PORT}' < deploy/nginx/zenonia.conf.template | sudo tee /etc/nginx/sites-available/zenonia.conf > /dev/null
sudo ln -s /etc/nginx/sites-available/zenonia.conf /etc/nginx/sites-enabled/zenonia.conf
sudo nginx -t
```

**Chỉ khi** `nginx -t` in `syntax is ok` và `test is successful` mới reload:

```bash
sudo systemctl reload nginx
curl -s http://pocketvalor.luckyday.io.vn/health
curl -s -o /dev/null -w '%{http_code}\n' http://pocketvalor.luckyday.io.vn/internal/zones
```

Thành công là: lệnh `curl` đầu in `{"ok":true}`, lệnh sau in `404`.

Nếu `nginx -t` báo lỗi: **đừng reload**. nginx vẫn đang chạy cấu hình cũ, site cũ không ảnh hưởng. Gỡ đúng file của mình rồi tìm nguyên nhân:

```bash
sudo rm /etc/nginx/sites-enabled/zenonia.conf
sudo nginx -t
```

| Nếu thấy | Làm gì |
|---|---|
| `address family not supported` ở `[::]:80` | Máy tắt IPv6: xóa dòng `listen [::]:80;` trong `/etc/nginx/sites-available/zenonia.conf` |
| `conflicting server name` | Một site khác đã nhận tên miền này. Tìm bằng `sudo grep -rl pocketvalor /etc/nginx/`, đừng tự sửa site đó khi chưa hiểu nó |
| `sites-enabled` không có, hoặc `nginx.conf` không include nó | nginx dùng `conf.d`: ghi file vào `/etc/nginx/conf.d/zenonia.conf` và bỏ lệnh `ln` |
| `502 Bad Gateway` | API chưa chạy, hoặc `API_PORT` trong file nginx khác `.env.prod` |
| Ra nội dung của site khác | DNS sai, hoặc đang gọi bằng IP thay vì tên miền |

## Bước 8. HTTPS bằng certbot

Plugin nginx của certbot sẽ thêm phần HTTPS **vào đúng file có `server_name` là tên miền này**.

```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d pocketvalor.luckyday.io.vn --redirect
sudo nginx -t
```

certbot hỏi email và điều khoản, trả lời theo hướng dẫn trên màn hình. Nếu `nginx -t` vẫn ổn thì reload và kiểm tra:

```bash
sudo systemctl reload nginx
curl -s https://pocketvalor.luckyday.io.vn/health
sudo certbot renew --dry-run
sudo grep -l "managed by Certbot" /etc/nginx/sites-enabled/*
```

Thành công là: `curl` in `{"ok":true}`, `renew --dry-run` báo thành công, và danh sách cuối có `zenonia.conf`. Nếu danh sách có các file khác thì đó có thể là site cũ đã dùng certbot từ trước, hãy so với mốc ở bước 2.

| Nếu thấy | Làm gì |
|---|---|
| `Could not automatically find a matching server block` | File ở bước 7 chưa được bật. Xem lại `ls -l /etc/nginx/sites-enabled` |
| `Timeout during connect` / `unauthorized` | DNS chưa đúng, hoặc cổng 80 bị chặn ở ufw hay ở tường lửa của nhà cung cấp |
| `too many certificates` | Let's Encrypt giới hạn số lần cấp. Đợi theo thời gian được báo, đừng thử lại liên tục |

## Bước 9. Kiểm tra từ ngoài

Đặt tên trước rồi chạy, đổi `kiemtra1` nếu đã dùng:

```bash
B=https://pocketvalor.luckyday.io.vn
U=kiemtra1
curl -s -o /dev/null -w 'health   %{http_code}\n' $B/health
curl -s -o /dev/null -w 'reset    %{http_code}\n' $B/reset
curl -s -o /dev/null -w 'internal %{http_code}\n' $B/internal/zones
curl -s -o /dev/null -w 'lach     %{http_code}\n' --path-as-is $B/auth/../internal/zones
curl -s -o /dev/null -w 'goc      %{http_code}\n' $B/
curl -s -o /dev/null -w 'register %{http_code}\n' -H 'content-type: application/json' -d "{\"username\":\"$U\",\"email\":\"$U@example.com\",\"password\":\"password123\"}" $B/auth/register
curl -s -o /dev/null -w 'login    %{http_code}\n' -H 'content-type: application/json' -H 'X-Forwarded-For: 1.2.3.4' -d "{\"login\":\"$U\",\"password\":\"password123\"}" $B/auth/login
pv logs api | grep '/auth/login' | tail -1
```

Thành công là:

```
health   200
reset    200
internal 404
lach     404
goc      404
register 201
login    200
```

Và trong dòng log cuối, `remoteAddress` là IP thật của máy gọi, **không phải** `1.2.3.4` và không phải `127.0.0.1` hay IP của Docker.

| Nếu thấy | Làm gì |
|---|---|
| `internal` khác 404 | **Dừng lại, chưa mở cho ai chơi.** Kiểm tra file nginx có đúng bản từ template không |
| `remoteAddress` là `1.2.3.4` | File nginx đã bị đổi `X-Forwarded-For`: khôi phục từ template. Nếu không, ai cũng tránh được giới hạn theo IP |
| `remoteAddress` là `127.0.0.1` | Biến `TRUST_PROXY` không vào được API: `pv config \| grep TRUST_PROXY`, nên là `"true"` |
| `register` ra 429 | Đã quá 5 tài khoản một giờ từ một IP. Đó là giới hạn đúng, đợi hoặc tăng `REGISTER_IP_MAX` |

## Bước 10. Thử mail đặt lại mật khẩu

Đăng ký một tài khoản bằng **email thật của bạn** thay cho `example.com` như bước 9, rồi:

```bash
curl -s -o /dev/null -w '%{http_code}\n' -H 'content-type: application/json' -d '{"email":"email-that-cua-ban@gmail.com"}' https://pocketvalor.luckyday.io.vn/auth/password-reset/request
pv logs --tail 20 api | grep -i "mail"
```

Thành công là: lệnh `curl` in `202`, không có dòng `could not be sent`, và hộp thư (xem cả Spam) có mail "Reset your Pocket Valor password". Bấm link, đặt mật khẩu mới, thử đăng nhập lại.

API luôn trả 202, kể cả khi email không tồn tại hoặc gửi lỗi, để không ai dò được ai có tài khoản. Vì vậy phải xem log mới biết có gửi được không. Yêu cầu lần hai trong vòng 60 giây sẽ không có mail nữa, đó là cố ý.

| Log ghi | Nghĩa là | Làm gì |
|---|---|---|
| `EAUTH`, `535` | Google từ chối đăng nhập | `SMTP_PASS` phải là App Password, không phải mật khẩu Gmail. `SMTP_USER` đúng tài khoản đã tạo nó |
| `ETIMEDOUT`, `ESOCKET`, `ECONNECTION` | Không nối được đến Gmail | Nhà cung cấp VPS có thể chặn cổng 587 đi ra. Thử `nc -vz smtp.gmail.com 587` trên VPS, hỏi họ mở cổng |
| `no mail was sent: SMTP is not configured` | `.env.prod` còn thiếu | Điền đủ rồi `pv up -d api` |

Sửa `.env.prod` ngay trên VPS bằng `nano .env.prod` được. Nhớ sửa cả bản trên máy Windows cho khớp.

## Bước 11. Zone server (cần bản build Linux)

**Bản build Linux chưa có**, vì máy chưa cài module Linux của Unity, và `PlayerBuildScript` hiện chỉ có `BuildWindows`, `BuildAndroid`. API vẫn chạy được khi chưa có zone, nhưng chưa ai chơi được.

**11a. Build trên Windows.** Trong Unity Hub, thêm module cho 6000.3.9f1: *Linux Build Support (Mono)* và/hoặc *Linux Dedicated Server Build Support*. Build từ một bản sao của project, **không** build trên project đang mở. Đặt tên file chạy là `Zenonia.x86_64`. Nếu tên khác, sửa `ZONE_EXECUTABLE` trong `.env.prod`.

**11b. Tải lên VPS.** Trong PowerShell, từ thư mục chứa bản build (`C:\duong-dan\build-linux` là ví dụ):

```powershell
tar -czf $env:TEMP\zone-build.tgz -C C:\duong-dan\build-linux .
scp $env:TEMP\zone-build.tgz <user>@<IP-VPS>:/opt/pocketvalor/
```

Trên VPS. Phải có `chmod`, vì tải từ Windows lên sẽ mất quyền chạy:

```bash
cd /opt/pocketvalor/deploy
pv --profile zone stop zone
rm -rf zone/build && mkdir -p zone/build
tar -xzf ../zone-build.tgz -C zone/build
chmod +x zone/build/Zenonia.x86_64
```

(Lần đầu, lệnh `stop` có thể báo không có `zone`. Bỏ qua được.)

**11c. Chạy.**

```bash
pv --profile zone up -d --build
pv --profile zone logs -f zone
```

Thành công là: thấy `save: real accounts on` rồi `save: the API answers, zone sessions reset`. Nhấn Ctrl+C để thoát xem log (zone vẫn chạy).

Từ đây **mọi lệnh `up` đều phải có `--profile zone`**, nếu không zone sẽ không được khởi động cùng.

| Nếu thấy | Làm gì |
|---|---|
| `error while loading shared libraries: libxxx.so` | Thiếu thư viện. Thêm gói chứa nó vào dòng `apt-get install` trong `deploy/zone/Dockerfile`, rồi `pv --profile zone up -d --build` |
| `No player at ...` | Build không nằm ở `zone/build`, hoặc tên file khác `ZONE_EXECUTABLE` |
| `is not executable` | `chmod +x` như 11b |
| `the API refuses this service key` | API và zone không cùng `SERVICE_KEY`. Chạy `pv --profile zone up -d` để cả hai nhận lại |
| `the API does not know zone` | `ZONE_ID` lạ: đặt cùng một giá trị, mặc định `frost-hollow` |
| Log zone ổn nhưng người chơi không vào được | Kiểm tra UDP 9050 ở ufw và ở tường lửa nhà cung cấp, và `ZONE_HOST` đúng tên miền. Client và zone cũng phải cùng phiên bản giao thức |

Client phải trỏ API vào `https://pocketvalor.luckyday.io.vn`. Sau khi đăng nhập, API trả cho client địa chỉ zone là `ZONE_HOST` và `ZONE_PORT`.

## Cập nhật

**API** (làm bước 1c và lệnh `scp` gói `.tgz` trên Windows trước):

```bash
cd /opt/pocketvalor
tar -xzf pocketvalor-server.tgz
cd deploy
pv --profile zone up -d --build api
curl -s http://127.0.0.1:3000/health
```

Gói không chứa `.env.prod` và `zone/build`, nên giải nén không ghi đè chúng. Nếu chưa có zone thì bỏ `--profile zone`.

**Zone:** làm lại 11a, 11b, rồi `pv --profile zone up -d --force-recreate zone`. Đổi số liệu skill thì phải build lại zone, vì bảng skill dùng chung với server. Khi dừng, người chơi bị ngắt kết nối với thông báo "Server is restarting", và tiến độ được lưu.

**Đổi file nginx:** sửa rồi luôn chạy `sudo nginx -t` trước `sudo systemctl reload nginx`. Không render lại từ template sau khi đã có certbot, vì sẽ mất phần HTTPS certbot đã thêm.

## Xem log

```bash
pv ps                          # trạng thái
pv logs --tail 100 api         # 100 dòng cuối
pv logs -f api                 # xem liên tục, Ctrl+C để thoát
pv --profile zone logs -f zone
pv logs --tail 50 backup
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

Log của Docker được giới hạn 5 file, mỗi file 10 MB cho mỗi dịch vụ. Trang `/reset` cố ý không ghi vào access log của nginx.

Xem save bị API từ chối: `pv --profile zone exec zone ls /data/save-failed`. Có file trong đó tức là tiến độ của người chơi chưa vào database, cần người xem.

## Backup và khôi phục

Service `backup` tự chạy `mongodump` mỗi ngày lúc 19:00 UTC (2:00 sáng giờ Việt Nam), và ngay khi khởi động nếu bản mới nhất đã quá một ngày. Giữ 14 ngày. Khi dump lỗi, các bản cũ không bị xóa. File nằm ở `/opt/pocketvalor/deploy/backups`.

Backup ngay bây giờ:

```bash
pv exec backup bash /backup-scripts/backup-now.sh
ls -lh /opt/pocketvalor/deploy/backups
```

Thành công là: có dòng `[backup] wrote zenonia-....archive.gz` và thấy file đó.

**Backup nằm trên cùng VPS, nên mất máy là mất cả backup.** Hãy thường xuyên chép về máy khác. Chạy trong PowerShell:

```powershell
scp "<user>@<IP-VPS>:/opt/pocketvalor/deploy/backups/*.archive.gz" D:\pocketvalor-backups\
```

File thuộc về root trong container: nếu `scp` báo không đọc được thì trên VPS chạy `sudo chmod a+r backups/*`. Trong đó có email và hash mật khẩu của người chơi, hãy giữ cẩn thận.

**Khôi phục** (ghi đè toàn bộ database hiện tại):

```bash
cd /opt/pocketvalor/deploy
pv --profile zone stop zone
pv stop api
ls -1t backups
pv exec backup bash /backup-scripts/restore.sh zenonia-YYYYMMDD-HHMMSS.archive.gz --yes
pv --profile zone up -d
curl -s http://127.0.0.1:3000/health
```

Dừng zone trước API, để lượt lưu cuối còn đến được API. Nếu file backup vừa chép từ máy khác lên thì đặt vào `backups/` trước.

Chưa có zone thì bỏ `--profile zone` ở dòng `up`. Thành công là: dòng `[restore] done`, `{"ok":true}`, và đăng nhập được bằng tài khoản có trong bản backup.

**Hãy thử khôi phục một lần trước khi có người chơi thật**: chưa ai chạy thử lệnh này với `mongorestore` thật. Bước kiểm tra ở `deploy/test/verify-stack.sh` có làm việc đó.

## Sinh lại bí mật

`deploy/.env.prod` đã có sẵn bí mật. Khi cần tạo mới, ví dụ nghi bị lộ:

```bash
openssl rand -hex 48    # JWT_SECRET
openssl rand -hex 32    # SERVICE_KEY
```

Sửa `.env.prod`, rồi `pv --profile zone up -d` (chưa có zone thì bỏ `--profile zone`). Đổi `JWT_SECRET` thì access token cũ hết hiệu lực và người chơi phải vào lại. Đổi `SERVICE_KEY` thì API và zone phải khởi động lại cùng nhau: lệnh trên làm việc đó.

## Trước khi mở cho người chơi

- `Assets/Resources/Data/dev-start.json` đang bật cheat để test (`noCooldowns`, `noMpCost`, level, vàng), và F10 (`DevBuffKey`) là phím test tạm.
- Mạng di động thường cho nhiều người chung một IP. Giới hạn `REGISTER_IP_MAX=5` tài khoản/giờ và `LOGIN_IP_MAX=30` lần/phút theo IP có thể chặn nhầm. Tăng trong `.env.prod` nếu có người kêu.
- Đã thử khôi phục backup một lần, và có chỗ chép backup ra ngoài VPS.
- Mongo không đặt mật khẩu. Nó chỉ nối được từ các container `api` và `backup` của stack này. Đừng bao giờ thêm `ports` cho `mongo`.
