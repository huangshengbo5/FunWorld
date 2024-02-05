using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CodiceApp.EventTracking.Plastic;
using Config.JsonConfig;
using Newtonsoft.Json;
using NPOI.OpenXmlFormats.Dml;
using OfficeOpenXml.FormulaParsing.Excel.Functions;
using UnityEngine;

using UnityEngine;
using UnityEditor;
using EventType = UnityEngine.EventType;


//todo 重新打开编辑器显示旧数据不对
public class ImageClickEditorTool : EditorWindow
{
    private Texture2D ori_LoadedImage;
    private Texture2D target_LoadedImage;

    private Vector2 clickPos;
    
    //当前行按钮，已记录的区域信息列表
    private List<Rect> clickShowRects = new List<Rect>();

    //鼠标最新点击的位置
    private Vector2 clickShowPosition = Vector2.zero;
    
    private ConfigJsonManager JsonManager = ConfigJsonManager.Instance;
    private Dictionary<int, ConfigJsonBase> PointJsonConfig;
    

    private int rawIndex = 1;
    private string TexturePath = "Assets/Resources/Texture/";
    private string JsonPointPath = Path.Combine( ExcelExporterUtil.ClientExcelDataOutputPath,"TextureSelectPoint.bytes");
    private string JsonPointTextPath = Path.Combine( ExcelExporterUtil.ClientDataOutputPath,"TextureSelectPoint.txt");
    private string TextureSuffix = ".png";

    private Dictionary<int, ConfigJsonBase> SelectPoionJsonConfig = new Dictionary<int, ConfigJsonBase>();
    
    
    Vector2 scrollPos;

    private Vector2 targetImageRect = new Vector2(230, 230);
    
    [MenuItem("Tools/Open Image Clicker")]
    public static void ShowWindow()
    {
        GetWindow<ImageClickEditorTool>("Image Clicker");
    }

