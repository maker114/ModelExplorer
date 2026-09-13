# Model Explorer

> 面向 3D 打印工作流的 SolidWorks 模型工作台：扫描工程目录、一键导出 STL、
> 直接在 Bambu Studio 里出 3MF，并顺手把散落的 STL / 3MF 与工程名整理干净。

**当前版本：V3.5.2** ｜ Windows 10/11 ｜ .NET Framework 4.8 ｜ WPF

---

## 它解决什么问题

从 SolidWorks 到 Bambu Studio 的这段路，手工做起来是重复劳动：

1. 在 SolidWorks 里逐个打开零件 / 装配体，导出 STL 到指定文件夹；
2. 打开 Bambu Studio，导入 STL，选打印机与耗材，另存为 3MF；
3. 把导出的 STL 从工程目录里挑出来，归到 `STL文件夹` / `3MF文件夹`；
4. 让文件名带上工程名前缀，方便以后在一堆 STL 里认人。

Model Explorer 把这几步串成一条流水线：选一次工程目录，剩下的点按钮就行。

---

## 功能一览

### 转换流水线

- 递归扫描工程目录下的 `.sldprt` / `.sldasm`，以深色卡片列表展示零件、装配体、STL、3MF；
- 选中模型 → 调用 SolidWorks API 导出 STL（可选二进制 / ASCII、单位 mm·cm·m·in、四档导出质量）；
- 自动打开 Bambu Studio 加载 STL，选好打印机与耗材后保存 3MF；
- 「保留历史版本」「导出后自动打开 Bambu Studio」可配置。

### 文件整理

- **整理 STL / 3MF**：把散落在分类文件夹之外的 STL / 3MF 归入 `STL文件夹` / `3MF文件夹`；
  支持「按文件夹整理」（每个来源目录各自建分类文件夹）与「按根目录整理」两种模式；
- **撤销整理**：内置移动历史，可一键撤回上一次整理；
- 同名文件自动避让，不会互相覆盖。

### 工程名整理

- **检查 / 补充工程名**：按规则算出目标文件名（`工程名_子工程名_主体`），先给出**预览清单**
  再执行，逐条可勾选；
- 装配体导出 STL（文件名含 ` - `）会被识别并补上 `[装配体导出]` 标记；
- 支持撤销工程名修改。

### 界面

- 12 套深色配色预设（中文传统色名：丹砂、柿子橙、金珀……），统一低饱和，配色可实时预览；
- 毛玻璃界面：半透明玻璃面板 + **极光渐变背景**，模糊与透明度各一个滑杆，可随时关掉；
- 自定义背景图：覆盖 / 填充 / 居中 / 拉伸四种适配，可调暗化，实时预览；
- 自绘圆角无边框窗口、圆角按钮、拨钮开关、圆角滚动条；
- 主界面与设置窗的分区标题统一使用一套线性小图标。

### 可读性约束（不是随手调的）

界面在**任何背景**下都要保证正文可读，这一点写进了渲染管线：

- 背景层烘焙时按亮度直方图自动压暗，保证面板上的小字与直接压在背景上的说明文字
  都维持 **4.5:1** 以上对比度（实测最坏情况约 5:1）；
- 类型标签色按每套配色自己的强调色相推导，同色系三档明度，12 套互不重复且都读得清；
- 这些约束都有自动化测试或离线渲染核对，改动不会悄悄退化。

---

## 快速开始

### 直接用（便携版）

1. 运行 `publish.ps1` 生成 `dist\ModelExplorer-<版本>-portable\`；
2. 双击该目录下的 `ModelExplorer.exe`（同级还有 `启动ModelExplorer.bat` 与 `使用说明.txt`）；
3. 「选择工程目录」→ 选到你的 SolidWorks 工程文件夹 → 开始扫描。

需要已安装 **SolidWorks 2022**（导出 STL 走它的 API）与 **Bambu Studio**（出 3MF）。
只做文件整理 / 工程名整理的话，不装也能用。

### 从源码构建

```powershell
# 构建 5 个工程 + 跑测试
powershell -ExecutionPolicy Bypass -File .\build.ps1

