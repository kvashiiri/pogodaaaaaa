using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WeatherApp.Models
{
    public class ForecastResponse
    {
        [JsonPropertyName("list")]
        public List<ForecastItem> List { get; set; }
    }

    public class ForecastItem
    {
        [JsonPropertyName("dt_txt")]
        public string DateText { get; set; }

        [JsonPropertyName("main")]
        public MainInfo Main { get; set; }

        [JsonPropertyName("weather")]
        public List<WeatherInfo> Weather { get; set; }
    }
}
