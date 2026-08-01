# FashionHub Postman

Import both JSON files into Postman and select the `FashionHub Local`
environment.

1. Fill customer/admin email and password locally. Do not export real secrets.
2. Run `Setup / Get CSRF Token`.
3. Run a login request.
4. Run `Setup / Get CSRF Token` again because the identity changed.
5. Run requests in Products, Cart, Account, Orders, Chat, or Admin.

Postman keeps the cookie authentication session in its cookie jar. The
collection-level pre-request script adds the current antiforgery token to
`POST`, `PUT`, `PATCH`, and `DELETE` requests.

For the HTTPS Visual Studio profile, Postman may ask you to trust the ASP.NET
Core development certificate. Keep SSL verification enabled after the
certificate is trusted.
