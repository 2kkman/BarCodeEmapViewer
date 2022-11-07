using Common.Utils.Tools;
using Common.Utils.Log;
using Common;
using System;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Threading;
using Dynamsoft;
using Dynamsoft.DBR;
using ContentAlignment = System.Drawing.ContentAlignment;
using Dynamsoft.Common;
using Dynamsoft.TWAIN;
using Dynamsoft.UVC;
using Dynamsoft.Core;
using Dynamsoft.PDF;
using Dynamsoft.TWAIN.Enums;
using Dynamsoft.Core.Enums;
using Dynamsoft.Core.Annotation;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Linq;
using Tesseract;

namespace WindowsFormsApp1
{
    public partial class FrmMain : Form
    {
        public const string _sLog = "FrmMain";
        public const string _sLogErr = _sLog + "Err";
        public static System.Threading.Timer _tTimerTagPolling;
        public static ConsoleUtil _UTIL = new ConsoleUtil();
        public string cameraPOS;
        public string eMap_Clicked;
        EnumBarcodeFormat mEmBarcodeFormat = 0;
        EnumBarcodeFormat_2 mEmBarcodeFormat_2 = 0;
        private readonly BarcodeReader mBarcodeReader;
        //string dbrLicenseKeys = "t0068NQAAAHNjkwC8ityMrtdLR5FGxyyC1ZcWif1RYaJdoOhyiUoHrkkFjYInYtmp2qiW08KG4FXBq3FUNyvJOoZsk8+oow8=";
        string dbrLicenseKeys = "t0074oQAAABQRLM27TUELy1M2a3z10yUA7aIndFLu3RuleBGuZQqUN8PaqZdx5dy+f6YuYzvrEk8urWB8dOhugYfmjH9QRQZFbPdgIsc=";
        
        string dntLicenseKeys = "t0072WQAAAFOO4Aa9Am1LUHF1OalFndItUlEeg/stiIA0cCrj/b88+Hez4/eWj4klqiEq4zwJ6Efz6Jsmc3oqZxisI/1pcmQ0iBKG";
        private int miRecognitionMode = 2;//best converage
        private PublicRuntimeSettings mNormalRuntimeSettings;
        public static string _sEMapBarImgDir = @"C:\eMap3D\BARIMG\";
        public static string _sTagTest = @"C:\eMap3D\BARTEST.txt";
        public static string _sEMapRawImgDir = @"D:\SPG_IMG\";
        bool bMutex = false;
        private ImageCore mImageCore = null;
        public FrmMain()
        {
            InitializeComponent();
            mBarcodeReader = new BarcodeReader(dbrLicenseKeys);
            mNormalRuntimeSettings = mBarcodeReader.GetRuntimeSettings();
            mImageCore = new ImageCore();
            mImageCore.ImageBuffer.MaxImagesInBuffer = 64;
            mEmBarcodeFormat = EnumBarcodeFormat.BF_DATAMATRIX | EnumBarcodeFormat.BF_CODE_128 | EnumBarcodeFormat.BF_DATAMATRIX | EnumBarcodeFormat.BF_EAN_13 | EnumBarcodeFormat.BF_ITF | EnumBarcodeFormat.BF_QR_CODE | EnumBarcodeFormat.BF_EAN_8 | EnumBarcodeFormat.BF_GS1_COMPOSITE;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            PublicRuntimeSettings tempCoverage = mBarcodeReader.GetRuntimeSettings();
            tempCoverage.BarcodeFormatIds = (int)this.mEmBarcodeFormat;
            tempCoverage.BarcodeFormatIds_2 = (int)this.mEmBarcodeFormat_2;
            // use default value of LocalizationModes
            tempCoverage.DeblurLevel = 9;
            tempCoverage.ExpectedBarcodesCount = 512;
            tempCoverage.ScaleDownThreshold = 214748347;
            tempCoverage.FurtherModes.TextFilterModes[0] = EnumTextFilterMode.TFM_GENERAL_CONTOUR;
            for (int i = 1; i < tempCoverage.FurtherModes.TextFilterModes.Length; i++)
                tempCoverage.FurtherModes.TextFilterModes[i] = EnumTextFilterMode.TFM_SKIP;
            tempCoverage.FurtherModes.GrayscaleTransformationModes[0] = EnumGrayscaleTransformationMode.GTM_ORIGINAL;
            tempCoverage.FurtherModes.GrayscaleTransformationModes[1] = EnumGrayscaleTransformationMode.GTM_INVERTED;
            for (int i = 2; i < tempCoverage.FurtherModes.GrayscaleTransformationModes.Length; i++)
                tempCoverage.FurtherModes.GrayscaleTransformationModes[i] = EnumGrayscaleTransformationMode.GTM_SKIP;

            mBarcodeReader.UpdateRuntimeSettings(tempCoverage);
            //string strErrorMessage = string.Empty;
            //mBarcodeReader.SetModeArgument("TextureDetectionModes", 0, "Sensitivity", "0", out strErrorMessage);
            //_tTimerTagPolling = new System.Threading.Timer(new TimerCallback(TagEventCallBack), null, 0, 10000);
            //btn_Test_Click(sender, e);
        }

