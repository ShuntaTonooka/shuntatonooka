using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace KinectCameraSample
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        // RGBカメラの解像度・フレームレート
        ColorImageFormat rgbFormat = ColorImageFormat.RgbResolution640x480Fps30;

        // Kinectセンサーからの画像情報を受け取るバッファ
        private byte[] pixelBuffer = null;

        // Kinectセンサーからの骨格情報を受け取るバッファ
        private Skeleton[] skeletonBuffer = null;

        // 画面に表示するビットマップ
        private RenderTargetBitmap bmpBuffer = null;

        // 顔のビットマップイメージ
        private BitmapImage maskImage = null;

        // ビットマップへの描画用DrawingVisual
        private DrawingVisual darwVisual = new DrawingVisual();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Kincetセンサーの取得(エラー処理など省略版)
            KinectSensor kinect = KinectSensor.KinectSensors[0];

            // 画像の読み込み
            Uri imgUri = new Uri("pack://application:,,,/images/1216.png");
            maskImage = new BitmapImage(imgUri);

            // カラー・骨格ストリームの有効化
            ColorImageStream clrStream = kinect.ColorStream;
            clrStream.Enable(rgbFormat);
            SkeletonStream skelStream = kinect.SkeletonStream;
            skelStream.Enable();

            // バッファの初期化
            pixelBuffer = new byte[kinect.ColorStream.FramePixelDataLength];
            skeletonBuffer = new Skeleton[skelStream.FrameSkeletonArrayLength];
            bmpBuffer = new RenderTargetBitmap(clrStream.FrameWidth, clrStream.FrameHeight, 96, 96, PixelFormats.Default);
            rgbImage.Source = bmpBuffer;

            // イベントハンドラの登録
            kinect.AllFramesReady += AllFramesReady;

            // Kinectセンサーからのストリーム取得を開始
            kinect.Start();
        }

        // FrameReady イベントのハンドラ
        // (画像情報を取得・顔の部分にマスクを上書きして描画)
        private void AllFramesReady(object sencer, AllFramesReadyEventArgs e)
        {
            KinectSensor kinect = sencer as KinectSensor;
            List<SkeletonPoint> headList = null;

            // 骨格情報から、頭の座標リストを作成
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                    headList = getHaedPoints(skeletonFrame);
            }

            // カメラの画像情報に、顔の位置にマスクを上書きして描画
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                if (imageFrame != null)
                    fillBitmap(kinect, imageFrame, headList);
            }
        }

        // 骨格情報から、頭の位置を取得しリストに入れて返す
        private List<SkeletonPoint> getHeadPoints(SkeletonFrame skelFrame)
        {
            // 処理結果のリストを空の状態で作成
            List<SkeletonPoint> results = new List<SkeletonPoint>();

            // 骨格情報をバッファにコピー
            skelFrame.CopySkeletonDataTo(skeletonBuffer);

            // 取得できた骨格毎にループ
            foreach (Skeleton skeleton in skeletonBuffer)
            {
                // トラッキングできていない骨格は処理しない
                if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                    continue;

                // 骨格から頭を取得
                Joint head = skeleton.Joints[JointType.Head];

                // 頭の位置が取得できない状態の場合は処理しない
                if (head.TrackingState != JointTrackingState.Tracked && head.TrackingState != JointTrackingState.Inferred)
                    continue;

                // 頭の位置を保存
                results.Add(head.Position);
            }
            return results;
        }

        // RGBカメラの画像情報に、顔の位置にマスクを上書きして描画する
        private void fillBitmap(KinectSensor kinect, ColorImageFrame imgFrame, List<SkeletonPoint> headList)
        {
            // 描画の準備
            var drawContext = drawVisual.RenderOpen();
            int frmWidth = imgFrame.Width;
            int frmHeight = imgFrame.Height;

            // 画像情報をバッファにコピー
            imgFrame.CopyPixelDataTo(pixelBuffer);

            // カメラの画像情報から背景ビットマップを作成し描画
            var bgImg = new WriteableBitmap(frmWidth, frmHeight, 96, 96, PixelFormats.Bgr32, null);
            bgImg.WritePixels(new Int32Rect(0, 0, frmWidth, frmHeight), pixelBuffer, frmWidth * 4, 0);
            drawContext.DrawImage(bgImg, new Rect(0, 0, frmWidth, frmHeight));

            // getHeadPointsで取得した各頭部(の位置)毎にループ
            for (int idx = 0; headList != null && idx < headList.Count; ++idx)
            {
                // 骨格の座標から画像情報の座標に変換
                ColorImagePoint headPt = kinect.MapSkeletonPointToColor(headList[idx], rgbFormat);
                drawContext.DrawImage(maskImage, rect);
            }

            // 画面を表示するビットマップに描画
            drawContext.Close();
            bmpBuffer.Render(drawVisual);
        }

        // ColorFrameReady イベントのハンドラ(画像情報を取得して描画)
        private void ColorImageReady(object sencer, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                // 画像情報の幅・高さ取得 ※途中で変わらない場合
                int frmWidth = imageFrame.Width;
                int frmHeight = imageFrame.Height;

                // 画像情報をバッファにコピー
                imageFrame.CopyPixelDataTo(pixelBuffer);
                // ビットマップに描画
                Int32Rect src = new Int32Rect(0, 0, frmWidth, frmHeight);
                bmpBuffer.WritePixels(src, pixelBuffer, frmWidth * 4, 0);
            }
        }
    }
}
