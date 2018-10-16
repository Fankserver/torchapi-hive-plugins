using Torch;

namespace HiveUplink
{
    public class HiveConfig : ViewModel
    {
        private int _hiveId;
        public int HiveId { get => _hiveId; set => SetValue(ref _hiveId, value); }

        private int _sectorId;
        public int SectorId { get => _sectorId; set => SetValue(ref _sectorId, value); }
    }
}
