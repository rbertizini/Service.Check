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
            //Contadores globais
            int gTot = 0;
            int sTot = 0;
            int sSuc = 0;
            int sErr = 0;
            int wTot = 0;
            int wSuc = 0;
            int wErr = 0;

            //Apresentação
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Verificação de serviços");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("");

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

            //Obtendo configuração
            var exeFile = Assembly.GetExecutingAssembly().Location;
            string exeDirectory = Path.GetDirectoryName(exeFile);
            string[] lines = File.ReadAllLines(Path.Combine(exeDirectory, "config.ini"));

            if (lines.Length == 0 || lines == null)
                Console.WriteLine("Configuração não localizada");

            foreach (string line in lines)
            {
                string[] info = line.Split(';');
                gTot++;

                if (info[0] == "S")
                {
                    int tsSuc = 0;
                    int tsErr = 0;
                    checkService(info[1], info[2], out tsSuc, out tsErr);

                    sTot++;
                    sSuc += tsSuc;
                    sErr += tsErr;

                    continue;
                }

                if (info[0] == "W")
                {
                    int twSuc = 0;
                    int twErr = 0;
                    checkWeb(info[1], out twSuc, out twErr);

                    wTot++;
                    wSuc += twSuc;
                    wErr += twErr;

                    continue;
                }
            }

            //Resumo
            Console.WriteLine("");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Resumo de processamento");
            Console.WriteLine("-----------------------------------");

            Console.ResetColor();
            Console.Write("Total de itens processados: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(gTot);
            Console.Write("\r\n");

            Console.ResetColor();
            Console.Write("Total de serviços processados: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(sTot);
            Console.Write("\r\n");

            Console.ResetColor();
            Console.Write("Total de serviços com sucesso: ");
            if (sTot > sSuc)
                Console.ForegroundColor = ConsoleColor.Red;
            else
                Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(sSuc);
            Console.Write("\r\n");

            Console.ResetColor();
            Console.Write("Total de serviços com erro: ");
            if (sErr > 0)
                Console.ForegroundColor = ConsoleColor.Red;
            else
                Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(sErr);
            Console.Write("\r\n");

            Console.ResetColor();
            Console.Write("Total de websites processados: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(wTot);
            Console.Write("\r\n");

            Console.ResetColor();
            Console.Write("Total de websites com sucesso: ");
            if (wTot > wSuc)
                Console.ForegroundColor = ConsoleColor.Red;
            else
                Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(wSuc);
            Console.Write("\r\n");

            Console.ResetColor();
            Console.Write("Total de websites com erro: ");
            if (wErr > 0)
                Console.ForegroundColor = ConsoleColor.Red;
            else
                Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(wErr);
            Console.Write("\r\n");

            //Finalização
            Console.ResetColor();
            Console.WriteLine("");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine(string.Concat("Finalizado - ", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
            Console.WriteLine("-----------------------------------");

            Console.ReadKey();
        }

        public static void checkWeb(string url, out int wSuc, out int wErr)
        {
            string item = string.Concat(url, ": ");
            Console.ResetColor();
            Console.Write(item);

            wSuc = 0;
            wErr = 0;

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

                        wSuc = 1;
                    }
                }
                catch (WebException wex)
                {
                    status = string.Concat("Erro - ", wex.Message);
                    color = ConsoleColor.Red;

                    Console.ForegroundColor = color;
                    Console.Write(status);
                    Console.ResetColor();

                    wErr = 1;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(ex.Message);
                Console.ResetColor();

                wErr = 1;
            }

            Console.Write("\r\n");
        }

        private static void checkService(string ip, string serv, out int sSuc, out int sErr)
        {
            string item = string.Concat(ip, " - ", serv, ": ");
            Console.ResetColor();
            Console.Write(item);

            sSuc = 0;
            sErr = 0;

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

                sSuc = 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(ex.Message);
                Console.ResetColor();

                sErr = 1;
            }

            Console.Write("\r\n");
        }
    }
}
