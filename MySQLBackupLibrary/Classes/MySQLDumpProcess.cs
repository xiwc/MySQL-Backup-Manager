﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQLBackupLibrary.Classes
{
    class MySQLDumpProcess
    {
        public Library library { get; set; }
        public string error { get; set; }
        public string output { get; set; }
        public string databaseName { get; set; }

        private bool isServerDown;

        public MySQLDumpProcess(string databaseName, Library library)
        {
            this.error = "";
            this.output = "";
            this.library = library;
            this.databaseName = databaseName;
            this.isServerDown = false;
        }

        /**
         * Process The MySQL Dump for each database
         */
        public void ProcessMySqlDump(Process process)
        {
            foreach (DatabaseInfo dbInfo in library.RetrieveAllDatabaseNodes())
            {
                System.Threading.Thread.Sleep(1000);   //Let Application Sleep for 1 second, preventing multiple backup executions of the same database.

                if (!isServerDown)
                {
                    string[] startTime = dbInfo.StartTime.ToString().Split(':');
                    if (Convert.ToInt32(startTime[0]) == DateTime.Now.Hour && Convert.ToInt32(startTime[1]) == DateTime.Now.Minute)
                    {
                        this.databaseName = dbInfo.DatabaseName;

                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = "mysqldump";
                        psi.RedirectStandardInput = false;
                        psi.RedirectStandardOutput = true;
                        psi.RedirectStandardError = true;
                        psi.StandardOutputEncoding = Encoding.UTF8;
                        psi.Arguments = string.Format(@"-u{0} -p{1} -h{2} --add-drop-database --add-drop-table --add-locks --comments --create-options --dump-date --lock-tables --databases {3}", dbInfo.User, dbInfo.Password, dbInfo.Host, this.databaseName);
                        psi.UseShellExecute = false;
                        psi.CreateNoWindow = true;

                        process = Process.Start(psi);

                        this.output = process.StandardOutput.ReadToEnd();
                        this.error = process.StandardError.ReadToEnd();

                        if (!this.HasErrorOccured(this.error))
                        {
                            library.WriteBackupFile(this.databaseName, this.output);
                            library.LogMessage("INFO", "Backup created of the database " + this.databaseName);
                        }
                    }
                }

                if (process != null)
                {
                    process.WaitForExit();
                }
            }
        }

        /**
         * Process The MySQL Dump for a single database
         */
        public void ProcessMySqlDump(Process process, string databaseName)
        {
            DatabaseInfo dbInfo = library.RetrieveDatabaseNode(databaseName);

            if (dbInfo != null)
            {
                if (!isServerDown)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "mysqldump";
                    psi.RedirectStandardInput = false;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    psi.StandardOutputEncoding = Encoding.UTF8;
                    psi.Arguments = string.Format(@"-u{0} -p{1} -h{2} --add-drop-database --add-drop-table --add-locks --comments --create-options --dump-date --lock-tables --databases {3}", dbInfo.User, dbInfo.Password, dbInfo.Host, this.databaseName);
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;

                    process = Process.Start(psi);

                    this.output = process.StandardOutput.ReadToEnd();
                    this.error = process.StandardError.ReadToEnd();

                    if (!this.HasErrorOccured(this.error))
                    {
                        library.WriteBackupFile(this.databaseName, this.output);
                        library.LogMessage("INFO", "Backup created of the database " + this.databaseName);
                    }
                }

                if (process != null)
                {
                    process.WaitForExit();
                }
            }
        }

        /**
         * Find out if an error has occured during the backup dump. Returns true if error has occured
         */
        private bool HasErrorOccured(string errorOutput)
        {
            bool errorOccured = false;

            //Can't find database error
            if (errorOutput.Contains("Got error: 1049"))
            {
                library.LogMessage("ERROR", errorOutput.Substring(errorOutput.IndexOf("Got error: 1049")));
                errorOccured = true;
            }
            //Can't find host error
            else if (errorOutput.Contains("Got error: 2005"))
            {
                library.LogMessage("ERROR", errorOutput.Substring(errorOutput.IndexOf("Got error: 2005")));
                errorOccured = true;
            }
            //Wrong user/password error
            else if (errorOutput.Contains("Got error: 1045"))
            {
                library.LogMessage("ERROR", errorOutput.Substring(errorOutput.IndexOf("Got error: 1045")));
                errorOccured = true;
            }
            //Can't connect to MySQL (probably is server down)
            else if (errorOutput.Contains("Got error: 2003"))
            {
                library.LogMessage("ERROR", errorOutput.Substring(errorOutput.IndexOf("Got error: 2003")).TrimEnd('\r', '\n'));
                this.isServerDown = true;
                errorOccured = true;
            }

            return errorOccured;
        }
    }
}
