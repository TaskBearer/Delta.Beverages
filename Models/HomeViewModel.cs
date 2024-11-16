namespace Delta.Beverages.Web.Models
{
    public class HomeViewModel
    {
        public HomeViewModel() { }
        public List<SKURun> SKURuns { get; set; }
        public List<SKU> AllSKUs { get; set; }
        public List<User> AllUsers { get; set; }
        public RunDetails RunDetails { get; set; }
        public List<AssetList> AssetList { get; set; }
        public List<Assets> AllAssets { get; set; }
        public List<RunsTimeWindowDetails> RunsTimeWindowDetails { get; set; }
    }
}
