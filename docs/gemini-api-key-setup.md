# Gemini API Key Configuration

## Security Notice

The Gemini API key is **NOT** hardcoded in the application for security reasons. You must configure it using User Secrets (for development) or environment variables (for production).

## Development Setup (User Secrets)

### Step 1: Navigate to Project Directory
```bash
cd FashionHub2/FashionHub.Web
```

### Step 2: Confirm User Secrets support

The web project already has a `UserSecretsId`, so no initialization command is
needed.

### Step 3: Set the Gemini API Key
```bash
dotnet user-secrets set "GeminiAI:ApiKey" "YOUR_ACTUAL_GEMINI_API_KEY_HERE"
```

### Step 4: Verify the key name without printing secret values
```powershell
dotnet user-secrets list |
    ForEach-Object { ($_ -split ' = ', 2)[0] }
```

You should see:
```
GeminiAI:ApiKey
```

## Production Setup (Environment Variables)

For production deployment, set the API key as an environment variable:

### Azure App Service
```bash
az webapp config appsettings set --name <app-name> --resource-group <resource-group> --settings GeminiAI__ApiKey="YOUR_KEY"
```

### Docker
```bash
docker run -e GeminiAI__ApiKey="YOUR_KEY" your-image
```

### Docker Compose
```yaml
services:
  web:
    environment:
      - GeminiAI__ApiKey=YOUR_KEY
```

### appsettings.json (NOT RECOMMENDED for production)
If you absolutely must use appsettings.json, add to `appsettings.Production.json`:
```json
{
  "GeminiAI": {
    "ApiKey": "YOUR_KEY"
  }
}
```
⚠️ **Warning**: Never commit this file with real API keys to git!

## Getting a Gemini API Key

1. Go to [Google AI Studio](https://aistudio.google.com/app/apikey)
2. Sign in with your Google account
3. Click "Create API Key"
4. Copy the generated key
5. Follow the setup steps above

## Troubleshooting

### Chat uses the grounded fallback instead of Gemini

When the key is missing, invalid, timed out or the Gemini API is unavailable,
the chat remains usable and responds from the grounded server-side context.
To enable Gemini-generated wording:

1. **Check User Secrets**: verify the key name with the safe command above.
2. **Verify Key Name**: Ensure it's exactly `GeminiAI:ApiKey` (case-sensitive)
3. **Restart Application**: After setting secrets, restart the development server

### Error: 401 Unauthorized from Gemini API

This means your API key is invalid or expired:

1. Verify the key at [Google AI Studio](https://aistudio.google.com/app/apikey)
2. Generate a new key if needed
3. Update your User Secrets with the new key

## Security Best Practices

✅ **DO**:
- Use User Secrets for local development
- Use environment variables for production
- Rotate API keys regularly
- Limit API key permissions/quotas

❌ **DON'T**:
- Commit API keys to git
- Share API keys in chat/email
- Hardcode API keys in source code
- Use production keys in development

## Additional Configuration

The Gemini API URL can also be configured (optional):

```bash
dotnet user-secrets set "GeminiAI:ApiUrl" "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent"
```

The default URL, timeout and output limit are already configured in
`appsettings.json`. The API key is sent in the `x-goog-api-key` header and is
never added to the request URL or application logs.
