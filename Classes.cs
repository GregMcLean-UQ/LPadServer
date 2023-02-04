using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;

namespace LPadServer
{
    class GMailSmtpSend
    {
        public static void SendMessage(string subject, string messageBody, string fromAddress, string fromName, string toAddress)
        {
            // configure the client and send the message
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
            client.EnableSsl = true;
            MailAddress from = new MailAddress(fromAddress, fromName);
            MailAddress to = new MailAddress(toAddress);
            MailMessage message = new MailMessage(from, to);
            message.Body = messageBody;
            message.Subject = subject;

            // establish credentials
            NetworkCredential gMailID = new NetworkCredential(fromAddress, "Ballymore", "");
            // establish credentials

            client.Credentials = gMailID;
            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }
    }
    public class Experiment
    {
        public string Name;
        public string ncFile;
        public string outputDirectory;
        public string csvFileName, tmpFileName, ncFileName;
        public int NumberOfTables;

        public Experiment(XElement xmlEl, string dataPath)
        {
            Name = xmlEl.Element("Name").Value;
            ncFile = xmlEl.Element("NCFile").Value;
            outputDirectory = xmlEl.Element("OutputDirectory").Value;

            NumberOfTables = Convert.ToInt32(xmlEl.Element("Tables").Value);

            csvFileName = dataPath + Name + ".csv";
            tmpFileName = dataPath + Name + ".tmp";
            ncFileName = dataPath + Name + ".nc";
        }
    }

    public class Configuration
    {
        public string dataDirectory;
        public List<Experiment> Experiments = new List<Experiment>();

        public Configuration(string xmlfileName)
        {
            try
            {

                // read the file and load the data structures
                XDocument xmlDoc = XDocument.Load(xmlfileName);
                XElement xmlEl = xmlDoc.Element("Configuration");
                dataDirectory = xmlEl.Element("DataDirectory").Value;

                foreach (var element in xmlEl.Elements())
                {
                    if (element.Name == "Experiment")
                    {
                        Experiments.Add(new Experiment(element, dataDirectory));
                    }
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
                //experiment = null;
            }
        }
    }

    public class Weight
    {
        public DateTime time;
        public double wt;
        public Weight(DateTime _t, double _wt)
        {
            time = _t;
            wt = _wt;
        }
    }

    public class DataPot
    {
        public List<Weight> weights;
    }

    public class DataCJC
    {
        public DateTime time;
        public double temperature;
        public DataCJC(DateTime _t, double _temp)
        {
            time = _t;
            temperature = _temp;
        }
    }


    public class DataTable
    {
        public List<DataPot> pots;
        public List<DataCJC> temperatures;

    }

    public class MovingAverage
    {
        Queue<double> values;
        int size;
        public MovingAverage(int _size)
        {
            size = _size;
            values = new Queue<double>();
        }
        public double Value()
        {
            return values.Average();
        }
        public void Add(double value)
        {
            values.Enqueue(value);
            if (values.Count > size)
                values.Dequeue();
        }
        public void Clear()
        {
            values.Clear();
        }
        public int Count()
        {
            return values.Count;
        }
    }


    public class SmoothData
    {
        // read data fron the netcdf then calculate water use
        // smooth and store smoothed wateruse
        int[] times;
        double[,] weights;
        double[,] waterUse;
        double[,] smoothAccWU;
        double[,] accWaterUse;
        //double[,] smoothDailyWU;

        //   int[] hourTimes;
        //
        int nRecs;
        //      int nPots = 128;
        int nPots = 16;
        string ncFileName;

        public SmoothData(Experiment exp)
        {
            ncFileName = exp.ncFileName;
            nPots = exp.NumberOfTables * 8;
            int ncid = 0;
            int status = NetCDF.nc_open(ncFileName, NetCDF.cmode.NC_SHARE.GetHashCode(), ref ncid);
            int dimensionID = 0;                        /* variable ID */
            int variableID = -1;
            nRecs = 0;

            status = NetCDF.nc_inq_dimid(ncid, "Weight", ref dimensionID);
            status = NetCDF.nc_inq_dimlen(ncid, dimensionID, ref nRecs); // so we know how many records

            double[] wts = new double[nRecs * nPots];    /* array to hold weights */
            int[] start = new int[] { 0, 0 }; /* start at first value */
            int[] count = new int[] { nRecs, nPots };
            status = NetCDF.nc_inq_varid(ncid, "Weight", ref variableID);
            status = NetCDF.nc_get_vara_double(ncid, variableID, start, count, wts);

            times = new int[nRecs];
            status = NetCDF.nc_inq_varid(ncid, "Time", ref variableID);
            start = new int[] { 0 }; /* start at first value */
            count = new int[] { nRecs };
            status = NetCDF.nc_get_vara_int(ncid, variableID, start, count, times);

            status = NetCDF.nc_close(ncid);


            //move 1 dim into 2 dim array         

            weights = twoDim(wts, nRecs, nPots);

        }