# 发布便携版（目录名按 AssemblyInfo 里的版本号生成）
powershell -ExecutionPolicy Bypass -File .\publish.ps1
```

要求：**.NET SDK 6.0+**（用于构建）、Windows 自带 **.NET Framework 4.8**（用于运行）。
也可以直接用 Visual Studio 2022 打开 `ModelExplorer.sln`，`F5` 调试主程序，
`Alt+F10` 用 XAML Hot Reload 实时改界面。

> 发布前请先关掉正在运行的 ModelExplorer：`publish.ps1` 要覆盖同名版本目录，
> 程序在跑时目录里的 exe / dll 被占用，脚本会明确提示而不是抛出原始错误。

**关于目录名**：开发机上这个仓库是工作区里的 `outputs\` 子目录（外层还有启动器与开发
辅助脚本，未纳入本仓库）。仓库内的路径都是**相对自身**的，克隆到任意目录名都能正常构建；
只是启动器那类外层脚本会去找同级的 `outputs\dist\`，若你也想用，把克隆目录命名为 `outputs` 即可。

---

## 仓库结构

| 路径 | 说明 |
| --- | --- |
| `ModelExplorer.Core/` | 共享业务库：扫描、工程名规则、整理、配置、STL 导出、Bambu 启动（无 UI 依赖，可单测） |
| `ModelExplorer/` | WPF 主程序（界面、主题、毛玻璃、对话框） |
| `ModelExplorerCli/` | 命令行转换工具 |
| `ModelExplorerAddin/` | SolidWorks 2022 插件（在 SolidWorks 里直接「导出到 Bambu」） |
| `ModelExplorer.Tests/` | 测试套件（**146 项**，无外部依赖，离线可跑） |
| `lib/SolidWorks/` | SolidWorks Interop 程序集的唯一来源（换 SolidWorks 版本只替换这里） |
| `build.ps1` / `publish.ps1` / `prune-dist.ps1` | 构建、打包、清理旧版本 |
| `Directory.Build.props` | 全仓库统一的构建配置（目标框架、平台、Interop 路径） |

各工程的详细说明、功能规则（统计口径、命名规则、标签判定、毛玻璃实现）与**逐版本更新报告**
见 [`ModelExplorer/README.md`](ModelExplorer/README.md)；
分类整理过的变更清单见 [`CHANGELOG.md`](CHANGELOG.md)。

---

## 测试

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
# 或直接跑
ModelExplorer.Tests\bin\Release\ModelExplorer.Tests.exe
```

覆盖范围：装配体导出判定的两套语义、分类文件夹命名、工程名整理规则（本项目最复杂的一段
业务规则）、扫描与 `[未整理] / [未对应]` 判定、整理与同名避让、详细统计汇总、配置默认值与
迁移、主界面图标与常量的一致性。全部用例都在临时目录里跑，不动用户配置、不调用 SolidWorks。

---

## 配置

统一保存在 `%APPDATA%\ModelExplorer\config.json`，主程序 / CLI / 插件共用一份。
升级不会丢配置：旧版本的三档毛玻璃强度、壁纸模糊等字段都会自动迁移到新字段。

---

## 说明与已知取舍

- **只支持 Windows**：SolidWorks API、WPF、分层窗口都绑死在 Windows 上；
- SolidWorks 的调用走官方 Interop，**需要本机装 SolidWorks**；没装时只有导出功能不可用；
- 编译输出目录必须同时包含 SolidWorks Interop DLL（`exe` 启动时要加载），
  这一点由 `Directory.Build.props` 统一处理，不要手工删；
- 毛玻璃是**应用内**实现（预烘焙背景位图 + 半透明面板），不是系统级模糊；
  拖动窗口尺寸时会先拉伸旧位图、停手后再重新烘焙；
- 配色预设取自 [520设计网「色彩搭配」](https://www.sj520.cn/tools/peise/) 的公开方案，
  按其色相在色谱上均匀取了 12 组，再压暗成深色界面的底 / 面板 / 描边阶梯。

---

## 许可

本仓库未附带开源许可证文件；如需转载或二次分发，请先与作者联系。
仓库内 `lib/SolidWorks/` 下的 Interop 程序集版权归 **Dassault Systèmes SolidWorks** 所有，
仅作编译引用之用。
