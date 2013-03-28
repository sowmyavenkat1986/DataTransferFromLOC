using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;
using Chronozoom.Entities;

namespace ConsoleApplication1
{
    class Program
    {
        public static string outputFilePath = "C:\\Users\\sowmya\\Desktop\\testoutput.txt";
        public static string batchurl = "http://chroniclingamerica.loc.gov/batches.json";
        public static System.IO.StreamWriter file;
        public static int my_count = 1;
        public static Storage dbInst;
        public static Collection rootCollection;
        //public static int maxtestCount = 1; // This is just to reduce the number of pages parsed for test purposes.
       
        static void Main(string[] args)
        {
            Console.Write("Enter the batch number");
           // InitializeDbContext();
            Decimal batchnum = Decimal.Parse(Console.ReadLine());
            if (batchnum != 1)
            {
                batchurl = batchurl.Remove(batchurl.LastIndexOf(".json")) + "/" + batchnum + ".json";
            }
            WriteToFlatJson();
            dbInst.SaveChanges();
        }

        private static void InitializeDbContext()
        {
            dbInst = new Storage();
            //rootCollection = new Collection();
            //rootCollection.Id = Guid.Empty;
            //rootCollection.Title = "Beta Content";
            //dbInst.Collections.Add(rootCollection);
            //dbInst.SuperCollections.Remove(dbInst.SuperCollections.Find(Guid.Empty));
            //dbInst.Collections.Remove(dbInst.Collections.Find(Guid.Empty));
            //dbInst.Timelines.Remove(dbInst.Timelines.Find(Guid.Empty));
    

            dbInst.Database.Connection.ConnectionString = "Server=tcp:u0l7hb80xg.database.windows.net,1433;Database=[sowmya_loc_test];User ID=cz-wds-svenkatdb@u0l7hb80xg;Password=Yucky!@#;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;";
            //dbInst.Database.Connection.ConnectionString = "Data Source=tcp:u0l7hb80xg.database.windows.net,1433;User Id=cz-wds-svenkatdb@u0l7hb80xg;Password=Yucky!@#";



        }


        /**
         * Taken from Data Migration project
         */
         private static Decimal? convertToDecimalYear(int? day, int? month, Decimal? year, string timeUnit)
        {
            Decimal? decimalyear = null;
            if (year.HasValue)
            {
                decimalyear = (Decimal)year; 
            }
            else 
            {
                return null; //if the value of the year var is null, return null
            }

            if (timeUnit != null) //if the timeUnit is null - we still calculate decimalyear in the first if of the function and return that value
            {
                if (string.Compare(timeUnit, "ce", true) == 0) //if the timeunit is CE
                {
                    int tempmonth = 1;
                    int tempday = 1;
                    if (month.HasValue && month > 0) //if the month and day values are null, calculating decimalyear with the first day of the year
                    {
                        tempmonth = (int)month;
                        if (day.HasValue && day > 0)
                        {
                            tempday = (int)day;
                        }
                    }
                    DateTime dt = new DateTime((int)decimalyear, tempmonth, tempday);
                    decimalyear = convertToDecimalYear(dt);
                }
                else if (string.Compare(timeUnit, "bce", true) == 0)
                {
                    decimalyear *= -1; //anything that is not CE is in the negative scale. 0 CE = O Decimal Year
                }
                else if (string.Compare(timeUnit, "ka", true) == 0)
                {
                    decimalyear *= -1000;
                }
                else if (string.Compare(timeUnit, "ma", true) == 0)
                {
                    decimalyear *= -1000000;
                }
                else if (string.Compare(timeUnit, "ga", true) == 0)
                {
                    decimalyear *= -1000000000;
                }
                else if (string.Compare(timeUnit, "ta", true) == 0)
                {
                    decimalyear *= -1000000000000;
                }
                else if (string.Compare(timeUnit, "pa", true) == 0)
                {
                    decimalyear *= -1000000000000000;
                }
                else if (string.Compare(timeUnit, "ea", true) == 0)
                {
                    decimalyear *= -1000000000000000000;
                }
                else
                {
                    Console.WriteLine(timeUnit); //was never hit with the current data
                }
            }
            return decimalyear;
        }

