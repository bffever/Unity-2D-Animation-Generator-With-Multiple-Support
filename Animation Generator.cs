using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Animations;
public class AnimationGenerator :EditorWindow
{
    private class CustomPair<T1,T2>
    {
      public  T1 Item1;
      public  T2 Item2;
    }


    [MenuItem("Custom Tools/Animation Generator")]
    private static void ShowWindow()
    {
        var window=EditorWindow.GetWindow<AnimationGenerator>("Animation Generator");
        window.minSize = new Vector2(800,800);
        window.Show();
    }


    private void OnEnable()
    {
        m_animation_path = new List<string>();
        m_animator_controller_path = new List<string>();
        m_animation_state_list=new List<CustomPair<int, string>>();
    }

    private void OnDisable()
    {
        m_animation_path.Clear();
        m_animator_controller_path.Clear();
        m_animation_state_list.Clear();
    }

    private void GeneratePathField(List<string> list,int path_count)
    {


        if(path_count>list.Count)
        {
            path_count = path_count-list.Count;
            for(int i=0;i<path_count;i++)
            {
                list.Add("");
            }
        }else if(path_count<list.Count)
        {
          if(path_count==0)
            {
                list.Clear();
            }else
            {

                path_count = list.Count - path_count;
                list.RemoveRange(list.Count - 1-path_count, path_count);

            }


        }

        if (list.Count > 0)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = EditorGUILayout.TextField("Path:", list[i]);
            }
        }
    }

    private void GenerateStateConditionField(List<CustomPair<int,string>> list,int total_count)
    {

        if (total_count > list.Count)
        {
            total_count = total_count - list.Count;
            for (int i = 0; i < total_count; i++)
            {
                list.Add(new CustomPair<int, string>());
            }
        }
        else if (total_count < list.Count)
        {
            if (total_count <= 0)
            {
                list.Clear();
            }else
            {
                total_count=list.Count - total_count;
                list.RemoveRange(list.Count-1-total_count, total_count);
            }


        }

        if (list.Count > 0)
        {

            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i].Item1=EditorGUILayout.IntField("Time:",list[i].Item1);
                list[i].Item2=EditorGUILayout.TextField("State name:", list[i].Item2);
                EditorGUILayout.EndHorizontal();
            }
        }


    }

    private void Generate()
    {
        var guids = Selection.assetGUIDs;
        var assets=new List<Texture2D>();

        foreach(var guid in guids)
        {
            GUIDToList(guid, assets);
        }

        if(assets.Count==0)
        {
            return;
        }

        foreach(var asset in assets)
        {
            GenerateProcess(asset);
        }

        
    }
    private void GUIDToList(string guid,List<Texture2D> list)
    {
        var path=AssetDatabase.GUIDToAssetPath(guid);
        var asset=AssetDatabase.LoadAssetAtPath<Object>(path);

        if(asset == null)
        {
            return;
        }

        if(asset is Texture2D texture)
        {
            list.Add(texture);
        }

    }

    private void GenerateProcess(Texture2D list)
    {
        var path=AssetDatabase.GetAssetPath(list);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();

        if (sprites.Length <= 0)
        {
            return;
        }

        m_animation_path.Add(list.name);
        var animation_path = Path.Combine(m_animation_path.ToArray());

        if(!Directory.Exists(animation_path))
        {
            Directory.CreateDirectory(animation_path);
        }

        var animator_path = Path.Combine(m_animator_controller_path.ToArray());

        if (!Directory.Exists(animator_path))
        {
            Directory.CreateDirectory(animator_path);
        }

        var timer = 0f;
        var animation_clip=new AnimationClip();
        var key_frame_list=new List<ObjectReferenceKeyframe>();
        var editor_curve_binding = new EditorCurveBinding()
        {
            path="",
            propertyName="m_Sprite",
            type=typeof(SpriteRenderer),
        };
        
        var states=new Dictionary<int,string>();
        for(int i=0;i<m_animation_state_count;i++)
        {
            states.Add(m_animation_state_list[i].Item1, m_animation_state_list[i].Item2 );
        }

        var anim_controller=AnimatorController.CreateAnimatorControllerAtPath(Path.Combine(animator_path,list.name+".controller"));

        for (int i = 0; i < sprites.Length; i++)
        {
            var sprite = sprites[i];    
            var key_frame=new ObjectReferenceKeyframe();
            var time=timer*(1/m_frame_rate); 
            key_frame.value = sprite;
            key_frame.time = time;

            key_frame_list.Add(key_frame);

            if (states.ContainsKey(i))
            {
                AnimationUtility.SetObjectReferenceCurve(animation_clip,editor_curve_binding,key_frame_list.ToArray());
                AssetDatabase.CreateAsset(animation_clip, Path.Combine(animation_path, states[i] + ".anim"));
                var state = anim_controller.layers[0].stateMachine.AddState(states[i]);
                state.motion = animation_clip;

                timer = 0;
                key_frame_list.Clear();
                animation_clip = new AnimationClip();

            }else
            {
                timer++;
            }

        }



    }

    private void OnGUI()
    {
        var title_style=new GUIStyle(GUI.skin.box);
        title_style.alignment = TextAnchor.MiddleCenter;

        var font_style=new GUIStyle(GUI.skin.textField);
        font_style.alignment = TextAnchor.MiddleRight;


        EditorGUILayout.LabelField("Animation Generator",title_style,GUILayout.ExpandWidth(true));

        GUILayout.BeginVertical("HelpBox");


        EditorGUILayout.LabelField("Animation Path");

        EditorGUILayout.BeginVertical("GroupBox");

        EditorGUILayout.BeginHorizontal();

        if(GUILayout.Button("Clear",GUILayout.Width(100)))
        {
            m_animation_path_count = 0;
            Repaint();
        }

        GUILayout.FlexibleSpace();



        if (GUILayout.Button("+", GUILayout.Width(50)))
        {
            m_animation_path_count++;
        }

        if (GUILayout.Button("-", GUILayout.Width(50)))
        {
            m_animation_path_count=Mathf.Max(0,m_animation_path_count-=1);
        }

        m_animation_path_count = Mathf.Max(0,EditorGUILayout.IntField(m_animation_path_count, font_style, GUILayout.Width(40)));



        EditorGUILayout.EndHorizontal();

        GeneratePathField(m_animation_path, m_animation_path_count);

        EditorGUILayout.EndVertical();

        EditorGUILayout.LabelField("Animator Controller Path", GUILayout.ExpandWidth(true));

        EditorGUILayout.BeginVertical("GroupBox");

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear", GUILayout.Width(100)))
        {
            m_animator_path_count = 0;
            Repaint();
        }

        GUILayout.FlexibleSpace();



        if (GUILayout.Button("+", GUILayout.Width(50)))
        {
            m_animator_path_count++;
        }

        if (GUILayout.Button("-", GUILayout.Width(50)))
        {
            m_animator_path_count = Mathf.Max(0, m_animator_path_count -= 1);
        }

        m_animator_path_count = Mathf.Max(0, EditorGUILayout.IntField(m_animator_path_count, font_style, GUILayout.Width(40)));



        EditorGUILayout.EndHorizontal();

        GeneratePathField(m_animator_controller_path, m_animator_path_count);

        EditorGUILayout.EndVertical();


        EditorGUILayout.LabelField("Animator State ", GUILayout.ExpandWidth(true));


        EditorGUILayout.BeginVertical("GroupBox");

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear", GUILayout.Width(100)))
        {
            m_animation_state_count = 0;
            Repaint();
        }

        GUILayout.FlexibleSpace();



        if (GUILayout.Button("+", GUILayout.Width(50)))
        {
            m_animation_state_count++;
        }

        if (GUILayout.Button("-", GUILayout.Width(50)))
        {
            m_animation_state_count = Mathf.Max(0, m_animation_state_count -= 1);
        }

        m_animation_state_count = Mathf.Max(0, EditorGUILayout.IntField(m_animation_state_count, font_style, GUILayout.Width(40)));



        EditorGUILayout.EndHorizontal();

        GenerateStateConditionField(m_animation_state_list, m_animation_state_count);

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        m_frame_rate = Mathf.Max(0, EditorGUILayout.FloatField("Frame rate", m_frame_rate,font_style,GUILayout.Width(300)));
        EditorGUILayout.EndHorizontal();


        if(GUILayout.Button("Generate"))
        {
            Generate();
        }



        GUILayout.EndVertical();


    }
    private float m_frame_rate=60;
    private int m_animation_path_count, m_animator_path_count , m_animation_state_count;
    private List<string> m_animation_path, m_animator_controller_path;
    private List<CustomPair<int,string>> m_animation_state_list;
    
    
}