        private double[,] twoDim(double[] oneDim, int d1, int d2)
        {
            double[,] twoD = new double[d1, d2];
            for (int i = 0; i < d1; i++)
                for (int j = 0; j < d2; j++)
                    twoD[i, j] = oneDim[i * nPots + j];
            return twoD;
        }

        private double[] oneDim(double[,] twoDim, int d1, int d2)
        {
            double[] oneD = new double[d1 * d2];    /* array to hold data */
            for (int i = 0; i < d1; i++)
                for (int j = 0; j < d2; j++)
                    oneD[i * nPots + j] = twoDim[i, j];
            return oneD;
        }

        private void reLoad(string variableName, double[] data)
        {
            // load calculated value into the netCdf file
            int ncid = 0;
            int status = NetCDF.nc_open(ncFileName, NetCDF.cmode.NC_WRITE.GetHashCode(), ref ncid);
            //int dimensionID = 0;                        /* variable ID */
            int variableID = -1;

            int[] start = new int[] { 0, 0 }; /* start at first value */
            int[] count = new int[] { nRecs, nPots };
            status = NetCDF.nc_inq_varid(ncid, variableName, ref variableID);
            status = NetCDF.nc_put_vara_double(ncid, variableID, start, count, data);

            status = NetCDF.nc_close(ncid);
        }

        public void calcWaterUse()
        {
            // calculate water use for each pot 
            // calc delta for each time unit. if > 100 assume watering and use running average
            // maintain running average

            waterUse = new double[nRecs, nPots];
            MovingAverage mvAvg = new MovingAverage(5);            // data[num recs][nPots]
            double irrigWt = 40;  // minimum to detect irrigation
            for (int pot = 0; pot < nPots; pot++)
            {
                waterUse[0, pot] = 0.0;
                for (int rec = 1; rec < nRecs; rec++)
                {
                    double wu = 0;
                    double deltaWt = weights[rec, pot] - weights[rec - 1, pot]; // will be -ve

                    if (Math.Abs(deltaWt) > irrigWt)      // irrigation has occurred - use mvAvg for this timestep
                    {
                        if (mvAvg.Count() == 0) wu = 0.0;
                        else
                            wu = mvAvg.Value();
                    }
                    else
                        wu = -deltaWt;
                    waterUse[rec, pot] = wu;
                    mvAvg.Add(wu);
                }
                mvAvg.Clear();    // clear for new pot
            }

            reLoad("WaterUse", oneDim(waterUse, nRecs, nPots));
        }

        public void smoothAccData()
        {
            // calculated accumulated water use and smooth
            smoothAccWU = new double[nRecs, nPots];
            accWaterUse = new double[nRecs, nPots];
            int range = 5; // Number of data points each side to sample.
            double decay = 0.8; // [0.0 - 1.0] How slowly to decay from raw value.


            // accumulate
            for (int pot = 0; pot < nPots; pot++)
            {
                accWaterUse[0, pot] = waterUse[0, pot];
                for (int rec = 1; rec < nRecs; rec++)
                {
                    accWaterUse[rec, pot] = accWaterUse[rec - 1, pot] + waterUse[rec, pot];
                }

                double[] noisy = new double[nRecs];
                for (int rec = 0; rec < nRecs; rec++)
                    noisy[rec] = accWaterUse[rec, pot];
                double[] clean = CleanData(noisy, range, decay);
                for (int rec = 0; rec < nRecs; rec++)
                    smoothAccWU[rec, pot] = clean[rec];
            }
        }

