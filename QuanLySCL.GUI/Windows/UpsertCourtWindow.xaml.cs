using QuanLySCL.BUS;
using QuanLySCL.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class UpsertCourtWindow : Window
    {
        private readonly CourtBUS _courtBus = new CourtBUS();
        private readonly bool _isEdit;
        private readonly Court _editingCourt;

        public string HeaderText { get; set; }
        public string CourtId { get; set; }
        public string CourtName { get; set; }

        public ObservableCollection<CourtType> CourtTypes { get; set; } = new ObservableCollection<CourtType>();
        public CourtType SelectedCourtType { get; set; }

        public ObservableCollection<string> StatusOptions { get; set; } =
            // DB now supports: Sẵn sàng / Bảo trì / Đang sử dụng.
            new ObservableCollection<string> { "Available", "In-use", "Maintenance" };

        public string SelectedStatus { get; set; } = "Available";

        public string ErrorText { get; set; }

        public UpsertCourtWindow(ObservableCollection<CourtType> courtTypes, Court editingCourt = null)
        {
            InitializeComponent();

            CourtTypes = courtTypes ?? _courtBus.GetCourtTypes();
            _editingCourt = editingCourt;
            _isEdit = editingCourt != null;

            if (_isEdit)
            {
                HeaderText = "Sửa sân";
                CourtId = editingCourt.Id;
                CourtName = editingCourt.Name;
                SelectedStatus = editingCourt.Status ?? "Available";

                // Court.Type holds display name (LoaiSan). Match by name.
                SelectedCourtType = CourtTypes.FirstOrDefault(t => t.Name == editingCourt.Type) ?? CourtTypes.FirstOrDefault();
            }
            else
            {
                HeaderText = "Thêm sân";
                CourtId = "(Tự sinh)";
                CourtName = string.Empty;
                SelectedCourtType = CourtTypes.FirstOrDefault();
                SelectedStatus = "Available";
            }

            DataContext = this;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorText = null;

            if (SelectedCourtType == null)
            {
                ErrorText = "Vui lòng chọn loại sân.";
                DataContext = null;
                DataContext = this;
                return;
            }

            if (!_isEdit)
            {
                var res = _courtBus.CreateCourtAutoId(CourtName, SelectedCourtType.Id, SelectedStatus);
                if (!res.ok)
                {
                    ErrorText = res.error ?? "Không thể thêm sân.";
                    DataContext = null;
                    DataContext = this;
                    return;
                }

                DialogResult = true;
                Close();
                return;
            }

            if (_editingCourt == null)
            {
                ErrorText = "Không có dữ liệu sân để sửa.";
                DataContext = null;
                DataContext = this;
                return;
            }

            if (!_courtBus.UpdateCourt(_editingCourt.Id, CourtName, SelectedCourtType.Id, SelectedStatus, out string error))
            {
                ErrorText = error ?? "Không thể cập nhật sân.";
                DataContext = null;
                DataContext = this;
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
