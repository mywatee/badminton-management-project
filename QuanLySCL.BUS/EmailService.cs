using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace QuanLySCL.BUS
{
    public class EmailService
    {
        // QUAN TRỌNG: API Key của bạn từ Brevo
        private const string ApiKey = "";
        private const string SenderEmail = "srqkendra961@gmail.com"; // Thay bằng email bạn đã đăng ký với Brevo
        private const string SenderName = "Hệ Thống Quản lý Sân Cầu Lông";

        public async Task<(bool success, string errorMsg)> SendOTPAsync(string receiverEmail, string otpCode)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("api-key", ApiKey);

                    var payload = new
                    {
                        sender = new { email = SenderEmail, name = SenderName },
                        to = new[] { new { email = receiverEmail } },
                        subject = "Mã xác thực OTP - QuanLySCL",
                        htmlContent = $@"
                            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #e2e8f0; border-radius: 10px;'>
                                <h2 style='color: #0984e3;'>Mã xác thực OTP</h2>
                                <p>Chào bạn,</p>
                                <p>Mã xác thực của bạn để đăng ký/đổi mật khẩu là:</p>
                                <div style='font-size: 24px; font-weight: bold; color: #10b981; margin: 20px 0;'>{otpCode}</div>
                                <p>Mã này có hiệu lực trong vòng 5 phút. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
                                <hr style='border: 0; border-top: 1px solid #eee;' />
                                <p style='font-size: 12px; color: #64748B;'>Đây là tin nhắn tự động từ hệ thống Quản Lý Sân Cầu Lông qua Brevo REST API.</p>
                            </div>"
                    };

                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    string jsonPayload = JsonSerializer.Serialize(payload, options);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        string errorDetail = await response.Content.ReadAsStringAsync();
                        return (false, $"Brevo API Error: {response.StatusCode} - {errorDetail}");
                    }

                    return (true, "Success");
                }
            }
            catch (System.Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }
    }
}