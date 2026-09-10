using System;
using System.Drawing;
using System.Windows.Forms;
using ModelExplorer;

namespace ModelExplorerAddin
{
    /// <summary>
    /// 插件设置窗体。v3.0.0 起直接读写共享的 <see cref="AppConfig"/>，
    /// 不再使用独立的 ModelExplorerAddin.config（同一开关在 GUI 与插件间互不生效的问题已修复）。
    /// </summary>
    public sealed class SettingsForm : Form
    {
        private readonly AppConfig _source;
        private TextBox _bambuPath;
        private CheckBox _keepHistory;
        private CheckBox _binaryStl;
        private ComboBox _stlUnits;
        private ComboBox _stlQuality;

        public SettingsForm(AppConfig settings)
        {
            _source = settings;

            Text = "Model Explorer 设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 260);
            AutoScaleMode = AutoScaleMode.Dpi;

            Label pathLabel = new Label();
            pathLabel.Text = "Bambu Studio 路径";
            pathLabel.SetBounds(16, 18, 130, 22);
            Controls.Add(pathLabel);

            _bambuPath = new TextBox();
            _bambuPath.Text = settings.BambuPath;
            _bambuPath.SetBounds(150, 16, 320, 24);
            Controls.Add(_bambuPath);

            Button browseButton = new Button();
            browseButton.Text = "浏览...";
            browseButton.SetBounds(480, 15, 64, 26);
            browseButton.Click += BrowseButton_Click;
            Controls.Add(browseButton);

            _keepHistory = new CheckBox();
            _keepHistory.Text = "保留历史版本（同名 STL 生成带时间戳的新文件）";
            _keepHistory.Checked = settings.KeepHistory;
            _keepHistory.SetBounds(24, 58, 440, 24);
            Controls.Add(_keepHistory);

            _binaryStl = new CheckBox();
            _binaryStl.Text = "使用二进制 STL";
            _binaryStl.Checked = settings.UseBinaryStl;
            _binaryStl.SetBounds(24, 92, 220, 24);
            Controls.Add(_binaryStl);

            Label unitsLabel = new Label();
            unitsLabel.Text = "STL 单位";
            unitsLabel.SetBounds(24, 128, 90, 22);
            Controls.Add(unitsLabel);

            _stlUnits = new ComboBox();
            _stlUnits.DropDownStyle = ComboBoxStyle.DropDownList;
            _stlUnits.Items.AddRange(new object[] { "mm", "cm", "m", "in" });
            _stlUnits.SelectedItem = SolidWorksStlExporter.NormalizeUnits(settings.StlUnits);
            _stlUnits.SetBounds(120, 126, 100, 24);
            Controls.Add(_stlUnits);

            Label qualityLabel = new Label();
            qualityLabel.Text = "STL 质量";
            qualityLabel.SetBounds(250, 128, 90, 22);
            Controls.Add(qualityLabel);

            _stlQuality = new ComboBox();
            _stlQuality.DropDownStyle = ComboBoxStyle.DropDownList;
            // 与主程序设置窗口显示同一组中文名称（配置里仍存 Coarse / Fine）
            foreach (string qualityName in StlQualityLabels.DisplayNames)
            {
                _stlQuality.Items.Add(qualityName);
            }
            _stlQuality.SelectedItem = StlQualityLabels.ToDisplay(settings.StlQuality);
            _stlQuality.SetBounds(346, 126, 100, 24);
            Controls.Add(_stlQuality);

            Button saveButton = new Button();
            saveButton.Text = "保存";
            saveButton.DialogResult = DialogResult.OK;
            saveButton.SetBounds(180, 196, 90, 28);
            Controls.Add(saveButton);

            Button cancelButton = new Button();
            cancelButton.Text = "取消";
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.SetBounds(290, 196, 90, 28);
            Controls.Add(cancelButton);

            AcceptButton = saveButton;
            CancelButton = cancelButton;
        }

        /// <summary>
        /// 返回保存后的完整配置：以进入窗体时的配置为基础，只改动本窗体负责的字段，
        /// 避免插件的设置保存把 GUI 的主题、上次目录等字段清掉。
        /// </summary>
        public AppConfig Result
        {
            get
            {
                _source.BambuPath = _bambuPath.Text.Trim();
                _source.KeepHistory = _keepHistory.Checked;
                _source.BinaryStl = _binaryStl.Checked;
                _source.StlUnits = (string)_stlUnits.SelectedItem;
                _source.StlQuality = StlQualityLabels.ToToken((string)_stlQuality.SelectedItem);
                return _source;
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "选择 Bambu Studio 可执行文件";
                dialog.Filter = "Bambu Studio|bambu-studio.exe|可执行文件|*.exe|所有文件|*.*";
                dialog.CheckFileExists = true;
                dialog.Multiselect = false;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _bambuPath.Text = dialog.FileName;
                }
            }
        }
    }
}
