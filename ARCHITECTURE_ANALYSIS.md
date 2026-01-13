# NeeView 项目架构与编程规范分析

## 项目概述

**NeeView** 是一个功能强大的图像查看器应用程序，基于 WPF (Windows Presentation Foundation) 开发，支持查看文件夹和压缩包中的图像，具有丰富的自定义功能。

### 技术栈
- **框架**: .NET 10.0 (net10.0-windows)
- **UI框架**: WPF (Windows Presentation Foundation)
- **开发环境**: Visual Studio 2026
- **语言**: C# (启用 Nullable 引用类型)
- **平台**: Windows 10 64位及以上

---

## 项目结构

### 1. 解决方案组成

项目采用多项目解决方案结构，主要包含以下项目：

```
NeeView.sln (主解决方案)
├── NeeView (主应用程序)
├── NeeLaboratory.Runtime (运行时库)
├── NeeLaboratory.IO.Search (IO搜索库)
├── NeeLaboratory.Remote (远程通信库)
├── NeeLaboratory.SourceGenerator (源代码生成器)
├── NeeView.Interop (互操作库)
├── NeeView.Susie (Susie插件支持)
├── NeeView.Susie.Server (Susie服务器)
├── NeeView.UnitTest (单元测试)
├── AnimatedImage (动画图像库)
├── SevenZipSharp (7z压缩支持)
└── Vlc.DotNet (视频播放支持)
```

### 2. 主项目 (NeeView) 目录结构

主项目采用**功能模块化**的目录组织方式，每个功能模块独立成文件夹：

#### 核心模块

| 模块名称 | 功能描述 |
|---------|---------|
| **Book** | 书籍（图像集合）核心逻辑，包含页面管理、书签、历史记录等 |
| **Page** | 页面抽象和管理，单个图像页面的表示 |
| **Command** | 命令系统，包含所有应用命令的定义和执行 |
| **Config** | 配置管理，应用的所有设置项 |
| **MainWindow** | 主窗口相关逻辑 |
| **MainView** | 主视图区域 |

#### UI组件模块

| 模块名称 | 功能描述 |
|---------|---------|
| **Controls** | 自定义控件 |
| **SidePanels** | 侧边栏面板（书架、页面列表、信息等） |
| **AddressBar** | 地址栏 |
| **BreadcrumbBar** | 面包屑导航栏 |
| **MenuBar** | 菜单栏 |
| **SearchBox** | 搜索框 |
| **Toast** | 通知提示 |

#### 功能模块

| 模块名称 | 功能描述 |
|---------|---------|
| **Archiver** | 压缩文件处理（ZIP, RAR, 7Z等） |
| **Bitmap** | 位图处理 |
| **Picture** | 图片加载和处理 |
| **PictureSource** | 图片源管理 |
| **Thumbnail** | 缩略图生成和缓存 |
| **ViewContents** | 视图内容渲染 |
| **ViewSources** | 视图数据源 |
| **PageFrames** | 页面框架布局 |
| **SlideShow** | 幻灯片放映 |
| **ExportImage** | 图像导出 |
| **Print** | 打印功能 |

#### 输入处理模块

| 模块名称 | 功能描述 |
|---------|---------|
| **MouseInput** | 鼠标输入处理 |
| **TouchInput** | 触摸输入处理 |
| **InputGesture** | 输入手势管理 |

#### 系统模块

| 模块名称 | 功能描述 |
|---------|---------|
| **System** | 系统级功能 |
| **External** | 外部程序集成 |
| **Script** | 脚本引擎（JavaScript支持） |
| **SaveData** | 数据持久化 |
| **JobEngine** | 任务调度引擎 |

#### 辅助模块

| 模块名称 | 功能描述 |
|---------|---------|
| **NeeView** | 核心工具类和扩展 |
| **NeeLaboratory** | 通用工具库 |
| **Converters** | WPF值转换器 |
| **Styles** | XAML样式资源 |
| **Resources** | 资源文件 |
| **Languages** | 多语言支持 |

---

## 核心架构模式

### 1. MVVM 模式

项目严格遵循 **MVVM (Model-View-ViewModel)** 架构模式：

- **Model**: 数据模型和业务逻辑（如 `Book`, `Page`, `BookSource`）
- **ViewModel**: 视图模型，继承自 `BindableBase` 或 `WeakBindableBase`
- **View**: XAML 视图文件

