namespace Delta.Beverages.Web.Models
{
    public class SKURun
    {
        public string ID { get; set; }
        public string SelectedLine { get; set; }
        public string SKU { get; set; }
        public DateTime ChangeTimeStamp { get; set; }
        public bool Selected { get; set; }

        public override string ToString()
        {
            return "Line " + SelectedLine + " - SKU " + SKU + " <br /> " + ChangeTimeStamp.ToShortDateString();
        }
    }
}
