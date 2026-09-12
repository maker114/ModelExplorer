using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ModelExplorer
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<ModelFile> _visibleParts;
        private readonly ObservableCollection<ModelFile> _visibleAssemblies;
        private readonly ObservableCollection<ModelFile> _visibleStls;
        private readonly List<ModelFile> _allModels;
        private readonly List<FileMove> _lastMoves = new List<FileMove>();
        private readonly List<FileMove> _lastProjectNameMoves = new List<FileMove>();
        private AppConfig _config;
        private bool _searchPlaceholder;
        private bool _settingsOpening;
        private bool _renameOpening;
        private bool _checkingProjectNames;
        private readonly HashSet<Expander> _animatingExpanders = new HashSet<Expander>();

        public MainWindow()
        {
            _searchPlaceholder = true;
            _visibleParts = new ObservableCollection<ModelFile>();
            _visibleAssemblies = new ObservableCollection<ModelFile>();
            _visibleStls = new ObservableCollection<ModelFile>();
            _allModels = new List<ModelFile>();
            _config = ConfigService.Load();
            ThemeManager.Apply(_config);
            SolidWorksConverter.Log += Converter_Log;

            InitializeComponent();
            Icon = AppIcon.WindowIcon;
            VersionText.Text = DisplayVersion();
            ApplyThemeResources();
            ApplyFontSettings();
            ApplyConfigToUi();
            ApplyFontScale((FrameworkElement)Content);
            UiAnimation.FadeInOnOpen(this);
            Log("Model Explorer 已启动");

            if (!string.IsNullOrEmpty(_config.LastDir) && Directory.Exists(_config.LastDir))
            {
                BeginScan(_config.LastDir);
            }
        }

        private void SetStatusText(string text)
        {
            StatusText.Text = text;
            UiAnimation.Flash(StatusText);
        }

        /// <summary>
        /// 标题栏上的版本号，取程序集版本的前三段（3.2.0.0 → "V 3.2.0"）。
        /// 版本号的单一真源是 ModelExplorer\AssemblyInfo.cs（publish.ps1 也从那里解析），
        /// 这里读程序集而不是再手写一份，升版时就不会漏改标题栏。
        /// </summary>
        private static string DisplayVersion()
        {
            Version version = typeof(MainWindow).Assembly.GetName().Version;
            if (version == null)
            {
                return "";
            }

            return string.Format("V {0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        private void ApplyThemeResources()
        {
            Resources["BgBrush"] = ThemeManager.Current.BgBrush;
            Resources["SidebarBrush"] = ThemeManager.Current.SidebarBrush;
            Resources["PanelBrush"] = ThemeManager.Current.PanelBrush;
            Resources["PanelActiveBrush"] = ThemeManager.Current.PanelActiveBrush;
            Resources["BorderBrush"] = ThemeManager.Current.BorderBrush;
            Resources["TextBrush"] = ThemeManager.Current.TextBrush;
            Resources["MutedBrush"] = ThemeManager.Current.MutedBrush;
            Resources["AccentBrush"] = ThemeManager.Current.AccentBrush;
            Resources["AccentHoverBrush"] = ThemeManager.Current.AccentHoverBrush;
            Resources["PartBrush"] = ThemeManager.Current.PartBrush;
            Resources["AssemblyBrush"] = ThemeManager.Current.AssemblyBrush;
            Resources["StlBrush"] = ThemeManager.Current.StlBrush;
            Resources["SuccessBrush"] = ThemeManager.Current.SuccessBrush;
            Resources["ErrorBrush"] = ThemeManager.Current.ErrorBrush;
            Resources["CodeBrush"] = ThemeManager.Current.CodeBrush;
            Application app = Application.Current;
            if (app != null)
            {
                app.Resources["ScrollBarTrackBrush"] = ThemeManager.Current.CodeBrush;
                app.Resources["ScrollBarThumbBrush"] = ThemeManager.Current.BorderBrush;
                app.Resources["ScrollBarThumbHoverBrush"] = ThemeManager.Current.PanelActiveBrush;
                app.Resources["ScrollBarThumbPressedBrush"] = ThemeManager.Current.AccentBrush;
            }

            // 玻璃背景层要等主题画刷就位后再铺，且必须在设置窗口改完主题重建主窗口时重铺
            Glass.Apply(ShellRoot, ThemeManager.Current);
        }

        private void ApplyFontSettings()
        {
            Resources["BadgeFontSize"] = (double)Math.Max(9, _config.FontSize - 1);
            Resources["NameFontSize"] = (double)Math.Max(10, _config.FontSize + 1);
            Resources["MetaFontSize"] = (double)Math.Max(8, _config.FontSize - 2);
        }

        private void ApplyConfigToUi()
        {
            // 「保留历史版本 / 导出后打开 Bambu Studio」于 V3.0.5 移入设置窗口，
            // 导出时直接读取 _config，不再有主界面控件。
            ApplyOrganizeModeToUi();
            PartList.ItemsSource = _visibleParts;
            AsmList.ItemsSource = _visibleAssemblies;
            StlList.ItemsSource = _visibleStls;
        }

        /// <summary>
        /// V3.1.3：「按文件夹整理」开关在设置窗口里，主界面用按钮文本回显当前模式，
        /// 免得点下去才知道文件会被整理到哪里。设置保存后主窗口会重建，因此这里就够了。
        /// </summary>
        private void ApplyOrganizeModeToUi()
        {
            OrganizeFilesButton.Content = _config.OrganizeByFolder
                ? "按文件夹整理文件"
                : "按根目录整理文件";
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChooseFolder_Click(object sender, RoutedEventArgs e)
        {
            string initial = !string.IsNullOrEmpty(DirectoryText.Text) && Directory.Exists(DirectoryText.Text)
                ? DirectoryText.Text
                : _config.LastDir;
            string path = ModernFolderPicker.PickFolder(this, initial);
            if (!string.IsNullOrEmpty(path))
            {
                BeginScan(path);
            }
        }

        private void OpenProjectFolder_Click(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(_config.LastDir) && Directory.Exists(_config.LastDir))
            {
                Process.Start("explorer.exe", "\"" + _config.LastDir + "\"");
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_config.LastDir) && Directory.Exists(_config.LastDir))
            {
                BeginScan(_config.LastDir);
            }
        }

        private void BeginScan(string path)
        {
            _config.LastDir = path;
            ConfigService.Save(_config);
            DirectoryText.Text = path;
            SetStatusText("正在扫描 " + path);
            ScanButton.IsEnabled = false;
            ExportButton.IsEnabled = false;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<ModelFile> result = ProjectScanner.Scan(path);
                    Dispatcher.BeginInvoke(new Action(() => OnScanComplete(result)));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SetStatusText("扫描失败");
                        Log("扫描失败：" + ex.Message);
                        ScanButton.IsEnabled = true;
                    }));
                }
            });
        }

        private void OnScanComplete(List<ModelFile> result)
        {
            _allModels.Clear();
            _allModels.AddRange(result);
            _visibleParts.Clear();
            _visibleAssemblies.Clear();
            _visibleStls.Clear();

            // V3.0.7：左侧「统计」只显示零件、装配体与文件总数；
            // STL / 3MF 数量不在统计框里显示（详细统计窗口不受影响）。
            int parts = 0;
            int asms = 0;
            foreach (ModelFile model in _allModels)
            {
                if (model.Kind == ModelKind.Part)
                {
                    parts++;
                }
                else if (model.Kind == ModelKind.Assembly)
                {
                    asms++;
                }
            }

            PartCountText.Text = parts.ToString();
            AsmCountText.Text = asms.ToString();
            TotalCountText.Text = _allModels.Count.ToString();
            UiAnimation.Pulse(PartCountText);
            UiAnimation.Pulse(AsmCountText);
            UiAnimation.Pulse(TotalCountText);
            ApplyFilter();
            ScanButton.IsEnabled = true;
            SetStatusText("扫描完成：" + _allModels.Count + " 个模型");
            Log("扫描完成：" + _allModels.Count + " 个模型");
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_searchPlaceholder)
            {
                ApplyFilter();
            }
        }

        private void SearchBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (_searchPlaceholder)
            {
                SearchBox.Text = "";
                SearchBox.Foreground = ThemeManager.Current.TextBrush;
                _searchPlaceholder = false;
            }
        }

        private void SearchBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "搜索零件或装配体";
                SearchBox.Foreground = ThemeManager.Current.MutedBrush;
                _searchPlaceholder = true;
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            string keyword = _searchPlaceholder ? "" : SearchBox.Text.Trim();
            _visibleParts.Clear();
            _visibleAssemblies.Clear();
            _visibleStls.Clear();
            foreach (ModelFile model in _allModels)
            {
                if (keyword.Length == 0 ||
                    model.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    model.RelativePath.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (model.Kind == ModelKind.Part)
                    {
                        _visibleParts.Add(model);
                    }
                    else if (model.Kind == ModelKind.Assembly)
                    {
                        _visibleAssemblies.Add(model);
                    }
                    else if (model.Kind == ModelKind.Stl)
                    {
                        _visibleStls.Add(model);
                    }
                }
            }

            if (PartHeaderText != null)
            {
                PartHeaderText.Text = "[" + _visibleParts.Count + "]";
            }
            if (AsmHeaderText != null)
            {
                AsmHeaderText.Text = "[" + _visibleAssemblies.Count + "]";
            }
            if (StlHeaderText != null)
            {
                StlHeaderText.Text = "[" + _visibleStls.Count + "]";
            }

            RefreshModelLists();
        }

        private void ExpanderHeader_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed)
            {
                return;
            }

            FrameworkElement header = sender as FrameworkElement;
            Expander expander = header == null ? null : FindAncestor<Expander>(header);
            if (expander == null)
            {
                return;
            }

            e.Handled = true;
            AnimateExpander(expander);
        }

        private void AnimateExpander(Expander expander)
        {
            FrameworkElement content = expander.Content as FrameworkElement;
            if (content == null || _animatingExpanders.Contains(expander))
            {
                return;
            }

            _animatingExpanders.Add(expander);
            if (expander.IsExpanded)
            {
                UiAnimation.AnimateOpacity(content, 1, 0, 160, delegate
                {
                    _animatingExpanders.Remove(expander);
                    expander.IsExpanded = false;
                    UiAnimation.SetContentOpacity(content, 1);
                });
                return;
            }

            UiAnimation.SetContentOpacity(content, 0);
            expander.IsExpanded = true;
            UiAnimation.AnimateOpacity(content, 0, 1, 180, delegate
            {
                _animatingExpanders.Remove(expander);
                UiAnimation.SetContentOpacity(content, 1);
            });
        }

        private void RefreshModelLists()
        {
            UiAnimation.Refresh(PartList);
            UiAnimation.Refresh(AsmList);
            UiAnimation.Refresh(StlList);
        }

        private void PartList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModelFile selected = PartList.SelectedItem as ModelFile;
            if (selected != null)
            {
                AsmList.SelectedItem = null;
                StlList.SelectedItem = null;
            }
            ShowSelection(selected);
        }

        private void AsmList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModelFile selected = AsmList.SelectedItem as ModelFile;
            if (selected != null)
            {
                PartList.SelectedItem = null;
                StlList.SelectedItem = null;
            }
            ShowSelection(selected);
        }

        private void StlList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModelFile selected = StlList.SelectedItem as ModelFile;
            if (selected != null)
            {
                PartList.SelectedItem = null;
                AsmList.SelectedItem = null;
            }
            ShowSelection(selected);
        }

        private ModelFile GetSelectedModel()
        {
            ModelFile part = PartList.SelectedItem as ModelFile;
            if (part != null)
            {
                return part;
            }
            ModelFile assembly = AsmList.SelectedItem as ModelFile;
            if (assembly != null)
            {
                return assembly;
            }
            return StlList.SelectedItem as ModelFile;
        }

        private void ShowSelection(ModelFile selected)
        {
            if (selected == null)
            {
                DetailName.Text = "未选择";
                DetailMeta.Text = "";
                ExportButton.IsEnabled = false;
                RenameButton.IsEnabled = false;
                return;
            }

            DetailName.Text = selected.Name;
            string status = "";
            if (selected.Kind == ModelKind.Stl)
            {
                if (selected.IsUnorganized)
                {
                    status += "未整理\n";
                }
                if (!selected.IsAssemblyExport)
                {
                    status += (selected.IsOrphan ? "未对应工程文件" : "已对应工程文件") + "\n";
                }
            }
            DetailMeta.Text =
                "类型：" + selected.TypeLabel + "\n" +
                status +
                "路径：" + selected.Path + "\n" +
                "大小：" + selected.SizeText + "\n" +
                "修改时间：" + selected.ModifiedText;
            UiAnimation.Pulse(DetailName);
            UiAnimation.Pulse(DetailMeta);
            ExportButton.IsEnabled = selected.Kind != ModelKind.Stl;
            RenameButton.IsEnabled = true;
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (_renameOpening)
            {
                return;
            }

            ModelFile selected = GetSelectedModel();
            if (selected == null)
            {
                return;
            }

            _renameOpening = true;
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(220)
            };
            timer.Tick += delegate
            {
                timer.Stop();
                _renameOpening = false;
                ShowRenameDialog(selected);
            };
            timer.Start();
        }

        private void ShowRenameDialog(ModelFile selected)
        {
            RenameDialog dialog = new RenameDialog(selected.Path);
            dialog.Owner = this;
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string sourcePath = selected.Path;
            string targetPath = dialog.TargetPath;
            if (string.IsNullOrEmpty(targetPath) ||
                string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RenameButton.IsEnabled = false;
            SetStatusText("正在重命名：" + selected.Name);
            Log("开始重命名：" + selected.Name);

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    File.Move(sourcePath, targetPath);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        selected.Name = Path.GetFileName(targetPath);
                        selected.Path = targetPath;
                        DetailName.Text = selected.Name;
                        SetStatusText("重命名完成：" + Path.GetFileName(targetPath));
                        Log("重命名完成：" + Path.GetFileName(targetPath));
                        RenameButton.IsEnabled = false;
                        BeginScan(_config.LastDir);
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SetStatusText("重命名失败");
                        Log("重命名失败：" + ex.Message);
                        RenameButton.IsEnabled = GetSelectedModel() != null;
                    }));
                }
            });
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            ModelFile selected = GetSelectedModel();
            if (selected == null)
            {
                return;
            }

            ConvertOptions options = new ConvertOptions
            {
                // 「保留历史版本 / 导出后打开 Bambu Studio」现由设置窗口维护
                KeepHistory = _config.KeepHistory,
                OpenBambu = _config.OpenBambu,
                BinaryStl = _config.UseBinaryStl,
                StlUnits = _config.StlUnits,
                StlQuality = _config.StlQuality
            };

            ExportButton.IsEnabled = false;
            SetStatusText("正在导出 STL：" + selected.Name);
            Log("开始转换：" + selected.Name);

            Thread worker = new Thread(delegate()
            {
                try
                {
                    ConvertResult result = SolidWorksConverter.Convert(selected.Path, options);
                    Dispatcher.BeginInvoke(new Action(() => OnExportComplete(result, options)));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ExportButton.IsEnabled = true;
                        SetStatusText("转换失败");
                        Log("转换失败：" + ex.Message);
                    }));
                }
            });
            worker.SetApartmentState(ApartmentState.STA);
            worker.IsBackground = true;
            worker.Start();
        }

        private void OnExportComplete(ConvertResult result, ConvertOptions options)
        {
            ExportButton.IsEnabled = true;
            Log("STL：" + result.StlPath);
            SetStatusText("STL 导出完成");

            if (options.OpenBambu)
            {
                SetStatusText("Bambu Studio 已打开");
            }
            else
            {
                SetStatusText("转换完成");
            }
        }

        private void OrganizeFiles_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_config.LastDir) || !Directory.Exists(_config.LastDir))
            {
                Log("请先选择工程目录");
                return;
            }

            string root = _config.LastDir;
            bool byFolder = _config.OrganizeByFolder;
            if (HasMultipleSourceFolders())
            {
                // 提示文案跟随当前模式，别和按钮上的「按…整理文件」自相矛盾
                string target = byFolder
                    ? "移动到各自所在目录的「" + WorkspaceNames.StlFolderName + " / " + WorkspaceNames.ThreeMfFolderName + "」中"
                    : "集中到工程根目录的「" + WorkspaceNames.StlFolderName + " / " + WorkspaceNames.ThreeMfFolderName + "」中";
                ConfirmDialog confirm = new ConfirmDialog(
                    "检测到零件/装配体来自多个文件夹。按当前设置，整理后 STL / 3MF 会" + target + "，是否继续？",
                    "确认整理");
                confirm.Owner = this;
                confirm.ShowDialog();
                if (!confirm.Confirmed)
                {
                    return;
                }
            }

            SetStatusText("正在整理 STL / 3MF 文件");
            Log("开始整理 STL / 3MF 文件（" + (byFolder ? "按文件夹" : "按根目录") + "）");
            UndoProjectNameButton.IsEnabled = false;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    OrganizeResult result = FileOrganizer.Organize(root, byFolder);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        foreach (OrganizeLogEntry entry in result.Logs)
                        {
                            Log(entry.SourceFolder + "：" + entry.StlCount + " 个 STL、" +
                                entry.ThreeMfCount + " 个 3MF → " + entry.TargetFolder);
                        }
                        if (result.DeletedFolders.Count > 0)
                        {
                            Log("删除空分类文件夹：" + string.Join("，", result.DeletedFolders.ToArray()));
                        }
                        Log("整理完成：移动 STL " + result.StlMoved + " 个、3MF " + result.ThreeMfMoved +
                            " 个；已有 STL " + result.StlAlreadyOrganized + " 个、3MF " +
                            result.ThreeMfAlreadyOrganized + " 个位于分类文件夹中");
                        SetStatusText("整理完成");
                        _lastMoves.Clear();
                        _lastMoves.AddRange(result.Moves);
                        UndoOrganizeButton.IsEnabled = result.Moves.Count > 0;
                        RetargetProjectNameUndoAfterOrganize(result.Moves);
                        UndoProjectNameButton.IsEnabled = _lastProjectNameMoves.Count > 0;
                        if (result.Moves.Count > 0)
                        {
                            List<OrganizeMoveInfo> report = new List<OrganizeMoveInfo>();
                            foreach (OrganizeLogEntry entry in result.Logs)
                            {
                                report.Add(new OrganizeMoveInfo
                                {
                                    SourceFolder = entry.SourceFolder,
                                    TargetFolder = entry.TargetFolder,
                                    StlCount = entry.StlCount,
                                    ThreeMfCount = entry.ThreeMfCount
                                });
                            }
                            OrganizeReportWindow reportWindow = new OrganizeReportWindow(
                                report,
                                result.CreatedFolders,
                                result.DeletedFolders);
                            reportWindow.Owner = this;
                            reportWindow.ShowDialog();
                        }
                        else
                        {
                            // V3.0.4：没有任何文件需要移动时也要给出明确反馈，
                            // 否则界面上什么都不发生，容易被误认为“扫描不到 STL / 3MF”。
                            ConfirmDialog.ShowInfo(
                                this,
                                "本次没有需要移动的文件。\n\n" +
                                "已扫描到 STL " + result.StlAlreadyOrganized + " 个、3MF " +
                                result.ThreeMfAlreadyOrganized + " 个，均已位于「" +
                                WorkspaceNames.StlFolderName + "」或「" + WorkspaceNames.ThreeMfFolderName + "」中。\n\n" +
                                "整理只处理散落在分类文件夹之外的文件，已在分类文件夹中的不会被再次移动。",
                                "整理完成");
                        }
                        BeginScan(root);
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SetStatusText("整理失败");
                        Log("整理失败：" + ex.Message);
                        UndoProjectNameButton.IsEnabled = _lastProjectNameMoves.Count > 0;
                    }));
                }
            });
        }

        private bool HasMultipleSourceFolders()
        {
            HashSet<string> folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModelFile model in _allModels)
            {
                if ((model.Kind == ModelKind.Part || model.Kind == ModelKind.Assembly) &&
                    !string.IsNullOrEmpty(model.Folder))
                {
                    folders.Add(model.Folder);
                }
            }
            return folders.Count > 1;
        }

        private void Stats_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_allModels.Count == 0)
            {
                Log("尚未扫描到模型，无法查看详细统计");
                return;
            }

            // V3.1.2：不再要求“零件来自多个文件夹”才允许打开。
            // 侧栏统计已不显示 STL / 3MF 数量，详细窗口是查看各类文件数量的入口，
            // 单文件夹工程同样需要它。汇总逻辑在 Core 的 FolderStatistics 中。
            List<FolderStat> stats = FolderStatistics.Build(_allModels);
            StatisticsWindow window = new StatisticsWindow(stats);
            window.Owner = this;
            window.ShowDialog();
        }

        private void UndoOrganize_Click(object sender, RoutedEventArgs e)
        {
            if (_lastMoves.Count == 0)
            {
                return;
            }

            List<FileMove> moves = new List<FileMove>(_lastMoves);
            UndoOrganizeButton.IsEnabled = false;
            UndoProjectNameButton.IsEnabled = false;
            SetStatusText("正在撤销整理");
            Log("开始撤销整理");

            ThreadPool.QueueUserWorkItem(delegate
            {
                UndoResult result = FileMoveUndo.Restore(moves, "原位置已存在文件");

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (string failure in result.Failures)
                    {
                        Log("撤销整理失败：" + failure);
                    }
                    _lastMoves.Clear();
                    RetargetProjectNameUndoAfterOrganizeUndo(result.RestoredMoves);
                    UndoProjectNameButton.IsEnabled = _lastProjectNameMoves.Count > 0;
                    SetStatusText("撤销整理完成：" + result.Restored + " 项");
                    Log("撤销整理完成：" + result.Restored + " 项" +
                        (result.Failures.Count > 0 ? "，失败 " + result.Failures.Count + " 项" : ""));
                    BeginScan(_config.LastDir);
                }));
            });
        }

        private void RetargetProjectNameUndoAfterOrganize(List<FileMove> organizeMoves)
        {
            foreach (FileMove organizeMove in organizeMoves)
            {
                foreach (FileMove projectMove in _lastProjectNameMoves)
                {
                    if (PathHelpers.PathEquals(projectMove.Dest, organizeMove.Source))
                    {
                        projectMove.Dest = organizeMove.Dest;
                    }
                }
            }
        }

        private void RetargetProjectNameUndoAfterOrganizeUndo(List<FileMove> restoredOrganizeMoves)
        {
            foreach (FileMove restoredMove in restoredOrganizeMoves)
            {
                foreach (FileMove projectMove in _lastProjectNameMoves)
                {
                    if (PathHelpers.PathEquals(projectMove.Dest, restoredMove.Dest))
                    {
                        projectMove.Dest = restoredMove.Source;
                    }
                }
            }
        }

        private void RemoveOrganizeMovesAffectedByProjectUndo(FileMove projectMove)
        {
            string preOrganizePath = Path.Combine(
                Path.GetDirectoryName(projectMove.Source),
                Path.GetFileName(projectMove.Dest));
            _lastMoves.RemoveAll(delegate(FileMove move)
            {
                return PathHelpers.PathEquals(move.Source, preOrganizePath) ||
                       PathHelpers.PathEquals(move.Dest, projectMove.Dest);
            });
        }

        private void CheckProjectNames_Click(object sender, RoutedEventArgs e)
        {
            if (_checkingProjectNames)
            {
                return;
            }
            if (string.IsNullOrEmpty(_config.LastDir) || !Directory.Exists(_config.LastDir))
            {
                Log("请先选择工程目录");
                return;
            }

            _checkingProjectNames = true;
            CheckProjectNamesButton.IsEnabled = false;
            UndoProjectNameButton.IsEnabled = false;
            string root = _config.LastDir;
            SetStatusText("正在分析工程名...");
            Log("开始检查工程名");

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<ModelFile> files = ProjectScanner.Scan(root);
                    List<ProjectNameChange> changes = ProjectNamePlanner.BuildPlan(root, files);
                    Dispatcher.BeginInvoke(new Action(() => OpenProjectNamePlan(files, changes)));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FinishProjectNameCheck("工程名检查失败", ex.Message);
                    }));
                }
            });
        }

        private void OpenProjectNamePlan(List<ModelFile> files, List<ProjectNameChange> changes)
        {
            if (changes.Count == 0)
            {
                // V3.0.4：没有需要改名时也要给出明确反馈（原来只写日志、不弹窗，
                // 界面上毫无反应，容易被误认为功能不可用）。
                ConfirmDialog.ShowInfo(
                    this,
                    "工程名检查完成，没有需要修改的文件。\n\n" +
                    DescribeScanResult(files) +
                    "\n\n所有文件的工程名前缀都已符合当前规则。",
                    "工程名检查完成");
                FinishProjectNameCheck("工程名检查完成，无需修改", DescribeScanResult(files));
                return;
            }

            ProjectNamePlanWindow window = new ProjectNamePlanWindow(
                changes,
                _config.ProjectNameUnchecked ?? new List<string>());
            window.Owner = this;
            if (window.ShowDialog() != true)
            {
                FinishProjectNameCheck("工程名检查已取消", "");
                return;
            }

            _config.ProjectNameUnchecked = window.NewUncheckedPaths;
            ConfigService.Save(_config);
            ExecuteProjectNameChanges(window.ConfirmedChanges);
        }

        private void ExecuteProjectNameChanges(List<ProjectNameChange> changes)
        {
            if (changes.Count == 0)
            {
                FinishProjectNameCheck("没有勾选需要修改的工程名", "");
                return;
            }

            SetStatusText("正在执行工程名修改...");
            Log("执行工程名修改：" + changes.Count + " 项");
            _lastProjectNameMoves.Clear();
            UndoProjectNameButton.IsEnabled = false;

            ThreadPool.QueueUserWorkItem(delegate
            {
                int applied = 0;
                List<string> failures = new List<string>();
                List<FileMove> appliedMoves = new List<FileMove>();
                foreach (ProjectNameChange change in changes)
                {
                    try
                    {
                        if (File.Exists(change.SourcePath) && !File.Exists(change.TargetPath))
                        {
                            File.Move(change.SourcePath, change.TargetPath);
                            applied++;
                            appliedMoves.Add(new FileMove
                            {
                                Source = change.SourcePath,
                                Dest = change.TargetPath
                            });
                        }
                        else if (!File.Exists(change.SourcePath))
                        {
                            failures.Add(change.OriginalName + "：源文件不存在");
                        }
                        else
                        {
                            failures.Add(change.OriginalName + "：目标文件已存在");
                        }
                    }
                    catch (Exception ex)
                    {
                        failures.Add(change.OriginalName + "：" + ex.Message);
                    }
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (string failure in failures)
                    {
                        Log("工程名修改失败：" + failure);
                    }
                    SetStatusText("工程名处理完成：" + applied + " 项");
                    Log("工程名处理完成：" + applied + " 项" +
                        (failures.Count > 0 ? "，失败 " + failures.Count + " 项" : ""));
                    _lastProjectNameMoves.AddRange(appliedMoves);
                    UndoProjectNameButton.IsEnabled = appliedMoves.Count > 0;
                    _checkingProjectNames = false;
                    CheckProjectNamesButton.IsEnabled = true;
                    if (applied > 0)
                    {
                        BeginScan(_config.LastDir);
                    }
                }));
            });
        }

        /// <summary>
        /// 把扫描结果概括成一句话，用于“无需修改 / 无需整理”这类反馈，
        /// 让用户确认程序确实扫描过了，而不是没有反应。
        /// </summary>
        private static string DescribeScanResult(List<ModelFile> files)
        {
            int parts = 0;
            int assemblies = 0;
            int stls = 0;
            int threeMfs = 0;
            foreach (ModelFile file in files)
            {
                if (file.Kind == ModelKind.Part)
                {
                    parts++;
                }
                else if (file.Kind == ModelKind.Assembly)
                {
                    assemblies++;
                }
                else if (file.Kind == ModelKind.Stl)
                {
                    stls++;
                }
                else
                {
                    threeMfs++;
                }
            }

            return "已扫描到 " + files.Count + " 个文件：零件 " + parts + " 个、装配体 " + assemblies +
                   " 个、STL " + stls + " 个、3MF " + threeMfs + " 个";
        }

        private void FinishProjectNameCheck(string message, string detail)
        {
            _checkingProjectNames = false;
            CheckProjectNamesButton.IsEnabled = true;
            UndoProjectNameButton.IsEnabled = _lastProjectNameMoves.Count > 0;
            SetStatusText(message);
            if (string.IsNullOrEmpty(detail))
            {
                Log(message);
            }
            else
            {
                Log(message + "：" + detail);
            }
        }

        private void UndoProjectName_Click(object sender, RoutedEventArgs e)
        {
            if (_lastProjectNameMoves.Count == 0)
            {
                return;
            }

            List<FileMove> moves = new List<FileMove>(_lastProjectNameMoves);
            UndoProjectNameButton.IsEnabled = false;
            UndoOrganizeButton.IsEnabled = false;
            SetStatusText("正在撤销工程名修改");
            Log("开始撤销工程名修改");

            ThreadPool.QueueUserWorkItem(delegate
            {
                UndoResult result = FileMoveUndo.Restore(moves, "原名称位置已存在文件");

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (string failure in result.Failures)
                    {
                        Log("撤销工程名失败：" + failure);
                    }
                    foreach (FileMove restoredMove in result.RestoredMoves)
                    {
                        RemoveOrganizeMovesAffectedByProjectUndo(restoredMove);
                    }
                    UndoOrganizeButton.IsEnabled = _lastMoves.Count > 0;
                    _lastProjectNameMoves.Clear();
                    SetStatusText("撤销工程名完成：" + result.Restored + " 项");
                    Log("撤销工程名完成：" + result.Restored + " 项" +
                        (result.Failures.Count > 0 ? "，失败 " + result.Failures.Count + " 项" : ""));
                    UndoProjectNameButton.IsEnabled = false;
                    BeginScan(_config.LastDir);
                }));
            });
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsOpening)
            {
                return;
            }

            _settingsOpening = true;
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(220)
            };
            timer.Tick += delegate
            {
                timer.Stop();
                _settingsOpening = false;
                OpenSettingsWindow();
            };
            timer.Start();
        }

        private void OpenSettingsWindow()
        {
            SettingsWindow dialog = new SettingsWindow(_config);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                _config = dialog.Result;
                ConfigService.Save(_config);
                MainWindow next = new MainWindow();
                Application.Current.MainWindow = next;
                next.Show();
                Close();
            }
        }

        private void Converter_Log(string message)
        {
            Dispatcher.BeginInvoke(new Action(() => Log(message)));
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer viewer = sender as ScrollViewer;
            if (viewer == null)
            {
                return;
            }
            double offset = viewer.VerticalOffset - e.Delta;
            if (offset < 0)
            {
                offset = 0;
            }
            if (offset > viewer.ScrollableHeight)
            {
                offset = viewer.ScrollableHeight;
            }
            viewer.ScrollToVerticalOffset(offset);
            e.Handled = true;
        }

        private void ApplyFontScale(FrameworkElement root)
        {
            int delta = _config.FontSize - 12;
            if (delta == 0)
            {
                return;
            }

            foreach (FrameworkElement element in FindVisualChildren<FrameworkElement>(root))
            {
                if (element is TextBlock)
                {
                    TextBlock text = (TextBlock)element;
                    text.FontSize = Math.Max(8, text.FontSize + delta);
                }
                else if (element is Button)
                {
                    Button button = (Button)element;
                    button.FontSize = Math.Max(8, button.FontSize + delta);
                }
                else if (element is TextBox)
                {
                    TextBox box = (TextBox)element;
                    box.FontSize = Math.Max(8, box.FontSize + delta);
                }
                else if (element is CheckBox)
                {
                    CheckBox check = (CheckBox)element;
                    check.FontSize = Math.Max(8, check.FontSize + delta);
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T)
                {
                    yield return (T)child;
                }
                foreach (T sub in FindVisualChildren<T>(child))
                {
                    yield return sub;
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                current = VisualTreeHelper.GetParent(current);
                T result = current as T;
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private void Log(string message)
        {
            string stamp = DateTime.Now.ToString("HH:mm:ss");
            LogBox.AppendText("[" + stamp + "] " + message + Environment.NewLine);
            LogBox.ScrollToEnd();
        }
    }
}
