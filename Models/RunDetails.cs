namespace Delta.Beverages.Web.Models
{
    public class RunDetails
    {
        public string SKUId { get; set; }
        public string SelectedLine { get; set; }
        public string SelectedShift { get; set; }
        public string SelectedUserID { get; set; }
        public string IsActive { get; set; }
        public string EndRunTimeStamp { get; set; }
        public string Description { get; set; }
        public string TotalCount { get; set; }
    }
}
