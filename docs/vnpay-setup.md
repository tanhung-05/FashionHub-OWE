# VNPAY sandbox setup

FashionHub implements the VNPAY PAY 2.1.0 redirect flow with HMAC-SHA512.
Card or bank credentials are entered only on VNPAY; FashionHub never stores them.

## 1. Register sandbox credentials

Register a test merchant at:

- https://sandbox.vnpayment.vn/devreg/

VNPAY supplies:

- `vnp_TmnCode`: merchant website code
- `vnp_HashSecret`: checksum secret

Do not commit either value. The official integration guide is:

- https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html

## 2. Configure Visual Studio or dotnet run

From `FashionHub2/FashionHub.Web`:

```powershell
dotnet user-secrets set "VnPay:TmnCode" "YOUR_TMN_CODE"
dotnet user-secrets set "VnPay:HashSecret" "YOUR_HASH_SECRET"
dotnet user-secrets set "VnPay:PaymentUrl" "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
dotnet user-secrets set "VnPay:ReturnUrl" "https://localhost:44306/payment/vnpay-return"
```

Use the HTTPS port shown by the active Visual Studio profile. Restart the app
after changing secrets.

## 3. Configure Docker

Set these values in the ignored `FashionHub2/.env` file:

```env
VNPAY_PAYMENT_URL=https://sandbox.vnpayment.vn/paymentv2/vpcpay.html
VNPAY_TMN_CODE=YOUR_TMN_CODE
VNPAY_HASH_SECRET=YOUR_HASH_SECRET
VNPAY_RETURN_URL=http://localhost:5167/payment/vnpay-return
VNPAY_TRANSACTION_API_URL=https://sandbox.vnpayment.vn/merchant_webapi/api/transaction
```

Then rebuild the web container:

```powershell
cd FashionHub2
docker compose up -d --build web
```

When the credentials are missing, FashionHub hides VNPAY from checkout and COD
continues to work. `/health` reports VNPAY as degraded until it is configured.

## 4. Apply the database upgrade

Back up the database, then run:

```powershell
sqlcmd -S localhost,1433 -d QL_SHOPQUANAO_PRO -U sa -P "YOUR_PASSWORD" `
  -C -b -i docs/database-add-vnpay-payments.sql
```

The script is idempotent. `DB_Fixed.sql` already includes the same schema for a
new database.

## 5. Return URL and IPN

- Return URL: `https://your-domain/payment/vnpay-return`
- IPN URL: `https://your-domain/payment/vnpay-ipn`

Return URL displays the result to the customer. IPN is the server-to-server
notification used to persist payment status. VNPAY cannot call an IPN on
`localhost`; a public HTTPS staging domain or a temporary HTTPS tunnel is needed
for end-to-end sandbox verification.

The implementation rejects invalid signatures, unknown references, and amount
mismatches. Repeated successful callbacks are handled without charging or
updating the order twice.

Pending VNPAY transactions older than 24 hours are reconciled with the signed
`querydr` API every 15 minutes. A verified paid response updates the order even
if its IPN was missed. Verified not-found, incomplete, or failed transactions
are expired, with inventory, coupon usage, and history restored atomically.
Invalid or unavailable gateway responses never cancel an order.

## 6. Sandbox test data

Use only VNPAY's published sandbox cards, never a real card. Current test data
and status are listed at:

- https://sandbox.vnpayment.vn/apis/docs/gioi-thieu/

Before production, complete VNPAY's merchant onboarding and test cases, replace
the sandbox endpoint and credentials, and verify IPN from the public host.
