using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using QuanLySCL.Models;

namespace QuanLySCL.GUI.Helpers
{
    public static class PrintHelper
    {
        public static void PrintInvoice(Invoice invoice, Booking booking, System.Collections.Generic.IEnumerable<BookingServiceDetail> services)
        {
            FlowDocument doc = new FlowDocument
            {
                PagePadding = new Thickness(50),
                FontFamily = new FontFamily("Segoe UI"),
                ColumnWidth = double.PositiveInfinity
            };

            // Header
            doc.Blocks.Add(new Paragraph(new Run("QUẢN LÝ SÂN CẦU LÔNG SCL")) { 
                FontSize = 24, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
            doc.Blocks.Add(new Paragraph(new Run("123 Đường Cầu Lông, P. Thể Thao, TP. HCM")) { 
                FontSize = 12, TextAlignment = TextAlignment.Center, Foreground = Brushes.Gray });
            doc.Blocks.Add(new Paragraph(new Run("ĐT: 0123 456 789")) { 
                FontSize = 12, TextAlignment = TextAlignment.Center, Foreground = Brushes.Gray });

            doc.Blocks.Add(new Section()); // Spacer

            // Invoice Info
            doc.Blocks.Add(new Paragraph(new Run("HÓA ĐƠN THANH TOÁN")) { 
                FontSize = 18, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 20, 0, 10) });
            
            Paragraph info = new Paragraph();
            info.Inlines.Add(new Run($"Mã HĐ: {invoice.Id}\n"));
            info.Inlines.Add(new Run($"Ngày: {invoice.IssuedAt:dd/MM/yyyy HH:mm}\n"));
            info.Inlines.Add(new Run($"Khách hàng: {booking.Customer}\n"));
            info.Inlines.Add(new Run($"Sân: {booking.Court} ({booking.Time})"));
            doc.Blocks.Add(info);

            // Table for items
            Table table = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 1, 0, 1) };
            table.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            TableRowGroup group = new TableRowGroup();
            
            // Header Row
            TableRow header = new TableRow();
            header.Cells.Add(new TableCell(new Paragraph(new Run("Nội dung")) { FontWeight = FontWeights.Bold }));
            header.Cells.Add(new TableCell(new Paragraph(new Run("SL")) { FontWeight = FontWeights.Bold }));
            header.Cells.Add(new TableCell(new Paragraph(new Run("Thành tiền")) { FontWeight = FontWeights.Bold }));
            group.Rows.Add(header);

            // Court Fee
            TableRow courtRow = new TableRow();
            courtRow.Cells.Add(new TableCell(new Paragraph(new Run("Tiền sân"))));
            courtRow.Cells.Add(new TableCell(new Paragraph(new Run("1"))));
            courtRow.Cells.Add(new TableCell(new Paragraph(new Run($"{invoice.CourtFee:N0}"))));
            group.Rows.Add(courtRow);

            // Services
            foreach (var svc in services)
            {
                TableRow row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(svc.ServiceName))));
                row.Cells.Add(new TableCell(new Paragraph(new Run(svc.Quantity.ToString()))));
                row.Cells.Add(new TableCell(new Paragraph(new Run($"{svc.Total:N0}"))));
                group.Rows.Add(row);
            }

            table.RowGroups.Add(group);
            doc.Blocks.Add(table);

            // Totals
            Paragraph totals = new Paragraph { TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
            totals.Inlines.Add(new Run($"Tổng cộng: {invoice.CourtFee + invoice.ServiceFee:N0}\n"));
            totals.Inlines.Add(new Run($"Giảm giá: -{invoice.Discount:N0}\n"));
            totals.Inlines.Add(new Run($"THANH TOÁN: {(invoice.CourtFee + invoice.ServiceFee - invoice.Discount):N0}") { 
                FontSize = 16, FontWeight = FontWeights.Bold });
            doc.Blocks.Add(totals);

            doc.Blocks.Add(new Paragraph(new Run("Cảm ơn quý khách! Hẹn gặp lại.")) { 
                TextAlignment = TextAlignment.Center, FontStyle = FontStyles.Italic, Margin = new Thickness(0, 40, 0, 0) });

            // Print
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                IDocumentPaginatorSource idp = doc;
                pd.PrintDocument(idp.DocumentPaginator, "In Hóa Đơn SCL");
            }
        }
    }
}
