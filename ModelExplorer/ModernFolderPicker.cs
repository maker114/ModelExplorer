using System;
using System.IO;
using System.Windows;

namespace ModelExplorer
{
    internal static class ModernFolderPicker
    {
        public static string PickFolder(Window owner, string initialPath)
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Title = "选择 SolidWorks 工程目录";
                dialog.ValidateNames = false;
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.FileName = "Folder Selection.";
                if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                {
                    dialog.InitialDirectory = initialPath;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string folder = Path.GetDirectoryName(dialog.FileName);
                    if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                    {
                        return folder;
                    }
                    return dialog.FileName;
                }
                return null;
            }
        }
    }
}
