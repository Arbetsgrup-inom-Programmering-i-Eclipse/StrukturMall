using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace StrukturMall
{
    public class Program
    {
        public static void Run(StructureSet strukturSet, Patient pat)
        {
            if (strukturSet != null)
            {
                MainForm main = new MainForm(strukturSet, pat);
                main.ShowDialog();
                //System.Windows.Forms.Application.Run(new MainForm(strukturSet, pat));
            }
            else
            {
                MessageBox.Show("Välj ett strukturset!");
            }
        }
    }
}
