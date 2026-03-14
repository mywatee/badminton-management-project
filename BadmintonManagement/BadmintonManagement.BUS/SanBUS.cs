using BadmintonManagement.DAL;
using BadmintonManagement.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace BadmintonManagement.BUS
{
    public class SanBUS
    {
        private SanDAL _sanDAL = new SanDAL();

        public List<SanDTO> LayTatCaSan() // Đổi tên hàm thành LayTatCaSan
        {
            return _sanDAL.GetAllSan();
        }
    }
}
