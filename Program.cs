using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
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

                string item = string.Concat(info[0], " - ", info[1], ": ");
                Console.ResetColor();
                Console.Write(item);

                string status = string.Empty;
                ConsoleColor color = ConsoleColor.White;
                try
                {
                    ServiceController sc = new ServiceController(info[1], info[0]);
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
                    Console.Write(ex.InnerException);
                    Console.ResetColor();
                }

                Console.Write("\r\n");
            }

            //Finalização
            Console.WriteLine("");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine(string.Concat("Finalizado - ", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
            Console.WriteLine("-----------------------------------");

            Console.ReadKey();
        }
    }
}
