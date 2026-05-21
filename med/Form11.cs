using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace med
{
    public partial class Form11 : Form
    {
        public Form11()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form10 newForm = new Form10();
            this.Hide();
            newForm.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form12 newsecondForm = new Form12();
            this.Hide();
            newsecondForm.Show();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form13 newthirtForm = new Form13();
            this.Hide();
            newthirtForm.Show();
        }
    }
}
