using QuanLySCL.GUI.Templates;
using QuanLySCL.GUI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace QuanLySCL.GUI.Services
{
    public class PrintService
    {
        public static void PrintReceipt(ReceiptViewModel viewModel)
        {
            var template = new ReceiptTemplate { DataContext = viewModel };
            
            // Layout to fixed size
            template.Measure(new Size(300, double.PositiveInfinity));
            template.Arrange(new Rect(new Point(0, 0), template.DesiredSize));
            template.UpdateLayout();

            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Thermal printers often have variable height. 
                // We print the template as a visual.
                printDialog.PrintVisual(template, $"Invoice_{viewModel.InvoiceId}");
            }
        }
    }
}
