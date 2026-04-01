using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class UpsertStaffWindow : Window
    {
        private readonly StaffBUS _bus = new StaffBUS();
        private readonly bool _isEdit;
        private readonly Staff _editing;

        public string HeaderText { get; set; }
        public string StaffId { get; set; }

        public string StaffName { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }

        public string ErrorText { get; set; }

        public UpsertStaffWindow(Staff editing = null)
        {
            InitializeComponent();

            _editing = editing;
            _isEdit = editing != null;

            if (_isEdit)
            {
                HeaderText = "Sửa nhân viên";
                StaffId = editing.Id;
                StaffName = editing.Name;
                Phone = editing.Phone;
                Role = editing.Role;
            }
            else
            {
                HeaderText = "Thêm nhân viên";
                StaffId = "(Tự sinh)";
                StaffName = string.Empty;
                Phone = string.Empty;
                Role = string.Empty;
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

            if (!_isEdit)
            {
                var res = _bus.CreateStaffAutoId(StaffName, Phone, email: null, Role, department: null, status: null, joinDate: DateTime.Today);
                if (!res.ok)
                {
                    ErrorText = res.error ?? "Không thể thêm nhân viên.";
                    Refresh();
                    return;
                }

                DialogResult = true;
                Close();
                return;
            }

            if (_editing == null)
            {
                ErrorText = "Thiếu dữ liệu để sửa.";
                Refresh();
                return;
            }

            var staff = new Staff
            {
                Id = _editing.Id,
                Name = StaffName,
                Phone = Phone,
                Role = Role,
                Department = "Chung",
                Status = "Active",
                JoinDate = DateTime.Today,
                Email = string.Empty
            };

            var upd = _bus.UpdateStaff(staff);
            if (!upd.ok)
            {
                ErrorText = upd.error ?? "Không thể cập nhật nhân viên.";
                Refresh();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Refresh()
        {
            DataContext = null;
            DataContext = this;
        }
    }
}
