namespace SixForce.Models
{
    public class ModbusRegisterMap
    {
        public ushort ForceStartAddress { get; set; }
        public ushort ForceRegisterCount { get; set; }
        public ushort MvStartAddress { get; set; }
        public ushort MvRegisterCount { get; set; }
        public ushort ClearFunctionAddress { get; set; }
        public byte ClearChannelStartCode { get; set; }
        public byte ClearAllChannelsCode { get; set; }
        public ushort DecouplingStartAddress { get; set; }
        public ushort DecouplingRowCount { get; set; }
        public ushort ElementsPerRow { get; set; }
        public ushort RegistersPerElement { get; set; }
        public ushort SkipRegistersPerRow { get; set; }
        public ushort SaveParametersAddress { get; set; } // 新增保存地址
        public int[] SaveParametersValue { get; set; } // 新增保存值
    }
}
