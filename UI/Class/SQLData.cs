using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using DevReport;
using MotionCtrl;

namespace UI
{
    public static class SQLData
    {
        public static readonly Object LockObj = new object();
        public static string TestDataerrSource(string file = "")
        {
            //if (file == "") file = DateTime.Now.ToString("yyyy_MM");
            //string filename = string.Format("{0}\\product\\{1}\\ErrDataBase\\{2}.db", Path.GetFullPath(".."), VAR.gsys_set.cur_product_name, file);
            //if (!Directory.Exists(filename))
            //{
            //    try
            //    {
            //        Directory.CreateDirectory(filename);
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show(VAR.IsChinese ? "建立文件夹出错：" : "Error creating folder:\r\n建立文件夹出错：" + ex.Message + "\r\n" + filename);
            //    }
            //}         
            //return string.Format("data source={0}", filename);


            string filename = string.Format("{0}\\product\\{1}\\ErrDataBase", Path.GetFullPath(".."),
                VAR.gsys_set.cur_product_name, file);
            if (!Directory.Exists(filename))
            {
                try
                {
                    Directory.CreateDirectory(filename);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(VAR.IsChinese ? "建立文件夹出错：" : "Error creating folder:\r\n建立文件夹出错：" + ex.Message + "\r\n" + filename);
                }
            }


            if (file == "") file = DateTime.Now.ToString("yyyy_MM");
            filename = string.Format("{0}\\product\\{1}\\ErrDataBase\\{2}.db", Path.GetFullPath(".."), VAR.gsys_set.cur_product_name, file);
            return string.Format("data source={0}", filename);
        }
        public static string TestDataSource(string file = "")
        {
            string filename = string.Format("{0}\\product\\{1}\\DataBase", Path.GetFullPath(".."),
                VAR.gsys_set.cur_product_name, file);
            if (!Directory.Exists(filename))
            {
                try
                {
                    Directory.CreateDirectory(filename);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(VAR.IsChinese?"建立文件夹出错：": "Error creating folder:\r\n建立文件夹出错：" + ex.Message + "\r\n" + filename);
                }
            }

           
            if (file == "") file = DateTime.Now.ToString("yyyy_MM");
             filename = string.Format("{0}\\product\\{1}\\DataBase\\{2}.db", Path.GetFullPath(".."),
                VAR.gsys_set.cur_product_name, file);
           //if (File.Exists(filename))
                return string.Format("data source={0}", filename);

           // return "";
        }

