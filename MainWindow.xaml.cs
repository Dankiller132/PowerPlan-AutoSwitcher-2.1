using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Drawing;

namespace PowerPlan_AutoSwitcher_2._1
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool StrartINSysTray = false;
        PowerPlanManager PPM = new PowerPlanManager();
        PowerPlan Temporal;
        string PowerplanListPath = AppDomain.CurrentDomain.BaseDirectory + @"\SavedPowerplans.spp";
        string ConfigurationPath = AppDomain.CurrentDomain.BaseDirectory + @"\config.cfg";
        ObservableCollection<string> PlansStrings;
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        int seconds_for_check,PercentToChange;
        Config Configuration;
        System.Windows.Forms.NotifyIcon _notifyIcon;
        bool leave = false;
        public MainWindow()
        {
            Configuration = PPM.Deserialize<Config>(ConfigurationPath);
            if (Configuration==null) { Configuration = new Config(); }
            InitializeComponent();
            Temporal = new PowerPlan();
            PPM.Plans = PPM.Deserialize<PowerPlanList>(PowerplanListPath);
            seconds_for_check=Configuration.secs==0?5:Configuration.secs;
            PlansStrings = PlansToStrings();
            a1.ToolTip = "Default (35)";
            secs.ToolTip = "Default is 1";
            var icono = Properties.Resources.BatConIcon;
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            this.Title = "Configure AutoSwitch";
            _notifyIcon.Icon = icono;
            Icon notifyIcon = _notifyIcon.Icon;

            // Convertir el icono de System.Drawing.Icon a System.Windows.Media.Imaging.BitmapSource
            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                notifyIcon.Handle,
                new Int32Rect(0, 0, notifyIcon.Width, notifyIcon.Height),
                BitmapSizeOptions.FromWidthAndHeight(notifyIcon.Width, notifyIcon.Height)
            );

            this.Icon = bitmapSource;
            // Establecer el icono de la ventana principal
            _notifyIcon.Visible = true;

            if (Configuration==null) { Configuration = new Config(); Configuration.secs = 5; Configuration.eneabled = true; settimer(); }
            else { PlanCB1.SelectedIndex = Configuration.Base; secs.Text = Configuration.secs!=5?Configuration.secs.ToString():"Default (5)"; PlanCB2.SelectedIndex = Configuration.SwitchTO; }
            en.IsChecked = Configuration.eneabled;            
            if (PPM.Plans == null) { PPM.Plans = new PowerPlanList();}
                PlanCB1.ItemsSource = PlansStrings;
                PlanCB2.ItemsSource = PlansStrings;
            datagrid.ItemsSource = PPM.Plans.items;

            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (s, args) => ShowWindow();
            timer.Tick += TimerTck;
            GUDTXT.IsEnabled = false;
            StrartINSysTray = Configuration.SysT;
            SelectModeCB.SelectedIndex = Configuration.mode;PercentToChange = Configuration.percent;
            SYST.IsChecked= StrartINSysTray;
            if (StrartINSysTray) { WindowState = WindowState.Minimized; this.Hide(); }
        }
        void ShowWindow() { this.Show(); WindowState = WindowState.Normal; }
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }

            base.OnStateChanged(e);
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Ocultar la ventana en lugar de cerrarla
            e.Cancel = !leave;
            this.Hide();

            // Mostrar el icono en la bandeja del sistema

            if (e.Cancel)
            {
                _notifyIcon.BalloonTipTitle = "AutoSwticher";
                _notifyIcon.BalloonTipText = "Application is still running, use the button Leave for closing it.";
                _notifyIcon.ShowBalloonTip(500); _notifyIcon.BalloonTipClicked += (s, args) => ShowWindow();
            }
            else { _notifyIcon.Dispose(); }
            base.OnClosing(e);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Temporal.guid = PPM.GetPowerPlanGuid();
            GUDTXT.Text=Temporal.guid.ToString();
        }

        private void GUDTXT_TextChanged(object sender, TextChangedEventArgs e)
        {
            return;
        }
        ObservableCollection<string> PlansToStrings()
        {
            ObservableCollection<string> res = new ObservableCollection<string>();
            if (PPM.Plans == null) return res;
            else if (PPM.Plans.items.Count == 0) { return res; }
            if (PlansStrings == null) { PlansStrings = new ObservableCollection<string>(); }
            foreach (var item in PPM.Plans.items)
            {
                if (!PlansStrings.Contains(item.Name)) { PlansStrings.Add(item.Name); }
                res.Add(item.Name);
            }
            if (res.Count == PPM.Plans.items.Count&& PlansStrings.Count != PPM.Plans.items.Count)
            {
                PlansStrings.Clear();
                foreach (var item in res)
                {
                    PlansStrings.Add(item);
                }
            }
            return res;
        }
        void settimer()
        {
            try
            {
            timer.Enabled = Configuration.eneabled;
            }
            catch
            {
                timer.Enabled = true;
            }
            timer.Interval = seconds_for_check * 1000;
            PPM.Serialize(Configuration, ConfigurationPath);
        }

        private void saveplanbt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GUDTXT.Text)) { MessageBox.Show("No Guid saved"); return; }
            if (string.IsNullOrWhiteSpace(PlanNameTXT.Text)) { MessageBox.Show("No Name saved"); return; }
            Temporal.Name = PlanNameTXT.Text;
            bool repetido = false;
            int repeatedindex=0;
            int i = 0;
            foreach (var item in PPM.Plans.items)
            {
                if (item.guid == Temporal.guid) { repetido = true;repeatedindex = i; break; }
                i++;
            }
            if (repetido)
            {
                MessageBoxResult res = MessageBox.Show("This plan is already saved \n replace Name?", "Duplicate detected", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (res == MessageBoxResult.Yes) { PPM.Plans.items[repeatedindex] = Temporal; }
            }
            else { PPM.Plans.items.Add(new PowerPlan(Temporal.Name, Temporal.guid)); }
            PPM.Serialize(PPM.Plans, PowerplanListPath);
            PlansToStrings();
        }

        private void Delbut_Click(object sender, RoutedEventArgs e)
        {
            PPM.Plans.items.RemoveAt(datagrid.SelectedIndex);
            PlansToStrings();
        }

        private void timevaluechanged(object sender, RoutedEventArgs e)
        {
            int seconds_for_check_temp = 0;
            if(int.TryParse(secs.Text, out seconds_for_check_temp))
            {seconds_for_check = seconds_for_check_temp==0? 1 : seconds_for_check_temp;settimer(); }
            secs.Text =seconds_for_check.ToString();
        }

        private void en_Checked(object sender, RoutedEventArgs e)
        {Configuration.eneabled = (bool)en.IsChecked;settimer(); PPM.Serialize(Configuration,ConfigurationPath); }


        void TimerTck(object sender, EventArgs e)
        {
            if(Configuration.eneabled)
            try
                {
                    switch (SelectModeCB.SelectedIndex)
                    {
                        case 0:
                            if (PPM.BateryCharging())
                            {
                                if (PPM.GetPowerPlanGuid() != PPM.Plans.items[PlanCB2.SelectedIndex].guid)
                                {
                                    PPM.SetPowerPlan(PPM.Plans.items[PlanCB2.SelectedIndex].guid);
                                }
                            }
                            else
                            {
                                if (PPM.GetPowerPlanGuid() != PPM.Plans.items[PlanCB1.SelectedIndex].guid)
                                {
                                    PPM.SetPowerPlan(PPM.Plans.items[PlanCB1.SelectedIndex].guid);
                                }
                            }
                            break;
                        case 1:
                            int batteryLifePercent = 0;
                            System.Windows.Forms.PowerStatus powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;

                            batteryLifePercent = (int)(powerStatus.BatteryLifePercent * 100);
                            if (batteryLifePercent > PercentToChange)
                            {
                                if (PPM.GetPowerPlanGuid() != PPM.Plans.items[PlanCB2.SelectedIndex].guid)
                                {
                                    PPM.SetPowerPlan(PPM.Plans.items[PlanCB2.SelectedIndex].guid);
                                }
                            }
                            else
                            {
                                if (PPM.GetPowerPlanGuid() != PPM.Plans.items[PlanCB1.SelectedIndex].guid)
                                {
                                    PPM.SetPowerPlan(PPM.Plans.items[PlanCB1.SelectedIndex].guid);
                                }
                            }
                            break;
                        case 2:
                            batteryLifePercent = 0;
                            powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
                            batteryLifePercent = (int)(powerStatus.BatteryLifePercent * 100);
                            if (batteryLifePercent < PercentToChange)
                            {
                                if (PPM.GetPowerPlanGuid() != PPM.Plans.items[PlanCB2.SelectedIndex].guid)
                                {
                                    PPM.SetPowerPlan(PPM.Plans.items[PlanCB2.SelectedIndex].guid);
                                }
                            }
                            else
                            {
                                if (PPM.GetPowerPlanGuid() != PPM.Plans.items[PlanCB1.SelectedIndex].guid)
                                {
                                    PPM.SetPowerPlan(PPM.Plans.items[PlanCB1.SelectedIndex].guid);
                                }
                            }

                            break;
                        default:
                            break;
                    }
                }
                catch { }

        }

        private void PlanCB1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Configuration.Base = PlanCB1.SelectedIndex;
            PPM.Serialize(Configuration, ConfigurationPath);

        }

        private void PlanCB2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Configuration.SwitchTO = PlanCB2.SelectedIndex;
            PPM.Serialize(Configuration,ConfigurationPath);
        }

        private void LEV_Click(object sender, RoutedEventArgs e)
        {
            leave = true; PPM.Serialize(Configuration,ConfigurationPath); PPM.Serialize(PPM.Plans,PowerplanListPath); Application.Current.Shutdown();
        }

        private void SelectModeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (SelectModeCB.SelectedIndex != 0)
                { a1.Visibility = Visibility.Visible; a2.Visibility = Visibility.Visible; }
                else
                { a1.Visibility = Visibility.Hidden; a2.Visibility = Visibility.Hidden; }
                Configuration.mode = SelectModeCB.SelectedIndex;
                PPM.Serialize(Configuration,ConfigurationPath); 
            }
            catch { }
        }

        private void aiCh2(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(a1.Text)) { a1.Text = "a"; }
        }

        private void SSY(object sender, RoutedEventArgs e)
        {
            Configuration.SysT = (bool)SYST.IsChecked;
            PPM.Serialize(Configuration,ConfigurationPath);
        }

        private void aiCh(object sender, RoutedEventArgs e)
        {
            int percent = 35;
            int temp = 0;
            if(!string.IsNullOrWhiteSpace(a1.Text))
            {

            if (int.TryParse(a1.Text, out temp ))
            { PercentToChange =
                    temp < 4 ?
                    5 : temp>100?
                    100:temp;
            }
            else{
                PercentToChange = percent;
            }
            a1.Text = PercentToChange.ToString();
            Configuration.percent = PercentToChange;
            PPM.Serialize(Configuration, ConfigurationPath);
            }

        }

    }

    [Serializable]
    public class Config
    {
        public int mode,secs,percent,Base,SwitchTO;
        public bool eneabled,SysT;
        public Config()
        {
            mode = 0;secs = 0;percent = 0;Base = 0;SwitchTO = 0;
            eneabled = true;SysT = false;


        }
    }
}
