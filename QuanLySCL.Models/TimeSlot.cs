using System;

namespace QuanLySCL.Models
{
    public class TimeSlot
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool LaKhungGioVang { get; set; }

        public string DisplayLabel => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }
}
