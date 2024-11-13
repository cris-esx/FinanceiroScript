namespace FinanceiroScript.Dominio.Interfaces.Servicos
{
    public interface INFSeServico
    {
        NFSe ExtrairDadosNFSeDoPdf(Stream pdfStream);
        string[] ObterTodasNFSes(string nfsePDFsPath);
        string RenomearEMoverNFSePdf(string pdfFile, NFSe nfseData, string destinationDir);
    }
}
