using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TimeCardReminder
{
    using System.Threading;
    using System.Windows;
 
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// タスクトレイに表示するアイコン
        /// </summary>
        private NotifyIconWrapper notifyIcon;
        private Mutex Mutex = new Mutex(false, "TimeCardReminder");

        private void ApplicationStart(object sender, StartupEventArgs e)
        {
            if (!this.Mutex.WaitOne(0, false))
            {
                // 既に起動しているため終了させる
                MessageBox.Show("ApplicationName は既に起動しています。", "二重起動防止", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                this.Mutex.Close();
                this.Mutex = null;
                Shutdown();
            }
        }

        private void ApplicationExit(object sender, ExitEventArgs e)
        {
            if (this.Mutex != null)
            {
                this.Mutex.ReleaseMutex();
                this.Mutex.Close();
            }
        }

        /// <summary>
        /// System.Windows.Application.Startup イベント を発生させます。
        /// </summary>
        /// <param name="e">イベントデータ を格納している StartupEventArgs</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            this.notifyIcon = new NotifyIconWrapper();
        }

        /// <summary>
        /// System.Windows.Application.Exit イベント を発生させます。
        /// </summary>
        /// <param name="e">イベントデータ を格納している ExitEventArgs</param>
        protected override void OnExit(ExitEventArgs e)
        {
            this.notifyIcon.Dispose();
            base.OnExit(e);
        }
    }
}
