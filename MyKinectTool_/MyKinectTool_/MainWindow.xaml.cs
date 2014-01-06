using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.IO;
using System.Windows.Threading;

namespace MyKinectTool_
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Kinectセンサクラス
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// 認識した人物の骨格情報
        /// </summary>
        private Skeleton skeleton;

        /// <summary>
        /// タイマーイベント用変数
        /// </summary>
        private DispatcherTimer dispatcherTimer;

        /// <summary>
        /// 準備段階のタイマー用イベント変数
        /// </summary>
        //追加事項
        private DispatcherTimer dispatcherTimer2;

        /// <summary>
        /// ファイルから読み込んできた骨格情報
        /// </summary>
        private Vector4[] pose1, pose2, kamae;

        /// <summary>
        /// 座標変換した骨格位置情報
        /// </summary>
        private ColorImagePoint[] cip = new ColorImagePoint[20];

        /// <summary>
        /// 骨格情報を表示する線
        /// </summary>
        private Line[] bones;

        /// <summary>
        /// Kinectセンサーからの画像情報を受け取る
        /// </summary>
        //追加事項
        private byte[] colorPixels;
       
        /// <summary>
        /// 画面に表示するビットマップ
        /// </summary>
        //追加事項
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// 認識状態
        /// </summary>
        enum State
        {
            None,
            Kamae,
            Pose1,
            Pose2,
        };

        /// <summary>
        /// 現在の認識状態
        /// </summary>
        private State nowState;

        /// <summary>
        /// 今から認識する状態
        /// </summary>
        //追加事項
        private State targetState;
        
        /// <summary>
        /// カウンタ
        /// </summary>
        private int counter;

        /// <summary>
        /// 認識するのか、判定するのかのフラグ
        /// </summary>
        //追加事項
        private bool flag;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Kinectを捜索
            // ここで行うことでプログラム起動時にKinectが接続されているか確認する。
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }
            #endregion

            #region Kinectが認識できたかどうか
            if (this.sensor != null)
            {
                //SkeletonStreamの有効
                this.sensor.SkeletonStream.Enable();

                //SkeletonStreamにイベントを追加
                this.sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(sensor_SkeletonFrameReady);

                //追加事項
                #region カラーストリームの使用
                //ColorStreamの有効化
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                
                //バッファの初期化
                colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];
                colorBitmap = new WriteableBitmap(sensor.ColorStream.FrameWidth,
                                                  sensor.ColorStream.FrameHeight,
                                                  96.0, 96.0, PixelFormats.Bgr32, null);
                this.Image.Source = colorBitmap;

                //イベントを追加
                this.sensor.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(sensor_ColorFrameReady);
                
                #endregion
                
                //センサを起動
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
            #endregion

            //インスタンス化
            this.skeleton = new Skeleton();

            //スレッドでの呼び出し優先度指定
            this.dispatcherTimer = new DispatcherTimer();
            //追加事項
            this.dispatcherTimer2 = new DispatcherTimer();

            //イベント追加
            this.dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            //追加事項
            this.dispatcherTimer2.Tick += new EventHandler(dispatcherTimer2_Tick);

            //タイマー動作開始
            this.dispatcherTimer.Start();
            //追加事項
            this.dispatcherTimer2.Start();

            //インターバル指定
            //追加事項
            this.dispatcherTimer2.Interval = new TimeSpan(0, 0, 1);

            //インスタンス化
            this.kamae = new Vector4[20];
            this.pose1 = new Vector4[20];
            this.pose2 = new Vector4[20];

            try
            {
                //読込処理を記述
            }
            catch (Exception ex)
            {
                Console.WriteLine("読み込み失敗");
            }

            //配列数宣言
            this.bones = new Line[19];

            for (int i = 0; i < this.bones.Length; i++)
            {
                //インスタンス化
                this.bones[i] = new Line();

                //描画設定
                this.bones[i].HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                this.bones[i].VerticalAlignment = System.Windows.VerticalAlignment.Center;
                this.bones[i].StrokeThickness = 2;
                this.bones[i].Stroke = Brushes.YellowGreen;

                //Canvasに追加
               // this.canvas1.Children.Add(this.bones[i]);
            }

            //初期化
            this.nowState = State.None;
            this.targetState = State.None;
            this.counter = 0;
            this.label9.FontSize = 30;
            this.flag = false;
        }

        //追加事項
        void sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                if (imageFrame != null)
                {
                    //画像情報の幅・高さ取得
                    int frmWidth = imageFrame.Width;
                    int frmHeight = imageFrame.Height;

                    //画像情報をバッファにコピー
                    imageFrame.CopyPixelDataTo(colorPixels);
                    //ビットマップに描画
                    Int32Rect src = new Int32Rect(0, 0, frmWidth, frmHeight);
                    colorBitmap.WritePixels(src, colorPixels, frmWidth * 4, 0);
                }
            }
        }

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (this.skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                if(this.flag)
                {
                    // 内積を計算して類似度を取得
                    int kamaeSimilarity = (int)((MyMath.Dot(JointType.ShoulderLeft, JointType.ElbowLeft, this.skeleton, this.kamae) +
                                                   MyMath.Dot(JointType.ElbowLeft, JointType.HandLeft, this.skeleton, this.kamae)) / 2 * 100);
                    int pose1Similarity = (int)((MyMath.Dot(JointType.ShoulderLeft, JointType.ElbowLeft, this.skeleton, this.pose1) +
                                                  MyMath.Dot(JointType.ElbowLeft, JointType.HandLeft, this.skeleton, this.pose1)) / 2 * 100);
                    int pose2Similarity = (int)(MyMath.Dot(JointType.ShoulderLeft, JointType.HandLeft, this.skeleton, this.pose2) * 100);

                    // 状態遷移
                    switch (this.nowState)
                    {
                        case State.None:
                            this.label9.Content = "準備はOK!!構えてみて!!";
                            if (kamaeSimilarity >= 90.0)
                            {
                                this.counter++;
                                if (this.counter >= slider4.Value)
                                {
                                    this.counter = 0;
                                    this.nowState = State.Kamae;
                                }
                            }
                            break;

                        case State.Kamae:
                            this.label9.Content = "かめはめ…";
                            if (pose1Similarity >= 80.0)
                                this.nowState = State.Pose1;

                            if (pose2Similarity >= this.slider3.Value)
                                this.nowState = State.Pose2;
                            break;

                        case State.Pose1:
                        case State.Pose2:

                            //this.counter++;

                            //if (this.counter > this.slider4.Value)
                            //{
                            //    this.counter = 0;
                            //    this.nowState = State.None;
                            //}
                            this.label9.Content = "波!!!!!!!!!!";
                            MyFileIO.PNGSave(this.colorBitmap);
                            if (pose1Similarity < this.slider2.Value)
                            {
                                this.counter = 0;
                                this.nowState = State.None;
                            }

                            break;

                        default:
                            break;
                    }

                    // 表示
                    this.label5.Content = kamaeSimilarity + " / " + this.slider1.Value.ToString("F0");
                    this.label6.Content = pose1Similarity + " / " + this.slider1.Value.ToString("F0");
                    this.label7.Content = pose2Similarity + " / " + this.slider1.Value.ToString("F0");
                }

                // 骨格を描画
                for (int i = 0; i < this.skeleton.Joints.Count; i++)
                {
                    // 3次元座標を2次元座標に変換
                    cip[i] = this.sensor.CoordinateMapper.MapSkeletonPointToColorPoint(this.skeleton.Joints[(JointType)i].Position, ColorImageFormat.RgbResolution640x480Fps30);

                    // Canvasに収まるように半分に縮小
                    cip[i].X /= 2;
                    cip[i].Y /= 2;
                }

                // 骨格情報をセット
                // 上半身
                this.SetBonePoint(0, 3, 2);
                this.SetBonePoint(1, 2, 4);
                this.SetBonePoint(2, 4, 5);
                this.SetBonePoint(3, 5, 6);
                this.SetBonePoint(4, 6, 7);
                this.SetBonePoint(5, 2, 8);
                this.SetBonePoint(6, 8, 9);
                this.SetBonePoint(7, 9, 10);
                this.SetBonePoint(8, 10, 11);
                //下半身
                this.SetBonePoint(9, 2, 1);
                this.SetBonePoint(10, 1, 0);
                this.SetBonePoint(11, 0, 12);
                this.SetBonePoint(12, 12, 13);
                this.SetBonePoint(13, 13, 14);
                this.SetBonePoint(14, 14, 15);
                this.SetBonePoint(15, 0, 16);
                this.SetBonePoint(16, 16, 17);
                this.SetBonePoint(17, 17, 18);
                this.SetBonePoint(18, 18, 19);
            }
            else
            {
                this.label5.Content = this.slider1.Value.ToString("F0");
                this.label6.Content = this.slider2.Value.ToString("F0");
                this.label7.Content = this.slider3.Value.ToString("F0");
                this.label8.Content = this.slider4.Value.ToString("F0") + "[ms]";
            }

            //this.label9.Content = "認識状態:" + this.nowState.ToString();
        }
        
        /// <summary>
        /// プレーする前の準備段階で起動する部分
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //追加事項
        void dispatcherTimer2_Tick(object sender, EventArgs e)
        {
            if (this.skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                if (!this.flag)
                {
                    //準備の状態遷移
                    switch (targetState)
                    {
                        case State.None:
                            counter++;
                            this.label9.Content = "これからかめはめ波のシュミレーションを行うよ!!";
                            if (counter >= 3)
                            {
                                targetState = State.Kamae;
                            }
                            break;
                        case State.Kamae:
                            this.label9.Content = "さぁかめはめ波の構えだよ!!（" + counter + "秒前）";
                            counter--;
                            if (counter < 0)
                            {
                                targetState = State.Pose1;
                                MyFileIO.SaveJoint("kamae", this.skeleton);
                                this.kamae = MyFileIO.LoadJoint("kamae");
                                counter = 3;
                            }
                            break;
                        case State.Pose1:
                            this.label9.Content = "次はかめはめ波打つポーズの練習だ!!（" + counter + "秒前）";
                            counter--;
                            if (counter < 0)
                            {
                                targetState = State.None;
                                MyFileIO.SaveJoint("pose1", this.skeleton);
                                this.pose1 = MyFileIO.LoadJoint("pose1");
                                counter = 0;
                                flag = true;
                            }
                            break;
                        case State.Pose2:
                            //三秒をカウントする処理
                            //三秒経った後にその人のポーズを記録
                            break;
                        default:
                            break;
                    }
                }
            }
        }
       
        /// <summary>
        /// SkeletonStreamのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            if (skeletons.Length != 0)
            {
                foreach (Skeleton skl in skeletons)
                {
                    if (skl.TrackingState == SkeletonTrackingState.Tracked)
                        this.skeleton = skl;
                }
            }
        }


        /// <summary>
        /// 骨格情報をセットする
        /// </summary>
        /// <param name="boneNo">描画する骨格番号</param>
        /// <param name="p1">座標変換した骨格位置情報のインデックス1</param>
        /// <param name="p2">座標変換した骨格位置情報のインデックス2</param>
        private void SetBonePoint(int boneNo, int p1, int p2)
        {
            this.bones[boneNo].X1 = cip[p1].X;
            this.bones[boneNo].Y1 = cip[p1].Y;
            this.bones[boneNo].X2 = cip[p2].X;
            this.bones[boneNo].Y2 = cip[p2].Y;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MyFileIO.SaveJoint("kamae", this.skeleton);
            this.kamae = MyFileIO.LoadJoint("kamae");
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            MyFileIO.SaveJoint("pose1", this.skeleton);
            this.pose1 = MyFileIO.LoadJoint("pose1");
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            MyFileIO.SaveJoint("pose2", this.skeleton);
            this.pose2 = MyFileIO.LoadJoint("pose2");
        }


    }
}
