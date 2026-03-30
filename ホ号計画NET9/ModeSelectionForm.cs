using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ホ号計画
{
    public partial class ModeSelectionForm : Form
    {
        public enum AnalysisMode
        {
            EEG,     // 脳波モード
            Heart    // 心拍モード
        }

        public AnalysisMode SelectedMode { get; private set; }
        public bool IsOkPressed { get; private set; } = false;

        private Button eegButton;
        private Button heartButton;
        private Label titleLabel;

        public ModeSelectionForm()
        {
            InitializeComponent();
            InitializeCustomUI();
        }

        private void InitializeCustomUI()
        {
            this.Text = "解析モード選択";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            // タイトルラベル
            titleLabel = new Label
            {
                Text = "解析モードを選択してください",
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 50)
            };
            titleLabel.Location = new Point((this.ClientSize.Width - titleLabel.PreferredWidth) / 2, 50);
            this.Controls.Add(titleLabel);

            // 脳波ボタン
            eegButton = new RoundedButton
            {
                Text = "脳波",
                Size = new Size(250, 50),
                Location = new Point((this.ClientSize.Width - 250) / 2, 120),
                Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold),
                BackColor = Color.LightBlue,
                ForeColor = Color.DarkBlue,
                UseVisualStyleBackColor = false,
                FlatStyle = FlatStyle.Flat
            };
            eegButton.FlatAppearance.BorderSize = 0;
            eegButton.Click += EegButton_Click;
            this.Controls.Add(eegButton);

            // 心拍ボタン
            heartButton = new RoundedButton
            {
                Text = "心拍",
                Size = new Size(250, 50),
                Location = new Point((this.ClientSize.Width - 250) / 2, 185),
                Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold),
                BackColor = Color.LightCoral,
                ForeColor = Color.DarkRed,
                UseVisualStyleBackColor = false,
                FlatStyle = FlatStyle.Flat
            };
            heartButton.FlatAppearance.BorderSize = 0;
            heartButton.Click += HeartButton_Click;
            this.Controls.Add(heartButton);
        }

        private void EegButton_Click(object sender, EventArgs e)
        {
            SelectedMode = AnalysisMode.EEG;
            IsOkPressed = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void HeartButton_Click(object sender, EventArgs e)
        {
            SelectedMode = AnalysisMode.Heart;
            IsOkPressed = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    // 角丸ボタンのカスタムクラス
    public class RoundedButton : Button
    {
        private int borderRadius = 15;

/*        public int BorderRadius
        {
            get { return borderRadius; }
            set { borderRadius = value; this.Invalidate(); }
        }*/

        protected override void OnPaint(PaintEventArgs pevent)
        {
            GraphicsPath path = new GraphicsPath();
            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            int radius = Math.Min(borderRadius, Math.Min(this.Width, this.Height) / 2);

            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();

            this.Region = new Region(path);

            using (SolidBrush brush = new SolidBrush(this.BackColor))
            {
                pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pevent.Graphics.FillPath(brush, path);
            }

            TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font, rect, this.ForeColor, 
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}