using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;

namespace pkmonhexeditor
{
    public partial class Form1 : Form
    {
        protected enum Game
        {
            EMER,
            SAPP,
            RUBY,
            NONE
        }
        private Dictionary<Game, int> offsets = new Dictionary<Game, int>{[Game.EMER] = 0x5B1DF8, [Game.RUBY] = 0x3F76E0, [Game.SAPP] = 0x3F771C};
        //these offsets might vary by rom
        private int currentoffset = 0;
        private string tfilename = null;
        private bool validloaded = false;

        public Form1()
        {
            using (StreamReader r = new StreamReader("bytedata.json"))
            {
                string json = r.ReadToEnd();

                this.ByteData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                this.ByteList = ByteData.Keys.ToArray().ToList();

            }
            
            InitializeComponent();
            //get another dict so we can read the bytes on the rom
            this.pkmonData = this.ByteData.Keys.ToDictionary(x => Convert.ToInt32(ByteData[x], 16), x => x);
            //set the list to all of them
            this.comboBox1.DataSource = new List<string>(ByteList);
            this.comboBox2.DataSource = new List<string>(ByteList);
            this.comboBox3.DataSource = new List<string>(ByteList);


        }
        



        private void loadrom_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();

            DialogResult result = fdlg.ShowDialog(); 

            if (result == DialogResult.OK) 
            {
                tfilename = fdlg.FileName;

                label2.Text = tfilename;

                Game game = checkFile(tfilename);

                string loadedtext = "Not loaded";

                switch (game)
                {
                    case Game.EMER:
                        loadedtext = "Emerald loaded!";
                        validloaded = true;
                        currentoffset = offsets[game];
                        break;
                    case Game.RUBY:
                        loadedtext = "Ruby loaded!";
                        validloaded = true;
                        currentoffset = offsets[game];
                        break;
                    case Game.SAPP:
                        loadedtext = "Sapphire loaded!";
                        validloaded = true;
                        currentoffset = offsets[game];
                        break;
                    case Game.NONE:
                        loadedtext = "File not RSE roms!";
                        currentoffset = 0;
                        validloaded = false;
                        break;             
                }

                setDefault();

                label1.Text = loadedtext;

            }

        }



        private Game checkFile(string filename)
        {
            string check = "None";
            
            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open))) //opening file stream
            {

                byte[] store = new byte[4];
                reader.BaseStream.Seek(0xA8, SeekOrigin.Begin);
                reader.Read(store, 0x0, 0x4); //check if these bytes are EMER, RUBY or SAPP
                check = System.Text.Encoding.Default.GetString(store);
                reader.Dispose();

                if (new List<string>{ "EMER", "RUBY", "SAPP"}.Contains(check))
                {
                    return (Game)Enum.Parse(typeof(Game), check);
                }
                else
                {
                    return Game.NONE;
                }

                
                    
            }
           
            
        }

        private void setDefault()
        {
            //set default to each of them by reading the rom
            this.comboBox1.SelectedIndex = this.comboBox1.FindString(readStarter(0));
            this.comboBox2.SelectedIndex = this.comboBox2.FindString(readStarter(2));
            this.comboBox3.SelectedIndex = this.comboBox3.FindString(readStarter(4));
        }


        private void comboBox1_Leave(object sender, EventArgs e)
        {
            if (!comboBox1.Items.Contains(comboBox1.Text)) //making sure the text in the box is valid since you can autocomplete
            {
                MessageBox.Show("Not valid Pokemon name", "Invalid Pokemon name");
                comboBox1.SelectedIndex = 0;
            }
        }

        private void comboBox2_Leave(object sender, EventArgs e)
        {
            if (!comboBox2.Items.Contains(comboBox2.Text)) 
            {
                MessageBox.Show("Not valid Pokemon name", "Invalid Pokemon name");
                comboBox2.SelectedIndex = 0;
            }
        }

        private void comboBox3_Leave(object sender, EventArgs e)
        {
            if (!comboBox3.Items.Contains(comboBox3.Text)) 
            {
                MessageBox.Show("Not valid Pokemon name", "Invalid Pokemon name");
                comboBox3.SelectedIndex = 0;
            }
        }


        private Tuple<byte, byte> parseHexval(string hex)
        {
            string first = hex.Substring(0, 4);

            string second =  "0x" + hex.Substring(4, 2);

            //tuples are fun

            return new Tuple<byte, byte>(Convert.ToByte(first, 16), Convert.ToByte(second, 16));
        }


        private void writeStarter(int extraoffset, string order, string combo)
        {
            if (!validloaded)
            {
                MessageBox.Show("Please load in valid Pokemon R/S/E ROM", "Invalid ROM");
            }
            else
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(tfilename, FileMode.Open))) //opening file stream
                {
                    var newoffset = currentoffset + extraoffset;

                    writer.BaseStream.Seek(newoffset, SeekOrigin.Begin);
                    string hexval = ByteData[combo];

                    Tuple<byte, byte> bytes = parseHexval(hexval);

                    writer.Write(bytes.Item1);
                    writer.Write(bytes.Item2);
                    writer.Dispose();
                    
                    MessageBox.Show($"{combo} written as {order} starter with bytes {hexval}", "Success!");
                }
            }
        }


        private int BytesToInt(byte[] starter)
        {
            string[] str = new string[2];
            str[0] = starter[0].ToString("X");
            str[1] = starter[1].ToString("X");

            for (int i = 0; i < 2; i++)
            {
                if (str[i].Length == 1)
                {
                    str[i] = "0" + str[i];
                }
            }
            string hv = "0x" + str[0] + str[1];
            int val = Convert.ToInt32(hv, 16);
            return val;

        }

        private string readStarter(int extraoffset)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(tfilename, FileMode.Open)))
            {

                reader.BaseStream.Seek(currentoffset + extraoffset, SeekOrigin.Begin);
                byte[] starter = reader.ReadBytes(2);


                int pkmonkey = BytesToInt(starter);
                reader.Dispose();
                //if the key is invalid then default to nothing
                try
                {
                    return pkmonData[pkmonkey];
                }
                catch (KeyNotFoundException e)
                {
                    return "";
                }
                
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            writeStarter(0, "first", comboBox1.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            writeStarter(2, "second", comboBox2.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            writeStarter(4, "third", comboBox3.Text);
        }



    }
}
