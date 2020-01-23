using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Servirtium.Demo
{ 
    internal class ClimateApi
    {
        internal const string DEFAULT_SITE = "http://climatedataapi.worldbank.org";

        private readonly string _site;
        public ClimateApi(string site= DEFAULT_SITE)
        {
            _site = site;
        }



        public async Task<double> getAveAnnualRainfall(int fromCCYY, int toCCYY, params string[] countryIsos)
        {
            HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            IEnumerable<Task<double>> avgTempPerCountry = countryIsos.Select(async countryIso =>
            {
                var rawXml = await httpClient.GetStringAsync($"{_site}/climateweb/rest/v1/country/annualavg/pr/{fromCCYY}/{toCCYY}/{countryIso}.xml");
                if (rawXml.Contains("Invalid country code. Three letters are required"))
                {
                    throw new Exception($"{countryIso} not recognized by climateweb");
                }
                var doc = XDocument.Parse(rawXml);
                var result = doc.Descendants(XNamespace.None + "annualData")
                    .Descendants(XNamespace.None + "double")
                    .Select(xe => Double.Parse(xe.Value));
                if (!result.Any())
                {
                    throw new Exception ($"date range {fromCCYY}-{toCCYY} not supported");
                }
                return result.Average();
            });
            //'Average of averages' logic replicates https://github.com/servirtium/demo-java-climate-data-tck/blob/master/src/main/java/com/paulhammant/climatedata/ClimateApi.java
            return (await Task.WhenAll(avgTempPerCountry)).Average();

        }
    }
}