# WeiKit-light

一个面向 **Windows 桌面（WinForms）** 的 C# 工具库，把配置、日志、加密、网络、内嵌 Web 服务、钉钉/樱花 Frp 推送，以及一组自绘控件打包成 `WeiKit.dll`，供业务程序直接引用。

- 目标框架：`.NET Framework 4.7.2`
- 程序集：`WeiKit`（`WeiKit.dll`）
- 作者：Wei Guang Xin
- 主页：<https://www-home.rsie3cp43.nyat.app:37071/>

---

## ⚠️ 免责声明（请先读这里）

本项目以 **MIT 许可** 发布，这同时意味着它按 **「按原样（AS IS）」** 提供：

> **THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
> IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
> FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
> AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
> LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
> OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
> SOFTWARE.**

用大白话讲：

- **不提供任何明示或默示的担保**——不保证「能用、好用、适合你的用途、无侵权」。
- **不承担任何责任**——无论因合同、侵权还是其他原因，作者/版权方对任何索赔、损害或其他责任概不负责。
- **风险自担**——你自己决定是否使用，也自己承担使用（或无法使用）本软件带来的一切后果，包括但不限于数据丢失、服务中断、商业损失、安全漏洞。

如果你把 `WeiKit` 用于生产环境、金融、医疗、监控报警等关键场景，请务必**自行测试、自行审计、自行兜底**。本项目不提供 SLA，也不提供任何形式的商业支持或质量承诺。

---

## 功能一览

| 模块 | 说明 |
|---|---|
| `Configs` | 配置项管理：增删改查、类型化读写、导入导出、`ConfigChanged` 事件，支持 AES 加密落盘 |
| `Logs` | 日志记录：多级日志、`RedirectConsoleOut` 接管控制台、自动落盘、内置日志窗口 |
| `ConsoleLogTheme` | 日志窗口主题：内置 13 个主题 + 目录化自定义（`themes\*.html`），当前主题持久化 |
| `CryptoHelper` | AES 加解密、文件加密存储 |
| `HttpLink` | HTTP 基础请求：GET/POST、图片下载、`DownTool` 下载器、`UploadTool` 上传器 |
| `WebServer` | 基于 `HttpListener` 的内嵌 Web 服务（软页面 + 访问日志，含路径穿越防护） |
| `DingDingAPI` | 钉钉自定义机器人消息推送（markdown） |
| `SakuraFrpcAPI` | 樱花 Frp 内网穿透接口封装 |
| `ProgTool` | 机器码生成、管理员运行/重启、编译时间、ClickOnce 卸载等 |
| `Tool` | 工具集合：开机自启、路径转换、控件圆角、动画、SHA256、安全跨线程调用、`WindowZoomer` 窗口控件缩放、`DataFlowList` 定长滑动窗口统计等 |
| `States` | 计数与统计字段的自动维护 |
| `WMessageBox` | 自定义消息框：普通/动态/图片消息框、`ShowToast` 边缘滑入的定时提示框（支持相对屏幕或宿主窗口定位），以及文本/开关/数字/日期/颜色/文件/路径等输入询问框 |
| `Comp.*` | 自绘控件：`Switch` / `Led` / `Chart` / `ProgressBar` / `ProgressRing` / `Panels` |
| `Window.*` | 内置窗口：配置管理窗口 `ConfigManag`、日志窗口 `Logsinfo`、消息框 `WMessageBoxBase`、Toast `WToast` |

## 依赖

| 包 | 版本 | 用途 |
|---|---|---|
| `Newtonsoft.Json` | 13.0.3 | JSON 序列化 |
| `Microsoft.Web.WebView2` | 1.0.3650.58 | 日志窗口的内嵌浏览器渲染 |

## 目录结构

```
WeiKit-light/
├── Comp/          # 自绘控件（Chart / Led / Panels / ProgressBar / ProgressRing / Switch）
├── Window/        # 内置窗口（ConfigManag / Logsinfo / WMessageBoxBase / WToast）
├── Resources/     # 界面图标资源
├── demo/          # 可直接运行的示例程序 WeiKit.Demo
├── doc/           # 使用文档（HTML）
├── Configs.cs     # 配置管理
├── Logs.cs        # 日志
├── CryptoHelper.cs# 加解密
├── HttpLink.cs    # 网络请求
├── WebServer.cs   # 内嵌 Web 服务
├── DingDingAPI.cs # 钉钉机器人
├── SakuraFrpcAPI.cs
├── WMessageBox.cs # 消息框静态入口
├── ProgTool.cs / Tool.cs / States.cs / ...
└── WeiKit.csproj / WeiKit.sln
```

## 构建

```powershell
# 还原 + 编译（产出 WeiKit.dll）
msbuild WeiKit.sln /t:Restore,Build /p:Configuration=Release
```

示例程序（含更多用法演示）见 [`demo/README.md`](demo/README.md)。

## 快速上手

```csharp
// ① 接管控制台输出到日志
Logs.RedirectConsoleOut();

// ② 注册配置项并加载
Configs.Add("App.TopMost", Configs.ValueType.Bool, false, "窗口置顶", "是否总在最前");
Configs.Load(configPath, key);   // key 非空时使用 AES 加密

// ③ 写日志
Logs.Println("Hello WeiKit");
```

完整推荐初始化顺序与各模块用法，请参考 `demo/Program.cs` 与 `demo/` 下各示例页。

### 窗口控件缩放（`Tool.WindowZoomer`）

窗口尺寸变化时，按比例同步缩放内部所有控件的大小与位置，可选同时缩放字体。在目标窗口的 `Load` 事件中创建实例即可：

```csharp
// 在窗口类内部声明
private Tool.WindowZoomer _zoomer;

private void MainForm_Load(object sender, EventArgs e)
{
    // 第二个参数：是否同时缩放字体（默认 true）
    _zoomer = new Tool.WindowZoomer(this, fontzoomer: true);
}
```

## 许可（License）

本项目基于 **MIT License** 发布，你可以自由地使用、复制、修改、合并、发布、再授权，但**必须保留版权声明与许可声明**。

再次强调：MIT 许可**不含任何担保、也不承担任何责任**，详见上方「免责声明」。

> MIT License 完整许可文本以仓库内 `LICENSE` 文件为准（如尚未单独放置，可自行按标准 MIT 模板添加；Copyright © 2025 Wei Guang Xin）。
