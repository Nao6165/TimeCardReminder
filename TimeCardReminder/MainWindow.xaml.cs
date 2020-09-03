using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;

#region ObservableCollection<T>を使用するために必要
using System.Collections.ObjectModel;
#endregion

using System.IO;
using System.Threading;

#region Windows API Code Pack のダイアログの名前空間を using
using Microsoft.WindowsAPICodePack.Dialogs;
#endregion

using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Interop;

#region WpfとFormのAPIの使い分け用
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using MessageBox = System.Windows.MessageBox;
#endregion

namespace TimeCardReminder
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        public MainWindow()
        {
            if(isFirst == true)
            {   // 初回起動時の処理
                // MainWindowの二重起動防止
                // 初回のみセマフォを生成
                _pool = new System.Threading.Semaphore(1, 1, SemaphoreName,
                                                   out createdNew);
            }

            if (_pool.WaitOne(0, false) == false)
            {   // セマフォ取得済みの場合MainWindowを起動しない
                // 既存のMainWindowをアクティブにする
                currentWindow.WindowState = WindowState.Normal;
                currentWindow.Activate();
                // NotifyIconWrapperに通知するため、二重起動発生フラグを立てる
                isDoubleBoot = true;
                return;
            }
            // 現在起動しているMainWindowのオブジェクトを保存
            currentWindow = this;

            InitializeComponent();

            // スケジュール情報を初期化
            Schedules schedules = new Schedules();
            schedules.mySchedules = new List<Schedule>();

            // スケジュール情報をロード
            ReadFromFile(ref schedules);

            foreach(Schedule s in schedules.mySchedules)
            {
                listBox1.Items.Add(s);
            }

            // 直近のリマインド項目をタイマーセット
            SetNextTimerEvent();

            checkBox2.IsChecked = Properties.Settings.Default.firstBootWindow;
            if (  (isFirst == true)
                &&(checkBox2.IsChecked == true)) 
            {
                Close();
            }

            // 初回フラグを更新
            isFirst = false;

        }

        private DispatcherTimer timer1;
        public Schedule nextSchedule = new Schedule(new DateTime(),null);
        public string scheduleFileName = "schedule.txt";

        public static Semaphore _pool;
        public const string SemaphoreName = "TimeCardReminderMainWindow";
        private bool createdNew;
        private static bool isFirst = true;
        private static bool isDoubleBoot = false;
        private static MainWindow currentWindow;
        private static String execFilePathWork = "";  // イベント発生時に実行するファイルPath

        /// <summary>
        /// MainWindowを２重起動しようとしたことを通知
        /// </summary>
        /// <returns></returns>
        public bool IsDoubleBoot() { return isDoubleBoot; }

        /// <summary>
        /// Setボタン押下時の動作。タイマーをセットする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            bool rst = SetNextTimerEvent();
            if(rst == true) 
            {
                MyMessageBox.Show(new Wpf32Window(this),
                    $"タイマーをセットしました",
                    "TimeCardReminder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MyMessageBox.Show(new Wpf32Window(this), 
                    $"タイマーは無効です",
                    "TimeCardReminder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            WriteToFile(scheduleFileName);
        }

        /// <summary>
        /// 次のタイマーイベントをセットする
        /// </summary>
        private bool SetNextTimerEvent()
        {
            // リストボックスに項目がなければ終了。
            if (listBox1.Items.Count == 0)
            {
                textBlock1.Text = $"次の通知はありません";
                return false;
            }

            if(timer1 != null) { timer1.Stop(); }

            // タイマのインスタンスを生成
            timer1 = new DispatcherTimer();

            // listBox1内のリストから次のスケジュールを検索
            Schedules schedules = new Schedules();
            schedules.mySchedules = new List<Schedule>();
            foreach (Schedule s in listBox1.Items)
            {
                schedules.mySchedules.Add(s);
            }

            bool ret = schedules.getNextSchedule(ref nextSchedule);
            if (ret == false)
            {
                textBlock1.Text = $"次の通知はありません";
                return false;    
            }

            // time1 にリマインド目標時刻を代入
            DateTime dt = nextSchedule.Timer;
            TimeSpan st = new TimeSpan(dt.Hour, dt.Minute, dt.Second);
            var time1 = DateTime.Today + st - DateTime.Now;

            // 目標時刻を過ぎていれば次の日の同時刻にする
            if (time1 < TimeSpan.Zero) time1 += new TimeSpan(24, 0, 0);

            timer1.Interval = time1;

            // 時間になったらそこから 24 時間毎に Method1 を呼び出すようタイマーをセット
            timer1.Tick += new EventHandler(Method1);

            timer1.Start();

            textBlock1.Text = $"次の通知は{nextSchedule.Timer.TimeOfDay.ToString()}です。";

            return true;
        }

        /// <summary>
        /// 通知イベント処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Method1(object sender, EventArgs e)
        {
            // MainWindowが閉じている場合があるので、MainWindowの中心に出るMyMessageBoxではなく、
            // ここではMessageBoxを使用する。
            if (nextSchedule.ExecFilePath != "")
            {
                if (File.Exists(nextSchedule.ExecFilePath))
                {
                    ExecMyFile(nextSchedule.ExecFilePath);
                }
                else
                {
                    MessageBox.Show($"ファイルがありません",
                        "TimeCardReminder",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information,
                        MessageBoxResult.OK,
                        MessageBoxOptions.DefaultDesktopOnly);
                }
            }
            MessageBox.Show($"{nextSchedule.Message}\r\n({DateTime.Now.ToString("HH:mm")})",
                "TimeCardReminder",
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                MessageBoxResult.OK,
                MessageBoxOptions.DefaultDesktopOnly);

            timer1.Stop();

            SetNextTimerEvent();
        }

        /// <summary>
        /// 指定のFileを開く(実行する)
        /// </summary>
        /// <param name="targetPath"></param>
        private void ExecMyFile(string targetPath)
        {
            System.Diagnostics.Process.Start(targetPath);
        }
        
        /// <summary>
        /// タイマを停止
        /// </summary>
        private void StopTimer(object sender, CancelEventArgs e)
        {
            timer1.Stop();
        }

        /// <summary>
        /// ListBoxの内容をファイルに書き込む
        /// </summary>
        /// <param name="fileName"></param>
        private void WriteToFile(string fileName)
        {
            System.IO.StreamWriter SaveFile = new System.IO.StreamWriter(fileName);
            foreach (Schedule item in listBox1.Items)
            {
                SaveFile.WriteLine($"{item.Timer}\t{item.Message}\t{item.Enable}\t{item.ExecFilePath}");
            }
            SaveFile.Close();
        }

        /// <summary>
        /// スケジュールファイルの読み取りを行う
        /// </summary>
        private void ReadFromFile(ref Schedules schedules)
        {
            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(scheduleFileName))
                {
                    // Read the stream to a string, and write the string to the console.
                    String line = sr.ReadLine();
                    while(line != null)
                    {
                        Schedule s = new Schedule(new DateTime(), null);
                        string[] split = line.Split(new Char[] { '\t' });
                        s.Timer = DateTime.Parse(split[0].ToString());
                        s.Message = split[1].ToString();
                        s.Enable = bool.Parse(split[2].ToString());
                        s.ExecFilePath = split[3].ToString();

                        // listBox1.Items.Add(s);
                        schedules.mySchedules.Add(s);

                        line = sr.ReadLine();
                    }                    
                }
            }
            catch (IOException e)
            {
                // 何もしない
                // Console.WriteLine("The file could not be read:");
                // Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// 追加ボタン押下―listBox1に項目を追加する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            // メッセージが設定されていなければ無効
            if((textBox1.Text == null)|| (textBox1.Text == "")) 
            {
                MyMessageBox.Show(new Wpf32Window(this),
                    $"メッセージがありません",
                    "TimeCardReminder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return; 
            }

            // ListBoxに登録
            Schedule schedule = new Schedule(dateTimePicker1.Value, textBox1.Text.ToString(),true, execFilePathWork);
            listBox1.Items.Add(schedule);

            // 追加したアイテムを選択
            listBox1.SelectedIndex = listBox1.Items.Count - 1;

            // ListBox設定をファイルに保存
            WriteToFile(scheduleFileName);
        }

        /// <summary>
        /// 削除ボタン押下時処理―listBox1内の項目を削除する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            // 選択項目がなければ終了
            if(listBox1.SelectedItems.Count == 0)
            {
                MyMessageBox.Show(new Wpf32Window(this),
                    $"アイテムが選択されていません",
                   "TimeCardReminder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return; 
            }
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            // ListBox設定をファイルに保存
            WriteToFile(scheduleFileName);
        }

        /// <summary>
        /// 変更ボタン押下時処理―listBox1にて指定した項目を変更する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            Schedule scheduleTempNew = new Schedule(new DateTime(0), null);
            Schedule scheduleTempOld;

            int lbIndex = listBox1.SelectedIndex;
            if(lbIndex == -1) 
            {
                MyMessageBox.Show(new Wpf32Window(this),
                    $"アイテムが選択されていません",
                    "TimeCardReminder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return; 
            }

            // 更新スケジュールを作成。
            scheduleTempOld = (Schedule)listBox1.SelectedItem;
            scheduleTempNew.Message = textBox1.Text;
            scheduleTempNew.Timer = dateTimePicker1.Value;
            scheduleTempNew.Enable = scheduleTempOld.Enable;
            scheduleTempNew.ExecFilePath = execFilePathWork;

            // 古いスケジュールを削除
            listBox1.Items.RemoveAt(lbIndex);

            // 更新スケジュールを挿入
            listBox1.Items.Insert(lbIndex, scheduleTempNew);

            // 更新したアイテムを選択
            listBox1.SelectedIndex = lbIndex;

            // ListBox設定をファイルに保存
            WriteToFile(scheduleFileName);

            MyMessageBox.Show(new Wpf32Window(this),
                $"選択項目を変更しました",
               "TimeCardReminder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

        }

        /// <summary>
        /// listBox1内の選択されているアイテムに変化があった時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Schedule schedule = (Schedule)listBox1.SelectedItem;
            if(schedule == null)
            {   // listBox1のItemが削除された場合は何もしない
                return;
            }
            textBox1.Text = schedule.Message;
            dateTimePicker1.Value = schedule.Timer;
            textBlock2.Text = System.IO.Path.GetFileName(schedule.ExecFilePath);
            if(schedule.ExecFilePath == "")
            {
                button5.Content = "参照";
            }
            else
            {
                button5.Content = "クリア";
            }
        }

        /// <summary>
        /// ウィンドウが閉じるときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void metroWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                currentWindow = null;   // 現在開いているMainWindowを閉じるので、無効にする
                isDoubleBoot = false;   // ２重起動フラグを寝かす
                _pool.Release();        // セマフォを解放する
                Properties.Settings.Default.Save(); // アプリのプロパティ―設定を保存
            }
            catch (Exception ex)
            {
                // 何もしない。
            }

        }

        /// <summary>
        /// 「起動時にこのWindowを開かない」チェックボックスをチェックした場合
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox2_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.firstBootWindow = true;
        }

        /// <summary>
        /// 「起動時にこのWindowを開かない」チェックボックスのチェックが外された場合
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox2_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.firstBootWindow = false;
        }

        /// <summary>
        /// 参照ボタン押下時に、ファイルを開くダイアログボックスを表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button5_Click(object sender, RoutedEventArgs e)
        {
            if (button5.Content.ToString() != "参照")
            {
                textBlock2.Text = "";
                execFilePathWork = "";
                button5.Content = "参照";
            }
            else
            {
                // ダイアログのインスタンスを生成
                var refExecFileDialog = new CommonOpenFileDialog("ファイルを開く");

                // ファイルの種類を設定
                refExecFileDialog.Filters.Add(new CommonFileDialogFilter("HTML ファイル", "*.html;*.htm"));
                refExecFileDialog.Filters.Add(new CommonFileDialogFilter("テキストファイル", "*.txt"));
                refExecFileDialog.Filters.Add(new CommonFileDialogFilter("実行ファイル", "*.exe"));
                refExecFileDialog.Filters.Add(new CommonFileDialogFilter("全てのファイル", "*.*"));

                // ダイアログを表示
                if (refExecFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    // MessageBox.Show(refExecFileDialog.FileName);
                    execFilePathWork = refExecFileDialog.FileName;
                    textBlock2.Text = System.IO.Path.GetFileName(refExecFileDialog.FileName);
                    button5.Content = "クリア";
                }
            }
        }
    }

    /// <summary>
    /// スケジュールクラス―タイマーイベント情報
    /// </summary>
    public class Schedule
    {
        public bool Enable { get; set; }        // リマインドの有効無効(チェックボックス)
        public DateTime Timer { get; set; }     // リマインド時間
        public string Message { get; set; }     // リマインド時のメッセージ
        public string ExecFilePath { get; set; }     // リマインド時の実行ファイルパス

        public Schedule(DateTime timer, String message, bool enable = false, String execFilePath = "")
        {
            this.Enable = enable;
            this.Timer = timer;
            this.Message = message;
            this.ExecFilePath = execFilePath;
        }
    }

    /// <summary>
    /// スケジュールスクラス―タイマーイベント情報を管理
    /// </summary>
    public class Schedules : ObservableCollection<Schedule>
    {
        public Schedules()
        {
        }

        public List<Schedule> mySchedules { get; set; }

        /// <summary>
        /// 現在時刻に最も近い予定を探す
        /// </summary>
        /// <returns></returns>
        public bool getNextSchedule(ref Schedule schedule)
        {
            TimeSpan tsMin = new TimeSpan();

            // 予定がなければ終了。初期化されたscheduleを返す。
            if(mySchedules[0].Message == null) { return false; }

            int cnt = 0;    // 有効データカウンタ
            // for (int i = 0; i < mySchedules.Count; i++)
            foreach(Schedule s in mySchedules)
            {
                // チェックボックスが有効なものだけで探す
                if (s.Enable == false) { continue; }
                cnt++;

                // TimeSpanの算出
                DateTime dt = s.Timer;
                TimeSpan ts = new TimeSpan(dt.Hour, dt.Minute, dt.Second);
                var tsSrc = DateTime.Today + ts - DateTime.Now;

                // 目標時刻を過ぎていれば次の日の同時刻にする
                if (tsSrc < TimeSpan.Zero) tsSrc += new TimeSpan(24, 0, 0);
                
                // TimeSpanの最も短い物を探す
                if ((cnt == 1) || (tsSrc < tsMin))
                {
                    tsMin = tsSrc;
                    schedule = s;
                }

            }

            // 何も選択されなかった。初期化されたscheduleを返す。
            if (cnt == 0) { return false; }

            return true;
        }
    }

    static class MyMessageBox
    {
        static class NativeWin32API
        {
            #region GetWindowLong "指定されたウィンドウに関する情報を取得する。"
            [DllImport("user32.dll")]
            static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);
            const int GWL_HINSTANCE = -6;   // アプリケーションのインスタンスハンドル
            public static IntPtr GetWindowHInstance(IntPtr hWnd) => GetWindowLong(hWnd, GWL_HINSTANCE);
            #endregion

            #region GetCurrentThreadId "プロセスを識別するカレントスレッドの一意のIDを取得する。"
            [DllImport("kernel32.dll")]
            public static extern IntPtr GetCurrentThreadId();
            #endregion

            #region WindowsHookEx "CBT(ウィンドウの生成などのイベントに応じてシステムから呼出される関数)の通知を受け取るフックプロシージャの設定"
            // アプリケーション定義のフックプロシージャをフックチェーン内にインストールする。
            [DllImport("user32.dll")]
            static extern IntPtr SetWindowsHookEx(int idHook, HOOKPROC lpfn, IntPtr hInstance, IntPtr threadId);
            const int WH_CBT = 5;   // トレーニング(CBT)アプリケーションの通知を受け取るフックプロシージャをインストール。
            public static IntPtr SetWindowsHookEx(HOOKPROC lpfn, IntPtr hInstance, IntPtr threadId) => SetWindowsHookEx(WH_CBT, lpfn, hInstance, threadId);

            // フックを解除する
            [DllImport("user32.dll")]
            public static extern bool UnhookWindowsHookEx(IntPtr hHook);

            // 現在のフックチェーン内の次のフックプロシージャに、フック情報を渡す。
            [DllImport("user32.dll")]
            public static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, IntPtr lParam);

            public delegate IntPtr HOOKPROC(int nCode, IntPtr wParam, IntPtr lParam);
            public const int HCBT_ACTIVATE = 5; // ウィンドウがこれからアクティブになる
            #endregion

            #region GetWindowRect "windowの位置と大きさを取得"
            [DllImport("user32.dll")]
            static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
            [StructLayout(LayoutKind.Sequential)]   // 宣言した順序通りにメンバーをレイアウト
            public struct RECT
            {
                public int Left, Top, Right, Bottom;
                public int Height
                {
                    get { return Bottom - Top; }
                    set { Bottom = value + Top; }
                }

                public int Width
                {
                    get { return Right - Left; }
                    set { Right = value + Left; }
                }
            }

            public static RECT GetWindowRect(IntPtr hWnd)
            {
                RECT rc;
                GetWindowRect(hWnd, out rc);
                return rc;
            }
            #endregion

            #region SetWindowPos "Windowの位置やサイズを変更する"
            [DllImport("user32.dll")]
            static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
            const uint SWP_NOSIZE = 0x0001;     // cxとcyを使わない。cx:Windowの新しい幅, cy:Windowの新しい高さ
            const uint SWP_NOZORDER = 0x0004;   // hWndInsertAfterを使わない。 hWndInsertAfter:Zオーダー(ウインドウが重なり合うときの列)を決めるためのウインドウハンドル
            const uint SWP_NOACTIVATE = 0x0010; // ウインドウをアクティブ化しない。 

            public static bool SetWindowPos(IntPtr hWnd, int x, int y)
            {
                var flags = SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE;
                return SetWindowPos(hWnd, 0, x, y, 0, 0, flags);
            }
            #endregion
        }

        /// <summary>
        /// Hookを設定してMessageBoxを呼び出す。
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="text"></param>
        /// <param name="caption"></param>
        /// <param name="buttons"></param>
        /// <param name="icon"></param>
        /// <returns></returns>
        public static DialogResult Show(System.Windows.Forms.IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            hOwner = owner.Handle;
            var hInstance = NativeWin32API.GetWindowHInstance(owner.Handle);
            var threadId = NativeWin32API.GetCurrentThreadId();
            hHook = NativeWin32API.SetWindowsHookEx(new NativeWin32API.HOOKPROC(HookProc), hInstance, threadId);
            return System.Windows.Forms.MessageBox.Show(owner, text, caption, buttons, icon);
        }

        private static IntPtr hOwner = (IntPtr)null;
        private static IntPtr hHook = (IntPtr)null;
        /// <summary>
        /// MessageBoxの位置をOwnerWindowの中心に設定する
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == NativeWin32API.HCBT_ACTIVATE)
            {
                // OwnerWindowとMessageBoxの領域を取得する
                var rcOwner = NativeWin32API.GetWindowRect(hOwner);
                var rcMsgBox = NativeWin32API.GetWindowRect(wParam);

                // MessageBoxをOwnerWindowの中心に移動する
                var x = rcOwner.Left + (rcOwner.Width - rcMsgBox.Width) / 2;
                var y = rcOwner.Top + (rcOwner.Height - rcMsgBox.Height) / 2;
                NativeWin32API.SetWindowPos(wParam, x, y);

                // フックを解除する
                NativeWin32API.UnhookWindowsHookEx(hHook);
                hHook = (IntPtr)null;
            }
            return NativeWin32API.CallNextHookEx(hHook, nCode, wParam, lParam);
        }
    }

    /// <summary>
    /// WpfのハンドラをFormsに使用できるようにする
    /// </summary>
    public class Wpf32Window : System.Windows.Forms.IWin32Window
    {
        public IntPtr Handle { get; private set; }

        public Wpf32Window(Window window)
        {
            // wpf->Forms
            this.Handle = new WindowInteropHelper(window).Handle;
        }
    }
}
