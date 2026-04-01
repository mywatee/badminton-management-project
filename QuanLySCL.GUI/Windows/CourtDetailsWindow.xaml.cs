using QuanLySCL.GUI.ViewModels;
using QuanLySCL.Models;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class CourtDetailsWindow : Window
    {
        public CourtDetailsWindow(Court court)
        {
            InitializeComponent();
            DataContext = new CourtDetailsViewModel(court);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

