using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace CatPost_Scanner
{
    class PipeClient
    {
        private NamedPipeClientStream client;
        private StreamReader sr;
        private StreamWriter sw;

        private string version = "5.0";

        private void SendMessage(string mess)
        {
            sw.WriteLine(mess);
            sw.Flush();
        }

        public void Update()
        {
            if (File.Exists("Updater.exe"))
            {
                string path = System.AppDomain.CurrentDomain.BaseDirectory;
                Process proc = new Process();
                proc.StartInfo.FileName = path + "Updater.exe";
                proc.StartInfo.WorkingDirectory = path;
                proc.Start();
                client.Connect(30000);
                string str;
                if (client.IsConnected)
                {
                    str = sr.ReadLine();
                    if (str == "ready")
                    {
                        SendMessage("update");
                        str = sr.ReadLine();
                        if (str == "getpath")
                        {
                            SendMessage(path);
                            str = sr.ReadLine();
                            if (str == "getversion")
                            {
                                SendMessage(version);
                                str = sr.ReadLine();
                                if (str == "getexename")
                                {
                                    SendMessage(System.AppDomain.CurrentDomain.FriendlyName);
                                    str = sr.ReadLine();
                                    if (str == "getid")
                                    {
                                        SendMessage(Process.GetCurrentProcess().Id.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public PipeClient()
        {
            client = new NamedPipeClientStream("catpost_update");
            sr = new StreamReader(client);
            sw = new StreamWriter(client);
        }
    }
}
