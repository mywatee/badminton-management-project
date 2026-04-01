using System;
using System.Windows.Input;
using System.Web;

namespace QuanLySCL.GUI.ViewModels
{
    public class VietQRViewModel : BaseViewModel
    {
        private string _qrCodeUrl = string.Empty;
        public string QrCodeUrl
        {
            get => _qrCodeUrl;
            set => SetProperty(ref _qrCodeUrl, value);
        }

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        private string _orderDescription = string.Empty;
        public string OrderDescription
        {
            get => _orderDescription;
            set => SetProperty(ref _orderDescription, value);
        }

        private string _bankName = "MBBank";
        public string BankName
        {
            get => _bankName;
            set => SetProperty(ref _bankName, value);
        }

        private string _accountNumber = "0358448073";
        public string AccountNumber
        {
            get => _accountNumber;
            set => SetProperty(ref _accountNumber, value);
        }

        private string _accountName = "PHAM LE HUY HOANG";
        public string AccountName
        {
            get => _accountName;
            set => SetProperty(ref _accountName, value);
        }

        public bool IsConfirmed { get; private set; } = false;

        public event Action? RequestClose;

        public ICommand ConfirmPaymentCommand { get; }
        public ICommand CancelPaymentCommand { get; }

        public VietQRViewModel()
        {
            ConfirmPaymentCommand = new RelayCommand(ConfirmPayment);
            CancelPaymentCommand = new RelayCommand(CancelPayment);
        }

        public void Initialize(decimal amount, string description)
        {
            TotalAmount = amount;
            OrderDescription = description;

            // Generate VietQR URL using img.vietqr.io API
            string cleanDesc = HttpUtility.UrlEncode(description);
            string cleanName = HttpUtility.UrlEncode(AccountName);
            
            QrCodeUrl = $"https://img.vietqr.io/image/MB-{AccountNumber}-compact2.png?amount={(long)amount}&addInfo={cleanDesc}&accountName={cleanName}";
        }

        private void ConfirmPayment(object obj)
        {
            IsConfirmed = true;
            RequestClose?.Invoke();
        }

        private void CancelPayment(object obj)
        {
            IsConfirmed = false;
            RequestClose?.Invoke();
        }
    }
}
