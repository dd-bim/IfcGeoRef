using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace IfcGeoRefChecker.IO
{
    public class LogOutput
    {
        public void WriteLogfile(List<string> log, string file)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            using(var writeLog = File.CreateText((@".\results\GeoRef_" + file + ".txt")))
            {
                try
                {
                    foreach(var entry in log)
                    {
                        writeLog.WriteLine(entry);
                    }
                }

                catch(Exception ex)
                {
                    writeLog.WriteLine($"Error occured while writing Logfile. \r\n Message: {ex.Message} \r\n Stack: {ex.StackTrace}");
                }
            };
        }
    }
}