        private Rectangle ConvertLocationPointToRect(Point[] points)
        {
            int left = points[0].X, top = points[0].Y, right = points[1].X, bottom = points[1].Y;
            for (int i = 0; i < points.Length; i++)
            {

                if (points[i].X < left)
                {
                    left = points[i].X;
                }

                if (points[i].X > right)
                {
                    right = points[i].X;
                }

                if (points[i].Y < top)
                {
                    top = points[i].Y;
                }

                if (points[i].Y > bottom)
                {
                    bottom = points[i].Y;
                }
            }
            Rectangle temp = new Rectangle(left, top, (right - left), (bottom - top));
            return temp;
        }

        private Bitmap ShowResultOnImage(Bitmap bitmap, TextResult[] textResults)
        {
            mImageCore.ImageBuffer.SetMetaData(mImageCore.ImageBuffer.CurrentImageIndexInBuffer, EnumMetaDataType.enumAnnotation, null, true);
            Graphics bitmapGraphics = Graphics.FromImage(bitmap);
            if (textResults != null)
            {
                List<AnnotationData> tempListAnnotation = new List<AnnotationData>();
                int nTextResultIndex = 0;
                for (var i = 0; i < textResults.Length; i++)
                {
                    var penColor = Color.Red;
                    TextResult result = textResults[i];

                    var rectAnnotation = new AnnotationData();
                    rectAnnotation.AnnotationType = AnnotationType.enumRectangle;
                    Rectangle boundingrect = ConvertLocationPointToRect(result.LocalizationResult.ResultPoints);
                    rectAnnotation.StartPoint = new Point(boundingrect.Left, boundingrect.Top);
                    rectAnnotation.EndPoint = new Point((boundingrect.Left + boundingrect.Size.Width), (boundingrect.Top + boundingrect.Size.Height));
                    rectAnnotation.FillColor = Color.Transparent.ToArgb();
                    rectAnnotation.PenColor = penColor.ToArgb();
                    rectAnnotation.PenWidth = 3;
                    rectAnnotation.GUID = Guid.NewGuid();

                    float fsize = bitmap.Width / 12.0f;
                    if (fsize < 25)
                        fsize = 25;

                    Font textFont = new Font("Times New Roman", fsize/3, FontStyle.Bold);

                    string strNo = (result != null) ? "[" + (nTextResultIndex++ + 1) + "]" : "";
                    SizeF textSize = Graphics.FromHwnd(IntPtr.Zero).MeasureString(strNo, textFont);

                    var textAnnotation = new AnnotationData();
                    textAnnotation.AnnotationType = AnnotationType.enumText;
                    textAnnotation.StartPoint = new Point(boundingrect.Left - 200, (int)(boundingrect.Top - textSize.Height * 1.25f));
                    textAnnotation.EndPoint = new Point((textAnnotation.StartPoint.X + (int)textSize.Width * 2), (int)(textAnnotation.StartPoint.Y + textSize.Height * 1.25f));
                    if (textAnnotation.StartPoint.X < 0)
                    {
                        textAnnotation.EndPoint = new Point((textAnnotation.EndPoint.X + textAnnotation.StartPoint.X), textAnnotation.EndPoint.Y);
                        textAnnotation.StartPoint = new Point(0, textAnnotation.StartPoint.Y);
                    }
                    if (textAnnotation.StartPoint.Y < 0)
                    {
                        textAnnotation.EndPoint = new Point(textAnnotation.EndPoint.X, (textAnnotation.EndPoint.Y - textAnnotation.StartPoint.Y));
                        textAnnotation.StartPoint = new Point(textAnnotation.StartPoint.X, 0);
                    }

                    textAnnotation.TextContent = strNo;
                    AnnoTextFont tempFont = new AnnoTextFont();
                    tempFont.TextColor = Color.Red.ToArgb();
                    tempFont.Size = (int)fsize;
                    tempFont.Name = "Times New Roman";
                    textAnnotation.FontType = tempFont;
                    textAnnotation.GUID = Guid.NewGuid();

                    tempListAnnotation.Add(rectAnnotation);
                    tempListAnnotation.Add(textAnnotation);
                    //Point antText = textAnnotation.EndPoint;
                    Point antText = new Point(textAnnotation.StartPoint.X, textAnnotation.EndPoint.Y);
                    bitmapGraphics.DrawString($"{strNo}:{result.BarcodeText}", textFont, Brushes.Red, antText);
                    //bitmapGraphics.DrawRectangle(Pens.Red, rectAnnotation.StartPoint.X, rectAnnotation.StartPoint.Y, rectAnnotation.EndPoint.X, rectAnnotation.EndPoint.Y);
                    Rectangle rct = new Rectangle(rectAnnotation.StartPoint.X, rectAnnotation.StartPoint.Y, rectAnnotation.EndPoint.X-rectAnnotation.StartPoint.X, rectAnnotation.EndPoint.Y - rectAnnotation.StartPoint.Y );
                    
                    bitmapGraphics.DrawRectangle(Pens.Red, rct);

                }
                mImageCore.ImageBuffer.SetMetaData(mImageCore.ImageBuffer.CurrentImageIndexInBuffer, EnumMetaDataType.enumAnnotation, tempListAnnotation, true);
                bitmapGraphics.DrawImage(bitmap, new Point(0, 0));
                return bitmap;
            }
            return null;
        }
        private string AddBarcodeText(string result, string barcodetext)
        {
            string temp = "";
            string temp1 = barcodetext;
            for (int j = 0; j < temp1.Length; j++)
            {
                if (temp1[j] == '\0')
                {
                    temp += "\\";
                    temp += "0";
                }
                else
                {
                    temp += temp1[j].ToString();
                }
            }
            result += string.Format("    Value: {0}\r\n", temp);
            return result;
        }

