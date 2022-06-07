using System.Text;

namespace GameFactoryImgExtract
{
    public partial class Form1 : Form
    {
        ImgFile? imgFile;
        int image = 0;

        public Form1()
        {
            InitializeComponent();
            pictureBoxWithInterpolationMode1.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            pictureBoxWithInterpolationMode2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        }

        private void LoadImgFile(string imageFlieName, string paletteFileName)
        {
            imgFile = new ImgFile(imageFlieName);
            Palette palette = new();
            palette.Load(paletteFileName);
            imgFile.Load();
            imgFile.palette = palette;
            image = 0;
            LoadImage(image);
        }


        private bool LoadImage(int imgId)
        {
            if (imgFile == null) return false;
            EncodedImage encodedImage = imgFile.GetImage(imgId);
            if (encodedImage.CreationInfo.FileLen == 0)
            {
                label1.Text = $"Image: {image}/{imgFile.ImageCount}\n" +
                $"Null Image";
                return true;
            }

            string error = "";
            (Image decodedImage, Exception? ex) = encodedImage.GetImage();
            pictureBoxWithInterpolationMode1.Image = decodedImage;
            pictureBoxWithInterpolationMode2.Image = decodedImage;
            if (ex != null) error = ex.Message;
            label1.Text = $"Image: {image}/{imgFile.ImageCount}\n" +
                $"Width: {encodedImage.Size.Width}x{encodedImage.Size.Height}\n" +
                $"File Size: {encodedImage.Header.fileLength}\n" +
                $"Color Mode: {encodedImage.Header.colorMode}\n" +
                $"Compression: {(encodedImage.Header.CompressionTGF ? "TGF" : (encodedImage.Header.CompressionRLE ? "RLE" : "None"))}\n" +
                $"Successfully Loaded: {(encodedImage.IsLoaded ? "Yes" : "No")}" +
                $"{(string.IsNullOrWhiteSpace(error) ? "" : "\n" + error)}";
            return string.IsNullOrWhiteSpace(error);
        }


        private void Button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                CheckFileExists = true,
                Filter = "Image files (*.img)|*.img|All files (*.*)|*.*",
                Multiselect = false,
                Title = "Open Image File"
            };
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string imgfile = dialog.FileName;

            dialog = new()
            {
                CheckFileExists = true,
                Filter = "Pallete files (*.pal)|*.pal|All files (*.*)|*.*",
                Multiselect = false,
                Title = "Open Palette File"
            };
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string palfile = dialog.FileName;

            LoadImgFile(imgfile, palfile);

            EnableAllButtons();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (imgFile == null) return;
            if (image > 0) image--;
            LoadImage(image);
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            if (imgFile == null) return;
            if (image < imgFile.ImageCount - 1) image++;
            LoadImage(image);
        }

        private void SaveFiles(string path)
        {
            if (imgFile == null)
            {
                EnableAllButtons();
                return;
            }
            string log = "";
            int img = 0;
            this.Invoke(() => { progressBar1.Maximum = (int)imgFile.ImageCount; });
            do
            {
                this.Invoke(() => { progressBar1.Value = img; });
                EncodedImage encodedImage = imgFile.GetImage(img);
                if (encodedImage.CreationInfo.FileLen == 0)
                {
                    log += $"{DateTime.Now:g} : ID {img}/{imgFile.ImageCount} empty, skipping\n";
                    img++;
                    continue;
                }

                string file = path + $"\\img{img.ToString().PadLeft(5, '0')}.png";
                if (File.Exists(file)) File.Delete(file);
                (Image bmp, Exception? ex) = encodedImage.GetImage();
                if (ex != null)
                {
                    log += $"{DateTime.Now:g} : ID {img}/{imgFile.ImageCount} broken, saving anyway: ({ex.Message})\n";
                }
                bmp.Save(file);
                bmp.Dispose();
                img++;
            } while (img < imgFile.ImageCount);
            if (!string.IsNullOrWhiteSpace(log))
            {
                File.WriteAllText(path + "\\log.txt", log);
            }
            this.Invoke(() => { progressBar1.Value = 0; });
            this.Invoke(EnableAllButtons);
            if (!string.IsNullOrWhiteSpace(log))
            {
                bool msgR = false;
                this.Invoke(() =>
                {
                    if (MessageBox.Show("Log was created, do you want to open it now?", "Log created", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        msgR = true;
                });
                if (msgR)
                {
                    new System.Diagnostics.Process
                    {
                        StartInfo = new(path + "\\log.txt")
                        {
                            UseShellExecute = true
                        }
                    }.Start();
                }
            }
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            if (imgFile == null) return;
            FolderBrowserDialog dialog = new();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                DisableAllButtons();
                Task task = new Task(() => { SaveFiles(dialog.SelectedPath); });
                task.Start();
            }
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            if (imgFile == null) return;
            EncodedImage encodedImage = imgFile.GetImage(image);
            if (encodedImage.CreationInfo.FileLen == 0)
            {
                return;
            }
            SaveFileDialog dialog = new()
            {
                Filter = "Image file (*.png)|*.png",
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(dialog.FileName)) File.Delete(dialog.FileName);
                ((Bitmap)encodedImage.GetImage().img).Save(dialog.FileName);
            }
        }

        private void DisableAllButtons()
        {
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
        }

        private void EnableAllButtons()
        {
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
        }
    }
}