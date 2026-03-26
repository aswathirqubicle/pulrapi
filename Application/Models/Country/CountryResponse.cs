
namespace Core.Application.Models.Country
{
    public class CountryResponse
    {
        public string Name { get; set; }

        public string Uid { get; set; }
        public string Iso2 { get; set; }
        public string Iso3 { get; internal set; }
        public string Iso4 { get; set; }
    }
}
