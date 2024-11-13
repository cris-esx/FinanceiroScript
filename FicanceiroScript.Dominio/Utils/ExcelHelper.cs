using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.Globalization;
using FinanceiroScript.Dominio;

public class ExcelHelper
{
    public static bool IsNFSeValid(NFSe nfseData, string excelFilePath)
    {
        if (nfseData == null) return false;
        if (string.IsNullOrEmpty(excelFilePath)) return false;

        try
        {
            if (string.IsNullOrEmpty(nfseData.Prestador.Cnpj) || string.IsNullOrEmpty(nfseData.DataCompetencia))
            {
                throw new ArgumentException("CNPJ e Competência são necessários para a busca.");
            }

            IWorkbook workbook = LoadExcelFile(excelFilePath);

            var sheet = workbook.GetSheetAt(0) ?? throw new Exception("Não foi possível acessar a planilha no arquivo Excel.");

            int cnpjColumnIndex = GetColumnIndexByTitle(sheet, "CNPJ");
            int competenciaColumnIndex = GetColumnIndexByTitle(sheet, "Competência");
            int salarioColumnIndex = GetColumnIndexByTitle(sheet, "Salário");

            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++) // Começar da segunda linha (index 1)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                string? cnpj = row.GetCell(cnpjColumnIndex)?.ToString()?.Trim();
                string? competencia = row.GetCell(competenciaColumnIndex)?.ToString()?.Trim();
                string? salario = FormatSalary(row.GetCell(salarioColumnIndex)?.ToString());

                if (IsMatch(cnpj: cnpj, competencia: competencia, salario: salario, nfseData: nfseData))
                {
                    Console.WriteLine("Tudo certo. Todos os dados necessários foram validados.");
                    return true;
                }
            }
            Console.WriteLine("Dados não encontrados no Excel para o CNPJ e Competência especificados.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao validar NFSe: {ex.Message}");
            return false;
        }
    }

    private static IWorkbook LoadExcelFile(string excelFilePath)
    {
        using (var fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read))
        {
            return excelFilePath.EndsWith(".xls") ? (IWorkbook)new HSSFWorkbook(fileStream) : new XSSFWorkbook(fileStream);
        }
    }

    private static bool IsMatch(string cnpj, string competencia, string salario, NFSe nfseData)
    {
        string? formattedServiceValue = FormatSalary(nfseData.ValorServico);
        string? formattedCompetencyDate = FormatCompetencyDate(nfseData.DataCompetencia);

        Console.WriteLine($"CNPJ no Excel: {cnpj}, CNPJ Procurado: {nfseData.Prestador.Cnpj}");
        Console.WriteLine($"Competência no Excel: {competencia}, Competência Procurada: {formattedCompetencyDate}");
        Console.WriteLine($"Salário no Excel: {salario}, Salário Procurado: {formattedServiceValue}");

        return cnpj?.Trim() == nfseData.Prestador.Cnpj.Trim() &&
               competencia?.Trim().Equals(formattedCompetencyDate, StringComparison.OrdinalIgnoreCase) == true &&
               salario == formattedServiceValue;
    }

    private static int GetColumnIndexByTitle(ISheet sheet, string title)
    {
        IRow headerRow = sheet.GetRow(0);
        if (headerRow == null)
        {
            throw new Exception("Cabeçalho não encontrado na planilha.");
        }

        for (int i = 0; i < headerRow.LastCellNum; i++)
        {
            var cell = headerRow.GetCell(i);
            if (cell != null && cell.StringCellValue.Equals(title, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new Exception($"Título '{title}' não encontrado na planilha.");
    }

    private static string FormatCompetencyDate(string competencyDate)
    {
        return DateTime.ParseExact(competencyDate, "dd/MM/yyyy", CultureInfo.InvariantCulture)
                        .ToString("dd-MMM-yyyy", new CultureInfo("en-US"));
    }

    private static string FormatSalary(string salary)
    {
        if (decimal.TryParse(salary, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedSalary))
        {
            return parsedSalary.ToString("N2", new CultureInfo("pt-BR")); // Exemplo: "2.500,00"
        }
        Console.WriteLine("Erro ao converter o valor do salário para decimal.");
        return salary;
    }
}
