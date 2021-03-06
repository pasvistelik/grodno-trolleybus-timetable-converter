﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using TransportClasses;
using System.Dynamic;

namespace GrodnoTrolleybusTimetableConverter
{
    class Converter
    {
        public dynamic convertation_result_route;

        private List<Timetable>[] timetables = null;

        private static int ParseTime(SimpleTime item)
        {
            int tmp = (item.hour > 4) ? 0 : 86400;
            return tmp + item.hour * 3600 + item.minute * 60;
        }
        private Converter(string filepath)
        {
            convertation_result_route = new ExpandoObject();


            string transportNumber = null, transportName = null;
            List<Timetable>[] fullTable = new List<Timetable>[2];
            List<Timetable>[] fullTable_depo = new List<Timetable>[2];


            List<DayOfWeek> workingDays = new List<DayOfWeek>();
            workingDays.AddRange(new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday });
            List<DayOfWeek> weekDays = new List<DayOfWeek>();
            weekDays.AddRange(new DayOfWeek[] { DayOfWeek.Saturday, DayOfWeek.Sunday });

            Regex timePattern = new Regex("[0-9]{2}");
            timetables = new List<Timetable>[2];
            Excel.Application ObjWorkExcel = new Excel.Application(); //открыть эксель
            Excel.Workbook ObjWorkBook = ObjWorkExcel.Workbooks.Open(filepath, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing); //открыть файл
            for (int k = 1; k <= 2; k++)
            {
                fullTable[k - 1] = new List<Timetable>();
                fullTable_depo[k - 1] = new List<Timetable>();

                timetables[k - 1] = new List<Timetable>();
                Excel.Worksheet ObjWorkSheet = (Excel.Worksheet)ObjWorkBook.Sheets[k]; //получить k-ый лист
                                                                                       //MessageBox.Show(ObjWorkSheet.Name);



                var lastCell = ObjWorkSheet.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell);//1 ячейку
                int allRows = lastCell.Row, allCollumns = lastCell.Column;
                for (int i = 1; i < allRows; i += 9)
                    if (((string)ObjWorkSheet.Cells[i, 3].Text).ToUpper().Contains("НАЗВАНИЕ ОСТАНОВКИ"))
                    {
                        allRows = i - 1;
                        break;
                    }

                string[,] list = new string[allCollumns, allRows]; // массив значений с листа равен по размеру листу

                Excel.Range cell = ObjWorkSheet.Cells;


                ////////////////////////////////////////////////////////////////////////////////
                /*var ss1 = (cell.Cells[6, 6] as Excel.Range).Characters;
                Excel.Characters tmp = (cell.Cells[6, 6] as Excel.Range).Characters;

                for (int i = 1; i < (cell.Cells[6, 6] as Excel.Range).Characters.Count; i += 3) MessageBox.Show((cell.Cells[6, 6] as Excel.Range).Characters[i, 2].Font.Color + " ]]] " + cell.Cells[6, 6].Text);
                for (int i = 1; i < (cell.Cells[7, 6] as Excel.Range).Characters.Count; i += 3) MessageBox.Show((cell.Cells[7, 6] as Excel.Range).Characters[i, 2].Font.Color + " ]]] " + cell.Cells[6, 6].Text);

                var ss = (cell.Cells[6, 6] as Excel.Range).CurrentArray.Count;
                MessageBox.Show(ss + " ]]] " + cell.Cells[6, 6].Text);*/


                if (k == 1)
                {
                    transportName = ((string)(cell[4, 2].Text)).Trim();
                    transportNumber = ((string)(cell[5, 2].Text)).Trim();

                    convertation_result_route.route_type = "trolleybus";
                    convertation_result_route.route_number = transportNumber;
                    convertation_result_route.route_name = transportName;
                    convertation_result_route.ways = new List<dynamic>();
                }

                dynamic main_way = new ExpandoObject();
                dynamic way_to_depo = new ExpandoObject();
                dynamic way_from_depo = new ExpandoObject();

