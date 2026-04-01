using System.Windows;
using QuanLySCL.GUI.ViewModels;

namespace QuanLySCL.GUI.Views
{
    public partial class VietQRWindow : Window
    {
        public VietQRViewModel ViewModel { get; }

        public VietQRWindow(decimal amount, string description)
        {
            InitializeComponent();
            ViewModel = new VietQRViewModel();
            DataContext = ViewModel;

            ViewModel.Initialize(amount, description);
            ViewModel.RequestClose += () => {
                this.DialogResult = ViewModel.IsConfirmed;
                this.Close();
            };
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }
    }
}
