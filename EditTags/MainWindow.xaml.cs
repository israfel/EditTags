using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.Net;
using HtmlAgilityPack;

namespace EditTags
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string filename;

        private string title;

        private string[] tags;

        private string[] supportedExts = { ".jpg", ".jpeg"};

        //private bool imgLoaded = false;

        //由于尚未实现png的metadata读写，额外加一个标记，=true则不做metadata相关的工作，具体在OnDrop()和HandleImgDrop()中出现
        private bool png = false;

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                filename = files[0];
                png = false;
                if (supportedExts.Contains(System.IO.Path.GetExtension(filename)))
                    HandleImgDrop();
                else
                    if (System.IO.Path.GetExtension(filename).Equals(".png"))
                    {
                        png = true;
                        msg("png图像的metadata操作暂不可行");
                        HandleImgDrop();
                    }
                    else
                        msg("仅支持jpg及png文件m(_ _)m");
            }
        }

        private void HandleImgDrop()
        {
            DisplayImg();
            if (!png)
            {
                F5Title();

                F5Tags();
            }
        }

        /// <summary>
        /// display image
        /// </summary>
        private void DisplayImg()
        {
            BitmapImage img = new BitmapImage();

            //BitmapImage.UriSource must be in a BeginInit/EndInit block
            img.BeginInit();
            //the following 2 lines are in reference to http://stackoverflow.com/questions/542217/load-a-bitmapsource-and-save-using-the-same-name-in-wpf-ioexception
            //answered by tom, edited by VirtualBlackFox.
            //or GetTags() would throw an IOException
            img.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            img.CacheOption = BitmapCacheOption.OnLoad;

            img.UriSource = new Uri(filename);

            img.DecodePixelWidth = (int)imgImage.Width;
            try
            {
                img.EndInit();
            }
            catch (Exception e)
            {
                msg(e.Message);
                return;
            }
            imgImage.Source = img;
        }

        private void F5Title()
        {
            //get title
            if (filename != null)
            {
                using (Stream imgFileStream = File.Open(filename, FileMode.Open, FileAccess.Read))
                {
                    BitmapDecoder decoder = BitmapDecoder.Create(imgFileStream, BitmapCreateOptions.None, BitmapCacheOption.Default);
                    //PngBitmapDecoder decoder = new PngBitmapDecoder(imgFileStream, BitmapCreateOptions.None, BitmapCacheOption.Default);
                    BitmapFrame frame = decoder.Frames[0];
                    BitmapMetadata metadata = frame.Metadata as BitmapMetadata;
                    
                    imgFileStream.Close();

                    title = null;
                    try
                    {
                        title = metadata.GetQuery("System.Title") as string;
                        //title = metadata.GetQuery("/tEXt/Title") as string;
                    }
                    catch (NotSupportedException e)
                    {
                        //or the Image will be refreshed but the tags will STAY
                        tagsStackPanel.Children.Clear();
                        msg("A NotSupportedException occured in F5Title().\n" + e.Message);
                    }
                }

                //display title
                if (title != null)
                    titleTextBlock.Text = title;
                else
                    //in reference to http://msdn.microsoft.com/en-us/library/system.io.path.getfilename.aspx
                    titleTextBlock.Text = System.IO.Path.GetFileNameWithoutExtension(filename);
            }
            titleTextBlock.Visibility = Visibility.Visible;
            titleTextBox.Visibility = Visibility.Collapsed;
            titleCommitButton.Visibility = Visibility.Collapsed;
        }

        private void F5Tags()
        {
            if (filename == null)
                tagsStackPanel.Children.Clear();
            else
            {
                //get tags
                using (Stream imgFileStream = File.Open(filename, FileMode.Open, FileAccess.Read))
                {
                    BitmapDecoder decoder = BitmapDecoder.Create(imgFileStream, BitmapCreateOptions.None, BitmapCacheOption.Default);

                    BitmapFrame frame = decoder.Frames[0];
                    BitmapMetadata metadata = frame.Metadata as BitmapMetadata;

                    imgFileStream.Close();

                    tags = null;
                    try
                    {
                        //tags = metadata.GetQuery("/iTXt/Keyword") as string[];
                        tags = metadata.GetQuery("System.Keywords") as string[];
                    }
                    catch (NotSupportedException e)
                    {
                        //or the Image will be refreshed but the tags will STAY
                        tagsStackPanel.Children.Clear();
                        msg("A NotSupportedException occured in F5Tags().\n" + e.Message);
                    }
                }
                //display tags
                tagsStackPanel.Children.Clear();
                if (tags != null)
                {
                    foreach (string tag in tags)
                    {
                        StackPanel tagStackPanel = new StackPanel();
                        tagStackPanel.Orientation = Orientation.Horizontal;

                        Button delTagButton = new Button();
                        delTagButton.Content = 'x';
                        delTagButton.Background = null;
                        delTagButton.BorderBrush = null;
                        delTagButton.Foreground = Brushes.Red;
                        delTagButton.Click += DelTagButton_Click;

                        tagStackPanel.Children.Add(delTagButton);

                        TextBlock tagTextBlock = new TextBlock();
                        tagTextBlock.Text = tag;
                        tagTextBlock.MouseUp += SearchTagInPixiv;
                        tagStackPanel.Children.Add(tagTextBlock);

                        tagsStackPanel.Children.Add(tagStackPanel);
                    }
                }
            }
        }

        private void msg(string s)
        {
            MessageBox.Show(s);
        }

        /// <summary>
        /// get the url of this pic's Pixiv page
        /// </summary>
        /// <returns>null if no pic is loaded</returns>
        private string getPixivUrl()
        {
            if (filename == null) return String.Empty;
            string filenameWE = System.IO.Path.GetFileNameWithoutExtension(filename);
            string url = @"http://www.pixiv.net/member_illust.php?mode=medium&illust_id=";
            if (filename.Contains('_'))
                url += filenameWE.Substring(0, filenameWE.IndexOf('_'));
            else
                url += filenameWE;
            return url;
        }

        private void SearchTagInPixiv(object sender, MouseButtonEventArgs e)
        {
            string tag = (sender as TextBlock).Text;
            string url = @"http://www.pixiv.net/search.php?s_mode=s_tag_full&word=";
            if (tag != null)
                url += tag;
            System.Diagnostics.Process.Start(url);
        }

        private void OnImgMouseUp(object sender, MouseButtonEventArgs e)
        {
            //in reference to http://social.msdn.microsoft.com/Forums/vstudio/en-US/8b2eff00-d407-4754-bf15-3e40b49b1b33/how-to-open-a-web-browser-via-wpf-application
            if (filename == null) return;
            string filenameWE = System.IO.Path.GetFileNameWithoutExtension(filename);
            string url = @"http://www.pixiv.net/member_illust.php?mode=medium&illust_id=";
            if (filename.Contains('_'))
                url += filenameWE.Substring(0, filenameWE.IndexOf('_'));
            else
                url += filenameWE;
            System.Diagnostics.Process.Start(url);
        }

        private void OnTitleMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (filename == null) return;
            titleTextBlock.Visibility = Visibility.Collapsed;
            titleTextBox.Text = titleTextBlock.Text;
            titleTextBox.Visibility = Visibility.Visible;
            //http://stackoverflow.com/questions/2888907/set-the-caret-cursor-position-to-the-end-of-the-string-value-wpf-textbox
            titleTextBox.Focus();
            titleTextBox.SelectAll();
            //titleTextBox.CaretIndex = titleTextBox.Text.Length;

            titleCommitButton.Visibility = Visibility.Visible;
        }

        private void OnTitleCommitButtonClick(object sender, RoutedEventArgs e)
        {
            SetMetadata("System.Title", titleTextBox.Text);
            //SetMetadata("/tEXt/Title", titleTextBox.Text);
            F5Title();
        }

        private void SetMetadata(string query, object data)
        {
            if (filename == null) return;

            //in reference to http://blog.andreweichacker.com/2009/02/reading-and-writing-tags-for-photos-in-wpf/

            using (Stream imgFileStream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                //in the comments of http://blog.andreweichacker.com/2009/02/reading-and-writing-tags-for-photos-in-wpf/
                //the author said changing BitmapCacheOption from Default to None can ensure the img being lossless
                BitmapDecoder decoder = BitmapDecoder.Create(imgFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                //PngBitmapDecoder decoder = new PngBitmapDecoder(imgFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                BitmapFrame frame = decoder.Frames[0];
                BitmapMetadata metadata = frame.Metadata as BitmapMetadata;
                InPlaceBitmapMetadataWriter writer = frame.CreateInPlaceBitmapMetadataWriter();
                writer.SetQuery(query, data);
                if (!writer.TrySave())
                {
                    imgFileStream.Close();
                    MakeRoomAndSetMetadata(query, data);
                }
            }
        }

        private void MakeRoomAndSetMetadata(string query, object data)
        {

            //msg("MakeRoomAndSetMetadata called.");

            uint paddingAmount = 5120;
            //http://www.dustyfish.com/blog/writing-photo-metadata-using-windows-imaging-component
            string exifPadding = "/app1/ifd/exif/PaddingSchema:Padding";
            string ifdPadding = "/app1/ifd/PaddingSchema:Padding";
            string xmpPadding = "/xmp/PaddingSchema:Padding";
            string[] paddings = { exifPadding, ifdPadding, xmpPadding };


            MemoryStream tmpStream = new MemoryStream();

            using (Stream imgFileStream = File.Open(filename, FileMode.Open, FileAccess.Read))
            {
                BitmapDecoder original = BitmapDecoder.Create(imgFileStream,
                    BitmapCreateOptions.PreservePixelFormat,
                    //if set to OnLoad, img would be re-encode
                    BitmapCacheOption.None);

                BitmapMetadata newMetadata = original.Frames[0].Metadata.Clone() as BitmapMetadata;

                //ensure we have enough padding
                foreach (string padding in paddings)
                {
                    if (Convert.ToInt32(newMetadata.GetQuery(padding)) < paddingAmount)
                        newMetadata.SetQuery(padding, paddingAmount);
                }

                newMetadata.SetQuery(query, data);

                BitmapFrame destFrame = BitmapFrame.Create(original.Frames[0],
                    original.Frames[0].Thumbnail,
                    newMetadata,
                    original.Frames[0].ColorContexts);

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(destFrame);

                //save to memorystream
                encoder.Save(tmpStream);
            }
            using (Stream outputImgFileStream = File.Open(filename, FileMode.Create, FileAccess.ReadWrite))
            //using (Stream outputImgFileStream = File.Open(@"C:\Users\Felix\Desktop\aaa.png", FileMode.Create, FileAccess.ReadWrite))
            {
                //write memorystream to original file
                tmpStream.WriteTo(outputImgFileStream);
            }
        }

        //
        private void DelTagButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> tagsList = tags.ToList();
            tagsList.Remove(
                (((sender as Control).Parent as StackPanel).Children[1] as TextBlock).Text);
            SetMetadata("System.Keywords", tagsList.ToArray());
            //SetMetadata("/iTXt/Keyword", tagsList.ToArray());
            F5Tags();
        }
        

        /// <summary>
        /// "Add" 按钮的click事件响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (newTag.Text == "") return;
            //http://stackoverflow.com/questions/1440265/how-to-add-a-string-to-a-string-array-theres-no-add-function
            List<string> tagsList = new List<string>();
            if (tags != null) 
                tagsList = tags.ToList();
            string tmp = newTag.Text;
            int i = 0;
            if (tmp[i] == 'c') ++i;
            if (tmp[i] == '*') ++i;
            int j = i;
            while (++j != tmp.Length)
            {
                if (tmp[j] == 'c')
                {
                    tagsList.Add(tmp.Substring(i, j - i));
                    i = j + ((tmp[j + 1] == '*') ? 2 : 1);
                    j = i;
                }
            }
            tagsList.Add(tmp.Substring(i));
            SetMetadata("System.Keywords", tagsList.ToArray());
            //SetMetadata("/iTXt/Keyword", tagsList.ToArray());
            newTag.Text = "";
            F5Tags();
        }

        private void newTag_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AddTagButton_Click(sender, null);
            else
                if (e.Key == Key.Escape)
                    newTag.Text = "";
        }

        private void TitleTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OnTitleCommitButtonClick(sender, null);
            else
                if (e.Key == Key.Escape)
                    //titleTextBox.Text = "";
                    F5Title();
        }

        /// <summary>
        /// “clear”按钮的click事件响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearTagsButton_Click(object sender, RoutedEventArgs e)
        {
            test();
            /*
            if (tags == null || tags.Length == 0)
                return;
            //ClearTagsButton.Effect
            //tagsStackPanel.Children.Clear();
            List<string> tagsList = tags.ToList();
            tagsList.Clear();
            SetMetadata("System.Keywords", tagsList.ToArray());
            F5Tags();
             */
        }


        private void test()
        {
            //http://stackoverflow.com/questions/4510212/how-i-can-get-web-pages-content-and-save-it-into-the-string-variable

            WebRequest request = WebRequest.Create(getPixivUrl());
            WebResponse response = request.GetResponse();
            Stream data = response.GetResponseStream();
            string htm = String.Empty;
            
            using (StreamReader sr = new StreamReader(data, Encoding.UTF8))
            {
                htm = sr.ReadToEnd();
            }

            //http://htmlagilitypack.codeplex.com/
            HtmlDocument hdoc=new HtmlDocument();
            hdoc.LoadHtml(htm);

            HtmlNode tagsList = hdoc.DocumentNode.SelectSingleNode("//ul[@class='inline-list']");
            if (tagsList != null)
            {
                foreach (HtmlNode tagLi in tagsList.ChildNodes)
                {
                    HtmlNode tagAHref = tagLi.LastChild;
                    MessageBox.Show(tagAHref.InnerText);
                }
            }
        }
    }


}