                string first_station_name = (string)(cell[1, 3].Text);
                string last_station_name = (string)(cell[allRows - 8, 3].Text);

                main_way.way_name = first_station_name + " - " + last_station_name;
                way_to_depo.way_name = first_station_name + " - Троллейбусное депо";
                way_from_depo.way_name = "Троллейбусное депо - " + last_station_name;

                main_way.stations_names = new List<string>();
                way_to_depo.stations_names = new List<string>();
                way_from_depo.stations_names = new List<string>();

                main_way.trips_by_days = new dynamic[2];
                way_to_depo.trips_by_days = new dynamic[2];
                way_from_depo.trips_by_days = new dynamic[2];

                main_way.trips_by_days[0] = new ExpandoObject();
                main_way.trips_by_days[1] = new ExpandoObject();
                main_way.trips_by_days[0].days_of_week = workingDays;
                main_way.trips_by_days[1].days_of_week = weekDays;
                main_way.trips_by_days[0].arrives = new List<List<int>>();
                main_way.trips_by_days[1].arrives = new List<List<int>>();

                way_to_depo.trips_by_days[0] = new ExpandoObject();
                way_to_depo.trips_by_days[1] = new ExpandoObject();
                way_to_depo.trips_by_days[0].days_of_week = workingDays;
                way_to_depo.trips_by_days[1].days_of_week = weekDays;
                way_to_depo.trips_by_days[0].arrives = new List<List<int>>();
                way_to_depo.trips_by_days[1].arrives = new List<List<int>>();

                way_from_depo.trips_by_days[0] = new ExpandoObject();
                way_from_depo.trips_by_days[1] = new ExpandoObject();
                way_from_depo.trips_by_days[0].days_of_week = workingDays;
                way_from_depo.trips_by_days[1].days_of_week = weekDays;
                way_from_depo.trips_by_days[0].arrives = new List<List<int>>();
                way_from_depo.trips_by_days[1].arrives = new List<List<int>>();





