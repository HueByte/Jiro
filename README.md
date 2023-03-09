<p align="center">
    <img src="assets/JiroBanner.png" style="border-radius: 15px;"/>
</p>

# ✨ `Jiro`
### Virtual Assistant powered by ChatGPT and custom code integration! 
This combination of ChatGPT's powerful AI capabilities and custom plugins enables Jiro to provide a wide range of services and support, including answering questions, assisting with tasks, providing recommendations, and much more. Whether users need help with work, school, or just day-to-day life, Jiro is there to lend a helping hand.

# ⚗️ Dev
## `Requirements`
- dotnet SDK
- Python
- Node
- OpenAI account (Optional)

## How to run
### **Jiro API**
1. navigate to `src/Jiro.Kernel/Jiro.Api`
2. rename `appsettings.example.json` to `appsettings.json`
3. Configure `appsettings.json`, especially `Gpt:AuthToken` for enabling conversations (can be obtained from https://platform.openai.com/account/api-keys)
4. run `dotnet run`
  
### **Jiro Tokenizer API**
1. navigate to `src/Jiro.TokenApi`
2. run `pip install -r requirements.txt`
3. run `uvicorn main:app --reload`

### **Jiro Web App**
1. navigate to `src/Jiro.Client`
2. rename `appsettings.example.json` to `appsettings.json` and configure it up to your needs
3. navigate to `src/Jiro.Client/clientapp/envExamples` and rename example files to `.env` and `.env.development`
4. move these renamed files to `src/Jiro.Client/clientapp`
5. run `dotnet run`
6. to run Client App, open the web app (by default `https://localhost:5001`) and wait for proxy to start 

## Base Dev Flow of Jiro
![DevFlow](assets/JiroDevFlow.png)

## Important configs
The default configs should assure that Jiro will run.<br />
If you want to run apps on your own custom urls and configs, this might be useful for you

### API
> appsettings.json

| Key | Description | Default Value |
| --- | --- | --- |
| urls | The urls used for API hosting | `http://localhost:18090;https://localhost:18091` |
| TokenizerUrl | The url for tokenizer API | `http://localhost:8000` |
| GPT:BaseUrl | The url for OpenAI API | `https://api.openai.com/v1/` |
| GPT:AuthToken | The Authorization token for GPT | *Obtain it from https://platform.openai.com/account/api-keys |

### Web
Read more about it [here](https://learn.microsoft.com/en-us/aspnet/core/client-side/spa/intro?view=aspnetcore-7.0)

- Web server
> appsettings.json

| Key | Description | Default Value |
| --- | --- | --- |
| urls | The urls used for web server | http://localhost:5000;https://localhost:5001 |

> Properties/launchSettings.json (for VisualStudio)

| Key | Description | Default Value |
| --- | --- | --- |
| profiles:Jiro.Client:applicationUrl | The urls used for web server | http://localhost:5000;https://localhost:5001 |

> clientapp/.env 
 
| Key | Description | Default Value |
| --- | --- | --- |
| API_URL | url for proxy that targets API | https://localhost:18091 |


> clientapp/.env.development

| Key | Description | Default Value |
| --- | --- | --- |
| PORT | url that client app will run on | 3000

> Jiro.Client.csproj

| Key | Description | Default Value |
| --- | --- | --- |
| SpaProxyServerUrl | url that's used for running client app | https://localhost:3000 |

### Matching values
- (API)`TokenizerUrl` must match url configured for Tokenizer API
- (Web)`SpaProxyServerUrl` must match resulting url from (clientapp)`PORT`
- (clientapp)`API_URL` must match one of the urls configured in (API)`urls`