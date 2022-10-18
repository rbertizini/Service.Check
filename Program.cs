using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Check
{
    class Program
    {
        static void Main(string[] args)
        {
            //Apresentação
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Verificação de serviços");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("");

            //Obtendo configuração
            var exeFile = Assembly.GetExecutingAssembly().Location;
            string exeDirectory = Path.GetDirectoryName(exeFile);
            string[] lines = File.ReadAllLines(Path.Combine(exeDirectory, "config.ini"));

            if (lines.Length == 0 || lines == null)
                Console.WriteLine("Configuração não localizada");

            foreach (string line in lines)
            {
                string[] info = line.Split(';');

                if (info[0] == "S")
                    checkService(info[1], info[2]);

                if (info[0] == "W")
                    checkWeb(info[1]);
            }

            //Finalização
            Console.WriteLine("");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine(string.Concat("Finalizado - ", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
            Console.WriteLine("-----------------------------------");

            Console.ReadKey();
        }

        private static void checkWeb(string url)
        {
            string item = string.Concat(url, ": ");
            Console.ResetColor();
            Console.Write(item);

            string status = string.Empty;
            ConsoleColor color = ConsoleColor.White;
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Timeout = 5000;
                request.AllowAutoRedirect = true;
                request.Method = "GET";
                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        status = response.StatusCode.ToString();
                        color = ConsoleColor.Green;

                        Console.ForegroundColor = color;
                        Console.Write(status);
                        Console.ResetColor();
                    }
                }
                catch (WebException wex)
                {
                    status = string.Concat("Erro - ", wex.Message);
                    color = ConsoleColor.Red;

                    Console.ForegroundColor = color;
                    Console.Write(status);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(ex.Message);
                Console.ResetColor();
            }

            Console.Write("\r\n");
        }

        private static void checkService(string ip, string serv)
        {
            //Verificando se a VPN está ativa - se não estive, liga
            if (Process.GetProcessesByName("openvpn-gui").Length <= 0)
            {
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("Iniciando e conectando VPN");
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("");

                Process p = new Process();
                p.StartInfo.FileName = "C:\\Program Files\\OpenVPN\\bin\\openvpn-gui.exe";
                p.StartInfo.WorkingDirectory = @"C:\Program Files\OpenVPN\bin";
                p.StartInfo.Arguments = " --connect VPN-LKM-Vivo.ovpn";
                p.Start();

                //Aguarda 5 segundos para estabilização
                Thread.Sleep(10000);
            }

            string item = string.Concat(ip, " - ", serv, ": ");
            Console.ResetColor();
            Console.Write(item);

            string status = string.Empty;
            ConsoleColor color = ConsoleColor.White;
            try
            {
                ServiceController sc = new ServiceController(serv, ip);
                switch (sc.Status)
                {
                    case ServiceControllerStatus.Stopped:
                        status = "Parado";
                        color = ConsoleColor.Red;
                        break;
                    case ServiceControllerStatus.StopPending:
                        status = "Pendente parar";
                        color = ConsoleColor.Yellow;
                        break;
                    case ServiceControllerStatus.Running:
                        status = "Executando";
                        color = ConsoleColor.Green;
                        break;
                    case ServiceControllerStatus.ContinuePending:
                        status = "Pendente continuar";
                        color = ConsoleColor.Yellow;
                        break;
                    case ServiceControllerStatus.Paused:
                        status = "Pausado";
                        color = ConsoleColor.Red;
                        break;
                    case ServiceControllerStatus.PausePending:
                        status = "Pendente pausar";
                        color = ConsoleColor.Yellow;
                        break;
                    case ServiceControllerStatus.StartPending:
                        status = "Pendente iniciar";
                        color = ConsoleColor.Yellow;
                        break;
                }

                Console.ForegroundColor = color;
                Console.Write(status);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(ex.Message);
                Console.ResetColor();
            }

            Console.Write("\r\n");
        }
    }
}
