using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Tobii.StreamEngine;
using LinuxProEye.Interface;

namespace LinuxProEye
{
    public class MainProgram
    {
        public static void Main(string[] args)
        {
            LinuxProEyeInterface lpe = new LinuxProEyeInterface();

            if (!lpe.Init())
            {
                Console.WriteLine("Could not init.");
            }

            lpe.EnumerateSupportedStreams();
            lpe.RegisterCallbacks();

            for (int i = 0; i < 1000; i++)
            {
                lpe.Update();
                Thread.Sleep(10);
            }

            lpe.Teardown();

        }
    }
}
