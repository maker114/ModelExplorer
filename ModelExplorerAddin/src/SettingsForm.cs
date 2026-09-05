using System;
using System.Drawing;
using System.Windows.Forms;

namespace ModelExplorerAddin
{
    public sealed class SettingsForm : Form
    {
        private TextBox _bambuPath;
        private CheckBox _keepHistory;
        private CheckBox _binaryStl;
        private ComboBox _stlUnits;
        private ComboBox _stlQuality;

        public SettingsForm(AddinSettings settings)
        {
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
            _bambuPath.Text = settings.BambuStudioPath;
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
            _binaryStl.Checked = settings.BinaryStl;
            _binaryStl.SetBounds(24, 92, 220, 24);
            Controls.Add(_binaryStl);

            Label unitsLabel = new Label();
            unitsLabel.Text = "STL 单位";
            unitsLabel.SetBounds(24, 128, 90, 22);
            Controls.Add(unitsLabel);

            _stlUnits = new ComboBox();
            _stlUnits.DropDownStyle = ComboBoxStyle.DropDownList;
            _stlUnits.Items.AddRange(new object[] { "mm", "cm", "m", "in" });
            _stlUnits.SelectedItem = NormalizeUnits(settings.StlUnits);
            _stlUnits.SetBounds(120, 126, 100, 24);
            Controls.Add(_stlUnits);

            Label qualityLabel = new Label();
            qualityLabel.Text = "STL 质量";
            qualityLabel.SetBounds(250, 128, 90, 22);
            Controls.Add(qualityLabel);

            _stlQuality = new ComboBox();
            _stlQuality.DropDownStyle = ComboBoxStyle.DropDownList;
            _stlQuality.Items.AddRange(new object[] { "Coarse", "Fine" });
            _stlQuality.SelectedItem = NormalizeQuality(settings.StlQuality);
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

        public AddinSettings Result
        {
            get
            {
                AddinSettings settings = new AddinSettings();
                settings.BambuStudioPath = _bambuPath.Text.Trim();
                settings.KeepHistory = _keepHistory.Checked;
                settings.BinaryStl = _binaryStl.Checked;
                settings.StlUnits = (string)_stlUnits.SelectedItem;
                settings.StlQuality = (string)_stlQuality.SelectedItem;
                return settings;
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

        private static string NormalizeUnits(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "cm")
            {
                return "cm";
            }
            if (normalized == "m")
            {
                return "m";
            }
            if (normalized == "in")
            {
                return "in";
            }
            return "mm";
        }

        private static string NormalizeQuality(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "coarse")
            {
                return "Coarse";
            }
            return "Fine";
        }
    }
}
