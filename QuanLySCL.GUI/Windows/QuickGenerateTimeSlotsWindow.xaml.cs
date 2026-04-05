using QuanLySCL.BUS;
using QuanLySCL.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace QuanLySCL.GUI.Windows
{
    public partial class QuickGenerateTimeSlotsWindow : Window
    {
        private readonly ObservableCollection<CourtTypePriceRow> _rows = new ObservableCollection<CourtTypePriceRow>();

        public QuickGenerateTimeSlotsWindow()
        {
            InitializeComponent();

            Loaded += (_, __) => UpdateApplyAllUi();

            TxtStep.Text = "30";
            TxtOpen.Text = "06:00";
            TxtClose.Text = "22:00";

            LoadCourtTypes();
        }

        private void LoadCourtTypes()
        {
            try
            {
                var types = new CourtBUS().GetCourtTypes();
                _rows.Clear();
                foreach (var t in types)
                {
                    _rows.Add(new CourtTypePriceRow
                    {
                        CourtTypeId = t.Id,
                        CourtTypeName = t.Name,
                        LeNormal = 0,
                        LePeak = 0,
                        FixedNormal = 0,
                        FixedPeak = 0
                    });
                }

                GridCourtTypePrices.ItemsSource = _rows;
            }
            catch
            {
                GridCourtTypePrices.ItemsSource = _rows;
            }
        }

        private void UpdateApplyAllUi()
        {
            // During InitializeComponent, Checked/Unchecked can fire before PerTypeBorder is wired up.
            // So we guard against null here and set the correct state once the window is Loaded.
            if (PerTypeBorder == null) return;

            bool applyAll = ChkApplyAll?.IsChecked == true;
            PerTypeBorder.Visibility = applyAll ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ApplyAll_Checked(object sender, RoutedEventArgs e)
        {
            UpdateApplyAllUi();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseTime(TxtOpen.Text, out var openTime) ||
                !TryParseTime(TxtClose.Text, out var closeTime))
            {
                MessageBox.Show("Vui lòng nhập giờ mở/đóng cửa đúng định dạng (hh:mm).", "Lỗi nhập liệu");
                return;
            }

            if (!int.TryParse((TxtStep.Text ?? string.Empty).Trim(), out int stepMinutes) || stepMinutes <= 0)
            {
                stepMinutes = 30;
            }

            if (closeTime <= openTime)
            {
                MessageBox.Show("Giờ đóng cửa phải sau giờ mở cửa.", "Lỗi nhập liệu");
                return;
            }

            if (stepMinutes % 5 != 0)
            {
                MessageBox.Show("Bước phút nên là bội số của 5 (khuyến nghị 30).", "Lỗi nhập liệu");
                return;
            }

            TimeSpan? peakStart = null;
            TimeSpan? peakEnd = null;
            if (!string.IsNullOrWhiteSpace(TxtPeakStart.Text) || !string.IsNullOrWhiteSpace(TxtPeakEnd.Text))
            {
                if (!TryParseTime(TxtPeakStart.Text, out var ps) || !TryParseTime(TxtPeakEnd.Text, out var pe))
                {
                    MessageBox.Show("Giờ vàng không đúng định dạng (hh:mm).", "Lỗi nhập liệu");
                    return;
                }

                if (pe <= ps)
                {
                    MessageBox.Show("Giờ vàng: thời điểm kết thúc phải sau thời điểm bắt đầu.", "Lỗi nhập liệu");
                    return;
                }

                peakStart = ps;
                peakEnd = pe;
            }

            if (!TryParseMoney(TxtLeNormal.Text, out decimal leNormal) ||
                !TryParseMoney(TxtLePeak.Text, out decimal lePeak) ||
                !TryParseMoney(TxtFixedNormal.Text, out decimal fixedNormal) ||
                !TryParseMoney(TxtFixedPeak.Text, out decimal fixedPeak))
            {
                MessageBox.Show("Vui lòng nhập giá hợp lệ (số VNĐ).", "Lỗi nhập liệu");
                return;
            }

            bool applyAll = ChkApplyAll.IsChecked == true;
            List<CourtTypePricePlan> pricePlans = new List<CourtTypePricePlan>();

            bool hasPeakSlotsConfigured = peakStart.HasValue && peakEnd.HasValue;
            if (!hasPeakSlotsConfigured)
            {
                // If user doesn't define peak hours, keep peak prices equal to normal.
                if (lePeak <= 0) lePeak = leNormal;
                if (fixedPeak <= 0) fixedPeak = fixedNormal;
            }

            bool inferPrice = ChkInferPrice.IsChecked == true;
            PriceInferer inferer = null;
            if (inferPrice && (leNormal <= 0 || fixedNormal <= 0 || (hasPeakSlotsConfigured && (lePeak <= 0 || fixedPeak <= 0))))
            {
                try
                {
                    var adminBus = new AdminBUS();
                    var existingSlots = adminBus.GetAllTimeSlots()?.ToList() ?? new List<TimeSlot>();
                    var existingPrices = adminBus.GetAllPriceEntries()?.ToList() ?? new List<PriceEntry>();
                    inferer = new PriceInferer(existingSlots, existingPrices);
                }
                catch
                {
                    inferer = null;
                }
            }

            if (applyAll)
            {
                foreach (var r in _rows)
                {
                    decimal planLeNormal = leNormal;
                    decimal planFixedNormal = fixedNormal;
                    decimal planLePeak = lePeak;
                    decimal planFixedPeak = fixedPeak;

                    if (inferPrice && inferer != null)
                    {
                        if (planLeNormal <= 0)
                            planLeNormal = inferer.InferPrice(r.CourtTypeId, "Lẻ", isPeak: false, targetDurationMinutes: stepMinutes);
                        if (planFixedNormal <= 0)
                            planFixedNormal = inferer.InferPrice(r.CourtTypeId, "Cố định", isPeak: false, targetDurationMinutes: stepMinutes);

                        if (hasPeakSlotsConfigured)
                        {
                            if (planLePeak <= 0)
                                planLePeak = inferer.InferPrice(r.CourtTypeId, "Lẻ", isPeak: true, targetDurationMinutes: stepMinutes);
                            if (planFixedPeak <= 0)
                                planFixedPeak = inferer.InferPrice(r.CourtTypeId, "Cố định", isPeak: true, targetDurationMinutes: stepMinutes);
                        }
                        else
                        {
                            if (planLePeak <= 0) planLePeak = planLeNormal;
                            if (planFixedPeak <= 0) planFixedPeak = planFixedNormal;
                        }
                    }

                    if (planLeNormal <= 0 || planFixedNormal <= 0 || (hasPeakSlotsConfigured && (planLePeak <= 0 || planFixedPeak <= 0)))
                    {
                        MessageBox.Show(
                            "Không suy ra được giá từ bảng giá hiện có (thiếu dữ liệu nền).\n" +
                            "Vui lòng nhập giá mặc định hoặc tạo trước ít nhất 1 giá cho 'Lẻ' và 'Cố định'.",
                            "Thiếu dữ liệu giá",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    pricePlans.Add(new CourtTypePricePlan
                    {
                        CourtTypeId = r.CourtTypeId,
                        LeNormal = planLeNormal,
                        LePeak = planLePeak,
                        FixedNormal = planFixedNormal,
                        FixedPeak = planFixedPeak
                    });
                }
            }
            else
            {
                if (_rows.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy Loại sân để áp giá.", "Thiếu dữ liệu");
                    return;
                }

                // If per-type mode, allow empty cells -> fallback to default.
                foreach (var r in _rows)
                {
                    decimal planLeNormal = r.LeNormal > 0 ? r.LeNormal : leNormal;
                    decimal planLePeak = r.LePeak > 0 ? r.LePeak : lePeak;
                    decimal planFixedNormal = r.FixedNormal > 0 ? r.FixedNormal : fixedNormal;
                    decimal planFixedPeak = r.FixedPeak > 0 ? r.FixedPeak : fixedPeak;

                    if (!hasPeakSlotsConfigured)
                    {
                        if (planLePeak <= 0) planLePeak = planLeNormal;
                        if (planFixedPeak <= 0) planFixedPeak = planFixedNormal;
                    }

                    if (inferPrice && inferer != null)
                    {
                        if (planLeNormal <= 0)
                            planLeNormal = inferer.InferPrice(r.CourtTypeId, "Lẻ", isPeak: false, targetDurationMinutes: stepMinutes);
                        if (planFixedNormal <= 0)
                            planFixedNormal = inferer.InferPrice(r.CourtTypeId, "Cố định", isPeak: false, targetDurationMinutes: stepMinutes);
                        if (hasPeakSlotsConfigured)
                        {
                            if (planLePeak <= 0)
                                planLePeak = inferer.InferPrice(r.CourtTypeId, "Lẻ", isPeak: true, targetDurationMinutes: stepMinutes);
                            if (planFixedPeak <= 0)
                                planFixedPeak = inferer.InferPrice(r.CourtTypeId, "Cố định", isPeak: true, targetDurationMinutes: stepMinutes);
                        }
                        else
                        {
                            if (planLePeak <= 0) planLePeak = planLeNormal;
                            if (planFixedPeak <= 0) planFixedPeak = planFixedNormal;
                        }
                    }

                    if (planLeNormal <= 0 || planFixedNormal <= 0 || (hasPeakSlotsConfigured && (planLePeak <= 0 || planFixedPeak <= 0)))
                    {
                        MessageBox.Show(
                            $"Không suy ra được giá cho loại sân '{r.CourtTypeName}'.\n" +
                            "Vui lòng nhập giá cho dòng này hoặc nhập giá mặc định phía trên.",
                            "Thiếu dữ liệu giá",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    pricePlans.Add(new CourtTypePricePlan
                    {
                        CourtTypeId = r.CourtTypeId,
                        LeNormal = planLeNormal,
                        LePeak = planLePeak,
                        FixedNormal = planFixedNormal,
                        FixedPeak = planFixedPeak
                    });
                }
            }

            if (pricePlans.Any(p => p.LeNormal < 0 || p.LePeak < 0 || p.FixedNormal < 0 || p.FixedPeak < 0))
            {
                MessageBox.Show("Giá không được âm.", "Lỗi nhập liệu");
                return;
            }

            bool overwrite = ChkOverwrite.IsChecked == true;

            // Generate slots in memory
            List<TimeSlot> slots = new List<TimeSlot>();
            var step = TimeSpan.FromMinutes(stepMinutes);
            for (var t = openTime; t + step <= closeTime; t = t + step)
            {
                var end = t + step;
                bool isPeak = peakStart.HasValue && peakEnd.HasValue && t >= peakStart.Value && t < peakEnd.Value;

                string id = "CA" + t.ToString("hhmm");
                string name = $"{t:hh\\:mm} - {end:hh\\:mm}";

                slots.Add(new TimeSlot
                {
                    Id = id,
                    Name = name,
                    StartTime = t,
                    EndTime = end,
                    LaKhungGioVang = isPeak
                });
            }

            if (slots.Count == 0)
            {
                MessageBox.Show("Không tạo được ca nào. Vui lòng kiểm tra khung giờ và bước phút.", "Không có dữ liệu");
                return;
            }

            // Create price entries
            List<PriceEntry> prices = new List<PriceEntry>();
            foreach (var slot in slots)
            {
                foreach (var plan in pricePlans)
                {
                    bool isPeak = slot.LaKhungGioVang;
                    prices.Add(new PriceEntry
                    {
                        CourtTypeId = plan.CourtTypeId,
                        SlotId = slot.Id,
                        BookingType = "Lẻ",
                        Price = isPeak ? plan.LePeak : plan.LeNormal
                    });
                    prices.Add(new PriceEntry
                    {
                        CourtTypeId = plan.CourtTypeId,
                        SlotId = slot.Id,
                        BookingType = "Cố định",
                        Price = isPeak ? plan.FixedPeak : plan.FixedNormal
                    });
                }
            }

            var bus = new AdminBUS();
            if (!bus.UpsertTimeSlotsAndPrices(slots, prices, overwrite, out int slotsInserted, out int slotsUpdated, out int pricesInserted, out int pricesUpdated, out string error))
            {
                MessageBox.Show("Không thể tạo nhanh: " + (error ?? "Lỗi không xác định"), "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(
                $"Đã xử lý xong.\n" +
                $"- Ca giờ: thêm mới {slotsInserted}, cập nhật {slotsUpdated}\n" +
                $"- Bảng giá: thêm mới {pricesInserted}, cập nhật {pricesUpdated}",
                "Thành công",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        private static bool TryParseTime(string input, out TimeSpan time)
        {
            time = default;
            string s = (input ?? string.Empty).Trim();
            return TimeSpan.TryParseExact(s, "hh\\:mm", CultureInfo.InvariantCulture, out time);
        }

        private static bool TryParseMoney(string input, out decimal value)
        {
            value = 0;
            string s = (input ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(s))
            {
                value = 0;
                return true;
            }

            // Accept "150000", "150.000", "150,000"
            s = s.Replace(".", string.Empty).Replace(",", string.Empty);
            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }
    }

    internal sealed class PriceInferer
    {
        private readonly Dictionary<string, (int minutes, bool isPeak)> _slotInfoById;
        private readonly List<PricePoint> _points;

        internal PriceInferer(IReadOnlyList<TimeSlot> slots, IReadOnlyList<PriceEntry> prices)
        {
            _slotInfoById = new Dictionary<string, (int minutes, bool isPeak)>(StringComparer.OrdinalIgnoreCase);
            if (slots != null)
            {
                foreach (var s in slots)
                {
                    if (s == null || string.IsNullOrWhiteSpace(s.Id)) continue;
                    int minutes = (int)Math.Max(0, (s.EndTime - s.StartTime).TotalMinutes);
                    _slotInfoById[s.Id.Trim()] = (minutes, s.LaKhungGioVang);
                }
            }

            _points = new List<PricePoint>();
            if (prices != null)
            {
                foreach (var p in prices)
                {
                    if (p == null) continue;
                    if (string.IsNullOrWhiteSpace(p.CourtTypeId) || string.IsNullOrWhiteSpace(p.SlotId) || string.IsNullOrWhiteSpace(p.BookingType))
                        continue;

                    if (!_slotInfoById.TryGetValue(p.SlotId.Trim(), out var info)) continue;
                    if (info.minutes <= 0) continue;

                    _points.Add(new PricePoint
                    {
                        CourtTypeId = p.CourtTypeId.Trim(),
                        BookingType = p.BookingType.Trim(),
                        IsPeak = info.isPeak,
                        Minutes = info.minutes,
                        Price = p.Price
                    });
                }
            }
        }

        internal decimal InferPrice(string courtTypeId, string bookingType, bool isPeak, int targetDurationMinutes)
        {
            if (string.IsNullOrWhiteSpace(bookingType) || targetDurationMinutes <= 0) return 0;

            string ct = (courtTypeId ?? string.Empty).Trim();
            string bt = bookingType.Trim();

            // Prefer: same court type + booking type + same peak flag
            var preferred = _points
                .Where(p => p.BookingType.Equals(bt, StringComparison.OrdinalIgnoreCase) &&
                            p.CourtTypeId.Equals(ct, StringComparison.OrdinalIgnoreCase) &&
                            p.IsPeak == isPeak)
                .ToList();

            if (preferred.Count == 0)
            {
                // Relax peak match first
                preferred = _points
                    .Where(p => p.BookingType.Equals(bt, StringComparison.OrdinalIgnoreCase) &&
                                p.CourtTypeId.Equals(ct, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (preferred.Count == 0)
            {
                // Relax court type: same booking type + same peak flag
                preferred = _points
                    .Where(p => p.BookingType.Equals(bt, StringComparison.OrdinalIgnoreCase) && p.IsPeak == isPeak)
                    .ToList();
            }

            if (preferred.Count == 0)
            {
                // Final fallback: same booking type
                preferred = _points
                    .Where(p => p.BookingType.Equals(bt, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (preferred.Count == 0) return 0;

            // Compute median per-minute
            var perMinute = preferred
                .Select(p => p.Price / p.Minutes)
                .OrderBy(v => v)
                .ToList();

            decimal median = Median(perMinute);

            // If we asked for peak but only had non-peak data, apply a peak factor if we can infer one.
            if (isPeak && preferred.All(p => p.IsPeak == false))
            {
                decimal factor = TryGetPeakFactor(bt);
                if (factor > 0) median *= factor;
            }

            decimal price = median * targetDurationMinutes;
            return RoundToThousand(price);
        }

        private decimal TryGetPeakFactor(string bookingType)
        {
            var peak = _points.Where(p => p.BookingType.Equals(bookingType, StringComparison.OrdinalIgnoreCase) && p.IsPeak)
                              .Select(p => p.Price / p.Minutes)
                              .OrderBy(v => v).ToList();
            var normal = _points.Where(p => p.BookingType.Equals(bookingType, StringComparison.OrdinalIgnoreCase) && !p.IsPeak)
                                .Select(p => p.Price / p.Minutes)
                                .OrderBy(v => v).ToList();

            if (peak.Count == 0 || normal.Count == 0) return 0;

            decimal mPeak = Median(peak);
            decimal mNormal = Median(normal);
            if (mNormal <= 0) return 0;
            return mPeak / mNormal;
        }

        private static decimal Median(List<decimal> sorted)
        {
            if (sorted == null || sorted.Count == 0) return 0;
            int n = sorted.Count;
            if (n % 2 == 1) return sorted[n / 2];
            return (sorted[(n / 2) - 1] + sorted[n / 2]) / 2;
        }

        private static decimal RoundToThousand(decimal price)
        {
            if (price <= 0) return 0;
            return Math.Round(price / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m;
        }

        private sealed class PricePoint
        {
            public string CourtTypeId { get; set; }
            public string BookingType { get; set; }
            public bool IsPeak { get; set; }
            public int Minutes { get; set; }
            public decimal Price { get; set; }
        }
    }

    internal class CourtTypePriceRow
    {
        public string CourtTypeId { get; set; }
        public string CourtTypeName { get; set; }
        public decimal LeNormal { get; set; }
        public decimal LePeak { get; set; }
        public decimal FixedNormal { get; set; }
        public decimal FixedPeak { get; set; }
    }

    internal class CourtTypePricePlan
    {
        public string CourtTypeId { get; set; }
        public decimal LeNormal { get; set; }
        public decimal LePeak { get; set; }
        public decimal FixedNormal { get; set; }
        public decimal FixedPeak { get; set; }
    }
}
