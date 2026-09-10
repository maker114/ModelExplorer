# Model Explorer Add-in

SolidWorks 2022 C# Add-in，把当前打开的零件或装配体一键导出为 STL，并自动交给 Bambu Studio 打开。

## 功能

- 导出到当前模型所在目录，文件名与模型同名
- 默认覆盖同名 STL，SolidWorks 不弹确认框
- 可在设置中开启“保留历史版本”，同名文件会生成 `模型_20260830_153000.stl`
- 可配置 Bambu Studio 路径、STL 单位、二进制/ASCII、精细/粗糙质量
- 菜单和工具栏都有入口

## 构建

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

或直接：

```powershell
dotnet build ModelExplorerAddin.csproj -c Release
```

输出到 `bin\Release\ModelExplorerAddin.dll`。

Interop 依赖统一来自仓库根 `lib\SolidWorks\`（含 `swpublished.dll`），
强名称密钥为仓库根 `ModelExplorer.snk`。此前构建脚本硬编码
`D:\SW2022\SOLIDWORKS\`，换机器即无法构建；现在任意机器均可离线构建。

## 安装

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

默认只注册当前用户。如果需要在所有 Windows 用户下使用，用管理员 PowerShell 运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -AllUsers
```

更简单的做法是关闭 SolidWorks 后，右键 `install_admin.bat`，选择“以管理员身份运行”，脚本会自动弹出 UAC 确认并完成注册。

然后重启 SolidWorks，在 `工具 > 插件` 中勾选 `Model Explorer Add-in`。

> **升级提示**：V3.0.0 起插件程序集版本变为 3.0.0.0。COM 注册信息里记录了程序集版本，
> 因此升级插件后必须重新运行 `install.ps1`，否则 SolidWorks 会因版本不匹配而无法加载插件。

## 配置

与主程序、命令行工具共用同一份配置：

```text
%APPDATA%\ModelExplorer\config.json
```

可以在 SolidWorks 菜单 `Model Explorer > Model Explorer Settings` 中修改，
也可以在主程序的“设置”窗口中修改，两侧改动会互相生效。

> V2.4.1 及更早版本使用的 `%APPDATA%\ModelExplorerAddin\ModelExplorerAddin.config`
> 会在首次运行时自动迁移，原文件改名为 `.migrated` 保留。

## 卸载

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

## 注意

- 当前模型必须已经保存过，插件才能确定“模型同目录”
- 插件按当前用户注册，不需要管理员权限
- 卸载插件前先退出 SolidWorks

## 版本变更

### V3.0.0

- **统一配置**：删除本工程自带的 `AddinSettings`，改用共享的
  `%APPDATA%\ModelExplorer\config.json`。此前插件里的“保留历史版本”开关
  与主程序各存一份、互不生效。
- **消除重复实现**：STL 导出的偏好设置、`SaveAs3` 调用、单位/质量映射、
  Bambu Studio 启动全部改为复用 `ModelExplorer.Core`。
- **补齐缺失依赖**：`SolidWorks.Interop.swpublished.dll` 此前从未进入版本控制，
  只在编译输出里存在；现已纳入仓库 `lib\SolidWorks\`，克隆后即可编译。
- **构建方式**：由旧式 csproj + `csc.exe` 脚本改为标准 SDK 项目并纳入
  `ModelExplorer.sln`，不再硬编码 `D:\SW2022\SOLIDWORKS\`。
- **保存设置更安全**：插件设置保存时会基于完整配置只修改自己负责的字段，
  不再覆盖主程序的主题、上次目录等设置。
- 程序集版本更新至 3.0.0.0（升级后需重新运行 `install.ps1`）。
