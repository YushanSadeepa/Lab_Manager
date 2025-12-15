using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Lab_Coordinator
{
    public partial class Form1 : Form
    {
        private string imagePath = "";
        private byte[] profileData = null;
        private string loggedRegNumber = "";

        private string filename;
        private byte[] data = null;

        SqlConnection con = new SqlConnection(
            "Data Source=DESKTOP-D5GPCJD\\SQLEXPRESS;Initial Catalog=Labmanager;Integrated Security=True;TrustServerCertificate=True"
        );

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel2.Visible = false;
            panel3.Visible = false;
        }

        private void panel1_Paint(object sender, PaintEventArgs e) { }

        // ===============================
        // HELPER: Correct image rotation
        // ===============================
        private Image CorrectImageRotation(Image img)
        {
            const int exifOrientationID = 0x112; // 274

            if (!img.PropertyIdList.Contains(exifOrientationID))
                return img;

            var prop = img.GetPropertyItem(exifOrientationID);
            int val = BitConverter.ToUInt16(prop.Value, 0);
            RotateFlipType rotateFlip = RotateFlipType.RotateNoneFlipNone;

            switch (val)
            {
                case 1: rotateFlip = RotateFlipType.RotateNoneFlipNone; break;
                case 2: rotateFlip = RotateFlipType.RotateNoneFlipX; break;
                case 3: rotateFlip = RotateFlipType.Rotate180FlipNone; break;
                case 4: rotateFlip = RotateFlipType.Rotate180FlipX; break;
                case 5: rotateFlip = RotateFlipType.Rotate90FlipX; break;
                case 6: rotateFlip = RotateFlipType.Rotate90FlipNone; break;
                case 7: rotateFlip = RotateFlipType.Rotate270FlipX; break;
                case 8: rotateFlip = RotateFlipType.Rotate270FlipNone; break;
            }

            if (rotateFlip != RotateFlipType.RotateNoneFlipNone)
                img.RotateFlip(rotateFlip);

            return img;
        }

        // ===============================
        // OPEN REGISTRATION PANEL
        // ===============================
        private void button2_Click(object sender, EventArgs e)
        {
            panel1.Visible = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
        }

        // ===============================
        // REGISTER BUTTON (button3)
        // ===============================
        private void button3_Click(object sender, EventArgs e)
        {
            string name = textBox3.Text;
            string reg = textBox5.Text;
            string year = comboBox1.Text;
            string email = textBox6.Text;

            if (name == "" || reg == "" || email == "")
            {
                MessageBox.Show("All fields required!");
                return;
            }

            if (profileData == null)
            {
                MessageBox.Show("Please browse and select a profile picture!");
                return;
            }

            con.Open();

            string q = "INSERT INTO registration (Name, Regnumber, Year, Email, profileimage) " +
                       "VALUES (@name, @reg, @year, @email, @pic)";

            SqlCommand cmd = new SqlCommand(q, con);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@reg", reg);
            cmd.Parameters.AddWithValue("@year", year);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@pic", profileData);

            cmd.ExecuteNonQuery();
            con.Close();

            MessageBox.Show("Registration Successful!");
            panel1.Visible = false;
        }

        // ===============================
        // LOGIN BUTTON (button1)
        // ===============================
        private void button1_Click(object sender, EventArgs e)
        {
            string name = textBox1.Text;
            string reg = textBox2.Text;

            con.Open();

            string q = "SELECT * FROM registration WHERE Name=@n AND Regnumber=@r";
            SqlCommand cmd = new SqlCommand(q, con);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@r", reg);

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            con.Close();

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Invalid login!");
                return;
            }

            MessageBox.Show("Login Successful!");

            // Logged-in student info
            loggedRegNumber = dt.Rows[0]["Regnumber"].ToString();
            label8.Text = dt.Rows[0]["Name"].ToString();

            // Load profile picture
            if (dt.Rows[0]["profileimage"] != DBNull.Value)
            {
                byte[] imgData = (byte[])dt.Rows[0]["profileimage"];
                using (MemoryStream ms = new MemoryStream(imgData))
                {
                    Image img = Image.FromStream(ms);
                    Image correctedImg = CorrectImageRotation(img);

                    pictureBox2.Image = correctedImg;
                    button8.BackgroundImage = correctedImg;
                    button8.BackgroundImageLayout = ImageLayout.Stretch;
                }
            }

            panel2.Visible = true;
        }

        // ===============================
        // BROWSE PROFILE PIC (button7)
        // ===============================
        private void button7_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select Profile Picture";
            dlg.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                imagePath = dlg.FileName;
                profileData = File.ReadAllBytes(imagePath);

                Image img = Image.FromFile(imagePath);
                pictureBox2.Image = CorrectImageRotation(img);

                MessageBox.Show("Profile Picture Loaded!");
            }
        }

        // ===============================
        // BROWSE REPORT FILE (button6)
        // ===============================
        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog doc = new OpenFileDialog();
            doc.Title = "Select Document";
            doc.Filter = "All Files(*.*)|*.*";

            if (doc.ShowDialog() == DialogResult.OK)
            {
                filename = Path.GetFileName(doc.FileName);
                textBox9.Text = doc.FileName;
                data = File.ReadAllBytes(doc.FileName);

                MessageBox.Show("File selected successfully!");
            }
        }

        // ===============================
        // SUBMIT REPORT (button5)
        // ===============================
        private void button5_Click(object sender, EventArgs e)
        {
            string expno = textBox8.Text;
            string date = dateTimePicker1.Text;
            string expname = textBox7.Text;

            if (expno == "" || expname == "" || date == "")
            {
                MessageBox.Show("Please fill all report submission details!", "Error");
                return;
            }

            if (string.IsNullOrEmpty(filename) || data == null)
            {
                MessageBox.Show("Please add file first!", "Error");
                return;
            }

            if (string.IsNullOrEmpty(loggedRegNumber))
            {
                MessageBox.Show("Please login first!", "Error");
                return;
            }

            con.Open();

            string query2 = "INSERT INTO reportsubmission (Regnumber, ExperimentNo, ExperimentName, SubmissionDate, Filename, Filedata) " +
                            "VALUES (@reg, @expno, @expname, @date, @filename, @data)";

            SqlCommand comd = new SqlCommand(query2, con);
            comd.Parameters.AddWithValue("@reg", loggedRegNumber); 
            comd.Parameters.AddWithValue("@expno", expno);
            comd.Parameters.AddWithValue("@expname", expname);
            comd.Parameters.AddWithValue("@date", date);
            comd.Parameters.AddWithValue("@filename", filename);
            comd.Parameters.AddWithValue("@data", data);

            comd.ExecuteNonQuery();
            con.Close();

            MessageBox.Show("Report submitted!");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            
        }
        
        private void button8_Click_1(object sender, EventArgs e)
        {

        }
    }
}