**示例**：
```csharp
// BindableBase 提供 INotifyPropertyChanged 实现
public class Config : BindableBase
{
    private static Config? _current;
    public static Config Current { get; }
    
    public SystemConfig System { get; set; } = new SystemConfig();
    public ViewConfig View { get; set; } = new ViewConfig();
    // ... 更多配置项
}
```

### 2. 单例模式 (Singleton)

大量使用单例模式管理全局状态和服务：

```csharp
public class CommandTable
{
    static CommandTable() => Current = new CommandTable();
    public static CommandTable Current { get; }
    
    private CommandTable()
    {
        InitializeCommandTable();
    }
}
```

**常见单例类**：
- `Config.Current` - 全局配置
- `CommandTable.Current` - 命令表
- `SaveData.Current` - 数据保存服务
- `ThemeManager.Current` - 主题管理器
- `ThumbnailCache.Current` - 缩略图缓存

### 3. 命令模式 (Command Pattern)

应用使用统一的命令系统：

- **CommandTable**: 命令注册表，管理所有命令
- **CommandElement**: 命令元素，封装命令逻辑
- **RoutedCommandTable**: WPF路由命令集成
- **输入绑定**: 支持键盘快捷键、鼠标手势、触摸手势

### 4. 策略模式 (Strategy Pattern)

用于处理不同类型的内容：

- **IViewSourceStrategy**: 视图源策略接口
  - `ImageViewSourceStrategy` - 图像
  - `MediaViewSourceStrategy` - 视频
  - `PdfViewSourceStrategy` - PDF
  - `SvgViewSourceStrategy` - SVG
  - `ArchiveViewSourceStrategy` - 压缩包

- **IViewContentStrategy**: 视图内容策略接口
  - `ImageViewContentStrategy`
  - `MediaViewContentStrategy`
  - `FileViewContentStrategy`

### 5. 工厂模式 (Factory Pattern)

```csharp
// 书籍工厂
public class BookFactory
{
    public IBook Create(BookAddress address, BookSource source, ...);
}

// 视图内容工厂
public class ViewContentFactory
{
    public ViewContent Create(ViewSource viewSource);
}
```

### 6. 观察者模式 (Observer Pattern)

通过事件和 `INotifyPropertyChanged` 实现：

```csharp
public class Book : IDisposable
{
    public event EventHandler<CurrentPageChangedEventArgs>? CurrentPageChanged;
    public event EventHandler<PageTerminatedEventArgs>? PageTerminated;
}
```

---

## 编程规范与习惯

### 1. 命名规范

#### 字段命名
- **私有字段**: 使用下划线前缀 + camelCase
  ```csharp
  private bool _isSplashScreenVisible;
  private CommandLineOption? _option;
  private MultiBootService? _multiBootService;
  ```

- **静态字段**: 同样使用下划线前缀
  ```csharp
  private static Config? _current;
  ```

#### 属性命名
- **公共属性**: PascalCase
  ```csharp
  public CommandLineOption Option { get; }
  public DateTime StartTime { get; private set; }
  ```

#### 方法命名
- **公共方法**: PascalCase
- **私有方法**: PascalCase
  ```csharp
  private void InitializeTextResource(string language)
  private CommandLineOption ParseCommandLineOption(string[] args)
  ```

#### 事件处理器命名
- 格式: `对象名_事件名`
  ```csharp
  private void Application_Startup(object sender, StartupEventArgs e)
  private void Application_Exit(object sender, ExitEventArgs e)
  private void Setting_PropertyChanged(object sender, PropertyChangedEventArgs e)
  ```

### 2. 代码组织

#### 区域划分 (Regions)
使用 `#region` 组织代码：

```csharp
#region Obsolete
[Obsolete("no used"), Alternative(nameof(Playlist), 39)]
public string? Pagemark { get; set; }
#endregion

#region Validate
// 验证相关代码
#endregion

#region Develop
// 开发辅助代码
#endregion
```

#### 文件组织
- 一个类一个文件
- 部分类 (Partial Class) 用于分离关注点：
  - `App.xaml.cs` - 主逻辑
  - `App.Exception.cs` - 异常处理
  - `App.Option.cs` - 选项处理

### 3. Nullable 引用类型

