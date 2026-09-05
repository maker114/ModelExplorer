# Model Explorer Add-in

SolidWorks 2022 C# Add-in，把当前打开的零件或装配体一键导出为 STL，并自动交给 Bambu Studio 打开。

## 功能

- 导出到当前模型所在目录，文件名与模型同名
- 默认覆盖同名 STL，SolidWorks 不弹确认框
- 可在设置中开启“保留历史版本”，同名文件会生成 `模型_20260830_153000.stl`
- 可配置 Bambu Studio 路径、STL 单位、二进制/ASCII、精细/粗糙质量
- 菜单和工具栏都有入口

## 构建

运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

构建脚本会读取：

- `D:\SW2022\SOLIDWORKS\SolidWorks.Interop.sldworks.dll`
- `D:\SW2022\SOLIDWORKS\SolidWorks.Interop.swconst.dll`
- `D:\SW2022\SOLIDWORKS\SolidWorks.Interop.swpublished.dll`

输出到 `bin\ModelExplorerAddin.dll`。

## 安装

构建后运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

默认只注册当前用户。如果需要在所有 Windows 用户下使用，用管理员 PowerShell 运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -AllUsers
```

更简单的做法是关闭 SolidWorks 后，右键 `install_admin.bat`，选择“以管理员身份运行”，脚本会自动弹出 UAC 确认并完成注册。

然后重启 SolidWorks，在 `工具 > 插件` 中勾选 `Model Explorer Add-in`。

配置保存在：

```text
%APPDATA%\ModelExplorerAddin\ModelExplorerAddin.config
```

也可以在 SolidWorks 菜单 `Model Explorer > Model Explorer Settings` 中修改。

## 卸载

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

## 注意

- 当前模型必须已经保存过，插件才能确定“模型同目录”
- 插件按当前用户注册，不需要管理员权限
- 卸载插件前先退出 SolidWorks
