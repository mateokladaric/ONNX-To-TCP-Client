using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FaceONNX;
using UMapx.Imaging;

namespace ONYX
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // task
            Task.Run(() =>
            {
                FaceONNX.FaceDetector faceDetector = new FaceONNX.FaceDetector();
                FaceONNX.FaceLandmarksExtractor faceLandmarksExtractor = new FaceONNX.FaceLandmarksExtractor();

                // tcp server
                TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 12456);
                server.Start();

                // accept client
                TcpClient client = server.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                while (true)
                {
                    // screenshot of the primary screen
                    Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                    Graphics graphics = Graphics.FromImage(bitmap as Image);
                    graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);

                    // faces
                    var faces = faceDetector.Forward(bitmap);

                    // rectangles around faces
                    foreach (var face in faces)
                    {
                        graphics.DrawRectangle(new Pen(Color.Red, 2), face);
                    }

                    if (faces.Length > 0)
                    {
                        // landmarks
                        var crop = BitmapTransform.Crop(bitmap, faces[0]);
                        var points = faceLandmarksExtractor.Forward(crop);

                        // all of the points into a string and sent to the client
                        string data = "";
                        foreach (var point in points)
                        {
                            data += point.X + "," + point.Y + ",";
                        }
                        data = data.Substring(0, data.Length - 1);
                        byte[] bytes = Encoding.ASCII.GetBytes(data);
                        stream.Write(bytes, 0, bytes.Length);


                        // drawing landmarks
                        foreach (var point in points)
                        {
                            graphics.FillEllipse(new SolidBrush(Color.Red), faces[0].Left + point.X, faces[0].Top + point.Y, 2, 2);
                        }

                        // showing result
                        pictureBox1.Image = bitmap;
                    }
                    else
                    {
                        // Sending "No Data" to client
                        byte[] data = Encoding.ASCII.GetBytes("No Data");
                        stream.Write(data, 0, data.Length);
                    }

                    // 100 ms delay
                    System.Threading.Thread.Sleep(1000);
                }
            });
        }
    }
}
