using System.Windows;
using QuanLySCL.GUI.Windows; // Import namespace chứa LoginWindow

namespace QuanLySCL.GUI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Tránh việc ứng dụng tự tắt khi LoginWindow (cửa sổ đầu tiên) đóng lại
            // trước khi kịp mở MainWindow.
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = new LoginWindow(); // Phải thuộc namespace Windows
            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                // Truyền thông tin user vào MainWindow nếu cần
                var mainWindow = new MainWindow(loginWindow.Username, loginWindow.Role, loginWindow.CustomerId);
                // Đặt MainWindow chính thức cho ứng dụng
                this.MainWindow = mainWindow;
                mainWindow.Show();

                // Sau khi đã có MainWindow, cho phép tắt app khi MainWindow đóng
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            else
            {
                this.Shutdown();
            }
        }
    }
}