using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace IfcGeoRefChecker.IO
{
    public class LogOutput
    {
        public void WriteLogfile(List<string> log, string file, string direc)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            string dashline = "\r\n----------------------------------------------------------------------------------------------------------------------------------------";
            var headline = $"\r\nExamination of {file}.ifc regarding georeferencing content ({DateTime.Now.ToShortDateString()}, {DateTime.Now.ToLongTimeString()})" + dashline + dashline + "\r\n";

            using(var writeLog = File.CreateText((direc+ "\\" + file + ".txt")))
            {
                try
                {
                    writeLog.WriteLine(headline);

                    foreach(var entry in log)
                    {
                        writeLog.WriteLine(entry);
                    }
                }

                catch(Exception ex)
                {
                    writeLog.WriteLine($"Error occured while writing Logfile. \r\n Message: {ex.Message}");
                }
            };
        }
    }
}