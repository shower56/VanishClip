using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Specialized;


namespace clipclip
{
    class Cliptolist   // 클립으로 들어가는 데이터에 관한 클래스
    {
        private String str = null;
        private Image img = null;
        private StringCollection file = null;
        private Object obj = null;
        private int checkType = 0;

        public Cliptolist()  // Cliptolist 클래스의 생성자에서 클립보드의 객체를 파싱하여 해당 포멧으로 검출
        {
            
            if (Clipboard.ContainsText())
            {
                str = Clipboard.GetText();                              
                checkType = 1;
            }
            else if (Clipboard.ContainsImage())
            {
                img = Clipboard.GetImage();
                checkType = 2;
            }
            else if (Clipboard.ContainsFileDropList())
            { 
                file = Clipboard.GetFileDropList();
                checkType = 3;                
            }
            else 
            {
                obj = Clipboard.GetDataObject();
                checkType = 4; 
            }

        }
        public Object GetClipObject()
        {
            return obj;
        }
        public int Get_CheckType()
        {
            return checkType;
        }
        public String GetClipText()
        {
            return str;
        }
        public Image GetClipImage()
        {
            return img;
        }
        public StringCollection GetClipFile()
        {
            return file;
        }
        public String GetFileName()
        {
            StringBuilder result = new StringBuilder();

            foreach (String files in file)
            {
                result.Append(files);
                result.AppendLine();
            }

            return result.ToString();
        }

        public Size GetImageSize()
        {
            return img.Size;
        }

        
    }
}
