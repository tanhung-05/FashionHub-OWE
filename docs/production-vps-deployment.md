# Deploy FashionHub len VPS cho nguoi moi

Tai lieu nay dung cho lan deploy dau tien cua FashionHub voi kien truc:

```text
Nguoi dung
    |
    | https://shop.example.com
    v
DNS -> IP cong cua VPS -> Caddy (HTTPS) -> ASP.NET Core
                                               |
                                               v
                                      SQL Server noi bo
```

Sau khi deploy, may tinh ca nhan co the tat. VPS tiep tuc chay web va database.
Nguoi dung khong truy cap SQL Server; chi ung dung ASP.NET Core duoc ket noi voi
SQL qua Docker network noi bo.

Domain da dang ky cho project nay la `oweshop.io.vn`. Cac cho dung
`shop.your-domain.com` ben duoi la vi du tong quat; khi thuc hien cho FashionHub,
dung `oweshop.io.vn`.

## 1. Phuong an nay phu hop o muc nao?

Mot VPS chay Docker la phuong an hop ly cho portfolio, demo phong van va mot
nhom nho nguoi dung thu. No giup the hien kien thuc Linux, Docker, DNS, HTTPS,
backup va van hanh.

Day chua phai kien truc cho cua hang lon vi web va database cung nam tren mot
server. Neu VPS hong, ca hai cung dung. Khi co nguoi dung va doanh thu that, nen
chuyen SQL sang managed database, anh sang object storage, them monitoring,
staging va co ke hoach disaster recovery.

## 2. Can mua va chuan bi gi?

### Ten mien

Mua mot ten mien de nho, vi du `owe-store.com`, sau do dung mot hostname nhu
`shop.owe-store.com`. Khong dang ky mot domain cua nguoi khac va khong dung URL
ngau nhien cua website khac.

Ten mien va VPS la hai thu rieng:

- Ten mien la dia chi nguoi dung go vao trinh duyet.
- VPS la may chu luon bat, co dia chi IPv4 cong.
- DNS record loai `A` noi ten mien voi IPv4 cua VPS.

### VPS

Chon VPS Linux co:

- Ubuntu 24.04 LTS x86-64.
- Toi thieu 2 vCPU, 4 GB RAM va 50 GB SSD.
- Khuyen nghi 8 GB RAM neu ngan sach cho phep, vi SQL Server kha ton RAM.
- IPv4 cong tinh.
- Snapshot hoac backup cua nha cung cap neu co.

Khong chon ARM cho cau hinh nay. SQL Server Linux container chi duoc Microsoft
ho tro tren CPU Intel/AMD x86-64.

### Tai khoan dich vu

Chuan bi:

- GitHub repository chua source code.
- Gmail App Password moi cho quen mat khau.
- Gemini API key neu bat chatbot.
- VNPAY sandbox Terminal ID va Hash Secret moi.
- Email dung de Caddy dang ky chung chi HTTPS.

Secret VNPAY va Gmail da tung duoc chia se trong chat phai duoc rotate truoc khi
deploy. Khong commit `.env.production` len Git.

## 3. Nhung file production da co

- `FashionHub2/compose.production.yml`: web, SQL Express, Caddy va volumes.
- `FashionHub2/Caddyfile`: reverse proxy va HTTPS tu dong.
- `FashionHub2/.env.production.example`: mau cau hinh khong chua secret that.
- `FashionHub2/scripts/deploy-production.sh`: validate, build va khoi dong.
- `FashionHub2/scripts/backup-production.sh`: tao va verify file `.bak`.
- `FashionHub2/scripts/promote-admin.sh`: cap quyen admin cho tai khoan da dang ky.

Cau hinh production chi public cong `80` va `443`. SQL Server va cong `8080`
cua ASP.NET Core khong duoc map ra Internet. Web ket noi bang login
`fashionhub_app` chi co quyen doc/ghi va execute; `sa` chi duoc dung boi script
khoi tao, backup va quan tri.

