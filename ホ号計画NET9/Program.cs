using System;
using System.Windows.Forms;

namespace ホ号計画
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // モード選択画面を表示
            using (var modeSelectionForm = new ModeSelectionForm())
            {
                if (modeSelectionForm.ShowDialog() == DialogResult.OK)
                {
                    // 選択されたモードでメインフォームを起動
                    Application.Run(new Form1(modeSelectionForm.SelectedMode));
                }
                // キャンセル時はそのまま終了
            }
        }
    }
}
