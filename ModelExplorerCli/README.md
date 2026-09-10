# Model Explorer CLI

不安装 SolidWorks 插件、不改 SolidWorks 注册表，用命令行把 SolidWorks 零件/装配体导出为 STL，交给 Bambu Studio 打开，并等你在 Bambu Studio 中选好打印机和耗材后，把当前项目保存为同名 `.3mf` 工程。

## 用法

```powershell
.\bin\Release\ModelExplorerCli.exe "D:\模型\part.sldprt"
```

参数：

- `--keep-history`：同名 STL 存在时生成带时间戳的新文件
- `--no-bambu`：只导出 STL，不启动 Bambu Studio
- `--output "D:\out\part.stl"`：指定 STL 输出路径
- `--no-3mf`：不生成 3MF 工程
- `--3mf-output "D:\out\part.3mf"`：指定 3MF 输出路径
- `--auto-3mf`：使用旧的后台导出方式直接生成 3MF（不会包含当前界面选择的打印机和耗材信息）
- `--save-only "D:\out\part.3mf"`：只触发一次保存 3MF，不做导出

示例：

```powershell
.\bin\Release\ModelExplorerCli.exe "D:\模型\part.sldprt" --keep-history --no-bambu
```

退出码：`0` 成功、`1` 异常、`2` 参数或模型路径无效、`3` 打开模型失败、`4` 导出 STL 失败。

## 原理

工具会启动一个隐藏的 SolidWorks 实例，用 SolidWorks API 打开模型、静默导出 STL，然后关闭模型并退出。它不会像 Add-in 那样写入 SolidWorks 加载项注册表，因此不会影响 SolidWorks 启动。

默认流程：

1. 在模型目录下创建 `STL文件夹`（与主程序使用同一分类文件夹）
2. 导出 STL 到 `模型目录\STL文件夹\`
3. 打开 Bambu Studio 并加载该 STL
4. 在 Bambu Studio 中选择打印机、耗材和打印参数
5. 回到命令行窗口按 Enter
6. 工具尝试用 `Ctrl+S` 把当前项目保存为同名 `.3mf`，3MF 默认放在模型同级目录

如果自动保存失败，请手动在 Bambu Studio 中另存为命令行提示的 3MF 路径。

## 配置

与主程序、SolidWorks 插件共用同一份配置：

```text
%APPDATA%\ModelExplorer\config.json
```

其中与本工具相关的字段为 `BambuPath`、`SolidWorksPath`、`KeepHistory`、
`BinaryStl`、`StlUnits`、`StlQuality`。这些值可以直接在主程序的“设置”里修改，
无需单独编辑文件。

> V2.4.1 及更早版本使用的 `%APPDATA%\ModelExplorerAddin\ModelExplorerAddin.config`
> 会在首次运行时自动迁移，原文件改名为 `.migrated` 保留。

## 构建

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

或直接：

```powershell
dotnet build ModelExplorerCli.csproj -c Release
```

输出到 `bin\Release\ModelExplorerCli.exe`。Interop 依赖统一来自仓库根
`lib\SolidWorks\`，不再需要本机存在 `D:\SW2022\SOLIDWORKS\`。

## 版本变更

### V3.0.0

- **修正分类文件夹名**：输出目录由 `STL文件` 改为 `STL文件夹`。
  旧名称与主程序不一致，会让 CLI 导出的 STL 在主程序中被误判为“未整理”并被重复搬移。
- **统一配置**：改用 `%APPDATA%\ModelExplorer\config.json`，与主程序、插件共享；
  旧配置自动迁移。此前 CLI 的“保留历史版本”等设置与主程序互不生效。
- **消除重复实现**：导出、配置、Bambu 启动全部复用 `ModelExplorer.Core`，
  删除本工程内重复的 `SetUserPreference` / `SaveAs3` / `SendKeys` 代码。
- **不再引用插件工程**：此前 CLI 依赖插件的 `AddinSettings`，现在只依赖 Core。
- **构建方式**：由 `csc.exe` 手工拼命令行改为标准 `dotnet build`，
  不再硬编码 `D:\SW2022\SOLIDWORKS\`，换机器即可构建。
- **读取 SolidWorks 路径**：现在会使用设置中的 `SLDWORKS.exe` 路径，此前只依赖 COM 注册。
- 参数与退出码与 V2.4.1 保持一致，现有脚本无需改动。
