using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Certificates;
using LMSFinal.Infrastructure.Email;
using Microsoft.Extensions.Options;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Infrastructure.Pdf
{

    public class QuestPdfCertificateGenerator : ICertificatePdfGenerator
    {
        private readonly EmailSettings _emailSettings;

        static QuestPdfCertificateGenerator()
        {

            QuestPDF.Settings.License = LicenseType.Community;
        }

        public QuestPdfCertificateGenerator(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public byte[] Generate(CertificateDto certificate)
        {
            var verifyUrl = $"{_emailSettings.ClientBaseUrl}/pages/verify-certificate.html?number={Uri.EscapeDataString(certificate.CertificateNumber)}";
            var qrCodeBytes = GenerateQrCode(verifyUrl);
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontFamily("Arial"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().AlignCenter().PaddingTop(6)
                            .Text("CERTIFICATE OF COMPLETION")
                            .FontSize(28).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().AlignCenter()
                            .Text("This certifies that")
                            .FontSize(13).FontColor(Colors.Grey.Darken1);

                        column.Item().AlignCenter()
                            .Text(certificate.StudentName)
                            .FontSize(24).Bold();

                        column.Item().AlignCenter()
                            .Text("has successfully completed the course")
                            .FontSize(13).FontColor(Colors.Grey.Darken1);

                        column.Item().AlignCenter()
                            .Text(certificate.CourseTitle)
                            .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().AlignCenter()
                            .Text($"Instructor: {certificate.InstructorName}")
                            .FontSize(11).FontColor(Colors.Grey.Darken1);

                        column.Item().AlignCenter()
                            .Text(certificate.CompletionDate.ToString("MMMM d, yyyy"))
                            .FontSize(11);

                        column.Item().PaddingTop(12).AlignCenter()
                            .Text($"Certificate ID: {certificate.CertificateNumber}")
                            .FontSize(10).FontColor(Colors.Grey.Darken2);

                        // QR ведёт прямо на страницу верификации с уже подставленным номером —
                        // тому, кто проверяет сертификат (например, работодателю), не нужно
                        // вручную набирать длинный номер на другом устройстве.
                        column.Item().AlignCenter().Width(70).Image(qrCodeBytes);

                        column.Item().AlignCenter()
                            .Text("Scan to verify, or visit the link below")
                            .FontSize(8).FontColor(Colors.Grey.Medium);

                        column.Item().AlignCenter()
                            .Text(verifyUrl)
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static byte[] GenerateQrCode(string content)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
            var pngQrCode = new PngByteQRCode(data);
            return pngQrCode.GetGraphic(20);
        }
    }

}
