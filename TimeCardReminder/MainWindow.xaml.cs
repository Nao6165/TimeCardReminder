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
// ObservableCollection<T>を使用するために必要
using System.Collections.ObjectModel;
using System.IO;

namespace TimeCardReminder
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // this.DataContext = new Schedules();

            Schedules schedules = new Schedules();
            schedules.mySchedules = new List<Schedule>();

            ReadFromFile(ref schedules);

            foreach(Schedule s in schedules.mySchedules)
            {
                listBox1.Items.Add(s);
            }

            SetNextTimerEvent();
        }

        private DispatcherTimer timer1;
        // public Schedules schedules = new Schedules();
        public Schedule nextSchedule = new Schedule(new DateTime(),null);
        public string scheduleFileName = "schedule.txt";

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
                MessageBox.Show($"タイマーをセットしました",
                    "TimeCardReminder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information,
                    MessageBoxResult.OK,
                    MessageBoxOptions.DefaultDesktopOnly);
            }
            else
            {
                MessageBox.Show($"タイマーは無効です",
                    "TimeCardReminder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information,
                    MessageBoxResult.OK,
                    MessageBoxOptions.DefaultDesktopOnly);
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

            // タイマのインスタンスを生成
            timer1 = new DispatcherTimer();

            // listBox1内のリストから次のスケジュールを検索
            // for (int i = 0; i < listBox1.Items.Count; i++)
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
                SaveFile.WriteLine($"{item.Timer}\t{item.Message}\t{item.Enable.ToString()}");
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
            if(textBox1.Text == null) 
            {
                MessageBox.Show($"メッセージがありません",
                    "TimeCardReminder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information,
                    MessageBoxResult.OK,
                    MessageBoxOptions.DefaultDesktopOnly);

                return; 
            }

            // ListBoxに登録
            Schedule schedule = new Schedule(dateTimePicker1.Value, textBox1.Text.ToString(),true);
            listBox1.Items.Add(schedule);
            

            // テキストボックス内の文字を消去
            textBox1.Text = null;

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
            if(listBox1.SelectedItems.Count == 0) { return; }
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            // ListBox設定をファイルに保存
            WriteToFile(scheduleFileName);
        }

        /// <summary>
        /// 編集ボタン押下時処理―listBox1にて指定した項目を変更する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            int lbIndex = listBox1.SelectedIndex;
            Schedule scheduleLb = (Schedule)listBox1.SelectedItem;

            Schedule schedule = new Schedule(new DateTime(), null);
            schedule.Message = textBox1.Text;
            schedule.Timer = dateTimePicker1.Value;
            schedule.Enable = scheduleLb.Enable;

            listBox1.Items.Remove(scheduleLb);

            listBox1.Items.Insert(lbIndex, schedule);

            // ListBox設定をファイルに保存
            WriteToFile(scheduleFileName);

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
        }
    }

    /// <summary>
    /// スケジュールクラス―タイマーイベント情報
    /// </summary>
    public class Schedule
    {
        public bool Enable { get; set; }
        public DateTime Timer { get; set; }
        public string Message { get; set; }

        public Schedule(DateTime timer, String message, bool enable = false)
        {
            this.Enable = enable;
            this.Timer = timer;
            this.Message = message;
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
}
