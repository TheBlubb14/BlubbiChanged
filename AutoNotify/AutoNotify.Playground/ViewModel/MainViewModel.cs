using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;
using AutoNotify;

namespace AutoNotify.Playground.ViewModel
{
    public sealed partial class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// This is the negateive from <see cref="LoadedCommand"/> <see langword="true"/> when <see langword="null"/>
        /// </summary>
        [AutoNotify]
        private ICommand closeeeeeeeedCommand;

        /// <summary>
        /// This is a iother command
        /// </summary>
        [AutoNotify]
        private ICommand _otherCommanad;

        [AutoNotify]
        private string title;
        [AutoNotify]
        private string a88er8;

        /// <inheritdoc/>
        [AutoNotify]
        private bool enn2444snn;

        [AutoNotify]
        private ICommand loadedCommand;

        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
            }
            else
            {
                // Code runs "for real"
                LoadedCommand = new RelayCommand(Loaded);
            }
        }

        private void Loaded()
        {

        }
    }
}
