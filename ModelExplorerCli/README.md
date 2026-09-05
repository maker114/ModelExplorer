# Model Explorer CLI

不安装 SolidWorks 插件、不改 SolidWorks 注册表，用命令行把 SolidWorks 零件/装配体导出为 STL，交给 Bambu Studio 打开，并等你在 Bambu Studio 中选好打印机和耗材后，把当前项目保存为同名 `.3mf` 工程。

## 用法

```powershell
.\bin\ModelExplorerCli.exe "D:\模型\part.sldprt"
```

参数：

- `--keep-history`：同名 STL 存在时生成带时间戳的新文件
- `--no-bambu`：只导出 STL，不启动 Bambu Studio
- `--output "D:\out\part.stl"`：指定 STL 输出路径
- `--no-3mf`：不生成 3MF 工程
- `--3mf-output "D:\out\part.3mf"`：指定 3MF 输出路径
- `--auto-3mf`：使用旧的后台导出方式直接生成 3MF（不会包含当前界面选择的打印机和耗材信息）

示例：

```powershell
.\bin\ModelExplorerCli.exe "D:\模型\part.sldprt" --keep-history --no-bambu
```

## 原理

工具会启动一个隐藏的 SolidWorks 实例，用 SolidWorks API 打开模型、静默导出 STL，然后关闭模型并退出。它不会像 Add-in 那样写入 SolidWorks 加载项注册表，因此不会影响 SolidWorks 启动。

默认流程：

1. 自动检测模型目录下是否有 `STL文件` 文件夹，没有则创建
2. 导出 STL 到 `模型目录\STL文件\`
3. 打开 Bambu Studio 并加载该 STL
4. 在 Bambu Studio 中选择打印机、耗材和打印参数
5. 回到命令行窗口按 Enter
6. 工具尝试用 `Ctrl+S` 把当前项目保存为同名 `.3mf`，3MF 默认放在模型同级目录

如果自动保存失败，请手动在 Bambu Studio 中另存为命令行提示的 3MF 路径。

## 配置

Bambu Studio 路径继续读取：

```text
%APPDATA%\ModelExplorerAddin\ModelExplorerAddin.config
```

默认值为 `E:\Bambu Studio\bambu-studio.exe`。

## 构建

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```
