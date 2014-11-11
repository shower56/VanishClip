using System;
using System.IO;
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
using MouseKeyboardLibrary;

namespace clipclip
{
    public partial class VanishClip : Form, OptionInterface
    {

        #region DLL 추가

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern
        IntPtr SetClipboardViewer(IntPtr hWnd);  // 클립보드를 모니터링하는 프로그램의 핸들등록

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern
        bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext); //클립보드를 모니터링하는 프로그램의 체인 구조를 변경

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);  // 클립보드에 메시지를 보낼 함수

        [DllImport("user32.dll")]
        static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);


        #endregion

        #region 맴버 필드

        [StructLayout(LayoutKind.Sequential)]   // 윈도우 폼 구조체 선언
        private struct WINDOWPOS
        {
            public IntPtr hwnd;    // 윈폼을 도킹시킬 프로세스의 핸들

            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }
        

        private NotifyIcon trayIcon = new NotifyIcon();      
        private ContextMenu trayMenu = new ContextMenu();
        
        private IntPtr ClipboardViewerNext;      

        private const Int32 WM_WINDOWPOSCHANGING = 0x0046;  //윈도우 창변경  윈프로시저 메세지
        private const Int32 WM_DRAWCLIPBOARD = 0x308; //클립보드 변경 윈 프로시저 메세지
        private const Int32 WM_CHANGECBCHAIN = 0x30D;  //클립보드 채인 윈 프로시저 메세지

        private const int ClipString = 1;
        private const int ClipBitmap = 2;
        private const int ClipFile = 3;
        private const int ClipObject = 4;

        private Bitmap text = new Bitmap(clipclip.Properties.Resources.text);
        private Bitmap file = new Bitmap(clipclip.Properties.Resources.file);
        private Bitmap error = new Bitmap(clipclip.Properties.Resources.error);

        private Timer timer = new Timer();
        private int counter = 0;

        private int OnVisTime = 3;
        private FontDialog FontData = new FontDialog();
        
        private int picWidth = 50;
        private int picHeight = 50;   
        


        private bool paste = false;
        private bool nofirst = false;
        private bool samelist = false;

        private KeyboardHook keyboardHook = new KeyboardHook();
        
        private List<Cliptolist> ClipList = new List<Cliptolist>();
        
        private ImageList imgList = new ImageList();     // 아이콘 리스트

        

        
        #endregion
        
        #region  생성자

        public VanishClip()
        {

            InitializeComponent();
            InitializeFont();


            imgList.ImageSize = new Size(picWidth, picHeight);   // 리스트에 데이터포멧에 따라 아이콘 등록하기
            listView1.SmallImageList = imgList;
            
            

            imgList.Images.Add(text);
            imgList.Images.Add(file);
            imgList.Images.Add(error);

            
            this.FormBorderStyle = FormBorderStyle.None; //폼 테두리 제거


            trayMenu.MenuItems.Add("사용팁", Help);
            trayMenu.MenuItems.Add("리스트 on/off ( Ctrl+ ` )", list);
            trayMenu.MenuItems.Add("설정", Option);  // 트레이 아이콘으로...
            trayMenu.MenuItems.Add("종료", OnExit);
            
            trayIcon.Text = "VanishClip";
            trayIcon.Icon = new Icon(clipclip.Properties.Resources.vanishclip, 40, 40);

            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            this.Visible = false;   //평소 폼 상태 
            this.TopMost = true;    // 항상 최상위
            

            this.Opacity = 1; //폼 투명도 0~1  0일수록 투명함


        }
        #endregion

        #region  Form_load & Timer_Tick

        private void Form1_Load(object sender, EventArgs e)
        {

            keyboardHook.KeyDown += new KeyEventHandler(keyboardHook_KeyDown); //여기서 훅에 의한 이벤트를 연결시킨다.
            keyboardHook.Start(); // 후킹 시작


            timer.Interval = 1000; //
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start(); //타이머를 발동시킨다.

            ChangeClipboardChain(this.Handle, ClipboardViewerNext);
            ClipboardViewerNext = SetClipboardViewer(this.Handle); // 클립보드  핸들 등록
            ClipList.Clear();
            
            //listView1.HeaderStyle = ColumnHeaderStyle.None;         // 리스트뷰 해더 제거
            
            
            
        }
        void timer_Tick(object sender, EventArgs e)
        {
            
            
            if (counter == OnVisTime) // 창을 보이고 싶은 시간
            {
                this.Visible = false;   //평소 폼 상태 
                timer.Stop();
                counter = 0;
            }
            counter++;
        }
        #endregion


        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        
        
        #region 트레이 아이콘 메뉴

        protected override void OnLoad(EventArgs e)
        {
            //Visible = false; // 윈도우에서 처음엔 보여지지 않게 한다.
            ShowInTaskbar = false; //  taskbar에서 본프로세스를 없앤다.

            base.OnLoad(e);
        }
        private void Help(object sender, EventArgs e)  // 트레이메뉴에서 사용법 확인하기
        {
            String HelpTip = "사용법 \n\n Ctrl + C 또는 Ctrl + X 를 통해 클립보드에 저장된 내용은\n 리스트에 또다시 저장된다. 최대 10개까지 저장되며 \n 리스트의 단축키는 Ctrl + 1 부터 Ctrl + 0 까지 이다. \n 리스트의 목록을 마우스 왼쪽 클릭을 통해도 붙여 넣을 수 있다.\n\n 리스트에 마우스 오른쪽 클릭을 하면 리스트에서 제거된다. \n\n";
            MessageBox.Show(HelpTip);
        }
        private void list(object sender, EventArgs e)  // 컨트롤 틸트와 같은 역할 
        {
            if (this.Visible == true)
                this.Visible = false;
            else if (this.Visible == false)
                this.Visible = true;
        }
        private void Option(object sender, EventArgs e)
        {
            ClipOption frmOption;

            frmOption = new ClipOption(this as OptionInterface, this.FontData, this.OnVisTime, this.picWidth , this.picHeight);
            frmOption.Show();
            GC.Collect();
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }
        #endregion
        
        /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
       #region 인터페이스과 옵션창 데이터 주고 받는 메서드들


        private void InitializeFont()
        {
            this.FontData.Font = new Font("굴림", 9);
            this.FontData.Color = Color.White;
        }
        public void i_ShowList(bool ok)
        {
            if (ok)
                this.Visible = true;
            else
                this.Visible = false;

        }

        public void i_AllListClear()
        {
            listView1.Clear();
            listView1.Update();
            ClipList.Clear();
        }

        public void i_SetVisibTime(int VisibTime)
        {
            SetOnVisTime(VisibTime);
        }

        public void i_SetFontStyle(FontDialog FontData)
        {
            this.FontData = FontData;
            SetFontStyle();
        }
        public void i_ChangePicSize(int picWidth, int picHeight)
        {
            this.picWidth = picWidth;
            this.picHeight = picHeight;

            imgList = ChangeImgList(this.picWidth, this.picHeight);
            listView1.SmallImageList = imgList;
            listView1.Update();
            this.Height = 39 + (ClipList.Count * picHeight);


            GC.Collect();

        }
        private ImageList ChangeImgList(int width, int height)
        {
            ImageList temp = new ImageList();
            temp.ImageSize = new Size(width, height);
            for (int i = 0; i < imgList.Images.Count; i++)
            {
                temp.Images.Add(imgList.Images[i]);
            }

            return temp;
        }


        private void SetFontStyle()
        {
            listView1.Font = FontData.Font;
            listView1.ForeColor = FontData.Color;
        }

        private void SetOnVisTime(int VisibTime)
        {
            OnVisTime = VisibTime;
        }

        #endregion
        
        #region 폼 화면 구석에 붙이기
        private void SetDockWindow(Form form, int dockMargin, ref Message message)
        {

            //현재 Form이 위치한 화면의 작업영역 가져옴(WorkingArea = 작업표시줄을 제외한 영역)
            Rectangle currentDesktopRect = (Screen.FromHandle(form.Handle)).WorkingArea;
            WINDOWPOS winPos = (WINDOWPOS)message.GetLParam(typeof(WINDOWPOS));
          
            this.StartPosition = FormStartPosition.Manual;     // 폼 생성할때 화면 구석에서 생성
            this.Location = new Point(currentDesktopRect.Right - winPos.cx, 0);

            //왼쪽

            if (Math.Abs(winPos.x - currentDesktopRect.Left) <= dockMargin)
            {
                winPos.x = currentDesktopRect.Left;
            }

            //위

            if (Math.Abs(winPos.y - currentDesktopRect.Top) <= dockMargin * 100)   //위쪽으로 무조건 붙이자  *100
            {
                winPos.y = currentDesktopRect.Top;
            }



            //오른쪽

            if (Math.Abs(winPos.x + winPos.cx - currentDesktopRect.Left - currentDesktopRect.Width) <= dockMargin * 100)  //오른쪽으로 무조건 붙이자 *100
            {
                winPos.x = currentDesktopRect.Right - winPos.cx;
            }


            //아래

            if (Math.Abs(winPos.y + winPos.cy - currentDesktopRect.Top - currentDesktopRect.Height) <= dockMargin)
            {
                winPos.y = currentDesktopRect.Bottom - form.Bounds.Height;
            }

            Marshal.StructureToPtr(winPos, message.LParam, false);

            message.Result = (IntPtr)0;

        }
        #endregion

        #region 윈도우 프로시저 메시지

        protected override void WndProc(ref Message m)
        {


            switch (m.Msg)
            {

                case WM_DRAWCLIPBOARD:  //클립보드 변경 될때
                    if (paste == true)break;
                    
                    Cliptolist clip = new Cliptolist();   // 클립보드에 들어온 새로운 객체
                    ListViewItem li = new ListViewItem();
                    if (this.OnVisTime != 0)
                    {
                        this.Visible = true;   //클립보드 변경되었을때 폼 보이기
                        timer.Start();
                    }

                    #region  리스트 중복 검사

                    if (clip.Get_CheckType() == ClipFile)      // 같은 파일이 있을때
                    {
                        for (int i = 0; i < ClipList.Count; i++) 
                        {
                            if (ClipList[i].Get_CheckType() == ClipFile)
                            {
                                if (clip.GetFileName() == ClipList[i].GetFileName())
                                {
                                    samelist = true;
                                    break;
                                }
                            }
                        }
                    }
                    else if (clip.Get_CheckType() == ClipString)      // 같은 스트링 있을때
                    {
                        for (int i = 0; i < ClipList.Count; i++)
                        {
                            if (ClipList[i].Get_CheckType() == ClipString)
                            {
                                if (clip.GetClipText().Equals(ClipList[i].GetClipText()))
                                {
                                    samelist = true;
                                    break;
                                }
                            }
                        }
                    }

                    else if (clip.Get_CheckType() == ClipBitmap)         //같은 이미지 있을 때
                    {
                        for (int i = 0; i < ClipList.Count; i++)
                        {
                            if (ClipList[i].Get_CheckType() == ClipBitmap)
                            {
                                if (clip.GetImageSize().Equals(ClipList[i].GetImageSize()))
                                {
                                    samelist = true;
                                    break;
                                }
                            }
                        }
                    }
                    else if (clip.Get_CheckType() == ClipObject)   // 같은 else가 있을 때
                    {
                        for (int i = 0; i < ClipList.Count; i++)
                        {
                            if (ClipList[i].Get_CheckType() == ClipObject)
                            {
                                if (clip.GetClipObject().Equals(ClipList[i].GetClipObject()))
                                {
                                    samelist = true;
                                    break;
                                }
                            }

                        }
                    }

                    #endregion

                    if (samelist == false)     // 클립보드내용과 리스트내용이 같은것이 없을 때
                    {
                        if (ClipList.Count> 9)  //리스트 내용이 10개가 되면 첫번째 인덱스부터 밀어넣기  
                        {                            
                            listView1.Items.RemoveAt(0);
                            listView1.Update();
                            ClipList.RemoveAt(0);
                        }
                        
                            
                        ClipList.Add(clip); // 클립보드내용 리스트에추가

                        // 아래 부터 중요한 부분임 

                        if (nofirst == true)       //  프로그램 시작시 클립보드에 있던내용 표시 제거 및 클립리스트에 데이터포멧에 따라 리스트에 데이터 표시
                        {
                            if(ClipList.Count <10)
                                this.Height = this.Height + this.picHeight;  //데이터가 새로 들어오면 리스트폼 height늘리기
                            if (clip.Get_CheckType() == ClipString)  // 새로 들어온 리스트의 데이터가 텍스트일때
                            {
                                li.Text = clip.GetClipText();   // 리스트에 표시될 텍스트를 할당한다.
                                li.ImageIndex = 0;   //0 은 텍스트라는 이미지인덱스
                            }
                            else if (clip.Get_CheckType() == ClipBitmap)  // 새로 들어온 리스트의 데이터가 이미지일때
                            {
                                li.Text = "[IMAGE] (" + clip.GetClipImage().Width + " X "+clip.GetClipImage().Height +")" ; // 리스트의 텍스트에는 이미지의 크기를 보여준다.
                                imgList.Images.Add((Image)clip.GetClipImage());// 그림이 들어오면 이미지인덱스에는 그 해당 이미지가 표시
                                li.ImageIndex = imgList.Images.Count-1;  //  

                            }
                            else if (clip.Get_CheckType() == ClipFile)  // 새로 들어온 리스트의 데이터가 파일
                            {
                                li.Text = clip.GetFileName();  // 해당 데이터의 파일이름 가져오기
                                li.ImageIndex = 1;  // 해당 데이터의 파일 인덱스는 파일이라는 것
                            }
                            else  // 텍스트도 이미지도 파일도 아닐때 
                            {
                                li.Text = "저장 되었습니다.";    
                                li.ImageIndex = 2;   // 이미지 인덱스는 else
                            }

                            //li.SubItems.Add("Ctrl + " + (ClipList.IndexOf(clip) + 1));
                            listView1.Items.Add(li);   // 리스트폼에 새로운데이터 추가
                           
                            
                            clip = null; 
                        }
                        nofirst = true;
                    }
                    samelist = false;

                    SendMessage(ClipboardViewerNext, m.Msg, m.WParam, m.LParam);
                    break;
                case WM_CHANGECBCHAIN:   //  클립보드 체인
                    if (m.WParam == ClipboardViewerNext)
                    {
                        ClipboardViewerNext = m.LParam;
                    }
                    else
                    {
                        SendMessage(ClipboardViewerNext, m.Msg, m.WParam, m.LParam);
                    }
                    break;

                case WM_WINDOWPOSCHANGING:    // 폼 무브 시

                    SetDockWindow(this, 25, ref m);

                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        #endregion

        #region Ctrl + 1~0 키로서 붙여 넣기 하는부분

        void keyboardHook_KeyDown(object sender, KeyEventArgs e)
        {
            
                
            if (e.Modifiers == Keys.Control)  //컨트롤을 눌른 상태에서
            {
                /*
                if (e.KeyCode == Keys.E)  // 전역키 이벤트가 먹는지 검사
                {
                    MessageBox.Show("SDF");
                }
                */

                if (e.KeyCode == Keys.D1)   // 각 키가 눌러지면 인덱스의 데이터를 붙여넣기 하자
                {

                    try
                    {
                        keybd_event((byte)Keys.D1, 0, 0x02, 0);
                        ClipPasteOnKey(0);

                    }
                    catch (Exception)
                    {
                        paste = false;
                    }
                }
                else if (e.KeyCode == Keys.D2)
                {
                    try
                    {
                        keybd_event((byte)Keys.D3, 0, 0x02, 0);
                        ClipPasteOnKey(1);
                    }
                    catch (Exception)
                    {
                        paste = false;
                    }
                }
                else if (e.KeyCode == Keys.D3)
                {
                    try
                    {
                        keybd_event((byte)Keys.D3, 0, 0x02, 0);
                        ClipPasteOnKey(2);
                    }
                    catch (Exception)
                    {
                        paste = false;
                    }
                }
                else if (e.KeyCode == Keys.D4)
                {
                    try
                    {
                        keybd_event((byte)Keys.D4, 0, 0x02, 0);
                        ClipPasteOnKey(3);
                    }
                    catch (Exception)
                    {
                        paste = false;
                    }
                }
                else if (e.KeyCode == Keys.D5)
                {
                    try
                    {
                        keybd_event((byte)Keys.D5, 0, 0x02, 0);
                        ClipPasteOnKey(4);
                    }
                    catch (Exception)
                    {
                        paste = false;
                    }
                } 
                else if (e.KeyCode == Keys.D6)
                {
                    try
                    {
                        keybd_event((byte)Keys.D6, 0, 0x02, 0);
                        ClipPasteOnKey(5);
                    }
                    catch (Exception)
                    {
                        paste = false;
                    }
                } 
                else if (e.KeyCode == Keys.D7)
                {
                    try
                    {
                        keybd_event((byte)Keys.D7, 0, 0x02, 0);
                        ClipPasteOnKey(6);
                    }
                    catch (Exception)
                    {
                        paste = false;
                    }
                } 
                else if (e.KeyCode == Keys.D8)
                {
                    try
                    {
                        keybd_event((byte)Keys.D8, 0, 0x02, 0);
                        ClipPasteOnKey(7);
                    }
                    catch (Exception)
                    {
                        paste = false;
                    }
                }
                else if (e.KeyCode == Keys.D9)
                {
                    try
                    {
                        keybd_event((byte)Keys.D9, 0, 0x02, 0);
                        ClipPasteOnKey(8);
                    }
                    catch (Exception)
                    {
                        paste = false;
                    }
                }
                if (e.KeyCode == Keys.D0)
                {
                    try
                    {
                        keybd_event((byte)Keys.D0, 0, 0x02, 0);
                        ClipPasteOnKey(9);
                    }
                    catch (Exception)
                    {
                        paste = false;
                    }
                }


            }
            if (e.Modifiers == Keys.Control)
            {
                if (e.KeyCode == Keys.Oemtilde)  //컨트롤 틸트키를 눌렀을때 리스트폼 보여주거나 없애기
                {

                    if (this.Visible == true)
                        this.Visible = false;
                    else if (this.Visible == false)
                        this.Visible = true;

                }
            }
        }
        //단축키가 전역훅으로 설정되어 있어 어느 프로세스간에도 키이벤트를 발생시킬수 있다.
        private void ClipPasteOnKey(int index)   // 단축키로 리스트의 데이터 붙여넣기 index는 키단축키의 선택자
        {

            paste = true;    //  붙여넣기 할때에도 클립보드로 들어가기 때문에 WndProc함수에 걸릴수 있으므로 예외처리를 위한 flag


            if (ClipList[index].GetClipText() != null)         // 리스트의 해당 인덱스의 데이터를 클립보드로 집어넣음 text,image,File,else를 가려내어 클리보드에 입력
                Clipboard.SetText(ClipList[index].GetClipText());
            else if (ClipList[index].GetClipImage() != null)
                Clipboard.SetImage(ClipList[index].GetClipImage());
            else if  (ClipList[index].GetClipFile() != null)
                Clipboard.SetFileDropList(ClipList[index].GetClipFile());
            else  if (ClipList[index].GetClipObject() != null)
                Clipboard.SetDataObject(ClipList[index].GetClipObject());
            else
                return;
                
            
           
            keybd_event((byte)Keys.V, 0, 0, 0);          //  Control이 눌러진상태이고 강제 V키 던져서 Ctrl + V  실행  SendMessage로 할려고 했으나 ㅜ 복사할 프로세스의 
            //System.Threading.Thread.Sleep(10);

          
            paste = false;
            
        }
        private void ClipPasteOnClick(int index)
        {

            paste = true;

            if (ClipList[index].GetClipText() != null)         // 리스트의 해당 인덱스의 데이터를 클립보드로 집어넣음 text,image,File,else를 가려내어 클리보드에 입력
                Clipboard.SetText(ClipList[index].GetClipText());
            else if (ClipList[index].GetClipImage() != null)
                Clipboard.SetImage(ClipList[index].GetClipImage());
            else if (ClipList[index].GetClipFile() != null)
                Clipboard.SetFileDropList(ClipList[index].GetClipFile());
            else if (ClipList[index].GetClipObject() != null)
                Clipboard.SetDataObject(ClipList[index].GetClipObject());
            else
                return;

            keybd_event((byte)Keys.LControlKey, 0, 0, 0);  //붙여넣기 하는부분,,, 완전 발로 짠듯한..( SendMessage로 붙여넣기 메시지를 발생 시키려 헀으나 붙여넣기 할 프로세스의 핸들찾기가 시간부족 결국 모로가도 서울로...)
            keybd_event((byte)Keys.V, 0, 0, 0);
            
            keybd_event((byte)Keys.V, 0, 0x02, 0);
            keybd_event((byte)Keys.LControlKey, 0, 0x02, 0);
               

            /*      단축키를 활용하여 리스트의 데이터를 붙여넣을 때 기존 클립보드의 내용을 살리려는 노력 실패....
             Cliptolist temp = new Cliptolist();
             if (temp.Get_CheckType() == ClipString)
                 Clipboard.SetText(temp.GetClipString());
             else if (temp.Get_CheckType() == ClipBitmap)
                 Clipboard.SetImage(temp.GetClipImage());
             else if (temp.Get_CheckType() == ClipFile)
                 Clipboard.SetFileDropList(temp.GetClipFile());
             temp = null;
             */
            paste = false;

        }
        #endregion

        #region 마우스 클릭 (왼쪽 : 해당 리스트 붙여 넣기, 오른쪽 : 해당 리스트 삭제)

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)    // ㅜㅜ 텝으로  리스트폼에서 붙여넣기 할 폼으로 포커스 이동 ㅜㅜ( 이전활성화된 프로세스를 못찾겟다 )
            {
                keybd_event((byte)Keys.LMenu, 0, 0, 0);
                System.Threading.Thread.Sleep(10);
                keybd_event((byte)Keys.Tab, 0, 0, 0);
                System.Threading.Thread.Sleep(10);
                keybd_event((byte)Keys.Tab, 0, 0x02, 0);
                System.Threading.Thread.Sleep(10);
                keybd_event((byte)Keys.LMenu, 0, 0x02, 0);
                System.Threading.Thread.Sleep(40);

                ClipPasteOnClick(listView1.FocusedItem.Index);
            }
            if (e.Button == MouseButtons.Right)    // 리스트의 해당 데이터 삭제
            {
                try
                {
                    if (listView1.FocusedItem.Index == listView1.Items.Count - 1) //마지막 데이터를 삭제하기
                    {
                        listView1.Items.RemoveAt(listView1.FocusedItem.Index);
                        ClipList.RemoveAt(ClipList.Count - 1);
                        listView1.Update();

                    }
                    else
                    {
                        listView1.Items.RemoveAt(listView1.FocusedItem.Index);  // 마지막이 아닌 선택한 데이터를 삭제하기

                        ClipList.RemoveAt(listView1.FocusedItem.Index);
                        listView1.Update();

                    }
                    this.Height = this.Height - this.picHeight;  //삭제후 리스트폼 height 축소하기
                    GC.Collect();
                }
                catch (NullReferenceException)
                {
                    ClipList.RemoveAt(0);
                }
            }
        }
        #endregion


    }

    
}
