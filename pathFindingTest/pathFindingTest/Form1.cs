using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace pathFindingTest
{
    public partial class Form1 : Form
    {
        public static int listViewLastIndex = 0;
        public static string fullpath;
        public static string lastKey = "";
        int midX = 960, midY = 505;
        public static bool bool_enemy = false;

        Action mainSkillDown,mainSkillUp;
        Action[] pressKeyList,releaseKeyList;
        List<int> buffList = new List<int>();

        DateTime lastMapTime = DateTime.Now;

        ViGEmClient client;
        Nefarius.ViGEm.Client.Targets.IDualShock4Controller ds4;
        Random rand;
        VirtualMouse.POINT pt = new VirtualMouse.POINT();
        [DllImport("user32.dll")]
        private static extern int RegisterHotKey(IntPtr hwnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")]
        public static extern bool InjectKeyboardInput(ref tagKEYBDINPUT input, uint count);

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);

        
        [DllImport("user32.dll")] 
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32")]public static extern IntPtr GetForegroundWindow();

        [DllImport("user32")]
        public static extern Boolean ShowWindow(IntPtr hWnd, Int32 nCmdShow);


        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo,bool fAttach);
        [DllImport("user32.dll", SetLastError=true)]
        static extern bool BringWindowToTop(IntPtr hWnd);
        


        public struct tagKEYBDINPUT
        {
            /// <summary>
            /// A virtual-key code.
            /// The code must be a value in the range 1 to 254.
            /// If the dwFlags member specifies KEYEVENTF_UNICODE, wVk must be 0.
            /// </summary>
            public ushort wVk; // 1 - 254

            /// <summary>
            /// A hardware scan code for the key.
            /// If dwFlags specifies KEYEVENTF_UNICODE, wScan specifies a Unicode character which is to be sent to the foreground application.
            /// </summary>
            public ushort wScan;

            /// <summary>
            /// Specifies various aspects of a keystroke.
            /// </summary>
            public uint dwFlags;

            /// <summary>
            /// The time stamp for the event, in milliseconds.
            /// If this parameter is zero, the system will provide its own time stamp.
            /// </summary>
            public uint time;

            /// <summary>
            /// An additional value associated with the keystroke.
            /// Use the GetMessageExtraInfo function to obtain this information.
            /// </summary>
            public ulong dwExtraInfo;
        }
        tagKEYBDINPUT keyboard;
        Bitmap lastMap = null;
        Bitmap bitmap_pin;
        Bitmap[] bitmap_Maps;
        Bitmap bitmap_die,bitmap_die2;

        Thread thread_적찾기;
        Thread thread_메인;
        
        Rectangle ROI_Map = new Rectangle(1608,49,286,206);
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312)
            {
                switch (m.WParam.ToInt32())
                {
                    case 1:
                    case 1001:
                    case 2001:
                        {
                            

                            if (thread_메인.ThreadState != System.Threading.ThreadState.Running)
                            {
                                thread_메인 = new Thread(new ThreadStart(main));
                                thread_메인.Start();
                                thread_적찾기 = new Thread(new ThreadStart(autoEnemy));
                                //thread_적찾기.Start();
                                
                            }

                        }
                        break;

                    case 2:
                    case 1002:
                    case 2002:
                        {
                            

                            try
                            {
                                thread_메인.Abort();
                            }
                            catch { }
                            try
                            {
                                thread_적찾기.Abort();
                            }
                            catch { }
                           
                            ReleaseAllKey();

                            {
                                Process[] processes = Process.GetProcesses();

                                foreach(Process process in processes)
                                {
                                    if(process.MainWindowTitle.Contains("GeForce NOW 디아블") || process.MainWindowTitle.Contains(" IV"))
                                    {
                                        try
                                        {
                                            SetWindowPos(process.MainWindowHandle, new IntPtr(-2), 0, 0, 0, 0, 0x3);
                                        }
                                        catch { }
                                    }
                                }

                    

                            }       // 화면 제일 앞으로

                            
                        }
                        break;


                    default:
                        break;

                }
            }
        }



        public Form1()
        {
            keyboard = new tagKEYBDINPUT();
            keyboard.dwFlags = 0;

            InitializeComponent();
            Form1.fullpath = Application.StartupPath + "\\";
            bitmap_Maps = new Bitmap[] {new Bitmap(fullpath+"img\\케지.jpg") };


            rand = new Random();
            bitmap_pin = new Bitmap(fullpath+"img\\pin.png");
            bitmap_die = new Bitmap(fullpath+"img\\죽음.png");
            bitmap_die2 = new Bitmap(fullpath+"img\\죽음2.png");
            {
                try { client = new ViGEmClient(); } catch { }
                try { ds4 = client.CreateDualShock4Controller(); } catch { }
                try
                {
                    ds4.AutoSubmitReport = false;
                }
                catch { }
                try
                {
                    ds4.Disconnect();

                }
                catch { }
                try
                {
                    ds4.Dispose();

                }
                catch { }
                try
                {
                    client.Dispose();

                }
                catch { }

                thread_메인 = new Thread(new ThreadStart(main));
                thread_메인.Abort();
                thread_적찾기 = new Thread(new ThreadStart(autoEnemy));
                thread_적찾기.Abort();
            }

            client = new ViGEmClient();
            ds4 = client.CreateDualShock4Controller();

            ds4.Connect();
            ds4.AutoSubmitReport = true;

            this.pictureBox1.Image = new Bitmap(Form1.fullpath +"img\\키설명.jpg");

            pressKeyList = new Action[]{
                ()=>ds4.SetButtonState(DualShock4Button.Triangle,true),
                ()=>ds4.SetButtonState(DualShock4Button.ShoulderRight,true),
                () => ds4.SetSliderValue(DualShock4Slider.LeftTrigger, 255),
                () => ds4.SetSliderValue(DualShock4Slider.RightTrigger, 255),
                ()=>ds4.SetButtonState(DualShock4Button.Cross,true),
                ()=>ds4.SetButtonState(DualShock4Button.Square,true),
                ()=>ds4.SetButtonState(DualShock4Button.Circle,true)
                };

            releaseKeyList = new Action[]{
                ()=>ds4.SetButtonState(DualShock4Button.Triangle,false),
                ()=>ds4.SetButtonState(DualShock4Button.ShoulderRight,false),
                () => ds4.SetSliderValue(DualShock4Slider.LeftTrigger, 0),
                () => ds4.SetSliderValue(DualShock4Slider.RightTrigger, 0),
                ()=>ds4.SetButtonState(DualShock4Button.Cross,false),
                ()=>ds4.SetButtonState(DualShock4Button.Square,false),
                ()=>ds4.SetButtonState(DualShock4Button.Circle,false)
                };


            comboBox1.SelectedIndex = 0;

        }

        private Bitmap getBmp()
        {
            while (true)
            {
                try
                {
                    System.Drawing.Size size = new System.Drawing.Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);


                    Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height,System.Drawing.Imaging.PixelFormat.Format24bppRgb);


                    Graphics g = Graphics.FromImage(bmp);


                    g.CopyFromScreen(0, 0, 0, 0, size);

                    return bmp;
                }
                catch { Delay(1); }
                
            }
        }

        private void main()
        {
            
            lastMapTime = DateTime.Now.AddSeconds(15);
            try
            {
                lastMap.Dispose();
                lastMap = null;
            }
            catch { }


            List<VirtualMouse.POINT> mapPoints = new List<VirtualMouse.POINT>();

            {
                System.Windows.Forms.CheckBox[] temp = new System.Windows.Forms.CheckBox[] {checkBox1,checkBox2,checkBox3,checkBox4,checkBox5,checkBox6,checkBox7};
                System.Windows.Forms.RadioButton[] temp2 = new System.Windows.Forms.RadioButton[] {radioButton1,radioButton2,radioButton3,radioButton4,radioButton5,radioButton6,radioButton7};
                System.Windows.Forms.Button[] temp3 = new System.Windows.Forms.Button[]{ button3,button4,button5,button6,button7};

                
                buffList.Clear();
                for(int i = 0 ; i < temp.Length; i++) {
                    if (temp2[i].Checked)
                    {
                        mainSkillDown = pressKeyList[i];
                        mainSkillUp = releaseKeyList[i];
                        continue;
                    }
                    if (temp[i].Checked)
                    {
                        buffList.Add(i);
                    }
                }

                foreach(var asdfg in temp3)
                {
                    var kkk = asdfg.Text.Split(',');

                    if(kkk.Length == 1)
                    {
                        continue;
                    }
                    else
                    {
                        
                        VirtualMouse.POINT e = new VirtualMouse.POINT();
                        e.X = int.Parse(kkk[0]);
                        e.Y = int.Parse(kkk[1]);;
                        mapPoints.Add(e);
                    }

                }
            }

            if(mapPoints.Count == 0)
            {
                this.Invoke(new MethodInvoker(delegate() 
                    { 
                        MessageBox.Show("루트를 정해주세요.");
                    })); 
        
            }

            for (int i = 0 ; i < mapPoints.Count ; i = (i+1)%mapPoints.Count)
            {
                {
                    Process[] processes = Process.GetProcesses();

                    foreach(Process process in processes)
                    {
                        if(process.MainWindowTitle.Contains("GeForce NOW 디아블") || process.MainWindowTitle.Contains(" IV"))
                        {
                            try
                            {
                                SetWindowPos(process.MainWindowHandle, new IntPtr(-1), 0, 0, 0, 0, 0x3);
                                SetForegroundWindow(process.MainWindowHandle);
                                SetForegroundWindow(process.MainWindowHandle);
                            }
                            catch { }
                        }
                    }

                    

                }       // 화면 제일 앞으로

                ds4.SetButtonState(DualShock4Button.ThumbRight, true);
                using(Bitmap b = getBmp())
                {
                    using(Bitmap bmp = cropImage(b, new Rectangle(821, 952, 45, 45)))
                    {
                        if((searchIMG(bmp, bitmap_die,out _) > 0.8)||(searchIMG(bmp, bitmap_die2,out _) > 0.8))  // 죽음
                        {
                            for(int j = 0 ; j < 5; j++)
                            {
                                pressKeyList[0]();
                                Delay(30);
                                releaseKeyList[0]();
                                Delay(30);
                            }
                        }
                    }



                    
                    
                    using(Bitmap bmp = cropImage(b, ROI_Map))
                    {
                        stuckCheck(bmp);
                        if (searchIMG(bmp,bitmap_pin,out pt) > 0.5)
                        {
                            // 130, 80 이 가운데
                            int diff = (int)(Math.Sqrt(Math.Pow(130-pt.X,2) + Math.Pow(80-pt.Y,2) ));

                            if(diff < 30)       //도착이라고 판정
                            {
                                Debug.WriteLine("도착이라고 판정 2");
                                ReleaseAllKey();
                                {
                                    keyboard.wScan = 23; // VK_I
                                    for(int j = 0; j < 6; j++)
                                    {
                                        keyboard.dwFlags = 8;
                                        InjectKeyboardInput(ref keyboard, 1);
                                        randomDelay(10,20);
                                        keyboard.dwFlags = 10;
                                        InjectKeyboardInput(ref keyboard, 1);
                                        randomDelay(10,20);
                                    }
                                    keyboard.wScan = 15;    // VK_TAB
                                    keyboard.dwFlags = 8;
                                    InjectKeyboardInput(ref keyboard, 1);
                                    randomDelay(10,20);
                                    keyboard.dwFlags = 10;
                                    InjectKeyboardInput(ref keyboard, 1);
                                    randomDelay(10,20);

                                }
                                Delay(300);
                                for(int scroll = 0 ; scroll < 5; scroll++)
                                {
                                    VirtualMouse.Scroll(-120);
                                    randomDelay(30,50);
                                }
                                {
                                    keyboard.wScan = 57;    // VK_SPACE
                                    keyboard.dwFlags = 8;
                                    InjectKeyboardInput(ref keyboard, 1);
                                    randomDelay(10,20);
                                    keyboard.dwFlags = 10;
                                    InjectKeyboardInput(ref keyboard, 1);
                                    randomDelay(10,20);
                                }
                                VirtualMouse.MoveTo(1,1,5);
                                Delay(300);
                                
                        
                                using(Bitmap c = getBmp())
                                {
                                    if (searchIMG(c, bitmap_Maps[0],out pt) > 0.8)
                                    {
                                        VirtualMouse.MoveTo(pt.X + mapPoints[i].X, pt.Y + mapPoints[i].Y, 5);
                                        VirtualMouse.RightClick();
                                    }
                                }
                                randomDelay(30,50);
                                {
                                    keyboard.wScan = 15;    // VK_TAB
                                    keyboard.dwFlags = 8;
                                    InjectKeyboardInput(ref keyboard, 1);
                                    randomDelay(10,20);
                                    keyboard.dwFlags = 10;
                                    InjectKeyboardInput(ref keyboard, 1);
                                    randomDelay(10,20);
                                }
                                Delay(800);

                                continue;
                            }
                        }
                    }
                    if (pathFinding(b))     //도착 이라고 판정
                    {
                        Debug.WriteLine("도착이라고 판정 1");
                        ReleaseAllKey();
                        {
                            keyboard.wScan = 23; // VK_I
                            for(int j = 0; j < 6; j++)
                            {
                                keyboard.dwFlags = 8;
                                InjectKeyboardInput(ref keyboard, 1);
                                randomDelay(10,20);
                                keyboard.dwFlags = 10;
                                InjectKeyboardInput(ref keyboard, 1);
                                randomDelay(10,20);
                            }
                            keyboard.wScan = 15;    // VK_TAB
                            keyboard.dwFlags = 8;
                            InjectKeyboardInput(ref keyboard, 1);
                            randomDelay(10,20);
                            keyboard.dwFlags = 10;
                            InjectKeyboardInput(ref keyboard, 1);
                            randomDelay(10,20);

                        }
                        
                            
                        Delay(300);

                        for(int scroll = 0 ; scroll < 5; scroll++)
                        {
                            VirtualMouse.Scroll(-120);
                            randomDelay(30,50);
                        }
                        
                        {
                            keyboard.wScan = 57;    // VK_SPACE
                            keyboard.dwFlags = 8;
                            InjectKeyboardInput(ref keyboard, 1);
                            randomDelay(10,20);
                            keyboard.dwFlags = 10;
                            InjectKeyboardInput(ref keyboard, 1);
                            randomDelay(10,20);
                        }

                        VirtualMouse.MoveTo(1,1,5);
                        Delay(300);

                        
                        
                        
                        using(Bitmap bmp = getBmp())
                        {
                            if (searchIMG(bmp, bitmap_Maps[0],out pt) > 0.8)
                            {
                                VirtualMouse.MoveTo(pt.X + mapPoints[i].X, pt.Y + mapPoints[i].Y, 5);
                                VirtualMouse.RightClick();
                            }
                        }
                        randomDelay(30,50);
                        { 
                            keyboard.wScan = 15;    // VK_TAB
                            keyboard.dwFlags = 8;
                            InjectKeyboardInput(ref keyboard, 1);
                            randomDelay(10,20);
                            keyboard.dwFlags = 10;
                            InjectKeyboardInput(ref keyboard, 1);
                            randomDelay(10,20);    
                        }
                        Delay(800);

                        continue;
                    }
                    

                }
                

                ds4.SetButtonState(DualShock4Button.ThumbRight, false);
            }
        }

        private void forceForegroundWindow(IntPtr ptr)
        {
            uint windowThreadProcessId = GetWindowThreadProcessId(GetForegroundWindow(),IntPtr.Zero);
            int currentThreadId = Thread.CurrentThread.ManagedThreadId;
            int CONST_SW_SHOW = 5;
            AttachThreadInput(windowThreadProcessId, (uint)currentThreadId, true);
            BringWindowToTop(ptr);
            ShowWindow(ptr, CONST_SW_SHOW);
            AttachThreadInput(windowThreadProcessId,(uint)currentThreadId, false);
        }


        private bool stuckCheck(Bitmap bitmapMap)
        {
            if(lastMap == null)
            {
                lastMap = new Bitmap(bitmapMap);
                lastMapTime = DateTime.Now.AddSeconds(15);
                return false;
            }

            if(searchIMG(lastMap,bitmapMap,out _) > 0.8)
            {
                if(lastMapTime < DateTime.Now)
                {
                    mainSkillDown();

                    for(int i = 0; i < buffList.Count; i++)
                    {
                        int fdafda = buffList[i];

                        pressKeyList[fdafda]();
                        Delay(rand.Next(30,50));
                    }


                    ReleaseAllKey();
                    ds4.SetDPadDirection(DualShock4DPadDirection.South);
                    Delay(rand.Next(30,50));
                    ds4.SetDPadDirection(DualShock4DPadDirection.None);
                    Delay(6000);

                    return true;
                }
                else
                {
                    return false;
                }

            }

            lastMap.Dispose();
            lastMap = new Bitmap(bitmapMap);
            lastMapTime = DateTime.Now.AddSeconds(15);

            return false;
        }
        
        public double searchIMG(Bitmap screen_img, Bitmap find_img, out VirtualMouse.POINT pt)
        {
            //스크린 이미지 선언
            using (Mat ScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screen_img))
            //찾을 이미지 선언
            using (Mat FindMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(find_img))
            //스크린 이미지에서 FindMat 이미지를 찾아라
            using (Mat res = ScreenMat.MatchTemplate(FindMat, TemplateMatchModes.CCoeffNormed))
            {
                //찾은 이미지의 유사도를 담을 더블형 최대 최소 값을 선언합니다.
                double minval, maxval = 0;
                //찾은 이미지의 위치를 담을 포인트형을 선업합니다.
                OpenCvSharp.Point minloc, maxloc;
                //찾은 이미지의 유사도 및 위치 값을 받습니다. 
                Cv2.MinMaxLoc(res, out minval, out maxval, out minloc, out maxloc);
                // Debug.WriteLine("찾은 이미지의 유사도 : " + maxval);


                pt.X = maxloc.X;
                pt.Y = maxloc.Y;

                return maxval;
            }
        }



        public bool isPink(byte r, byte g, byte b)
        {
            try
            {
                double h,s,v;
                rgb2hsv(r,g,b,out h,out s,out v);
                if((h > 304) ||(h < 294))
                {
                    return false;
                }

                if(s < 0.7)
                {
                    return false;
                }
                if(v < 0.7)
                {
                    return false;
                }
                
                return true;

            }
            catch { }

            return false;
        }

        public void rgb2hsv(int r,int g,int b,out double h, out double s, out double v)
        {
            double      min, max, delta;

            min = r < g ? r : g;
            min = min  < b ? min  : b;

            max = r > g ? r : g;
            max = max  > b ? max  : b;

            v = max;                                // v
            delta = max - min;
            if (delta < 0.00001)
            {
                s = 0;
                h = 0; // undefined, maybe nan?
                v /= 255;
                return;
            }
            if( max > 0.0 ) { // NOTE: if Max is == 0, this divide would cause a crash
                s = (delta / max);                  // s
            } else {
                // if max is 0, then r = g = b = 0              
                // s = 0, h is undefined
                s = 0.0;
                h = 0.0;                            // its now undefined
                v /= 255;
                return;
            }
            if( r >= max )                           // > is bogus, just keeps compilor happy
                h = ( g - b ) / delta;        // between yellow & magenta
            else
            if( g >= max )
                h = 2.0 + ( b - r ) / delta;  // between cyan & yellow
            else
                h = 4.0 + ( r - g ) / delta;  // between magenta & cyan

            h *= 60.0;                              // degrees

            if( h < 0.0 )
                h += 360.0;

            v /= 255;
            return;
        }

        [HandleProcessCorruptedStateExceptions]
        private unsafe bool pathFinding(Bitmap b)
        {
            
            BitmapData bitmapData = null;
            
            try
            {
                unsafe
                {
                    bitmapData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, b.PixelFormat);
                    int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(b.PixelFormat) / 8;
                    byte* PtrFirstPixel = (byte*)bitmapData.Scan0;
                    int bitmapStride = bitmapData.Stride;


                    bool tempBool = findEnemyHP(bitmapData, 804, 809, 47, 47, PtrFirstPixel, bytesPerPixel, bitmapStride);
                    if (tempBool)
                    {
                        ds4.SetAxisValue(DualShock4Axis.LeftThumbX, 127);
                        ds4.SetAxisValue(DualShock4Axis.LeftThumbY, 127);       // 일단 멈춰!
                        ds4.SetButtonState(DualShock4Button.ThumbRight, true);
                        mainSkillDown();
                        if (bool_enemy)
                        {

                            for(int i = 0; i < buffList.Count; i++)
                            {
                                int fdafda = buffList[i];

                                pressKeyList[fdafda]();
                                Delay(rand.Next(30,50));
                                releaseKeyList[fdafda]();
                                Delay(rand.Next(30,50));
                            }

                        }
                        else
                        {
                            for(int i = 0; i < buffList.Count; i++)
                            {
                                int fdafda = buffList[i];

                                pressKeyList[fdafda]();
                                Delay(rand.Next(30,50));
                                releaseKeyList[fdafda]();
                                Delay(rand.Next(30,50));
                            }
                            
                            bool_enemy = true;
                        }
                        b.UnlockBits(bitmapData);
                        mainSkillUp();
                        ds4.SetButtonState(DualShock4Button.ThumbRight, false);
                        return false;
                    }
                    else
                    {
                        if (bool_enemy)
                        {
                            //주 스킬 업 업!
                            bool_enemy = false;
                        }
                    }

                    for (int y = 150; y <= 800; y++)         // x 범위 :(755-1180) , y 범위 : (150 - 800)
                    {
                        byte* currentLine = PtrFirstPixel + (y * bitmapStride);
                        for (int x = 755 * bytesPerPixel; x < 1180 * bytesPerPixel; x = x + bytesPerPixel)
                        {
                            if(isPink(currentLine[x + 2],currentLine[x + 1], currentLine[x]))
                            {
                                // Debug.WriteLine((x/bytesPerPixel)+"," + y);
                                //byte tempX = (byte)((double)((x / bytesPerPixel) - midX) * xRatio + 127.0);
                                //byte tempY = (byte)((double)(y-midY) * yRatio + 127.0);

                                int tX = (x / bytesPerPixel) - midX;
                                int tY = y - midY;
                                // Debug.WriteLine(tX+",,,"+tY);
                                double max = Math.Abs(tX);
                                double AbsTy = Math.Abs(tY);
                                if(AbsTy > max)
                                {
                                    max = AbsTy;
                                }
                                byte tmpX = (byte)((((double)tX)/(max)) * 127.0 + 128.0);
                                byte tmpY = (byte)((((double)tY)/(max)) * 127.0 + 128.0);




                                ds4.SetAxisValue(DualShock4Axis.LeftThumbX, tmpX);
                                ds4.SetAxisValue(DualShock4Axis.LeftThumbY, tmpY);
                                // Debug.WriteLine(tmpX+","+tmpY);
                                //Debug.WriteLine("결과 값 : " + (byte)((double)(tempX - midX) * xRatio + 127) + "," + ((byte)((double)(y-midY) * yRatio + 127)));
                                

                                b.UnlockBits(bitmapData);
                                return false;
                            }
                        }
                    }

                    
                }
            }
            catch { }

            ds4.SetAxisValue(DualShock4Axis.LeftThumbX, 127);
            ds4.SetAxisValue(DualShock4Axis.LeftThumbY, 127);
            b.UnlockBits(bitmapData);
            return true;
        }

        private Bitmap cropImage(Bitmap origin, Rectangle cropRect)
        {
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);



            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(origin, new Rectangle(0, 0, target.Width, target.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }
            return target;
        }


        [HandleProcessCorruptedStateExceptions]
        public unsafe bool findEnemyHP(BitmapData bitmapData, int LeftX, int RightX, int LeftY, int RightY, byte* PtrFirstPixel,int bytesPerPixel,int bitmapStride )
        {
            try 
            { 
                for(int y = LeftY; y <= RightY; y++)
                {
                    byte* currentLine = PtrFirstPixel + (y * bitmapStride);
                    for (int x = LeftX * bytesPerPixel; x < RightX * bytesPerPixel; x = x + bytesPerPixel)
                    {
                        double h,s,v;
                        rgb2hsv(currentLine[x+2],currentLine[x+1],currentLine[x],out h, out s, out v);
                        if( (h < 357.0) && (h > 2.5))
                        {
                            return false;
                            
                        }
                        
                        if(s < 0.9)
                        {

                            return false;
                        }
                        if(v < 0.25)
                        {
                            return false;
                        }

                    }
                }
            }
            catch
            {

            }


            return true;
        }

        


        public static class VirtualMouse
    {
        // constants for the mouse_input() API function
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const int MOUSEEVENTF_WHEEL = 0x0800;


        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static System.Drawing.Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            // NOTE: If you need error handling
            // bool success = GetCursorPos(out lpPoint);
            // if (!success)

            return lpPoint;
        }

        // import the necessary API function so .NET can
        // marshall parameters appropriately
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);


        public static void MoveTo(int x, int y, int nSpeed = 0)
        {
            System.Drawing.Point ptCur;
            var rect = Screen.PrimaryScreen.Bounds;
            int xCur, yCur;
            int delta;
            const int nMinSpeed = 32;

            x = 65535 * x / (rect.Right - 1) + 1;
            y = 65535 * y / (rect.Bottom - 1) + 1;

            if (nSpeed == 0)
            {
                mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, x, y, 0, 0);
                Task.Delay(10).Wait();
                return;
            }

            if (nSpeed < 0 || nSpeed > 100)
                nSpeed = 10; // Default is speed 10

            ptCur = GetCursorPosition();
            xCur = (int) ptCur.X * 65535 / (rect.Right - 1) + 1;
            yCur = (int) ptCur.Y * 65535 / (rect.Bottom - 1) + 1;

            // Mouse Calculation magic fickt meinen kopf ... im out now
            while (xCur != x || yCur != y)
            {
                if (xCur < x)
                {
                    delta = (x - xCur) / nSpeed;
                    if (delta == 0 || delta < nMinSpeed)
                        delta = nMinSpeed;
                    if (xCur + delta > x)
                        xCur = x;
                    else
                        xCur += delta;
                }
                else if (xCur > x)
                {
                    delta = (xCur - x) / nSpeed;
                    if (delta == 0 || delta < nMinSpeed)
                        delta = nMinSpeed;
                    if (xCur - delta < x)
                        xCur = x;
                    else
                        xCur -= delta;
                }

                if (yCur < y)
                {
                    delta = (y - yCur) / nSpeed;
                    if (delta == 0 || delta < nMinSpeed)
                        delta = nMinSpeed;
                    if (yCur + delta > y)
                        yCur = y;
                    else
                        yCur += delta;
                }
                else if (yCur > y)
                {
                    delta = (yCur - y) / nSpeed;
                    if (delta == 0 || delta < nMinSpeed)
                        delta = nMinSpeed;
                    if (yCur - delta < y)
                        yCur = y;
                    else
                        yCur -= delta;
                }

                mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, xCur, yCur, 0, 0);

                Task.Delay(10).Wait();
            }
        }

        // simulates a click-and-release action of the left mouse
        // button at its current position
        public static void Scroll(int scroll)
        {
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, scroll, 0);  // -Value für ScrollDown und +Value für ScrollUp 
                                                              // Bsp.: VirtualMouse.Scroll(-120); VirtualMouse.Scroll(+120);
        }
        public static void LeftClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }  
        public static void LeftDown()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }
        public static void LeftUp()
        {
            mouse_event(MOUSEEVENTF_LEFTUP, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }

        public static void RightClick()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_RIGHTUP, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
        }    
        public static void RightDown()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
            
        }
        public static void RightUp()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
            
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator System.Drawing.Point(POINT point)
            {
                return new System.Drawing.Point(point.X, point.Y);
            }
        }
    }

        private static DateTime Delay(int ms)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, ms);
            DateTime AfterWards = ThisMoment.Add(duration);
            while (AfterWards >= ThisMoment)
            {
                System.Windows.Forms.Application.DoEvents();
                ThisMoment = DateTime.Now;

            }
            return DateTime.Now;
        }       // thread sleep 대체
        private void autoEnemy()
        {
            while (true)
            {
                ds4.SetButtonState(DualShock4Button.ThumbRight, true);
                Delay(rand.Next(30,60));
                ds4.SetButtonState(DualShock4Button.ThumbRight, false);
                Delay(rand.Next(30,60));
            }
        }

        private void randomDelay(int first, int last)
        {
            Delay(rand.Next(first, last));
        }

        

        private void ReleaseAllKey()
        {

            ds4.SetAxisValue(DualShock4Axis.LeftThumbX, 127);
            ds4.SetAxisValue(DualShock4Axis.LeftThumbY, 127);
            foreach (var temp in DualShock4Button.GetAll<DualShock4Button>())
            {
                ds4.SetButtonState(temp, false);
            }
            ds4.SetDPadDirection(DualShock4DPadDirection.None);
            ds4.SetSliderValue(DualShock4Slider.LeftTrigger, 0);
            ds4.SetSliderValue(DualShock4Slider.RightTrigger, 0);
            ds4.SetAxisValue(DualShock4Axis.RightThumbY, 127);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                ReleaseAllKey();
            }
            catch { }
            try
            {
                ds4.AutoSubmitReport = false;
            }
            catch { }
            try
            {
                ds4.Disconnect();

            }
            catch { }
            try
            {
                ds4.Dispose();

            }
            catch { }
            try
            {
                client.Dispose();

            }
            catch { }



            System.Diagnostics.Process[] mProcess = System.Diagnostics.Process.GetProcessesByName(Application.ProductName);
            foreach (System.Diagnostics.Process p in mProcess)
            {
                p.Kill();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //while (true)
            //{
            //    Bitmap b = getBmp();
            //    Bitmap b2 = cropImage(b,ROI_Map);

            //    if (searchIMG(b2,bitmap_pin,out pt) > 0.5)
            //    {
            //        // 130, 80 이 가운데
            //        int diff = (int)(Math.Sqrt(Math.Pow(130-pt.X,2) + Math.Pow(80-pt.Y,2) ));
            //        Debug.WriteLine(diff);
            //    }
            //    b2.Dispose();
            //    b.Dispose();
            //}
            Delay(1000);
            for(int i = 0 ; i < 5; i++)
            {
                pressKeyList[1]();
                Delay(30);
                releaseKeyList[1]();
                Delay(30);
            }
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SearchOverlay selectArea = new SearchOverlay();
            if(selectArea.ShowDialog() == DialogResult.OK)
            {
                setMapPositionButton(sender);
            }
        }

        private void setMapPositionButton(object sender)
        {
            System.Windows.Forms.Button tempButton = (System.Windows.Forms.Button)sender;
            Debug.WriteLine(Form1.lastKey);

            using(Bitmap b = getBmp())
            {
                if(searchIMG(b, bitmap_Maps[0],out pt) > 0.8)
                {
                    var temp = Form1.lastKey.Split(',');
                    int x = int.Parse(temp[0]) - pt.X;
                    int y= int.Parse(temp[1]) - pt.Y;
                    tempButton.Text = x + "," + y;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            SearchOverlay selectArea = new SearchOverlay();
            if(selectArea.ShowDialog() == DialogResult.OK)
            {
                setMapPositionButton(sender);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            SearchOverlay selectArea = new SearchOverlay();
            if(selectArea.ShowDialog() == DialogResult.OK)
            {
                setMapPositionButton(sender);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            SearchOverlay selectArea = new SearchOverlay();
            if(selectArea.ShowDialog() == DialogResult.OK)
            {
                setMapPositionButton(sender);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SearchOverlay selectArea = new SearchOverlay();
            if(selectArea.ShowDialog() == DialogResult.OK)
            {
                setMapPositionButton(sender);
            }
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.InitialDirectory = Form1.fullpath + "saveData";
                saveFileDialog.Filter = "SettingFiles|*.jamsu2";
                saveFileDialog.Title = "세팅 파일 저장";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string tempData = "jamsumon!@";
                    System.Windows.Forms.CheckBox[] list_checkbox = new System.Windows.Forms.CheckBox[] {checkBox1,checkBox2,checkBox3,checkBox4,checkBox5,checkBox6,checkBox7 };
                    System.Windows.Forms.RadioButton[] list_radio = new System.Windows.Forms.RadioButton[] {radioButton1,radioButton2,radioButton3,radioButton4,radioButton5,radioButton6,radioButton7 };
                    System.Windows.Forms.Button[] list_button = new System.Windows.Forms.Button[]{ button3,button4,button5,button6,button7};

                    string strCheckbox = "";
                    foreach(var abc in list_checkbox)
                    {
                        if (abc.Checked)
                        {
                            strCheckbox = strCheckbox + "1#";
                        }
                        else
                        {
                            strCheckbox = strCheckbox + "0#";
                        }
                    }
                    string strRadio = "";
                    foreach(var abc in list_radio)
                    {
                        if (abc.Checked)
                        {
                            strRadio = strRadio + "1#";
                        }
                        else
                        {
                            strRadio = strRadio + "0#";
                        }
                    }
                    string strButton = "";
                    foreach(var abc in list_button)
                    {
                        strButton = strButton + abc.Text + "#";
                    }

                    tempData = tempData + strCheckbox+"!@"+strRadio+"!@"+strButton+"!@";


                    string saveData = Convert.ToBase64String(Encoding.Unicode.GetBytes(tempData));
                    FileStream fs = File.Open(saveFileDialog.FileName, FileMode.Create);

                    // BinaryWriter는 파일스트림을 사용해서 객체를 생성한다
                    using (BinaryWriter wr = new BinaryWriter(fs))
                    {
                        wr.Write(saveData);
                    }
                    MessageBox.Show("저장 성공");
                }

            }
            catch
            {
                MessageBox.Show("설정 값을 저장 중에 오류가 발생하였습니다.");
            }
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = Form1.fullpath + "saveData";
                openFileDialog.Filter = "SettingFiles|*.jamsu2";
                openFileDialog.Title = "세팅 파일 불러오기";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    System.Windows.Forms.CheckBox[] list_checkbox = new System.Windows.Forms.CheckBox[] {checkBox1,checkBox2,checkBox3,checkBox4,checkBox5,checkBox6,checkBox7 };
                    System.Windows.Forms.RadioButton[] list_radio = new System.Windows.Forms.RadioButton[] {radioButton1,radioButton2,radioButton3,radioButton4,radioButton5,radioButton6,radioButton7 };
                    System.Windows.Forms.Button[] list_button = new System.Windows.Forms.Button[]{ button3,button4,button5,button6,button7};

                    string s;
                    using (BinaryReader rdr = new BinaryReader(File.Open(openFileDialog.FileName, FileMode.Open)))
                    {
                        s = rdr.ReadString();

                    }
                    s = Encoding.Unicode.GetString(Convert.FromBase64String(s));
                    string[] result = s.Split(new string[] { "!@" }, StringSplitOptions.None);

                    if (!result[0].Equals("jamsumon"))
                    {
                        MessageBox.Show("데이터 변조 위험. 불러오기 실패");
                        return;
                    }
                    {
                        var tmp = result[1].Split('#');
                        for(int i = 0 ; i < list_checkbox.Length; i++)
                        {
                            if (tmp[i].Equals("0"))
                            {
                                list_checkbox[i].Checked = false;
                            }
                            else
                            {
                                list_checkbox[i].Checked = true;
                            }
                        }
                    }
                    {
                        var tmp = result[2].Split('#');
                        for(int i = 0 ; i < list_radio.Length; i++)
                        {
                            if (tmp[i].Equals("0"))
                            {
                                list_radio[i].Checked = false;
                            }
                            else
                            {
                                list_radio[i].Checked = true;
                            }
                        }
                    }
                    {
                        var tmp = result[3].Split('#');
                        for(int i = 0 ; i < list_button.Length; i++)
                        {
                            try
                            {
                                list_button[i].Text = tmp[i].ToString();
                            }
                            catch { }
                            
                        }
                    }

                }
            }

            catch
            {
                MessageBox.Show("설정 값을 불러오는 중 오류가 발생하였습니다.");
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            System.Drawing.Point coordinates = me.Location;
            if(coordinates.X < 62)
            {
                checkBox1.Checked = !checkBox1.Checked;
            }
            else if(coordinates.X < 130)
            {
                checkBox2.Checked = !checkBox2.Checked;
            }
            else if(coordinates.X < 192)
            {
                checkBox3.Checked = !checkBox3.Checked;
            }
            else if(coordinates.X < 252)
            {
                checkBox4.Checked = !checkBox4.Checked;
            }
            else if(coordinates.X < 314)
            {
                checkBox5.Checked = !checkBox5.Checked;
            }
            else
            {
                checkBox6.Checked = !checkBox6.Checked;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            checkBox7.Checked = !checkBox7.Checked;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            {
                RegisterHotKey(this.Handle, 1, 0, (int)Keys.F6);
                RegisterHotKey(this.Handle, 1001, 2, (int)Keys.F6);
                RegisterHotKey(this.Handle, 2001, 4, (int)Keys.F6);

                RegisterHotKey(this.Handle, 2, 0, (int)Keys.F7);
                RegisterHotKey(this.Handle, 1002, 2, (int)Keys.F7);
                RegisterHotKey(this.Handle, 2002, 4, (int)Keys.F7);
            }
        }
    }
}
