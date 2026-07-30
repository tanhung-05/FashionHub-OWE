# Password reset setup

## 1. Update an existing database

Run `docs/database-add-password-reset.sql` against `QL_SHOPQUANAO_PRO`.
The script is idempotent and preserves existing data.

`DB_Fixed.sql` already contains the same schema for a fresh database.

## 2. Configure SMTP locally

Run these commands from `FashionHub2/FashionHub.Web`:

```powershell
dotnet user-secrets set "Email:Host" "smtp.example.com"
dotnet user-secrets set "Email:Port" "587"
dotnet user-secrets set "Email:EnableSsl" "true"
dotnet user-secrets set "Email:UserName" "smtp-user"
dotnet user-secrets set "Email:Password" "smtp-password-or-app-password"
dotnet user-secrets set "Email:FromEmail" "no-reply@example.com"
dotnet user-secrets set "Email:FromName" "OWE"
dotnet user-secrets set "PasswordReset:PublicBaseUrl" "http://localhost:5197"
```

Use the SMTP host, port, username, and password supplied by the selected
email provider. Do not place credentials in `appsettings.json` or commit them
to Git.

## 3. Configure production

Use environment variables or the deployment platform's secret store:

```text
Email__Host
Email__Port
Email__EnableSsl
Email__UserName
Email__Password
Email__FromEmail
Email__FromName
PasswordReset__PublicBaseUrl
```

`PasswordReset__PublicBaseUrl` must be an HTTPS URL in Production.

## 4. Verify

1. Open `/Account/Login`.
2. Select **Quên mật khẩu?**.
3. Submit an existing account email.
4. Open the received link and choose a new password.
5. Confirm the link cannot be reused and the old password no longer works.
