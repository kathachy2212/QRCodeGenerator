using Microsoft.AspNetCore.Mvc;
using QRCodeGenerator.ViewModels;
using QRCoder;
using System.Drawing;
using System.Text.RegularExpressions;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Microsoft.AspNetCore.Authorization;
using PdfSharpCore.Drawing.BarCodes;
using System.Drawing.Imaging;
using System;
using BarcodeLib; 

namespace QRCodeGenerator.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(new QRCodeViewModel());
        }

        [HttpPost]
        public IActionResult Index(QRCodeViewModel model, string action)
        {
            if (action == "SetFieldCount" && model.FieldCount > 0)
            {
                model.Step = 2;
                model.FieldNames = new List<string>(new string[model.FieldCount]);
            }
            else if (action == "SetFieldNames")
            {
                model.Step = 3;
                model.FieldValues = new List<string>(new string[model.FieldCount]);
            }
            else if (action == "GenerateQRCode")
            {
                var lines = new List<string>();
                for (int i = 0; i < model.FieldCount; i++)
                {
                    string name = model.FieldNames?[i] ?? $"Field{i + 1}";
                    string value = model.FieldValues?[i] ?? "";
                    lines.Add($"{name}: {value}");
                }

                string content = string.Join("\n", lines);

                using var generator = new QRCoder.QRCodeGenerator();
                var qrCodeData = generator.CreateQrCode(content, QRCoder.QRCodeGenerator.ECCLevel.Q);

                var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(20);

                model.QRCodeImageBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
                model.Step = 4;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DownloadQRCodePdf(string QRCodeImageBase64)
        {
            try
            {
                // Strip the base64 header
                var base64Data = Regex.Replace(QRCodeImageBase64, @"^data:image\/[a-zA-Z]+;base64,", "");
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                using var stream = new MemoryStream();
                var document = new PdfDocument();
                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);

                using var imageStream = new MemoryStream(imageBytes);
                var image = XImage.FromStream(() => imageStream);

                // Define desired image size in points (1 point = 1/72 inch)
                double desiredWidth = 300;
                double desiredHeight = 300;

                // Center the image on the page
                double x = (page.Width - desiredWidth) / 2;
                double y = (page.Height - desiredHeight) / 2;

                // Draw the image
                gfx.DrawImage(image, x, y, desiredWidth, desiredHeight);

                document.Save(stream, false);
                return File(stream.ToArray(), "application/pdf", "QRCode.pdf");
            }
            catch (Exception ex)
            {
                return Content("Error generating PDF: " + ex.Message);
            }
        }


        public IActionResult Barcode()
        {
            return View(new BarcodeViewModel());
        }

        [HttpPost]
        public IActionResult Barcode(BarcodeViewModel model)
        {
            if (!string.IsNullOrEmpty(model.BarcodeText))
            {
                var barcode = new Barcode
                {
                    IncludeLabel = true,
                    Alignment = AlignmentPositions.CENTER,
                    LabelPosition = LabelPositions.BOTTOMCENTER
                };

                using (var image = barcode.Encode(TYPE.CODE128, model.BarcodeText, Color.Black, Color.White, 300, 100))
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Png);
                    model.BarcodeImageBase64 = $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
                }
            }

            return View(model);
        }

    }
}
