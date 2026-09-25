using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia_Serial_assist.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia_Serial_assist.ViewModels;
using Avalonia.Threading;
using Tmds.DBus.Protocol;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Avalonia_Serial_assist.Views.Component;

public partial class LeftConfigPanel : UserControl
{
    public static UartService GlobalUart { get; private set; }
    public UartConfig UartConfig;
    public UartService UartOperator;
    public static List<string> ports;
    private bool _isOpenSerialPort = false;

    private DispatcherTimer _refreshTimer;

    public LeftConfigPanel()
    {
        InitializeComponent();



        UartOperator = new UartService();
        GlobalUart = UartOperator; //全局

        if (UartOperator.GetPorts().Count == 0)
        {
            CBPORT.Items.Add(new ComboBoxItem { Content = "No Ports" });
        }

        if (UartOperator.GetPorts() == ports) return;

        ports = UartOperator.GetPorts();
        CBPORT.Items.Clear();
        for (int i = 0; i < ports.Count; i++)
        {
            CBPORT.Items.Add(new ComboBoxItem { Content = ports[i] });
        }

        CBPORT.SelectedIndex = 0;
        Debug.WriteLine($"Refreshed {ports.Count} ports");
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _refreshTimer.Stop();
        _refreshTimer.Tick -= RefreshFunc;
        base.OnUnloaded(e);
    }

    private void enableRefreshTimer()
    {
        _refreshTimer = new DispatcherTimer();
        _refreshTimer.Interval = TimeSpan.FromMilliseconds(int.Parse(TBXAutoSendTime.Text));
        _refreshTimer.Tick += RefreshFunc;
        _refreshTimer.Start();
    }

    private void RefreshFunc(object? sender, EventArgs e)
    {
        // 定时发送启用
        Components.RATPanel.Instance.GetSendData();
        GlobalUart.SendLine(Components.RATPanel.Instance.GetSendData(), Components.RATPanel.Instance.GetEncode());
        Components.RATPanel.Instance.AddToWindowItems(Components.RATPanel.Instance.GetSendData(), "TX");
    }


    public void configUART()
    {
        var baudItem = CBBaudRate.SelectedItem as ComboBoxItem;
        var portItem = CBPORT.SelectedItem as ComboBoxItem;
        var parityItem = CBParity.SelectedItem as ComboBoxItem;
        var dataItem = CBDataBits.SelectedItem as ComboBoxItem;
        var stopItem = CBStopBits.SelectedItem as ComboBoxItem;

        Debug.WriteLine($"configUART {baudItem} {portItem} {parityItem} {dataItem} {stopItem}");

        if (baudItem == null || portItem == null || parityItem == null || dataItem == null || stopItem == null)
            return;

        //     <ComboBoxItem Content="None" />
        // <ComboBoxItem Content="Odd" />
        // <ComboBoxItem Content="Even" />
        // <ComboBoxItem Content="Space" />
        string parityStr = parityItem.Content.ToString().Trim();
        UartConfig = new UartConfig

        {
            BaudRate = int.Parse(baudItem.Content.ToString()),
            PortName = portItem.Content.ToString(),
            Parity = (Parity)Enum.Parse(typeof(Parity), parityStr),
            DataBits = int.Parse(dataItem.Content.ToString()),
            StopBits = (StopBits)int.Parse(stopItem.Content.ToString()),
        };
    }

    private async void BTNOpenSerial_OnClick(object? sender, RoutedEventArgs e)
    {
        configUART();

        Debug.WriteLine($"CBPORT.SelectedIndex {CBPORT.SelectedIndex}");

        //如果为已打开状态，关闭串口
        if (_isOpenSerialPort)
        {
            var sucess = UartOperator.Close();
            if (!sucess)
            {
                await MessageBox("关闭串口失败");
            }

            _isOpenSerialPort = false;
            BTNOpenSerial.IsChecked = false;
            BTNOpenSerial.Content = "打开串口";

            return;
        }

        if (UartConfig != null)
        {
            var isOpen = UartOperator.Open(UartConfig);
            if (isOpen)
            {
                _isOpenSerialPort = true;
                BTNOpenSerial.IsChecked = true;
                BTNOpenSerial.Content = "关闭串口";
            }
            else
            {
                await MessageBox("打开串口失败");
            }
        }
    }

