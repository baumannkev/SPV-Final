﻿using Microsoft.Office.Tools.Ribbon;
using System;

namespace SPV_Project_Add_In
{
    partial class ExportToSPV : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public ExportToSPV()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tab1 = this.Factory.CreateRibbonTab();
            this.group1 = this.Factory.CreateRibbonGroup();
            this.btnExportToSPV = this.Factory.CreateRibbonButton();
            this.tab1.SuspendLayout();
            this.group1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tab1
            // 
            this.tab1.ControlId.ControlIdType = Microsoft.Office.Tools.Ribbon.RibbonControlIdType.Office;
            this.tab1.Groups.Add(this.group1);
            this.tab1.Label = "TabAddIns";
            this.tab1.Name = "tab1";
            // 
            // group1
            // 
            this.group1.Items.Add(this.btnExportToSPV);
            this.group1.Name = "group1";
            // 
            // btnExportToSPV
            // 
            this.btnExportToSPV.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnExportToSPV.Image = global::SPV_Project_Add_In.Properties.Resources.baumann_associates_logo;
            this.btnExportToSPV.Label = "SPV Visualizer";
            this.btnExportToSPV.Name = "btnExportToSPV";
            this.btnExportToSPV.ShowImage = true;
            this.btnExportToSPV.SuperTip = "Export tasks to SPV Web";
            this.btnExportToSPV.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnExportToSPV_Click);
            // 
            // ExportToSPV
            // 
            this.Name = "ExportToSPV";
            this.RibbonType = "Microsoft.Project.Project";
            this.Tabs.Add(this.tab1);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.Ribbon1_Load);
            this.tab1.ResumeLayout(false);
            this.tab1.PerformLayout();
            this.group1.ResumeLayout(false);
            this.group1.PerformLayout();
            this.ResumeLayout(false);

        }


        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab tab1;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup group1;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnExportToSPV;
    }

    partial class ThisRibbonCollection
    {
        internal ExportToSPV Ribbon1
        {
            get { return this.GetRibbon<ExportToSPV>(); }
        }
    }
}
