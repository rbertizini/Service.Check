using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Policy;
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
            int pTot = 0;
            int pSuc = 0;
            int pErr = 0;
            int mTot = 0;
            int mSuc = 0;
            int mErr = 0;

            //Apresentação
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Verificação de serviços e sites");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("");

            //Verificando VPN
            bool vpnOn = false;

            //Verificando se a OpenVPN GUI
            if (Process.GetProcessesByName("openvpn-gui").Length <= 0)
            {
                if (File.Exists("C:\\Program Files\\OpenVPN\\bin\\openvpn-gui.exe"))
                {
                    Console.WriteLine("> Iniciando e conectando VPN LKM");
                    Console.WriteLine("");

                    Process p = new Process();
                    p.StartInfo.FileName = "C:\\Program Files\\OpenVPN\\bin\\openvpn-gui.exe";
                    p.StartInfo.WorkingDirectory = @"C:\Program Files\OpenVPN\bin";
                    p.StartInfo.Arguments = " --connect VPN-LKM-Vivo.ovpn";
                    p.Start();

                    //Aguarda 5 segundos para estabilização
                    Thread.Sleep(12000);
                    vpnOn = true;
                }
            }

            //Verificando se a OpenVPN Connect
            if (vpnOn == false)
            {
                if (File.Exists("C:\\Program Files\\OpenVPN Connect\\OpenVPNConnect.exe"))
                {
                    Console.WriteLine("> Iniciando e conectando VPN LKM");
                    Console.WriteLine("");

                    int tSuc = 0;
                    int tErr = 0;
                    checkPing("192.168.197.29", "1", out tSuc, out tErr, true);
                    if (tSuc == 0)
                    {

                        Process p = new Process();
                        p.StartInfo.FileName = "C:\\Program Files\\OpenVPN Connect\\OpenVPNConnect.exe";
                        p.StartInfo.WorkingDirectory = @"C:\\Program Files\\OpenVPN Connect\\";
                        p.StartInfo.Arguments = "";
                        p.Start();

                        //Aguarda conexão manual
                        Console.WriteLine("> Após iniciar a conexão VPN, pressione enter para continuar...");
                        while (Console.ReadKey().Key != ConsoleKey.Enter)
                        {
                            Console.WriteLine("> Após iniciar a conexão VPN, pressione enter para continuar...");
                        }

                    }

                    vpnOn = true;
                }
            }

            //Verificando se foi conectada alguma VPN
            if (vpnOn == false)
            {
                Console.WriteLine("> Confirme o funcionamento de sua VPN");
                Console.WriteLine("");
            }

            //Verificando velocidade de conexão
            double[] speeds = new double[5];
            for (int i = 0; i < 5; i++)
            {
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Reset();
                stopwatch.Start();
                WebClient webClient = new WebClient();
                byte[] bytes = webClient.DownloadData("http://www.lkm.com.br");

                stopwatch.Stop();

                double seconds = stopwatch.Elapsed.TotalSeconds;
                double speed = bytes.Count() / seconds;
                speeds[i] = speed;
            }
            Console.WriteLine(string.Format("> Estimativa velocidade internet: {0}MB/s", ((speeds.Average() / 1000).ToString("0.##"))));
            Console.WriteLine("");

            //Obtendo configuração
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Verificação de ambientes");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("");

            var exeFile = Assembly.GetExecutingAssembly().Location;
            string exeDirectory = Path.GetDirectoryName(exeFile);
            string[] lines = File.ReadAllLines(Path.Combine(exeDirectory, "config.ini"));

            if (lines.Length == 0 || lines == null)
                Console.WriteLine("Configuração não localizada");

            foreach (string line in lines)
            {
                string[] info = line.Split(';');
                gTot++;

                //Verificação de services
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

                //Verificação de webservices e sites
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

                //Verificação de ping
                if (info[0] == "P")
                {
                    int tpSuc = 0;
                    int tpErr = 0;
                    checkPing(info[1], info[2], out tpSuc, out tpErr);

                    gTot = gTot - 1;
                    gTot = gTot + tpSuc + tpErr;
                    pTot = pTot + tpSuc + tpErr;
                    pSuc += tpSuc;
                    pErr += tpErr;

                    continue;
                }

                //Processo de movimentação de arquivos - move
                if (info[0] == "M")
                {
                    int tmSuc = 0;
                    int tmErr = 0;
                    MoveFile(info[1], info[2], out tmSuc, out tmErr);

                    gTot = gTot - 1;
                    gTot = gTot + tmSuc + tmErr;
                    mTot = mTot + tmSuc + tmErr;
                    mSuc += tmSuc;
                    mErr += tmErr;

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

            //Serviços
            Console.ResetColor();
            Console.Write("Total de serviços processados: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(sTot);
            Console.Write("\r\n");

            Console.ResetColor();
            Console.Write("Total de serviços com sucesso: ");
            if (sTot > sSuc)
                Console.ForegroundColor = ConsoleColor.Yellow;
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

            //Webservice e websites
            Console.ResetColor();
            Console.Write("Total de websites processados: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(wTot);
            Console.Write("\r\n");

            Console.ResetColor();
            Console.Write("Total de websites com sucesso: ");
            if (wTot > wSuc)
                Console.ForegroundColor = ConsoleColor.Yellow;
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

            //Ping
            Console.ResetColor();
            Console.Write("Total de pings processados: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(pTot);
            Console.Write("\r\n");

            Console.ResetColor();
            Console.Write("Total de pings com sucesso: ");
            if (pTot > pSuc)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else
                Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(pSuc);
            Console.Write("\r\n");

            Console.ResetColor();
            Console.Write("Total de pings com erro: ");
            if (pErr > 0)
                Console.ForegroundColor = ConsoleColor.Red;
            else
                Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(pErr);
            Console.Write("\r\n");

            //Movimentação de arquivos
            Console.ResetColor();
            Console.Write("Total de arquivos movidos: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(mTot);
            Console.Write("\r\n");

            Console.ResetColor();
            Console.Write("Total de arquivos movidos com sucesso: ");
            if (mTot > mSuc)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else
                Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(mSuc);
            Console.Write("\r\n");

            Console.ResetColor();
            Console.Write("Total de arquivos movidos com erro: ");
            if (mErr > 0)
                Console.ForegroundColor = ConsoleColor.Red;
            else
                Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(mErr);
            Console.Write("\r\n");

            //Finalização
            Console.ResetColor();
            Console.WriteLine("");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine(string.Concat("Finalizado - ", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
            Console.WriteLine("-----------------------------------");

            Console.ReadKey();
        }

        private static void MoveFile(string dirOrig, string dirDest, out int mSuc, out int mErr)
        {
            Console.ResetColor();
            Console.WriteLine(string.Concat("Dir. Origem: ", dirOrig));

            Console.ResetColor();
            Console.WriteLine(string.Concat("Dir. Destino: ", dirDest));

            mSuc = 0;
            mErr = 0;

            string status = string.Empty;
            ConsoleColor color = ConsoleColor.White;
            try
            {
                var files = from file in Directory.EnumerateFiles(dirOrig) select file;
                foreach (var file in files)
                {
                    try
                    {
                        File.Move(file, Path.Combine(dirDest, Path.GetFileName(file)));

                        Console.ResetColor();
                        Console.Write("Arquivo: ");

                        color = ConsoleColor.Green;
                        Console.ForegroundColor = color;
                        Console.Write(Path.GetFileName(file));
                        Console.ResetColor();

                        mSuc++;
                    }
                    catch (Exception mex)
                    {
                        Console.ResetColor();
                        Console.Write("Arquivo: ");

                        status = string.Concat("Erro - ", mex.Message);
                        color = ConsoleColor.Red;

                        Console.ForegroundColor = color;
                        Console.Write(status);
                        Console.ResetColor();

                        mErr++;
                    }

                    Console.Write("\r\n");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(ex.Message);
                Console.ResetColor();

                mErr = 1;
            }

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
                
                //Criando loop de validação - force
                for (int i = 0;i <= 2; i++)
                { 
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

                            response.Dispose();
                            break;
                        }
                    }
                    catch (WebException wex)
                    {
                        status = string.Format("Erro (tent {0}) - {1}", (i+1), wex.Message);
                        color = ConsoleColor.Red;

                        Console.ForegroundColor = color;
                        Console.Write(status);
                        Console.ResetColor();
                        
                        //Qeubra de linha para os textos
                        if (i < 2)
                            Console.Write("\r\n");

                        wErr = 1;
                        Thread.Sleep(2000);
                    }
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
                        sErr = 1;
                        break;
                    case ServiceControllerStatus.StopPending:
                        status = "Pendente parar";
                        color = ConsoleColor.Yellow;
                        sErr = 1;
                        break;
                    case ServiceControllerStatus.Running:
                        status = "Executando";
                        color = ConsoleColor.Green;
                        sSuc = 1;
                        break;
                    case ServiceControllerStatus.ContinuePending:
                        status = "Pendente continuar";
                        color = ConsoleColor.Yellow;
                        sErr = 1;
                        break;
                    case ServiceControllerStatus.Paused:
                        status = "Pausado";
                        color = ConsoleColor.Red;
                        sErr = 1;
                        break;
                    case ServiceControllerStatus.PausePending:
                        status = "Pendente pausar";
                        color = ConsoleColor.Yellow;
                        sErr = 1;
                        break;
                    case ServiceControllerStatus.StartPending:
                        status = "Pendente iniciar";
                        color = ConsoleColor.Yellow;
                        sErr = 1;
                        break;
                }

                Console.ForegroundColor = color;
                Console.Write(status);
                Console.ResetColor();

                sc.Dispose();
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

        private static void checkPing(string ip, string qtd, out int pSuc, out int pErr, bool silenMode = false)
        {
            pSuc = 0;
            pErr = 0;

            string status = string.Empty;
            try
            {
                for (int i = 0; i < Int32.Parse(qtd); i++)
                {
                    string item = string.Concat(ip, ": ");
                    Console.ResetColor();
                    if (!silenMode)
                        Console.Write(item);

                    ConsoleColor color = ConsoleColor.White;

                    Ping pinger = null;
                    try
                    {
                        pinger = new Ping();
                        PingReply resp = pinger.Send(ip);

                        if (resp.Status == IPStatus.Success)
                        {
                            status = string.Concat(resp.Status.ToString(), " RTT=", resp.RoundtripTime, "ms TTL=", resp.Options.Ttl, "ms BYTES=", resp.Buffer.Length);

                            color = ConsoleColor.Green;

                            Console.ForegroundColor = color;
                            if (!silenMode)
                                Console.Write(status);
                            Console.ResetColor();

                            pSuc++;
                        }
                        else
                        {
                            status = string.Concat("Erro - ", resp.Status.ToString());
                            color = ConsoleColor.Red;

                            Console.ForegroundColor = color;
                            if (!silenMode)
                                Console.Write(status);
                            Console.ResetColor();

                            pErr++;
                        }
                    }
                    catch (PingException pex)
                    {
                        status = string.Concat("Erro - ", pex.Message);
                        color = ConsoleColor.Red;

                        Console.ForegroundColor = color;
                        if (!silenMode)
                            Console.Write(status);
                        Console.ResetColor();

                        pErr++;
                    }
                    finally
                    {
                        pinger.Dispose();
                    }

                    if (!silenMode)
                        Console.Write("\r\n");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (!silenMode)
                    Console.Write(ex.Message);
                Console.ResetColor();

                pErr = 1;

                if (!silenMode)
                    Console.Write("\r\n");
            }
        }
    }
}