    private async Task MessageBox(string msg)
    {
        var box = MessageBoxManager.GetMessageBoxStandard("错误", msg, (ButtonEnum)ButtonResult.Ok, Icon.Error);
        await box.ShowAsync();
    }

    private void CBPORT_OnDropDownOpened(object? sender, EventArgs e)
    {

        if (UartOperator.GetPorts().Count == 0)
        {
            CBPORT.Items.Add(new ComboBoxItem { Content = "No Ports" });
        }


        if (UartOperator.GetPorts() == ports) return;

        ports = UartOperator.GetPorts();
        CBPORT.Items.Clear();
        for (int i = 0; i < ports.Count; i++)
        {
            CBPORT.Items.Add(new ComboBoxItem { Content = ports[i] });
        }

        CBPORT.SelectedIndex = 0;
        Debug.WriteLine($"Refreshed {ports.Count} ports");
    }

    private void BTNCountRST_OnClick(object? sender, RoutedEventArgs e)
    {
        Components.RATPanel.Instance.ClearData();
    }

    // 自动发送
    private void CBTXAutoSend_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!IsWorkNormal())
        {
            CBTXAutoSend.IsChecked = false;
            return;
        }

        if (CBTXAutoSend.IsChecked == true)
        {
            enableRefreshTimer();
            // _refreshTimer.Interval = TimeSpan.FromMilliseconds(int.Parse(TBXAutoSendTime.Text)/1000); // 转换为ms
        }
        else
        {
            _refreshTimer.Stop();
        }
    }

    //判断是否工作处于正常态
    private bool IsWorkNormal()
    {
        // 先判断控件存在、文本非空
        if (TBXAutoSendTime == null || string.IsNullOrEmpty(TBXAutoSendTime.Text))
            return false;

        // 串口未打开直接false
        if (!_isOpenSerialPort)
            return false;

        // 判断RATPanel实例是否存在
        var ratPanel = Components.RATPanel.Instance;
        if (ratPanel == null)
            return false;

        // 获取发送数据并判空再取长度
        string sendData = ratPanel.GetSendData();
        if (string.IsNullOrEmpty(sendData))
            return false;

        return sendData.Length > 0;
    }

    private void TBRTSSet_OnClick(object? sender, RoutedEventArgs e)
    {
        if (UartOperator.GetRtsState())
        {
            UartOperator.SetRts(false);
            TBRTSSet.IsChecked = false;
            return;
        }
        
        //需要对串口重开关处理
        if (_isOpenSerialPort)
        {
            UartOperator.Close();
            _isOpenSerialPort = false;
            UartOperator.SetRts(true);
            _isOpenSerialPort=UartOperator.Open(UartConfig);
            TBRTSSet.IsChecked = true;
            return;
        }
        UartOperator.SetRts(true);
        TBRTSSet.IsChecked = true;

        
    }

    private void TBDTRSet_OnClick(object? sender, RoutedEventArgs e)
    {
        if (UartOperator.GetDtrState())
        {
            UartOperator.SetDtr(false);
            TBDTRSet.IsChecked = false;
            return;
        }
        
        //需要对串口重开关处理
        if (_isOpenSerialPort)
        {
            UartOperator.Close();
            _isOpenSerialPort = false;
            UartOperator.SetDtr(true);
            _isOpenSerialPort=UartOperator.Open(UartConfig);
            TBDTRSet.IsChecked = true;
            return;
        }
        UartOperator.SetDtr(true);
        TBDTRSet.IsChecked = true;

    }

    private void CBRXHexDisplay_OnClick(object? sender, RoutedEventArgs e)
    {
        UartOperator.IsHexRx = CBRXHexDisplay.IsChecked == !UartOperator.IsHexRx;
        
    }

    private void CBTXHex_OnClick(object? sender, RoutedEventArgs e)
    {
        UartOperator.IsHexTx = CBTXHex.IsChecked == !UartOperator.IsHexTx;
    }
}