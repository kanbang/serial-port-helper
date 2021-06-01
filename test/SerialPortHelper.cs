using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace test
{
    public sealed class SerialPortHelper
    {
        private static readonly Lazy<SerialPortHelper> lazy = new Lazy<SerialPortHelper>(() => new SerialPortHelper());
        public static SerialPortHelper Instance { get { return lazy.Value; } }

        private SerialPort _serialPort;
        private Thread _readThread;
        private volatile bool _reading;

        public enum ReadMode
        {
            Read,
            ReadLine
        }

        private volatile ReadMode _readMode;

        private SerialPortHelper()
        {
            _serialPort = new SerialPort();
            _readThread = null;
            _reading = false;
            _readMode = ReadMode.Read;
        }

        public void SetReadMode(ReadMode readmode)
        {
            _readMode = readmode;
        }

        /// <summary>
        /// Update the serial port status to the event subscriber
        /// </summary>
        public event EventHandler<string> OnStatusChanged;

        /// <summary>
        /// Update received data from the serial port to the event subscriber
        /// </summary>
        public event EventHandler<string> OnDataReceived;

        /// <summary>
        /// Update true/false for the serial port connection to the event subscriber
        /// </summary>
        public event EventHandler<bool> OnSerialPortOpened;

        /// <summary>
        /// Return true if the serial port is currently connected
        /// </summary>
        public bool IsOpen { get { return _serialPort.IsOpen; } }

        /// <summary>
        /// Open the serial port connection using basic serial port settings
        /// </summary>
        /// <param name="portname">COM1 / COM3 / COM4 / etc.</param>
        /// <param name="baudrate">0 / 100 / 300 / 600 / 1200 / 2400 / 4800 / 9600 / 14400 / 19200 / 38400 / 56000 / 57600 / 115200 / 128000 / 256000</param>
        /// <param name="parity">None / Odd / Even / Mark / Space</param>
        /// <param name="databits">5 / 6 / 7 / 8</param>
        /// <param name="stopbits">None / One / Two / OnePointFive</param>
        /// <param name="handshake">None / XOnXOff / RequestToSend / RequestToSendXOnXOff</param>
        public void Open(
            string portname = "COM1",
            int baudrate = 9600,
            Parity parity = Parity.None,
            int databits = 8,
            StopBits stopbits = StopBits.One,
            Handshake handshake = Handshake.None)
        {
            Close();

            _serialPort.PortName = portname;
            _serialPort.BaudRate = baudrate;
            _serialPort.Parity = parity;
            _serialPort.DataBits = databits;
            _serialPort.StopBits = stopbits;
            _serialPort.Handshake = handshake;

            _serialPort.ReadTimeout = 50;
            _serialPort.WriteTimeout = 50;

            TryToOpen();

            StartReading();
        }

        /// <summary>
        /// Close the serial port connection
        /// </summary>
        public void Close()
        {
            StopReading();

            _serialPort.Close();

            NotifyStatusChanged("Connection closed.");

            NotifySerialPortOpened(false);
        }

        /// <summary>
        /// Send/write string to the serial port
        /// </summary>
        /// <param name="message"></param>
        public void SendString(string message)
        {
            if (_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Write(message);

                    NotifyStatusChanged(string.Format("Message sent: {0}", message));

                }
                catch (Exception ex)
                {
                    NotifyStatusChanged(string.Format("Failed to send string: {0}", ex.Message));

                }
            }
        }


        private void StartReading()
        {
            if (!_reading)
            {
                _reading = true;
                _readThread = new Thread(ReadPort);
                _readThread.Start();
            }
        }

        private void StopReading()
        {
            if (_reading)
            {
                _reading = false;
                _readThread.Join();
                _readThread = null;
            }
        }

        private void TryToOpen()
        {
            try
            {
                _serialPort.Open();
            }
            catch (IOException)
            {
                NotifyStatusChanged(string.Format("{0} does not exist.", _serialPort.PortName));
            }
            catch (UnauthorizedAccessException)
            {
                NotifyStatusChanged(string.Format("{0} already in use.", _serialPort.PortName));
            }
            catch (Exception ex)
            {
                NotifyStatusChanged("Error: " + ex.Message);
            }

            if (_serialPort.IsOpen)
            {
                string stopbits;
                switch (_serialPort.StopBits)
                {
                    case StopBits.One:
                        stopbits = "1"; break;
                    case StopBits.OnePointFive:
                        stopbits = "1.5"; break;
                    case StopBits.Two:
                        stopbits = "2"; break;
                    default:
                        stopbits = StopBits.None.ToString().Substring(0, 1);
                        break;
                }

                string p = _serialPort.Parity.ToString().Substring(0, 1);
                string hs = _serialPort.Handshake == Handshake.None ? "No Handshake" : _serialPort.Handshake.ToString();

                NotifyStatusChanged(string.Format(
                    "Connected to {0}: {1} bps, {2}{3}{4}, {5}.",
                    _serialPort.PortName,
                    _serialPort.BaudRate,
                    _serialPort.DataBits,
                    p,
                    stopbits,
                    hs));


                NotifySerialPortOpened(true);
            }
            else
            {
                NotifySerialPortOpened(false);
            }
        }

        private void ReadPort()
        {
            string data;
            byte[] buffer = new byte[_serialPort.ReadBufferSize + 1];
            int count = 0;

            while (_reading)
            {
                if (_serialPort.IsOpen)
                {
                    try
                    {
                        if (_readMode == ReadMode.Read)
                        {
                            count = _serialPort.Read(buffer, 0, _serialPort.ReadBufferSize);
                            data = Encoding.ASCII.GetString(buffer, 0, count);
                        }
                        else
                        {
                            data = _serialPort.ReadLine();
                        }

                        NotifyDataReceived(data);

                    }
                    catch (TimeoutException)
                    {
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    NotifyStatusChanged("try to open : " + _serialPort.PortName + " ...");

                    TryToOpen();

                    TimeSpan waitTime = new TimeSpan(0, 0, 0, 0, 1000);
                    Thread.Sleep(waitTime);
                }
            }
        }

        /// <summary>
        /// notify serial port status event
        /// </summary>
        /// <param name="str"></param>
        private void NotifyStatusChanged(string str)
        {
            if (OnStatusChanged != null)
            {
                OnStatusChanged(this, str);
            }
        }

        /// <summary>
        /// notify serial port data event
        /// </summary>
        /// <param name="str"></param>
        private void NotifyDataReceived(string str)
        {
            if (OnDataReceived != null)
            {
                OnDataReceived(this, str);
            }
        }

        /// <summary>
        /// notify serial port open event
        /// </summary>
        /// <param name="bOpen"></param>
        private void NotifySerialPortOpened(bool bOpen)
        {
            if (OnSerialPortOpened != null)
            {
                OnSerialPortOpened(this, bOpen);
            }
        }
    }
}
