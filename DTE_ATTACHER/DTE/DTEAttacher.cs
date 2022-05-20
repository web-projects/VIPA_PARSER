﻿using EnvDTE80;
using System.Linq;

namespace DTE_ATTACHER.DTE
{
    public static class DTEAttacher
    {
        public static bool Attach(string targetProcess)
        {
            bool result = false;

            try
            {
                DTE2 dte = GetCurrent();

                EnvDTE.Processes processes = dte.Debugger.LocalProcesses;

                foreach (EnvDTE.Process process in processes.Cast<EnvDTE.Process>().Where(proc => proc.Name.IndexOf(targetProcess) != -1))
                {
                    process.Attach();
                    result = true;
                }
            }
            catch
            {

            }

            return result;
        }

        internal static DTE2 GetCurrent()
        {
            DTE2 dte2 = (DTE2)Marshal2.GetActiveObject("VisualStudio.DTE.16.0"); // For VisualStudio 2019
            return dte2;
        }
    }
}