        public static string TestDataTable(SQLiteHelper sh, string tablename = "", bool bnew = true, string dbName = "")
        {
            tablename = tablename.Length > 0 ? tablename : DateTime.Now.ToString("TyyyyMMdd");
            DataTable dt= new DataTable();
            try
            {
                //dt = sh.Select($"select * from {(dbName.Length > 0 ? $"[{dbName}]." : "")}sqlite_master where name = '{tablename}'");
                string str = string.Format("select * from {0}sqlite_master where name = '{1}'",dbName.Length > 0 ? "["+dbName+"]." : "", tablename);
                dt = sh.Select(str);
            }
            catch (Exception e)
            {
                return "";
            }

            if (dt.Rows.Count == 0)
            {
                if (bnew)
                {
                    SQLiteTable tb = new SQLiteTable(tablename);
                    tb.Columns.Add(new SQLiteColumn("ID", ColType.Integer, true, true, true, "0"));
                    tb.Columns.Add(new SQLiteColumn("TIME", ColType.DateTime, false, false, false,
                        DateTime.Now.ToString("s"))); //yyyy/MM/dd HH:mm:ss
                    tb.Columns.Add(new SQLiteColumn("BARCODE", ColType.Text, false, false, false, "NOBARCODE"));
                    tb.Columns.Add(new SQLiteColumn("NUM", ColType.Integer, false, false, false, "0"));
                    tb.Columns.Add(new SQLiteColumn("WS_ID", ColType.Integer, false, false, false, "0"));
                    tb.Columns.Add(new SQLiteColumn("PC_ID", ColType.Integer, false, false, false, "0"));
                    tb.Columns.Add(new SQLiteColumn("BOX_ID", ColType.Integer, false, false, false, "0"));
                    tb.Columns.Add(new SQLiteColumn("RES", ColType.Integer, false, false, false, "0"));
                    tb.Columns.Add(new SQLiteColumn("CT", ColType.Integer, false, false, false, "0"));
                    tb.Columns.Add(new SQLiteColumn("PUD", ColType.Text, false, false, false, "正常"));
                    lock (LockObj)
                    {
                        sh.DropTable(tb.TableName);
                        sh.CreateTable(tb);
                    }
                }
                else return "";
            }

            return tablename;
        }
        public static string TesterrDataTable(SQLiteHelper sh, string tablename = "", bool bnew = true, string dbName = "")
        {
            tablename = tablename.Length > 0 ? tablename : DateTime.Now.ToString("TyyyyMMdd");
            DataTable dt = new DataTable();
            try
            {
                //dt = sh.Select($"select * from {(dbName.Length > 0 ? $"[{dbName}]." : "")}sqlite_master where name = '{tablename}'");
                string str = string.Format("select * from {0}sqlite_master where name = '{1}'", dbName.Length > 0 ? "[" + dbName + "]." : "", tablename);
                dt = sh.Select(str);
            }
            catch (Exception e)
            {
                return "";
            }

            if (dt.Rows.Count == 0)
            {
                if (bnew)
                {
                    SQLiteTable tb = new SQLiteTable(tablename);
                    tb.Columns.Add(new SQLiteColumn("ID", ColType.Integer, true, true, true, "0"));
                    tb.Columns.Add(new SQLiteColumn("TIME", ColType.DateTime, false, false, false, DateTime.Now.ToString("s")));
                    tb.Columns.Add(new SQLiteColumn("WSFEED", ColType.Integer, false, false, false, "0"));
                    tb.Columns.Add(new SQLiteColumn("WS", ColType.Integer, false, false, false, "0"));
                    tb.Columns.Add(new SQLiteColumn("TYPE", ColType.Text, false, false, false, "TYPE"));
                    tb.Columns.Add(new SQLiteColumn("ERRMSG", ColType.Text, false, false, false, "ERRMSG"));


                    lock (LockObj)
                    {
                        sh.DropTable(tb.TableName);
                        sh.CreateTable(tb);
                    }
                }
                else return "";
            }

            return tablename;
        }
        public static bool ConnectionChk(string file = "")
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(TestDataSource(file)))
                {
                    conn.Open();
                    conn.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, ex.Message, DReport.EmErrCode.ConnectFailed);
                return false;
            }
        }
        public static bool ConnectionerrChk(string file = "")
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(TestDataerrSource(file)))
                {
                    conn.Open();
                    conn.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                VAR.msg.AddMsg(Msg.EM_MSGTYPE.ERR, ex.Message, DReport.EmErrCode.ConnectFailed);
                return false;
            }
        }

        /// <summary>
        /// 聚合时间范围内的表格，跨度限定31天
        /// </summary>
        /// <param name="dtFrom">起始时间</param>
        /// <param name="dtEnd">结束时间</param>
        /// <param name="sh"></param>
        /// <param name="strFirstTable">第一个表格名称</param>
        /// <param name="strTable">表格聚合字符串</param>
        /// <returns>返回表格数</returns>
        public static int AttachFileAndGetTable(DateTime dtFrom, DateTime dtEnd, ref SQLiteHelper sh,
            ref string strFirstTable, ref string strTable)
        {
            if ((dtEnd - dtFrom).Days > 31)
            {
                MessageBox.Show(VAR.IsChinese?@"时间跨度不能超一个月": "Time span cannot exceed one month!\r\n时间跨度不能超一个月", VAR.IsChinese?@"提示": "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return -1;
            }

            int dtCnt = 0;
            DateTime dateTemp = dtFrom;

            //跨月份
            for (int m= dtFrom.Month; m <= dtEnd.Month; m++, dateTemp=dateTemp.AddMonths(1))
            {
               // string filename =$"{Path.GetFullPath("..")}\\product\\{VAR.gsys_set.cur_product_name}\\DataBase\\{dateTemp:yyyy_MM}.db";
                string temp = dateTemp.ToString("yyyy_MM");
                string filename = string.Format("{0}\\product\\{1}\\DataBase\\{2}.db", Path.GetFullPath(".."),VAR.gsys_set.cur_product_name, temp);
                if (File.Exists(filename))
                {
                    sh.AttachDatabase(filename, dateTemp.ToString("yyyy_MM"));
                }
            }

            //跨天
            dateTemp = dtFrom;
            strTable = "";
            strFirstTable = "";
            for (; dateTemp <= dtEnd.AddDays(1); dateTemp = dateTemp.AddDays(1))
            {
                //当月无数据
                // string filename = $"{Path.GetFullPath("..")}\\product\\{VAR.gsys_set.cur_product_name}\\DataBase\\{dateTemp:yyyy_MM}.db";
                string temp = dateTemp.ToString("yyyy_MM");
                string filename = string.Format("{0}\\product\\{1}\\DataBase\\{2}.db", Path.GetFullPath(".."), VAR.gsys_set.cur_product_name, temp);
                if (!File.Exists(filename)) continue;

                //exist?
                string tablename = dateTemp.ToString("TyyyyMMdd");
                tablename = TestDataTable(sh, tablename, false, dateTemp.ToString("yyyy_MM"));
                if ("" == tablename) continue;
                if (strFirstTable == "") strFirstTable = tablename;
                if (strTable != "")
                {
                    strTable += " UNION ALL ";
                }

                strTable += string.Format("select * from {0}",dateTemp.ToString("TyyyyMMdd"));
               
                dtCnt++;
            }

            if (strTable.Length > 0)
                strTable = string.Format("({0})", strTable);

            return dtCnt;
        }

        public static DataTable TestDataSelect(SQLSelector Selector, DataGridView dgv = null)
        {
            int ct = Environment.TickCount;
            int tablcecnt = 0;
            string tablecollection = "";
            string firstTbname = "";
            DataTable dt = new DataTable();

            using (SQLiteConnection conn = new SQLiteConnection(TestDataSource()))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();
                    SQLiteHelper sh = new SQLiteHelper(cmd);

                    tablcecnt = AttachFileAndGetTable(Selector.DateTimeForm, Selector.DateTimeEnd, ref sh,
                        ref firstTbname, ref tablecollection);

                    if (tablcecnt > 0)
                    {
                        string select = "";
                        if (Selector.Condition == "")
                        {
                            //select =$"select * from {tablecollection} where time between '{Selector.DateTimeForm:s}' and '{Selector.DateTimeEnd:s}'order by time;";
                            select = string.Format("select * from {0} where time between '{1:s}' and '{2:s}'order by time;",tablecollection,Selector.DateTimeForm, Selector.DateTimeEnd);
                        }
                        else
                        {
                            //select =$"select * from {tablecollection} where time between '{Selector.DateTimeForm:s}' and '{Selector.DateTimeEnd:s}' and {Selector.Condition} order by time;";
                            select = string.Format("select * from {0} where time between '{1:s}' and '{2:s}' and {3} order by time;", tablecollection,  Selector.DateTimeForm, Selector.DateTimeEnd, Selector.Condition);
                        }

                        dt = sh.Select(select);
                        //renum
                        int id = 1;
                        foreach (DataRow row in dt.Rows)
                        {
                            row["ID"] = id++;
                        }
                    }
                }
            }
            
            if (dgv != null)
            {
                if (dt.Rows.Count > 0)
                {
                    if (VAR.IsChinese)
                    {
                        dgv.DataSource = dt;
                        dgv.Columns["ID"].HeaderText = "序号";
                        dgv.Columns["ID"].Width = 70;
                        dgv.Columns["TIME"].HeaderText = "时间";
                        dgv.Columns["TIME"].MinimumWidth = 160;
                        dgv.Columns["TIME"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        dgv.Columns["BARCODE"].HeaderText = "二维码";
                        dgv.Columns["BARCODE"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        dgv.Columns["BARCODE"].MinimumWidth = 160;
                        dgv.Columns["WS_ID"].HeaderText = "工站";
                        dgv.Columns["WS_ID"].Width = 80;
                        dgv.Columns["PC_ID"].HeaderText = "电脑";
                        dgv.Columns["PC_ID"].Width = 80;
                        dgv.Columns["BOX_ID"].HeaderText = "工装";
                        dgv.Columns["BOX_ID"].Width = 80;
                        dgv.Columns["NUM"].HeaderText = "编号";
                        dgv.Columns["NUM"].Width = 80;
                        dgv.Columns["RES"].HeaderText = "结果";
                        dgv.Columns["RES"].Width = 80;
                        dgv.Columns["CT"].HeaderText = "用时";
                        dgv.Columns["CT"].Width = 80;
                        dgv.Columns["PUD"].HeaderText = "类型";
                        dgv.Columns["PUD"].Width = 80;
                    }
                    else
                    {
                        dgv.DataSource = dt;
                        dgv.Columns["ID"].HeaderText = "SN";
                        dgv.Columns["ID"].Width = 70;
                        dgv.Columns["TIME"].HeaderText = "TIME";
                        dgv.Columns["TIME"].MinimumWidth = 160;
                        dgv.Columns["TIME"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        dgv.Columns["BARCODE"].HeaderText = "Barcode";
                        dgv.Columns["BARCODE"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        dgv.Columns["BARCODE"].MinimumWidth = 160;
                        dgv.Columns["WS_ID"].HeaderText = "WS";
                        dgv.Columns["WS_ID"].Width = 80;
                        dgv.Columns["PC_ID"].HeaderText = "PC";
                        dgv.Columns["PC_ID"].Width = 80;
                        dgv.Columns["BOX_ID"].HeaderText = "BOX_ID";
                        dgv.Columns["BOX_ID"].Width = 80;
                        dgv.Columns["NUM"].HeaderText = "NUM";
                        dgv.Columns["NUM"].Width = 80;
                        dgv.Columns["RES"].HeaderText = "RES";
                        dgv.Columns["RES"].Width = 80;
                        dgv.Columns["CT"].HeaderText = "CT";
                        dgv.Columns["CT"].Width = 80;
                        dgv.Columns["PUD"].HeaderText = "PUD";
                        dgv.Columns["PUD"].Width = 80;
                    }
                }
                else
                {
                    dt.Clear();
                    dgv.DataSource = dt;
                }
                
            }

            //Selector.Lable =$"{firstTbname}...{(tablcecnt > 0 ? tablcecnt.ToString() : "")} [{Environment.TickCount - ct}ms]\r\n[{dt?.Rows.Count:000000}]";
            Selector.Lable = string.Format("{0}_{1}[{2}ms]\r\n[{3}]", firstTbname, tablcecnt > 0 ? tablcecnt.ToString() : "",Environment.TickCount - ct, dt!=null? dt.Rows.Count:000000);
            return dt;
        }
        public static DataTable TestDataerrSelect(SQLSelector Selector, DataGridView dgv = null)
        {
            int ct = Environment.TickCount;
            int tablcecnt = 0;
            string tablecollection = "";
            string firstTbname = "";
            DataTable dt = new DataTable();

            using (SQLiteConnection conn = new SQLiteConnection(TestDataerrSource()))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();
                    SQLiteHelper sh = new SQLiteHelper(cmd);

                    tablcecnt = AttachFileAndGetTable(Selector.DateTimeForm, Selector.DateTimeEnd, ref sh,
                        ref firstTbname, ref tablecollection);

                    if (tablcecnt > 0)
                    {
                        string select = "";
                        if (Selector.Condition == "")
                        {
                            //select =$"select * from {tablecollection} where time between '{Selector.DateTimeForm:s}' and '{Selector.DateTimeEnd:s}'order by time;";
                            select = string.Format("select * from {0} where time between '{1:s}' and '{2:s}'order by time;", tablecollection, Selector.DateTimeForm, Selector.DateTimeEnd);
                        }
                        else
                        {
                            //select =$"select * from {tablecollection} where time between '{Selector.DateTimeForm:s}' and '{Selector.DateTimeEnd:s}' and {Selector.Condition} order by time;";
                            select = string.Format("select * from {0} where time between '{1:s}' and '{2:s}' and {3} order by time;", tablecollection, Selector.DateTimeForm, Selector.DateTimeEnd, Selector.Condition);
                        }

                        dt = sh.Select(select);
                        //renum
                        int id = 1;
                        foreach (DataRow row in dt.Rows)
                        {
                            row["ID"] = id++;
                        }
                    }
                }
            }

            if (dgv != null)
            {
                if (dt.Rows.Count > 0)
                {
                    dgv.DataSource = dt;
                    dgv.Columns["ID"].HeaderText = "序号";
                    dgv.Columns["ID"].MinimumWidth = 80;
                    dgv.Columns["TIME"].HeaderText = "时间";
                    dgv.Columns["TIME"].MinimumWidth = 200;
                    dgv.Columns["WSFEED"].HeaderText = "工站";
                    dgv.Columns["WSFEED"].MinimumWidth = 40;
                    dgv.Columns["WS"].HeaderText = "夹具";
                    dgv.Columns["WS"].MinimumWidth = 40;
                    dgv.Columns["TYPE"].HeaderText = "类型";
                    dgv.Columns["TYPE"].MinimumWidth = 180;
                    dgv.Columns["ERRMSG"].HeaderText = "报警记录";
                    dgv.Columns["ERRMSG"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgv.Columns["ERRMSG"].MinimumWidth = 240;

                }
                else
                {
                    dt.Clear();
                    dgv.DataSource = dt;
                }

            }

            //Selector.Lable =$"{firstTbname}...{(tablcecnt > 0 ? tablcecnt.ToString() : "")} [{Environment.TickCount - ct}ms]\r\n[{dt?.Rows.Count:000000}]";
            Selector.Lable = string.Format("{0}...{1} [{2}ms]\r\n[{3}]", firstTbname, tablcecnt > 0 ? tablcecnt.ToString() : "", Environment.TickCount - ct, dt != null ? dt.Rows.Count : 000000);
            return dt;
        }
        public static void TestNGCount(SQLSelector Selector, Chart chart = null)
        {
            DataTable dtall = null;
            int dt_cnt = 0;
            int count = 0;
            int ng_cnt=0;
            double per = 0;
            int all_cnt = 0;
            int allcnt = 1;
            string tablecollection = "";
            string firsttablename = "No Data";
            int ct = Environment.TickCount;

            ConnectionChk();
            using (SQLiteConnection conn = new SQLiteConnection(TestDataSource()))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();
                    SQLiteHelper sh = new SQLiteHelper(cmd);

                    //聚合相关表格
                    int tablcecnt = AttachFileAndGetTable(Selector.DateTimeForm, Selector.DateTimeEnd, ref sh,
                        ref firsttablename, ref tablecollection);

                    //查询
                    if (tablecollection.Length > 0)
                    {
                        string select = "";
                        if (Selector.Condition.Length > 0) select = string.Format("select RES, count(*) as CNT from {0} where TIME between '{1:s}' and '{2:s}' and {3} group by RES order by CNT desc", tablecollection, Selector.DateTimeForm, Selector.DateTimeEnd, Selector.Condition);
                        else select = string.Format("select RES, count(*) as CNT from {0} where TIME between '{1:s}' and '{2:s}'  group by RES order by CNT desc", tablecollection, Selector.DateTimeForm, Selector.DateTimeEnd);

                        dtall = sh.Select(select);

                        string pud = string.Empty;
                        if (Selector.Condition.Contains("PUD = '正常'")) pud = "and PUD = '正常'";
                        else if (Selector.Condition.Contains("PUD = '复测'")) pud = "and PUD = '复测'";
                        string str = string.Format(
                            "select * from {0} where time between '{1:s}' and '{2:s}'{3} order by time;",
                            tablecollection, Selector.DateTimeForm, Selector.DateTimeEnd, pud);

                        DataTable dt = sh.Select(str);

                        if (dt.Rows.Count > 0)
                        {
                            allcnt = dt.Rows.Count;
                        }
                    }
                }
            }

            if (chart != null)
            {
                chart.ChartAreas[0].AxisX.LabelStyle.Enabled = true;
                chart.ChartAreas[0].AxisX.LabelStyle.Angle = 0;
                chart.ChartAreas[0].AxisX.LabelStyle.Interval = 1;
                chart.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
                chart.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightGray;
                chart.ChartAreas[0].AxisX.IsMarginVisible = false;
                chart.ChartAreas[0].AxisY.IsMarginVisible = false;
                chart.Series.Clear();
                chart.Series.Add("NG");
                chart.Series["NG"].IsValueShownAsLabel = true;
              //  chart.Series["NG"].IsVisibleInLegend = false;
                chart.Series["NG"].Color = Color.Orange;
                chart.Series["NG"].Points.Clear();
                chart.Series["NG"].LegendText = string.Empty;
                DataTable dt = Utility.ReadFromXml(VAR.gsys_set.GetCurProductPath + "ngcode.xml");
                int other = 0;
                int res;
                if (dtall != null)
                {
                    foreach (DataRow row in dtall.Rows)
                    {
                        res = int.Parse(row["RES"].ToString());
                        if (res > 0)
                        {
                            // if (chart.Series["NG"].Points.Count > 20)
                            if (chart.Series["NG"].Points.Count > 20)
                            {                               
                                other += int.Parse(row["CNT"].ToString());
                            }
                            else
                            {                                                           
                               chart.Series["NG"].Points.AddXY(row["RES"].ToString(), row["CNT"]);
                                if (chart.Series["NG"].Points.Count <= 10)
                                {
                                    string a = row["RES"].ToString();
                                    DataRow[] drs = dt.Select(string.Format("代码 = '{0}'",a));
                                    if (dtall.Rows.Count > 0)
                                    {
                                        try
                                        {
                                            ng_cnt = int.Parse(dtall.Compute("sum(CNT)", "RES="+a).ToString());
                                           // all_cnt= int.Parse(dtall.Compute("sum(CNT)", "RES >= 0").ToString());
                                             per = ng_cnt;
                                            if (allcnt > 0) per = per / allcnt * 100;
                                            else per = 0;
                                        }
                                        catch (Exception e)
                                        {
                                        }
                                    }
                                    if (drs.Length>0)
                                    chart.Series["NG"].LegendText += chart.Series["NG"].Points.Count.ToString() + ":" + row["RES"].ToString() + "->"+ drs[0]["名称"] + "(" + per.ToString("f2") + "%)\r\n";
                                }
                              
                            }
                        }
                    }
                    if (other > 0)
                    {
                        chart.Series["NG"].Points.AddXY("other", other);
                    }
                    for (int n = chart.Series["NG"].Points.Count; n < 20; n++)
                    {
                        chart.Series["NG"].Points.AddXY(" ", "");
                    }

                    if (dtall.Rows.Count > 0)
                    {
                        try
                        {
                            count = int.Parse(dtall.Compute("sum(CNT)", "RES>0").ToString());
                        }
                        catch (Exception e)
                        {
                            count = -1;
                        }
                    }
                    else count = -1;
                }
            }
            //Selector.Lable =$"{firsttablename}...{dt_cnt} [{Environment.TickCount - ct}ms]\r\n[{count:000000}]   [{count * 100.0 / allcnt:F2}%]";
            Selector.Lable = string.Format("{0}_{1}[{2}ms]\r\n[{3:000000}]   [{4:f2}%]", firsttablename, dt_cnt, Environment.TickCount - ct, count,count * 100.0 / allcnt);
        }
        public static void TestDataProductCount(SQLSelector Selector, Chart chart = null)
        {
            DateTime date_temp = DateTime.Parse(Selector.DateTimeForm.ToString("yyyy-MM-dd HH:00:00"));
            if (chart != null)
            {
                chart.ChartAreas[0].AxisX.LabelStyle.Enabled = true;
                chart.ChartAreas[0].AxisX.LabelStyle.Angle = -60;
                chart.ChartAreas[0].AxisX.LabelStyle.Interval = 1;
                chart.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
                chart.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightGray;
                chart.Series.Clear();
                //chart.Series.Add("总数");
                //chart.Series["总数"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
                //chart.Series["总数"].IsValueShownAsLabel = true;
                for (int n = 1; n <= 4; n++)
                {
                    string s = VAR.IsChinese?string.Format("工站{0}",n): string.Format("WS{0}", n);
                    chart.Series.Add(s);
                    chart.Series[s].Points.Clear();
                    chart.Series[s].BorderWidth = 3;
                    chart.Series[s].ChartType = SeriesChartType.StackedColumn;
                }
            }
            int count = 0;
            int tablcecnt = 0;
            string table_temp = "";
            int ct = Environment.TickCount;
            string FirstTbname = "No Data";
            while ((Selector.DateTimeEnd - date_temp) > new TimeSpan(1, 0, 0))
            {
                //提取file
                string file = date_temp.ToString("yyyy_MM");
                string filename = string.Format("{0}\\product\\{1}\\DataBase\\{2}.db", Path.GetFullPath(".."), VAR.gsys_set.cur_product_name, file);
                if (!File.Exists(filename))
                {
                    date_temp = date_temp.AddMonths(1);
                    continue;
                };

                ConnectionChk();
                using (SQLiteConnection conn = new SQLiteConnection(TestDataSource(file)))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand())
                    {
                        cmd.Connection = conn;
                        conn.Open();
                        string s = VAR.IsChinese?string.Format("工站{0}",1): string.Format("WS{0}", 1);
                        for (int h = date_temp.Hour; h < 24; h++, date_temp = date_temp.AddHours(1))
                        {
                            //提取table
                            if ((Selector.DateTimeEnd - date_temp) < new TimeSpan(1, 0, 0)) break;
                            SQLiteHelper sh = new SQLiteHelper(cmd);
                            string tablename = date_temp.ToString("TyyyyMMdd");
                            tablename = TestDataTable(sh, tablename, false);
                            if ("" == tablename) continue;
                            if (tablename != table_temp) tablcecnt++;
                            table_temp = tablename;
                            //提取data
                            string select = "";
                            if (Selector.Condition == "") select =string.Format("select time,ws_id, count(*) from {0} where time between '{1:s}' and '{2:s}' group by WS_ID; ", tablename, date_temp, (date_temp + new TimeSpan(1, 0, 0)));
                            else select =string.Format("select time,ws_id, count(*) from {0} where time between '{1:s}' and '{2:s}' and {3} group by WS_ID; ", tablename, date_temp, (date_temp + new TimeSpan(1, 0, 0)), Selector.Condition);
                            DataTable dt = sh.Select(select);

                            int ws_id = 0;
                            int num = 0;
                            int sum = 0;
                            bool bws1 = false;
                            bool bws2 = false;
                            bool bws3 = false;
                            bool bws4 = false;
                            if (dt != null && dt.Rows.Count > 0)
                            {
                                if (FirstTbname == "No Data") FirstTbname = tablename;
                                foreach (DataRow row in dt.Rows)
                                {
                                    ws_id = int.Parse(row["WS_ID"].ToString());
                                    switch (ws_id)
                                    {
                                        case 0:
                                            bws1 = true;
                                            break;
                                        case 1:
                                            bws2 = true;
                                            break;
                                        case 2:
                                            bws3 = true;
                                            break;
                                        case 3:
                                            bws4 = true;
                                            break;
                                    }
                                    num = int.Parse(row["count(*)"].ToString());
                                    s = VAR.IsChinese?string.Format("工站{0}", ws_id + 1): string.Format("WS{0}", ws_id + 1);
                                    chart.Series[s].Points.AddXY(date_temp.ToString("HH:mm"), num);
                                    sum += num;
                                }
                                //补空
                                if (VAR.IsChinese)
                                {
                                    if (!bws1) chart.Series["工站1"].Points.AddXY(date_temp.ToString("HH:mm"), 0);
                                    if (!bws2) chart.Series["工站2"].Points.AddXY(date_temp.ToString("HH:mm"), 0);
                                    if (!bws3) chart.Series["工站3"].Points.AddXY(date_temp.ToString("HH:mm"), 0);
                                    if (!bws4) chart.Series["工站4"].Points.AddXY(date_temp.ToString("HH:mm"), 0);
                                }
                                else
                                {
                                    if (!bws1) chart.Series["WS1"].Points.AddXY(date_temp.ToString("HH:mm"), 0);
                                    if (!bws2) chart.Series["WS2"].Points.AddXY(date_temp.ToString("HH:mm"), 0);
                                    if (!bws3) chart.Series["WS3"].Points.AddXY(date_temp.ToString("HH:mm"), 0);
                                    if (!bws4) chart.Series["WS4"].Points.AddXY(date_temp.ToString("HH:mm"), 0);
                                }

                                count += sum;
                                //chart.Series["总数"].Points.AddXY(date_temp.ToString("HH:mm"), sum);
                            }
                            
                        }
                    }
                }
                Selector.Lable = string.Format("{0}_{1}[{2}ms]\r\n[{3:000000}]", FirstTbname, tablcecnt > 0 ? tablcecnt.ToString() : "", Environment.TickCount - ct, count);
            }
        }

        public static void TestDataSoketCount(SQLSelector Selector, Chart chart = null)
        {

            int ct = Environment.TickCount;
            int tablcecnt = 0;
            string tablecollection = "";
            string firstTbname = "";
            int[,] xy = new int[4, 64];

            DataTable dt = new DataTable();
            ConnectionChk();
            int cnt_ttl = 1;
            int cnt_ng = 0;
            using (SQLiteConnection conn = new SQLiteConnection(TestDataSource()))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();
                    SQLiteHelper sh = new SQLiteHelper(cmd);

                    tablcecnt = AttachFileAndGetTable(Selector.DateTimeForm, Selector.DateTimeEnd, ref sh,
                        ref firstTbname, ref tablecollection);

                    if (tablcecnt > 0)
                    {
                        string select = "";
                        if (Selector.Condition.Length > 0)
                            select = string.Format(
                                "select WS_ID,NUM, count(*) as CNT from {0} where TIME between '{1}' and '{2}' and {3} group by WS_ID,NUM",
                                tablecollection, Selector.DateTimeForm.ToString("s"),
                                Selector.DateTimeEnd.ToString("s"), Selector.Condition);
                        else
                            select = string.Format(
                                "select WS_ID,NUM, count(*) as CNT from {0} where TIME between '{1}' and '{2}' group by WS_ID,NUM",
                                tablecollection, Selector.DateTimeForm.ToString("s"),
                                Selector.DateTimeEnd.ToString("s"));

                        dt = sh.Select(string.Format(
                            "select count(*) as CNT from {0} where TIME between '{1}' and '{2}'",
                            tablecollection, Selector.DateTimeForm.ToString("s"),
                            Selector.DateTimeEnd.ToString("s")));
                        if(dt.Rows.Count>0)
                            cnt_ttl = int.Parse(dt.Rows[0]["CNT"].ToString());

                        dt = sh.Select(select);
                        if (dt.Rows.Count > 0)
                            cnt_ng = int.Parse(dt.Compute("sum(CNT)", "true").ToString());

                        foreach (DataRow row in dt.Rows)
                        {
                            int ws_id = int.Parse(row["WS_ID"].ToString());
                            int num = int.Parse(row["NUM"].ToString()) + ws_id * 16 - 1;
                            int cnt = int.Parse(row["CNT"].ToString());
                            xy[ws_id, num] += cnt;
                        }
                    }
                }
            }

            if (chart != null)
            {
                chart.ChartAreas[0].AxisX.LabelStyle.Enabled = false;
                chart.ChartAreas[0].AxisX.LabelStyle.IsEndLabelVisible = true;
                chart.ChartAreas[0].AxisX.LabelStyle.Angle = 90;
                chart.ChartAreas[0].AxisX.LabelAutoFitStyle = LabelAutoFitStyles.DecreaseFont;
                chart.ChartAreas[0].AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
                chart.ChartAreas[0].AxisX.LabelStyle.Interval = 1;
                chart.ChartAreas[0].AxisX.MajorGrid.Interval = 16;
                chart.ChartAreas[0].AxisX.MajorGrid.Enabled = true;
                chart.ChartAreas[0].AxisX.MajorGrid.IntervalOffset = 0;
                chart.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightGray;
                chart.ChartAreas[0].AxisX.IsMarginVisible = true;
                 chart.ChartAreas[0].AxisX.IsStartedFromZero = true;
                chart.ChartAreas[0].AxisX.IntervalOffset = 0;
                chart.Series.Clear();
                for (int n = 1; n <= 4; n++)
                {
                    string s = VAR.IsChinese?string.Format("工站{0}", n): string.Format("WS{0}", n);
                    chart.Series.Add(s);
                    chart.Series[s].Points.Clear();
                    chart.Series[s]["PointWidth"] = "2";
                    chart.Series[s].IsValueShownAsLabel = true;
                    for (int m = 0; m < (n - 1) * 16 + 16; m++)
                    {
                        if (m < (n - 1) * 16)//|| xy[n - 1, m]==0
                        {
                            chart.Series[s].Points.AddXY(m + 0, 0);
                            chart.Series[s].Points[m].IsEmpty = true;
                            continue;
                        }
                        chart.Series[s].Points.AddXY(m + 0, xy[n - 1, m]);
                        //chart.Series[s].Points[m].AxisLabel = string.Format("{0}", m%16);
                    }
                }
            }

            Selector.Lable = string.Format("{0}_{1}[{2}ms]\r\n[{3:000000}]  {4:F2}%", firstTbname,
                tablcecnt > 0 ? tablcecnt.ToString() : "", Environment.TickCount - ct,
                new DataTable() == null ? -1 : cnt_ng, cnt_ng*100.0/cnt_ttl);
        }

        //public static EM_RES TestDataAdd(List<WS.MdDat> ListMd)
        public static EM_RES TestDataAdd(WSFEED ws)
        {
            ConnectionChk();
            using (SQLiteConnection conn = new SQLiteConnection(TestDataSource()))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();
                    int okcnt = 0;
                    SQLiteHelper sh = new SQLiteHelper(cmd);
                    string table = TestDataTable(sh);
                    lock (LockObj)
                    {
                        var dic = new Dictionary<string, object>();
                        sh.BeginTransaction();
                        foreach (WSFEED.MdDat md in ws.list_md)
                        {
                            if (!md.benable) continue;
                             if(!md.Ishavemd) continue;
                            if (md.res < 0) continue;
                            if ((md.bardcode == null || md.bardcode.Length < 1) && PT_SET.BarcodeMode != (int)PT_SET.BAR_SCAN.NO_SCAN) continue;
                            if (md.res > 0)
                            {
                                if (!VAR.Isnormal) COUNT_DATA.NgTwoTestCnt++;
                                if (md.res == PT_SET.ngCode) COUNT_DATA.ngctrlngcnt++;
                                COUNT_DATA.ngcnt[ws.id]++;
                                //gy0123增加NG类型统计
                                if (Math.Round((double) COUNT_DATA.ngctrlngcnt / COUNT_DATA.ngctrlallcnt, 4) >=
                                    PT_SET.ngScale / 100)
                                {
                                    MessageBox.Show($"NG码{PT_SET.ngCode}的不良率超过限制，NG个数为{COUNT_DATA.ngctrlngcnt},总数为{COUNT_DATA.ngctrlallcnt}");
                                    COUNT_DATA.ngctrlallcnt = 0;
                                    COUNT_DATA.ngctrlngcnt = 0;
                                }
                                if (md.res == 266 || md.res == 274 || md.res == 270) COUNT_DATA.openimagerate++;
                                 if (md.res == (int)WSFEED.Md_RES.NG_EXPOSURE) COUNT_DATA.cnt_ng_exposure++;
                                else if (md.res == (int)WSFEED.Md_RES.NG_IIC) COUNT_DATA.cnt_ng_iic++;
                                else if (md.res == (int)WSFEED.Md_RES.NG_IMAGE|| md.res == (int)WSFEED.Md_RES.NG_IMAGE2) COUNT_DATA.cnt_ng_iic++;
                                else if (md.res == (int)WSFEED.Md_RES.NG_MISS_PIX) COUNT_DATA.cnt_ng_miss_pix++;
                                else if (md.res == (int)WSFEED.Md_RES.NG_OS) COUNT_DATA.cnt_ng_OS++;
                                else  COUNT_DATA.cnt_ng_other++;
                                //NG数据上传
                                DRpt.Report_NgProduct(md.res, (ws.id+1) * 100 + md.Num, md.bardcode, !VAR.Isnormal);
                            }
                            else if (md.res == 0)
                            {
                                okcnt++;
                                if (!VAR.Isnormal) COUNT_DATA.OkTwoTestCnt++;
                                COUNT_DATA.okcnt[ws.id]++;
                            }

                            if (PT_SET.bSameNGTip && VAR.bSameNGTip_Temp)
                            {
                                if (md.last_res == md.res && md.res > 0) md.NgSameRes_cnt++;
                                else md.NgSameRes_cnt = 1;
                                md.last_res = md.res;
                            }
                               
                            dic["TIME"] = DateTime.Now.ToString("s");
                            dic["BARCODE"] = md.bardcode == null ? "" : md.bardcode;
                            dic["NUM"] = md.Num;
                            dic["WS_ID"] = md.WS_ID;
                            dic["PC_ID"] = md.PC_ID;
                            dic["BOX_ID"] = md.TestBox_ID;
                            dic["RES"] = md.res;
                            dic["CT"] = md.ct;
                            dic["PUD"] = md.IsNormal==true ? "正常" : "复测";
                            try
                            {
                                sh.Insert(table, dic);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
                        //上传OK数量
                        DRpt.Report_OkProduct((ws.id+ 1) * 100+okcnt, !VAR.Isnormal);
                        sh.Commit();
                        conn.Close();
                    }
                }
            }
            return EM_RES.OK;
        }

        public static EM_RES TesterrDataAdd(int id, int wsid, String type = "", String mes = "")
        {
           if(! ConnectionerrChk())return EM_RES.ERR;
            using (SQLiteConnection conn = new SQLiteConnection(TestDataerrSource()))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();
                    SQLiteHelper sh = new SQLiteHelper(cmd);
                    string table = TesterrDataTable(sh);
                    lock (LockObj)
                    {
                        var dic = new Dictionary<string, object>();
                        sh.BeginTransaction();
                        dic["TIME"] = DateTime.Now.ToString("s");
                        dic["WSFEED"] = id;
                        dic["WS"] = wsid;//夹具号
                        dic["TYPE"] = type;
                        dic["ERRMSG"] = mes;
                        try
                        {
                            sh.Insert(table, dic);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }

                        sh.Commit();
                        conn.Close();
                    }
                }
            }
            return EM_RES.OK;
        }
    }
}