using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.Globalization;
using FinanceiroScript.Dominio;
using Microsoft.Extensions.Logging;
using FicanceiroScript.Dominio.Interfaces.Helpers;

public class ExcelHelper : IExcelHelper
{
    private readonly ILogger<ExcelHelper> _logger;

    public ExcelHelper(ILogger<ExcelHelper> logger)
    {
        _logger = logger;
    }

    public bool VerificarSeDadosNFSeBatemComExcel(NFSe nfseData, string caminhoArquivoExcel)
    {
        if (nfseData == null)
        {
            _logger.LogError("Dados NFSe não pode ser nulo");
            return false;
        }

        if (string.IsNullOrEmpty(caminhoArquivoExcel))
        {
            _logger.LogError("Caminho do arquivo excel não pode ser nulo ou vazio");
            return false;
        };

        try
        {
            if (string.IsNullOrEmpty(nfseData.Prestador.Cnpj) || string.IsNullOrEmpty(nfseData.DataCompetencia))
            {
                _logger.LogError("CNPJ e Competência são necessários para a busca.");
                return false;
            }

            IWorkbook workbook = CarregarArquivoExcel(caminhoArquivoExcel);

            var sheet = workbook.GetSheetAt(0) ?? throw new Exception("Não foi possível acessar a planilha no arquivo Excel.");

            int cnpjColumnIndex = ObterIndexDaColunaPorTitulo(sheet, "CNPJ");
            int competenciaColumnIndex = ObterIndexDaColunaPorTitulo(sheet, "Competência");
            int salarioColumnIndex = ObterIndexDaColunaPorTitulo(sheet, "Salário");

            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                string? cnpj = row.GetCell(cnpjColumnIndex)?.ToString()?.Trim();
                string? competencia = row.GetCell(competenciaColumnIndex)?.ToString()?.Trim();
                string? salario = FormatarValorEmReais(row.GetCell(salarioColumnIndex)?.ToString());

                if (CompararDadosNFSeComDadosExcel(cnpj: cnpj, competencia: competencia, salario: salario, nfseData: nfseData))
                {
                    _logger.LogInformation("Validação bem-sucedida: os dados NFSe correspondem aos dados da planilha.");
                    return true;
                }
            }

            _logger.LogWarning("Dados não encontrados no Excel para o CNPJ e Competência especificados.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao validar NFSe no arquivo Excel: {caminhoArquivoExcel}");
            return false;
        }
    }

    private IWorkbook CarregarArquivoExcel(string caminhoArquivoExcel)
    {
        try
        {
            using (var fileStream = new FileStream(caminhoArquivoExcel, FileMode.Open, FileAccess.Read))
            {
                _logger.LogInformation($"Carregando arquivo Excel: {caminhoArquivoExcel}");

                if (caminhoArquivoExcel.EndsWith(".xls"))
                {
                    return new HSSFWorkbook(fileStream);
                }
                else if (caminhoArquivoExcel.EndsWith(".xlsx"))
                {
                    return new XSSFWorkbook(fileStream);
                }
                else
                {
                    _logger.LogError("Formato de arquivo não suportado. Use um arquivo .xls ou .xlsx.");
                    throw new ArgumentException("Formato de arquivo não suportado. Use um arquivo .xls ou .xlsx.");
                }
            }
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, $"Arquivo Excel não encontrado: {caminhoArquivoExcel}");
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, $"Erro ao acessar o arquivo Excel: {caminhoArquivoExcel}");
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, $"Erro no formato do arquivo Excel: {caminhoArquivoExcel}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro inesperado ao carregar o arquivo Excel: {caminhoArquivoExcel}");
            throw;
        }
    }

    private bool CompararDadosNFSeComDadosExcel(string cnpj, string competencia, string salario, NFSe nfseData)
    {
        string? valorServicoFormatado = FormatarValorEmReais(nfseData.ValorServico);
        string? dataCompetenciaFormatada = FormatarDataCompetencia(nfseData.DataCompetencia);

        _logger.LogInformation($"CNPJ no Excel: '{cnpj}', CNPJ Procurado: '{nfseData.Prestador.Cnpj}'");
        _logger.LogInformation($"Competência no Excel: '{competencia}', Competência Procurada: '{dataCompetenciaFormatada}'");
        _logger.LogInformation($"Salário no Excel: '{salario}', Salário Procurado: '{valorServicoFormatado}'");

        return cnpj?.Trim() == nfseData.Prestador.Cnpj.Trim() &&
               competencia?.Trim().Equals(dataCompetenciaFormatada, StringComparison.OrdinalIgnoreCase) == true &&
               salario == valorServicoFormatado;
    }

    private static int ObterIndexDaColunaPorTitulo(ISheet folhaPlanilha, string titulo)
    {
        IRow cabecalhoPlanilha = folhaPlanilha.GetRow(0);
        if (cabecalhoPlanilha == null)
        {
            throw new Exception("Cabeçalho não encontrado na planilha.");
        }

        for (int i = 0; i < cabecalhoPlanilha.LastCellNum; i++)
        {
            var celula = cabecalhoPlanilha.GetCell(i);
            if (celula != null && celula.StringCellValue.Equals(titulo, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new Exception($"Título '{titulo}' não encontrado na planilha.");
    }

    private static string FormatarDataCompetencia(string dataCompetencia)
    {
        return DateTime.ParseExact(dataCompetencia, "dd/MM/yyyy", CultureInfo.InvariantCulture)
                        .ToString("dd-MMM-yyyy", new CultureInfo("en-US"));
    }

    private static string FormatarValorEmReais(string valor)
    {
        decimal valorConvertido;

        bool conversaoPtBr = decimal.TryParse(valor, NumberStyles.Any, new CultureInfo("pt-BR"), out valorConvertido);

        bool conversaoInvariante = !conversaoPtBr && decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out valorConvertido);

        if (conversaoPtBr || conversaoInvariante)
        {
            return valorConvertido.ToString("N2", new CultureInfo("pt-BR")); // Exemplo: 2.500,00
        }

        Console.WriteLine("Erro ao converter o valor do salário para decimal.");
        return valor;
    }
}
