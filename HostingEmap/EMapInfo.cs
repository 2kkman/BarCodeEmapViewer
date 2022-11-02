using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Utils.Log;
using Common.Data.Objects;
using Common.Utils.Tools;
using System.IO;
using System.Drawing;
using System.Windows;

namespace Common
{
    public class eMap
    {
        static ConsoleUtil _UTIL = new ConsoleUtil();
        private static string _sLog = "eMap";

        public DragonFlyConfig DragonFly { get; set; }

        public EnviromentConfig Enviroment { get; set; }
        public dataNodeList[] NodeList { get; set; }
        public dataLinkList[] LinkList { get; set; }
        public dataObjectList[] ObjectList { get; set; }
        public Caption[] CaptionList { get; set; }

        /// </summary>
        public eMap()
        {
            try
            {
                DragonFly = new DragonFlyConfig();
                DragonFly.StartNodeID = "0001_0401";

                Enviroment = new EnviromentConfig();
                Enviroment.Width = 600;
                Enviroment.Depth = 300;
                Enviroment.Wall = 0;
                Enviroment.materialType = 0;
                Enviroment.MaxTier = 4;
                Enviroment.LinkWidth = 2.0;

                NodeList = new dataNodeList[2];
                for (int i = 0; i < NodeList.Length; i++)
                {
                    NodeList[i] = new dataNodeList();
                    NodeList[i].id = i.ToString();
                    NodeList[i].posX = 0;
                    NodeList[i].posY = 0;
                    NodeList[i].posZ = 0;
                }

                LinkList = new dataLinkList[1];
                for (int i = 0; i < LinkList.Length; i++)
                {
                    LinkList[i] = new dataLinkList();
                    LinkList[i].id = "10001";
                    LinkList[i].sNode = "0";
                    LinkList[i].eNode = "1";
                    LinkList[i].VertexCount = 0; ;
                }

                ObjectList = new dataObjectList[1];
                for (int i = 0; i < 1; i++)
                {
                    ObjectList[i] = new dataObjectList();
                    ObjectList[i].id = "0301";
                    ObjectList[i].type = "1";
                    ObjectList[i].materialType = "4";
                    ObjectList[i].width = 34.375;
                    ObjectList[i].height = 275;
                    ObjectList[i].depth = 61;
                    ObjectList[i].tier = 6;
                    ObjectList[i].posX = 51.5625;
                    ObjectList[i].posY = 0;
                    ObjectList[i].posX = 17.875;
                }

                CaptionList = new Caption[1];
                for (int i = 0; i < 1; i++)
                {
                    CaptionList[i] = new Caption();
                    CaptionList[i].TargetID = ObjectList[i].id;
                    CaptionList[i].Tier = i;
                    CaptionList[i].FontSize = 0.5;
                    CaptionList[i].FontColor = "FF000064";
                    CaptionList[i].Content = "테스트";
                }
            }
            catch (Exception ex)
            {
                LogManager.Trace0("eMap", "생성자 Exception : " + ex.ToString());
            }
        }

        public static void XMLEdit(string sXMLPath, string sPATHData)
        {
            eMap emapTmp = (eMap)XMLObject.XMLReadFile(sXMLPath, typeof(eMap));
            emapTmp = new eMap();
            DragonFlyConfig dfc = new DragonFlyConfig();
            dfc.StartNodeID = "9999";
            emapTmp.DragonFly = dfc;

            //TODO : 일단 ObjectList 부터
            EnviromentConfig ev = emapTmp.Enviroment;
            ev.Width = 2750;
            ev.Depth = 3500;
            ev.materialType = 0;
            ev.Wall = 0;
            ev.LinkWidth = 2.5;
            ev.MaxTier = 4;
            emapTmp.Enviroment = ev;

            List<dataObjectList> lsObjSmall = new List<dataObjectList>();
            List<dataNodeList> lsNodeListSmall = new List<dataNodeList>();
            List<dataLinkList> lsLinkListSmall = new List<dataLinkList>();
            List<Caption> lsCaptionListSmall = new List<Caption>();

            bool bOK = GenerateEMapEx(sPATHData, out lsObjSmall, out lsLinkListSmall, out lsNodeListSmall, out lsCaptionListSmall);
            emapTmp.ObjectList = lsObjSmall.ToArray();
            emapTmp.LinkList = lsLinkListSmall.ToArray();
            emapTmp.NodeList = lsNodeListSmall.ToArray();
            emapTmp.CaptionList = lsCaptionListSmall.ToArray();

            XMLObject.XMLWriteFile(sXMLPath, emapTmp);
            //FormClose(this, true);
            //string sMsg = _UTIL.getXMLFromModel(emapTmp);
            //LogManager.Trace0(_sLog, "{0}-{1}",sMsg);
        }

