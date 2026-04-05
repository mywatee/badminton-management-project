using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace QuanLySCL.GUI.Windows
{
    public partial class CourtActionWindow : Window
    {
        public string ResultAction { get; private set; } = "Cancel";

        public CourtActionWindow(string courtName, string timeLabel)
        {
            InitializeComponent();
            txtTitle.Text = $"{courtName} - {timeLabel}";
        }

        public void SetupView(string statusKey)
        {
            if (statusKey == "Booked")
            {
                // Left button: Check-In
                txtLeft.Text = "NHẬN SÂN";
                iconLeft.Kind = PackIconKind.CheckCircleOutline;
                btnLeft.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#F0FDF4");
                btnLeft.BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#A3E635");
                iconLeft.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#166534");
                txtLeft.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#166534");

                // Right button: Cancel
                txtRight.Text = "HỦY LỊCH";
                iconRight.Kind = PackIconKind.CalendarRemove;
                btnRight.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FFF7ED");
                btnRight.BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FB923C");
                iconRight.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#C2410C");
                txtRight.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#C2410C");
            }
            else // InUse
            {
                // Left button: Service
                txtLeft.Text = "DỊCH VỤ";
                iconLeft.Kind = PackIconKind.BeerOutline;
                
                // Right button: Check-Out
                txtRight.Text = "THANH TOÁN";
                iconRight.Kind = PackIconKind.ReceiptTextCheckOutline;
            }
        }

        private void Service_Click(object sender, RoutedEventArgs e)
        {
            ResultAction = "Service";
            DialogResult = true;
            Close();
        }

        private void CheckOut_Click(object sender, RoutedEventArgs e)
        {
            ResultAction = "CheckOut";
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ResultAction = "Cancel";
            DialogResult = false;
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
