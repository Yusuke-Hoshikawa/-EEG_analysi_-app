using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ホ号計画
{
    public partial class BandSelectionForm : Form
    {
        public class BandSettings
        {
            public bool Delta { get; set; }
            public bool Theta { get; set; }
            public bool Alpha { get; set; }
            public bool Beta { get; set; }
            public bool Gamma { get; set; }
            public bool LF { get; set; }
            public bool HF { get; set; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public BandSettings EegBandSettings { get; set; } = new BandSettings();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public BandSettings HeartBandSettings { get; set; } = new BandSettings();
        
        private ModeSelectionForm.AnalysisMode _currentMode;

        public BandSelectionForm(ModeSelectionForm.AnalysisMode mode)
        {
            InitializeComponent();
            _currentMode = mode;
            UpdateUIForMode();
        }

        private void UpdateUIForMode()
        {
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                this.Text = "脳波バンド選択";
                eegBandPanel.Visible = true;
                heartBandPanel.Visible = false;
                
                // 初期値設定
                deltaBandCheckBox.Checked = EegBandSettings.Delta;
                thetaBandCheckBox.Checked = EegBandSettings.Theta;
                alphaBandCheckBox.Checked = EegBandSettings.Alpha;
                betaBandCheckBox.Checked = EegBandSettings.Beta;
                gammaBandCheckBox.Checked = EegBandSettings.Gamma;
            }
            else
            {
                this.Text = "心拍バンド選択";
                eegBandPanel.Visible = false;
                heartBandPanel.Visible = true;
                
                // 初期値設定
                lfBandCheckBox.Checked = HeartBandSettings.LF;
                hfBandCheckBox.Checked = HeartBandSettings.HF;
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            // 設定を保存
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                EegBandSettings.Delta = deltaBandCheckBox.Checked;
                EegBandSettings.Theta = thetaBandCheckBox.Checked;
                EegBandSettings.Alpha = alphaBandCheckBox.Checked;
                EegBandSettings.Beta = betaBandCheckBox.Checked;
                EegBandSettings.Gamma = gammaBandCheckBox.Checked;
            }
            else
            {
                HeartBandSettings.LF = lfBandCheckBox.Checked;
                HeartBandSettings.HF = hfBandCheckBox.Checked;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void selectAllButton_Click(object sender, EventArgs e)
        {
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                deltaBandCheckBox.Checked = true;
                thetaBandCheckBox.Checked = true;
                alphaBandCheckBox.Checked = true;
                betaBandCheckBox.Checked = true;
                gammaBandCheckBox.Checked = true;
            }
            else
            {
                lfBandCheckBox.Checked = true;
                hfBandCheckBox.Checked = true;
            }
        }

        private void deselectAllButton_Click(object sender, EventArgs e)
        {
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                deltaBandCheckBox.Checked = false;
                thetaBandCheckBox.Checked = false;
                alphaBandCheckBox.Checked = false;
                betaBandCheckBox.Checked = false;
                gammaBandCheckBox.Checked = false;
            }
            else
            {
                lfBandCheckBox.Checked = false;
                hfBandCheckBox.Checked = false;
            }
        }

        public string GetSelectedBandsDisplayText()
        {
            var selectedBands = new List<string>();
            
            if (_currentMode == ModeSelectionForm.AnalysisMode.EEG)
            {
                if (EegBandSettings.Delta) selectedBands.Add("δ");
                if (EegBandSettings.Theta) selectedBands.Add("θ");
                if (EegBandSettings.Alpha) selectedBands.Add("α");
                if (EegBandSettings.Beta) selectedBands.Add("β");
                if (EegBandSettings.Gamma) selectedBands.Add("γ");
            }
            else
            {
                if (HeartBandSettings.LF) selectedBands.Add("LF");
                if (HeartBandSettings.HF) selectedBands.Add("HF");
            }

            if (selectedBands.Count == 0)
                return "なし";

            return string.Join(", ", selectedBands);
        }
    }
}