                for (int startString = 1; startString < allRows; startString += 9) // по всем строкам
                {
                    bool is_to_depo_finded = false;
                    /*
                    if (cell[startString + 3, 2].Text == "МАРШРУТ" || cell[startString + 3, 2].Text == string.Empty) continue;
                    string filename = "full." + cell[5, 2].Text + ".(" + cell[startString + 3, 2].Text + ")." + ((string)(cell[startString, 3].Text)).Replace("\"", "") + ".json";
                    string filename_depo = "file." + cell[5, 2].Text + ".(" + cell[startString + 3, 2].Text + ")." + ((string)(cell[startString, 3].Text)).Replace("\"", "") + ".depo.json";


                    DirectoryInfo myPath = Directory.CreateDirectory(@"..\..\..\..\..\GrodnoTrolleybusTimetableConverterResults\" + cell[5, 2].Text);
                    DirectoryInfo myPath_depo = Directory.CreateDirectory(@"..\..\..\..\..\GrodnoTrolleybusTimetableConverterResults\" + cell[5, 2].Text + "_depo");
                    
                    
                    StreamWriter SW = new StreamWriter(new FileStream(myPath.FullName + @"\" + filename, FileMode.Create, FileAccess.Write));
                    StreamWriter SW_depo = new StreamWriter(new FileStream(myPath_depo.FullName + @"\" + filename_depo, FileMode.Create, FileAccess.Write));
                    */


                    ObservableCollection<SimpleTime> workingTimes = new ObservableCollection<SimpleTime>();
                    ObservableCollection<SimpleTime> workingTimesToDepo = new ObservableCollection<SimpleTime>();
                    if (Regex.IsMatch(cell[startString + 4, 3].Text, "^5"))
                    {
                        //workingTimes.Add(new SimpleTime(5, int.Parse((timePattern.Match(cell[startString + 4, 3].Text)).Value)));
                        //MessageBox.Show(workingTimes[workingTimes.Count-1].ToString());
                        for (int i = startString + 4; i <= startString + 6; i++)
                        {
                            foreach (Match s in timePattern.Matches(cell[i, 3].Text))
                            {
                                workingTimes.Add(new SimpleTime(5, int.Parse(s.Value)));
                                //MessageBox.Show(workingTimes[workingTimes.Count - 1].ToString());
                            }
                        }
                    }
                    for (int j = 4, hour = (j + 2) % 24; j <= 22; j++, hour = (j + 2) % 24)
                    {
                        for (int i = startString + 4; i <= startString + 6; i++)
                        {
                            if (!timePattern.IsMatch(cell[i, j].Text)) continue;

                            //cell[i, j]//.NumberFormat = "Text";
                            Excel.Range tmp_cell = (cell[i, j] as Excel.Range);

                            //var tmp00 = tmp_cell.Characters;

                            //try
                            //{
                            for (int q = 1; q < tmp_cell.Value.ToString().Length; q += 3)
                            {
                                string tm0000 = null;
                                try
                                {
                                    tm0000 = tmp_cell.Characters[q, 2].Text;
                                }
                                catch
                                {
                                    tm0000 = tmp_cell.Value.ToString();
                                }

                                if (tmp_cell.Characters[q, 2].Font.Color == 0)
                                {
                                    //MessageBox.Show("В депо: " + (cell.Cells[i, j] as Excel.Range).Characters[q, 2].Text);
                                    workingTimesToDepo.Add(new SimpleTime(hour, int.Parse(tm0000)));
                                    //MessageBox.Show(workingTimesToDepo[workingTimesToDepo.Count - 1].ToString());
                                    is_to_depo_finded = true;
                                }
                                else
                                {
                                    //MessageBox.Show((cell.Cells[i, j] as Excel.Range).Characters[q, 2].Font.Color);
                                    //MessageBox.Show("Не в депо: " + (cell.Cells[i, j] as Excel.Range).Characters[q, 2].Text);
                                    workingTimes.Add(new SimpleTime(hour, int.Parse(tm0000)));
                                    //MessageBox.Show(workingTimes[workingTimes.Count - 1].ToString());
                                }
                            }
                            /*}
                            catch(Exception ex)
                            {
                                foreach (Match s in timePattern.Matches(cell[i, j].Text))
                                {
                                    workingTimes.Add(new SimpleTime(hour, int.Parse(s.Value)));
                                    //MessageBox.Show(workingTimes[workingTimes.Count - 1].ToString());
                                }
                            }*/
                        }
                    }
                    Table workingTable = new Table(workingDays, workingTimes);
                    Table workingTableToDepo = new Table(workingDays, workingTimesToDepo);

                    ObservableCollection<SimpleTime> weekTimes = new ObservableCollection<SimpleTime>();
                    ObservableCollection<SimpleTime> weekTimesToDepo = new ObservableCollection<SimpleTime>();
                    if (Regex.IsMatch(cell[startString + 7, 3].Text, "^5"))
                    {
                        //weekTimes.Add(new SimpleTime(5, int.Parse((timePattern.Match(cell[startString + 7, 3].Text)).Value)));
                        //MessageBox.Show(weekTimes[weekTimes.Count - 1].ToString());
                        for (int i = startString + 7; i <= startString + 8; i++)
                        {
                            foreach (Match s in timePattern.Matches(cell[i, 3].Text))
                            {
                                weekTimes.Add(new SimpleTime(5, int.Parse(s.Value)));
                                //MessageBox.Show(weekTimes[weekTimes.Count - 1].ToString());
                            }
                        }
                    }
                    for (int j = 4, hour = (j + 2) % 24; j <= 22; j++, hour = (j + 2) % 24)
                    {
                        for (int i = startString + 7; i <= startString + 8; i++)
                        {
                            if (!timePattern.IsMatch(cell[i, j].Text)) continue;

                            Excel.Range tmp_cell = (cell[i, j] as Excel.Range);
                            //try
                            //{
                            for (int q = 1; q < (cell.Cells[i, j] as Excel.Range).Characters.Count; q += 3)
                            {
                                string tm0000 = null;
                                try
                                {
                                    tm0000 = tmp_cell.Characters[q, 2].Text;
                                }
                                catch
                                {
                                    tm0000 = tmp_cell.Value.ToString();
                                }

                                if ((cell.Cells[i, j] as Excel.Range).Characters[q, 2].Font.Color == 0)
                                {
                                    //MessageBox.Show("В депо: " + (cell.Cells[i, j] as Excel.Range).Characters[q, 2].Text);
                                    weekTimesToDepo.Add(new SimpleTime(hour, int.Parse(tm0000)));
                                    //MessageBox.Show(weekTimesToDepo[weekTimesToDepo.Count - 1].ToString());
                                    is_to_depo_finded = true;
                                }
                                else
                                {
                                    //MessageBox.Show("Не в депо: " + (cell.Cells[i, j] as Excel.Range).Characters[q, 2].Text);
                                    weekTimes.Add(new SimpleTime(hour, int.Parse(tm0000)));
                                    //MessageBox.Show(weekTimes[weekTimes.Count - 1].ToString());
                                }
                            }
                            /*}
                            catch
                            {
                                foreach (Match s in timePattern.Matches(cell[i, j].Text))
                                {
                                    weekTimes.Add(new SimpleTime(hour, int.Parse(s.Value)));
                                    //MessageBox.Show(weekTimes[weekTimes.Count - 1].ToString());
                                }
                            }*/
                        }
                    }
                    Table weekTable = new Table(weekDays, weekTimes);
                    Table weekTableToDepo = new Table(weekDays, weekTimesToDepo);

                    ObservableCollection<Table> t = new ObservableCollection<Table>();
                    t.Add(workingTable);
                    t.Add(weekTable);
                    Timetable tbl = new Timetable(null, null, TableType.table, t);
                    fullTable[k - 1].Add(tbl);

                    timetables[k - 1].Add(tbl);

                    //SW.Write(JsonConvert.SerializeObject(tbl));
                    /*SW.Write(tbl.Serialize());
                    SW.Close();*/

                    ObservableCollection<Table> t2 = new ObservableCollection<Table>();
                    t2.Add(workingTableToDepo);
                    t2.Add(weekTableToDepo);
                    Timetable tbl2 = new Timetable(null, null, TableType.table, t2);
                    fullTable_depo[k - 1].Add(tbl2);

                    timetables[k - 1].Add(tbl2);

                    //SW_depo.Write(JsonConvert.SerializeObject(tbl2));
                    /*SW_depo.Write(tbl2.Serialize());
                    SW_depo.Close();*/


                    main_way.stations_names.Add((string)(cell[startString, 3].Text));
                    if (is_to_depo_finded) way_to_depo.stations_names.Add((string)(cell[startString, 3].Text));
                    else way_from_depo.stations_names.Add((string)(cell[startString, 3].Text));
                }

