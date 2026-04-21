using System.Threading.Tasks;

namespace MedProject.Services.Interfaces;

public interface IReportWindowService
{
    Task ShowPreviewAsync(byte[] pdfBytes, string reportTitle);
}
