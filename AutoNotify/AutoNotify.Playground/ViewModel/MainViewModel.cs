using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Text;
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
        private ICommand closeeedCommand;

        /// <summary>
        /// This is a iother command
        /// </summary>
        [AutoNotify]
        private ICommand _otherCommand;

        [AutoNotify]
        private string title;
        [AutoNotify]
        private string a;
        [AutoNotify]
        private bool ennnn;

        public ICommand LoadedCommand { get; set; }

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
