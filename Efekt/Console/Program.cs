using System;

namespace Efekt
{
    public static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                Tests.Tests.RunAllTests();
               
                if (args.Length == 0)
                {
                    Console.WriteLine("Efekt interpreter. pass file(s) and folder(s) arguments to evaluate.");

                    Console.Read(); return 1;
                }

                var cw = new ConsoleWriter();
                var prog = Prog.Load2(cw, cw, args);
                var res = prog.Run();
                if (res != Void.Instance)
                    prog.OutputPrinter.Write(res);
                
                return 0;
            }
            catch (EfektProgramException)
            {
                return 3;
            }
            catch (EfektException)
            {
                return 2;
            }/*
            catch (Exception ex)
            {
                Console.Write(ex.Message + ex.StackTrace);
                return 1;
            }*/
        }
    }
}