using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using AOSharp.Tweaks;

namespace AOSharp
{
    public partial class TweaksWindow : MetroWindow, INotifyPropertyChanged
    {
        private string _installDirectory;
        private bool _isProcessing;

        public string InstallDirectory
        {
            get => _installDirectory;
            set
            {
                _installDirectory = value;
                OnPropertyChanged(nameof(InstallDirectory));
                OnPropertyChanged(nameof(IsValidInstallDirectory));
                OnPropertyChanged(nameof(CanEnableLargeAddressAware));
            }
        }

        public bool IsValidInstallDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(InstallDirectory))
                    return false;

                string versionFile = Path.Combine(InstallDirectory, "version.id");
                return Directory.Exists(InstallDirectory) && File.Exists(versionFile);
            }
        }

        public bool CanEnableLargeAddressAware => IsValidInstallDirectory && !_isProcessing;

        public TweaksWindow()
        {
            InitializeComponent();
            DataContext = this;
            InstallDirectory = "";
            _isProcessing = false;

            // Ensure button state is updated
            OnPropertyChanged(nameof(CanEnableLargeAddressAware));
        }

        private async void BrowseDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select AO Install Directory";
                dialog.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(InstallDirectory) && Directory.Exists(InstallDirectory))
                {
                    dialog.SelectedPath = InstallDirectory;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Check if version.id exists in the selected directory
                    string versionFile = Path.Combine(dialog.SelectedPath, "version.id");

                    if (File.Exists(versionFile))
                    {
                        InstallDirectory = dialog.SelectedPath;
                    }
                    else
                    {
                        await this.ShowMessageAsync(
                            "Invalid Directory",
                            "The selected directory does not appear to be a valid AO installation.\nThe file 'version.id' was not found.",
                            MessageDialogStyle.Affirmative,
                            new MetroDialogSettings
                            {
                                AffirmativeButtonText = "OK"
                            });
                    }
                }
            }
        }

        private async void EnableLargeAddressAwareButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await this.ShowMessageAsync(
                "Confirmation",
                "Warning: This will make irreversible modifications to game files, do you want to proceed?",
                MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings
                {
                    AffirmativeButtonText = "Yes",
                    NegativeButtonText = "No",
                    DefaultButtonFocus = MessageDialogResult.Negative
                });

            if (result == MessageDialogResult.Affirmative)
            {
                // Set processing flag
                _isProcessing = true;
                OnPropertyChanged(nameof(CanEnableLargeAddressAware));

                // Show progress dialog
                var progressController = await this.ShowProgressAsync(
                    "Processing",
                    "Enabling large address aware functionality...",
                    false);

                progressController.SetIndeterminate();

                try
                {
                    // Run the operation on a background thread
                    await Task.Run(() => new LargeAddressAwareTweak(InstallDirectory).Run());

                    await progressController.CloseAsync();

                    await this.ShowMessageAsync(
                        "Success",
                        "Large address aware functionality has been enabled successfully.",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "OK"
                        });
                }
                catch (Exception ex)
                {
                    await progressController.CloseAsync();

                    await this.ShowMessageAsync(
                        "Error",
                        $"An error occurred while enabling large address aware:\n{ex.Message}",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "OK"
                        });
                }
                finally
                {
                    // Clear processing flag
                    _isProcessing = false;
                    OnPropertyChanged(nameof(CanEnableLargeAddressAware));
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}