using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;


namespace InstaCommenter
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern int VkKeyScan(char ch);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string pClassName, string pWindowName);


        public ChromiumWebBrowser chromeBrowser;

        Thread thread;

        // Form reference for Anti-minimize.
        static Form1 myForm;

        // WindowState for Anti-minimize.
        static FormWindowState preWindowState;

        // Delegate.
        public delegate void myDelegate();

        // Anti-minimize. (Because CefSharp doesn't work in minimize-window.)
        private void UpWindow()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new myDelegate(this.UpWindow));
                return;
            }

            if (this.WindowState == FormWindowState.Minimized)
            {
                myForm.WindowState = preWindowState;

                int height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

                this.SetBounds(0, height - 10, 0, 0, BoundsSpecified.Y);
            }

        }




        // Message for setText()
        string message = "";

        // Set text.
        public void setText()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new myDelegate(this.setText));
                return;
            }

            this.Text = "AutoLikeSender4 " + message;

        }


        // キーストロークの入力.
        char myChar = ' ';
        public void insertChar()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new myDelegate(this.insertChar));
                return;
            }


            
            KeyEvent k = new KeyEvent();        
            k.WindowsKeyCode = (int)myChar;
            k.FocusOnEditableField = true;
            k.IsSystemKey = false;
            k.Type = KeyEventType.Char;
            chromeBrowser.GetBrowser().GetHost().SendKeyEvent(k);
            
            
        }


        // Close the form.
        public void formClose()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new myDelegate(this.formClose));
                return;
            }

            myForm.Close();

        }

        // Down window
        private void downWindow()
        {

            if (this.InvokeRequired)
            {
                this.Invoke(new myDelegate(this.downWindow));
                return;
            }

            int height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            this.SetBounds(0, height - 10, 0, 0, BoundsSpecified.Y);



        }

        public Form1()
        {
            InitializeComponent();
            InitializeChromium();


            // Remember current WindowState.
            myForm = this;
            preWindowState = this.WindowState;

            try
            {

                thread = new Thread(new ThreadStart(() =>
                {
                    // メインスレッド

                    // under.txtがある場合、ウィンドウを自動的に下へ隠す。
                    if (File.Exists("under.txt"))
                    {
                        downWindow();
                    }

                    // ログインページ読み込み
                    chromeBrowser.Load("https://instagram.com/");
                                       

                    // いいね付与する検索ワード一覧読み込み
                    List<string> keywords = loadText("keywords.txt");

                    // いいね回数制限読み込み
                    int limit = int.Parse(loadJScript("limitter.txt"));

                    // いいねインターバル読み込み
                    int interval = int.Parse(loadJScript("interval.txt"))*1000;

                    // いいね済回数
                    int counter = 0;
                    
                    //ログイン待ち
                    Thread.Sleep(60000);
                    
                    
                    
                    

                    // 検索キーワードごとに
                    for (int j = 0; j < keywords.Count; j++)
                    {
                        string myKeyword = keywords[j];

                        Thread.Sleep(10000);
                        message = "検索結果のページ読み込み中 (" + counter + "/" + limit;
                        setText();
                        UpWindow();
                        chromeBrowser.Load("https://www.instagram.com/explore/tags/" + myKeyword);
                        
                        // スクロールダウン
                        Thread.Sleep(10000);
                        message = "スクロールダウン中(" + counter + "/" + limit;
                        setText();
                        UpWindow();
                        chromeBrowser.ExecuteScriptAsync(loadJScript("jscript_scrolldown.txt"));
                        Thread.Sleep(10000);
                        chromeBrowser.ExecuteScriptAsync(loadJScript("jscript_scrolldown.txt"));


                        // 最新の投稿をクリック
                        Thread.Sleep(10000);
                        message = "最新投稿選択中(" + counter + "/" + limit;
                        setText();
                        UpWindow();
                        chromeBrowser.ExecuteScriptAsync(loadJScript("jscript_imageselect.txt"));


                        // [リミット÷キーワード数]回、いいねする。
                        for (int k = 0; k < (limit / keywords.Count); k++)
                        {
                            // いいね付与
                            Thread.Sleep(interval);
                            message = "いいね付与中(" + counter + "/" + limit;
                            setText();
                            UpWindow();
                            chromeBrowser.ExecuteScriptAsync(loadJScript("jscript_like.txt"));

                            // 次ボタン押下
                            Thread.Sleep(2000);
                            message = "次ボタン押下中(" + counter + "/" + limit;
                            setText();
                            UpWindow();
                            chromeBrowser.ExecuteScriptAsync(loadJScript("jscript_next.txt"));

                            counter++;
                        }

                        
                        
                    }
                    


                    // 終了した旨表示。
                    message = "FINISHED. ";
                    setText();
                    Thread.Sleep(60000);

                    // フォームを閉じてアプリケーションを終了させる。
                    formClose();


                }));

                // 上記スレッドを起動する。
                thread.Start();


            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => { MessageBox.Show(this, ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error); }));
            }
        }

        public void InitializeChromium()
        {
            CefSettings settings = new CefSettings();
            settings.BrowserSubprocessPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                   Environment.Is64BitProcess ? "x64" : "x86",
                                                   "CefSharp.BrowserSubprocess.exe");
            // ロケールを日本に
            settings.Locale = "ja";
            settings.AcceptLanguageList = "ja-JP";

            settings.CachePath = System.Environment.CurrentDirectory + "\\cache";
            settings.PersistSessionCookies = true;

            Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);

            chromeBrowser = new ChromiumWebBrowser("https://instagram.com");

            // アドレス変わったときのイベントハンドラ追加
            chromeBrowser.AddressChanged += Browser_AddressChanged;

            this.Controls.Add(chromeBrowser);
            chromeBrowser.Dock = DockStyle.Fill;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            thread.Abort();

            Cef.Shutdown();
        }


        // テキストファイルを行ごとに読み込む。　ファイルが無ければnullを返す。
        private List<string> loadText(string fileName)
        {
            string line = "";

            List<string> al = new List<string>();

            try
            {
                using (StreamReader sr = new StreamReader(fileName, Encoding.GetEncoding("UTF-8")))
                {

                    while ((line = sr.ReadLine()) != null)
                    {
                        
                        al.Add(line);
                    }
                }

                return al;

            }
            catch (Exception ex)
            {
                return null;
            }
        }



        // ファイル全体を１行として読み込む。
        private String loadJScript(string fileName)
        {
            StreamReader sr = new StreamReader(fileName, Encoding.GetEncoding("Shift_JIS"));

            string text = sr.ReadToEnd();

            sr.Close();

            return text;
        }

        
        private string cropMyName(string url)
        {
            int start = url.IndexOf(".com/") + 5;
            int end = url.Length - 1;
            return url.Substring(start, end - start);
        }

        // HTMLからユーザーIDを取り出す
        private string getMyName()
        {
            string myHTML = GetHTMLFromWebBrowser();

            int end = myHTML.LastIndexOf("のプロフィール写真");

            string subHTML = myHTML.Substring(0, end);

            int start = subHTML.LastIndexOf("alt=")+5;

            return myHTML.Substring(start, end - start);
        }

        // HTMLコードを取得
        private string GetHTMLFromWebBrowser()
        {
            Task<String> taskHtml = chromeBrowser.GetBrowser().MainFrame.GetSourceAsync();

            string response = taskHtml.Result;
            return response;
        }

        //カレントURL取得
        string currentURL = "";
        private void Browser_AddressChanged(object sender,AddressChangedEventArgs e)
        {
            currentURL = e.Address;
        }


        // 文字列中の特定のキーワードの登場回数を数える
        private int countStr(string target, string keyword)
        {
            int count = 0;
            int nextpos = 0;

            for (count = 0; count < 100; count++)
            {
                nextpos = target.IndexOf(keyword);

                if (nextpos >= 0)
                {
                    target = target.Substring(nextpos + 1);
                }
                else
                {
                    break;
                }
            }

            return count;
        }
    }
}