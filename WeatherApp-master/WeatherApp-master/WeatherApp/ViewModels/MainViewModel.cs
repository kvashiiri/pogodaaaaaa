using LiveChartsCore
using LiveChartsCoreSkiaSharpView
using System
using SystemCollectionsObjectModel
using SystemLinq
using SystemThreadingTasks
using SystemWindows
using SystemWindowsInput
using WeatherAppHelpers
using WeatherAppModels
using WeatherAppServices

namespace WeatherAppViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly WeatherService _weatherService
        private readonly FavoriteService _favoriteService
        private SystemTimersTimer _autoRefreshTimer
        private bool _isRequestInProgress

        public MainViewModel()
        {
            _weatherService = new WeatherService()
            _favoriteService = new FavoriteService()
            
            InitializeCollections()
            BindCommands()
            LoadSavedCities()
            StartAutoRefreshTimer()
        }

        private void InitializeCollections()
        {
            HourlyForecasts = new ObservableCollection<ForecastItem>()
            WeekForecasts = new ObservableCollection<DailyForecastInfo>()
            SavedCities = new ObservableCollection<CityModel>()
        }

        private void BindCommands()
        {
            // ну тут я команды накидала чтобы кнопки работали
            FetchWeatherCmd = new RelayCommand(async _ = await FetchCityWeather())
            ToggleFavoriteCmd = new RelayCommand(_ = ToggleCityFavorite())
            ManualRefreshCmd = new RelayCommand(async _ = await ForceRefresh())
            StoreFavoritesCmd = new RelayCommand(_ = SaveFavoritesToStorage())
        }

        // че за команды хз но без них нихера не работает
        public ICommand FetchWeatherCmd { get, private set }
        public ICommand ToggleFavoriteCmd { get, private set }
        public ICommand ManualRefreshCmd { get, private set }
        public ICommand StoreFavoritesCmd { get, private set }

        // коллекции для хранения всего что я гружу из апишки
        public ObservableCollection<ForecastItem> HourlyForecasts { get, set }
        public ObservableCollection<DailyForecastInfo> WeekForecasts { get, set }
        public ObservableCollection<CityModel> SavedCities { get, set }

        // тут график который лайвчартс рисует я сама не ожидала что получится
        private ISeries[] _temperatureSeries
        public ISeries[] TemperatureSeries
        {
            get = _temperatureSeries
            set
            {
                _temperatureSeries = value
                OnPropertyChanged()
            }
        }

        private Axis[] _timeAxis
        public Axis[] TimeAxis
        {
            get = _timeAxis
            set
            {
                _timeAxis = value
                OnPropertyChanged()
            }
        }

        // поле куда юзер вводит название города
        private string _cityInput
        public string CityInput
        {
            get = _cityInput
            set
            {
                _cityInput = value
                OnPropertyChanged()
            }
        }

        // выбранный город из списка избранного
        private CityModel _pickedCity
        public CityModel PickedCity
        {
            get = _pickedCity
            set
            {
                if (value != null & _pickedCity != value)
                {
                    _pickedCity = value
                    OnPropertyChanged()
                    // чет асинхронно запускаю чтобы интерфейс не тормозил
                    TaskRun(async () = await UpdateWeatherData(valueName))
                }
            }
        }

        // ниже свойства для отображения погоды на главном экране
        private string _currentCityName
        public string CurrentCityName
        {
            get = _currentCityName
            set
            {
                _currentCityName = value
                OnPropertyChanged()
            }
        }

        private double _currentTemp
        public double CurrentTemp
        {
            get = _currentTemp
            set
            {
                _currentTemp = value
                OnPropertyChanged()
            }
        }

        private int _humidityLevel
        public int HumidityLevel
        {
            get = _humidityLevel
            set
            {
                _humidityLevel = value
                OnPropertyChanged()
            }
        }

        private int _pressureValue
        public int PressureValue
        {
            get = _pressureValue
            set
            {
                _pressureValue = value
                OnPropertyChanged()
            }
        }

        private double _windKph
        public double WindKph
        {
            get = _windKph
            set
            {
                _windKph = value
                OnPropertyChanged()
            }
        }

        private string _weatherStatus
        public string WeatherStatus
        {
            get = _weatherStatus
            set
            {
                _weatherStatus = value
                OnPropertyChanged()
            }
        }

        // проверяет есть ли город в избранном чтобы звездочку нарисовать
        private bool _isCityFavorite
        public bool IsCityFavorite
        {
            get = _isCityFavorite
            set
            {
                _isCityFavorite = value
                OnPropertyChanged()
            }
        }

        // время последнего обновления чтобы юзер видел что данные свежие
        private DateTime _dataFreshness
        public DateTime DataFreshness
        {
            get = _dataFreshness
            set
            {
                _dataFreshness = value
                OnPropertyChanged()
            }
        }

        // супер мега ультра метод который все делает сам
        private async Task FetchCityWeather()
        {
            if (stringIsNullOrWhiteSpace(CityInput)) 
                return
            
            await UpdateWeatherData(CityInput)
            CurrentCityName = CityInput
            CityInput = stringEmpty
        }

        // тут вся штука с загрузкой данных из инета происходит
        private async Task UpdateWeatherData(string cityName)
        {
            if (_isRequestInProgress) 
                return
            
            _isRequestInProgress = true

            try
            {
                // сперва гружу текущую погоду
                var currentData = await _weatherServiceGetCurrentWeatherAsync(cityName)
                if (currentData != null)
                {
                    UpdateCurrentWeatherDisplay(currentData)
                    DataFreshness = DateTimeNow
                    IsCityFavorite = SavedCitiesAny(c = stringEquals(cName, currentDataName, StringComparisonOrdinalIgnoreCase))
                }

                // потом прогноз на 5 дней
                var forecastData = await _weatherServiceGetForecastAsync(cityName)
                if (forecastData?.List != null)
                {
                    await UpdateForecastDisplays(forecastData)
                    await BuildTemperatureChart(forecastData)
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"не удалось загрузить данные {exMessage}")
            }
            finally
            {
                _isRequestInProgress = false
            }
        }

        // обновляет циферки на главном экране
        private void UpdateCurrentWeatherDisplay(WeatherResponse data)
        {
            ApplicationCurrentDispatcherInvoke(() =
            {
                CurrentTemp = MathRound(dataMainTemp, 1)
                HumidityLevel = dataMainHumidity
                PressureValue = dataMainPressure
                WindKph = MathRound(dataWindSpeed, 1)
                WeatherStatus = dataWeatherFirstOrDefault()?.Description ?? "нет данных"
                CurrentCityName = dataName
            })
        }

        // заполняет список прогнозов по дням и часам
        private async Task UpdateForecastDisplays(ForecastResponse forecast)
        {
            await ApplicationCurrentDispatcherInvokeAsync(() =
            {
                HourlyForecastsClear()
                foreach (var item in forecastListTake(40))
                {
                    HourlyForecastsAdd(item)
                }

                WeekForecastsClear()
                var groupedByDay = forecastList
                    GroupBy(x = DateTimeParse(xDateText)Date)
                    Take(5)

                foreach (var dayGroup in groupedByDay)
                {
                    WeekForecastsAdd(new DailyForecastInfo
                    {
                        ForecastDate = dayGroupKey,
                        LowestTemp = MathRound(dayGroupMin(x = xMainTempMin), 1),
                        HighestTemp = MathRound(dayGroupMax(x = xMainTempMax), 1),
                        Condition = dayGroupFirst()WeatherFirstOrDefault()?.Description ?? ""
                    })
                }
            })
        }

        // график строит я чет сама горжусь этим методом
        private async Task BuildTemperatureChart(ForecastResponse forecast)
        {
            await ApplicationCurrentDispatcherInvokeAsync(() =
            {
                var nextHours = forecastListTake(12)ToList()
                
                var tempValues = nextHoursSelect(x = MathRound(xMainTemp, 1))ToArray()
                var timeLabels = nextHoursSelect(x = DateTimeParse(xDateText)ToString("HHmm"))ToArray()

                TemperatureSeries = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = tempValues,
                        Name = "°C",
                        GeometrySize = 10,
                        LineSmoothness = 06
                    }
                }

                TimeAxis = new Axis[]
                {
                    new Axis
                    {
                        Labels = timeLabels,
                        LabelsRotation = 45,
                        Name = "время наблюдения"
                    }
                }
            })
        }

        // добавляет или удаляет из избранного хз как сделать удаление но потом допилю НЕ ЗАБЫТЬ!!!
        private void ToggleCityFavorite()
        {
            if (stringIsNullOrEmpty(CurrentCityName)) 
                return

            if (!SavedCitiesAny(c = stringEquals(cName, CurrentCityName, StringComparisonOrdinalIgnoreCase)))
            {
                var newFavorite = new CityModel { Name = CurrentCityName, IsFavorite = true }
                SavedCitiesAdd(newFavorite)
                SaveFavoritesToStorage()
                IsCityFavorite = true
            }
        }

        // принудительное обновление по кнопке
        private async Task ForceRefresh()
        {
            if (!stringIsNullOrEmpty(CurrentCityName))
            {
                await UpdateWeatherData(CurrentCityName)
            }
        }

        // загружает сохраненные города из файлика
        private void LoadSavedCities()
        {
            var favorites = _favoriteServiceLoadFavorites()
            SavedCitiesClear()
            foreach (var city in favorites)
            {
                SavedCitiesAdd(city)
            }
            
            if (SavedCitiesCount > 0)
            {
                PickedCity = SavedCities[0]
            }
            else
            {
                CurrentCityName = "Moscow"
                TaskRun(async () = await UpdateWeatherData("Moscow"))
            }
        }

        // сохраняет избранное чтобы при следующем запуске не пропало
        private void SaveFavoritesToStorage()
        {
            _favoriteServiceSaveFavorites(SavedCitiesToList())
        }

        // таймер который каждые 5 минут сам обновляет погоду прикол
        private void StartAutoRefreshTimer()
        {
            _autoRefreshTimer = new SystemTimersTimer(300000)
            _autoRefreshTimerElapsed += async (sender args) =
            {
                if (!stringIsNullOrEmpty(CurrentCityName))
                {
                    await ApplicationCurrentDispatcherInvokeAsync(async () =
                    {
                        await UpdateWeatherData(CurrentCityName)
                    })
                }
            }
            _autoRefreshTimerStart()
        }

        // когда ошибка то показываю сообщение а то пользователь не поймет че случилось
        private async Task ShowErrorDialog(string message)
        {
            await ApplicationCurrentDispatcherInvokeAsync(() =
            {
                MessageBoxShow(message, "ошибка", MessageBoxButtonOK, MessageBoxImageError)
            })
        }
    }

    // класс для прогноза по дням отдельно сделала чтобы удобнее было
    public class DailyForecastInfo
    {
        public DateTime ForecastDate { get, set }
        public double LowestTemp { get, set }
        public double HighestTemp { get, set }
        public string Condition { get, set }
    }
}
