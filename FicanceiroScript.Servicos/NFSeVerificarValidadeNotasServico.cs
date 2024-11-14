using FinanceiroScript.Dominio;
using FinanceiroScript.Dominio.Interfaces.Helpers;
using FinanceiroScript.Dominio.Interfaces.Servicos;
using FinanceiroScript.Servicos;
using Microsoft.Extensions.Logging;
using NLog;

public class NFSeVerificarValidadeNotasServico : INFSeVerificarValidadeNotasServico
{
    private readonly INFSeServico _nfseServico;
    private readonly IDiretorioHelper _diretorioHelper;
    private readonly ILogger<NFSeVerificarValidadeNotasServico> _logger;

    public NFSeVerificarValidadeNotasServico(INFSeServico nfseServico, IDiretorioHelper diretorioHelper, ILogger<NFSeVerificarValidadeNotasServico> logger)
    {
        _nfseServico = nfseServico;
        _diretorioHelper = diretorioHelper;
        _logger = logger;
    }

    public void VerificarValidadeNotasFiscais()
    {
        _logger.LogInformation("Função verificar validade das notas fiscais iniciada.");

        string caminhoRaizApp = _diretorioHelper.ObterCaminhoRaizAplicacao();
        string dirNotas = Path.Combine(caminhoRaizApp, "Notas");
        string dirNotasValidas = _diretorioHelper.ObterDiretorioValidos();
        string dirNotasErros = _diretorioHelper.ObterDiretorioErros();
        string[] arquivosPdfs = _nfseServico.ObterTodasNFSes(dirNotas);
        string caminhoArquivoExcel = Path.Combine(caminhoRaizApp, "TesteExcelDocs", "Folha_Teste_18.10.24.xlsx");

        if (arquivosPdfs == null || arquivosPdfs.Length < 1)
        {
            _logger.LogError("Não foram encontrados pdfs.");
            return;
        }

        foreach (string pdf in arquivosPdfs)
        {
            try
            {
                using var pdfStream = new FileStream(pdf, FileMode.Open, FileAccess.Read);
                NFSe nfseData = _nfseServico.ExtrairDadosNFSeDoPdf(pdfStream);

                bool isValid = ExcelHelper.IsNFSeValid(nfseData, caminhoArquivoExcel);
                string dirDestino = isValid ? dirNotasValidas : dirNotasErros;

                string novoCaminhoArquivoPdf = _nfseServico.RenomearEMoverNFSePdf(pdf, nfseData, dirDestino);

                string status = isValid ? "válida" : "inválida";
                _logger.LogInformation($"NFSe {status}: {Path.GetFileName(novoCaminhoArquivoPdf)}");
            }
            catch (IOException ioEx)
            {
                _logger.LogError($"Erro ao acessar arquivo '{Path.GetFileName(pdf)}': {ioEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao processar arquivo '{Path.GetFileName(pdf)}': {ex.Message}");
            }
        }
    }
}