                /*for (int i = 0; i < lastCell.Column; i++) //по всем колонкам
                {
                    for (int j = 0; j < lastCell.Row; j++) // по всем строкам
                    {
                        cell = ObjWorkSheet.Cells[j + 1, i + 1];
                        
                        list[i, j] = cell.Text.ToString();//считываем текст в строку
                        if (list[i, j] != string.Empty) try { MessageBox.Show(cell.Value); } catch { }
                    }
                }*/





                for (int i = 0, n = fullTable_depo[k - 1].Count; i < n; i++)
                {
                    if (fullTable_depo[k - 1][i].table[0].times.Count > 0)
                    {
                        List<int> tmp0 = new List<int>();
                        foreach (var item in fullTable_depo[k - 1][i].table[0].times)
                        {
                            tmp0.Add(ParseTime(item));
                        }
                        way_to_depo.trips_by_days[0].arrives.Add(tmp0);
                    }
                    if (fullTable_depo[k - 1][i].table[1].times.Count > 0)
                    {
                        List<int> tmp1 = new List<int>();
                        foreach (var item in fullTable_depo[k - 1][i].table[1].times)
                        {
                            tmp1.Add(ParseTime(item));
                        }
                        way_to_depo.trips_by_days[1].arrives.Add(tmp1);
                    }
                }

