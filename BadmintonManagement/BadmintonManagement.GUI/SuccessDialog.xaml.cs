using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BadmintonManagement.GUI
{
    /// <summary>
    /// Interaction logic for SuccessDialog.xaml
    /// </summary>
    public partial class SuccessDialog : Window
    {
        public SuccessDialog(string maKH)
        {
            InitializeComponent();
            txtMaKH.Text = "Mã khách hàng của bạn là: " + maKH;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; // Đánh dấu là đã bấm OK
            this.Close();
        }
        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; // Trả về kết quả true để các form cha biết
            this.Close(); // Đóng thông báo
        }
        private void btnGoToLogin_Click(object sender, RoutedEventArgs e)
        {
            // Cách 1: Chỉ đóng cái thông báo này lại
            this.Close();

            // Cách 2: Nếu bạn muốn nó đóng thông báo VÀ đóng luôn cả form Đăng ký để về Login
            // (Giả sử bạn gọi dialog này từ RegisterWindow)
            // DialogResult = true; 
            // this.Close();
        }

    }
}
