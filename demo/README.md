# WeiKit-light 示例演示程序（WeiKit.Demo）

一个**可直接运行**的 WinForms 示例，用真实读写文件/网络的方式演示 WeiKit-light（程序集 `WeiKit.dll`）精简后的全部能力：配置、日志、加密解密、程序工具、网络，以及 6 个自绘控件。

## 一、如何运行

### 方式 1：Visual Studio

1. 打开 `demo\Demo.sln`（`Demo.csproj` 通过 `ProjectReference` 引用上层的 `WeiKit.csproj`，会一并编译）。
2. 设 `WeiKit.Demo` 为启动项目，直接 F5。

### 方式 2：命令行（MSBuild）

```powershell
# 还原 + 编译（会同时产出 WeiKit.dll 与 WeiKit.Demo.exe）
msbuild demo\Demo.sln /t:Restore,Build /p:Configuration=Debug

# 运行界面
.\demo\bin\Debug\WeiKit.Demo.exe
```

编译产物在 `demo\bin\Debug\`，其中已自动包含 `WeiKit.dll`、`Newtonsoft.Json.dll`、
`Microsoft.Web.WebView2.*.dll` 与 `runtimes\win-x64\native\WebView2Loader.dll`（日志窗口需要）。

### 方式 3：无界面自检（推荐先跑这个）

```powershell
cd demo\bin\Debug
.\WeiKit.Demo.exe /smoke
```

自检会真实执行 87 项断言（配置读写、加密往返、篡改检测、日志落盘、工具函数、数据流统计等），
逐项打印 `[PASS]` / `[FAIL]` / `[SKIP]`，最后以**退出码 0（全通过）或 1（有失败）**结束，适合做冒烟测试。

结果同时写入 `DemoData\smoke_result.txt`（UTF-8）—— 命令行控制台编码千差万别，看文件更可靠。

> `WinExe` 是 GUI 子系统程序，用 `>` 重定向 stdout 到文件是**拿不到内容**的（Windows 行为，非本程序问题）；
> 请用管道、或直接看上面的结果文件。

## 二、界面结构

主窗体用 `TabControl` 分成 7 页，每页对应一个模块；底部状态栏实时显示配置路径与日志条数。

| 标签页 | 演示内容 |
|---|---|
| **首页** | 快捷入口（打开日志/配置窗口）、当前运行环境、五个模块的演示索引 |
| **① 配置管理** | 8 个配置项的增删改查、`Read/WriteValue/WriteStrValue` 差异、`Reset`、导入导出、配置管理窗口（树形分组 + 搜索）、配置事件计数 |
| **② 日志・加密解密** | 四种级别日志、`MaxLogPcs` 自动落盘、日志窗口（库托管 / 自建 + 主题切换）、导出、AES 加密落盘与解密、**篡改 1 字节后解密**、错误密码解密、密文十六进制查看 |
| **③ 程序工具** | 机器码、编译时间、文件 SHA256、随机串、`RunCmdCode`、路径转换、随机目录、注册表自启动、`DataFlowList` 统计与两种异常演示 |
| **④ 网络** | GET/POST、图片下载、`DownTool`（含进度/速度/剩余、模式选择、取消、大小探测）、`UploadTool`、内置 `WebServer`（软页面 + 访问日志）、钉钉机器人 |
| **⑤ 自绘控件** | `Switch` / `Led` / `Chart`（实时动画曲线）/ `ProgressBar`（含越界演示）/ `ProgressRing` / `Panels`（与普通 Panel 对照） |
| **关于** | 库信息、API 速查、Demo 中已规避的陷阱清单 |

菜单栏还提供：打开程序/工作目录、日志与配置窗口、窗口置顶、管理员重启、重启程序、导出日志。

## 三、工作目录与文件

程序启动时会创建 `<exe目录>\DemoData\`，所有演示产物都在里面，便于清理：

| 文件 | 说明 | 由谁产生 |
|---|---|---|
| `demo.config` | **AES 加密**的配置文件（用记事本打开是乱码） | `Configs.Save(path, key)` |
| `secret.dat` | 加密演示文件：`[32 字节随机盐] + [AES-CBC 密文]` | `CryptoHelper.SaveEncrypted` |
| `auto_saved.log` | 日志达到 `MaxLogPcs` 后的自动落盘文件 | `Logs.AutoSavePath` |
| `smoke_result.txt` | `/smoke` 自检的 UTF-8 结果报告 | `SmokeTest` |
| `downloaded.bin` | 下载器默认保存位置 | `HttpLink.DownTool` |

配置文件路径 = `Tool.GetProgramPath() + "DemoData\demo.config"`，加密密钥在 `Program.ConfigSaveKey`。

## 四、初始化流程（`Program.cs`）

这个顺序是本库的推荐用法，也是 Demo 最重要的示范之一：

```csharp
static void Main(string[] args)
{
    // ① 先抓真实控制台流（RedirectConsoleOut 之后就抓不到了）
    TextWriter realConsole = Console.Out;

    // ② 接管 Console：此后库内部所有 Console.WriteLine 都进日志
    Log.RedirectConsoleOut();
    Log.MaxLogPcs = 5000;
    Log.AutoSavePath = Path.Combine(WorkDir, "auto_saved.log");   // 默认值已是程序目录下的完整文件路径

    // ③ 先 Add 出全部配置项，再 Load 覆盖值
    Cfg.Add("App.TopMost", Configs.ValueType.Bool, false, "窗口置顶", "主窗口是否总在最前");
    // …共 8 项
    if (!Cfg.Load(ConfigPath, ConfigSaveKey))
        Cfg.Save(ConfigPath, ConfigSaveKey);      // 明文/加密都按显式路径写出

    // ④ 订阅变更事件，统一响应
    Cfg.ConfigChanged += c => Log.Println("[配置变更] 数据已更新。");

    Application.Run(new MainForm());
}
```

## 五、已被本 Demo 验证修复的库缺陷

开发这个 Demo 的过程暴露了不少库缺陷，**现在都已在库里修掉**，并由 `/smoke` 断言守住（详见使用文档「附录 B · 陷阱清单（含修复状态）」）。
这些断言的语义是「**验证修复生效**」，而不是记录缺陷：

| # | 原缺陷 | 修复后 Demo/断言中的体现 |
|---|---|---|
| 1 | 单参数 `Save(path)` 被解析成 `Save(SaveKey: path)`，首次保存静默不写文件 | `Save()` / `Save(path)` / `Save(path, key)` 三个重载彻底分离并返回 `bool`；断言《Save(path) 直接按路径明文写入》 |
| 2 | `Save(key)` 在未载入状态下静默不保存 | 返回 `false` 并打印原因；断言《Save() 未指定路径时返回 false 且不抛异常》 |
| 3 | `WriteStrValue` 解析失败污染 `Value` | 失败保持原值；断言《WriteStrValue 解析失败不再污染 Value》 |
| 4 | `Copy(null)` 静默无效、不复制 `HelpUrl` | 抛 `ArgumentNullException`；断言《Copy(null) 抛 ArgumentNullException》《Copy / CopyAll 复制成功并返回结果》 |
| 5 | `InputData` 不触发 `ConfigChanged`、无返回值 | 返回 `bool` 并触发事件；断言《InputData 导入会触发 ConfigChanged 并返回结果》 |
| 6 | 日志窗口因 `NumericUpDown` 上限 100，`MaxLogPcs > 100` 时打不开 | 窗口上限放宽为 `int.MaxValue`；断言《日志窗口能接受大于 100 的 MaxLogPcs》 |
| 7 | `Logs.GetLastLog()` 空列表抛异常 | 返回 `null` / `string.Empty`；断言《GetLastLog / GetLastLogText 空列表不再抛异常》 |
| 8 | `RedirectConsoleOut` 不可逆 + 事件里写 Console 会栈溢出 | 返回原输出流 + `RestoreConsoleOut` + 双重入保护；两条断言 |
| 9 | `AutoSavePath` 默认是目录、落盘失败还清空日志 | 默认改为完整文件路径、自动建目录、失败保留日志；三条断言 |
| 10 | `DataFlowList` 只支持 `int`/`float` | 改为 `Convert` 转换，四种数值类型混用；两条断言 |
| 11 | `LinkConversion` 分支重复、缺 `./` 反向 | 重写为双向转换；断言《Tool.LinkConversion 双向转换》 |
| 12 | `AbsNewFolder` 默认按工作目录解析 | 默认改为程序目录；断言《Tool.AbsNewFolder 默认建在程序目录》 |
| 13 | `RunCmdCode` 不重定向 stderr | 合并 stderr；断言《Tool.RunCmdCode 会捕获 stderr 输出》 |
| 14 | `SafeInvoke` 对 null/已释放控件会崩 | 安全返回 + 新增异步 `SafeBeginInvoke`；断言 |
| 15 | `ControlMove` 定时器泄漏、控件释放后仍访问 | 动画结束/控件释放都释放定时器；断言《Tool.ControlMove 动画会走到目标位置》 |
| 16 | `SetControlFillet` 误释放 Win32 区域句柄 | 只在失败时释放；断言《Tool.SetControlFillet 空句柄/已释放控件不再抛异常》 |
| 17 | `ProgTool.GetBuildTime` 在确定性构建下返回 1961 年 | 三级回退；断言《ProgTool.GetBuildTime 合理》 |
| 18 | `States` 计数字段恒为 0 | 自动重算 + `RecalculateCounts()`；断言《States 计数字段自动维护》 |
| 19 | `WebServer` 路径穿越、`StopServer` 不等待、受限环境构造即崩 | 403 防护 + `Join` + Listener 延迟创建；断言《WebServer 路径穿越防护》等 |
| 20 | `DingDingAPI` 每次 new `HttpClient` | 复用静态实例 |
| 21 | `SakuraFrpcAPI` 全局降级 TLS 且不恢复 | 临时放宽并在 `finally` 还原 |
| 22 | `Comp.Chart` 空数据索引越界、`Comp.ProgressBar` 越界溢出 | 均加了防护；控件页可直接点出来 |

**仍然存在、属于设计取舍或环境限制**（Demo 中如实标注，未做修改）：

| # | 项目 | 说明 |
|---|---|---|
| A | `DownTool` 需要服务端支持 `HEAD` 与 `Range` | 网络页提供「仅探测文件大小」与三种模式，默认推荐单线程 |
| B | `DownTool` / `UploadTool` 回调在**后台线程** | 网络页统一 `Tool.SafeInvoke` 回 UI 线程 |
| C | `PostTask` 失败与「成功但空响应」都返回空串 | 输出区明确提示需要区分时自写 `HttpClient` |
| D | `GetImageFromUrl` 返回的 `Image` 绑定已释放的流 | 下载后立即 `new Bitmap(img)` 复制 |
| E | `WebServer` LANS 模式需要 URL ACL / 管理员权限 | 启动失败时弹出 `netsh http add urlacl` 命令 |
| F | `Switch.IsOpenChange` 只在点击时触发 | 控件页「切换 IsOpen（不触发事件）」按钮 + `SetIsOpen(x, raiseEvent)` |
| G | `Comp.Panels` 圆角是 Win32 硬裁剪 | 控件页与普通 Panel 对照展示 |
| H | `GetRandomString` 的 `AddedChar` 是**追加**字符 | 工具页可填附加字符集 + 自检断言 |
| I | `Read` 返回 `object`（整型是 `long`） | 自检断言《Read 返回声明类型（Long → long）》 |

## 六、自检（`/smoke`）说明

* 共 **87 项断言**，覆盖：程序工具、数据流、配置、日志、加密解密、网络、States、主题、以及上表的全部修复项。
* 结果三种状态：
  * `[PASS]` —— 通过，计入退出码。
  * `[FAIL]` —— 失败，退出码变 1，并打印异常类型、消息与**堆栈**。
  * `[SKIP]` —— **依赖运行环境**的检查，不计入失败也不计入通过。目前只有 2 项：需要 `HttpListener` 的两条
    （本机沙箱里 `new HttpListener()` 直接抛 `PlatformNotSupportedException`，常规 Windows 上会 PASS）。
* 断言全部是「验证行为正确」，没有「记录缺陷」性质的断言。失败即表示回归。

## 七、文件清单

| 文件 | 作用 |
|---|---|
| `Demo.sln` / `Demo.csproj` | 解决方案与项目（`ProjectReference` 引用 `..\WeiKit.csproj`） |
| `Program.cs` | 入口、全局 `Logs`/`Configs`、工作目录、初始化顺序、`/smoke` 分发 |
| `MainForm.cs` | 主窗体、菜单、状态栏、7 个标签页组装 |
| `Ui.cs` | Demo 自己的界面小工具（可滚动页、分组框、表格、控件工厂） |
| `ConfigDemo.cs` | ① 配置管理页 |
| `LogCryptoDemo.cs` | ② 日志 + 加密解密页 |
| `ToolDemo.cs` | ③ 程序工具页 |
| `NetDemo.cs` | ④ 网络页 |
| `CompDemo.cs` | ⑤ 自绘控件页 |
| `SmokeTest.cs` | 无界面自检（87 项断言） |

> 注意：每个 Demo 页都是「普通类 + `Build()` 返回 `Control`」，不是 `UserControl` 子类，
> 因此代码中不能直接访问 `Cursor`、`IsDisposed` 等 `Control` 成员，
> 需要借助 `Build()` 里保存的 `hostPanel` 字段（`NetDemo`、`ToolDemo` 里各有一处示范）。