        public void getDailyWaterUse(Experiment exp)
        {
            // sum up water use for each hour, at the hour from smoothed accumulated waterrt use
            // ignore first period
            // first calculate interpolated accWu at the hour then calculate differences
            List<int> hours = new List<int>();
            List<List<double>> interpAccWu = new List<List<double>>();
            int nextHr = times[0] + 60 - (times[0] % 60);      // net hour after start - minutes after epoch
                                                               //hours.Add(nextHr);
            for (int rec = 1; rec < nRecs; rec++)
            {
                if (times[rec] > nextHr)
                {
                    int diff = times[rec] - nextHr;
                    hours.Add(nextHr);
                    nextHr += 60;
                    List<double> interp = new List<double>();
                    for (int pot = 0; pot < nPots; pot++)
                    {
                        interp.Add(smoothAccWU[rec, pot] + (smoothAccWU[rec, pot] - smoothAccWU[rec - 1, pot]) * diff / 60.0);
                    }
                    interpAccWu.Add(interp);
                }
            }

            // disaggregate
            int nPoints = interpAccWu.Count;
            double[,] hourlyWU = new double[nPoints, nPots];
            for (int i = 0; i < nPots; i++)
                hourlyWU[0, i] = 0.0;
            for (int point = 1; point < nPoints; point++)
                for (int pot = 0; pot < nPots; pot++)
                    hourlyWU[point, pot] = interpAccWu[point][pot] - interpAccWu[point - 1][pot];

            // printout
            File.Delete(exp.outputDirectory + "data\\hourlyAcc.csv");
            File.Delete(exp.outputDirectory + "data\\hourly.csv");
            for (int rec = 0; rec < interpAccWu.Count; rec++)
            {
                DateTime dt = new DateTime(2010, 1, 1).AddMinutes(hours[rec]);
                string line = dt.ToString("d/MM/yyyy h:mm:ss tt");
                for (int pot = 0; pot < nPots; pot++)
                {
                    line += "," + interpAccWu[rec][pot].ToString("F2");
                }
                File.AppendAllText(exp.outputDirectory + "data\\hourlyAcc.csv", line + Environment.NewLine);
            }

            for (int rec = 0; rec < interpAccWu.Count; rec++)
            {
                DateTime dt = new DateTime(2010, 1, 1).AddMinutes(hours[rec]);
                string line = dt.ToString("d/MM/yyyy h:mm:ss tt");
                for (int pot = 0; pot < nPots; pot++)
                {
                    line += "," + hourlyWU[rec, pot].ToString("F2");
                }
                File.AppendAllText(exp.outputDirectory + "data\\hourly.csv", line + Environment.NewLine);
            }
            File.Copy(exp.outputDirectory + "data\\hourly.csv", exp.outputDirectory + "data\\LpadDataHourly.csv", true);

        }


        public void writeToFile(string outFileName, string variableName)
        {
            // create a file of one of the variables
            // 30/09/2014 4:48:00 PM,4.13,1.7,0,5.07,6.01,57100.

            File.Delete(outFileName);
            // convert timestamp to date time
            for (int rec = 0; rec < nRecs; rec++)
            {
                DateTime dt = new DateTime(2010, 1, 1).AddMinutes(times[rec]);
                string line = dt.ToString("d/MM/yyyy h:mm:ss tt");
                for (int pot = 0; pot < nPots; pot++)
                {
                    if (variableName == "Weights")
                        line += "," + weights[rec, pot].ToString("F2");
                    else if (variableName == "WaterUse")
                        line += "," + waterUse[rec, pot].ToString("F2");
                    else if (variableName == "Smoothed")
                        line += "," + smoothAccWU[rec, pot].ToString("F2");
                    else if (variableName == "Accum")
                        line += "," + accWaterUse[rec, pot].ToString("F2");
                    //   else if (variableName == "Daily")
                    //     line += "," + smoothDailyWU[rec, pot].ToString("F2");
                }
                File.AppendAllText(outFileName, line + Environment.NewLine);
            }

        }

        static private double[] CleanData(double[] noisy, int range, double decay)
        {
            double[] clean = new double[noisy.Length];
            double[] coefficients = Coefficients(range, decay);

            // Calculate divisor value.
            double divisor = 0;
            for (int i = -range; i <= range; i++)
                divisor += coefficients[Math.Abs(i)];

            // Clean main data.
            for (int i = range; i < clean.Length - range; i++)
            {
                double temp = 0;
                for (int j = -range; j <= range; j++)
                    temp += noisy[i + j] * coefficients[Math.Abs(j)];
                clean[i] = temp / divisor;
            }

            // Calculate leading and trailing slopes.
            double leadSum = 0;
            double trailSum = 0;
            int leadRef = range;
            int trailRef = Math.Max(0, clean.Length - range - 1);
            for (int i = 1; i <= range; i++)
            {
                leadSum += (clean[leadRef] - clean[leadRef + i]) / i;
                trailSum += (clean[trailRef] - clean[trailRef - i]) / i;
            }
            double leadSlope = leadSum / range;
            double trailSlope = trailSum / range;

            // Clean edges.
            for (int i = 1; i <= range; i++)
            {
                clean[leadRef - i] = clean[leadRef] + leadSlope * i;
                clean[trailRef + i] = clean[trailRef] + trailSlope * i;
            }
            return clean;
        }
        static private double[] Coefficients(int range, double decay)
        {
            // Precalculate coefficients.
            double[] coefficients = new double[range + 1];
            for (int i = 0; i <= range; i++)
                coefficients[i] = Math.Pow(decay, i);
            return coefficients;
        }

    }


