namespace HospitalStatistics.Core.Services.Interfaces.Entities
{
    public interface IFileExcelService
    {
        /// <summary>
        /// Сохраняет файл в директорию, указанную в Settings.settings (ExcelReportsDirectory)
        /// </summary>
        /// <param name="fileName">Имя файла (например, Отчет_7.xlsx)</param>
        /// <param name="bytes">Содержимое файла</param>
        /// <returns>Полный путь к сохранённому файлу или null, если ошибка</returns>
        string SaveReportToDefaultDirectory(string fileName, byte[] bytes);

        /// <summary>
        /// Показывает диалог "Сохранить как" и возвращает выбранный путь или null, если отменено.
        /// </summary>
        string ShowSaveFileDialog(string defaultFileName, string filter = "Excel files (*.xlsx)|*.xlsx");

        /// <summary>
        /// Сохраняет файл по указанному пути.
        /// </summary>
        bool SaveFile(string path, byte[] bytes);

        /// <summary>
        /// Отправляет файл на печать через shell.
        /// </summary>
        bool PrintFile(string path);

    }
}
