using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WeatherApp.Models;

namespace WeatherApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void City_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.DataContext is CityModel city)
            {
                var vm = DataContext as ViewModels.MainViewModel;
                if (vm != null)
                {
                    vm.SelectedCity = city;
                }
            }
        }

        private void RemoveFavorite_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var cityName = button?.Tag?.ToString();

            if (!string.IsNullOrEmpty(cityName))
            {
                var vm = DataContext as ViewModels.MainViewModel;
                if (vm != null)
                {
                    var cityToRemove = vm.FavoriteCities.FirstOrDefault(c => c.Name == cityName);
                    if (cityToRemove != null)
                    {
                        vm.FavoriteCities.Remove(cityToRemove);
                        vm.SaveFavoritesCommand?.Execute(null);

                        // Если удалили текущий выбранный город, выбираем первый
                        if (vm.SelectedCity?.Name == cityName && vm.FavoriteCities.Count > 0)
                        {
                            vm.SelectedCity = vm.FavoriteCities[0];
                        }
                    }
                }
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}