SQL Server su dung edition `Express`, duoc phep dung production mien phi cho
workload nho. Compose local van su dung `Developer`, chi danh cho dev/test.

## 4. Chuan bi VPS

Dang nhap lan dau bang SSH theo huong dan cua nha cung cap:

```bash
ssh root@YOUR_VPS_IP
```

Cap nhat may va cai cong cu co ban:

```bash
apt update
apt upgrade -y
apt install -y ca-certificates curl git ufw
```

Cai Docker Engine va Docker Compose plugin theo huong dan Ubuntu chinh thuc:

- https://docs.docker.com/engine/install/ubuntu/
- https://docs.docker.com/compose/install/linux/

Kiem tra:

```bash
docker --version
docker compose version
```

Mo firewall:

```bash
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 443/udp
ufw enable
ufw status
```

Docker co quy tac firewall rieng. Khong them mapping cong SQL `1433` vao Compose
production. Neu SSH dung cong khac, mo cong do truoc khi bat UFW.

## 5. Dua source code len VPS

Vi du dat project tai `/opt/fashionhub`:

```bash
cd /opt
git clone YOUR_GITHUB_REPOSITORY_URL fashionhub
cd /opt/fashionhub/FashionHub2
```

Neu repository private, dung deploy key hoac GitHub CLI; khong luu mat khau
GitHub trong source.

Tao file secret production:

```bash
cp .env.production.example .env.production
nano .env.production
chmod 600 .env.production
```

Phai thay tat ca placeholder. Ba gia tri sau phai cung hostname:

```dotenv
DOMAIN=oweshop.io.vn
PUBLIC_BASE_URL=https://oweshop.io.vn
VNPAY_RETURN_URL=https://oweshop.io.vn/payment/vnpay-return
```

Tao `SA_PASSWORD` va `APP_DB_PASSWORD` ngau nhien, khac nhau, dai va khong trung
mat khau ca nhan. `APP_DB_PASSWORD` toi thieu 20 ky tu, khong chua dau nhay don
hoac chuoi `$(`. File
`.env.production` bi Git ignore va chi nam tren VPS.

## 6. Tro DNS

Tai noi quan ly DNS cua domain, tao hai record:

```text
Type  Name  Content          TTL
A     @     YOUR_VPS_IPV4    Auto
A     www   YOUR_VPS_IPV4    Auto
```

Neu dung Cloudflare, de `DNS only` trong lan cap chung chi dau tien. Kiem tra DNS:

```bash
dig +short oweshop.io.vn
dig +short www.oweshop.io.vn
```

Ket qua phai la IPv4 cua VPS. DNS co the can vai phut den vai gio de cap nhat.

## 7. Deploy lan dau

Tai thu muc `FashionHub2` tren VPS:

```bash
chmod +x scripts/*.sh
./scripts/deploy-production.sh
```

Script se:

1. Kiem tra `.env.production` va placeholder.
2. Validate Compose.
3. Build image ASP.NET Core Release.
4. Khoi dong SQL Server Express.
5. Khoi tao database moi tu `DB_Fixed.sql`.
6. Khoi dong web va Caddy.

Caddy tu xin va gia han chung chi HTTPS khi DNS va cong `80/443` dung.

Kiem tra:

```bash
docker compose --env-file .env.production -f compose.production.yml ps
docker compose --env-file .env.production -f compose.production.yml logs --tail=100 caddy web
curl -I https://oweshop.io.vn
curl https://oweshop.io.vn/health
```

Khong chay `docker compose down -v`; tuy chon `-v` xoa database, anh, khoa cookie
va chung chi trong volumes.

## 8. Tao tai khoan admin cua ban

1. Mo website public va dang ky bang email cua ban.
2. Tren VPS, chay:

```bash
./scripts/promote-admin.sh your-registered-email@example.com
```

3. Dang xuat va dang nhap lai de cookie nhan role Admin.

Khong seed mat khau admin production vao Git va khong sua role cua email la.

