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
        }

        private DispatcherTimer timer1;

        /// <summary>
        /// Setボタン押下時の動作。タイマーをセットする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            // タイマのインスタンスを生成
            timer1 = new DispatcherTimer();

            // time1 にリマインド目標時刻を代入
            DateTime dt = dateTimePicker1.Value;
            TimeSpan st = new TimeSpan(dt.Hour, dt.Minute, dt.Second);
            //var time1 = DateTime.Today + new TimeSpan(23, 20, 0) - DateTime.Now;
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
            // MessageBox.Show($"時間ですよ！({DateTime.Now.ToString("HH:mm")})");

            MessageBox.Show($"{textBox1.Text.ToString()}({DateTime.Now.ToString("HH:mm")})",
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
    }
}
