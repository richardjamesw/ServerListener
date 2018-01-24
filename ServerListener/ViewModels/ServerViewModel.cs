using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ServerListener.ViewModels
{
   class ServerViewModel : INotifyPropertyChanged
   {
      #region Instance Variables & Constructors
      private static object lockObj = new object();
      private static volatile ServerViewModel instance;
      private TcpListener server = null;
      private Thread listenerThread = null;

      public static ServerViewModel Instance
      {
         get
         {
            if (instance == null)
            {
               lock (lockObj)
               {
                  if (instance == null)
                     instance = new ServerViewModel();
               }
            }
            return instance;
         }
      }

      public ServerViewModel()
      {
         StartCommand = new DelegateCommand(OnStart, CanStart);
         StopCommand = new DelegateCommand(OnStop, CanStop);
      }
      #endregion

      #region INotifyPropertyChanged
      public event PropertyChangedEventHandler PropertyChanged;
      private void NotifyPropertyChanged(string str)
      {
         if (PropertyChanged != null)
         {
            PropertyChanged(this, new PropertyChangedEventArgs(str));
         }
      }
      #endregion

      #region Properties

      private string ipaddress = "Any";
      public string txtIPAddress
      {
         get
         {
            return ipaddress;
         }
         set
         {
            ipaddress = value;
            NotifyPropertyChanged("txtIPAddress");
         }
      }

      private Int32 port = 0;
      public Int32 txtPort
      {
         get
         {
            return port;
         }
         set
         {
            port = value;
            NotifyPropertyChanged("txtPort");
         }
      }

      private string status = "Status..";
      public string txtStatus
      {
         get
         {
            return status;
         }
         set
         {
            status = value;
            NotifyPropertyChanged("txtStatus");
         }
      }

      private void AppendMessage(string s)
      {
         txtStatus += '\n' + s;
      }
      private void ClearStatusBox()
      {
         txtStatus = "";
      }
      private void ClearStatusBox(string s)
      {
         txtStatus = s;
      }
      #endregion

      #region Commands

      private void Listen()
      {
         //IPAddress ip = IPAddress.Parse(txtIPAddress);
         var r = new Regex(@"\d.\d.\d.\d");
         IPAddress ip;
         if (r.IsMatch(txtIPAddress))
         {
            ip = IPAddress.Parse(txtIPAddress);
         }
         else
         {
            ip = IPAddress.Any;
         }
         
         try
         {
            AppendMessage("Attempting to connect to server..");
            server = new TcpListener(ip, txtPort);
            RaiseCanExecuteChangedCommands();
            server.Start();
            // Buffer for reading data
            Byte[] bytes = new Byte[256];
            string data = null;
            // Listening loop
            for (;;)
            {
               HexToASCII("");
               AppendMessage("Waiting for a connection... ");

               // Perform a blocking call to accept requests.
               //// Could also use server.AcceptSocket() here.
               TcpClient client = server.AcceptTcpClient();
               AppendMessage("CONNECTED.");

               // Get a stream object for reading and writing
               NetworkStream stream = client.GetStream();

               // Loop to receive all the data sent by the client.
               int i; data = null;
               while ((i = stream.Read(bytes, 0, bytes.Length)) > 2)
               {
                  // Translate data bytes to a ASCII string.
                  data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                  AppendMessage($"RECEIVED: {data}");
                  AppendMessage($"PARSED: {HexToASCII(data)}");

                  //// Process the data sent by the client.
                  //data = data.ToUpper();

                  //byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                  //// Send back a response.
                  //stream.Write(msg, 0, msg.Length);
                  //AppendMessage($"SENT: {data}");
               }

               // Shutdown and end connection
               if (client != null)
                  client.Close();
               AppendMessage("Connection Closed.\n");
            }
         }
         catch (Exception e)
         {
            AppendMessage("Error communicating with server..");
            AppendMessage($"Error: {e} \n\n");
         }
         finally
         {
            if(server != null)
            {
               server.Stop();
            }
         }
      }


      /// <summary>
      /// Take hex code received from the RUPS2000 software and parse to ASCII.
      /// This hex code is buried in the received message.
      /// </summary>
      /// <param name="hex">Entire message received from RUPS200 software. This string needs to be trimmed of non-hex value data.</param>
      /// <returns></returns>
      private string HexToASCII(string hex)
      {
         string ascii = string.Empty;
         if (hex == null) return ascii;
         // Remove non-hex data
         var match = Regex.Match(hex, @"sms_content=(?<group>[a-f0-9]+) HTTP");
         hex = match.Groups["group"].Value;

         try
         {

            for (int i = 0; i < hex.Length; i += 2)
            {
               string hs = string.Empty;
               hs = hex.Substring(i, 2);
               // skip 00's
               if (hs == "00") continue;
               
               ascii += Convert.ToChar(Convert.ToUInt32(hs, 16));
            }
            // Remove PC name and get received message
            match = Regex.Match(ascii, @": (?<group>[a-f0-9]+) HTTP");
            ascii = match.Groups["group"].Value;

         }
         catch (Exception)
         {
            return "Invalid HEX data..";

         }

         return ascii;
      }

      /// <summary>
      /// Command for start button
      /// </summary>
      public DelegateCommand StartCommand { get; private set; }
      private void OnStart()
      {
         listenerThread = new Thread(Listen);
         listenerThread.Start();
      }
      private bool CanStart()
      {
         return server == null;
      }
      /// <summary>
      /// Command for stop button
      /// </summary>
      public DelegateCommand StopCommand { get; private set; }
      private void OnStop()
      {
         // Kill thread and listener
         server.Stop();
         server = null;
         listenerThread = null;
         RaiseCanExecuteChangedCommands();
      }
      private bool CanStop()
      {
         return server != null;
      }

      private void RaiseCanExecuteChangedCommands()
      {
         StartCommand.RaiseCanExecuteChanged();
         StopCommand.RaiseCanExecuteChanged();
      }
      #endregion
   }
}
