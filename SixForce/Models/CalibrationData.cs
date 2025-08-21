using CommunityToolkit.Mvvm.ComponentModel;

namespace SixForce.Models
{
    public partial class CalibrationData : ObservableObject
    {
        [ObservableProperty]
        public string? channel;
        [ObservableProperty]
        public string? mvValue;
        [ObservableProperty]
        public string? forceValue;
    }
}
