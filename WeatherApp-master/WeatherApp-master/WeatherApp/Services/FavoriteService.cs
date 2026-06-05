using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public class FavoriteService
    {
        private readonly string _favoritesFile;

        public FavoriteService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataPath, "WeatherApp");

            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            _favoritesFile = Path.Combine(appFolder, "favorites.json");
        }

        public List<CityModel> LoadFavorites()
        {
            try
            {
                if (File.Exists(_favoritesFile))
                {
                    var json = File.ReadAllText(_favoritesFile);
                    return JsonSerializer.Deserialize<List<CityModel>>(json) ?? new List<CityModel>();
                }
            }
            catch { }

            return new List<CityModel>
            {
                new CityModel { Name = "Moscow", IsFavorite = true },
                new CityModel { Name = "London", IsFavorite = true },
                new CityModel { Name = "New York", IsFavorite = true }
            };
        }

        public void SaveFavorites(List<CityModel> favorites)
        {
            try
            {
                var json = JsonSerializer.Serialize(favorites);
                File.WriteAllText(_favoritesFile, json);
            }
            catch { }
        }
    }
}
