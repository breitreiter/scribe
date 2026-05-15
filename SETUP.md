---
superseded_by: imp/reference/azure-services-setup.md
---

# Scribe - Azure Services Setup Guide

This guide walks you through setting up all the Azure services required to run Scribe.

## Prerequisites

- Azure subscription ([create a free account](https://azure.microsoft.com/free/))
- Azure CLI installed (optional, but recommended)

## Services You'll Need

1. **Azure AI Speech** - For fast transcription with speaker diarization
2. **Azure OpenAI** - For generating summaries and processing transcripts

---

## 1. Azure AI Speech Service

### Create the Service

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **"Create a resource"**
3. Search for **"Speech"** or navigate to **AI + Machine Learning → Speech**
4. Click **"Create"**
5. Fill in the details:
   - **Subscription**: Your Azure subscription
   - **Resource group**: Create new or use existing (e.g., `scribe-resources`)
   - **Region**: Choose a region (e.g., `East US`)
   - **Name**: Give it a unique name (e.g., `scribe-speech-service`)
   - **Pricing tier**:
     - **Free (F0)**: Good for testing
     - **Standard (S0)**: For production use
6. Click **"Review + create"** then **"Create"**
7. Wait for deployment to complete

### Get Your Credentials

1. Go to your Speech Service resource
2. In the left menu, click **"Keys and Endpoint"** (under Resource Management)
3. Copy:
   - **KEY 1** (or KEY 2) → This is your `ApiKey`
   - **Location/Region** → This is your `Region` (e.g., `eastus`)
   - **Endpoint** → Should look like `https://eastus.api.cognitive.microsoft.com`

### Configure in appsettings.json

```json
"AzureSpeech": {
  "Endpoint": "https://eastus.api.cognitive.microsoft.com",
  "ApiKey": "your-key-from-above",
  "Region": "eastus",
  "Locale": "en-US"
}
```

**Important Notes:**
- The endpoint format is `https://{region}.api.cognitive.microsoft.com` (no custom name in URL)
- Fast Transcription supports files up to 300 MB and less than 2 hours duration
- Transcription is synchronous and returns results quickly (faster than real-time)

---

## 2. Azure OpenAI Service

### Create the Service

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **"Create a resource"**
3. Search for **"Azure OpenAI"**
4. Click **"Create"**
5. Fill in the details:
   - **Subscription**: Your Azure subscription
   - **Resource group**: Use the same (e.g., `scribe-resources`)
   - **Region**: Choose a region that supports GPT-4o-mini (e.g., `East US 2`, `Sweden Central`)
   - **Name**: Unique name (e.g., `scribe-openai`)
   - **Pricing tier**: Standard S0
6. Click **"Review + submit"** then **"Create"**
7. Wait for deployment to complete

**Note**: Azure OpenAI requires application approval. If you haven't been approved yet, apply at [https://aka.ms/oai/access](https://aka.ms/oai/access). Approval typically takes 1-2 business days.

### Deploy GPT-4o-mini Model

1. Go to your Azure OpenAI resource
2. Click **"Go to Azure OpenAI Studio"** (or navigate to [https://oai.azure.com](https://oai.azure.com))
3. In Azure OpenAI Studio, click **"Deployments"** (in the left menu)
4. Click **"+ Create new deployment"**
5. Fill in:
   - **Select a model**: Choose `gpt-4o-mini`
   - **Model version**: Use the latest available
   - **Deployment name**: `gpt-4o-mini` (or your preferred name)
   - **Deployment type**: Standard
   - **Tokens per minute rate limit**: 30K (or higher if available)
6. Click **"Create"**

### Get Your Credentials

1. Go back to Azure Portal → Your OpenAI resource
2. In the left menu, click **"Keys and Endpoint"**
3. Copy:
   - **KEY 1** → This is your `ApiKey`
   - **Endpoint** → Should look like `https://scribe-openai.openai.azure.com/`
4. Note your **Deployment name** from the deployment step above

### Configure in appsettings.json

```json
"Completion": {
  "Provider": "AzureOpenAI",
  "AzureOpenAI": {
    "Endpoint": "https://scribe-openai.openai.azure.com/",
    "ApiKey": "your-key-from-above",
    "DeploymentName": "gpt-4o-mini",
    "ModelName": "gpt-4o-mini"
  }
}
```

---

## Complete Configuration Example

Your final `appsettings.json` should look like this:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "System": "Warning",
      "Microsoft": "Warning"
    }
  },
  "Transcription": {
    "AzureSpeech": {
      "Endpoint": "https://eastus.api.cognitive.microsoft.com",
      "ApiKey": "your-speech-api-key-here",
      "Region": "eastus",
      "Locale": "en-US"
    }
  },
  "Completion": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "https://your-openai.openai.azure.com/",
      "ApiKey": "your-openai-key-here",
      "DeploymentName": "gpt-4o-mini",
      "ModelName": "gpt-4o-mini"
    }
  }
}
```

---

## Testing Your Setup

1. Build the project:
   ```bash
   dotnet build
   ```

2. Run with a small test audio file:
   ```bash
   dotnet run -- /path/to/test-audio.mp3
   ```

3. Watch for:
   - ✅ Configuration validation passes
   - ✅ Transcription starts
   - ✅ Transcription completes quickly (faster than real-time)
   - ✅ Speaker diarization identifies multiple speakers

---

## Troubleshooting

### "Resource not found" or HTTP 404
- Double-check your endpoints don't have typos
- Ensure endpoint format is correct (no trailing `/` issues)
- Verify your API keys are copied correctly

### "Invalid credentials" or HTTP 401
- Your API key might be wrong or expired
- Try regenerating keys in Azure Portal
- Make sure you're using the right key for the right service

### "Could not find a part of the path"
- Make sure your audio file path is absolute, not relative
- Check for spaces in the path (use quotes if needed)

### "File too large" errors
- Fast Transcription supports files up to 300 MB
- Files must be less than 2 hours in duration
- Supported formats: WAV, MP3, OPUS/OGG, FLAC, WMA, AAC, ALAW, MULAW, AMR, WebM, and SPEEX

---

## Cost Estimation

**For a typical 1-hour meeting:**

| Service | Cost |
|---------|------|
| Azure AI Speech (fast transcription) | ~$0.36 |
| Azure OpenAI (GPT-4o-mini completion) | ~$0.15 |
| **Total per 1-hour meeting** | **~$0.51** |

**Monthly costs (if idle):**
- Speech Service: $0 (pay per use only)
- OpenAI: $0 (pay per use only)

**Tips to reduce costs:**
- Use smaller audio files for testing
- Consider batch processing multiple files
- Monitor usage in Azure Cost Management

---

## Security Best Practices

1. **Never commit appsettings.json**
   - It's already in `.gitignore`
   - Use environment variables for production deployments

2. **Rotate keys periodically**
   - Azure Portal → Your resource → Keys → Regenerate

3. **Use Managed Identities in production**
   - Avoids storing keys in config files
   - See: [Azure Managed Identity docs](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)

---

## Next Steps

Once your services are configured:
1. Test with a small audio file (< 5 minutes)
2. Verify the transcription completes successfully
3. Check that speaker diarization is working (multiple speakers detected)
4. Move on to implementing speaker name assignment and summary generation

## Support

- Azure AI Speech docs: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/
- Azure AI Fast Transcription docs: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/fast-transcription-create
- Azure OpenAI docs: https://learn.microsoft.com/en-us/azure/ai-services/openai/