                List<int> tmp0_from_depo_indexes = new List<int>();
                List<int> tmp1_from_depo_indexes = new List<int>();
                for (int i = 0, n = fullTable[k - 1].Count; i < n; i++)
                {
                    if (fullTable[k - 1][i].table[0].times.Count > 0)
                    {
                        List<int> tmp0 = new List<int>();
                        List<int> tmp0_from_depo = new List<int>();
                        if (i == 0)
                        {
                            foreach (var item in fullTable[k - 1][i].table[0].times)
                            {
                                tmp0.Add(ParseTime(item));
                            }
                        }
                        else
                        {
                            for (int j = 0, m = fullTable[k - 1][i].table[0].times.Count, t = 0; j < m; j++)
                            {
                                //for (int r = 0; r < ) ;
                                int tmp_int_time = ParseTime(fullTable[k - 1][i].table[0].times[j]);
                                int max_length = main_way.trips_by_days[0].arrives[i - 1].Count;
                                //ParseTime(fullTable[k - 1][i - 1].table[0].times[j - t0])
                                if (j - t < max_length && main_way.trips_by_days[0].arrives[i - 1][j - t] <= tmp_int_time + 60 /*!!!!!!!*/ && !tmp0_from_depo_indexes.Contains(j))
                                {
                                    if (main_way.trips_by_days[0].arrives[i - 1][j - t] < tmp_int_time) tmp0.Add(tmp_int_time);
                                    else tmp0.Add(tmp_int_time + 60);//!!!!!
                                }
                                else
                                {
                                    tmp0_from_depo.Add(tmp_int_time);
                                    t++;
                                    tmp0_from_depo_indexes.Add(j);
                                }
                            }
                        }
                        main_way.trips_by_days[0].arrives.Add(tmp0);
                        if (tmp0_from_depo.Count > 0) way_from_depo.trips_by_days[0].arrives.Add(tmp0_from_depo);
                    }

                    if (fullTable[k - 1][i].table[1].times.Count > 0)
                    {
                        List<int> tmp1 = new List<int>();
                        List<int> tmp1_from_depo = new List<int>();
                        if (i == 0)
                        {
                            foreach (var item in fullTable[k - 1][i].table[1].times)
                            {
                                tmp1.Add(ParseTime(item));
                            }
                        }
                        else
                        {
                            for (int j = 0, m = fullTable[k - 1][i].table[1].times.Count, t = 0; j < m; j++)
                            {
                                //for (int r = 0; r < ) ;
                                int tmp_int_time = ParseTime(fullTable[k - 1][i].table[1].times[j]);
                                int max_length = main_way.trips_by_days[1].arrives[i - 1].Count;
                                //ParseTime(fullTable[k - 1][i - 1].table[0].times[j - t0])
                                if (j - t < max_length && main_way.trips_by_days[1].arrives[i - 1][j - t] <= tmp_int_time + 60 /*!!!!!!!*/ && !tmp1_from_depo_indexes.Contains(j))
                                {
                                    if (main_way.trips_by_days[1].arrives[i - 1][j - t] < tmp_int_time) tmp1.Add(tmp_int_time);
                                    else tmp1.Add(tmp_int_time + 60);//!!!!!
                                }
                                else
                                {
                                    tmp1_from_depo.Add(tmp_int_time);
                                    t++;
                                    tmp1_from_depo_indexes.Add(j);
                                }
                            }
                        }
                        main_way.trips_by_days[1].arrives.Add(tmp1);
                        if (tmp1_from_depo.Count > 0) way_from_depo.trips_by_days[1].arrives.Add(tmp1_from_depo);
                    }
                }


