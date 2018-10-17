using Torch;

namespace HiveUplink
{
    public class HiveConfig : ViewModel
    {
        private string _hiveId;
        public string HiveId { get => _hiveId; set => SetValue(ref _hiveId, value); }

        private string _sectorId;
        public string SectorId { get => _sectorId; set => SetValue(ref _sectorId, value); }
    }
}
