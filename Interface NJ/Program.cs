using PoohPlcLink;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static Interface_NJ.InterfaceDB;

namespace Interface_NJ
{
    class Program
    {
        //  Logging
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //  App setting
        private static string _plcIp1 = Convert.ToString(ConfigurationManager.AppSettings["plc1Ip"]);
        private static int _plcTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["plcTimeOut"]);
        private static PoohFinsETN _Plc1 = new PoohFinsETN();

        //  Local
        private static int _plcDelayMs = 20;

        //  Main function
        static void Main(string[] args)
        {
            //  Log
            log.Info("Program start");
            Console.WriteLine(GetDatetimeNowConsole() + "Program start");

            //  ตั้งค่า Interface ระหว่าง Service และ PLC ( **Recheck Net and Node number )
            PlcSetup(_Plc1, _plcIp1, 0, 0, 10, _plcTimeout);

            //  Main loop  
            while (true)
            {
                //  PLC 1 Read Loop
                PlcMainLoop(_Plc1);

                //  PLC 1 Write Loop
                int timeAccum = 0;
                int mainLoopDelay = Convert.ToInt32(ConfigurationManager.AppSettings["delayLoop_ms"]);
                int writeLoopDelay = Convert.ToInt32(ConfigurationManager.AppSettings["delayWriteLoop_ms"]);
                while (timeAccum <= mainLoopDelay)
                {
                    //  PLC 1 Write Setting
                    PlcMainWriteSettingLoop(_Plc1);
                    timeAccum += writeLoopDelay;
                    System.Threading.Thread.Sleep(writeLoopDelay);
                }

                //  Delay loop
                //System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["delayLoop_ms"]));
            }
        }

        //  Local Function
        private static bool ValidateIPv4(string ipString)
        {
            if (String.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }
        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        private static string GetDatetimeNowConsole()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " : ";
        }

        //  SQL Function
        private static DataTable pInsert_tr_temp(int temp_no, double actual_temp)
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {

                //  อ่านค่าจาก Store pInsert_tr_temp
                //  pInsert_tr_temp (@t1 int,@t2 int,@t3 int)
                SqlParameterCollection param = new SqlCommand().Parameters;
                param.AddWithValue("@temp_no", SqlDbType.Int).Value = temp_no;
                param.AddWithValue("@temp_actual", SqlDbType.Decimal).Value = actual_temp;
                ds = new DBClass().SqlExcSto("pInsert_tr_temp", "DbSet", param);
                dataTable = new DataTable();
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pInsert_tr_temp SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pInsert_tr_temp Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pGet_plc_setting_addr()
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {

                //  อ่านค่าจาก Store pGet_plc_setting_addr 
                SqlParameterCollection param = new SqlCommand().Parameters;
                ds = new DBClass().SqlExcSto("pGet_plc_setting_addr", "DbSet", param);
                dataTable = new DataTable();
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pGet_plc_setting_addr SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pGet_plc_setting_addr Exception : " + ex.Message);
            }
            return dataTable;
        }

