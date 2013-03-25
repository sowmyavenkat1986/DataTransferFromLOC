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

namespace ConsoleApplication1
{
    class Program
    {
        public static string outputFilePath = "C:\\Users\\sowmya\\Desktop\\testoutput.txt";
        public static System.IO.StreamWriter file;
       
        static void Main(string[] args)
        {
            WriteToFlatJson();   
        }


        private static void ParseJPages(JArray pagesArray)
        {
            //to parse each page of an issue. 
            //Here we write into a json object of type timeline or with similar attributes
           //Use a JSON serializer here instead of trying to create manual objects. 
            WebClient wc = new WebClient();
            if (pagesArray != null && pagesArray.Count > 0)
            {

                foreach (JObject obj in pagesArray)
                {
                    string pageurl = (string)obj["url"];
                    Stream data = wc.OpenRead(pageurl);
                    StreamReader reader = new StreamReader(data);
                    string s = reader.ReadToEnd();
                    JObject jobj = JObject.Parse(s);
                    FlatJsonObject j = new FlatJsonObject();
                    j.Id = Guid.NewGuid();
                    JObject temp = (JObject)jobj["title"];
                    JObject temp2 = (JObject)jobj["issue"];
                    j.Title = (string)temp["name"] + "," + (string)temp2["date_issued"] + "," + jobj["sequence"];
                    j.FromTimeUnit = "ce"; //to be changed for a different source
                    String dateString = (string)temp2["date_issued"];
                    Decimal year = Decimal.Parse(dateString.Substring(0,4));
                    j.FromMonth = int.Parse(dateString.Substring(5,2));
                    j.FromDay = int.Parse(dateString.Substring(8, 2));
                    j.FromYear = convertToDecimalYear(j.FromDay, j.FromMonth, year, j.FromTimeUnit);
                    j.ToDay = j.FromDay;
                    j.ToMonth = j.FromMonth;
                    j.ToYear = j.FromYear;
                    j.URLofData = (String)jobj["pdf"]; //other forms available
                    String output = JsonConvert.SerializeObject(j, Formatting.Indented);
                    file.WriteLine(output);
                }

            }
            JObject x = new JObject();


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
            WebClient wc = new WebClient();
            if (issuesArray != null && issuesArray.Count > 0)
            {

                foreach (JObject obj in issuesArray)
                {
                    string pagesurl = (string)obj["url"];
                    Stream data = wc.OpenRead(pagesurl);
                    StreamReader reader = new StreamReader(data);
                    string s = reader.ReadToEnd();
                    JObject jobj = JObject.Parse(s);
                    JArray pagesArray = (JArray)jobj["pages"];
                    ParseJPages(pagesArray);
                }

            }
        }

        
        private static void ParseJBatch(JArray parseArray)
        {
            WebClient wc = new WebClient();
            if (parseArray != null && parseArray.Count > 0)
            {
                
                foreach (JObject obj in parseArray)
                    {
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
 
        
        public static void WriteToFlatJson()
        {
            WebClient myWebClient = new WebClient();
            file = new System.IO.StreamWriter(outputFilePath);
            String nextBatchLink = "";
            do
            {
                Stream data = myWebClient.OpenRead("http://chroniclingamerica.loc.gov/batches.json");
                StreamReader reader = new StreamReader(data);
                string s = reader.ReadToEnd();
                JObject jobj = JObject.Parse(s);
                JArray parseArray = (JArray)jobj["batches"];
                ParseJBatch(parseArray);
                nextBatchLink = (String)jobj["next"];
            } while (nextBatchLink.Length != 0);

            file.Close();
        }
    }

    class FlatJsonObject
    {
        public Guid Id;
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
