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
using static Pulling_Line_Service.InterfaceDB;

namespace Pulling_Line_Service
{
    class Program
    {
        //  Logging
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //  App setting
        private static string _plcIp1 = Convert.ToString(ConfigurationManager.AppSettings["plc1Ip"]);
        private static string _plcIp2 = Convert.ToString(ConfigurationManager.AppSettings["plc2Ip"]);
        private static int _plcTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["plcTimeOut"]);
        private static PoohFinsETN _Plc1 = new PoohFinsETN();
        private static PoohFinsETN _Plc2 = new PoohFinsETN();

        //  Local
        private static int _plcDelayMs = 20;

        //  Main function
        static void Main(string[] args)
        {
            //  Log
            log.Info("Program start");
            Console.WriteLine("Program start");

            //  ตั้งค่า Interface ระหว่าง Service และ PLC ( **Recheck Net and Node number )
            PlcSetup(_Plc1, _plcIp1, 1, 1, (byte)Int32.Parse(GetLocalIPAddress().Split('.')[3]), _plcTimeout);
            PlcSetup(_Plc2, _plcIp2, 1, 1, (byte)Int32.Parse(GetLocalIPAddress().Split('.')[3]), _plcTimeout);

            //  Main loop
            string[] groupCode = { "G0", "G1", "G2", "G3", "G4", "G5", "G6" };
            while (true)
            {
                ////  PLC 1 Loop
                //PlcMainLoop(_Plc1);
                //System.Threading.Thread.Sleep(_plcDelayMs);

                ////  PLC 2 Loop
                //PlcMainLoop(_Plc2);
                //System.Threading.Thread.Sleep(_plcDelayMs);

                //  Check machine down event in each group
                for (int i = 0; i < groupCode.Length; i++)
                    LineApiMachineDownEachGroup(groupCode[i]);

                //  Check machine down end job
                LineApiMachineDownEndJobEachGroup();

                //  Delay loop
                System.Threading.Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["delayLoop_ms"]));
            }
        }

        //  Line API Function
        private static void LineApiMachineDownAllGroup()
        {
            try
            {
                DataTable dataTable = pProcessLineApiMachineDownAllGroupTest();
                if (dataTable != null)
                {
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            try
                            {
                                LineNotifyMsg(dataTable.Rows[i]["line_token"].ToString(), dataTable.Rows[i]["message"].ToString());
                            }
                            catch (Exception ex)
                            {
                                log.Error("LineApiMachineDownAllGroup error in loop : " + ex.Message);
                            }
                            finally
                            {
                                System.Threading.Thread.Sleep(100);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("LineApiMachineDownAllGroup error : " + ex.Message);
            }
        }
        private static void LineApiMachineDownEachGroup(string groupCode)
        {
            try
            {
                DataTable dataTable = pProcessSearchMachineDownTimeOverMinute(groupCode);
                if (dataTable != null)
                {
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            try
                            {
                                //  Sending Line
                                bool res = LineNotifyMsg(dataTable.Rows[i]["line_token"].ToString(), dataTable.Rows[i]["message"].ToString().Replace("$$", "\r\n"));
                                //  Update active group
                                if (res)
                                    pProcessUpdateMachineDownActiveLineGroup(Convert.ToInt32(dataTable.Rows[i]["Table_StatusMachine_id"]));
                                //  Print
                                Console.WriteLine("Sending Line with group " + groupCode + " Table_StatusMachine_id (" + dataTable.Rows[i]["Table_StatusMachine_id"].ToString() + ")");
                            }
                            catch (Exception ex)
                            {
                                log.Error("LineApiMachineDownAllGroup error in loop : " + ex.Message);
                            }
                            finally
                            {
                                System.Threading.Thread.Sleep(1000);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("LineApiMachineDownEachGroup error : " + ex.Message);
            }
        }
        private static void LineApiMachineDownEndJobEachGroup()
        {
            try
            {
                DataTable dataTable = pProcessSearchMachineDownTimeEndJob();
                if (dataTable != null)
                {
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            try
                            {
                                //  Extract data from sql
                                int Table_StatusMachine_id = Convert.ToInt32(dataTable.Rows[i]["Table_StatusMachine_id"]);
                                string message = dataTable.Rows[i]["message"].ToString();
                                string[] linetokenGroup = dataTable.Rows[i]["line_token"].ToString().Split('|');
                                string[] groupCodeList = dataTable.Rows[i]["group_code_list"].ToString().Split('|');
                                for (int j = 0; j < linetokenGroup.Length; j++)
                                {
                                    //  Sending Line
                                    bool res = LineNotifyMsg(linetokenGroup[j], message.Replace("$$", "\r\n"));
                                    //  Update active group
                                    if (res)
                                        pProcessUpdateMachineDownActiveLineGroupEndJob(Convert.ToInt32(dataTable.Rows[i]["Table_StatusMachine_id"]));
                                    //  Print
                                    Console.WriteLine("Sending Line with group (end job)" + groupCodeList[j] + " Table_StatusMachine_id (" + dataTable.Rows[i]["Table_StatusMachine_id"].ToString() + ")");
                                }
                            }
                            catch (Exception ex)
                            {
                                log.Error("LineApiMachineDownAllGroup error in loop : " + ex.Message);
                            }
                            finally
                            {
                                System.Threading.Thread.Sleep(1000);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("LineApiMachineDownEachGroup error : " + ex.Message);
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
        private static bool LineNotifyMsg(string lineToken, string message)
        {
            try
            {
                //message = System.Web.HttpUtility.UrlEncode(message, Encoding.UTF8);
                var request = (HttpWebRequest)WebRequest.Create("https://notify-api.line.me/api/notify");
                var postData = string.Format("message={0}", message);
                var data = Encoding.UTF8.GetBytes(postData);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.Headers.Add("Authorization", "Bearer " + lineToken);
                var stream = request.GetRequestStream();
                stream.Write(data, 0, data.Length);
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                log.Error("LineNotifyMsg error : " + ex.Message);
                return false;
            }
        }
        private static void LineNotifyAdv(string lineToken, string message, int stickerPackageID, int stickerID, string pictureUrl)
        {
            try
            {
                //  https://developers.line.biz/en/docs/messaging-api/sticker-list/#sticker-definitions
                var request = (HttpWebRequest)WebRequest.Create("https://notify-api.line.me/api/notify");
                var postData = string.Format("message={0}", message);
                if (stickerPackageID > 0 && stickerID > 0)
                {
                    var stickerPackageId = string.Format("stickerPackageId={0}", stickerPackageID);
                    var stickerId = string.Format("stickerId={0}", stickerID);
                    postData += "&" + stickerPackageId.ToString() + "&" + stickerId.ToString();
                }
                if (pictureUrl != "")
                {
                    var imageThumbnail = string.Format("imageThumbnail={0}", pictureUrl);
                    var imageFullsize = string.Format("imageFullsize={0}", pictureUrl);
                    postData += "&" + imageThumbnail.ToString() + "&" + imageFullsize.ToString();
                }
                var data = Encoding.UTF8.GetBytes(postData);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.Headers.Add("Authorization", "Bearer " + lineToken);
                var stream = request.GetRequestStream();
                stream.Write(data, 0, data.Length);
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        //  SQL Function
        private static DataTable pProcessLineApiMachineDownAllGroupTest()
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessLineApiMachineDownAllGroupTest
                SqlParameterCollection param = new SqlCommand().Parameters;
                ds = new DBClass().SqlExcSto("pProcessLineApiMachineDownAllGroupTest", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessLineApiMachineDownAllGroupTest SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessLineApiMachineDownAllGroupTest Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessSearchMachineDownTimeOverMinute(string Table_MasterLineMachineDown_code)
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessSearchMachineDownTimeOverMinute
                //  pProcessSearchMachineDownTimeOverMinute (@Table_MasterLineMachineDown_code varchar(10))
                SqlParameterCollection param = new SqlCommand().Parameters;
                param.AddWithValue("@Table_MasterLineMachineDown_code", SqlDbType.VarChar).Value = Table_MasterLineMachineDown_code;
                ds = new DBClass().SqlExcSto("pProcessSearchMachineDownTimeOverMinute", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessSearchMachineDownTimeOverMinute SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessSearchMachineDownTimeOverMinute Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessSearchMachineDownTimeEndJob()
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessSearchMachineDownTimeEndJob
                //  pProcessSearchMachineDownTimeEndJob
                SqlParameterCollection param = new SqlCommand().Parameters;
                ds = new DBClass().SqlExcSto("pProcessSearchMachineDownTimeEndJob", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessSearchMachineDownTimeEndJob SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessSearchMachineDownTimeEndJob Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessUpdateMachineDownActiveLineGroup(int Table_StatusMachine_id)
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessUpdateMachineDownActiveLineGroup
                //  pProcessUpdateMachineDownActiveLineGroup(@Table_StatusMachine_id int)
                SqlParameterCollection param = new SqlCommand().Parameters;
                param.AddWithValue("@Table_StatusMachine_id", SqlDbType.Int).Value = Table_StatusMachine_id;
                ds = new DBClass().SqlExcSto("pProcessUpdateMachineDownActiveLineGroup", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessUpdateMachinDownActiveLineGroup SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessUpdateMachinDownActiveLineGroup Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessUpdateMachineDownActiveLineGroupEndJob(int Table_StatusMachine_id)
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessUpdateMachineDownActiveLineGroupEndJob
                //  pProcessUpdateMachineDownActiveLineGroupEndJob(@Table_StatusMachine_id int)
                SqlParameterCollection param = new SqlCommand().Parameters;
                param.AddWithValue("@Table_StatusMachine_id", SqlDbType.Int).Value = Table_StatusMachine_id;
                ds = new DBClass().SqlExcSto("pProcessUpdateMachineDownActiveLineGroupEndJob", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessUpdateMachineDownActiveLineGroupEndJob SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessUpdateMachineDownActiveLineGroupEndJob Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pInsert_Table_StatusMachine(string MachineNo, DateTime EventDateTime, string Status, string DownTimeCode, string ProblemCode)
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                if ((EventDateTime == DateTime.MinValue) || (EventDateTime == DateTime.MinValue))
                    return null;

                //  อ่านค่าจาก Store pInsert_Table_StatusMachine
                //  pInsert_Table_StatusMachine (@MachineNo varchar(50),@EventDateTime datetime,@Status varchar(20)
                //  ,@DownTimeCode varchar(20)
                //  ,@ProblemCode varchar(20))
                SqlParameterCollection param = new SqlCommand().Parameters;
                param.AddWithValue("@MachineNo", SqlDbType.VarChar).Value = MachineNo;
                param.AddWithValue("@EventDateTime", SqlDbType.DateTime).Value = EventDateTime;
                param.AddWithValue("@Status", SqlDbType.VarChar).Value = Status;
                param.AddWithValue("@DownTimeCode", SqlDbType.VarChar).Value = DownTimeCode;
                param.AddWithValue("@ProblemCode", SqlDbType.VarChar).Value = ProblemCode;
                ds = new DBClass().SqlExcSto("pInsert_Table_StatusMachine", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pInsert_Table_StatusMachine SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pInsert_Table_StatusMachine Exception : " + ex.Message);
            }
            return dataTable;
        }

        //  PLC function
        private static void PlcMainLoop(PoohFinsETN plc)
        {
            PlcMachineDownTimeLoop(plc);
        }
        private static void PlcMachineDownTimeLoop(PoohFinsETN plc)
        {
            try
            {
                //  Machine status
                if (PlcReadEM2(plc, 30000) == 1)  //  3. NJ have data
                {
                    //  Read data (E2_0 - E2_99)
                    string E2_DATA = string.Empty;
                    System.Threading.Thread.Sleep(_plcDelayMs);
                    E2_DATA = PlcReadMemoryString(plc, 0, 99);

                    //  Check E2_DATA
                    if (string.IsNullOrEmpty(E2_DATA)) return;

                    //  Load data to var
                    string[] data = new string[5];
                    data[0] = E2_DATA.Substring(0, 9).Replace("\n", "");                         //  MachineNo
                    data[1] = E2_DATA.Substring(10, 19).Replace("\n", "");                      //  EventDateTime
                    data[2] = E2_DATA.Substring(20, 29).Replace("\n", "");                      //  Status
                    data[3] = E2_DATA.Substring(30, 39).Replace("\n", "");                      //  DownTimeCode
                    data[4] = E2_DATA.Substring(40, E2_DATA.Length - 1).Replace("\n", "");      //  ProblemCode

                    //  4.PC got data >> finish (E2_300010= 1)
                    pInsert_Table_StatusMachine(data[0], Convert.ToDateTime(data[1]), data[2], data[3], data[4]);
                    PlcWriteEM2(plc, 30010, 1);
                    System.Threading.Thread.Sleep(_plcDelayMs);

                    //  5. NJ Ack.
                    while (true)
                    {
                        if (PlcReadEM2(plc, 30000) == 2)
                        {
                            //  6. PC Ack.
                            System.Threading.Thread.Sleep(_plcDelayMs);
                            PlcWriteEM2(plc, 30010, 1);
                            break;
                        }
                        else
                            System.Threading.Thread.Sleep(_plcDelayMs);
                    }

                    //  7. NJ clear data and flag
                    while (true)
                    {
                        if (PlcReadEM2(plc, 30000) == 0)
                        {
                            // 8.PC clear data and flag
                            System.Threading.Thread.Sleep(_plcDelayMs);
                            PlcWriteEM2(plc, 30010, 0);
                            break;
                        }
                        else
                            System.Threading.Thread.Sleep(_plcDelayMs);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("PlcMachineDownTimeLoop : " + ex.Message);
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
                Console.WriteLine(e.Message);
            }
        }
        private static void PlcWriteEM2(PoohFinsETN plc, Int16 addr, int value)
        {
            try
            {
                PoohFinsETN.MemoryTypes mt = PoohFinsETN.MemoryTypes.EM2;
                PoohFinsETN.DataTypes dt = PoohFinsETN.DataTypes.UnSignBIN;
                plc.WriteMemoryWord(mt, addr, value, dt);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private static int PlcReadEM2(PoohFinsETN plc, Int16 addr)
        {
            try
            {
                PoohFinsETN.MemoryTypes mt = PoohFinsETN.MemoryTypes.EM2;
                PoohFinsETN.DataTypes dt = PoohFinsETN.DataTypes.UnSignBIN;
                int res = plc.ReadMemoryWord(mt, addr, 1, dt)[0];
                return res;
            }
            catch (Exception e)
            {
                return -1;
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
                return null;
            }
        }

    }
}