项目启用了 Nullable 引用类型 (`<Nullable>enable</Nullable>`)：

```csharp
// 可空类型明确标注
private static Config? _current;
private CommandLineOption? _option;

// 非空断言
public static Config Current => _current ??= new Config();
public CommandLineOption Option => _option ?? throw new InvalidOperationException("_option must not be null");
```

### 4. 异步编程

使用现代异步模式：

```csharp
private async void Application_Startup(object sender, StartupEventArgs e)
{
    await InitializeAsync(e);
}

private async ValueTask InitializeAsync(StartupEventArgs e)
{
    // 异步初始化逻辑
}
```

### 5. 资源管理

#### IDisposable 模式
```csharp
public void Dispose()
{
    Dispose(true);
    GC.SuppressFinalize(this);
}

protected virtual void Dispose(bool disposing)
{
    if (disposing)
    {
        // 释放托管资源
    }
}
```

#### Using 语句
```csharp
using var span = DebugSpan();
// 自动释放资源
```

### 6. 调试和诊断

#### 条件编译
```csharp
#if DEBUG
return new DebugSpanScope(Stopwatch, "App." + callerMethodName);
#else
return null;
#endif
```

#### 诊断属性
```csharp
[Conditional("DEBUG")]
private void DebugStamp(string label)
{
    DebugSpanScope.Dump(Stopwatch, "App." + label);
}
```

#### 调用者信息
```csharp
private IDisposable? DebugSpan([CallerMemberName] string callerMethodName = "")
```

### 7. 配置和序列化

#### JSON 序列化控制
```csharp
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public PrintModel.Memento? Print { get; set; }

[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
public WindowTitleConfig? WindowTittle { get; set; }
```

#### 属性映射
```csharp
[PropertyMapLabel("Word.Bookmark")]
public BookmarkConfig Bookmark { get; set; }

[PropertyMapIgnore]
public PrintModel.Memento? Print { get; set; }
```

#### 版本兼容性
```csharp
[Obsolete("no used"), Alternative(nameof(Playlist), 39)] // ver.39
public string? Pagemark { get; set; }

[Obsolete("Typo"), Alternative(nameof(WindowTitle), 39)]
public WindowTitleConfig? WindowTittle { get; set; }
```

### 8. 注释规范

#### XML 文档注释
```csharp
/// <summary>
/// コマンドライン引数処理
/// </summary>
/// <returns></returns>
private CommandLineOption ParseCommandLineOption(string[] args)

/// <summary>
/// アプリの起動時間(ms)取得
/// </summary>
public int TickCount => System.Environment.TickCount - _tickBase;
```

#### 内联注释
```csharp
// DLL 検索パスから現在の作業ディレクトリ (CWD) を削除
NativeMethods.SetDllDirectory("");

// 起動処理排他ロック取得。時間内 (30sec) に獲得できなければアプリ終了
var bootLock = new BootProcessLock(30 * 1000);
```

### 9. 错误处理

#### 异常处理模式
```csharp
try
{
    var setting = settingResource.Load() ?? new UserSetting();
    return setting.EnsureConfig();
}
catch (Exception ex)
{
    var dialog = new UserSettingLoadFailedDialog(true);
    var result = dialog.ShowDialog(ex);
    if (result != true)
    {
        throw new OperationCanceledException();
    }
    return new UserSetting().EnsureConfig();
}
```

#### 断言使用
```csharp
Debug.Assert(_current is null, "Already set.");
Debug.Assert(!position.IsEmpty());
Debug.Assert(direction is -1 or +1);
```

### 10. 依赖注入和服务定位

虽然没有使用专门的DI容器，但通过单例模式实现服务定位：

```csharp
// 服务访问
Config.Current.System.Language
CommandTable.Current.TryExecute(commandName)
SaveData.Current.DisableSave()
```

---

## 配置系统架构

### 配置层次结构

```
Config (根配置)
├── SystemConfig (系统配置)
├── ViewConfig (视图配置)
├── BookConfig (书籍配置)
├── BookSettingConfig (书籍设置)
├── ImageConfig (图像配置)
├── ArchiveConfig (压缩包配置)
├── HistoryConfig (历史记录配置)
├── WindowConfig (窗口配置)
├── ThemeConfig (主题配置)
├── MouseConfig (鼠标配置)
├── TouchConfig (触摸配置)
├── CommandConfig (命令配置)
├── ScriptConfig (脚本配置)
└── ... (更多配置模块)
```

