using System;

namespace SixForce.Exceptions
{
    /// <summary>
    /// Modbus通信异常
    /// </summary>
    public class ModbusException : Exception
    {
        /// <summary>
        /// Modbus错误码（如果有）
        /// </summary>
        public byte ErrorCode { get; }
        
        /// <summary>
        /// 异常类型
        /// </summary>
        public ModbusExceptionType ExceptionType { get; }
        
        /// <summary>
        /// 初始化新实例
        /// </summary>
        public ModbusException()
        {
        }
        
        /// <summary>
        /// 初始化新实例
        /// </summary>
        public ModbusException(string message) 
            : base(message)
        {
        }
        
        /// <summary>
        /// 初始化新实例
        /// </summary>
        public ModbusException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
        
        /// <summary>
        /// 初始化新实例
        /// </summary>
        public ModbusException(string message, byte errorCode) 
            : base($"{message} (错误码: 0x{errorCode:X2})")
        {
            ErrorCode = errorCode;
        }
        
        /// <summary>
        /// 初始化新实例
        /// </summary>
        public ModbusException(string message, ModbusExceptionType exceptionType) 
            : base(message)
        {
            ExceptionType = exceptionType;
        }
    }
    
    /// <summary>
    /// Modbus异常类型
    /// </summary>
    public enum ModbusExceptionType
    {
        /// <summary>
        /// 通信超时
        /// </summary>
        Timeout,
        
        /// <summary>
        /// CRC校验失败
        /// </summary>
        CrcError,
        
        /// <summary>
        /// 从机地址不匹配
        /// </summary>
        SlaveIdMismatch,
        
        /// <summary>
        /// 功能码错误
        /// </summary>
        FunctionCodeError,
        
        /// <summary>
        /// 设备返回异常
        /// </summary>
        DeviceError,
        
        /// <summary>
        /// 通信断开
        /// </summary>
        ConnectionLost
    }
}