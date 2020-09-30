using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioFileApp
{
    public partial class Form1 : Form
    {
        public static string outputLocation = "c:/config";
        public static string outputTrackFolder = "";
        public static string outputArtFolder = "";
        public static int multiple = 1000;
        public static string trackfile = "//Tracks_list.csv";
        private static bool isLoader = false;

        public static int totalLoad = 0;
        public static int currentLoad = 0;
        public static int isNew = 0;

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        //  albums dinctionary => (album number , album title)
        //  albumMusicCount dinctionary => (album no , total songs of albm)
        // albumTrackNames  dinctionary => (track name, album no)

        private void button1_Click(object sender, EventArgs e)
        {
            //Execution();
            //Thread ex = new Thread(Execution);
            //ex.Start();
            progressBar1.Value = 0;
            backgroundWorker1.RunWorkerAsync();
        }

        private void Execution()
        {
            int albNo = 1;
            timer1.Enabled = true;
            bool isError = false;

            outputLocation = outConfig.Text;
            outputArtFolder = outArt.Text + "//";
            outputTrackFolder = outTracks.Text + "//";

            string[] files = new string[] { "one", "two", "three" };
            string[] albumFolders = new string[] { "one", "two", "three" };
            isLoader = true;
            currentLoad = 0;
            totalLoad = 0;
            if (checkBox1.Checked)
            {
                isNew = 1;
            }
            else 
            {
                isNew = 0;
            }


            try
            {
                if (System.IO.Directory.Exists(textBox1.Text))
                {
                    files = System.IO.Directory.GetFiles(textBox1.Text);
                    totalLoad += files.Length;
                }
                if (System.IO.Directory.Exists(textBox2.Text))
                {
                    albumFolders = System.IO.Directory.GetDirectories(textBox2.Text);
                    totalLoad += albumFolders.Length;
                    
                }
                { // loader
                    //progressBar1.Maximum = 100;
                    //progressBar1.Step = 1;
                    //progressBar1.Value = 0;
                    //backgroundWorker1.RunWorkerAsync();
                }
                timer1.Enabled = true;
            }
            catch
            {
                MessageBox.Show("Invalid Location");
                Application.Exit();
            }

            {
                var csv = new StringBuilder();
                UpdateLoader();
                Dictionary<int, string> albums = new Dictionary<int, string>();
                Dictionary<int, int> albumMusicCount = new Dictionary<int, int>();
                Dictionary<string, int> albumTrackNames = new Dictionary<string, int>();
                if (System.IO.Directory.Exists(outputLocation))
                {
                    var trackLocation = outputLocation + trackfile;
                    var albumLocation = outputLocation + "//album_list.csv";
                    if (System.IO.File.Exists(trackLocation))
                    {
                        totalLoad += 50;
                        this.EntryOutput("Filling cache with previous track records");
                        FillCacheWithOldEntries(albums, albumMusicCount, albumTrackNames, trackLocation);
                        currentLoad += 50;
                    }
                    if (System.IO.File.Exists(albumLocation))
                    {
                        totalLoad += 50;
                        this.EntryOutput("Filling cache with previous album records");
                        //FillCacheWithOldEntries(albums, albumMusicCount, albumTrackNames, albumLocation);
                        currentLoad += 50;
                    }
                }
                else
                {
                    this.EntryOutput("Creating output directory");
                    System.IO.Directory.CreateDirectory(outputLocation);
                }
                if (files.Length > 0)
                {
                    timer1.Enabled = true;
                    this.EntryOutput("Working on tracks folder...");
                    ReadTagsAndCreateCsv(ref albNo, files, ref csv, albums, albumMusicCount, albumTrackNames, false);
                    // MessageBox.Show("Done!");


                }
                if (albumFolders.Length > 0)
                {
                    this.EntryOutput("Working on albums folder...");
                    foreach (var albumFold in albumFolders)
                    {
                        currentLoad += 1;
                        files = System.IO.Directory.GetFiles(albumFold);
                        totalLoad += files.Length;
                        try
                        {
                            timer1.Enabled = true;
                            ReadTagsAndCreateCsvForAlbum(ref albNo, files, ref csv, albums, albumMusicCount, albumTrackNames, true);
                        }
                        catch (Exception ex)
                        {
                            this.EntryOutput("stopped due to errors!");
                            MessageBox.Show(ex.ToString());
                            isError = true;
                            break;
                        }
                    }
                }

            }
            if (isError)
            {
                this.EntryOutput("Not Done.");
            }
            else
            {
                this.EntryOutput("Done!");
                MessageBox.Show("Done");
            }
            currentLoad = totalLoad;
            isLoader = false;
        }

        private static void FillCacheWithOldEntries(Dictionary<int, string> albums, Dictionary<int, int> albumMusicCount, Dictionary<string, int> albumTrackNames, string trackLocation)
        {
            var reader = new System.IO.StreamReader(System.IO.File.OpenRead(trackLocation));
            List<String> lines = new List<String>();
            int line_no = 1;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                lines.Add(line);
                if (line_no > 1)
                {
                    
                    var values = line.Split(',');
                    int track_num = Convert.ToInt32(values[1]);
                    string track_name = values[3];
                    var album_num = track_num / multiple;
                    int songCount = track_num % multiple;
                    string album_name = values[5];
                    if (albums.ContainsValue(album_name))
                    {
                        albumMusicCount[album_num] += 1;
                        albumTrackNames.Add(track_name, album_num);
                    }
                    else
                    {
                        albums.Add(album_num, album_name);
                        albumMusicCount.Add(album_num, songCount);
                        albumTrackNames.Add(track_name, album_num);
                    }
                }
                line_no += 1;
            }
            reader.Close();

            if (isNew == 1) 
            {
                using (StreamWriter writer = new StreamWriter(trackLocation, false))
                {
                    int x = 0;
                    foreach (String line in lines)
                    {
                        if (x > 0)
                        {
                            var values = line.Split(',');
                            values[7] = "0";
                            var nLine = String.Join(",", values);
                            writer.WriteLine(nLine);
                        }
                        else 
                        {
                            writer.WriteLine(line);
                        }
                        x++;
                    }
                }
            }
        }

        private static void ReadTagsAndCreateCsvForAlbum(ref int albNo, string[] files, ref StringBuilder csv, Dictionary<int, string> albums, Dictionary<int, int> albumMusicCount, Dictionary<string, int> albumTrackNames, bool isAlbum)
        {
      
            List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
            int numOfTracks = files.Where(x => x.Contains(".mp3")).Count();
            files = files.OrderByDescending(x => x).ToArray();
            //if (isAlbum)
            //{
            //    files = new string[1] { files[0] };
            //}

            
            var cou = files.Length;

            if (albums.Count > 0)
            {
                albNo = albums.Keys.Max() +1;
            }

            

            string album = "";
            bool albumFirstEntry = true;

            for (int x = 0; x < cou; x++)
            {
                currentLoad += 1;
                if (System.IO.File.Exists(files[x]) && files[x].IndexOf(".mp3") > 0)
                {
                    var track = albNo * multiple;
                    var file = TagLib.File.Create(files[x]);

                    if (album == "")
                    {
                        album = file.Tag.Album;
                        if (cou > 2)
                        {
                            var firstName = "";
                            var secondName = "";
                            if (files[0].IndexOf(".mp3") > 0)
                            {
                                var nexF = TagLib.File.Create(files[0]);
                                firstName = nexF.Tag.Album;
                            }
                            if (files[1].IndexOf(".mp3") > 0)
                            {
                                var nexF = TagLib.File.Create(files[1]);
                                if (firstName == "")
                                {
                                    firstName = nexF.Tag.Album;
                                }
                                else
                                {
                                    secondName = nexF.Tag.Album;
                                }
                            }
                            if (files[2].IndexOf(".mp3") > 0)
                            {
                                var nexF = TagLib.File.Create(files[2]);
                                secondName = nexF.Tag.Album;
                            }

                            if (firstName == secondName && firstName != null && firstName != "")
                            {
                                album = file.Tag.Album;
                            }
                            else
                            {
                                System.IO.FileInfo fInfo = new System.IO.FileInfo(files[x]);
                                album = fInfo.Directory.Name;
                            }

                        }
                        else if(album=="" || album==null)
                        {
                            System.IO.FileInfo fInfo = new System.IO.FileInfo(files[x]);
                            album = fInfo.Directory.Name;
                        }
                    }

                    var title = file.Tag.Title;
                    if (title != null)
                    {
                        title = FixString(title);
                    }
                    else
                    {
                        title = Path.GetFileName(files[x]);
                        title = FixString(title);
                    }

                    if (album != null)
                    {
                        album = FixString(album);
                        if (albums.ContainsValue(album))
                        {
                            albNo = albums.Where(j => j.Value == album).First().Key;
                            if(albumTrackNames.ContainsKey(title))
                            {
                                continue;
                                Form1 fo = new Form1();
                                fo.EntryOutput("File already exists, moving to next");
                            }
                            if (albumMusicCount.ContainsKey(albNo))
                            {
                                track = albNo * multiple + (albumMusicCount[albNo] + 1);
                                albumMusicCount[albNo] += 1;
                            }
                            else
                            {
                                track = albNo * multiple;
                                albumMusicCount.Add(albNo, 0);
                            }
                            
                            albumTrackNames.Add(title, albNo);
                        }
                        else
                        {
                            albums.Add(albNo, album);
                            if (albumTrackNames.ContainsKey(title))
                            {
                                continue;
                                Form1 fo = new Form1();
                                fo.EntryOutput("File already exists, moving to next");
                            }
                            else
                            {
                                albumTrackNames.Add(title, albNo);
                                track = albNo * multiple;
                                albumMusicCount.Add(albNo, 0);
                            }
                        }
                    }

                   
                    var genre = file.Tag.Genres.Length > 0 ? file.Tag.Genres[0] : "0";
                    if (genre != null)
                    {
                        genre = FixString(genre);
                    }

                    var artist = file.Tag.Artists.Length > 0 ? file.Tag.Artists[0] : "0";
                    if (artist != null)
                    {
                        artist = FixString(artist);
                    }

                    var art = albNo * multiple;

                    if (albumFirstEntry)
                    {
                        csv = WriteToCSV(csv, track, album, title, genre, artist, art, numOfTracks, false);
                        csv = WriteToCSV(csv, track, album, title, genre, artist, art, numOfTracks, true);
                        albumFirstEntry = false;
                    }
                    else 
                    {
                        csv = WriteToCSV(csv, track, album, title, genre, artist, art, numOfTracks, true);
                    }
                    
                    if(isAlbum)
                    {
                        
                        if (!System.IO.Directory.Exists(outputTrackFolder))
                        {
                            System.IO.Directory.CreateDirectory(outputTrackFolder);
                        }
                        var newFileCopied = outputTrackFolder + track + ".mp3";
                        if (!File.Exists(newFileCopied)) 
                        {
                            System.IO.File.Copy(files[x], newFileCopied);
                        }
                        

                        using (TagLib.File f = TagLib.File.Create(newFileCopied))
                        {
                            f.RemoveTags(TagLib.TagTypes.Id3v1);
                            f.RemoveTags(TagLib.TagTypes.Id3v2);
                            f.Save();
                        }
                    }
                }
                else if (ImageExtensions.Contains(System.IO.Path.GetExtension(files[x]).ToUpperInvariant()))
                {
                    if (!System.IO.Directory.Exists(outputArtFolder))
                    {
                        System.IO.Directory.CreateDirectory(outputArtFolder);
                    }
                    var art = albNo * multiple;
                    var artFileName = art + System.IO.Path.GetExtension(files[x]);
                    var artLoc = outputArtFolder + artFileName;
                    if (System.IO.File.Exists(artLoc))
                    { }
                    else
                    {
                        System.IO.File.Copy(files[x],artLoc);
                    }
                }

            }
        }

        private static void ReadTagsAndCreateCsv(ref int albNo, string[] files, ref StringBuilder csv, Dictionary<int, string> albums, Dictionary<int, int> albumMusicCount, Dictionary<string, int> albumTrackNames, bool isAlbum)
        {
         
            var cou = files.Length;
            for (int x = 0; x < cou; x++)
            {
                currentLoad += 1;
                if (System.IO.File.Exists(files[x]) && files[x].IndexOf(".mp3") > 0)
                {
                    if (albums.Count > 0)
                    {
                        albNo = albums.Keys.Max() + 1;
                    }
                    var track = albNo * multiple;
                    
                    try
                    {
                        var file = TagLib.File.Create(files[x]);
                        var album = file.Tag.Album;
                        var title = file.Tag.Title;
                        if (title != null)
                        {
                            title = FixString(title);
                        }
                        else
                        {
                            title = Path.GetFileName(files[x]);
                            title = title.ToUpper();
                            title = FixString(title); 
                        }


                        if (album != null)
                        {
                            album = FixString(album);
                            if (albums.ContainsValue(album))
                            {
                                albNo = albums.Where(j => j.Value == album).First().Key;
                                if (albumTrackNames.ContainsKey(title))
                                {
                                    Form1 fo = new Form1();
                                    fo.EntryOutput("File already exists, moving to next");
                                    continue;

                                } if (albumMusicCount.ContainsKey(albNo))
                                {
                                    track = albNo * multiple + (albumMusicCount[albNo] + 1);
                                    albumMusicCount[albNo] += 1;
                                }
                                else
                                {
                                    track = albNo * multiple;
                                    albumMusicCount.Add(albNo, 0);
                                }
                                albumTrackNames.Add(title, albNo);
                            }
                            else
                            {
                                if (albumTrackNames.ContainsKey(title))
                                {
                                    Form1 fo = new Form1();
                                    fo.EntryOutput("File already exists, moving to next");
                                    continue;
                                  
                                }
                                albums.Add(albNo, album);
                                track = albNo * multiple;
                                albumMusicCount.Add(albNo, 0);
                                albumTrackNames.Add(title, albNo);
                            }
                        }
                        else
                        {
                            album = title;
                            if (title != null)
                            {
                                if (albumTrackNames.ContainsKey(title))
                                {
                                    Form1 fo = new Form1();
                                    fo.EntryOutput("File already exists, moving to next");
                                    continue;
                                   
                                }
                                else
                                {
                                    albumTrackNames.Add(title, albNo);
                                }
                            }
                            albums.Add(albNo, album);
                            track = albNo * multiple;
                            albumMusicCount.Add(albNo, 0);

                        }


                        var genre = file.Tag.Genres.Length > 0 ? file.Tag.Genres[0] : "0";
                        if (genre != null)
                        {
                            genre = FixString(genre);
                        }

                        var artist = file.Tag.Artists.Length > 0 ? file.Tag.Artists[0] : "0";
                        if (artist != null)
                        {
                            artist = FixString(artist);
                        }

                        var art = 0;
                        if (file.Tag.Pictures.Length >= 1 && isAlbum)
                        {
                            var bin = (byte[])(file.Tag.Pictures[0].Data.Data);
                            art = albNo * multiple;
                        }

                        csv = WriteToCSV(csv, track, album, title, genre, artist, art);
                        if (!isAlbum)
                        {
                            if (!System.IO.Directory.Exists(outputTrackFolder))
                            {
                                System.IO.Directory.CreateDirectory(outputTrackFolder);
                            }
                            var newFileCopied = outputTrackFolder + track + ".mp3";
                            System.IO.File.Copy(files[x], newFileCopied);

                            using (TagLib.File f = TagLib.File.Create(newFileCopied))
                            {
                                f.RemoveTags(TagLib.TagTypes.Id3v1);
                                f.RemoveTags(TagLib.TagTypes.Id3v2);
                                f.Save();
                            }
                        } 
                    }
                    catch (Exception ex)
                    {
                        Form1 fo = new Form1();
                        fo.EntryOutput("Problem with file " + files[x] + " , moving to next");
                        continue;
                        
                    }
                    
                }
                else
                {
                    //                        MessageBox.Show("Invalid location!");
                }

            }
        }

        private static string FixString(string title)
        {
            title = title.ToUpper();
            string ret = Regex.Replace(title, @"[^0-9a-zA-Z']+", " ");
            //var chars = title.Split(' ');
            //string ret = chars[0];
            ////foreach (var item in chars) 
            //for (int i = 1; i < chars.Length;i++ )
            //{
            //    ret += " " + Regex.Replace(chars[i], @"[^0-9a-zA-Z]+", " ");
            //}
            return ret;
        }



        private static StringBuilder WriteToCSV(StringBuilder csv, int track, string album, string title, string genre, string artist, int art,int numOfTracks=1,bool isTrack=true)
        {
            

            string loc = outputLocation;
            if (isTrack)//(art == 0)
            {
                string newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", art.ToString(), track.ToString(), numOfTracks, title, artist, album, genre, isNew, Environment.NewLine);
                csv.Append(newLine);

                loc += trackfile;


                if (System.IO.File.Exists(loc) && csv.Length > 0)
                {
                    System.IO.File.AppendAllText(loc, csv.ToString());
                }
                else
                {
                    System.IO.File.WriteAllText(loc, "Art,Track,Number of Tracks,Title,Artist,Album,Genre,New \n".Replace("\n", Environment.NewLine));
                    System.IO.File.AppendAllText(loc, csv.ToString());
                }
                Form1 f = new Form1();
                f.EntryOutput("Track added : " + track);
            }
            else
            {
                string newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", track.ToString(), track.ToString(), numOfTracks.ToString(), title, artist, album, genre,isNew, Environment.NewLine);
                csv.Append(newLine);

                loc += "//album_list.csv";
                
                if (System.IO.File.Exists(loc) && csv.Length > 0)
                {
                    System.IO.File.AppendAllText(loc, csv.ToString());
                }
                else
                {
                    System.IO.File.WriteAllText(loc, "Id,Track,Number of Tracks,Title,Artist,Album Name,Genre,New \n".Replace("\n", Environment.NewLine));
                    System.IO.File.AppendAllText(loc, csv.ToString());
                }
            }
            
            
            csv = new StringBuilder();
            return csv;
        }

        private void EntryOutput(string entry)
        {
            //this.listBox1.Items.Add(entry);
            //this.listBox1.Refresh();

            Extn.SafeInvoke(listBox1, new Action(() => listBox1.Items.Add(entry)), false);
        }


        

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        

        private void button2_Click_1(object sender, EventArgs e)
        {
            totalLoad = 100;
            currentLoad = 0;
            timer1.Enabled = true;
            var a = outConfig.Text + trackfile;
            var b = outConfig.Text + "//album_list.csv";
            var master = new StringBuilder();
            this.EntryOutput("Reading Files...");
            ReadForMaster(a, master);
            currentLoad += 40;
            ReadForMaster(b, master);
            currentLoad += 40;
            var loc = outConfig.Text + "//Master_list.csv";
            if (System.IO.File.Exists(loc))
            {
                System.IO.File.Delete(loc);
            }
            System.IO.File.WriteAllText(loc, "Art,Track,Number of Tracks,Title,Artist,Album,Genre \n".Replace("\n", Environment.NewLine));
            System.IO.File.AppendAllText(loc, master.ToString());
            currentLoad += 20;
            this.EntryOutput("Master File created");
            timer1.Enabled = false;
        }

        private static void ReadForMaster(string a, StringBuilder master)
        {
            if (System.IO.File.Exists(a))
            {
                var reader = new System.IO.StreamReader(System.IO.File.OpenRead(a));
                int line_no = 1;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line_no > 1)
                    {
                        master.AppendLine(line);
                    }
                    line_no += 1;
                }
                reader.Close();
            }
        }


        private void Calculate(int i)
        {
            double pow = Math.Pow(i, i);
        }

        //private void button3_Click(object sender, EventArgs e)
        //{
        //    //progressBar1.Maximum = 100;
        //    //progressBar1.Step = 1;
        //    //progressBar1.Value = 0;
        //    //backgroundWorker1.RunWorkerAsync();
        //    currentLoad = totalLoad;
        //}

        private void InitLoader()
        {
     
        }

        private void UpdateLoader() 
        {
            
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
                var backgroundWorker = sender as BackgroundWorker;
                
                //while (currentLoad < totalLoad)
                //{
                //    EntryOutput(currentLoad  + " / " + totalLoad);
                //    backgroundWorker.ReportProgress((currentLoad * 100) / totalLoad);
                //}
            
                Execution();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer1.Enabled = false;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            if (currentLoad <= totalLoad && totalLoad > 0)
            {
                backgroundWorker1.ReportProgress((currentLoad * 100) / totalLoad);
            }
            else if (currentLoad >= totalLoad) 
            {
                backgroundWorker1.ReportProgress(100);
            }
            if (currentLoad == totalLoad && totalLoad > 0)
            {
                timer1.Enabled = false;
            }
            else 
            {
                timer1.Enabled = true;
            }
            
            
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            var drive = textBox3.Text;
            if (drive.Contains(":\\"))
            {
                textBox1.Text = textBox1.Text.Replace(textBox1.Text.Substring(0, (textBox1.Text.IndexOf(":\\"))) + ":\\", drive);
                textBox2.Text = textBox2.Text.Replace(textBox2.Text.Substring(0, (textBox2.Text.IndexOf(":\\"))) + ":\\", drive);

                outArt.Text = outArt.Text.Replace(outArt.Text.Substring(0, (outArt.Text.IndexOf(":\\"))) + ":\\", drive);
                outConfig.Text = outConfig.Text.Replace(outConfig.Text.Substring(0, (outConfig.Text.IndexOf(":\\"))) + ":\\", drive);
                outTracks.Text = outTracks.Text.Replace(outTracks.Text.Substring(0, (outTracks.Text.IndexOf(":\\"))) + ":\\", drive);
            }
            else
            {
                MessageBox.Show("Drive format is incorrect, please use something like 'C:\\' ");
            }
        }
    }
}
