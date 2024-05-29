using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;


namespace PowerPlan_AutoSwitcher_2
{

    
  
    class PowerPlanManager
    {
        const bool f = false;
        const bool t = true;
        #region DLLs
        private static Guid GUID_SLEEP_SUBGROUP =
     new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20");
        private static Guid GUID_HIBERNATEIDLE =
           new Guid("9d7815a6-7ee4-497e-8888-515a05f02364");
        [DllImport("powrprof.dll")]
        static extern uint PowerSetActiveScheme(
      IntPtr UserRootPowerKey,
       [MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid);

        [DllImport("powrprof.dll")]
        static extern uint PowerGetActiveScheme(
           IntPtr UserRootPowerKey
           , ref IntPtr ActiveScheme);

        [DllImport("powrprof.dll")]
        static extern uint PowerReadACValue(
           IntPtr RootPowerKey,
           ref Guid SchemeGuid,
           ref Guid SubGroupOfPowerSettingGuid,
           ref Guid PowerSettingGuid,
           ref int Type,
           ref int Buffer,
           ref uint BufferSize);
        #endregion
       internal PowerPlanList Plans = new PowerPlanList();
        
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        internal bool BateryCharging()
        {
            
            SelectQuery Sq = new SelectQuery("Win32_Battery");
            ManagementObjectSearcher objOSDetails = new ManagementObjectSearcher(Sq);
            ManagementObjectCollection osDetailsCollection = objOSDetails.Get();
            StringBuilder sb = new StringBuilder();
            ushort res = 0;
            foreach (var item in osDetailsCollection)
            {
                res = (ushort)item["BatteryStatus"];
            }
            return res == 1 ? f : t;
        }
        internal void SetPowerPlan(Guid guid)
        {
            PowerSetActiveScheme(IntPtr.Zero, guid);
        }
        internal Guid GetPowerPlanGuid()
        {
            IntPtr ActiveScheme = IntPtr.Zero;
            PowerGetActiveScheme(IntPtr.Zero, ref ActiveScheme);
            Guid ActivePolicy = Marshal.PtrToStructure<Guid>
               (ActiveScheme);
            return ActivePolicy;
        }
       internal T Deserialize<T>(string path)
        {
            try
            {
                System.IO.FileStream stream = new System.IO.FileStream(path, System.IO.FileMode.OpenOrCreate);
                T result = (T)binaryFormatter.Deserialize(stream);
                return result;
            }
            catch {
            
            
            }
            return default;
        }
       internal bool Serialize(object @this, string there) {
            try {
                System.IO.FileStream stream = new System.IO.FileStream(there,System.IO.FileMode.OpenOrCreate);
                binaryFormatter.Serialize(stream, @this) ;
                return t; }
            catch { return f; }
        }
    }
    [Serializable]
    public class PowerPlanList
    {
        public ObservableCollection<PowerPlan> items;
        public PowerPlanList() { items = new ObservableCollection<PowerPlan>(); }
        public PowerPlanList(ObservableCollection<PowerPlan> plans)
        {
            if (plans.Count > 0)
                foreach (var item in plans)
                {
                    items.Add(item);
                }
            else { items = new ObservableCollection<PowerPlan>(); }
        }
        public PowerPlanList(PowerPlan[] plans)
        {
            if (plans.Length > 0)
                foreach (var item in plans)
                {
                    items.Add(item);
                }
            else { items = new ObservableCollection<PowerPlan>(); }
        }
    }
    [Serializable]
    public class PowerPlan{
       public PowerPlan() { }
        public PowerPlan(string name, Guid Guid) { Name = name; guid = Guid; }
        public string Name { get; set; }
        public Guid guid;
        public string GUID { get { return guid.ToString(); } }
    }


}
