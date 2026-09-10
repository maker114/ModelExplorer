# SolidWorks Interop 依赖

本目录是仓库内**唯一**的 SolidWorks Interop 程序集来源，由 `Directory.Build.props`
以属性 `SolidWorksLibDir` 提供给所有工程。

| 文件 | 大小 | 用途 |
|---|---|---|
| `SolidWorks.Interop.sldworks.dll` | 2.72 MB | SolidWorks API 主互操作程序集 |
| `SolidWorks.Interop.swconst.dll` | 450 KB | 枚举常量（`swUserPreferenceToggle_e` 等） |
| `SolidWorks.Interop.swpublished.dll` | 43 KB | `ISwAddin` 等插件发布接口 |

## 为什么放在这里

v2.4.1 及更早版本存在两个问题，v3.0.0 一并修复：

1. `ModelExplorerAddin.csproj`、`ModelExplorerAddin\build.ps1`、`ModelExplorerCli\build.ps1`
   把路径硬编码为 `D:\SW2022\SOLIDWORKS\`，换一台机器就无法构建；
2. 插件所需的 `SolidWorks.Interop.swpublished.dll` **从未进入版本控制**，
   只在 `ModelExplorerAddin\bin\` 里有一份编译产物副本，仓库克隆后插件无法编译。

现在三个 DLL 都在仓库内，路径只有一处声明，任何机器克隆后即可离线构建。

## 版本

取自本机 SolidWorks 2022 安装目录，与 `D:\SW2022\SOLIDWORKS\` 下的同名文件逐字节一致：

```powershell
Get-FileHash .\SolidWorks.Interop.sldworks.dll -Algorithm MD5
Get-FileHash 'D:\SW2022\SOLIDWORKS\SolidWorks.Interop.sldworks.dll' -Algorithm MD5
```

更换 SolidWorks 版本时，用新版本安装目录下的同名文件替换本目录内容即可，
无需修改任何工程文件。
