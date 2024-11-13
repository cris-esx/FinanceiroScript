namespace FinanceiroScript.Dominio.Interfaces.Helpers
{
    public interface IDiretorioHelper
    {
        string GetAppRootPath();
        string GetResultDirectory();
        string GetValidosDirectory();
        string GetErrosDirectory();
    }
}
