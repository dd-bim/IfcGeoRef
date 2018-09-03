using System;
using System.IO;
using System.Windows.Forms;
using Xbim.Ifc;

namespace IFCGeoRefChecker
{
    public partial class Form_Program : Form
    {
        private IfcStore modelObj;
        private string modelFile;

        private GeoRefChecker checkObj = new GeoRefChecker();

        public Form_Program()
        {
            InitializeComponent();
        }

        private void ifcImport_Click(object sender, EventArgs e)
        {
            var model = new Import();

            this.modelObj = model.IfcModel;

            this.modelFile = model.IfcFile;

            this.label2.Text = $"{this.modelFile}.ifc";

            this.label4.Text = "not checked";
            this.label10.Text = "?";
            this.label11.Text = "?";
            this.label12.Text = "?";
            this.label13.Text = "?";
            this.label14.Text = "?";
        }

        private void checkGeoRef_Click(object sender, EventArgs e)
        {
            this.checkObj.Result = "";

            this.checkObj.GetAddress(this.modelObj);
            this.label10.Text = (this.checkObj.Error == true) ? "Error occured" : ((this.checkObj.GeoRef == true) ? "true" : "false");

            this.checkObj.GetLatLon(this.modelObj);
            this.label11.Text = (this.checkObj.Error == true) ? "Error occured" : ((this.checkObj.GeoRef == true) ? "true" : "false");

            this.checkObj.GetSitePlcm(this.modelObj);
            this.label12.Text = (this.checkObj.Error == true) ? "Error occured" : ((this.checkObj.GeoRef == true) ? "true" : "false");

            this.checkObj.GetWorldCoordinateSystem(this.modelObj);
            this.label13.Text = (this.checkObj.Error == true) ? "Error occured" : ((this.checkObj.GeoRef == true) ? "true" : "false");

            this.checkObj.GetMapConversion(this.modelObj);
            this.label14.Text = (this.checkObj.Error == true) ? "Error occured" : ((this.checkObj.GeoRef == true) ? "true" : "false");

            using(var writeLog = File.CreateText((@".\results\GeoRef_" + this.modelFile + ".txt")))

            {
                writeLog.WriteLine(
                    $"\r\nExamination of \"{this.modelFile}.ifc\" regarding georeferencing content ({DateTime.Now.ToShortDateString()}, {DateTime.Now.ToLongTimeString()})" +
                    "\r\n----------------------------------------------------------------------------------------------------------------------------------------" +
                    "\r\n----------------------------------------------------------------------------------------------------------------------------------------"
                    );

                writeLog.WriteLine(this.checkObj.Result);

                this.label4.Text = "Done.";
            }
        }

        private void quit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@".\results\GeoRef_" + this.modelFile + ".txt");
            }
            catch
            {
                MessageBox.Show("Error occured. Please check application directory for the folder \"results\" and the corresponding file.");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var info = new Form_Info();

            info.Show();
        }
    }
}