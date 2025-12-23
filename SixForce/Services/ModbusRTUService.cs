using SixForce.Constants;
using SixForce.Models;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows;
using System.Windows.Documents;

namespace SixForce.Services
{
    public class ModbusRTUService : IModbusService
    {
        private readonly SerialPort _serialPort = new();
        private CancellationTokenSource? _cts;
        private Action<Dictionary<string, (string mvValue, string forceValue)>>? _callback;
        private readonly object _serialLock = new();

        private ModbusRegisterMap? _map;

        private volatile bool _isReadingPaused = false;

        public void SetRegisterMap(String key, ModbusRegisterMap map)
        {
            _map = map;
        }


        public byte SlaveId { get; set; } = 1; // 实现接口中的SlaveId属性

        public bool IsConnected => _serialPort.IsOpen;

        public void Connect(string portName, int baudRate)
        {
            if (_serialPort.IsOpen) return;

            _serialPort.PortName = portName;
            _serialPort.BaudRate = baudRate;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Open();

        }

        public void Disconnect()
        {
            try
            {
                StopReading();
                
                // 使用超时机制避免阻塞
                if (_serialPort.IsOpen)
                {
                    // 尝试获取锁，但设置超时避免死锁
                    if (Monitor.TryEnter(_serialLock, TimeSpan.FromMilliseconds(1000)))
                    {
                        try
                        {
                            if (_serialPort.IsOpen)
                            {
                                _serialPort.Close();
                            }
                        }
                        finally
                        {
                            Monitor.Exit(_serialLock);
                        }
                    }
                    else
                    {
                        // 如果无法获取锁，强制关闭串口
                        try
                        {
                            _serialPort.Close();
                        }
                        catch
                        {
                            // 忽略关闭时的异常
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"断开连接时发生异常: {ex.Message}");
                // 不重新抛出异常，确保断开操作能够完成
            }
        }

        public void StartReading(
            Action<Dictionary<string, (string mvValue, string forceValue)>> dataReceivedCallback,
            Action<Exception> errorCallback,
            int interval = 50)
        {
            StopReading();
            if (!_serialPort.IsOpen) throw new InvalidOperationException("Serial port not connected");

                _callback = dataReceivedCallback;
                _cts = new CancellationTokenSource();
                var cts = _cts;

                Task.Run(async () =>
                {
                    while (!cts.IsCancellationRequested)
                    {

                        // 在每次循环开始时检查是否需要暂停
                        if (_isReadingPaused)
                        {
                            await Task.Delay(100, cts.Token); // 暂停时也做个短暂等待，避免空转
                            continue;
                        }

                        try
                        {
                            if (_map == null)
                            {
                                throw new InvalidOperationException("StartReading  Modbus寄存器映射未设置");
                            }

                            // 读取mV值（整型格式，12个寄存器）
                            byte[] mvRequest = BuildModbusReadRequest(SlaveId, _map.MvStartAddress, _map.MvRegisterCount);
                            byte[] mvResponse = SendModbusRequest(mvRequest);
                            var mvValues = ParseMvValues(mvResponse);

                            // 在两次请求之间增加一个短暂的延时，给设备处理时间
                            await Task.Delay(AppConstants.Modbus.DefaultReadDelayMs, cts.Token);

                            // 读取力值（整型格式，12个寄存器）
                            byte[] forceRequest = BuildModbusReadRequest(SlaveId, _map.ForceStartAddress, _map.ForceRegisterCount);
                            byte[] forceResponse = SendModbusRequest(forceRequest);
                            var forceValues = ParseForceValues(forceResponse);

                            // 组合数据
                            var data = new Dictionary<string, (string mvValue, string forceValue)>
                            {
                                ["Fx"] = (mvValues[0], forceValues[0]),
                                ["Fy"] = (mvValues[1], forceValues[1]),
                                ["Fz"] = (mvValues[2], forceValues[2]),
                                ["Mx"] = (mvValues[3], forceValues[3]),
                                ["My"] = (mvValues[4], forceValues[4]),
                                ["Mz"] = (mvValues[5], forceValues[5])
                            };

                            _callback?.Invoke(data);
                        }
                        catch (Exception ex)
                        {
                            if (!cts.IsCancellationRequested)
                            {
                                await Task.Delay(AppConstants.DataAcquisition.ErrorRetryWaitTimeMs, cts.Token); // 错误后重试等待
                                continue;
                            }
                            errorCallback?.Invoke(ex); // 通知异常
                            Console.WriteLine($"Modbus读取错误: {ex.Message}");
                            // 不要在这里直接调用Disconnect，避免递归调用和死锁
                            // 让上层处理连接断开
                            break;
                        }

                        await Task.Delay(interval, cts.Token);
                    }
                }, cts.Token);
        }

        // 新增公开方法用于控制暂停和恢复
        public void PauseReading()
        {
            _isReadingPaused = true;
        }

        public void ResumeReading()
        {
            _isReadingPaused = false;
        }

        public void StopReading()
        {
            _cts?.Cancel();
        }

        public void Dispose()
        {
            Disconnect();
            _serialPort.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task ClearChannelAsync(int channel)
        {
            // 暂停后台读取
            PauseReading();
            // 等待一小段时间，确保当前正在进行的读取操作完成
            await Task.Delay(AppConstants.Modbus.ClearChannelWaitTimeMs);
            try
            {

                if (_map == null)
                    throw new InvalidOperationException("Modbus寄存器映射未设置");
                if (!_serialPort.IsOpen)
                    throw new InvalidOperationException("串口未连接");
                // 多功能码地址：1574 (0x0626)，寄存器数量：2
                ushort address = _map.ClearFunctionAddress;
                ushort count = 2;
                byte functionCode;
                if (channel >= 1 && channel <= 6)
                    functionCode = (byte)(_map.ClearChannelStartCode + (channel - 1)); // 通道1=0x15, 通道6=0x1A
                else if (channel == 7)
                    functionCode = _map.ClearAllChannelsCode; // 所有通道
                else
                    throw new ArgumentException("通道号必须是1-7");

                byte[] request = new byte[13];
                request[0] = SlaveId;
                request[1] = 0x10; // 写多个寄存器
                request[2] = (byte)(address >> 8);
                request[3] = (byte)(address & 0xFF);
                request[4] = (byte)(count >> 8);
                request[5] = (byte)(count & 0xFF);
                request[6] = 0x04; // 字节数（写2个寄存器=4字节）
                request[7] = 0x00;
                request[8] = 0x00;
                request[9] = 0x00;
                request[10] = functionCode; // 功能码

                var crc = CalculateCRC(request, 0, 11);
                request[11] = (byte)(crc & 0xFF);
                request[12] = (byte)(crc >> 8);

                SendModbusRequest(request);
            }
            finally
            {
                // 无论成功还是失败，都要恢复后台读取
                ResumeReading();
            }
        }

        public async Task<int[,]> ReadDecouplingMatrixAsync()
        {
            if (_map == null) throw new InvalidOperationException("寄存器映射未设置");
            if (!_serialPort.IsOpen) throw new InvalidOperationException("串口未连接");

            int[,] matrix = new int[_map.DecouplingRowCount, _map.ElementsPerRow];

            for (int row = 0; row < matrix.GetLength(0); row++)
            {
                for (int col = 0; col < matrix.GetLength(1); col++)
                {
                    ushort address = (ushort)(_map.DecouplingStartAddress +
                        row * (_map.ElementsPerRow * _map.RegistersPerElement + _map.SkipRegistersPerRow) +
                        col * _map.RegistersPerElement);

                    bool success = false;

                    for (int retry = 0; retry < 3 && !success; retry++)
                    {
                        try
                        {
                            var req = BuildModbusReadRequest(SlaveId, address, 2);
                            var resp = SendModbusRequest(req);

                            // 解析返回值（两个寄存器组成 32 位）
                            if (resp.Length >= 9 && resp[1] == 0x03)
                            {
                                int value = (resp[3] << 24) | (resp[4] << 16) |
                                            (resp[5] << 8) | resp[6];
                                matrix[row, col] = value;
                                success = true;
                            }
                            else if (resp.Length > 1 && (resp[1] & 0x80) != 0)
                            {
                                throw new Exception($"设备返回异常码 {resp[2]:X2}");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (retry == 2)
                                throw new Exception($"读取 [{row},{col}] 失败: {ex.Message}");
                            await Task.Delay(AppConstants.Modbus.DecouplingRetryDelayMs);
                        }
                                            }
                    
                                            await Task.Delay(AppConstants.Modbus.DecouplingWriteIntervalMs);                }
            }

            return matrix;
        }

        public async Task WriteDecouplingMatrixAsync(int[,] matrix)
        {
            if (_map == null) throw new InvalidOperationException("寄存器映射未设置");
            if (!_serialPort.IsOpen) throw new InvalidOperationException("串口未连接");

            for (int row = 0; row < matrix.GetLength(0); row++)
            {
                for (int col = 0; col < matrix.GetLength(1); col++)
                {
                    ushort address = (ushort)(_map.DecouplingStartAddress +
                        row * (_map.ElementsPerRow * _map.RegistersPerElement + _map.SkipRegistersPerRow) +
                        col * _map.RegistersPerElement);

                    int value = matrix[row, col];
                    bool success = false;

                    for (int retry = 0; retry < 3 && !success; retry++)
                    {
                        try
                        {
                            var req = BuildWriteTwoRegistersRequest(SlaveId, address, value);
                            var resp = SendModbusRequest(req);

                            if (resp.Length >= 6 && resp[1] == 0x10)
                            {
                                success = true;
                            }
                            else if (resp.Length > 1 && (resp[1] & 0x80) != 0)
                            {
                                throw new Exception($"设备返回异常码 {resp[2]:X2}");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (retry == 2)
                                throw new Exception($"写入 [{row},{col}] 失败: {ex.Message}");
                            await Task.Delay(AppConstants.Modbus.DecouplingRetryDelayMs); // 重试前等一会
                        }
                    }

                    await Task.Delay(AppConstants.Modbus.DecouplingWriteIntervalMs); // 给设备缓冲时间
                }
            }
            await SaveParametersAsync();
        }

        public async Task SaveParametersAsync()
        {
            if (_map == null) throw new InvalidOperationException("寄存器映射未设置");
            if (!_serialPort.IsOpen) throw new InvalidOperationException("串口未连接");
            Console.WriteLine($"保存参数: 地址=0x{_map.SaveParametersAddress:X4}, 值={string.Join(",", _map.SaveParametersValue)}");
            await WriteRegistersAsync(_map.SaveParametersAddress, _map.SaveParametersValue);
            await Task.Delay(100); // 等待设备处理
        }

        private async Task WriteRegistersAsync(ushort startAddress, int[] values)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("值数组不能为空");
            // 移除 values.Length == 4 检查
            Console.WriteLine($"写入寄存器: 地址=0x{startAddress:X4}, 值={string.Join(",", values)}");

            var request = BuildWriteRegistersRequest(SlaveId, startAddress, values);
            var response = SendModbusRequest(request);

            if (response.Length < 6 || response[1] != 0x10)
                throw new InvalidOperationException($"写入失败，响应无效: {BitConverter.ToString(response)}");

            await Task.Delay(50); // 给设备一些时间处理
        }

        private static byte[] BuildWriteRegistersRequest(byte slaveId, ushort startAddress, int[] values)
        {
            if (values == null || values.Length == 0) throw new ArgumentException("Values cannot be empty");
            int registerCount = values.Length * 2;  // 每个int占2寄存器
            int byteCount = values.Length * 4;     // 每个int占4字节
            byte[] request = new byte[7 + byteCount + 2];

            request[0] = slaveId;
            request[1] = 0x10;
            request[2] = (byte)(startAddress >> 8);
            request[3] = (byte)(startAddress & 0xFF);
            request[4] = (byte)(registerCount >> 8);
            request[5] = (byte)(registerCount & 0xFF);
            request[6] = (byte)byteCount;

            for (int i = 0; i < values.Length; i++)
            {
                ushort highWord = (ushort)(values[i] >> 16);
                ushort lowWord = (ushort)(values[i] & 0xFFFF);
                request[7 + i * 4] = (byte)(highWord >> 8);
                request[7 + i * 4 + 1] = (byte)(highWord & 0xFF);
                request[7 + i * 4 + 2] = (byte)(lowWord >> 8);
                request[7 + i * 4 + 3] = (byte)(lowWord & 0xFF);
            }

            ushort crc = CalculateCRC(request, 0, 7 + byteCount);
            request[7 + byteCount] = (byte)(crc & 0xFF);
            request[7 + byteCount + 1] = (byte)(crc >> 8);
            return request;
        }


        private static byte[] BuildModbusReadRequest(byte slaveId, ushort startAddress, ushort registerCount)
        {
            byte[] request = new byte[8];
            request[0] = slaveId; // 从机地址
            request[1] = 0x03; // 功能码：读取保持寄存器
            request[2] = (byte)(startAddress >> 8); // 起始地址高字节
            request[3] = (byte)(startAddress & 0xFF); // 起始地址低字节
            request[4] = (byte)(registerCount >> 8); // 寄存器数量高字节
            request[5] = (byte)(registerCount & 0xFF); // 寄存器数量低字节
            var crc = CalculateCRC(request, 0, 6);
            request[6] = (byte)(crc & 0xFF); // CRC低字节
            request[7] = (byte)(crc >> 8); // CRC高字节
            return request;
        }

        /// <summary>
        /// 构造一个写 2 个寄存器（一个 32 位值）的请求
        /// 功能码 0x10, 长度固定 2
        /// </summary>
        private static byte[] BuildWriteTwoRegistersRequest(byte slaveId, ushort startAddress, int value)
        {
            ushort highWord = (ushort)(value >> 16);
            ushort lowWord = (ushort)(value & 0xFFFF);

            byte[] request = new byte[13];
            request[0] = slaveId;
            request[1] = 0x10; // 写多个寄存器
            request[2] = (byte)(startAddress >> 8);
            request[3] = (byte)(startAddress & 0xFF);
            request[4] = 0x00; // 寄存器数量高字节
            request[5] = 0x02; // 寄存器数量低字节（2个寄存器）
            request[6] = 0x04; // 字节计数（4字节）

            // 数据，高16位在前，低16位在后
            request[7] = (byte)(highWord >> 8);
            request[8] = (byte)(highWord & 0xFF);
            request[9] = (byte)(lowWord >> 8);
            request[10] = (byte)(lowWord & 0xFF);

            ushort crc = CalculateCRC(request, 0, 11);
            request[11] = (byte)(crc & 0xFF);
            request[12] = (byte)(crc >> 8);

            return request;
        }


        private byte[] SendModbusRequest(byte[] request)
        {
            if (!_serialPort.IsOpen)
                throw new InvalidOperationException("串口未打开，无法发送请求");

            // 使用TryEnter避免无限等待
            if (!Monitor.TryEnter(_serialLock, TimeSpan.FromMilliseconds(5000)))
                throw new TimeoutException("获取串口锁超时，可能存在死锁");

            try
            {
                Trace.WriteLine("发送: " + BitConverter.ToString(request));
                Console.WriteLine("发送: " + BitConverter.ToString(request));
                _serialPort.DiscardInBuffer();
                _serialPort.Write(request, 0, request.Length);

                int expectedLength = GetExpectedResponseLength(request);
                byte[] response = new byte[expectedLength > 5 ? expectedLength : 5]; // 最小支持 5 字节异常响应
                int bytesRead = 0;
                DateTime startTime = DateTime.Now;
                TimeSpan timeout = TimeSpan.FromMilliseconds(AppConstants.Modbus.DefaultTimeoutMs); // 使用配置的超时时间

                while (bytesRead < expectedLength)
                {
                    // 检查串口是否仍然打开
                    if (!_serialPort.IsOpen)
                        throw new InvalidOperationException("串口在读取过程中被关闭");

                    if (DateTime.Now - startTime > timeout)
                        throw new TimeoutException($"读取Modbus响应超时，预期长度：{expectedLength}，已接收：{bytesRead}");
                    
                    if (_cts?.IsCancellationRequested == true)
                        throw new OperationCanceledException();

                    int available = _serialPort.BytesToRead;
                    if (available > 0)
                    {
                        int toRead = Math.Min(available, expectedLength - bytesRead);
                        int n = _serialPort.Read(response, bytesRead, toRead);
                        bytesRead += n;
                        Trace.WriteLine($"接收中: {BitConverter.ToString([.. response.Take(bytesRead)])}");
                        Console.WriteLine($"接收中: {BitConverter.ToString([.. response.Take(bytesRead)])}");
                    }
                    else
                    {
                        // 使用更短的等待时间，提高响应性
                        Thread.Sleep(5);
                    }
                }

                Trace.WriteLine("接收完成: " + BitConverter.ToString([.. response.Take(bytesRead)]));
                Console.WriteLine("接收完成: " + BitConverter.ToString([.. response.Take(bytesRead)]));

                // 检查异常响应
                if ((response[1] & 0x80) != 0) // 异常响应
                {
                    byte errorCode = response[2];
                    throw new InvalidOperationException($"Modbus异常响应，错误码：0x{errorCode:X2}");
                }

                // 校验从机地址
                if (response[0] != request[0])
                    throw new InvalidOperationException($"响应从机地址不匹配，期望：{request[0]}，实际：{response[0]}");

                // 校验功能码
                if (response[1] != request[1])
                    throw new InvalidOperationException($"响应功能码不匹配，期望：{request[1]}，实际：{response[1]}");

                // 校验 CRC
                ushort crc = CalculateCRC(response, 0, bytesRead - 2);
                if (response[bytesRead - 2] != (byte)(crc & 0xFF) || response[bytesRead - 1] != (byte)(crc >> 8))
                    throw new Exception("CRC校验失败");

                return response;
            }
            catch (TimeoutException ex)
            {
                Trace.WriteLine("超时异常: " + ex.Message);
                Console.WriteLine("超时异常: " + ex.Message);
                throw; // 重新抛出TimeoutException，不要包装
            }
            catch (Exception ex)
            {
                Trace.WriteLine("异常: " + ex.Message);
                Console.WriteLine("异常: " + ex.Message);
                throw new InvalidOperationException($"发送Modbus请求失败：{ex.Message}", ex);
            }
            finally
            {
                Monitor.Exit(_serialLock);
            }
        }


        private static string[] ParseForceValues(byte[] response)
        {
            string[] channels = ["Fx", "Fy", "Fz", "Mx", "My", "Mz"];
            string[] values = new string[6];

            // 验证响应长度：地址(1) + 功能码(1) + 字节计数(1) + 数据(24) + CRC(2) = 29 字节
            if (response.Length < 3 + 6 * 4 + 2)
            {
                throw new InvalidOperationException($"力值数据解析失败，响应长度不足，期望：29 字节，实际：{response.Length} 字节");
            }

            // 验证字节计数
            if (response[2] != 6 * 4)
            {
                throw new InvalidOperationException($"力值数据字节计数错误，期望：24 字节，实际：{response[2]} 字节");
            }

            for (int i = 0; i < 6; i++)
            {
                int index = 3 + i * 4; // 每个通道占4字节
                if (index + 3 >= response.Length)
                {
                    throw new InvalidOperationException($"力值数据解析失败，通道 {channels[i]} 数据不足，索引：{index}");
                }

                // 解析为32位整型数
                int value = (response[index] << 24) | (response[index + 1] << 16) |
                            (response[index + 2] << 8) | response[index + 3];
                //values[i] = (value / 100.0).ToString("F2"); // 力值 = 值/100，保留2位小数
                values[i] = value.ToString(); // 力值 = 值/100，保留2位小数
            }

            return values;
        }

        private static string[] ParseMvValues(byte[] response)
        {
            string[] values = new string[6];

            // 验证响应长度
            if (response.Length < 3 + 6 * 4 + 2)
            {
                throw new InvalidOperationException($"mV值数据解析失败，响应长度不足，期望：29 字节，实际：{response.Length} 字节");
            }

            // 验证字节计数
            if (response[2] != 6 * 4)
            {
                throw new InvalidOperationException($"mV值数据字节计数错误，期望：24 字节，实际：{response[2]} 字节");
            }

            for (int i = 0; i < 6; i++)
            {
                int index = 3 + i * 4; // 每个通道占 4 字节
                if (index + 3 >= response.Length)
                {
                    throw new InvalidOperationException($"mV值数据解析失败，通道 {i + 1} 数据不足，索引：{index}");
                }
                int value = (response[index] << 24) | (response[index + 1] << 16) |
                            (response[index + 2] << 8) | response[index + 3];
                //values[i] = (value / 100000.0).ToString("F3"); // mV值 = 值/100000，保留 3 位小数
                values[i] = value.ToString(); // mV值 = 值/100000，保留 3 位小数
            }

            return values;
        }

        private static ushort CalculateCRC(byte[] data, int start, int length)
        {
            ushort crc = 0xFFFF;
            for (int pos = start; pos < start + length; pos++)
            {
                crc ^= data[pos];
                for (int i = 8; i > 0; i--)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }

        private static int GetExpectedResponseLength(byte[] request)
        {
            byte functionCode = request[1];
            ushort registerCount = (ushort)((request[4] << 8) | request[5]);

            return functionCode switch
            {
                0x03 => 3 + 2 * registerCount + 2, // 读保持寄存器，29 字节（3 + 24 + 2）
                0x10 => 8,                        // 写多个寄存器，8 字节（成功响应）
                0x06 => 8,                        // 写单个寄存器
                _ => throw new NotSupportedException($"暂不支持功能码 {functionCode:X2}")
            };
        }

    }
}