        string TextResulttoString(TextResult[] textResult)
        {
            string strResult = string.Empty;
            for (var i = 0; i < textResult.Length; i++)
            {
                Rectangle tempRectangle = ConvertLocationPointToRect(textResult[i].LocalizationResult.ResultPoints);
                strResult += string.Format("  Barcode: {0}\r\n", (i + 1));
                string strFormatString = "";

                if (textResult[i].BarcodeFormat == EnumBarcodeFormat.BF_NULL)
                    strFormatString = textResult[i].BarcodeFormatString_2;
                else
                    strFormatString = textResult[i].BarcodeFormatString;
                strResult += string.Format("    Type: {0}\r\n", strFormatString);
                strResult = AddBarcodeText(strResult, textResult[i].BarcodeText);
                strResult += string.Format("    Hex Data: {0}\r\n", ToHexString(textResult[i].BarcodeBytes));
                strResult += string.Format("    Region: {{Left: {0}, Top: {1}, Width: {2}, Height: {3}}}\r\n", tempRectangle.Left.ToString(),
                                               tempRectangle.Top.ToString(), tempRectangle.Width.ToString(), tempRectangle.Height.ToString());
                strResult += string.Format("    Module Size: {0}\r\n", textResult[i].LocalizationResult.ModuleSize);
                strResult += string.Format("    Angle: {0}\r\n", textResult[i].LocalizationResult.Angle);
                strResult += "\r\n";
            }
            return strResult;
        }

        public string GetNumberFromString(string sMSG)
        {
            string sReply = string.Empty;
            char[] chaTmp = sMSG.ToArray();
            ///가독아스키문자만 남기고 다 걸러냄.
            foreach (char byTmp in chaTmp)
            {
                if (byTmp >= 48 && byTmp <= 57)
                {
                    sReply += (char)byTmp;
                }
            }
            return sReply;
        }

        string[] TextResulttoList(TextResult[] textResult)
        {
            List<string> lsReturn = new List<string>();
            foreach(TextResult tr in textResult)
            {
                string sBar = GetNumberFromString(tr.BarcodeText);
                
                if(lsReturn.Contains(sBar) == false)
                {
                    lsReturn.Add(sBar);
                }
            }
            return lsReturn.ToArray();
        }

        private static string ToHexString(byte[] bytes)
        {
            string hexString = string.Empty;

            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2") + " ");
                }

