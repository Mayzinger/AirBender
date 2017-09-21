﻿using Topshelf;

namespace SokkaServer
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>                                 
            {
                x.Service<SokkaService>(s =>                     
                {
                    s.ConstructUsing(name => new SokkaService());
                    s.WhenStarted(tc => tc.Start());             
                    s.WhenStopped(tc => tc.Stop());              
                });
                x.RunAsLocalSystem();                            

                x.SetDescription("Enables communication with AirBender Bluetooth Host Devices.");        
                x.SetDisplayName("SokkaServer");                       
                x.SetServiceName("SokkaServer");                                
            });
        }
    }
}