    public partial class lPadServerForm : Form
    {

        private int timeStamp(string strTime)
        {
            // convert string time to minutes from 1/1/2010
            DateTime dt = DateTime.Parse(strTime);
            TimeSpan t = (dt - new DateTime(2010, 1, 1));
            return (int)t.TotalMinutes;
        }

        private void addStatus(string message)
        {
            string newMessage = DateTime.Now.ToString() + " : " + message;
            File.AppendAllText(@"\LPad\LPadDataMonitor.log", newMessage + Environment.NewLine);
            statusListBox.Items.Add(newMessage);
            if (statusListBox.Items.Count > 25)
                statusListBox.Items.RemoveAt(0);
            statusListBox.Refresh();
        }

        public bool loadData(List<DataTable> dataTables, Experiment exp)
        {
            if (!File.Exists(exp.csvFileName))
            {
                addStatus(@"Cannot find " + exp.csvFileName);
                return false;
            }
            try
            {
                string[] lines = File.ReadAllLines(exp.csvFileName);

                int nTables = 2;
                // 22/09/2010 1:56:07 PM,69255.3,64079,68218.3,61192.3,65349,63770.6,68001.1,66934.1,  etc
                // last 3 values are humidity, temperature and solar
                // now has temperature for each table

                int nPots = 8;
                char[] charSeparators = { ',' };
                string[] parseline = lines[0].Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
                //			int nTables = (parseline.Count() - 4) / nPots;
                //int nTables = 16;// (parseline.Count() - 5) / nPots;

                //dataTables = new List<DataTable>();
                //         environment = new DataEnvironment();
                //         environment.envs = new List<EnvReading>();

                for (int i = 0; i < nTables; i++)
                {
                    DataTable t = new DataTable();
                    t.pots = new List<DataPot>();
                    for (int j = 0; j < nPots; j++)
                    {
                        DataPot p = new DataPot();
                        p.weights = new List<Weight>();
                        t.pots.Add(p);
                    }
                    dataTables.Add(t);
                }

                foreach (string line in lines)
                {
                    // 22/09/2010 1:56:07 PM,69255.3,64079,68218.3,61192.3,65349,63770.6,68001.1,66934.1,  etc
                    // last 3 values are humidity, temperature and solar
                    // 4 is test weight
                    // LAST nTables are the 7018 temperatures
                    parseline = line.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
                    if (parseline.Count() < 9)
                        continue;
                    DateTime aTime = DateTime.Parse(parseline[0]);

                    // 
                    //int nReadings = parseline.Count();
                    //for (int i = 1; i < nReadings - 3; i++)
                    for (int i = 1; i < nPots * nTables + 1; i++)
                    {
                        int potNo = (i - 1) % nPots;
                        int tableNo = (i - 1) / 8;
                        Weight wt = new Weight(aTime, Convert.ToDouble(parseline[i]));
                        dataTables[tableNo].pots[potNo].weights.Add(wt);
                    }
                }
            }
            catch (Exception ex)
            {
                addStatus(@"Problem loading \LPad\lPadServer.xml " + ex.Message);
                return false;
            }
            return true;
        }

        public void addSeries(Experiment exp, Chart chart, int tableNo, int potNo, List<DataTable> dataTables)
        {

            String seriesName = "Table " + (tableNo + 1).ToString() + " Pot " + (potNo + 1).ToString();
            Series newSeries = chart.Series.Add(seriesName);
            newSeries.ChartType = SeriesChartType.Line;
            foreach (Weight wt in dataTables[tableNo].pots[potNo].weights)
            {
             //   if (wt.wt >= exp.minChartValue && wt.wt <= exp.maxChartValue)
                    newSeries.Points.AddXY(wt.time, wt.wt);
            }
            chart.Invalidate();
        }


    }


}