        static bool GenerateEMapEx(string sEmapPath, out List<dataObjectList> lsObjSmall, out List<dataLinkList> lsLinkListSmall, out List<dataNodeList> lsNodeListSmall, out List<Caption> lsCaptionListSmall)
        {
            bool bReturn = true;
            try
            {
                Dictionary<string, dataObjectList> dicEmap = new Dictionary<string, dataObjectList>();

                dataNodeList dnOrigin = new dataNodeList();
                int iObjType = 3;
                int iMaterialType = 8;
                //int iBlockWidth = 53;

                int iDFMarginSpace = 60;
                int iCellDepth = 25;
                int iCellHeight = 53;

                lsObjSmall = new List<dataObjectList>();
                lsNodeListSmall = new List<dataNodeList>();
                lsLinkListSmall = new List<dataLinkList>();
                lsCaptionListSmall = new List<Caption>();

                int iCurrentAngle = 180; //시작시 뒤집혀져있음
                System.Drawing.Point ptDF_Center_Old = new System.Drawing.Point(1000, 200); //시작 지점.
                dnOrigin.posX = ptDF_Center_Old.X;  //데이터노드 - 시작지점
                dnOrigin.posZ = ptDF_Center_Old.Y;
                dnOrigin.posY = 0;
                dnOrigin.id = "99999";
                dnOrigin.type = "0";
                System.Drawing.Point ptDF_AntPoint = new System.Drawing.Point();
                lsNodeListSmall.Add(dnOrigin);

                bool bResult = true;
                List<Dictionary<string, string>> lsDicEmap = new List<Dictionary<string, string>>(_UTIL.Strings2DicArray(File.ReadAllLines(sEmapPath), true, "\t"));

                int iCntNode = 0;


                foreach (Dictionary<string, string> dicCurrentTmp in lsDicEmap)
                {

                    int iNodeLength = -1;
                    string sLast = dicCurrentTmp[MasterField.LASTSEEN];
                    string sTYPE = dicCurrentTmp[MasterField.TYPE];
                    string sMOVE_ = dicCurrentTmp[MasterField.MOVE_];
                    string sTier = dicCurrentTmp[MasterField.TIER];
                    string sANGLE = dicCurrentTmp[MasterField.ANGLE];
                    bool bValidLength = int.TryParse(dicCurrentTmp[MasterField.LENGTH], out iNodeLength);
                    int iCurrentAngleTmp = -1;
                    bool bValidAngle = int.TryParse(dicCurrentTmp[MasterField.ANGLE], out iCurrentAngleTmp);
                    bool bRightDirection = sMOVE_.IndexOf("R") >= 0;


                    //안테나 포인터 좌표 계산 - 안테나 포인터의 궤적이 선반 위치가 된다.
                    //이 부분을 잘 반영해야 함. DF의 두께와 너비를 반영.
                    if (bValidAngle)
                    {
                        iCurrentAngle += iCurrentAngleTmp;
                    }
                    else
                    {
                        if (sMOVE_.StartsWith("CC"))
                        {
                            iCurrentAngle += 270;
                        }
                        else if (sMOVE_.StartsWith("CW"))
                        {
                            iCurrentAngle += 90;
                        }
                    }
                    iCurrentAngle = iCurrentAngle % 360;

                    System.Drawing.Point ptDF_Center_New = GetNextPoint(iCurrentAngle, bRightDirection, iNodeLength, ptDF_Center_Old); //경로 이동 후 포인트
                    System.Drawing.Point ptDF_Center_Mid = getMidpoint(ptDF_Center_Old, ptDF_Center_New);  //이동전과 이동후의 중점포인트.
                    ptDF_AntPoint = GetDFPointer(iCurrentAngle, iDFMarginSpace, ptDF_Center_Mid); //중점 포인트의 DF안테나 위치 (선반의 센터가 됨)

                    dataNodeList dnCurrentPos = new dataNodeList();
                    dnCurrentPos.posX = ptDF_Center_New.X;
                    dnCurrentPos.posZ = ptDF_Center_New.Y;
                    dnCurrentPos.id = string.Format("{0}{1}", "9", sLast);
                    dnCurrentPos.type = "0";

                    dataLinkList dlCurrent = new dataLinkList();
                    dlCurrent.VertexCount = 0;
                    dlCurrent.eNode = dnCurrentPos.id;
                    dlCurrent.sNode = lsNodeListSmall[lsNodeListSmall.Count - 1].id;
                    dlCurrent.id = string.Format("{0}{1}", "8", sLast);


                    if (iCntNode < 100000)
                    {
                        lsNodeListSmall.Add(dnCurrentPos);
                        lsLinkListSmall.Add(dlCurrent);
                    }

                    //이거 이후 iCurrentZ,X 값 조절 필요.
                    //RFID가 없으면 dObj 를 생성하지 않는다.다만 Angel 에 따라 거리만 늘려줌.
                    if (sTier.Equals("0") == false)
                    {
                        iCntNode++;
                        dataObjectList dObj = new dataObjectList();
                        //dObj.id = int.Parse(dicCurrentTmp[MasterField.START]).ToString();
                        dObj.id = sLast;
                        dObj.type = iObjType.ToString();
                        dObj.materialType = iMaterialType.ToString();
                        ushort usTier = 0;
                        bool bTierValue = ushort.TryParse(sTier, out usTier);
                        dObj.tier = usTier;

                        int iTierCnt = 4 < dObj.tier ? 4 : dObj.tier;

                        for (int j = 0; j <= iTierCnt; j++)
                        {
                            Caption cp = new Caption();
                            cp.Tier = j;
                            cp.FontSize = 1.5;
                            cp.FontColor = "#000000";
                            cp.TargetID = dObj.id;
                            cp.Content = string.Empty;
                            //cp.Content = string.Format("{0}-{1}", cp.TargetID, cp.Tier);
                            lsCaptionListSmall.Add(cp);
                        }

                        //TODO : MOVE_ 를 파싱해서 Angel 구하는 루틴

                        dObj.width = iNodeLength;
                        dObj.height = iCellHeight * dObj.tier;
                        dObj.angle = iCurrentAngle;
                        dObj.depth = iCellDepth;
                        dObj.posY = 0;
                        dObj.posX = ptDF_AntPoint.X;
                        dObj.posZ = ptDF_AntPoint.Y;

                        if (sLast.Equals("0300"))
                        {
                            dObj.posY = 0;
                        }
                        lsObjSmall.Add(dObj);
                    }
                    ptDF_Center_Old = new System.Drawing.Point(ptDF_Center_New.X, ptDF_Center_New.Y);
                } //End of Foreach
                  //lsNodeListSmall.Clear();
                  //lsLinkListSmall.Clear();

            }
            catch (Exception ex)
            {
                lsObjSmall = new List<dataObjectList>();
                lsLinkListSmall = new List<dataLinkList>();
                lsCaptionListSmall = new List<Caption>();
                lsNodeListSmall = new List<dataNodeList>();
                LogManager.Trace2(_sLog, "{0} - {1}", _UTIL.GetMethodName(), ex.ToString());
                bReturn = false;
            }
            return bReturn;
        }

