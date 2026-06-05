using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WeatherApp.Helpers;
using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly WeatherService _weatherService;
        private readonly FavoriteService _favoriteService;
        private bool _isLoading = false;
        private System.Timers.Timer _updateTimer;

        public ObservableCollection<ForecastItem> Forecasts { get; set; }
        public ObservableCollection<DailyForecast> DailyForecasts { get; set; }
        public ObservableCollection<CityModel> FavoriteCities { get; set; }

        private ISeries[] _series;
        public ISeries[] Series
        {
            get => _series;
            set
            {
                _series = value;
                OnPropertyChanged();
            }
        }

        private Axis[] _xAxes;
        public Axis[] XAxes
        {
            get => _xAxes;
            set
            {
                _xAxes = value;
                OnPropertyChanged();
            }
        }

        private string _searchCity;
        public string SearchCity
        {
            get => _searchCity;
            set
            {
                _searchCity = value;
                OnPropertyChanged();
            }
        }

        private CityModel _selectedCity;
        public CityModel SelectedCity
        {
            get => _selectedCity;
            set
            {
                if (value != null && _selectedCity != value)
                {
                    _selectedCity = value;
                    OnPropertyChanged();
                    Task.Run(async () => await LoadWeatherAsync(value.Name));
                }
            }
        }

        private string _city;
        public string City
        {
            get => _city;
            set
            {
                _city = value;
                OnPropertyChanged();
            }
        }

        private double _temperature;
        public double Temperature
        {
            get => _temperature;
            set
            {
                _temperature = value;
                OnPropertyChanged();
            }
        }

        private int _humidity;
        public int Humidity
        {
            get => _humidity;
            set
            {
                _humidity = value;
                OnPropertyChanged();
            }
        }

        private int _pressure;
        public int Pressure
        {
            get => _pressure;
            set
            {
                _pressure = value;
                OnPropertyChanged();
            }
        }

        private double _windSpeed;
        public double WindSpeed
        {
            get => _windSpeed;
            set
            {
                _windSpeed = value;
                OnPropertyChanged();
            }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                _isFavorite = value;
                OnPropertyChanged();
            }
        }

        private DateTime _lastUpdate;
        public DateTime LastUpdate
        {
            get => _lastUpdate;
            set
            {
                _lastUpdate = value;
                OnPropertyChanged();
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand AddToFavoritesCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SaveFavoritesCommand { get; }

        public MainViewModel()
        {
            _weatherService = new WeatherService();
            _favoriteService = new FavoriteService();

            Forecasts = new ObservableCollection<ForecastItem>();
            DailyForecasts = new ObservableCollection<DailyForecast>();
            FavoriteCities = new ObservableCollection<CityModel>();

            SearchCommand = new RelayCommand(async _ => await SearchCityWeather());
            AddToFavoritesCommand = new RelayCommand(_ => AddCurrentToFavorites());
            RefreshCommand = new RelayCommand(async _ => await RefreshWeather());
            SaveFavoritesCommand = new RelayCommand(_ => SaveFavoriteCities());

            LoadFavoriteCities();

            if (FavoriteCities.Count > 0)
            {
                SelectedCity = FavoriteCities[0];
            }
            else
            {
                City = "Moscow";
                Task.Run(async () => await LoadWeatherAsync("Moscow"));
            }

            StartAutoUpdate();
        }

        private async Task SearchCityWeather()
        {
            if (string.IsNullOrWhiteSpace(SearchCity)) return;
            await LoadWeatherAsync(SearchCity);
            City = SearchCity;
            SearchCity = "";
        }

        private async Task LoadWeatherAsync(string cityName)
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                var weather = await _weatherService.GetCurrentWeatherAsync(cityName);
                if (weather != null)
                {
                    Temperature = Math.Round(weather.Main.Temp, 1);
                    Humidity = weather.Main.Humidity;
                    Pressure = weather.Main.Pressure;
                    WindSpeed = Math.Round(weather.Wind.Speed, 1);
                    Description = weather.Weather.FirstOrDefault()?.Description ?? "";
                    City = weather.Name;
                    LastUpdate = DateTime.Now;
                    IsFavorite = FavoriteCities.Any(c => c.Name.Equals(weather.Name, StringComparison.OrdinalIgnoreCase));
                }

                var forecast = await _weatherService.GetForecastAsync(cityName);
                if (forecast != null && forecast.List != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Forecasts.Clear();
                        foreach (var item in forecast.List.Take(40))
                        {
                            Forecasts.Add(item);
                        }

                        DailyForecasts.Clear();
                        var dailyGroups = forecast.List
                            .GroupBy(x => DateTime.Parse(x.DateText).Date)
                            .Take(5);

                        foreach (var group in dailyGroups)
                        {
                            DailyForecasts.Add(new DailyForecast
                            {
                                Date = group.Key,
                                MinTemp = Math.Round(group.Min(x => x.Main.TempMin), 1),
                                MaxTemp = Math.Round(group.Max(x => x.Main.TempMax), 1),
                                Description = group.First().Weather.FirstOrDefault()?.Description ?? ""
                            });
                        }
                    });

                    await BuildChart(forecast);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task BuildChart(ForecastResponse forecast)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var hourlyData = forecast.List.Take(12).ToList();

                var temperatures = hourlyData.Select(x => Math.Round(x.Main.Temp, 1)).ToArray();
                var labels = hourlyData.Select(x => DateTime.Parse(x.DateText).ToString("HH:mm")).ToArray();

                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = temperatures,
                        Name = "Температура, °C",
                        GeometrySize = 8
                    }
                };

                XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = labels,
                        LabelsRotation = 45,
                        Name = "Время"
                    }
                };
            });
        }

        private void AddCurrentToFavorites()
        {
            if (string.IsNullOrEmpty(City)) return;

            if (!FavoriteCities.Any(c => c.Name.Equals(City, StringComparison.OrdinalIgnoreCase)))
            {
                var city = new CityModel { Name = City, IsFavorite = true };
                FavoriteCities.Add(city);
                SaveFavoriteCities();
                IsFavorite = true;
            }
        }

        private async Task RefreshWeather()
        {
            if (!string.IsNullOrEmpty(City))
            {
                await LoadWeatherAsync(City);
            }
        }

        private void LoadFavoriteCities()
        {
            var favorites = _favoriteService.LoadFavorites();
            FavoriteCities.Clear();
            foreach (var city in favorites)
            {
                FavoriteCities.Add(city);
            }
        }

        private void SaveFavoriteCities()
        {
            _favoriteService.SaveFavorites(FavoriteCities.ToList());
        }

        private void StartAutoUpdate()
        {
            _updateTimer = new System.Timers.Timer(300000);
            _updateTimer.Elapsed += async (s, e) =>
            {
                if (!string.IsNullOrEmpty(City))
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await LoadWeatherAsync(City);
                    });
                }
            };
            _updateTimer.Start();
        }
    }

    public class DailyForecast
    {
        public DateTime Date { get; set; }
        public double MinTemp { get; set; }
        public double MaxTemp { get; set; }
        public string Description { get; set; }
    }
}
