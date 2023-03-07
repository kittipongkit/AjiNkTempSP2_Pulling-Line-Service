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

        //  Main function
        static void Main(string[] args)
        {
            //  Log
            log.Info("Program start");
            Console.WriteLine(GetDatetimeNowConsole() + "Program start");

            //  Broadcast
            //LineApiMachineDownAllGroup();

            //  Main loop
            string[] groupCode = { "G0", "G1", "G2", "G3", "G4", "G5", "G6" };
            while (true)
            {

                //  Check machine down event in each group
                for (int i = 0; i < groupCode.Length; i++)
                    LineApiMachineDownEachGroup(groupCode[i]);

                //  Check machine down end job
                LineApiMachineDownEndJobEachGroup();

                //  Check machine down (NonSelectCase)
                LineApiSearchMachineDownTimeOverMinute_NonSelectCase();

                //  Check count static
                for (int i = 0; i < groupCode.Length; i++)
                    LineApiCountStatic(groupCode[i]);

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
                                Console.WriteLine(GetDatetimeNowConsole() + "Sending Line with group " + groupCode + " Table_StatusMachine_id (" + dataTable.Rows[i]["Table_StatusMachine_id"].ToString() + ")");
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
                                    Console.WriteLine(GetDatetimeNowConsole() + "Sending Line with group (end job)" + groupCodeList[j] + " Table_StatusMachine_id (" + dataTable.Rows[i]["Table_StatusMachine_id"].ToString() + ")");
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
        private static void LineApiSearchMachineDownTimeOverMinute_NonSelectCase()
        {
            try
            {
                DataTable dataTable = pProcessSearchMachineDownTimeOverMinute_NonSelectCase();
                if (dataTable != null)
                {
                    if (dataTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            try
                            {
                                //  Extract data from sql
                                int Table_YieldMonitoring_id = Convert.ToInt32(dataTable.Rows[i]["Table_StatusMachine_id"]);
                                string message = dataTable.Rows[i]["message"].ToString();
                                string[] linetokenGroup = dataTable.Rows[i]["line_token"].ToString().Split('|');

                                for (int j = 0; j < linetokenGroup.Length; j++)
                                {
                                    //  Sending Line
                                    bool res = LineNotifyMsg(linetokenGroup[j], message.Replace("$$", "\r\n"));
                                }

                                //  Update active group
                                pProcessUpdateMachineDownActiveLineGroupEndJob_NonSelectCase(Convert.ToInt32(dataTable.Rows[i]["Table_StatusMachine_id"]));
                                //  Print
                                Console.WriteLine(GetDatetimeNowConsole() + "Sending Line with group (non-select case) Table_StatusMachine_id (" + dataTable.Rows[i]["Table_StatusMachine_id"].ToString() + ")");
                            }
                            catch (Exception ex)
                            {
                                log.Error("pProcessUpdateMachineDownActiveLineGroupEndJob_NonSelectCase error in loop : " + ex.Message);
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
                log.Error("pProcessSearchMachineDownTimeOverMinute_NonSelectCase error : " + ex.Message);
            }
        }
        private static void LineApiCountStatic(string groupCode)
        {
            try
            {
                DataTable dataTable = pProcessSearchCountStatistic(groupCode);
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
                                    pProcessUpdateCountStatistic(groupCode);
                                //  Print
                                Console.WriteLine(GetDatetimeNowConsole() + "Sending pProcessSearchCountStatistic " + groupCode + " Table_StatusMachine_id (" + dataTable.Rows[i]["Table_StatusMachine_id"].ToString() + ")");
                            }
                            catch (Exception ex)
                            {
                                log.Error("LineApiCountStatic error in loop : " + ex.Message);
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
                log.Error("LineApiCountStatic error : " + ex.Message);
            }
        }

        //  Local Function
        private static string GetDatetimeNowConsole()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " : ";
        }
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
                Console.WriteLine(GetDatetimeNowConsole() + ex.ToString());
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
                Console.WriteLine(GetDatetimeNowConsole() + ex.ToString());
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
        private static DataTable pProcessSearchYieldLessThanCriteria()
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessSearchYieldLessThanCriteria
                //  pProcessSearchMachineDownTimeEndJob
                SqlParameterCollection param = new SqlCommand().Parameters;
                ds = new DBClass().SqlExcSto("pProcessSearchYieldLessThanCriteria", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessSearchYieldLessThanCriteria SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessSearchYieldLessThanCriteria Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessUpdateYieldActiveLineGroup(int Table_YieldMonitoring_id)
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessUpdateYieldActiveLineGroup
                //  [dbo].[pProcessUpdateYieldActiveLineGroup](@Table_YieldMonitoring_id int)
                SqlParameterCollection param = new SqlCommand().Parameters;
                param.AddWithValue("@Table_YieldMonitoring_id", SqlDbType.Int).Value = Table_YieldMonitoring_id;
                ds = new DBClass().SqlExcSto("pProcessUpdateYieldActiveLineGroup", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessUpdateYieldActiveLineGroup SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessUpdateYieldActiveLineGroup Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessSearchMachineDownTimeOverMinute_NonSelectCase()
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessSearchMachineDownTimeOverMinute_NonSelectCase
                //  pProcessSearchMachineDownTimeOverMinute_NonSelectCase
                SqlParameterCollection param = new SqlCommand().Parameters;
                ds = new DBClass().SqlExcSto("pProcessSearchMachineDownTimeOverMinute_NonSelectCase", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessSearchMachineDownTimeOverMinute_NonSelectCase SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessSearchMachineDownTimeOverMinute_NonSelectCase Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessUpdateMachineDownActiveLineGroupEndJob_NonSelectCase(int Table_StatusMachine_id)
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessUpdateMachineDownActiveLineGroupEndJob_NonSelectCase
                //  pProcessUpdateMachineDownActiveLineGroupEndJob_NonSelectCase(@Table_StatusMachine_id int)
                SqlParameterCollection param = new SqlCommand().Parameters;
                param.AddWithValue("@Table_StatusMachine_id", SqlDbType.Int).Value = Table_StatusMachine_id;
                ds = new DBClass().SqlExcSto("pProcessUpdateMachineDownActiveLineGroupEndJob_NonSelectCase", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessUpdateMachineDownActiveLineGroupEndJob_NonSelectCase SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessUpdateMachineDownActiveLineGroupEndJob_NonSelectCase Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessSearchMachineDownTimeEndJob_B401()
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessSearchMachineDownTimeEndJob_B401
                //  pProcessSearchMachineDownTimeEndJob_B401
                SqlParameterCollection param = new SqlCommand().Parameters;
                ds = new DBClass().SqlExcSto("pProcessSearchMachineDownTimeEndJob_B401", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessSearchMachineDownTimeEndJob_B401 SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessSearchMachineDownTimeEndJob_B401 Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessSearchMachineDownTimeOverMinute_B201()
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessSearchMachineDownTimeOverMinute_B201
                //  pProcessSearchMachineDownTimeOverMinute_B201
                SqlParameterCollection param = new SqlCommand().Parameters;
                ds = new DBClass().SqlExcSto("pProcessSearchMachineDownTimeOverMinute_B201", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessSearchMachineDownTimeOverMinute_B201 SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessSearchMachineDownTimeOverMinute_B201 Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessUpdateMachineDownActiveLineGroup_B201(int Table_StatusMachine_id)
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessUpdateMachineDownActiveLineGroup_B201
                //  pProcessUpdateMachineDownActiveLineGroup_B201(@Table_StatusMachine_id int)
                SqlParameterCollection param = new SqlCommand().Parameters;
                param.AddWithValue("@Table_StatusMachine_id", SqlDbType.Int).Value = Table_StatusMachine_id;
                ds = new DBClass().SqlExcSto("pProcessUpdateMachineDownActiveLineGroup_B201", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessUpdateMachineDownActiveLineGroup_B201 SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessUpdateMachineDownActiveLineGroup_B201 Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessSearchMachineDownTimeEndJob_B201()
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessSearchMachineDownTimeEndJob_B201
                //  pProcessSearchMachineDownTimeEndJob_B201
                SqlParameterCollection param = new SqlCommand().Parameters;
                ds = new DBClass().SqlExcSto("pProcessSearchMachineDownTimeEndJob_B201", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessSearchMachineDownTimeEndJob_B201 SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessSearchMachineDownTimeEndJob_B201 Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessSearchCountStatistic(string Table_MasterLineMachineDown_code)
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessSearchCountStatistic
                //  pProcessSearchCountStatistic (@Table_MasterLineMachineDown_code varchar(10))
                SqlParameterCollection param = new SqlCommand().Parameters;
                param.AddWithValue("@Table_MasterLineMachineDown_code", SqlDbType.VarChar).Value = Table_MasterLineMachineDown_code;
                ds = new DBClass().SqlExcSto("pProcessSearchCountStatistic", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessSearchCountStatistic SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessSearchCountStatistic Exception : " + ex.Message);
            }
            return dataTable;
        }
        private static DataTable pProcessUpdateCountStatistic(string Table_MasterLineMachineDown_code)
        {
            DataTable dataTable = new DataTable();
            DataSet ds = new DataSet();
            try
            {
                //  อ่านค่าจาก Store pProcessSearchCountStatistic
                //  pProcessSearchCountStatistic (@Table_MasterLineMachineDown_code varchar(10))
                SqlParameterCollection param = new SqlCommand().Parameters;
                param.AddWithValue("@Table_MasterLineMachineDown_code", SqlDbType.VarChar).Value = Table_MasterLineMachineDown_code;
                ds = new DBClass().SqlExcSto("pProcessUpdateCountStatistic", "DbSet", param);
                dataTable = ds.Tables[0];
            }
            catch (SqlException e)
            {
                dataTable = null;
                log.Error("pProcessUpdateCountStatistic SqlException : " + e.Message);
            }
            catch (Exception ex)
            {
                dataTable = null;
                log.Error("pProcessUpdateCountStatistic Exception : " + ex.Message);
            }
            return dataTable;
        }
    }
}