        static System.Drawing.Point GetDFPointer(int iCurrentAngle, int iDFMarginSpace, System.Drawing.Point dfCenter)
        {
            double iCurrentX_PT, iCurrentZ_PT = 0;
            if (iCurrentAngle == 0) // 0도의 정의 - Right 으로 주행했을때 X축 값이 증가하는 상태
            {
                iCurrentX_PT = dfCenter.X;
                iCurrentZ_PT = dfCenter.Y + iDFMarginSpace;
            }
            else if (iCurrentAngle == 90) // 90도의 정의 - Right 으로 주행했을때 Z축 값이 감소하는 상태.
            {
                iCurrentX_PT = dfCenter.X + iDFMarginSpace;
                iCurrentZ_PT = dfCenter.Y;
            }
            else if (iCurrentAngle == 180)// 180도의 정의 - Right 으로 주행했을때 X축 값이 증가하는 상태.
            {
                iCurrentX_PT = dfCenter.X;
                iCurrentZ_PT = dfCenter.Y - iDFMarginSpace;
            }
            else// 270도의 정의 - Right 으로 주행했을때 Z축 값이 증가하는 상태.
            {
                iCurrentX_PT = dfCenter.X - iDFMarginSpace;
                iCurrentZ_PT = dfCenter.Y;
            }
            return new System.Drawing.Point((int)iCurrentX_PT, (int)iCurrentZ_PT);
        }

