using System;
using System.Collections.Generic;
using System.IO;

namespace ModelExplorer
{
    public sealed class UndoResult
    {
        public UndoResult()
        {
            Failures = new List<string>();
            RestoredMoves = new List<FileMove>();
        }

        public int Restored { get; set; }
        public List<string> Failures { get; private set; }
        public List<FileMove> RestoredMoves { get; private set; }
    }

    /// <summary>
    /// 撤销文件移动（整理 / 工程名修改共用）。
    ///
    /// v2.4.1 的“撤销整理”和“撤销工程名修改”是两段近乎逐字相同的 40 行代码，
    /// 唯一差别是一句提示文案，这里合并为一处。
    /// </summary>
    public static class FileMoveUndo
    {
        /// <param name="occupiedMessage">目标原位置已被占用时的提示后缀。</param>
        public static UndoResult Restore(IList<FileMove> moves, string occupiedMessage)
        {
            UndoResult result = new UndoResult();
            if (moves == null)
            {
                return result;
            }

            // 逆序回退，避免同一批移动出现顺序依赖。
            for (int i = moves.Count - 1; i >= 0; i--)
            {
                FileMove move = moves[i];
                try
                {
                    if (File.Exists(move.Dest) && !File.Exists(move.Source))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(move.Source));
                        File.Move(move.Dest, move.Source);
                        result.Restored++;
                        result.RestoredMoves.Add(move);
                    }
                    else if (!File.Exists(move.Dest))
                    {
                        result.Failures.Add(Path.GetFileName(move.Dest) + "：文件不存在");
                    }
                    else
                    {
                        result.Failures.Add(Path.GetFileName(move.Dest) + "：" + occupiedMessage);
                    }
                }
                catch (Exception ex)
                {
                    result.Failures.Add(Path.GetFileName(move.Dest) + "：" + ex.Message);
                }
            }

            return result;
        }
    }
}