### 配置特点

1. **模块化**: 每个功能模块有独立的配置类
2. **层次化**: 配置采用树形结构组织
3. **可序列化**: 支持 JSON 序列化/反序列化
4. **版本兼容**: 使用 `Obsolete` 和 `Alternative` 属性处理版本迁移
5. **数据绑定**: 继承 `BindableBase` 支持 WPF 数据绑定

---

## 数据持久化

### 保存机制

1. **UserSetting**: 用户设置文件
   - 位置: `%LocalAppData%\NeeView\`
   - 格式: JSON
   - 包含: Config, History, Bookmark 等

2. **SaveData**: 数据保存服务
   - 自动保存
   - 文件监视
   - 版本控制

3. **ThumbnailCache**: 缩略图缓存
   - 使用 SQLite 数据库
   - 自动清理过期数据

---

## 多语言支持

### 实现方式

1. **资源文件**: `.restext` 格式
   - `shared.restext` - 共享资源
   - `ja.restext` - 日语
   - `en.restext` - 英语
   - `zh-Hans.restext` - 简体中文
   - `zh-Hant.restext` - 繁体中文
   - 等等...

2. **TextResources**: 文本资源管理器
   ```csharp
   TextResources.Initialize(culture);
   InputGestureDisplayString.Initialize(TextResources.Resource);
   ```

3. **动态切换**: 运行时可切换语言

---

## 扩展性设计

### 1. 脚本系统

- **引擎**: Jint (JavaScript 解释器)
- **脚本位置**: `Resources/Scripts/`
- **功能**: 
  - 自定义命令
  - 事件钩子 (如 `OnBookLoaded`)
  - 配置访问
  - UI 交互

### 2. 插件系统

- **Susie 插件**: 支持经典的 Susie 图像插件
- **扩展点**: 通过接口定义扩展点
  - `IViewSourceStrategy` - 自定义视图源
  - `IViewContentStrategy` - 自定义视图内容

### 3. 主题系统

- **主题文件**: JSON 格式
- **内置主题**:
  - DarkTheme
  - LightTheme
  - DarkMonochromeTheme
  - LightMonochromeTheme
  - HighContrastTheme
- **自定义主题**: CustomThemeTemplate.json

---

## 性能优化策略

### 1. 异步加载

- 图像异步加载
- 缩略图异步生成
- 压缩包异步解压

### 2. 缓存机制

- **ThumbnailCache**: 缩略图缓存
- **BookThumbnailPool**: 书籍缩略图池
- **PageThumbnailPool**: 页面缩略图池

### 3. 虚拟化

- **VirtualizingWrapPanel**: 虚拟化面板（NuGet包）
- 大量图像时只渲染可见部分

### 4. 内存管理

- 弱引用 (`WeakBindableBase`)
- 及时释放资源 (`IDisposable`)
- 临时文件管理 (`Temporary.Current`)

---

## 测试策略

### 单元测试

- 项目: `NeeView.UnitTest`
- 框架: 标准 .NET 测试框架

### 互操作测试

- 项目: `NeeView.Interop.DotNetTest`
- 测试本地互操作功能

---

## 构建和部署

### 配置类型

1. **Debug**: 调试版本
2. **Release**: 发布版本
3. **Remote**: 远程调试版本（输出到特定目录）

### 平台支持

- AnyCPU
- x64
- x86

### 依赖管理

#### NuGet 包
- `bblanchon.PDFium.Win32` - PDF 渲染
- `Jint` - JavaScript 引擎
- `MetadataExtractor` - 元数据提取
- `Microsoft.Xaml.Behaviors.Wpf` - XAML 行为
- `PhotoSauce.MagicScaler` - 图像缩放
- `SharpVectors` - SVG 支持
- `System.Data.SQLite.Core` - SQLite 数据库
- `VirtualizingWrapPanel` - 虚拟化面板

#### 本地库
- `libwebp.dll` - WebP 图像支持
- `MediaInfo.dll` - 媒体信息
- `NeeView.Interop.dll` - 本地互操作

---

## 版本控制策略

### 版本号格式

```
MAJOR.MINOR.BUILD
```

- **MAJOR**: 功能添加或变更时更新
- **MINOR**: 仅 bug 修复时更新
- **BUILD**: 自动分配（提交次数）

### 分支策略

1. **master**: 主开发分支，接受 Pull Request
2. **version-XX**: 各主要版本维护分支
3. **feature-XX**: 功能开发分支（临时）

---

## 关键设计决策

### 1. 为什么使用 WPF？

- 强大的数据绑定
- XAML 声明式 UI
- 丰富的控件和样式系统
- 硬件加速渲染

### 2. 为什么使用单例模式？

- 全局状态管理
- 简化依赖访问
- 避免重复初始化

### 3. 为什么启用 Nullable 引用类型？

- 编译时空引用检查
- 提高代码安全性
- 明确意图

### 4. 为什么使用策略模式处理不同内容类型？

- 易于扩展新类型
- 解耦内容处理逻辑
- 符合开闭原则

---

## 代码质量工具

### EditorConfig

项目包含 `.editorconfig` 文件，定义编码规范：

```ini
[*.cs]
# 不使用简单 using 语句
csharp_prefer_simple_using_statement = false

