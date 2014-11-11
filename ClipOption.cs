using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace clipclip
{
    public partial class ClipOption : Form
    {
        private OptionInterface frmOption = null;
        FontDialog fontdlg = new FontDialog();

        public ClipOption(OptionInterface frmOption, FontDialog fontdlg, int visibTime, int picWidth, int picHeight)
        {

            InitializeComponent();
            this.frmOption = frmOption;
            this.fontdlg = fontdlg;
            textBox1.Text = "안녕하세요";
            textBox1.ForeColor = fontdlg.Color;
            textBox1.Font = fontdlg.Font;
            this.VisibTimeDomain.Value = visibTime;
            this.picwidthDomain.Value = picWidth;
            this.picheightDomain.Value = picHeight;
            frmOption.i_ShowList(true);
        }

        private void button2_Click(object sender, EventArgs e)  // 취소 버튼
        {
            frmOption.i_ShowList(false);
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)   //글자 편집 버튼
        {
            
            fontdlg.ShowColor = true;
            fontdlg.MaxSize = 30;
            fontdlg.MinSize = 9;

            if (fontdlg.ShowDialog() == DialogResult.OK)
            {
                textBox1.Font = fontdlg.Font;
                textBox1.ForeColor = fontdlg.Color;
            }
            frmOption.i_SetFontStyle(fontdlg);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;  // 택스트박스에 글자 입력 안되기 하기
        }

        private void button1_Click(object sender, EventArgs e)   // 확인 버튼
        {
            frmOption.i_SetFontStyle(fontdlg);
            frmOption.i_SetVisibTime((int)VisibTimeDomain.Value);
            frmOption.i_ChangePicSize((int)this.picwidthDomain.Value, (int)this.picheightDomain.Value);
            frmOption.i_ShowList(false);
            this.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            
            DialogResult myDialog;
            myDialog = MessageBox.Show("정말 모든내용을 지우시겠어요?", "Caption", MessageBoxButtons.YesNo);
            if (myDialog == DialogResult.Yes)
            {
                frmOption.i_AllListClear();
            }

            if (myDialog == DialogResult.No)
            {
               
            }
        }

    



    }
}
