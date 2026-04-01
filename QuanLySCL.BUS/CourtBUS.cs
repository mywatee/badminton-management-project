using QuanLySCL.DAL;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;

namespace QuanLySCL.BUS
{
    public class CourtBUS
    {
        private readonly CourtDAL _courtDal = new CourtDAL();

        public ObservableCollection<Court> GetAllCourts()
        {
            return _courtDal.GetAllCourts();
        }

        public ObservableCollection<Court> GetAvailableCourts()
        {
            var allCourts = _courtDal.GetAllCourts();
            var availableCourts = new ObservableCollection<Court>();

            foreach (var court in allCourts)
            {
                if (court.Status == "Available")
                    availableCourts.Add(court);
            }

            return availableCourts;
        }

        public bool UpdateStatus(string courtId, string newStatus)
        {
            if (string.IsNullOrWhiteSpace(courtId))
                return false;

            // Placeholder: implement real maintenance checks if needed.
            if (newStatus == "Available" && IsUnderMaintenance(courtId))
                return false;

            int result = _courtDal.UpdateCourtStatus(courtId.Trim(), newStatus);
            return result > 0;
        }

        private bool IsUnderMaintenance(string courtId)
        {
            return false;
        }

        // Admin features
        public ObservableCollection<CourtType> GetCourtTypes()
        {
            return _courtDal.GetCourtTypes();
        }

        public (bool ok, string courtId, string error) CreateCourtAutoId(string courtName, string courtTypeId, string statusEn = "Available")
        {
            if (string.IsNullOrWhiteSpace(courtName))
                return (false, null, "Tên sân không được để trống.");

            if (string.IsNullOrWhiteSpace(courtTypeId))
                return (false, null, "Vui lòng chọn loại sân.");

            for (int attempt = 0; attempt < 3; attempt++)
            {
                string nextId = _courtDal.GetNextCourtId();
                try
                {
                    int rows = _courtDal.CreateCourt(nextId, courtName.Trim(), courtTypeId.Trim(), statusEn);
                    if (rows > 0)
                        return (true, nextId, null);
                }
                catch (Exception ex)
                {
                    // Best-effort retry for concurrent inserts.
                    if (ex.Message.IndexOf("PRIMARY KEY", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    return (false, null, ex.Message);
                }
            }

            return (false, null, "Không thể tạo mã sân mới. Vui lòng thử lại.");
        }

        public bool UpdateCourt(string courtId, string courtName, string courtTypeId, string statusEn, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(courtId)) { error = "Mã sân không hợp lệ."; return false; }
            if (string.IsNullOrWhiteSpace(courtName)) { error = "Tên sân không được để trống."; return false; }
            if (string.IsNullOrWhiteSpace(courtTypeId)) { error = "Vui lòng chọn loại sân."; return false; }

            try
            {
                int rows = _courtDal.UpdateCourt(courtId.Trim(), courtName.Trim(), courtTypeId.Trim(), statusEn);
                return rows > 0;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool DeleteCourt(string courtId, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(courtId)) { error = "Mã sân không hợp lệ."; return false; }

            try
            {
                int rows = _courtDal.DeleteCourt(courtId.Trim());
                return rows > 0;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
