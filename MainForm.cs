using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using VMS.TPS.Common.Model.API;

namespace StrukturMall
{
    public partial class MainForm : Form
    {
        private StrukturManipulering sm;
        private Patient _pat;
        private StructureSet _strSet;
        public MainForm(StructureSet strSet, Patient pat)
        {
            InitializeComponent();
            this._pat = pat;
            this._strSet = strSet;
            sm = new StrukturManipulering(_pat);
            InitializeGUI();
        }

        private void InitializeGUI()
        {
            //Clear textboxes etc
            cB_strukturmall.Items.Clear();
            List<string> templates = sm.GetAllTemplateNames();
            cB_strukturmall.DataSource = templates;  
        }

        private void btn_Ok_Click(object sender, EventArgs e)
        {
            string fileName = cB_strukturmall.SelectedItem + ".xml";
            StructureTemplates curr_structTemp = sm.ImportStructureTemplate(fileName);
            StructureSet dtDos = sm.FindARTPlanStructureSet(_pat, _strSet);

            sm.CopyStructuresToDTDos(dtDos, curr_structTemp);
            sm.ColorCorrector(dtDos, curr_structTemp);
            MessageBox.Show("Klart!");

            //Close();
        }
    }
}