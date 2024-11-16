using Delta.Beverages.Web.Models;

namespace Delta.Beverages.Web
{
    public interface IRunRepository
    {
        List<SKURun> GetSKURuns();
        List<SKU> GetAllSKUs();
        List<User> GetAllUsers();
        RunDetails GetRunDetails(string runId);
        //void AddRun(Run run);
        //void UpdateRun(Run run);
        void EndProduction();
        void StartRun(string runId);
    }
}
