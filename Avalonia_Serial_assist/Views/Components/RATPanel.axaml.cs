using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.IO.Ports;
using System.Linq;
using Avalonia_Serial_assist.ViewModels;
using Avalonia_Serial_assist.Views.Component;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Media;


namespace Avalonia_Serial_assist.Views.Components;

public partial class RATPanel : UserControl
{
    public static RATPanel Instance { get; set; }

    private ObservableCollection<WindowItem> windowItems = new ObservableCollection<WindowItem>();
    private string rxstr, txstr;
    private long rxcount, txcount;

    public RATPanel()
    {
        InitializeComponent();
        if (LeftConfigPanel.GlobalUart != null)
        {
            LeftConfigPanel.GlobalUart.DataReceived += OnSerialDataReceived;
        }

        LIBReceive.ItemsSource = windowItems;

        Instance = this;
    }

    private void OnSerialDataReceived(object? sender, UartDataReceivedArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            rxstr += e.Text;
            rxcount += e.Length;
            LabRX.Text = $"{rxcount}";

            // LabTX.Text += e.Text;
            //组合内容

            if (LeftConfigPanel.GlobalUart.IsHexRx)
            {
                var msg = LeftConfigPanel.GlobalUart.BytesToString(e.Text, CBRXEncode.SelectedItem as string);
                windowItems.Add(new WindowItem(DateTime.Now.ToString("HH:mm:ss"),
                    $"{LeftConfigPanel.GlobalUart.StringToHex(msg)}  ({msg})", $"{e.Length}", "RX"));
                // LIBReceive.ItemsSource = windowItems;
                LIBReceive.ScrollIntoView(windowItems.Last());
                return;
            }

            windowItems.Add(new WindowItem(DateTime.Now.ToString("HH:mm:ss"),
                LeftConfigPanel.GlobalUart.BytesToString(e.Text, CBRXEncode.SelectedItem as string), $"{e.Length}",
                "RX"));
            // LIBReceive.ItemsSource = windowItems;
            LIBReceive.ScrollIntoView(windowItems.Last());
        });
    }


    private void BtnSend_OnClick(object? sender, RoutedEventArgs e)
    {
        string data = TBSendData.Text;
        if (data.Length == 0) return;
        LeftConfigPanel.GlobalUart.OnSerialDataSend(data);
        LeftConfigPanel.GlobalUart.SendLine(data, CBRXEncode.SelectedItem as string);
        txcount += data.Length;
        LabTX.Text = $"{txcount}";

        if (LeftConfigPanel.GlobalUart.IsHexTx)
        {
            windowItems.Add(new WindowItem(DateTime.Now.ToString("HH:mm:ss"),
                $"{LeftConfigPanel.GlobalUart.StringToHex(data)}  ({data})", $"{data.Length}", "TX"));
            // LIBReceive.ItemsSource = windowItems;
            LIBReceive.ScrollIntoView(windowItems.Last());
            return;
        }

        windowItems.Add(new WindowItem(DateTime.Now.ToString("HH:mm:ss"), data, $"{data.Length}", "TX"));
        // LIBReceive.ItemsSource = windowItems;
        LIBReceive.ScrollIntoView(windowItems.Last());
        // LIBReceive.Foreground=Brushes.Blue;
    }

    private void BTNCountRST_OnClick(object? sender, RoutedEventArgs e)
    {
        rxcount = 0;
        txcount = 0;
        LabRX.Text = $"{rxcount}";
        LabTX.Text = $"{txcount}";
    }

    // 被外部调用清空数据
    public void ClearData()
    {
        Debug.WriteLine("ClearData");
        windowItems.Clear();
        rxstr = "";
        txstr = "";
        rxcount = 0;
        txcount = 0;
        LabRX.Text = $"{rxcount}";
        LabTX.Text = $"{txcount}";
    }

    //被外部调取获得发送区内容
    public string GetSendData()
    {
        return TBSendData.Text;
    }


    private WindowItem? GetRightClickItem(object sender)
    {
        if (sender is not MenuItem mi) 
        {
            Debug.WriteLine("sender 不是 MenuItem");
            return null;
        }
        if (mi.Parent is not ContextMenu cm) 
        {
            Debug.WriteLine("MenuItem 的 Parent 不是 ContextMenu");
            return null;
        }
        // 使用 ContextMenu 的 DataContext 而不是 PlacementTarget
        return cm.DataContext as WindowItem;
    }

    //外部调用获得编码类型
    public string GetEncode()
    {
        return CBRXEncode.SelectedItem as string;
    }

    //被掉哟添加到发送区列表/
    public void AddToWindowItems(string msg, string dir)
    {
        windowItems.Add(new WindowItem(DateTime.Now.ToString("HH:mm:ss"), msg, $"{msg.Length}", dir));
        // LIBReceive.ItemsSource = windowItems;
        LIBReceive.ScrollIntoView(windowItems.Last());
    }


    private void CTMClear_OnClick(object? sender, RoutedEventArgs e)
    {
        ClearData();
    }

    private async void CTMCopy_OnClick(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"CTMCopy {sender}");
        var item = GetRightClickItem(sender);
        if (item == null) return;
        
        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard != null)
        {
            await top.Clipboard.SetTextAsync(item.Message);
        }
        

    }

    private void CTMDel_OnClick(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"CTMDel {sender}");
        var item = GetRightClickItem(sender);
        if (item == null) return;

        int index = windowItems.IndexOf(item);
        if (index >= 0)
        {
            windowItems.RemoveAt(index);
        }
    }
}

public class WindowItem
{
    public string Time { get; set; }
    public string Message { get; set; }
    public string Size { get; set; }
    public string Direction { get; set; }
    
    public WindowItem()
    {
        
    }

    public WindowItem(string time, string message, string size, string direction)
    {
        Time = time;
        Message = message;
        Size = size;
        Direction = direction;
    }
}