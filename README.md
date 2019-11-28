# PixivDownloader
无需代理 下载p站图片

#### 如何使用
1. 递归克隆此项目 `git clone --recursive https://github.com/Feng-Bu-Jue/PixivDownloader.git`
2. 确认已安装 [dotnet core sdk2.1](https://dotnet.microsoft.com/download/dotnet-core/2.1 "Heading link") 和 Visual Studio2017
3. 在 `HangfireServer/appsettings.json` 里配置你的p站账号
4. 运行HangfireServer项目并访问 `https://localhost:5001/run?keyword=这里输入想要下载的关键词(默认是明日方舟)`

### 其他
如果你想使用 dotnet 直连访问(无需代理) p站API 可以使用这个仓库 [PixivApi](https://github.com/Feng-Bu-Jue/PixivApi "Heading link")
