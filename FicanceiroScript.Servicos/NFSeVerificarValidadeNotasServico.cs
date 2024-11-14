using FinanceiroScript.Dominio;
using FinanceiroScript.Dominio.Interfaces.Helpers;
using FinanceiroScript.Dominio.Interfaces.Servicos;
using FinanceiroScript.Servicos;
using Microsoft.Extensions.Logging;
using NLog;

public class NFSeVerificarValidadeNotasServico : INFSeVerificarValidadeNotasServico
{
    private readonly INFSeServico _nfseServico;
    private readonly IDiretorioHelper _directoryHelper;
    private readonly ILogger<NFSeVerificarValidadeNotasServico> _logger;

    public NFSeVerificarValidadeNotasServico(INFSeServico nfseServico, IDiretorioHelper directoryHelper, ILogger<NFSeVerificarValidadeNotasServico> logger)
    {
        _nfseServico = nfseServico;
        _directoryHelper = directoryHelper;
        _logger = logger;
    }

    public void VerificarValidadeNotasFiscais()
    {
        _logger.LogInformation("Função verificar validade das notas fiscais iniciada.");

        string appRootPath = _directoryHelper.GetAppRootPath();
        string notasDirPath = Path.Combine(appRootPath, "Notas");
        string notasValidasDirPath = _directoryHelper.GetValidosDirectory();
        string notasErrosDirPath = _directoryHelper.GetErrosDirectory();
        string[] pdfFiles = _nfseServico.ObterTodasNFSes(notasDirPath);
        string excelFilePath = Path.Combine(appRootPath, "TesteExcelDocs", "Folha_Teste_18.10.24.xlsx");

        if (pdfFiles == null || pdfFiles.Length < 1)
        {
            _logger.LogError("Não foram encontrados pdfs.");
            return;
        }

        foreach (string pdfFile in pdfFiles)
        {
            try
            {
                using var pdfStream = new FileStream(pdfFile, FileMode.Open, FileAccess.Read);
                NFSe nfseData = _nfseServico.ExtrairDadosNFSeDoPdf(pdfStream);

                bool isValid = ExcelHelper.IsNFSeValid(nfseData, excelFilePath);
                string dirDestino = isValid ? notasValidasDirPath : notasErrosDirPath;

                string newFilePath = _nfseServico.RenomearEMoverNFSePdf(pdfFile, nfseData, dirDestino);

                string status = isValid ? "válida" : "inválida";
                _logger.LogInformation($"NFSe {status}: {Path.GetFileName(newFilePath)}");
            }
            catch (IOException ioEx)
            {
                _logger.LogError($"Erro ao acessar arquivo '{Path.GetFileName(pdfFile)}': {ioEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao processar arquivo '{Path.GetFileName(pdfFile)}': {ex.Message}");
            }
        }
    }
}
