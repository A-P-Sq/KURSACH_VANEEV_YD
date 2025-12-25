using System;
using System.Data;
using System.Windows;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AutoShopCoursework
{
    public partial class ReportWindow : Window
    {
        private DataTable reportData;

        public ReportWindow()
        {
            InitializeComponent();
            LoadReportData();
        }

        private void LoadReportData()
        {
            try
            {
                // Используем уже готовую функцию из вашей БД
                string sql = "SELECT * FROM public.get_sales_with_details() ORDER BY sale_date DESC";
                reportData = DbHelper.GetTable(sql);
                dgReport.ItemsSource = reportData.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private void BtnExportWord_Click(object sender, RoutedEventArgs e)
        {
            if (reportData == null || reportData.Rows.Count == 0) return;

            var saveDialog = new Microsoft.Win32.SaveFileDialog();
            saveDialog.Filter = "Документы Word (*.docx)|*.docx";
            saveDialog.FileName = $"Отчет_Продажи_{DateTime.Now:ddMMyyyy}.docx";

            if (saveDialog.ShowDialog() == true)
            {
                CreateWordDocument(saveDialog.FileName);
                MessageBox.Show("Отчет успешно сохранен!");
            }
        }

        private void CreateWordDocument(string filePath)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Заголовок
                AddTitle(body, "ОТЧЁТ ПО ПРОДАЖАМ АВТОСАЛОНА");
                AddParagraph(body, $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}");
                body.Append(new Paragraph(new Run(new Break()))); // Пропуск строки

                // Создание таблицы
                Table table = new Table();
                table.AppendChild(CreateTableProperties());

                // Шапка таблицы (названия колонок из вашего get_sales_with_details)
                TableRow headerRow = new TableRow();
                AddCell(headerRow, "ID", "800", true);
                AddCell(headerRow, "Клиент", "3000", true);
                AddCell(headerRow, "Автомобиль", "3000", true);
                AddCell(headerRow, "Дата продажи", "2000", true);
                AddCell(headerRow, "Сумма", "1500", true);
                table.Append(headerRow);

                // Данные
                decimal totalSum = 0;
                foreach (DataRow row in reportData.Rows)
                {
                    TableRow dataRow = new TableRow();
                    AddCell(dataRow, row["sale_id"].ToString(), "800");
                    AddCell(dataRow, row["client_fullname"].ToString(), "3000");
                    AddCell(dataRow, row["car_info"].ToString(), "3000");

                    DateTime date = Convert.ToDateTime(row["sale_date"]);
                    AddCell(dataRow, date.ToString("dd.MM.yyyy"), "2000");

                    decimal price = Convert.ToDecimal(row["price_sold"]);
                    AddCell(dataRow, price.ToString("N2") + " ₽", "1500");

                    totalSum += price;
                    table.Append(dataRow);
                }

                body.Append(table);

                // Итоговая сумма
                AddParagraph(body, $"\nИтого продано автомобилей: {reportData.Rows.Count}");
                AddParagraph(body, $"Общая выручка: {totalSum:N2} ₽", true);

                mainPart.Document.Save();
            }
        }

        // Вспомогательные методы для чистоты кода
        private void AddTitle(Body body, string text)
        {
            Paragraph p = body.AppendChild(new Paragraph());
            ParagraphProperties pp = p.AppendChild(new ParagraphProperties(new Justification() { Val = JustificationValues.Center }));
            Run r = p.AppendChild(new Run(new Text(text)));
            r.RunProperties = new RunProperties(new Bold(), new FontSize() { Val = "36" });
        }

        private void AddParagraph(Body body, string text, bool isBold = false)
        {
            Paragraph p = body.AppendChild(new Paragraph());
            Run r = p.AppendChild(new Run(new Text(text)));
            if (isBold) r.RunProperties = new RunProperties(new Bold());
        }

        private void AddCell(TableRow row, string text, string width, bool isHeader = false)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(new Text(text))));
            if (isHeader) cell.GetFirstChild<Paragraph>().GetFirstChild<Run>().RunProperties = new RunProperties(new Bold());
            cell.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = width }));
            row.Append(cell);
        }

        private TableProperties CreateTableProperties()
        {
            TableProperties tp = new TableProperties(new TableBorders(
                new TopBorder() { Val = BorderValues.Single, Size = 4 },
                new BottomBorder() { Val = BorderValues.Single, Size = 4 },
                new LeftBorder() { Val = BorderValues.Single, Size = 4 },
                new RightBorder() { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 2 },
                new InsideVerticalBorder() { Val = BorderValues.Single, Size = 2 }
            ));
            return tp;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}