## 9. Cau hinh VNPAY sandbox

Trong merchant sandbox, dung URL cua chinh domain:

```text
Return URL: https://oweshop.io.vn/payment/vnpay-return
IPN URL:    https://oweshop.io.vn/payment/vnpay-ipn
```

IPN phai truy cap duoc tu Internet qua HTTPS. Sandbox chi tao giao dich thu;
khong thu tien that. Muon thanh toan ngan hang that can merchant production duoc
VNPAY phe duyet va bo credential production rieng.

## 10. Backup database

Tao backup thu cong:

```bash
./scripts/backup-production.sh
```

Script dung `BACKUP DATABASE ... WITH CHECKSUM`, chay `RESTORE VERIFYONLY`, sau
do copy file vao `FashionHub2/backups/sqlserver` tren VPS.

Backup khong bat `COMPRESSION` vi SQL Server Express 2022 khong ho tro tao
compressed backup.

Day van chua phai disaster recovery neu file chi nam cung VPS. Can dong bo file
`.bak` sang object storage, may khac hoac storage cua nha cung cap. Anh san pham
trong volume `product_images` cung can backup rieng.

Sau khi da chay backup thu cong thanh cong, co the dat cron hang ngay. Vi du luc
02:00 UTC:

```cron
0 2 * * * cd /opt/fashionhub/FashionHub2 && ./scripts/backup-production.sh >> /var/log/fashionhub-backup.log 2>&1
```

Backup chi dang tin khi da thu restore tren moi truong test.

## 11. Cap nhat phien ban

Truoc moi lan cap nhat:

```bash
cd /opt/fashionhub/FashionHub2
./scripts/backup-production.sh
cd ..
git pull --ff-only
cd FashionHub2
./scripts/deploy-production.sh
```

Neu co thay doi schema, khong chay lai `DB_Fixed.sql` tren database co du lieu.
Phai dung SQL upgrade script da test, backup truoc va ap dung mot lan.

Sau deploy, smoke test toi thieu:

- Trang chu va danh sach/chi tiet san pham.
- Dang ky, dang nhap, quen mat khau.
- Gio hang, dia chi, checkout COD.
- VNPAY sandbox Return URL va IPN.
- Don hang cua toi va huy don.
- Admin san pham, don hang va bao cao.
- `/health` va logs khong co exception moi.

## 12. Van hanh hang ngay

Lenh can nho:

```bash
docker compose --env-file .env.production -f compose.production.yml ps
docker compose --env-file .env.production -f compose.production.yml logs -f web
docker compose --env-file .env.production -f compose.production.yml logs -f caddy
docker stats
df -h
```

Moi thang nen cap nhat OS/Docker co kiem soat, xem dung luong dia, kiem tra backup
va rotate secret khi co nghi ngo lo thong tin.

## 13. Checklist truoc khi gui link vao CV

- Domain HTTPS hoat dong, khong co canh bao certificate.
- Khong co secret trong Git/GitHub history.
- SQL `1433` khong mo public.
- Tai khoan admin cua ban dung duoc; demo account khong co mat khau cong khai.
- VNPAY duoc ghi ro la Sandbox.
- Email reset password gui link dung domain.
- Backup database va anh da duoc tao, copy ra ngoai VPS va thu restore.
- Build/test CI tren `main` dang xanh.
- Co anh chup, tai khoan demo khach hang va README mo ta kien truc.

## Tai lieu chinh thuc

- Docker Engine tren Ubuntu: https://docs.docker.com/engine/install/ubuntu/
- Docker Compose plugin: https://docs.docker.com/compose/install/linux/
- SQL Server Linux containers: https://learn.microsoft.com/sql/linux/containers/deploy
- ASP.NET Core sau reverse proxy: https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer
- Caddy automatic HTTPS: https://caddyserver.com/docs/automatic-https
- Cloudflare DNS records: https://developers.cloudflare.com/dns/manage-dns-records/how-to/create-dns-records/
