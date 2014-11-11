using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace clipclip
{
    public interface OptionInterface
    {
        void i_SetFontStyle(FontDialog fontdata);
        void i_SetVisibTime(int VisibTime);
        void i_AllListClear();
        void i_ChangePicSize(int picWidth, int picHeight);
        void i_ShowList(bool ok);
    }
}
