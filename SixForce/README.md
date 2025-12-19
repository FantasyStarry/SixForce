# SixForce - 六维力传感器标定软件

## 项目简介

SixForce是一个专业的六维力传感器标定和数据采集软件，采用C# WPF技术开发，支持多种型号的六维力传感器设备。该软件通过Modbus RTU协议实现与硬件设备的通信，提供实时数据采集、解耦矩阵配置、通道校准等功能。

## 主要功能

### 🔧 核心功能
- **多型号支持**: 支持DR304A、嵌入式、503B等多种型号的六维力传感器
- **实时数据采集**: 支持六通道力值和mV值的实时读取和显示
- **解耦矩阵管理**: 支持解耦矩阵的读取、编辑和写入操作
- **通道校准**: 提供通道清除和校准功能
- **参数保存**: 支持设备参数的保存和恢复

### 📊 数据采集
- 6个通道数据实时显示：Fx, Fy, Fz, Mx, My, Mz
- 支持力值和mV值的同时采集
- 可调节采集频率（默认50ms间隔）
- 异常处理和自动重连机制

### ⚙️ 解耦控制
- 6×6解耦矩阵的读写操作
- 实时矩阵编辑功能
- 参数自动保存到设备

### 🎛️ 用户界面
- 现代化Material Design风格界面
- 三栏式布局：左侧面板、中心面板、右侧面板
- 直观的操作控制和数据显示

## 技术架构

### 🏗️ 架构设计
- **架构模式**: MVVM (Model-View-ViewModel)
- **UI框架**: WPF (.NET 8.0/10.0)
- **通信协议**: Modbus RTU over Serial
- **依赖注入**: Microsoft.Extensions.DependencyInjection

### 📦 核心技术栈
- **.NET**: 8.0/10.0 Windows
- **UI框架**: WPF with Material Design
- **MVVM框架**: CommunityToolkit.Mvvm 8.4.0
- **串口通信**: System.IO.Ports
- **Excel处理**: ClosedXML 0.105.0
- **文档处理**: DocumentFormat.OpenXml 3.3.0

### 📁 项目结构
```
SixForce/
├── Models/                 # 数据模型
│   ├── CalibrationData.cs     # 校准数据结构
│   └── ModbusRegisterMap.cs   # Modbus寄存器映射
├── Services/               # 服务层
│   ├── IModbusService.cs      # Modbus服务接口
│   ├── ModbusRTUService.cs    # Modbus RTU实现
│   ├── IMessageService.cs     # 消息服务接口
│   └── MessageService.cs      # 消息服务实现
├── ViewModels/             # 视图模型
│   ├── MainViewModel.cs       # 主窗口视图模型
│   ├── LeftPanelViewModel.cs  # 左侧面板视图模型
│   ├── CenterPanelViewModel.cs # 中心面板视图模型
│   ├── RightPanelViewModel.cs # 右侧面板视图模型
│   └── DecouplingMatrixViewModel.cs # 解耦矩阵视图模型
├── Views/                  # 视图
│   ├── MainWindow.xaml         # 主窗口
│   ├── LeftPanel.xaml          # 左侧面板
│   ├── CenterPanel.xaml        # 中心面板
│   ├── RightPanel.xaml         # 右侧面板
│   └── DecouplingMatrixView.xaml # 解耦矩阵视图
├── Configs/                # 配置文件
│   └── register_config.json    # 寄存器配置
└── Assets/                 # 资源文件
    └── Images/
        └── favicon.ico         # 应用图标
```

## 支持的设备型号

### DR304A
- **力值寄存器**: 地址 2560, 12个寄存器
- **mV值寄存器**: 地址 2640, 12个寄存器
- **清除功能地址**: 2592
- **解耦矩阵**: 1616起始地址，6×6矩阵

### 嵌入式
- **力值寄存器**: 地址 2560, 12个寄存器
- **mV值寄存器**: 地址 2760, 12个寄存器
- **清除功能地址**: 1574
- **解耦矩阵**: 1616起始地址，6×6矩阵

### 503B
- **力值寄存器**: 地址 2020, 12个寄存器
- **mV值寄存器**: 地址 2000, 12个寄存器
- **清除功能地址**: 3000
- **注意**: 该型号不支持解耦录入功能

## 快速开始

### 系统要求
- Windows 10/11
- .NET 8.0 Runtime 或 .NET 10.0 Runtime
- 串口设备（USB转串口或RS485转换器）

### 安装运行
1. 下载最新版本的SixForce.exe
2. 确保已安装相应的.NET Runtime
3. 连接六维力传感器设备到串口
4. 运行SixForce.exe

### 基本操作
1. **选择设备型号**: 在顶部下拉框中选择对应的传感器型号
2. **连接设备**: 配置串口参数并建立连接
3. **开始采集**: 启动实时数据采集
4. **解耦设置**: 根据需要编辑解耦矩阵
5. **通道校准**: 使用清除功能进行通道校准

## 配置说明

### 寄存器配置 (register_config.json)
配置文件定义了不同型号设备的Modbus寄存器映射：
- `ForceStartAddress`: 力值数据起始地址
- `MvStartAddress`: mV值数据起始地址
- `ClearFunctionAddress`: 清除功能地址
- `DecouplingStartAddress`: 解耦矩阵起始地址
- `SaveParametersAddress`: 参数保存地址

### 自定义配置
如需支持新的设备型号，在`Configs/register_config.json`中添加新的配置项即可。

## API参考

### IModbusService接口
```csharp
public interface IModbusService: IDisposable
{
    bool IsConnected { get; }
    byte SlaveId { get; set; }
    void Connect(string portName, int baudRate);
    void Disconnect();
    void StartReading(
        Action<Dictionary<string, (string mvValue, string forceValue)>> dataReceivedCallback, 
        Action<Exception> errorCallback,
        int interval = 50);
    void StopReading();
    Task ClearChannelAsync(int channel);
    void SetRegisterMap(String key, ModbusRegisterMap map);
    Task<int[,]> ReadDecouplingMatrixAsync();
    Task WriteDecouplingMatrixAsync(int[,] matrix);
    Task SaveParametersAsync();
}
```

## 故障排除

### 常见问题
1. **连接失败**: 检查串口参数设置和设备连接
2. **数据异常**: 确认设备型号选择正确
3. **解耦操作失败**: 检查设备是否支持解耦功能（503B不支持）
4. **通信超时**: 调整串口波特率和通信超时设置

### 日志信息
程序运行时会输出详细的调试信息到控制台，包括：
- Modbus请求和响应数据
- CRC校验结果
- 异常错误信息

## 开发说明

### 构建项目
```bash
# 使用Visual Studio
1. 打开SixForce.csproj
2. 选择目标框架（.NET 8.0或10.0）
3. 生成解决方案

# 使用dotnet CLI
dotnet build --framework net10.0-windows
```

### 运行调试
```bash
# Debug模式运行（显示控制台窗口）
dotnet run --configuration Debug

# Release模式运行
dotnet run --configuration Release
```

## 许可证

本项目采用专有许可证。详情请联系开发团队。

## 技术支持

如需技术支持或有任何问题，请联系：
- 技术支持邮箱: support@sixforce.com
- 项目维护者: DST德森特团队

## 版本历史

### v1.0.0 (当前版本)
- 支持DR304A、嵌入式、503B三种设备型号
- 实现完整的Modbus RTU通信
- 提供解耦矩阵编辑功能
- 支持通道清除和参数保存
- 现代化Material Design界面

---

**注意**: 本软件专为六维力传感器标定和数据分析而设计，请确保在使用前正确理解设备规格和操作流程。