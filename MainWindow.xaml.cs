using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.IO;
using System.ComponentModel;

namespace PiezoTestSetup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker Mainworker = new BackgroundWorker();
        BackgroundWorker Stageworker = new BackgroundWorker();
        MessageChannel msgchnl;
        mrj4commander mr;
        SerialPort port;
        SerialPort Voltport;
        public static double loadv=0.0;
        public static string volt;
        public static string voltread = "";
        public static string load = "";
        string datafile = "";
        List<string> readBuffer = new List<string>();
        public bool sine = false;
        //public static double[] force = new double[n];
        //public static double[] delx = new double[n];
        public MainWindow()
        {
            Mainworker.WorkerReportsProgress = true;
            Mainworker.DoWork += Mainworker_DoWork;
            Mainworker.ProgressChanged += Mainworker_ProgressChanged;

            Stageworker.WorkerReportsProgress = true;
            Stageworker.DoWork += Stageworker_DoWork;
            Stageworker.ProgressChanged += Stageworker_ProgressChanged;


            msgchnl = new MessageChannel();           
            mr = new mrj4commander(msgchnl);
            InitializeComponent();            
            msgchnl.msgChanged += new MessageEventHandler(MessageChanged);
            msgchnl.OnChanged(EventArgs.Empty);
            jogmode.IsEnabled = false;
            upbutton.IsEnabled = false;
            downbutton.IsEnabled = false;
            posmode.IsEnabled = false;
            posrun.IsEnabled = false;
            //distlabel.IsEnabled = false;
            //disttb.IsEnabled = false;
            steplabel.IsEnabled = false;
            steptb.IsEnabled = false;
             port= new SerialPort();
            try
            {
                if (port.IsOpen == true) port.Close();
                port.PortName = "COM13";//Change COM port here or Load Cell
                port.BaudRate = 9600;
                port.DataBits = 8;
                port.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "1");
                port.Parity = (Parity)Enum.Parse(typeof(Parity), "2");
                System.Threading.Thread.Sleep(2000);
                port.DataReceived += new SerialDataReceivedEventHandler(Datahnd);
                port.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in openning the COM Port\n" + e.Message + "\n" + e.StackTrace + "\nException is:  \n" + e.InnerException);
                Console.ReadKey();
            }
            Voltport = new SerialPort();
            try
            {
                if (Voltport.IsOpen == true) Voltport.Close();
                Voltport.PortName = "COM4";//Change COM port here or Votage Reading
                Voltport.BaudRate = 9600;
                Voltport.DataBits = 8;
                Voltport.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "1");
                Voltport.Parity = (Parity)Enum.Parse(typeof(Parity), "0");
                System.Threading.Thread.Sleep(2000);
                Voltport.DataReceived += new SerialDataReceivedEventHandler(VoltportReceive);
                Voltport.Open();
                //Voltport.Write("*IDN?");
                //Thread.Sleep(1000);
                //Voltage.Content = "IDN: " + volt;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in openning the COM Port\n" + e.Message + "\n" + e.StackTrace + "\nException is:  \n" + e.InnerException);
                Console.ReadKey();
            }
            string path = "";
            // Configure open file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save Log File Location";
            dlg.InitialDirectory = "C:\\piezodata";
            dlg.FileName = "LogFile"; // Default file name
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "Comma Seperated Values (.csv)|*.csv"; // Filter files by extension 
            //dlg.Filter = "Text File (.txt)|*.txt";
            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {               
                path = dlg.FileName;
            }
            else
            {
                path = "C:\\piezodata\\default";
            }            
            saveFilePath.Content = path.ToString();          
            datafile = System.IO.Path.Combine(path);
            this.Mainworker.RunWorkerAsync();
            
        }

        private void VoltportReceive(object sender, SerialDataReceivedEventArgs e)
        {
            byte terminator = 0x0a;
            // Initialize a buffer to hold the received data
            byte[] buffer = new byte[this.Voltport.ReadBufferSize];

            // There is no accurate method for checking how many bytes are read
            // unless you check the return from the Read method
            int bytesRead = this.Voltport.Read(buffer, 0, buffer.Length);

            // For the example assume the data we are received is ASCII data.
            voltread+= Encoding.ASCII.GetString(buffer, 0, bytesRead);
            int index = voltread.IndexOf((char)terminator);
            // Check if string contains the terminator 
            if ( index> -1)
            {
                // If tString does contain terminator we cannot assume that it is the last character received
                string workingString = voltread.Substring(0, voltread.IndexOf((char)terminator));
                // Remove the data up to the terminator from tString
                voltread = "";// voltread.Substring(voltread.IndexOf((char)terminator));
                // Do something with workingString
                volt = workingString;
                readBuffer.Add(workingString); 
            }
        }

        private void Mainworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Voltage.Content = "Voltage: " + volt;
        }

        private void Mainworker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgk = (BackgroundWorker)sender;
            if (!(Voltport.IsOpen == true)) Voltport.Open();
            string msg = ":FETCh?";
            //send the message to the port
            while (true)
            {
                Voltport.WriteLine(msg);
                Thread.Sleep(300);
                string s = "";
                bgk.ReportProgress(100);
            }
        }
        private void Stageworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Voltage.Content = "Voltage finished";
        }

        private void Stageworker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgk = (BackgroundWorker)sender;
            double delx = (double)mr.distance / 4194304 * 5.5;
            string line = "";            
            Thread.Sleep(2000);
            getload();
            using (StreamWriter writer = new StreamWriter(datafile, true))
            {
                writer.WriteLine("deltax,load");
                try
                {
                    for (int i = 0; i < mr.numberofsteps; i++)
                    {
                        mr.posdirection = mrj4commander.posup;
                        mr.setpos();
                        mr.movepos();
                        Thread.Sleep(1000);
                        mr.posdirection = mrj4commander.posdown;
                        mr.setpos();
                        mr.movepos();
                        Thread.Sleep(1000);
                        getload();
                        msgchnl.setcurrMessage("delx= " + delx * (i + 1) + "-> Load= " + -1 * loadv);
                        line = "" + delx * (i + 1) + "," + -1 * loadv;
                        writer.WriteLine(line);
                    }
                }
                finally
                {
                    writer.Close();
                }
            }
            bgk.ReportProgress(100);
        }


        private void MessageChanged(object sender,EventArgs e)
        {
            //MessgBox.Text = msgchnl.getMessage();
        }
        private void speedtb_TextChanged(object sender, TextChangedEventArgs e)
        {
            double speedTest;
            string value = speedtb.Text;
            if(value.ToCharArray().Length>10)
            {
                msgchnl.setcurrMessage("Incorrect speed value it should be between -32767 to 32767");
                return;
            }
            bool check = Double.TryParse(value, out speedTest);
            if (check && speedTest<32768 )
            {
                mr.speed = (Int16)speedTest;
            }
            else
            {
                msgchnl.setcurrMessage("Incorrect speed value it should be between -32767 to 32767");
            }
        }
        private void acctb_TextChanged(object sender, TextChangedEventArgs e)
        {
            double accTest;
            string value = acctb.Text;
            if (value.ToCharArray().Length > 10)
            {
                msgchnl.setcurrMessage("Incorrect acc/decc value. It should be between -2147483647 to 2147483647");

                return;
            }
            bool check = Double.TryParse(value, out accTest);
            if (check && accTest < 2147483648)
            {
                mr.acc = (Int32)accTest;
            }
            else
            {
                msgchnl.setcurrMessage("Incorrect acc/decc value. It should be between -2147483647 to 2147483647");
            }
        }
        
        private void disttb_TextChanged(object sender, TextChangedEventArgs e)
        {
            double distTest;
            
            string value = disttb.Text;
            if (value.ToCharArray().Length > 4)
            {
                msgchnl.setcurrMessage("Incorrect distance value. It should be between 0.001 to 10.1");
                disttb.Text = "3.0";
                mr.distance = 2287802;
                return;
            }
            bool check = Double.TryParse(value, out distTest);
            if (check && distTest >0.01 && distTest <10.1)//)
            {
                distTest *= 4194304 / 5.5;
                mr.distance = (Int32)distTest;
            }
            else
            {
               //disttb.Text = "3.0";
                mr.distance = 2287802;
                msgchnl.setcurrMessage("Incorrect distance value. It should be between 0.01 to 10.1");
                return;
            }


        }

        private void steptb_TextChanged(object sender, TextChangedEventArgs e)
        {
            double stepTest;
            string value = steptb.Text;
            if (value.ToCharArray().Length > 10)
            {
                msgchnl.setcurrMessage("Incorrect step value. It should be between 1 to 1000");
                return;
            }
            bool check = Double.TryParse(value, out stepTest);
            if (check && stepTest < 1000 && stepTest>0)
            {
                mr.numberofsteps = (Int32)stepTest;
            }
            else
            {
                msgchnl.setcurrMessage("Incorrect step value. It should be between 1 to 1000");
            }
        }

        private void jogrb_Checked(object sender, RoutedEventArgs e)
        {
            jogmode.IsEnabled = true;
            upbutton.IsEnabled = true;
            downbutton.IsEnabled = true;
            posmode.IsEnabled = false;
            posrun.IsEnabled = false;
            //distlabel.IsEnabled = false;
            //disttb.IsEnabled = false;
            steplabel.IsEnabled = false;
            steptb.IsEnabled = false;
        }

        private void posrb_Checked(object sender, RoutedEventArgs e)
        {
            jogmode.IsEnabled = false;
            upbutton.IsEnabled = false;
            downbutton.IsEnabled = false;
            posmode.IsEnabled = true;
            posrun.IsEnabled =true;
            //distlabel.IsEnabled = true;
            //disttb.IsEnabled = true;
            steplabel.IsEnabled = true;
            steptb.IsEnabled = true;
        }

        private void posrun_Click(object sender, RoutedEventArgs e)
        {
            if (!sine)
            {
                double delx = (double)mr.distance / 4194304 * 5.5;
                string line = "";
                mr.posdirection = mrj4commander.posup;
                mr.setpos();
                Thread.Sleep(3000);
                getload();
                using (StreamWriter writer = new StreamWriter(datafile, true))
                {
                    writer.WriteLine("deltax,load");
                    try
                    {

                        for (int i = 0; i < mr.numberofsteps; i++)
                        {
                            mr.movepos();
                            Thread.Sleep(2000);
                            getload();
                            msgchnl.setcurrMessage("delx= " + delx * (i + 1) + "-> Load= " + -1 * loadv);
                            line = "" + delx * (i + 1) + "," + -1 * loadv;
                            writer.WriteLine(line);
                        }
                    }
                    finally
                    {
                        writer.Close();
                    }
                }
            }
            else
            {
                Stageworker.RunWorkerAsync();
            }
        }

        private void upbutton_Click(object sender, RoutedEventArgs e)
        {
            /*mr.jogdirection = mrj4commander.jogup;
            mr.setjog();
            mr.movejog();
            System.Threading.Thread.Sleep(2000);
            mr.stopjog();*/
            mr.posdirection = mrj4commander.posup;
            mr.setpos();           
            mr.movepos();            
        }

        private void downbutton_Click(object sender, RoutedEventArgs e)
        {
            /*mr.jogdirection = mrj4commander.jogdown;
            mr.setjog();
            mr.movejog();
            System.Threading.Thread.Sleep(2000);
            mr.stopjog();*/
            mr.posdirection = mrj4commander.posdown;
            mr.setpos();
            mr.movepos();
        }
        public static void Datahnd(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            load += sp.ReadExisting();
            //Console.WriteLine("Data Received:");
            //Console.Write(indata + " ");
        }
        public static bool getload()
        {
            double loadvtemp = 0.0;
            byte[] tempload = Encoding.ASCII.GetBytes(load);

            //Console.WriteLine(Encoding.ASCII.GetChars(new byte[]{tempload[5]} )[0]+" "+ Encoding.ASCII.GetChars(new byte[] { tempload[7] })[0] + " "+ Encoding.ASCII.GetChars(new byte[] { tempload[8] })[0]);
            string ck0 = new string(Encoding.ASCII.GetChars(new byte[] { tempload[8] }));
            string ck1 = new string(Encoding.ASCII.GetChars(new byte[] { tempload[7] }));
            string ck2 = new string(Encoding.ASCII.GetChars(new byte[] { tempload[5] }));
            //Console.WriteLine("ck2: " + ck2 + " ck1: " + ck1 + " ck0: " + ck0);
            double k0 = 0.0;
            double k1 = 0.0;   
            double k2 = 0.0;
            if (Double.TryParse(ck0, out k0) &&
            Double.TryParse(ck1, out k1) && Double.TryParse(ck2, out k2))
            {
                Console.WriteLine("k0: " + k0 + "k1: " + k1 + "k2: " + k2);
                loadvtemp = (k0 / 100 + k1 / 10 + k2);
                loadv = loadvtemp;
                return true;
            }
            else
            {
                loadv = 0.0;
                return false;
            }
        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            mr.closeConn();
            port.Close();
            //Mainworker.CancelAsync();
            //Voltport.Close();
            using (StreamWriter writer = new StreamWriter(datafile, true))
            {
                writer.WriteLine("deltax,load");
                try
                {

                    foreach (string voltagereading in readBuffer)
                    {
                        writer.WriteLine(voltagereading);
                    }
                }
                finally
                {
                    writer.Close();
                }
            }
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            sine = true;
        }
    }
}
