using System;
using Microsoft.Kinect;
using System.IO;
using System.Globalization;
using System.Windows.Media.Imaging;

namespace MyKinectTool_
{
    class MyFileIO
    {
        /// <summary>
        /// すべてのジョイントの(x,y)を、nameの状態として保存
        /// </summary>
        /// <param name="name"></param>
        /// <param name="skl"></param>
        public static void SaveJoint(String name, Skeleton skl)
        {
            using (StreamWriter sw = new StreamWriter(name + ".csv"))
            {
                foreach (Joint joint in skl.Joints)
                    sw.Write(joint.Position.X + "," + joint.Position.Y + "," + joint.Position.Z + "\n");

                sw.Close();
            }
        }

        public static Vector4[] LoadJoint(String name)
        {
            Vector4[] jointPos = new Vector4[20];

            using (StreamReader sr = new StreamReader(name + ".csv"))
            {
                // すべての文字列を読み込み
                string str = sr.ReadToEnd();

                // 文字列を指定した文字で区切り分割する
                string[] buff = str.Split(new char[] { ',', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < jointPos.Length; i++)
                {
                    jointPos[i].X = float.Parse(buff[i * 3]);
                    jointPos[i].Y = float.Parse(buff[i * 3 + 1]); ;
                    jointPos[i].Z = float.Parse(buff[i * 3 + 2]); ;
                }

                sr.Close();
            }

            return jointPos;
        }

        /// <summary>
        /// 画像保存
        /// </summary>
        /// <param name="wb"></param>
        /// <returns></returns>
        public static string PNGSave(WriteableBitmap wb)
        {
            // create a png bitmap encoder which knows how to save a .png file
            BitmapEncoder encoder = new PngBitmapEncoder();

            // create frame from the writable bitmap and add to encoder
            encoder.Frames.Add(BitmapFrame.Create(wb));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            //string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string myPhotos = "";
            string path = Path.Combine(myPhotos, "KinectSnapshot-" + time + ".png");

            // write the new file to disk
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Properties.Resources.ScreenshotWriteSuccess, path);
            }
            catch (IOException)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Properties.Resources.ScreenshotWriteFailed, path);
            }
        }
    }
}
