using FinanceiroScript.Dominio;
using FinanceiroScript.Dominio.Interfaces.Helpers;
using FinanceiroScript.Dominio.Interfaces.Servicos;

public class NFSeVerificarValidadeNotasServico : INFSeVerificarValidadeNotasServico
{
    private readonly INFSeServico _nfseServico;
    private readonly IDiretorioHelper _directoryHelper;

    public NFSeVerificarValidadeNotasServico(INFSeServico nfseServico, IDiretorioHelper directoryHelper)
    {
        _nfseServico = nfseServico;
        _directoryHelper = directoryHelper;
    }

    public void VerificarValidadeNotasFiscais()
    {
        string appRootPath = _directoryHelper.GetAppRootPath();
        string notasDirPath = Path.Combine(appRootPath, "Notas");
        string notasValidasDirPath = _directoryHelper.GetValidosDirectory();
        string notasErrosDirPath = _directoryHelper.GetErrosDirectory();
        string[] pdfFiles = _nfseServico.ObterTodasNFSes(notasDirPath);
        string excelFilePath = Path.Combine(appRootPath, "TesteExcelDocs", "Folha_Teste_18.10.24.xlsx");

        if (pdfFiles == null || pdfFiles.Length < 1)
        {
            Console.WriteLine("Não foram encontrados pdfs.");
            return;
        }
        foreach (string pdfFile in pdfFiles)
        {
            try
            {
                using var pdfStream = new FileStream(pdfFile, FileMode.Open, FileAccess.Read);
                NFSe nfseData = _nfseServico.ExtrairDadosNFSeDoPdf(pdfStream);

                bool isValid = ExcelHelper.IsNFSeValid(nfseData, excelFilePath);
                string destinationDir = isValid ? notasValidasDirPath : notasErrosDirPath;

                string newFilePath = _nfseServico.RenomearEMoverNFSePdf(pdfFile, nfseData, destinationDir);

                string status = isValid ? "válida" : "inválida";
                Console.WriteLine($"NFSe {status}: {Path.GetFileName(newFilePath)}");
            }
            catch (IOException ioEx)
            {
                Console.Error.WriteLine($"Erro ao acessar arquivo '{Path.GetFileName(pdfFile)}': {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao processar arquivo '{Path.GetFileName(pdfFile)}': {ex.Message}");
            }
        }
    }
}
