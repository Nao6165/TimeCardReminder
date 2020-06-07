﻿using System;
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
            schedules.mySchedules = new List<Schedule>();
        }

        private DispatcherTimer timer1;
        public Schedules schedules = new Schedules();
        public Schedule nextSchedule = new Schedule(new DateTime(),null);

        /// <summary>
        /// Setボタン押下時の動作。タイマーをセットする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            // リストボックスに項目がなければ終了。
            if (ListBox1.Items.Count == 0) { return; }

            // タイマのインスタンスを生成
            timer1 = new DispatcherTimer();

            for (int i = 0; i < ListBox1.Items.Count; i++)
            {
                schedules.mySchedules.Add((Schedule)ListBox1.Items.GetItemAt(i));
            }
            nextSchedule = schedules.getNextSchedule();

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

        }

        private void Method1(object sender, EventArgs e)
        {

            //            MessageBox.Show($"{textBox1.Text.ToString()}({DateTime.Now.ToString("HH:mm")})",
            MessageBox.Show($"{nextSchedule.Message}({DateTime.Now.ToString("HH:mm")})",
            "caption",
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                MessageBoxResult.OK,
                MessageBoxOptions.DefaultDesktopOnly);

            timer1.Stop();
        }

        // タイマを停止
        private void StopTimer(object sender, CancelEventArgs e)
        {
            timer1.Stop();
        }

        /// <summary>
        /// 追加ボタン押下―ListBox1に項目を追加する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            // メッセージが設定されていなければ無効
            if(textBox1.Text == null) { return; }

            // ListBoxに登録
            Schedule schedule = new Schedule(dateTimePicker1.Value, textBox1.Text.ToString());
            ListBox1.Items.Add(schedule);
            

            // テキストボックス内の文字を消去
            textBox1.Text = null;
        }

        /// <summary>
        /// 削除ボタン押下処理―ListBox1内の項目を削除する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            // 選択項目がなければ終了
            if(ListBox1.SelectedItems.Count == 0) { return; }
            ListBox1.Items.RemoveAt(ListBox1.SelectedIndex);
        }
    }

    public class Schedule
    {
        public DateTime Timer { get; set; }
        public string Message { get; set; }

        public Schedule(DateTime timer, String message)
        {
            this.Timer = timer;
            this.Message = message;
        }
    }

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
        public Schedule getNextSchedule()
        {
            Schedule schedule = new Schedule(new System.DateTime(0), null);
            TimeSpan tsMin = new TimeSpan();
            int nextIndex = 0;

            // 予定がなければ終了
            if(mySchedules[0].Message == null) { return null; }

            for (int i = 0; i < mySchedules.Count; i++)
            {
                DateTime dt = mySchedules[i].Timer;
                TimeSpan ts = new TimeSpan(dt.Hour, dt.Minute, dt.Second);
                var tsSrc = DateTime.Today + ts - DateTime.Now;

                // 目標時刻を過ぎていれば次の日の同時刻にする
                if (tsSrc < TimeSpan.Zero) tsSrc += new TimeSpan(24, 0, 0);

                if ((i == 0) || (tsSrc < tsMin))
                {
                    tsMin = tsSrc;
                    nextIndex = i;
                }

            }

            return mySchedules[nextIndex];
        }
    }
}