        static System.Drawing.Point getMidpoint(System.Drawing.Point x1, System.Drawing.Point x2) //두 점사이의 중점을 구하는 메서드
        {
            System.Drawing.Point result = new System.Drawing.Point((x1.X + x2.X) / 2, (x1.Y + x2.Y) / 2);
            return result;
        }
        static System.Drawing.Point GetNextPoint(int iCurrentAngle, bool bRightDirection, int iNodeLength, System.Drawing.Point ptOld)
        {
            double iCurrentX = ptOld.X;
            double iCurrentZ = ptOld.Y;

            //안테나 포인터 좌표 계산 - 안테나 포인터의 궤적이 선반 위치가 된다.
            //이 부분을 잘 반영해야 함. DF의 두께와 너비를 반영.
            if (iCurrentAngle == 0) // 0도의 정의 - Right 으로 주행했을때 X축 값이 증가하는 상태
            {
                if (bRightDirection)
                {
                    iCurrentX += iNodeLength;
                }
                else
                {
                    iCurrentX -= iNodeLength;
                }
            }
            else if (iCurrentAngle == 90) // 90도의 정의 - Right 으로 주행했을때 Z축 값이 감소하는 상태.
            {
                if (bRightDirection)
                {
                    iCurrentZ -= iNodeLength;
                }
                else
                {
                    iCurrentZ += iNodeLength;
                }
            }
            else if (iCurrentAngle == 180)// 180도의 정의 - Right 으로 주행했을때 X축 값이 감소하는 상태.
            {
                if (bRightDirection)
                {
                    iCurrentX -= iNodeLength;
                }
                else
                {
                    iCurrentX += iNodeLength;
                }
            }
            else// 270도의 정의 - Right 으로 주행했을때 Z축 값이 증가하는 상태.
            {
                if (bRightDirection)
                {
                    iCurrentZ += iNodeLength;
                }
                else
                {
                    iCurrentZ -= iNodeLength;
                }
            }
            return new System.Drawing.Point((int)iCurrentX, (int)iCurrentZ);
        }


    }

    /// <summary>
    /// Antenna Port Setting Value
    /// </summary>
    public class EnviromentConfig
    {
        public double Width { get; set; }
        public double Depth { get; set; }
        public double Wall { get; set; }
        public double LinkWidth { get; set; }
        public double MaxTier { get; set; }
        public double materialType { get; set; }
    }
    public class DragonFlyConfig
    {
        public string StartNodeID { get; set; }
    }


    public class dataNodeList
    {
        public string id { get; set; }
        public string type { get; set; }
        public double posX { get; set; }
        public double posY { get; set; }
        public double posZ { get; set; }
    }
    public class dataLinkList
    {
        public string id { get; set; }
        public string sNode { get; set; }
        public string eNode { get; set; }
        public ushort VertexCount { get; set; }
    }

    public class dataObjectList
    {
        public string id { get; set; }
        public string type { get; set; }
        public string materialType { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public double depth { get; set; }
        public double angle { get; set; }
        public ushort tier { get; set; }
        public double posX { get; set; }
        public double posY { get; set; }
        public double posZ { get; set; }
    }

    public class Caption
    {
        public string TargetID { get; set; }
        public double Tier { get; set; }
        public double FontSize { get; set; }
        public string FontColor { get; set; }
        public string Content { get; set; }
    }
}
