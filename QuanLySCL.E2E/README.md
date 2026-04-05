# QuanLySCL.E2E (Playwright)

Mục tiêu: chạy E2E test bằng Playwright và xuất report ra Excel dựa trên template `TemplateTest.xlsx`.

## Chuẩn bị

1) Cài browser cho Playwright (1 lần/mỗi máy):

```powershell
cd QuanLySCL.E2E
dotnet build
dotnet playwright install
```

2) Thiết lập biến môi trường (tối thiểu `E2E_BASE_URL`):

```powershell
$env:E2E_BASE_URL="http://localhost:3000"
$env:E2E_WORD_SPEC="C:\\Users\\ASUS\\Desktop\\chucnang.docx"
$env:E2E_EXCEL_TEMPLATE="C:\\Users\\ASUS\\Desktop\\TemplateTest.xlsx"
$env:E2E_REPEAT="1"
```

## Chạy test

```powershell
dotnet test .\\QuanLySCL.E2E\\QuanLySCL.E2E.csproj
```

Report Excel sẽ được tạo trong `TestResults\\QuanLySCL_Playwright_Report_*.xlsx`.