                (convertation_result_route.ways as List<dynamic>).AddRange(new dynamic[] { main_way, way_from_depo, way_to_depo });
            }



            DirectoryInfo fullTablePath = Directory.CreateDirectory(@"..\..\..\..\..\GrodnoTrolleybusTimetableConverterResults");
            string fullTableFilename = "full.trolleybus." + transportNumber + ".(" + transportName + ").json";
            string fullTableFilename_depo = "full.trolleybus." + transportNumber + ".(" + transportName + ").depo.json";

            StreamWriter fullTableSW = new StreamWriter(new FileStream(fullTablePath.FullName + @"\" + fullTableFilename, FileMode.Create, FileAccess.Write));
            StreamWriter fullTableSW_depo = new StreamWriter(new FileStream(fullTablePath.FullName + @"\" + fullTableFilename_depo, FileMode.Create, FileAccess.Write));


            StreamWriter new_fullTableSW = new StreamWriter(new FileStream(fullTablePath.FullName + @"\" + "NEW_" + fullTableFilename, FileMode.Create, FileAccess.Write));
            new_fullTableSW.Write(JsonConvert.SerializeObject(fullTable));
            new_fullTableSW.Close();


            fullTableSW.Write(Timetable.SerializeFullTable(fullTable));
            fullTableSW.Close();

            fullTableSW_depo.Write(Timetable.SerializeFullTable(fullTable_depo));
            fullTableSW_depo.Close();


            //List<Timetable>[] test = Timetable.DeserializeFullTable(Timetable.SerializeFullTable(fullTable));



            ObjWorkBook.Close(false, Type.Missing, Type.Missing); //закрыть не сохраняя
            ObjWorkExcel.Quit(); // выйти из экселя
            Marshal.ReleaseComObject(ObjWorkBook);
            Marshal.ReleaseComObject(ObjWorkExcel);
            ObjWorkBook = null;
            ObjWorkExcel = null;
            GC.Collect(); // убрать за собой
        }
        /*private void FindDepoRoutes()
        {
            ObservableCollection<SimpleTime> workDaysTableTimeNow = null, workDaysTableTimePrev = null;
            foreach (List<Timetable> timetables in timetables)
            {
                for (int i = 1; i < timetables.Count; i++)
                {
                    if ((workDaysTableTimeNow = timetables[i].table[0].times).Count != (workDaysTableTimePrev = timetables[i-1].table[0].times).Count)
                    {
                        Stack<int> workDayDepoIndexes = new Stack<int>();
                        for (int j = workDaysTableTimeNow.Count - 1, lastindex = workDaysTableTimePrev.Count - 1; j >= 1; j--)
                        {
                            //MessageBox.Show("WHILE --- checking if * >= " + workDaysTableTimeNow[j-1]);
                            while (lastindex >= 0 && (workDaysTableTimePrev[lastindex] >= workDaysTableTimeNow[j]))
                            {
                                //MessageBox.Show("checking " + workDaysTableTimePrev[lastindex] + " >= "+ workDaysTableTimeNow[j]);
                                if (workDaysTableTimePrev[lastindex] >= workDaysTableTimeNow[j])
                                {
                                    workDayDepoIndexes.Push(lastindex);
                                    MessageBox.Show(workDayDepoIndexes.Peek() + " ("+ workDaysTableTimePrev[lastindex] + ")");
                                }
                                lastindex--;
                            }
                            if (lastindex >= 0 && (workDaysTableTimePrev[lastindex] >= workDaysTableTimeNow[j-1])) lastindex--;
                        }
                    }
                }
            }
        }*/
        public static dynamic Convert(string filepath)
        {
            Converter converter = new Converter(filepath);
            //converter.FindDepoRoutes();

            return converter.convertation_result_route;
        }
    }
}
