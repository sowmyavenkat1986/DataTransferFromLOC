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
        public static string outputFilePath = "C:\\Users\\sowmya\\Desktop\\LOCdata\\BatchOutput";
        public static string basebatchurl = "http://chroniclingamerica.loc.gov/batches.json";
        public static System.IO.StreamWriter file;
        public static string batchurl = basebatchurl;
        public static string output1 = "";
        public static string output2 = "";
        public static string output3 = "";
        public static string output4 = "";
        public static Decimal currBatch = 0;

        /**
         * Gets the input required from the user - batch numbers and output file path
         */
        static void Main(string[] args)
        {
            List<Decimal> batchnum = new List<decimal>();
            Console.Write("Enter the absolute path of the folder in which you want to store the results - \n");
            Console.Write("Format - C:\\Users\\sowmya\\Desktop\\LOCdata - \n(Stores the data in LOCdata folder, The empty LOCdata folder already exists) :\n");
            outputFilePath = Console.ReadLine();
            Console.Write("Enter the batch numbers to be run separated by commas - Format - 1,2,3,4 \n");
            String batchn = Console.ReadLine();
            String[] batcharr = batchn.Split(',');
            for (int i = 0; i < batcharr.Length; i++)
            {
                batchnum.Add(Decimal.Parse(batcharr[i]));
            }

            foreach (Decimal d in batchnum)
            {
                Console.WriteLine("Processing Batch " + d);
                currBatch = d;
                if (d != 1)
                {   //setting the url to be read for this batch
                    batchurl = basebatchurl.Remove(batchurl.LastIndexOf(".json")) + "/" + d + ".json";
                }
                WriteToJson();
            }
        }

        public static void WriteToJson()
        {
            WebClient myWebClient = new WebClient();
            try
            {
                output1 = outputFilePath + "\\Batch" + currBatch;

                Stream data = myWebClient.OpenRead(batchurl);
                StreamReader reader = new StreamReader(data);
                string s = reader.ReadToEnd();
                JObject jobj = JObject.Parse(s);
                RootObject ro = new RootObject();
                if ((String)jobj["next"] != null)
                {
                    ro.next = (String)jobj["next"];
                    ro.local_next = outputFilePath + "\\Batch" + (currBatch + 1) + ".json";
                }
                if ((String)jobj["previous"] != null)
                {
                    ro.previous = (String)jobj["previous"];
                    ro.local_previous = outputFilePath + "\\Batch" + (currBatch - 1) + ".json";
                }
                JArray parseArray = (JArray)jobj["batches"];
                ro.batches = ParseJBatch(parseArray); //parsing all the information in a batch
                String output = JsonConvert.SerializeObject(ro, Formatting.Indented);
                file = new System.IO.StreamWriter(outputFilePath + "\\Batch" + currBatch + ".json");
                file.WriteLine(output);
                file.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            file.Close();
        }


        /**
         * Parses the data within a batch and writes it into a file within the batch folder
         */
        private static List<Batch> ParseJBatch(JArray parseArray)
        {
            List<Batch> ret_list = new List<Batch>();
            WebClient wc = new WebClient();
            if (parseArray != null && parseArray.Count > 0)
            {
                foreach (JObject obj in parseArray)
                {
                    try
                    {
                        Batch b = new Batch();
                        Awardee a = new Awardee();
                        JObject temp = (JObject)obj["awardee"];
                        a.name = (String)temp["name"];
                        a.url = (String)temp["url"];
                        b.awardee = a;
                        b.ingested = (String)obj["ingested"];
                        JArray temp2 = (JArray)obj["lccns"];
                        b.lccns = temp2.Select(jv => (string)jv).ToList();
                        b.name = (String)obj["name"];
                        b.page_count = (int)obj["page_count"];
                        b.url = (String)obj["url"];
                        output2 = output1 + "." + (b.name);
                        b.local_url = output2 + ".json";

                        Stream data = wc.OpenRead((String)obj["url"]);
                        StreamReader reader = new StreamReader(data);
                        string s = reader.ReadToEnd();
                        JObject jobj = JObject.Parse(s);
                        JArray issuesArray = (JArray)jobj["issues"];
                        b.issues = ParseJIssues(issuesArray);

                        //writing to the awardee file inside the batch file
                        file = new System.IO.StreamWriter(output2 + ".json");
                        String output = JsonConvert.SerializeObject(b, Formatting.Indented);
                        file.WriteLine(output);
                        file.Close();
                        ret_list.Add(b);
                    }
                    catch(Exception e)
                    {
                        continue;
                    }     
                }
            }
            return ret_list;
        }

        /**
         * Parses all the issues by an awardee
         */
        private static List<Issue> ParseJIssues(JArray issuesArray)
        {
            List<Issue> ret_list = new List<Issue>();
            WebClient wc = new WebClient();
            if (issuesArray != null && issuesArray.Count > 0)
            {
                foreach (JObject obj in issuesArray)
                {
                    try
                    {
                        Stream data = wc.OpenRead((String)obj["url"]);
                        StreamReader reader = new StreamReader(data);
                        string s = reader.ReadToEnd();
                        JObject jobj = JObject.Parse(s);
                        JArray pagesArray = (JArray)jobj["pages"];
                        Issue i = new Issue();
                        Batch b = new Batch();
                        b.url = (String)(((JObject)jobj["batch"])["url"]);
                        b.name = (String)(((JObject)jobj["batch"])["name"]);
                        b.local_url = output2 + ".json";
                        i.batch = b;
                        i.date_issued = (String)obj["date_issued"];
                        i.volume = (String)jobj["volume"];
                        i.edition = (int)jobj["edition"];
                        i.number = (String)jobj["number"];
                        Title t = new Title();
                        JObject temp = (JObject)obj["title"];
                        t.name = (String)temp["name"];
                        t.url = (String)temp["url"];
                        i.title = t;
                        i.url = (String)obj["url"];
                        output3 = output2 + "." + t.name.Remove(t.name.Length - 1) + "-" + i.date_issued;
                        i.local_url = output3 + ".json";
                        i.pages = ParseJPages(pagesArray);
                        //writing to the Issue file
                        file = new System.IO.StreamWriter(output3 + ".json");
                        String output = JsonConvert.SerializeObject(i, Formatting.Indented);
                        file.WriteLine(output);
                        file.Close();
                        ret_list.Add(i);
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                }
            }
            return ret_list;
        }

        private static List<Page> ParseJPages(JArray pagesArray)
        {
            List<Page> ret_list = new List<Page>();
            WebClient wc = new WebClient();
            if (pagesArray != null && pagesArray.Count > 0)
            {
                foreach (JObject obj in pagesArray)
                {
                    Stream data;
                    try
                    {
                        data = wc.OpenRead((String)obj["url"]);
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                    StreamReader reader = new StreamReader(data);
                    string s = reader.ReadToEnd();
                    JObject jobj = JObject.Parse(s);
                    Page p = new Page();
                    p.url = (String)obj["url"];
                    Issue i = new Issue();
                    i.url = (String)(((JObject)jobj["issue"])["url"]);
                    i.date_issued = (String)(((JObject)jobj["issue"])["date_issued"]);
                    i.local_url = output3 + ".json";
                    p.issue = i;
                    p.jp2 = (String)jobj["jp2"];
                    p.ocr = (String)jobj["ocr"];
                    p.pdf = (String)jobj["pdf"];
                    p.sequence = (int)jobj["sequence"];
                    p.text = (String)jobj["text"];
                    p.title = i.title;
                    //writing the page file
                    output4 = output3 + ".Page" + p.sequence;
                    p.local_url = output4 + ".json";
                    file = new System.IO.StreamWriter(output4 + ".json");
                    String output = JsonConvert.SerializeObject(p, Formatting.Indented);
                    file.WriteLine(output);
                    file.Close();
                    ret_list.Add(p);
                }
            }
            return ret_list;
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

    }

    public class Awardee
    {
        public string url { get; set; }
        public string name { get; set; }
    }

    public class Batch
    {
        public string name { get; set; }
        public string url { get; set; }
        public string local_url { get; set; }
        public int page_count { get; set; }
        public Awardee awardee { get; set; }
        public List<string> lccns { get; set; }
        public string ingested { get; set; }
        public List<Issue> issues { get; set; }
    }

    public class RootObject
    {
        public List<Batch> batches { get; set; }
        public string next { get; set; }
        public string local_next { get; set; }
        public string previous { get; set; }
        public string local_previous { get; set; }
    }

    public class Issue
    {
        public Title title { get; set; }
        public string url { get; set; }
        public string local_url { get; set; }
        public string date_issued { get; set; }
        public string number { get; set; }
        public Batch batch { get; set; }
        public string volume { get; set; }
        public int edition { get; set; }
        public List<Page> pages { get; set; }
    }

    public class Title
    {
        public string url { get; set; }
        public string name { get; set; }
    }

    public class Page
    {
        public string url { get; set; }
        public string local_url { get; set; }
        public string jp2 { get; set; }
        public int sequence { get; set; }
        public string text { get; set; }
        public Title title { get; set; }
        public string pdf { get; set; }
        public string ocr { get; set; }
        public Issue issue { get; set; }
    }
}



  