    string WidthStr = "20";
    string HeightStr = "20";
    private int WidthIntValue = 0;
    private int HeightIntValue = 0;
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(30,30,200,1000));
        GUILayout.Label("所有的图片行索引Index",EditorStyles.boldLabel);
        
        //显示可滑动列表
        scrollPos = GUILayout.BeginScrollView(scrollPos ,GUILayout.Width(200), GUILayout.Height(400));
        if (PointJsonConfig == null)
        {
            JsonManager.Init();
            PointJsonConfig = JsonManager.GetConfigs("CfgTexturePoint");
            SelectPoionJsonConfig = JsonManager.GetConfigs("CfgTextureSelectPoint");
        }
        var length = PointJsonConfig.Count;
        for (int i = 1; i <= length; i++)
        {
            if (GUILayout.Button("RawButton：" + i))
            {
                //选中了行按钮
                HandlerRowBtnClick(i);
            }
        }
        GUILayout.EndScrollView();
        
        GUILayout.EndArea();
        
        
        GUILayout.BeginArea(new Rect(230,30,200,200));
        GUILayout.Label("Load Image and Click:");
        GUILayout.Space(10);
        
        if (Event.current.type == EventType.MouseDown)
        {
            GUIUtility.keyboardControl = 0; // 释放键盘焦点
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            GUIUtility.keyboardControl = 0; // 释放键盘焦点
            GUI.FocusControl(null); // 释放焦点
        }
        //todo  宽度和高度值无法一直动态修改，只能在加载图片之前修改
        GUILayout.Label("请输入宽度：");
        WidthStr = EditorGUILayout.TextField("宽度：", WidthStr);
        GUILayout.Label("请输入高度：");
        HeightStr = EditorGUILayout.TextField("高度：", HeightStr);
        
        if (GUILayout.Button("清理当前图片已保存所有点"))
        {
            clickShowRects.Clear();
            SelectPoionJsonConfig.Clear();
            DeleteLastClick();
            SaveClickPositions();
        }

        if (GUILayout.Button("清除当前图片已保存的最新的点"))
        {
            if (clickShowRects.Count>0)
            {
                clickShowRects.RemoveAt(clickShowRects.Count-1);
            }

            if (SelectPoionJsonConfig.Count >0)
            {
                SelectPoionJsonConfig[SelectPoionJsonConfig.Count] = null;
            }
            DeleteLastClick();
            SaveClickPositions();
        }

        if (GUILayout.Button("清除刚按下的点"))
        {
            DeleteLastClick();
        }
        if (GUILayout.Button("SavePoints"))
        {
            SaveClickPositions();
        }
        
        GUILayout.EndArea();

        if (!int.TryParse(WidthStr, out WidthIntValue))
        {
            WidthIntValue = 0;
        }
        if (!int.TryParse(HeightStr, out HeightIntValue))
        {
            HeightIntValue = 0;
        }
        if (target_LoadedImage != null)
        {
            GUILayout.BeginArea(new Rect(targetImageRect.x,targetImageRect.y,target_LoadedImage.width + 10,target_LoadedImage.height+10 ));
            GUILayout.Label(target_LoadedImage, GUILayout.Width(target_LoadedImage.width), GUILayout.Height(target_LoadedImage.height));
            Event currentEvent = Event.current;
        
            // 监听鼠标点击事件
            Vector2 mousePoint = new Vector2();
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                Vector2 mousePosition = currentEvent.mousePosition;
                mousePoint.x = mousePosition.x + targetImageRect.x;
                mousePoint.y = mousePosition.y + targetImageRect.y ;
                clickShowPosition = mousePoint;
                Repaint();
            }
            GUILayout.EndArea();
        }
        if (ori_LoadedImage != null)
        {
            GUILayout.BeginArea(new Rect(targetImageRect.x + target_LoadedImage.width + 10,targetImageRect.y,ori_LoadedImage.width + 10,ori_LoadedImage.height+10 ));
            GUILayout.Label(ori_LoadedImage, GUILayout.Width(ori_LoadedImage.width), GUILayout.Height(ori_LoadedImage.height));
            GUILayout.EndArea();
        }
        
        DrawClickMark();
    }
    
    void DrawClickMark() 
    {
        //展示当前记录的区域信息
        foreach (var clickPos in clickShowRects)
        {
            Rect rect = new Rect(clickPos.x, clickPos.y, clickPos.width, clickPos.height);
            Color color = new Color(0,0,0,0);
            Handles.DrawSolidRectangleWithOutline(rect,color,Color.red);
        }

        if (IsHaveLastClick())
        {
            //展示最后一次点击的区域信息
            var lastRect = GetLastClickShowRect();
            Handles.DrawSolidRectangleWithOutline(lastRect, new Color(0,0,0,0),Color.green);
        }
    }

    //获取最后一次点击的显示Rect
    Rect GetLastClickShowRect()
    {
        Vector2 startPos = new Vector2();
        startPos.x = clickShowPosition.x - WidthIntValue / 2;
        startPos.y = clickShowPosition.y - HeightIntValue / 2;
        var lastPointRect = new Rect(startPos.x,startPos.y,WidthIntValue,HeightIntValue);
        return lastPointRect;
    }

    
    //获取最后一次点击的保存Rect
    Rect GetLastClickSaveRect()
    {
        Vector2 startPos = new Vector2();
        startPos.x = clickShowPosition.x - WidthIntValue / 2 - targetImageRect.x;
        startPos.y = clickShowPosition.y - HeightIntValue / 2 - targetImageRect.y;
        var lastPointRect = new Rect(startPos.x,startPos.y,WidthIntValue,HeightIntValue);
        return lastPointRect;
    }

    //判断是否有新点击
    bool IsHaveLastClick()
    {
        return clickShowPosition != Vector2.zero;
    }

    //清除最后一次点击
    void DeleteLastClick()
    {
        clickShowPosition = Vector2.zero;
    }

    //行索引按钮的点击事件
    void HandlerRowBtnClick(int index)
    {
        clickShowRects.Clear();
        rawIndex = index;
        Debug.Log("HandlerRowBtnClick,index:"+index);
        var info = PointJsonConfig[index] as CfgTexturePoint;
        var oriImagePath =  Path.Combine(TexturePath, info.Ori_TexturePath+TextureSuffix); 
        var targetImagePath = Path.Combine(TexturePath, info.Target_TexturePath+TextureSuffix);
        ori_LoadedImage = new Texture2D(2, 2);
        byte[] fileData = System.IO.File.ReadAllBytes(oriImagePath);
        ori_LoadedImage.LoadImage(fileData);
        target_LoadedImage = new Texture2D(2, 2);
        fileData = System.IO.File.ReadAllBytes(targetImagePath);
        target_LoadedImage.LoadImage(fileData);
        CollectOldRects();
    }

    void CollectOldRects()
    {
        //显示已记录的点位置
        if (SelectPoionJsonConfig != null)
        {
            var selectInfo = new CfgTextureSelectPoint();
            if (SelectPoionJsonConfig.ContainsKey(rawIndex))
            {
                selectInfo = SelectPoionJsonConfig[rawIndex] as CfgTextureSelectPoint;
                var pointStrList = selectInfo.Points;
                List<Vector2> pointList = new List<Vector2>();
                List<Rect> rectList = new List<Rect>();
                foreach (var pointStrItem in pointStrList)
                {
                    var strList = pointStrItem.Split(',');
                    //显示的位置需要进行修正
                    var x = int.Parse(strList[0]) + targetImageRect.x;
                    var y = int.Parse(strList[1]) + targetImageRect.y;
                    var width = int.Parse(strList[2]);
                    var height = int.Parse(strList[3]);
                    rectList.Add(new Rect(x,y,width,height));
                }
                clickShowRects = rectList;
            }
        }
        DrawClickMark();
        Repaint();
    }

    //保存所有的点击位置点
    void SaveClickPositions()
    {
        if (SelectPoionJsonConfig == null)
        {
            SelectPoionJsonConfig = new Dictionary<int, ConfigJsonBase>();
        }
        var info = new CfgTextureSelectPoint();
        if (SelectPoionJsonConfig != null && SelectPoionJsonConfig.ContainsKey(rawIndex))
        {
            info = SelectPoionJsonConfig[rawIndex] as CfgTextureSelectPoint;
        }
        else
        {
            info.Points = new List<string>();
        }

        var saveRect = GetLastClickSaveRect();
        var pointStr = saveRect.x + "," + saveRect.y+","+saveRect.width+","+saveRect.height;
        info.ID = rawIndex;
        info.Points.Add(pointStr);
        
        //增删
        if (SelectPoionJsonConfig.ContainsKey(rawIndex))
        {
            SelectPoionJsonConfig[rawIndex] = info;
        }
        else
        {
            SelectPoionJsonConfig.Add(rawIndex,info);
        }
        var objContainer = new ConfigJsonContainer();;
        objContainer.typeName = "CfgTextureSelectPoint";
        foreach (var selectPointJsonConfigItem in SelectPoionJsonConfig)
        {
            var cfg = selectPointJsonConfigItem.Value as ConfigJsonBase;
            objContainer.dataMap.Add(cfg.ID, cfg);
        }
        JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
        var content = JsonConvert.SerializeObject(objContainer, settings);
        File.WriteAllText(JsonPointPath, content);
        File.WriteAllText(JsonPointTextPath, content);
        AssetDatabase.Refresh();
        DeleteLastClick();
        Debug.Log("保存成功！");
        CollectOldRects();
    }
}

public class ImageWindow : EditorWindow {

    private Texture2D image;

    public static void ShowWindow(Texture2D img) {
        ImageWindow window = EditorWindow.GetWindow<ImageWindow>();
        window.image = img;
        window.titleContent = new GUIContent("Image Window");
    }

    void OnGUI() {
        if (image != null)
        {
            GUILayout.Label(image, GUILayout.Width(image.width), GUILayout.Height(image.height));
        } else {
            GUILayout.Label("No image selected.");
        }
    }
}