# 不使用对象初始化器简化
dotnet_style_object_initializer = false

# 不使用复合赋值
dotnet_style_prefer_compound_assignment = false

# 私有字段命名规则：下划线前缀 + camelCase
dotnet_naming_rule.private_or_internal_field_should_be_begin_with__.severity = suggestion
```

---

## 学习路径建议

### 1. 入门阶段

1. 理解 WPF 基础和 MVVM 模式
2. 熟悉项目目录结构
3. 阅读 `App.xaml.cs` 了解应用启动流程
4. 研究 `Config.cs` 了解配置系统

### 2. 核心功能

1. **Book 模块**: 理解书籍和页面抽象
2. **Command 模块**: 学习命令系统
3. **ViewSources/ViewContents**: 理解内容渲染流程
4. **PageFrames**: 学习页面布局算法

### 3. 高级特性

1. **Script 模块**: JavaScript 脚本集成
2. **Archiver 模块**: 压缩包处理
3. **Thumbnail 模块**: 缩略图生成和缓存
4. **MouseInput/TouchInput**: 输入处理

### 4. 扩展开发

1. 实现自定义 `IViewSourceStrategy`
2. 编写脚本扩展
3. 创建自定义主题
4. 开发 Susie 插件

---

## 常见模式和最佳实践

### 1. 属性变更通知

```csharp
public class MyViewModel : BindableBase
{
    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
```

### 2. 延迟初始化

```csharp
private static Lazy<MyService> _instance = new(() => new MyService());
public static MyService Instance => _instance.Value;
```

### 3. 事件订阅管理

```csharp
private void AttachEvents()
{
    _source.PropertyChanged += Source_PropertyChanged;
}

private void DetachEvents()
{
    _source.PropertyChanged -= Source_PropertyChanged;
}

public void Dispose()
{
    DetachEvents();
}
```

### 4. 配置访问

```csharp
// 读取配置
var language = Config.Current.System.Language;

// 修改配置（自动触发保存）
Config.Current.View.PageMode = PageMode.SinglePage;
```

---

## 总结

NeeView 是一个架构清晰、模块化良好的 WPF 应用程序，具有以下特点：

### 优点

1. ✅ **模块化设计**: 功能模块独立，职责清晰
2. ✅ **可扩展性**: 通过接口和策略模式支持扩展
3. ✅ **代码规范**: 统一的命名和组织规范
4. ✅ **类型安全**: 启用 Nullable 引用类型
5. ✅ **性能优化**: 异步加载、缓存、虚拟化
6. ✅ **多语言支持**: 完善的国际化机制
7. ✅ **脚本支持**: JavaScript 脚本扩展能力

### 学习价值

- WPF 应用架构设计
- MVVM 模式实践
- 大型项目组织
- 性能优化技巧
- 插件系统设计

---

## 参考资源

- [NeeView 官方网站](https://neelabo.github.io/NeeView)
- [GitHub 仓库](https://github.com/neelabo/NeeView)
- [开发语言文件指南](NeeView/Languages)
- [示例脚本](SampleScripts)

---

**文档版本**: 1.0  
**生成日期**: 2026-01-13  
**基于版本**: NeeView 主分支 (master)