        //  PLC function
        private static void PlcMainLoop(PoohFinsETN plc)
        {
            float[] t1 = PlcReadDM(plc, 500, 20);
            System.Threading.Thread.Sleep(_plcDelayMs);
            Console.WriteLine("----" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "----");
            Console.WriteLine("T#1 : " + t1[0] + "'C    H#1 : " + t1[1] + " %");
            Console.WriteLine("T#2 : " + t1[2] + "'C    H#2 : " + t1[3] + " %");
            Console.WriteLine("T#3 : " + t1[4] + "'C    H#3 : " + t1[5] + " %");
            Console.WriteLine("T#4 : " + t1[6] + "'C    H#4 : " + t1[7] + " %");
            Console.WriteLine("T#5 : " + t1[8] + "'C    H#5 : " + t1[9] + " %");

            //float h1 = PlcReadDM(plc, 518);
            //System.Threading.Thread.Sleep(_plcDelayMs);
            //Console.WriteLine("H#5 : " + h1);

            //long h1 = PlcReadMemoryDWord(plc, 518, 2)[0];
            //System.Threading.Thread.Sleep(_plcDelayMs);
            //Console.WriteLine("h1 : " + h1);
            //Console.WriteLine("------------------------------");

            //System.Threading.Thread.Sleep(_plcDelayMs);

            pInsert_tr_temp(7, t1[0]);
            pInsert_tr_temp(8, t1[1]);
            pInsert_tr_temp(1, t1[2]);
            pInsert_tr_temp(2, t1[3]);
            pInsert_tr_temp(3, t1[4]);
            pInsert_tr_temp(4, t1[5]);
            pInsert_tr_temp(5, t1[6]);
            pInsert_tr_temp(6, t1[7]);
            pInsert_tr_temp(9, t1[8]);
            pInsert_tr_temp(10, t1[9]);
        }
        private static void PlcMainWriteSettingLoop(PoohFinsETN plc)
        {
            try
            {
                //Console.WriteLine("Write Data to PLC 1");
                DataTable dt = pGet_plc_setting_addr();
                if (dt != null)
                {
                    //Console.WriteLine("Write Data to PLC 2");
                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            //  Reload data
                            string temp_name = dt.Rows[i]["temp_name"].ToString();
                            Int16 addr_hi = Int16.Parse(dt.Rows[i]["plc_addr_sp_hi"].ToString());
                            Int16 addr_lo = Int16.Parse(dt.Rows[i]["plc_addr_sp_lo"].ToString());
                            float[] limit_hi = { Convert.ToSingle(dt.Rows[i]["limit_hi"].ToString()) };
                            float[] limit_low = { Convert.ToSingle(dt.Rows[i]["limit_low"].ToString()) };

                            //  Write HI
                            PlcWriteMemoryFloat32(plc, addr_hi, limit_hi);
                            System.Threading.Thread.Sleep(_plcDelayMs);
                            // Console.WriteLine(temp_name + " HI Upload : " + limit_hi[0] + " at D" + addr_hi.ToString());

                            //  Write LO
                            PlcWriteMemoryFloat32(plc, addr_lo, limit_low);
                            System.Threading.Thread.Sleep(_plcDelayMs);
                            // Console.WriteLine(temp_name + " LO Upload : " + limit_low[0] + " at D" + addr_lo.ToString());

                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(GetDatetimeNowConsole() + e.Message);
            }
        }
        private static void PlcSetup(PoohFinsETN plc, string plcIpAddr, byte plcNetNo, byte pcNetNo, byte pcNodeNo, int timeout)
        {
            try
            {
                //  ตรวจสอบหมายเลข IP ของ PLC
                if (ValidateIPv4(plcIpAddr))
                {
                    plc.PLC_IPAddress = plcIpAddr;
                    plc.PLC_NetNo = plcNetNo;
                    plc.PLC_NodeNo = (byte)Int16.Parse(plcIpAddr.Split('.')[3]);
                    plc.PLC_UDPPort = 9600;
                    plc.PC_NetNo = pcNetNo;
                    plc.PC_NodeNo = pcNodeNo;
                    plc.TimeOutMSec = timeout;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(GetDatetimeNowConsole() + e.Message);
            }
        }
        private static float[] PlcReadDM(PoohFinsETN plc, Int16 addr, Int16 size)
        {
            try
            {
                PoohFinsETN.MemoryTypes mt = PoohFinsETN.MemoryTypes.DM;
                PoohFinsETN.DataTypes dt = PoohFinsETN.DataTypes.UnSignBIN;
                //int res = plc.ReadMemoryWord(mt, addr, 1, dt)[0];
                float[] res = plc.ReadMemoryFloat32(mt, addr, size);
                return res;
            }
            catch (Exception e)
            {
                Console.WriteLine(GetDatetimeNowConsole() + e.Message);
                return null;
            }
        }
        private static string PlcReadMemoryString(PoohFinsETN plc, Int16 addr, Int16 size)
        {
            try
            {
                PoohFinsETN.MemoryTypes mt = PoohFinsETN.MemoryTypes.EM2;
                PoohFinsETN.DataTypes dt = PoohFinsETN.DataTypes.UnSignBIN;
                string res = plc.ReadMemoryString(mt, addr, size);
                return res;
            }
            catch (Exception e)
            {
                Console.WriteLine(GetDatetimeNowConsole() + e.Message);
                return null;
            }
        }
        private static long[] PlcReadMemoryDWord(PoohFinsETN plc, Int16 addr, Int16 size)
        {
            try
            {
                PoohFinsETN.MemoryTypes mt = PoohFinsETN.MemoryTypes.EM2;
                PoohFinsETN.DataTypes dt = PoohFinsETN.DataTypes.UnSignBIN;
                long[] res = plc.ReadMemoryDWord(mt, addr, size, dt);
                return res;
            }
            catch (Exception e)
            {
                Console.WriteLine(GetDatetimeNowConsole() + e.Message);
                return null;
            }
        }
        private static void PlcWriteMemoryFloat32(PoohFinsETN plc, Int16 addr, float[] writeDataFloat)
        {
            try
            {
                PoohFinsETN.MemoryTypes mt = PoohFinsETN.MemoryTypes.DM;
                plc.WriteMemoryFloat32(mt, addr, writeDataFloat);
            }
            catch (Exception e)
            {
                Console.WriteLine(GetDatetimeNowConsole() + e.Message);
            }
        }
    }
}
