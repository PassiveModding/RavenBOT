namespace RavenBOT.Modules.Statistics.Models
{
    public class GrafanaOrganisation
    {
        public class Address
        {
            public string address1 { get; set; }
            public string address2 { get; set; }
            public string city { get; set; }
            public string zipCode { get; set; }
            public string state { get; set; }
            public string country { get; set; }
        }

        public int id { get; set; }
        public string name { get; set; }
        public Address address { get; set; }
    }
}