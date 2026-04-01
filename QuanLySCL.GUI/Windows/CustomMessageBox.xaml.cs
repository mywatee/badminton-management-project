using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox(string message, string type = "success")
        {
            InitializeComponent();
            txtMessage.Text = message;

            // Đổi icon tùy loại thông báo
            if (type == "error")
            {
                txtIcon.Text = "❌";
                btnOK.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#d63031");
            }
            else if (type == "warning")
            {
                txtIcon.Text = "⚠️";
                btnOK.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#f1c40f");
            }
            else
            {
                txtIcon.Text = "✅";
                btnOK.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#2ecc71");
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Hàm tĩnh để gọi nhanh từ các màn hình khác
        public static void Show(string message, string type = "success")
        {
            var msg = new CustomMessageBox(message, type);
            msg.ShowDialog();
        }
    }
}