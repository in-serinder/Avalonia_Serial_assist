using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Avalonia_Serial_assist.ViewModels;
using System;
using System.IO.Ports;

// 串口配置 Model
public class UartConfig
{
    public string PortName { get; set; } = "";
    public int BaudRate { get; set; }
    public Parity Parity { get; set; }
    public int DataBits { get; set; }
    public StopBits StopBits { get; set; }
}

// 数据接收事件参数
public class UartDataReceivedArgs : EventArgs
{
    public byte[] Text { get; }
    public int Length { get; }
    public string TXText { get; }
    public int TXLength { get; }
    public UartDataReceivedArgs(byte[] text, int len,string txtext,int txlen)
    {
        Text = text;
        Length = len;
        TXText = txtext;
        TXLength = txlen;
    }
}

// 实例串口服务，无static
public class UartService
{
    private SerialPort? _serialPort;
    private string _rxStr = string.Empty;
    private int _rxLen = 0;
    private string _txstr = string.Empty;
    private int _txlen = 0;

    public bool RTS { get; set; }
    public bool DTR { get; set; }
    
    public bool IsHexRx { get; set; } = false;
    public bool IsHexTx { get; set; } = false;


    // 数据接收事件，推送给ViewModel
    public event EventHandler<UartDataReceivedArgs>? DataReceived;

    public bool Open(UartConfig config)
    {
        _serialPort = new SerialPort(
            config.PortName,
            config.BaudRate,
            config.Parity,
            config.DataBits,
            config.StopBits
        );
        try
        {
            _serialPort.Open();
            _serialPort.DataReceived += OnSerialDataReceived;
            _serialPort.RtsEnable = RTS;
            _serialPort.DtrEnable = DTR;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Close()
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.DataReceived -= OnSerialDataReceived;
            _serialPort.Close();
            _serialPort.Dispose();
            _serialPort = null;
            return true;
        }
        return false;
    }
    public void SetRts(bool enable)
    {
        RTS = enable;
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.RtsEnable = enable;
        }
    }
    
    public void SetDtr(bool enable)
    {
        DTR = enable;
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.DtrEnable = enable;
        }
    }
    
    public bool GetRtsState()
    {
        if (_serialPort != null && _serialPort.IsOpen)
            return _serialPort.RtsEnable;
        return RTS;
    }

    
    public bool GetDtrState()
    {
        if (_serialPort != null && _serialPort.IsOpen)
            return _serialPort.DtrEnable;
        return DTR;
    }
    
    public void SendString(string text)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
            return;
        _serialPort.Write(text);
    }
    
    public static string BytesToHexString(byte[] data)
    {
        if (data == null || data.Length == 0)
            return string.Empty;

        return string.Join(" ", data.Select(b => b.ToString("X2")));
    }
    
    public string StringToHex(string text, string encodingName = "UTF-8")
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        Encoding enc = encodingName switch
        {
            "GBK" => Encoding.GetEncoding("GBK"),
            "GB2312" => Encoding.GetEncoding("GB2312"),
            "ASCII" => Encoding.ASCII,
            _ => Encoding.UTF8
        };

        byte[] buf = enc.GetBytes(text);
        return BytesToHexString(buf);
    }
    
    public void SendLine(string text, string encName = "UTF-8")
    {
        if (_serialPort == null || !_serialPort.IsOpen)
            return;

        Encoding enc = encName switch
        {
            "UTF-8" => Encoding.UTF8,
            "GBK" => Encoding.GetEncoding("GBK"),
            "GB2312" => Encoding.GetEncoding("GB2312"),
            _ => Encoding.UTF8
        };

        // 文本 + 标准串口换行 \r\n
        byte[] data = enc.GetBytes(text + "\r\n");
        SendBytes(data);

        // 记录发送缓存与长度
        _txstr += text + "\r\n";
        _txlen += data.Length;
    }


    public void SendBytes(byte[] buffer, int offset = 0, int count = -1)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
            return;
        if (count == -1) count = buffer.Length;
        _serialPort.Write(buffer, offset, count);
    }

    public List<string> GetPorts()
    {
        return SerialPort.GetPortNames().ToList();
    }

    private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort is not { IsOpen: true }) return;
        // string buf = _serialPort.ReadExisting();
        // _rxStr += buf;
        // _rxLen += buf.Length;
        int avail = _serialPort.BytesToRead;
        byte[] rxBuffer = new byte[avail];
        _serialPort.Read(rxBuffer, 0, avail);

        // 抛出事件，把数据抛给上层ViewModel
        // DataReceived?.Invoke(this, new UartDataReceivedArgs(buf, buf.Length,_txstr,_txlen));
        DataReceived?.Invoke(this, new UartDataReceivedArgs(rxBuffer, avail,_txstr,_txlen));

    }
    
    
    
    public string BytesToString(byte[] data, string encodingName = "UTF-8")
    {
        Encoding encoding = encodingName switch
        {
            "UTF-8" => Encoding.UTF8,
            "GBK" => Encoding.GetEncoding("GBK"),
            "GB2312" => Encoding.GetEncoding("GB2312"),
            "ASCII" => Encoding.ASCII,
            "Latin1" => Encoding.Latin1,
            _ => Encoding.UTF8
        };
        return encoding.GetString(data);
    }
    // 发送数据传递 估计用不上
    public void OnSerialDataSend(string data)
    {
        // _serialPort?.Write(data);
        // _txstr += data;
        // _txlen += data.Length;
    }
    
}