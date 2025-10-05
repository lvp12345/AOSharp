using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using AOSharp.Data;
using AOSharp.Models;
using Serilog;

namespace AOSharp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        //private ObservableCollection<Profile> _profiles;
        //public ObservableCollection<Profile> Profiles { get { return _profiles; } }

        //private ObservableCollection<Assembly> _assemblies;
        //public ObservableCollection<Assembly> Assemblies { get { return _assemblies; } }

        public Config Config;

        private Profile _activeProfile;

        public Profile ActiveProfile
        {
            get { return _activeProfile; }
            set
            {
                _activeProfile = value;
                OnPropertyChanged("ActiveProfile");
            }
        }

        private bool _hasProfileSelected;

        public bool HasProfileSelected
        {
            get { return _hasProfileSelected; }
            set
            {
                _hasProfileSelected = value;
                OnPropertyChanged("HasProfileSelected");
            }
        }

        public MainWindow()
        {
            try
            {
                //_profiles = GetProfiles();
                //_assemblies = GetAssemblies();

                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File("Log.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                Log.Information("MainWindow constructor started");

                Config = Config.Load(Directories.ConfigFilePath);

                Config.Plugins.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) =>
                {
                    try
                    {
                        if(e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
                            Config.Save();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error in Plugins.CollectionChanged event handler");
                        CrashLogger.LogException(ex, "Plugins.CollectionChanged");
                    }
                };

                this.DataContext = this;

                InitializeComponent();

                PluginsDataGrid.DataContext = Config;
                ProfileListBox.DataContext = new ProfilesModel(Config);

                Log.Information("MainWindow constructor completed successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Critical error in MainWindow constructor");
                CrashLogger.LogException(ex, "MainWindow Constructor");
                throw; // Re-throw to let the crash logger handle it
            }
        }

        private async void ShowAddPluginDialog(object sender, RoutedEventArgs e)
        {
            BaseMetroDialog dialog = (BaseMetroDialog)this.Resources["AddPluginDialog"];
            dialog.DataContext = new AddAssemblyModel();
            await this.ShowMetroDialogAsync(dialog);
            await dialog.WaitUntilUnloadedAsync();
        }

        private async void CloseAddPluginDialog(object sender, RoutedEventArgs e)
        {
            BaseMetroDialog dialog = (BaseMetroDialog)this.Resources["AddPluginDialog"];
            await this.HideMetroDialogAsync(dialog);
        }

        private void LocalPathDialogButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog() { Filter = "DLL Files (*.dll)|*.dll" };

            if (dialog.ShowDialog() == true)
            {
                BaseMetroDialog addPluginDialog = (BaseMetroDialog)this.Resources["AddPluginDialog"];
                ((AddAssemblyModel)addPluginDialog.DataContext).DllPath = dialog.FileName;
            }
        }

        private async void AddPluginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("AddPluginButton_Click started");

                BaseMetroDialog addPluginDialog = (BaseMetroDialog)this.Resources["AddPluginDialog"];
                AddAssemblyModel dataModel = (AddAssemblyModel)addPluginDialog.DataContext;

                if (string.IsNullOrEmpty(dataModel.DllPath))
                {
                    Log.Warning("AddPluginButton_Click: No plugin path specified");
                    await this.ShowMessageAsync("Error", "No plugin path specified.");
                    return;
                }

                if (dataModel.DllPath.ToLower().Contains("\\obj\\"))
                {
                    Log.Warning("AddPluginButton_Click: Invalid plugin path contains \\obj\\: {DllPath}", dataModel.DllPath);
                    await this.ShowMessageAsync("Error", $"Invalid plugin path specified: path should not include \\obj\\. Did you mean {dataModel.DllPath.Replace("\\obj\\", "\\bin\\")}?");
                    return;
                }

                //Should never happen but just in case some idiot deletes a plugin after selecting it..
                if (!File.Exists(dataModel.DllPath))
                {
                    Log.Warning("AddPluginButton_Click: Plugin file does not exist: {DllPath}", dataModel.DllPath);
                    return;
                }

                FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(dataModel.DllPath);
                var hash = Utils.HashFromFile(dataModel.DllPath);

                Log.Information("Adding plugin: {PluginName} ({Version}) from {Path}",
                    fileInfo.ProductName, fileInfo.FileVersion, dataModel.DllPath);

                Config.Plugins.Add(hash, new PluginModel()
                {
                    Name = fileInfo.ProductName,
                    Version = fileInfo.FileVersion,
                    Path = dataModel.DllPath
                });

                await this.HideMetroDialogAsync(addPluginDialog);
                Log.Information("AddPluginButton_Click completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in AddPluginButton_Click");
                CrashLogger.LogException(ex, "AddPluginButton_Click");
                await this.ShowMessageAsync("Error", $"Failed to add plugin: {ex.Message}");
            }
        }

        private void PluginEnabledCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Profile selectedProfile = (Profile)ProfileListBox.SelectedItem;

            if (selectedProfile == null)
                return;

            KeyValuePair<string, PluginModel> plugin = (KeyValuePair<string, PluginModel>)PluginsDataGrid.SelectedItem;

            if (plugin.Value.IsEnabled)
                selectedProfile.EnabledPlugins.Add(plugin.Key);
            else
                selectedProfile.EnabledPlugins.Remove(plugin.Key);

            if (!Config.Profiles.Contains(selectedProfile))
                Config.Profiles.Add(selectedProfile);

            Config.Save();
        }

        private void ProfileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Profile profile = (Profile)ProfileListBox.SelectedItem;

            HasProfileSelected = profile != null;
            ActiveProfile = profile;

            if(profile == null)
            {
                foreach (PluginModel plugin in Config.Plugins.Values)
                    plugin.IsEnabled = false;

                PluginsDataGrid.IsEnabled = false;
            }
            else if(!PluginsDataGrid.IsEnabled)
            {
                PluginsDataGrid.IsEnabled = true;
            }

            foreach (KeyValuePair<string, PluginModel> plugin in Config.Plugins)
                plugin.Value.IsEnabled = profile.EnabledPlugins.Contains(plugin.Key);
        }

        private async void InjectButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("InjectButton_Clicked started");

                Profile profile = (Profile)ProfileListBox.SelectedItem;

                if (profile == null)
                {
                    Log.Warning("InjectButton_Clicked: No profile selected");
                    return;
                }

                IEnumerable<string> plugins = Config.Plugins.Where(x => profile.EnabledPlugins.Contains(x.Key)).Select(x => x.Value.Path);

                if(!plugins.Any())
                {
                    Log.Warning("InjectButton_Clicked: No plugins selected for profile {ProfileName}", profile.Name);
                    await this.ShowMessageAsync("Error", "No plugins selected.");
                    return;
                }

                Log.Information("Injecting {PluginCount} plugins for profile {ProfileName}: {Plugins}",
                    plugins.Count(), profile.Name, string.Join(", ", plugins.Select(System.IO.Path.GetFileName)));

                if(profile.Inject(plugins))
                {
                    Log.Information("Injection successful for profile {ProfileName}", profile.Name);
                    PluginsDataGrid.IsEnabled = false;
                }
                else
                {
                    Log.Error("Injection failed for profile {ProfileName}", profile.Name);
                    await this.ShowMessageAsync("Error", "Failed to inject.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in InjectButton_Clicked");
                CrashLogger.LogException(ex, "InjectButton_Clicked");
                await this.ShowMessageAsync("Error", $"Injection failed with error: {ex.Message}");
            }
        }

        private void EjectButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("EjectButton_Clicked started");

                Profile profile = (Profile)ProfileListBox.SelectedItem;

                if (profile == null)
                {
                    Log.Warning("EjectButton_Clicked: No profile selected");
                    return;
                }

                Log.Information("Ejecting profile {ProfileName}", profile.Name);
                profile.Eject();

                PluginsDataGrid.IsEnabled = true;
                Log.Information("EjectButton_Clicked completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in EjectButton_Clicked");
                CrashLogger.LogException(ex, "EjectButton_Clicked");
                MessageBox.Show($"Ejection failed with error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TweaksButton_Click(object sender, RoutedEventArgs e)
        {
            var tweaksWindow = new TweaksWindow();
            tweaksWindow.Owner = this;
            tweaksWindow.ShowDialog();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public Visibility TrueValue { get; set; }
        public Visibility FalseValue { get; set; }

        public BoolToVisibilityConverter()
        {
            // set defaults
            TrueValue = Visibility.Visible;
            FalseValue = Visibility.Collapsed;
        }

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return null;
            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (Equals(value, TrueValue))
                return true;
            if (Equals(value, FalseValue))
                return false;
            return null;
        }
    }

    public class RemovePluginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[1] == DependencyProperty.UnsetValue)
            {
                return null;
            }

            Tuple<ObservableDictionary<string, PluginModel>, string> tuple = new Tuple<ObservableDictionary<string, PluginModel>, string>(
                (ObservableDictionary<string, PluginModel>)values[0], (string)values[1]);
            return tuple;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PathIsInvalidConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path)
            {
                return path.Contains(@"\obj\");
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
