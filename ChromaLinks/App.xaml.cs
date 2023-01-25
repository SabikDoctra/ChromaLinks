using ChromaControl.Shared;

using Microsoft.Win32;

using OpenRGB.NET;
using Razer.Chroma.Broadcast;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ChromaLinks
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static RzChromaBroadcastAPI _api;
        private static OpenRGBClient _sdk;
        private bool _isLinkOpenRGB = true;
        private OpenRGB.NET.Models.Device[] _devices;
        private ContextMenuStrip _menu;
        private string _openRGBMenuText => _isLinkOpenRGB ? "Disable OpenRGB" : "Enable OpenRGB";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var icon = GetResourceStream(new Uri("icon.ico", UriKind.Relative)).Stream;
            _menu = new ContextMenuStrip();
            _menu.Items.Add(_openRGBMenuText, null, OpenRGBLink_Click);
            _menu.Items.Add("Exit", null, Exit_Click);
            var notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Visible = true,
                Icon = new System.Drawing.Icon(icon),
                Text = "ChromaLinks",
                ContextMenuStrip = _menu
            };
            notifyIcon.MouseClick += NotifyIcon_Click;
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(systemEvents_SessionSwitch);
            var _syncTask = Task.Factory.StartNew(() => startChromaSync());
        }

        private void startChromaSync()
        {
            if (!Utilities.InitializeEnvironment("THUNDERX3", "776775F5-0E6E-4E19-9D67-F4E25E2DCF0E"))
                return;

            Thread.Sleep(5000);
            // Setup OpenRGB Device
            onLed();

            _api = new RzChromaBroadcastAPI();
            _api.ConnectionChanged += _api_ConnectionChanged;
            _api.ColorChanged += _api_ColorChanged;

            try
            {
                _api.Init(Guid.Parse(Utilities.ApplicationGuid));
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                throw;
            }
        }

        private void _api_ColorChanged(object sender, RzChromaBroadcastColorChangedEventArgs e)
        {
            if (_isLinkOpenRGB)
            {
                var _i = 0;
                var _colorFromSynapse = 0;
                foreach (var _device in _devices)
                {
                    foreach (var _color in _device.Colors)
                    {
                        _color.R = e.Colors[_colorFromSynapse].R;
                        _color.G = e.Colors[_colorFromSynapse].G;
                        _color.B = e.Colors[_colorFromSynapse].B;
                        _colorFromSynapse++;
                        if (_colorFromSynapse > 4)
                            _colorFromSynapse = 0;
                    }
                    _sdk.UpdateLeds(_i, _device.Colors);
                    _i++;
                }
            }
        }

        private void _api_ConnectionChanged(object sender, RzChromaBroadcastConnectionChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void NotifyIcon_Click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {

            }
        }

        private void OpenRGBLink_Click(object sender, EventArgs e)
        {
            if (_isLinkOpenRGB)
                _isLinkOpenRGB = false;
            else
                _isLinkOpenRGB = true;

            _menu.Items[0].Text = _openRGBMenuText;
        }

        private void systemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            SessionSwitchReason reason = e.Reason;
            if (reason != SessionSwitchReason.SessionLock)
                onLed();
            else
                offLed();
        }

        private void onLed()
        {
            var _sdkInitialized = false;
            while (!_sdkInitialized)
            {
                try
                {
                    _sdk = new OpenRGBClient(name: "ChromaLinks OpenRGB Client", autoconnect: true, timeout: 1000);
                    var deviceCount = _sdk.GetControllerCount();
                    if(deviceCount != 0)
                    {
                        _sdkInitialized = true;
                        var devices = _sdk.GetAllControllerData();
                        
                        var _i = 0;
                        _devices = new OpenRGB.NET.Models.Device[deviceCount];
                        foreach (var device in devices)
                        {
                            _devices[_i] = device;
                            _i++;
                        }
                    }
                }
                catch (TimeoutException)
                {
                    Thread.Sleep(5000);
                }
            }
        }

        private void offLed()
        {
            _isLinkOpenRGB = false;
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Shutdown();
        }
    }
}
