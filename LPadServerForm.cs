using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace LPadServer
{
    public partial class lPadServerForm : Form
    {
        Configuration configuration;
        string xmlfileName = @"\LPad\LPadServer.xml";
        DateTime lastWrite;
        //        int nTables = 16;
        int nPots = 8;

        public lPadServerForm()
        {
            InitializeComponent();
            // check to see if there is a current version running
            System.Diagnostics.Process[] monitor = System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location));
            if (monitor.Count() > 1)
                // kill the other process
                monitor[0].Kill();
        }

        private void LPadServerForm_Load(object sender, EventArgs e)
        {
            if (!File.Exists(xmlfileName))
            {
                addStatus(@"Cannot Find configuration file \LPad\lPadServer.xml");
                return;
            }

            configuration = new Configuration(xmlfileName);
            if (configuration.Experiments.Count == 0)
            {
                addStatus(@"Error reading configuration file \LPad\lPadServer.xml");
                return;
            }

            Text = "LPad Server - ";
            foreach (var exp in configuration.Experiments)
            {
                addStatus("Experiment : " + exp.Name);
                if (Text[Text.Length - 1] != ' ')
                    Text += ", ";
                Text += exp.Name;

            }

            fileSystemWatcher.Path = configuration.dataDirectory;
            fileSystemWatcher.Filter = Path.GetFileName("*.tmp");

            lastWrite = DateTime.Now;

            // when server starts on a reboot send a message
            string[] args = Environment.GetCommandLineArgs();
            if (args.Count() > 1)
                sendMessage("Lpad Server started.");
        }

        private void fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {

            // When the file is changed, read the information and store
            //  to stop multiple firing
            try
            {
                fileSystemWatcher.EnableRaisingEvents = false;
                addStatus("Incoming data");

                Thread.Sleep(2000);  // wait until file is complete

                foreach (var exp in configuration.Experiments)
                {
                    if (exp.Name == Path.GetFileNameWithoutExtension(e.FullPath))
                    {
                        processData(exp, e.FullPath);
                    }
                }
            }
            finally
            {
                Thread.Sleep(1000);  // to stop multiple firing

                fileSystemWatcher.EnableRaisingEvents = true;
            }
        }

        private void processData(Experiment exp, string dataFileName)
        {
            //find the experiment the tmp file is attached to - if none then don't do anything
            // when a new tmp file appears, append to the csv file,  add to nc file and remove file
            // copy new file to web dir for download
            // append to data file
            string[] lines = File.ReadAllLines(dataFileName);
            File.AppendAllLines(exp.csvFileName, lines);
            addStatus("Data Appended to : " + exp.csvFileName);

            // add data to the .nc file
            if (importDataFile(lines, exp) == 0)
                addStatus("Data Loaded into : " + exp.ncFileName);
            else
                addStatus("Problem loading NetCdf file : " + exp.ncFileName);

            // remove file
            File.Delete(exp.tmpFileName);
            if (File.Exists(Path.Combine(exp.outputDirectory, ".lock")))
                File.Delete(Path.Combine(exp.outputDirectory, ".lock"));
            lastWrite = DateTime.Now;

            SmoothData smooth = new SmoothData(exp);
            smooth.calcWaterUse();
            smooth.smoothAccData();
            smooth.getDailyWaterUse(exp);
            addStatus("Daily Water Use Calculated : " + exp.ncFileName);

            smooth.writeToFile(exp.outputDirectory + "Data\\LPadDataWU.csv", "Smoothed");

            smooth.writeToFile(exp.outputDirectory + "Data\\MetData.csv", "MetData");
            // copy the data for download
            File.Copy(exp.csvFileName, exp.outputDirectory + "Data\\LPadData.csv", true);
            addStatus("Completed processing new data for: " + exp.ncFileName);

        }

        private void lPadTimer_Tick(object sender, EventArgs e)
        {
            // periodically check the age of the file
            // if the age is over 45 minutes, send an email
            // if a file called .lock exists, do nothing
            addStatus("Checking LPad data");
            foreach (var exp in configuration.Experiments)
            {
                if (File.Exists(exp.outputDirectory + ".lock"))
                {
                    addStatus("Experiment " + exp.Name + " data locked");
                    continue;
                }
                // get the age of the file
                FileInfo file = new FileInfo(exp.csvFileName);
                TimeSpan age = DateTime.Now - file.LastWriteTime;
                int fileAge = Convert.ToInt32(age.TotalMinutes);


                if (fileAge > 45 && notMonday())
                {
                    string msg = "Experiment " + exp.Name + " not sending data - Last data " + fileAge + " minute" + ((fileAge == 1) ? " ago" : "s ago");
                    sendMessage(msg);
                    addStatus(msg);
                    // set the lock
                    File.WriteAllText(exp.outputDirectory + ".lock", "Lock File");
                }
                else
                {
                    if (fileAge > 45)  // must be monday
                        addStatus("Experiment " + exp.Name + " needs resetting - Monday - arrived " + fileAge + " minute" + ((fileAge == 1) ? " ago" : "s ago"));
                    else
                        addStatus("Experiment " + exp.Name + " data OK - arrived " + fileAge + " minute" + ((fileAge == 1) ? " ago" : "s ago"));
                }
            }
        }

        private bool notMonday()
        {
            // if monday and the time is < 9:00 don't send a message
            DayOfWeek today = DateTime.Today.DayOfWeek;
            TimeSpan nineAM = new TimeSpan(9, 0, 0); //9 o'clock
            TimeSpan now = DateTime.Now.TimeOfDay;

            if (today == DayOfWeek.Monday && now < nineAM)
            {
                return false;
            }
            return true;
        }

        private void sendMessage(string status)
        {
            // set up message address and messages and send
            string msg = status + "  " + DateTime.Now.ToString();
            //GMailSmtpSend.SendMessage("Lysimeter Monitor Message", msg, "greg.b.mclean@gmail.com", "Lysimeter Monitor", "k.deifel@uq.edu.au");
            GMailSmtpSend.SendMessage("Lysimeter Monitor Message", msg, "greberry@gmail.com", "Lysimeter Monitor", "greberry@gmail.com");
            addStatus("Message Sent");
        }

        private void eMailTestButton_Click(object sender, EventArgs e)
        {
            sendMessage("eMail Test from LPad Server");
        }

        private void createNcButton_Click(object sender, EventArgs e)
        {
            // create an .nc file
            // if one already exists exit
            foreach (var exp in configuration.Experiments)
            {
                if (File.Exists(exp.ncFileName))
                {
                    MessageBox.Show("Can't do that! " + exp.ncFileName + " already exists. skipped.");
                    continue;
                }
                if (createNC(exp.ncFileName, exp.NumberOfTables, nPots * exp.NumberOfTables, exp.Name) == 0) //check with GREG
                    addStatus("NC File created : " + exp.ncFileName);
                else
                    addStatus("Problem creating NC File ");
            }
        }

        private int createNC(string ncFileName, int nTables, int nPots, string ExperimentName)
        {
            //Create a NetCDF file for lysimeter data storage
            // this is date, 128 pots, 3 met, 6 temperature

            int ncid = 1;
            int status = NetCDF.nc_create(ncFileName, (int)NetCDF.cmode.NC_CLOBBER, ref ncid);

            // add dimensions
            int[] dimensionIDs = new int[5];
            status = NetCDF.nc_def_dim(ncid, "Time", 0, ref dimensionIDs[0]);
            status = NetCDF.nc_def_dim(ncid, "PotNo", nPots, ref dimensionIDs[1]);
            status = NetCDF.nc_def_dim(ncid, "TableNo", nTables, ref dimensionIDs[2]);
            status = NetCDF.nc_def_dim(ncid, "Met", 3, ref dimensionIDs[3]);        // humidity, temperature and radiation inside
            status = NetCDF.nc_def_dim(ncid, "MetExt", 3, ref dimensionIDs[3]);        // humidity, temperature and radiation externally
                                                                                       //         status = NetCDF.nc_def_dim(ncid, "RTD", 6, ref dimensionIDs[4]);        // 6 temperature devices

            // add variables
            int _varID = 0;
            status = NetCDF.nc_def_var(ncid, "Weight", NetCDF.nc_type.NC_FLOAT, 2, dimensionIDs, ref _varID);
            status = NetCDF.nc_def_var(ncid, "WaterUse", NetCDF.nc_type.NC_FLOAT, 2, dimensionIDs, ref _varID);

            //         int[] tempDim = new int[] { dimensionIDs[0], dimensionIDs[4] };
            //         status = NetCDF.nc_def_var(ncid, "Temperature", NetCDF.nc_type.NC_FLOAT, 2, tempDim, ref _varID);

            status = NetCDF.nc_def_var(ncid, "Time", NetCDF.nc_type.NC_INT, 1, dimensionIDs, ref _varID);

            int[] metDim = new int[] { dimensionIDs[0], dimensionIDs[3] };
            status = NetCDF.nc_def_var(ncid, "MetData", NetCDF.nc_type.NC_FLOAT, 2, metDim, ref _varID);

            int[] metExtDim = new int[] { dimensionIDs[0], dimensionIDs[3] };
            status = NetCDF.nc_def_var(ncid, "MetDataExt", NetCDF.nc_type.NC_FLOAT, 2, metDim, ref _varID);

            // add attributes
            string attr = "LPAD_Experiment_Data";
            string attrValue = ExperimentName;
            status = NetCDF.nc_put_att_text(ncid, NetCDF.NC_GLOBAL, attr, attrValue.Length, attrValue);

            // end the file
            status = NetCDF.nc_enddef(ncid);
            status = NetCDF.nc_close(ncid);

            return status;



        }

        private int importDataFile(string[] lines, Experiment exp)
        {
            //         int nPots = 128;
            //         int nRTDs = 6;
            int nPots = exp.NumberOfTables * 8;

            int nRecords = lines.Count();
            double[] wts = new double[nRecords * nPots];    /* array to hold weights */
            //         double[] temps = new double[nRecords * nRTDs];    /* array to hold temperatures */

            int[] times = new int[nRecords];              // array to hold time
            double[] met = new double[nRecords * 3];      /* array to hold environment */
            double[] metExt = new double[nRecords * 3];      /* array to hold external environment */

            for (int i = 0; i < nRecords; i++)
            {
                // read a record
                string[] vals = lines[i].Split(',');
                times[i] = timeStamp(vals[0]);   // time
                int nData = vals.Count();

                for (int j = 0; j < nPots; j++) // weights
                    wts[i * nPots + j] = Convert.ToDouble(vals[j + 1]);
                for (int j = 0; j < 3; j++)   // met data
                    met[i * 3 + j] = Convert.ToDouble(vals[j + nPots + 1]);
                for (int j = 0; j < 3; j++)   // External met data
                   metExt[i * 3 + j] = Convert.ToDouble(vals[j + nPots + 4]);

                //int rtdStart = nData - 6;
                //for (int j = 0; j < nRTDs; j++) // RTD temperatures - last 6
                //   temps[i * nRTDs + j] = Convert.ToDouble(vals[j + rtdStart]);// extra temperature in some files so 4 or 5 TODO

            }

            // load the netCDF file
            int variableID = -1;
            int ncid = 0;
            int status = NetCDF.nc_open(exp.ncFileName, NetCDF.cmode.NC_WRITE.GetHashCode(), ref ncid);
            int dimensionID = 0;                        /* variable ID */

            status = NetCDF.nc_inq_varid(ncid, "Time", ref variableID);
            int currentRecords = 0;
            status = NetCDF.nc_inq_dimid(ncid, "Time", ref dimensionID);
            status = NetCDF.nc_inq_dimlen(ncid, dimensionID, ref currentRecords);
            // so we know how many current records

            int[] start = new int[] { currentRecords }; /* start at first value */
            int[] count = new int[] { nRecords };
            status = NetCDF.nc_put_vara_int(ncid, variableID, start, count, times);

            start = new int[] { currentRecords, 0 }; /* start at first value */
            count = new int[] { nRecords, nPots };
            status = NetCDF.nc_inq_varid(ncid, "Weight", ref variableID);
            status = NetCDF.nc_put_vara_double(ncid, variableID, start, count, wts);

            //start = new int[] { currentRecords, 0 }; /* start at first value */
            //count = new int[] { nRecords, nRTDs };
            //status = NetCDF.nc_inq_varid(ncid, "Temperature", ref variableID);
            //status = NetCDF.nc_put_vara_double(ncid, variableID, start, count, temps);

            count = new int[] { nRecords, 3 };
            status = NetCDF.nc_inq_varid(ncid, "MetData", ref variableID);
            status = NetCDF.nc_put_vara_double(ncid, variableID, start, count, met);

            //count = new int[] { nRecords, 3 };
            status = NetCDF.nc_inq_varid(ncid, "MetDataExt", ref variableID);
            status = NetCDF.nc_put_vara_double(ncid, variableID, start, count, metExt);
            NetCDF.nc_sync(ncid);

            status = NetCDF.nc_close(ncid);
            return status;
        }

        private void loadAllNcbutton_Click(object sender, EventArgs e)
        {
            foreach (var exp in configuration.Experiments)
            {
                string ncFileName = exp.ncFileName;
                string tmpFileName = exp.tmpFileName;
                tmpFileName = exp.csvFileName;  // load all data
                if (!File.Exists(tmpFileName)) continue;
                string[] lines = File.ReadAllLines(tmpFileName);
                importDataFile(lines, exp);

            }
        }


    }



}