                hexString = strB.ToString();

            }

            return hexString;
        }
        Dictionary<string, List<string>> _dicResult = new Dictionary<string, List<string>>();

        private void TagEventCallBack(object sender)
        {
            List<string> lsFilenameDelete = new List<string>();

            string[] arrFiles = Directory.GetFiles(_sEMapRawImgDir, "*.jp*");
            try
            {
                /// 태그값 전송 실패 경고 업데이트 루틴 (오늘과 날짜가 다른 데이터 파일이 존재하면 한미톡 경고를 보낸다)
                List<string> lsMeaageSend = new List<string>();
                ///이하 로직 테스트 필요.디버그 모드로 손 좀 보자.
                string sTargetPath = _sEMapBarImgDir;
                if (Directory.Exists(sTargetPath) == false)
                {
                    Directory.CreateDirectory(sTargetPath);
                }
                foreach (string filename in arrFiles)
                {
                    //파일명 : 20211119160604601_R_CAM4_2_0020.jpg - 지시정보 없는거
                    //        20211130103705645_CamBoth_CAM3_R_3_0020.jpg 지시정보 있는거
                    string sFolderNameOnly = _UTIL.GetFolderInfo(filename);
                    string sFileNameOnly = filename.Replace(sFolderNameOnly, string.Empty);
                    string sFileSaveFullPath = sTargetPath + sFileNameOnly;
                    string sFileNameOnlyWithoutExt = _UTIL.GetIndexString(sFileNameOnly, ".", 0);
                    string[] saFieldInfo = sFileNameOnlyWithoutExt.Split("_".ToCharArray());
                    /// 2021~2022에서는 아래처럼
                    //string sTimeStamp = saFieldInfo[0];
                    //string sTier = saFieldInfo[4];
                    //string sNode = saFieldInfo[5];
                    //string sTierNode = $"{sTier}_{sNode}";
                    
                    //2022~2023 에서는
                    string sTimeStamp = saFieldInfo[2];
                    string sTier = saFieldInfo[2];
                    string sNode = saFieldInfo[0];
                    string sTierNode = $"{sTier}_{sNode}";


                    //Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
                    DateTime beforeRead = DateTime.Now;
                    using (var bmp = (Bitmap)Bitmap.FromFile(filename))
                    {
                       
                        TextResult[] textResults = mBarcodeReader.DecodeBitmap(bmp, "");
                        string sResultTmp = TextResulttoString(textResults);
                        if (textResults != null && textResults.Length > 0)
                        {
                            List<string> lsResult = new List<string>(TextResulttoList(textResults));
                            if( _dicResult.ContainsKey(sTierNode))
                            {
                                List<string> lsOld = _dicResult[sTierNode];
                                foreach(string sBar in lsResult)
                                {
                                    if(lsOld.Contains(sBar) == false)
                                    {
                                        _dicResult[sTierNode].Add(sBar);
                                    }
                                }
                            }
                            else
                            {
                                _dicResult.Add(sTierNode, lsResult);
                            }

                            Invoke((MethodInvoker)delegate { txt_Result.AppendText(sResultTmp); });
                            int iLimit = mImageCore.ImageBuffer.MaxImagesInBuffer;
                            int iCurrent = mImageCore.ImageBuffer.HowManyImagesInBuffer;
                            if (iCurrent < iLimit)
                            {
                                mImageCore.IO.LoadImage(filename);
                                DateTime afterRead = DateTime.Now;
                                int timeElapsed = (int)(afterRead - beforeRead).TotalMilliseconds;
                                Bitmap bitCheck = ShowResultOnImage(bmp, textResults);
                                if (bitCheck != null)
                                {
                                    bitCheck.Save(sFileSaveFullPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                                }
                                mImageCore.ImageBuffer.RemoveAllImages();
                            }
                            else
                            {
                                return;
                            }
                        }
                    }//end of bitmap using.

                }//end of foreach
            }
            catch (Exception ex)
            {
                LogManager.Trace2(_sLog, "{0}-{ 1}", _UTIL.GetMethodName(), ex.ToString());
            }
            finally
            {
                //bMutex = false;
            }
            foreach (string filename in arrFiles)
            {
                try
                {
                    File.Delete(filename);
                }
                catch(Exception ex)
                {
                    string sTmp = $"{filename} - {ex}";
                    txt_OCR.AppendText(sTmp + "\r\n");
                }
            }

            if(_dicResult.Count > 0)
            {
                CheckResult();
            }
        }

        void CheckResult()
        {
            string sResult = "BARLIST/";
            foreach (string sKey in _dicResult.Keys)
            {
                List<string> lsCurrent = _dicResult[sKey];
                string sBarValuePart = _UTIL.StringMerge(lsCurrent.ToArray(), ":");
                string sPart = $"`{sBarValuePart},{sKey}";
                sResult += sPart;
            }
            File.WriteAllText(_sTagTest, sResult);
            txt_Result.Text = sResult;
            _dicResult.Clear();
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_tTimerTagPolling != null)
                _tTimerTagPolling.Dispose();
        }

        private void btn_Test_Click(object sender, EventArgs e)
        {
            TagEventCallBack(e);
        }

        /// <summary>
        /// 개발테스트용. 이미지 리네임.
        /// </summary>
        //void EditImgFiles()
        //{
        //    List<string> lsReturn = new List<string>();
        //    if (Directory.Exists(_sEMapBarImgDir))
        //    {
        //        List<string> lsTmp = new List<string>(Directory.GetFiles(_sEMapBarImgDir, "*.jp*"));
        //        foreach (string sFileTmp in lsTmp)
        //        {
        //            string sFileName = _UTIL.GetLastString(_UTIL.GetFileNameFromFullFileName(sFileTmp), "\\");
        //            if (sFileName.IndexOf(sFilter) >= 0)
        //            {
        //                lsReturn.Add(sFileTmp);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        Directory.CreateDirectory(_sEMapBarImgDir);
        //    }
        //    return;
        //}

        string GetRandomBox(string[] saTmp)
        {
            return saTmp[DateTime.Now.Ticks % saTmp.Length];
        }

        private void btn_modify_Click(object sender, EventArgs e)
        {
            string sJobPath = _sEMapRawImgDir;
            string[] saDevID = new string[3] { "L", "R", "F" };
            string[] sasDesc = new string[4] { "CAM1", "CAM2", "CAM3", "CAM4" };
            string[] saTier = new string[3] { "1", "2", "3" };
            string[] saNextNode = new string[3] { "0010", "0020", "0030" };
            List<string> lsReturn = new List<string>();
            if (Directory.Exists(sJobPath))
            {
                
                List<string> lsTmp = new List<string>(Directory.GetFiles(sJobPath, "*.jp*"));
                foreach (string sFileTmp in lsTmp)
                {
                    string sDevID = GetRandomBox(saDevID);
                    string sTier = GetRandomBox(saTier);
                    string sDesc = GetRandomBox(sasDesc);
                    string sNextNode = GetRandomBox(saNextNode);

                    try
                    {
                        string sImgName = $"{sJobPath}{_UTIL.GetCurrentTime()}_{sDevID}_{sDesc}_{sTier}_{sNextNode}.jpg";
                        if (File.Exists(sImgName) == false)
                        {
                            File.Move(sFileTmp, sImgName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    Thread.Sleep(1);
                }
            }
            else
            {
                Directory.CreateDirectory(sJobPath);
            }
            return;
        }

        private void btn_Result_Click(object sender, EventArgs e)
        {
            CheckResult();
        }

        private void btn_OCR_Click(object sender, EventArgs e)
        {
            string sBuffer = string.Empty;
            string[] arrFiles = Directory.GetFiles(_sEMapRawImgDir, "*.jp*");
            string whitelist = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890.- ";
            //화이트리스트 사용할시 화이트리스트 목록
            //화이트리스트 적용
            foreach (string filename in arrFiles)
            {
                using (var bmp = (Bitmap)Bitmap.FromFile(filename))
                {
                    var engine = new TesseractEngine(@"d:\tessdata\", "eng", EngineMode.TesseractOnly);
                    engine.SetVariable("tessedit_char_whitelist", whitelist);
                    Pix pix = PixConverter.ToPix(bmp);
                    var result = engine.Process(pix);
                    
                    string sOCR_Result = result.GetText().Replace("\n", "\r\n");
                    if (sOCR_Result.Length > sBuffer.Length)
                    {
                        sBuffer = sOCR_Result;
                        //string sTmp = $"{filename}\r\n{sOCR_Result}";
                        //txt_OCR.AppendText(sTmp + "\r\n");
                    }
                    engine.Dispose();
                }
            }
            txt_OCR.AppendText(sBuffer + "\r\n");

        }
    }
}
