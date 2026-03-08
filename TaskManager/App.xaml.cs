using System.Windows;

namespace TaskManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Microsoft.Data.Sqlite が必要とする SQLite プロバイダーを明示的に設定
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
            base.OnStartup(e);
        }
    }
}
