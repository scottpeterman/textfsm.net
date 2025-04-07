using System;
using TextFSM;  // This assumes your library's namespace is TextFSM, not TextFSMLibrary

namespace TextFSMLibrary.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing TextFSM implementation...");
            
            // Define a template for testing
            string template = @"Value Interface (\S+)
Value IP_Address (\S+)
Value OK (\S+)
Value Method (\S+)
Value Status (.+?)
Value Protocol (\S+)

Start
  ^${Interface}\s+${IP_Address}\s+${OK}\s+${Method}\s+${Status}\s+${Protocol}\s*$$ -> Record
";

            // Sample data
            string data = @"Interface                  IP-Address      OK? Method Status                Protocol
FastEthernet0/0            192.168.1.1     YES NVRAM  up                    up      
FastEthernet0/1            unassigned      YES NVRAM  administratively down down    
FastEthernet0/2            192.168.2.1     YES NVRAM  up                    up";

            try
            {
                // Create TextFSM instance
                var fsm = new TextFSM.TextFSM(template);
                
                // Parse data
                var results = fsm.ParseTextToDicts(data);
                
                // Display results
                Console.WriteLine($"Found {results.Count} interfaces:");
                foreach (var record in results)
                {
                    Console.WriteLine(record["Interface"]);
                }
                
                Console.WriteLine("Test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}