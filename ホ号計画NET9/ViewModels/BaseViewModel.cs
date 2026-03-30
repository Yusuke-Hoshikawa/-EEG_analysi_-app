using CommunityToolkit.Mvvm.ComponentModel;

namespace ホ号計画.ViewModels
{
    public abstract class BaseViewModel : ObservableObject
    {
        private bool _isBusy;
        private string _statusMessage;
        private int _progressValue;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        protected BaseViewModel()
        {
            _statusMessage = "Ready";
        }
    }
}