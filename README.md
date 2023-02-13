# ✨ `Jiro `
### Virtual Assistant powered by ChatGPT (once API gets released) and custom code integration! 
### This assistant combines the cutting-edge language processing capabilities of OpenAI's ChatGPT with the flexibility and awesomeness of Jiro plugins.

# ⚗️ Dev
## `Requirements`
- dotnet SDK
- Python
- Node

## How to run
### **Jiro API**
1. navigate to Jiro.Kernel/Jiro.Api
2. rename `appsettings.example.json` to `appsettings.json` and configure it up to your needs
3. run `dotnet run`
  
### **Jiro Console Client**
1. navigate to Jiro.Communication
2. run `pip install -r requirements.txt`
3. run `py main.py` or `python main.py`

### **Jiro Web App**
1. navigate to Jiro.Client
2. rename `appsettings.example.json` to `appsettings.json` and configure it up to your needs
3. navigate to Jiro.Client/clientapp/envExamples and rename example files to `.env` and `.env.development`
4. move these renamed files to Jiro.Client/clientapp
5. dotnet run
6. to run Client App, open the web app and wait for proxy to start 

> `ASPNETCORE_HTTPS_PORT` is used to configure proxy for Jiro API (should point to your API port)

>  `PORT` is used to configure dev client app port, while changing it - you should also change the port of `SpaProxyServerUrl` in `Jiro.Client.csproj`

> `HTTPS` is used to enable HTTPS 
