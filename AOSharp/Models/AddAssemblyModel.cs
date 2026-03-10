using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AOSharp.Models
{
    public class AddAssemblyModel : INotifyPropertyChanged
    {
        private string _dllPath;
        private string[] _dllPaths;

        public string[] DllPaths
        {
            get { return _dllPaths; }
            set
            {
                _dllPaths = value;
                OnPropertyChanged("DllPaths");
                OnPropertyChanged("DllPath");
            }
        }

        public string DllPath
        {
            get
            {
                if (_dllPaths != null && _dllPaths.Length > 1)
                    return $"{_dllPaths.Length} files selected";
                return _dllPaths?.FirstOrDefault() ?? _dllPath;
            }
            set
            {
                _dllPath = value;
                _dllPaths = new[] { value };
                OnPropertyChanged("DllPath");
            }
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
}
