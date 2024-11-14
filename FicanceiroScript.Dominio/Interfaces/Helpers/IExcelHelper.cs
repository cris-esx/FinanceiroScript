using FinanceiroScript.Dominio;

namespace FicanceiroScript.Dominio.Interfaces.Helpers
{
    public interface IExcelHelper
    {
        bool VerificarSeDadosNFSeBatemComExcel(NFSe nfseData, string caminhoArquivoExcel);
    }
}
