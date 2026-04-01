using QuanLySCL.GUI.ViewModels;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class AddServiceWindow : Window
    {
        public AddServiceWindow(string bookingId)
        {
            InitializeComponent();
            var vm = new AddServiceViewModel(bookingId);
            vm.RequestClose += (s, result) => { 
                this.DialogResult = result;
                this.Close(); 
            };
            DataContext = vm;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
