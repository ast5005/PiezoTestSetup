using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PiezoTestSetup
{
   
    public class  mrj4commander
    {
        #region class variables
        public int numberofsteps = 5;
        public static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x06D3, 0x01D0);
        MessageChannel msgchnl;
        internal int n = 5;
        private UsbDevice MyUsbDevice;
        private UsbEndpointWriter writer;
        private UsbEndpointReader reader;
        private IUsbDevice wholeUsbDevice;
        internal ErrorCode ec = ErrorCode.None;
        private byte[] readBuffer;
        private string load = "";
        internal double[] force;
        internal double[] delx;
        internal Dictionary<byte,char> decode = new Dictionary< byte, char>();
        internal Dictionary<UInt32, char> hexdic = new Dictionary<UInt32, char>();
        internal Dictionary<string, byte> encode = new Dictionary<string, byte>();
        internal MessageChannel msg;
        internal int byteswritten;
        private const Int16 exe = 7845;
        private const Int32 sllon = 786439;
        internal const Int16 posup = 256;
        internal const Int16 posdown = 257;
        internal const Int16 jogup = 4103;
        internal const Int16 jogdown = 2055;
        internal const Int16 jogstop = 7;
        internal const Int16 posmode = 2;
        internal const Int16 endmod = 0;
        private byte[] clrdata = { 0x43,0x4C,0x52,0x20};
        private byte[] stopdata = { 0x53, 0x54, 0x4F, 0x50 };
        internal Int16 posdirection { get; set; }
        internal Int16 jogdirection { get; set; }
        internal bool modeselec { get; set; }
        internal  short speed { get; set; }
        internal Int32 acc { get; set; }
        internal Int32 distance { get; set; }
        #endregion
        public mrj4commander(MessageChannel i_msgchnl)
        {
            this.msgchnl = i_msgchnl;
            ec = ErrorCode.None;
            readBuffer = new byte[32];
            try
            {
                msgchnl.setcurrMessage("Initilazing MRJ-4");
                // Find and open the usb device.
                MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);
                // If the device is open and ready
                if (MyUsbDevice == null) throw new Exception("Device Not Found.");
                // If this is a "whole" usb device (libusb-win32, linux libusb)
                // it will have an IUsbDevice interface. If not (WinUSB) the 
                // variable will be null indicating this is an interface of a 
                // device.
                wholeUsbDevice = MyUsbDevice as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    // This is a "whole" USB device. Before it can be used, 
                    // the desired configuration and interface must be selected.
                    // Select config #1
                    wholeUsbDevice.SetConfiguration(1);
                    // Claim interface #0.
                    wholeUsbDevice.ClaimInterface(0);
                }
                // open read endpoint 1.
                reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
                // open write endpoint 1.
                writer = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep02);
                msgchnl.setcurrMessage("Initilazing Completed");
            }
            catch (Exception ex)
            {               
                msgchnl.setcurrMessage((ec != ErrorCode.None ? ec + ":" : String.Empty) + ex.Message);
            }
            this.posdirection = posup;
            this.jogdirection = jogup;
            #region ASCII encode definition
            encode.Add("0 ", 0x20);
            encode.Add("00", 0x30);
            encode.Add("01", 0x31);
            encode.Add("02", 0x32);
            encode.Add("03", 0x33);
            encode.Add("04", 0x34);
            encode.Add("05", 0x35);
            encode.Add("06", 0x36);
            encode.Add("07", 0x37);
            encode.Add("08", 0x38);
            encode.Add("09", 0x39);
            encode.Add("0A", 0x41);
            encode.Add("0B", 0x42);
            encode.Add("0C", 0x43);
            encode.Add("0D", 0x44);
            encode.Add("0E", 0x45);
            encode.Add("0F", 0x46);        
            encode.Add("0L", 0x4C);
            encode.Add("0O", 0x4F);
            encode.Add("0P", 0x50);
            encode.Add("0R", 0x52);
            encode.Add("0S", 0x53);           
            encode.Add("0T", 0x54); 
            #endregion
            #region ASCII decode definition
            decode.Add(0x30, '0');
            decode.Add(0x31, '1');
            decode.Add(0x32, '2');
            decode.Add(0x33, '3');
            decode.Add(0x34, '4');
            decode.Add(0x35, '5');
            decode.Add(0x36, '6');
            decode.Add(0x37, '7');
            decode.Add(0x38, '8');
            decode.Add(0x39, '9');
            decode.Add(0x41, 'A');
            decode.Add(0x42, 'B');
            decode.Add(0x43, 'C');
            decode.Add(0x44, 'D');
            decode.Add(0x45, 'E');
            decode.Add(0x46, 'F');
            #endregion
            #region Hex Conversion definition
            hexdic.Add(0, '0');
            hexdic.Add(1, '1');
            hexdic.Add(2, '2');
            hexdic.Add(3, '3');
            hexdic.Add(4, '4');
            hexdic.Add(5, '5');
            hexdic.Add(6, '6');
            hexdic.Add(7, '7');
            hexdic.Add(8, '8');
            hexdic.Add(9, '9');
            hexdic.Add(10, 'A');
            hexdic.Add(11, 'B');
            hexdic.Add(12, 'C');
            hexdic.Add(13, 'D');
            hexdic.Add(14, 'E');
            hexdic.Add(15, 'F');
            #endregion
        }
        public bool closeConn()
        {
            if (MyUsbDevice != null)
            {
                if (MyUsbDevice.IsOpen)
                {
                    // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                    // it exposes an IUsbDevice interface. If not (WinUSB) the 
                    // 'wholeUsbDevice' variable will be null indicating this is 
                    // an interface of a device; it does not require or support 
                    // configuration and interface selection.
                    IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                    if (!ReferenceEquals(wholeUsbDevice, null))
                    {
                        // Release interface #0.
                        wholeUsbDevice.ReleaseInterface(0);
                    }

                    MyUsbDevice.Close();
                }
                MyUsbDevice = null;

                // Free usb resources
                UsbDevice.Exit();
                return true;
            }
            return true;
        }
        private int exec(byte[] code, out int bytesWritten)
        {
            int byteswrit = 0;
            int bytesread = 0;
            //writer.Dispose(); // I think this also helps to reestablish the connection and get rid of the error
            if (reconnect())
            {
                //Console.WriteLine("Reconnecting");
                //reconnected, try to write to the device again
                ec = writer.Write(code, 2000, out byteswrit);  
                if (ec != ErrorCode.None)
                {
                    //log the error
                }
            }
            
            bytesWritten = byteswrit;
            ec = reader.Read(readBuffer, 100, out bytesread);
            //Console.WriteLine(bytesread + " bytes were read");
            //Console.WriteLine(BitConverter.ToString(readBuffer, 0, bytesread) + "|  " + Encoding.ASCII.GetString(readBuffer));
            return ec == ErrorCode.None ? 1 : 0;

        }
        private bool reconnect()
        {
            //clear the info so far
            if (MyUsbDevice != null)
            {
                writer.Dispose();
                wholeUsbDevice.ReleaseInterface(0);
                wholeUsbDevice.Close();
                MyUsbDevice.Close();
                UsbDevice.Exit();
            }
            //now start over
            MyUsbFinder = new UsbDeviceFinder(0x06D3, 0x01D0);
            MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);
            // If the device is open and ready
            if (MyUsbDevice == null)
            {
                msgchnl.setcurrMessage("Problem in reconnect() MyUsbDevice is null");
                return false;
            }
            else
            {
                wholeUsbDevice = MyUsbDevice as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    // Select config #1
                    wholeUsbDevice.SetConfiguration(1);
                    // Claim interface #0.
                    wholeUsbDevice.ClaimInterface(0);
                }
                writer = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep02);
                reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
                //Console.WriteLine("New Writer an reader was assigned");
                return true;
            }
        }
        private byte[] getchecksum(byte[] rawComm)
        {
            int length = rawComm.Length;
            byte[] builtComm = new byte[2];
            int sum = 0x00;
            for(int i=7;i< length;i++)
            {
                
                sum += rawComm[i];
            }
            byte[] bytearray = BitConverter.GetBytes(sum);  
            string lows = BitConverter.ToString(new byte[] 
            { (byte)(bytearray[0] & 0x0F) }).Replace("-", string.Empty);
            string highs = BitConverter.ToString(new byte[] 
            { (byte)((bytearray[0] & 0xF0) >> 4) }).Replace("-", string.Empty);
            builtComm[0] = encode[highs];
            builtComm[1] = encode[lows];
            return builtComm;
        }
        public byte[] getcommand(byte prefix,char[] comm,char[] datano,char[] data)
        {
            int length = data.Length + 16;
            byte[] buildComm = new byte[length];
            byte bytecount;
            if(data.Length==4)
            {
                bytecount = 0x11;
            }
            else
            {
                bytecount = 0x15;
            }
            buildComm[0] = 0x01;
            buildComm[1] = 0xff;
            buildComm[2] = bytecount;
            buildComm[3] = 0x10;
            buildComm[4] = 0x00;
            buildComm[5] = 0x00;
            buildComm[6] = 0x01;
            buildComm[7] = 0x30;
            buildComm[8]= encode["0"+comm[0].ToString()];
            buildComm[9] = encode["0" + comm[1].ToString()];
            buildComm[10] = 0x02;
            buildComm[11] = encode["0" + datano[0].ToString()];
            buildComm[12] = encode["0" + datano[1].ToString()];
            for (int i = 13; i <13+data.Length; i++)
            {
                string tempstring= data[i - 13].ToString();
                buildComm[i] = encode["0" + tempstring];
            }

                
            buildComm[13+data.Length] = 0x03;
            buildComm[data.Length+14] = 0x00;
            buildComm[data.Length + 15] = 0x00;
            byte[] temp= getchecksum(buildComm);
            buildComm[data.Length + 14] = temp[0];
            buildComm[data.Length +15] = temp[1];
            return buildComm;
            
        }
        public byte[] getcommand(byte prefix, char[] comm, char[] datano, byte[] data)
        {
            int length = data.Length + 16;
            byte[] buildComm = new byte[length];
            byte bytecount;
            if (data.Length == 4)
            {
                bytecount = 0x11;
            }
            else
            {
                bytecount = 0x15;
            }
            buildComm[0] = 0x01;
            buildComm[1] = 0xff;
            buildComm[2] = bytecount;
            buildComm[3] = 0x10;
            buildComm[4] = 0x00;
            buildComm[5] = 0x00;
            buildComm[6] = 0x01;
            buildComm[7] = 0x30;
            buildComm[8] = encode["0" + comm[0].ToString()];
            buildComm[9] = encode["0" + comm[1].ToString()];
            buildComm[10] = 0x02;
            buildComm[11] = encode["0" + datano[0].ToString()];
            buildComm[12] = encode["0" + datano[1].ToString()];
            for (int i = 13; i < 13 + data.Length; i++) buildComm[i] = data[i - 13];
            buildComm[13 + data.Length] = 0x03;
            buildComm[data.Length + 14] = 0x00;
            buildComm[data.Length + 15] = 0x00;
            byte[] temp = getchecksum(buildComm);
            buildComm[data.Length + 14] = temp[0];
            buildComm[data.Length + 15] = temp[1];
            return buildComm;

        }
        public byte[] getcommand(byte prefix, char[] comm, char[] datano)
        {
            int length = 16;
            byte[] buildComm = new byte[length];           
            buildComm[0] = 0x01;
            buildComm[1] = 0xff;
            buildComm[2] = 0x0d;
            buildComm[3] = 0x10;
            buildComm[4] = 0x00;
            buildComm[5] = 0x00;
            buildComm[6] = 0x01;
            buildComm[7] = 0x30;
            buildComm[8] = encode["0"+comm[0].ToString()];
            buildComm[9] = encode["0"+comm[1].ToString()];
            buildComm[10] = 0x02;
            buildComm[11] = encode["0"+datano[0].ToString()];
            buildComm[12] = encode["0"+datano[1].ToString()];           
            buildComm[13] = 0x03;
            buildComm[14] = 0x00;
            buildComm[15] = 0x00;
            byte[] temp = getchecksum(buildComm);
            buildComm[14] = temp[0];
            buildComm[15] = temp[1];
            return buildComm;

        }
        public char[] getascii(byte[] data)
        {
            char[] ascii = new char[data.Length];
            for (int i = 0; i < data.Length; i++) ascii[i] = decode[data[i]];
            return ascii;
        }
        public char[] gethex(UInt32 data)
        {
            char[] hex;
            char[] hexR;
            if (data>65535)
            {
                hex = new char[8];
                hexR = new char[8];
            }
            else
            {
                hex = new char[4];
                hexR = new char[4];
            }
            for (int j = 0;j< hex.Length;j++) hex[j] = '0';
            UInt32 quo = data;
            int i = 0;
            if (data < 16)
            {
                hex[3] = hexdic[data];
                return hex;
            }
            while (data >= 16)
            {
                quo = data % 16;
                hexR[i] = hexdic[quo];
                data = (data - quo) / 16;
                i++;
            }
            hexR[i++] = hexdic[data];
            for (int j = i; j < hexR.Length; j++) hexR[j] = '0';  
            for (int j = 0; j < hex.Length; j++) hex[hex.Length - 1 - j] = hexR[j];
             return hex;
        }
        public bool setpos()
        {
            char[] data = gethex((UInt32)posmode);
            byte[] setmode = getcommand(0x00,new char[] { '8', 'B' },new char[] { '0','0'},data);
            exec(setmode, out byteswritten);
            turnoffinput();
            setspeed();
            checkunkown();
            setacc();
            devicestatus();
            inputdonoff();
            clr();
            setdist();
            setposdirection(); 
            return true;
        }
        public bool setjog()
        {
            char[] data = gethex((UInt32)posmode);
            byte[] setmode = getcommand(0x00, new char[] { '8', 'B' }, new char[] { '0', '0' }, data);
            turnoffinput();
            setspeed();
            checkunkown();
            setacc();
            devicestatus(); 
            return true;
        }
        public bool turnoffinput()
        {

            char[] data = gethex((UInt32)exe);
            byte[] code = getcommand(0x00, new char[] { '9', '0' }, new char[] { '0', '0' },data);
            exec(code,out byteswritten);
            return true;
        }
        public bool setspeed()
        {
            char[] data = gethex((UInt32)speed);
            byte[] code= getcommand(0x00, new char[] { 'A', '0' }, new char[] { '1', '0' }, data);
            exec(code, out byteswritten);
            return true;
        }
        public bool checkoperationmode()
        {           
            byte[] code = getcommand(0x00, new char[] { '0', '0' }, new char[] { '1', '2' });
            exec(code, out byteswritten);
            //implement read function
            return true;
        }
        public bool checkunkown()
        {
            byte[] code = getcommand(0x00, new char[] { '0', '0' }, new char[] { '2', '1' });
            exec(code, out byteswritten);
            //implement read function
            return true;
        }
        public bool setacc()
        {
            char[] data = gethex((UInt32)acc);
            byte[] code = getcommand(0x00, new char[] { 'A', '0' }, new char[] { '1', '1' }, data);
            exec(code, out byteswritten);
            return true;
        }
        public bool setdist()
        {
            char[] data = gethex((UInt32)distance);
            byte[] code = getcommand(0x00, new char[] { 'A', '0' }, new char[] { '2', '0' }, data);
            exec(code, out byteswritten);
            return true;
        }
        public bool setposdirection()
        {
            char[] data = gethex((UInt32)posdirection);
            byte[] code = getcommand(0x00, new char[] { 'A', '0' }, new char[] { '2', '1' }, data);
            exec(code, out byteswritten);
            return true;
        }        
        public bool movejog()
        {
            char[] data = gethex((UInt32)jogdirection); 
            byte[] code = getcommand(0x00, new char[] { '9', '2' }, new char[] { '0', '0' }, data);
            exec(code, out byteswritten);
            return true;
        }
        public bool stopjog()
        {
            char[] data = gethex((UInt32)jogstop);
            byte[] code = getcommand(0x00, new char[] { '9', '2' }, new char[] { '0', '0' }, data);
            exec(code, out byteswritten);
            return true;
        }
        public bool devicestatus()
        {
            byte[] code = getcommand(0x00, new char[] { '1', '2' }, new char[] { '0', '0' });
            exec(code, out byteswritten);
            //implement read function
            return true;
        }
        public bool inputdonoff()
        {
            char[] data = gethex((UInt32)sllon);
            byte[] code = getcommand(0x00, new char[] { '9', '2' }, new char[] { '0', '0' }, data);
            exec(code, out byteswritten);
            return true;
        }
        public bool clr()
        {
            byte[] code = getcommand(0x00, new char[] { 'A', '0' }, new char[] { '4', '1' }, clrdata);
            exec(code, out byteswritten);
            return true;
        }
        public bool stoppos()
        {
            byte[] code = getcommand(0x00, new char[] { 'A', '0' }, new char[] { '4', '1' }, stopdata);
            exec(code, out byteswritten);
            return true;
        }
        public bool movepos()
        {
            char[] data = gethex((UInt32)exe);
            byte[] code = getcommand(0x00, new char[] { 'A', '0' }, new char[] { '4', '0' }, data);
            exec(code, out byteswritten);
            return true;
        }
        public bool currAlarm()
        {
            byte[] code = getcommand(0x00, new char[] { '0', '2' }, new char[] { '0', '0' });
            exec(code, out byteswritten);
            //implement read function
            return true;
        }
        public bool endmode()
        {
            char[] data = gethex((UInt32)endmod); 
            byte[] setmode = getcommand(0x00, new char[] { '8', 'B' }, new char[] { '0', '0' }, data);
            return true;
        }
        public bool testDCMode()
        {
            posdirection = mrj4commander.posup;
            setpos();
            for(int i=1;i<numberofsteps+1;i++)
            {
                Thread.Sleep(1000);
                movepos(); 
            }            
            return true;

        }
    }
}
