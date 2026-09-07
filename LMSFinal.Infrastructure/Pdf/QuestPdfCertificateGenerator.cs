using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Certificates;
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
        static QuestPdfCertificateGenerator()
        {

            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] Generate(CertificateDto certificate)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontFamily("Arial"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(18);

                        column.Item().AlignCenter().PaddingTop(20)
                            .Text("CERTIFICATE OF COMPLETION")
                            .FontSize(30).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().AlignCenter()
                            .Text("This certifies that")
                            .FontSize(14).FontColor(Colors.Grey.Darken1);

                        column.Item().AlignCenter()
                            .Text(certificate.StudentName)
                            .FontSize(26).Bold();

                        column.Item().AlignCenter()
                            .Text("has successfully completed the course")
                            .FontSize(14).FontColor(Colors.Grey.Darken1);

                        column.Item().AlignCenter()
                            .Text(certificate.CourseTitle)
                            .FontSize(22).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().AlignCenter()
                            .Text($"Instructor: {certificate.InstructorName}")
                            .FontSize(12).FontColor(Colors.Grey.Darken1);

                        column.Item().AlignCenter()
                            .Text(certificate.CompletionDate.ToString("MMMM d, yyyy"))
                            .FontSize(12);

                        column.Item().PaddingTop(40).AlignCenter()
                            .Text($"Certificate ID: {certificate.CertificateNumber}")
                            .FontSize(11).FontColor(Colors.Grey.Darken2);

                        column.Item().AlignCenter()
                            .Text("Verify at lmsfinal.example.com/certificates/verify")
                            .FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }

}