       /**
         * Taken from Data Migration project
         */
        private static Decimal convertToDecimalYear(DateTime dateTime)
        {
            Decimal year = dateTime.Year;
            Decimal secondsInThisYear = DateTime.IsLeapYear(dateTime.Year) ? 366 * 24 * 60 * 60 : 365 * 24 * 60 * 60;
            Decimal secondsElapsedSinceYearStart = 
                (dateTime.DayOfYear - 1) * 24 * 60 * 60 + dateTime.Hour * 60 * 60 + dateTime.Minute * 60 + dateTime.Second;

            Decimal fractionalYear = secondsElapsedSinceYearStart / secondsInThisYear;

            return year + fractionalYear;
        }

        
        private static void ParseJIssues(JArray issuesArray)
        {
            //int index = 0;
            WebClient wc = new WebClient();
            if (issuesArray != null && issuesArray.Count > 0)
            {
                foreach (JObject obj in issuesArray)
                {
                   // if (index < maxtestCount) // for test purposes
                    {
                        //index++;
                        JObject temp = (JObject)obj["title"];
                        String title = (string)temp["name"] + "," + (String)obj["date_issued"];
                        string pagesurl = (string)obj["url"];
                        Stream data;
                        try
                        {
                            data = wc.OpenRead(pagesurl);
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                        StreamReader reader = new StreamReader(data);
                        string s = reader.ReadToEnd();
                        JObject jobj = JObject.Parse(s);
                        JArray pagesArray = (JArray)jobj["pages"];

                        for (int i = 0; i < pagesArray.Count; i++)
                        {
                            //Timeline j = new Timeline();
                            FlatJsonObject j = new FlatJsonObject();
                            j.Id = Guid.NewGuid();
                           // j.Threshold = "";
                           // j.Regime = "";
                           // j.Sequence = null;
                           // j.UniqueID = my_count++;
                            j.Title = title + "," + pagesArray[i]["sequence"];
                            j.FromTimeUnit = "ce"; //to be changed for a different source
                            String dateString = (string)obj["date_issued"];
                            Decimal year = Decimal.Parse(dateString.Substring(0, 4));
                            j.FromMonth = int.Parse(dateString.Substring(5, 2));
                            j.FromDay = int.Parse(dateString.Substring(8, 2));
                            j.FromYear = convertToDecimalYear(j.FromDay, j.FromMonth, year, j.FromTimeUnit);
                           // j.ToTimeUnit = j.FromTimeUnit;
                            j.ToDay = j.FromDay;
                            j.ToMonth = j.FromMonth;
                            j.ToYear = j.FromYear;
                            j.count = my_count++;
                           // j.Height = 80; //should this be 80??
                            j.URLofData = pagesurl.Remove(pagesurl.LastIndexOf(".json")) + "/seq-" + i + ".pdf";
                           // j.Exhibits = new System.Collections.ObjectModel.Collection<Exhibit>();
                           // j.Exhibits.Add(createExhibit(j, URLofData));
                           // j.Collection = rootCollection;
                            //dbInst.Timelines.Add(j);
                            //other forms available
                            String output = JsonConvert.SerializeObject(j, Formatting.Indented);
                            file.WriteLine(output);
                        }
                    }
                }

            }
        }

        //used if creating timeline objects instead of flat json objects
        private static Exhibit createExhibit(Timeline j, String URL)
        {
            Exhibit e = new Exhibit();
            e.ID = Guid.NewGuid();
            e.Title = "Exhibit -" + j.Title;
            e.Threshold = "";
            e.Regime = "";
            e.TimeUnit = j.FromTimeUnit;
            e.Day = j.FromDay;
            e.Month = j.FromMonth;
            e.Year = j.FromYear;
            e.UniqueID = my_count++;
            e.Sequence = null;
            ContentItem c = new ContentItem();
            c.ID = Guid.NewGuid();
            c.Title = "ContentItem -" + j.Title;
            c.Caption = "PDF - " +j.Title;
            c.Threshold = "";
            c.Regime = "";
            c.TimeUnit = j.FromTimeUnit;
            c.Year = j.ToYear;
            c.MediaType = "PDF";
            c.Uri = URL;
            c.MediaSource = "Library of Congress";
            c.Attribution = "Library of Congress";
            c.UniqueID = my_count++;
            c.Order = 1;
            c.HasBibliography = false;
            // Insert into db here
            dbInst.ContentItems.Add(c);
            e.ContentItems = new System.Collections.ObjectModel.Collection<ContentItem>();
            e.ContentItems.Add(c);
            dbInst.Exhibits.Add(e);
            return e;
        }
        
        private static void ParseJBatch(JArray parseArray)
        {
            //int index = 0;
            WebClient wc = new WebClient();
            if (parseArray != null && parseArray.Count > 0)
            {
                
                foreach (JObject obj in parseArray)
                {
                   // if (index < maxtestCount) // for test purposes
                    {
                        //index++;
                        string issuesurl = (string)obj["url"];
                        Stream data = wc.OpenRead(issuesurl);
                        StreamReader reader = new StreamReader(data);
                        string s = reader.ReadToEnd();
                        JObject jobj = JObject.Parse(s);
                        JArray issuesArray = (JArray)jobj["issues"];
                        ParseJIssues(issuesArray);
                    }
                }    
                
            }
        }
 
        
        public static void WriteToFlatJson()
        {
            WebClient myWebClient = new WebClient();
            file = new System.IO.StreamWriter(outputFilePath);
            //String nextBatchLink = "";
            try
            {
                Stream data = myWebClient.OpenRead(batchurl);
                StreamReader reader = new StreamReader(data);
                string s = reader.ReadToEnd();
                JObject jobj = JObject.Parse(s);
                JArray parseArray = (JArray)jobj["batches"];
                ParseJBatch(parseArray);
                //nextBatchLink = (String)jobj["next"];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }


            file.Close();
        }
    }

    class FlatJsonObject
    {
        public Guid Id;
        public Decimal count;
        public string Title;
        public string FromTimeUnit;
        public int? FromDay;
        public int? FromMonth;
        public decimal? FromYear;
        public int? ToDay;
        public int? ToMonth;
        public decimal? ToYear;
        public string URLofData; //can be a list<string> 
    }

}
