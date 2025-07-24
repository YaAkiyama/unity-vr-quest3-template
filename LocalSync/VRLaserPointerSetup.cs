using UnityEngine;
using UnityEditor;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VRレーザーポインターの自動セットアップ用エディタースクリプト
/// </summary>
public class VRLaserPointerSetup : EditorWindow
{
    [MenuItem("VR Tools/Setup Laser Pointers")]
    public static void ShowWindow()
    {
        GetWindow<VRLaserPointerSetup>("VR Laser Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("VR Laser Pointer Setup", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Left Hand Laser", GUILayout.Height(30)))
        {
            CreateLaserPointer(XRNode.LeftHand);
        }
        
        if (GUILayout.Button("Create Right Hand Laser", GUILayout.Height(30)))
        {
            CreateLaserPointer(XRNode.RightHand);
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Setup Both Hands", GUILayout.Height(40)))
        {
            CreateLaserPointer(XRNode.LeftHand);
            CreateLaserPointer(XRNode.RightHand);
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Laser Material", GUILayout.Height(30)))
        {
            CreateLaserMaterial();
        }
    }
    
    private void CreateLaserPointer(XRNode handNode)
    {
        // XR Originを探す
        XROrigin xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin not found in scene!");
            return;
        }
        
        // Camera Offsetを取得
        Transform cameraOffset = xrOrigin.CameraFloorOffsetObject?.transform ?? xrOrigin.transform;
        
        // GameObjectを作成
        string handName = handNode == XRNode.LeftHand ? "LeftHandLaser" : "RightHandLaser";
        GameObject laserObject = new GameObject(handName);
        laserObject.transform.SetParent(cameraOffset);
        
        // 位置を設定（コントローラーの推定位置）
        Vector3 localPosition = handNode == XRNode.LeftHand ? 
            new Vector3(-0.3f, -0.1f, 0.1f) : 
            new Vector3(0.3f, -0.1f, 0.1f);
        laserObject.transform.localPosition = localPosition;
        laserObject.transform.localRotation = Quaternion.identity;
        
        // VRLaserPointerスクリプトを追加
        VRLaserPointer laserPointer = laserObject.AddComponent<VRLaserPointer>();
        
        // XR Ray Interactorを追加
        XRRayInteractor rayInteractor = laserObject.AddComponent<XRRayInteractor>();
        
        // LineRendererを追加（VRLaserPointerのStart()で自動追加されるが、事前に追加）
        LineRenderer lineRenderer = laserObject.AddComponent<LineRenderer>();
        
        // XR Interaction Managerを取得して設定
        XRInteractionManager interactionManager = FindObjectOfType<XRInteractionManager>();
        if (interactionManager != null)
        {
            rayInteractor.interactionManager = interactionManager;
        }
        
        // レーザーマテリアルを設定
        Material laserMaterial = FindOrCreateLaserMaterial();
        
        // VRLaserPointerの設定をエディタで反映
        SerializedObject serializedPointer = new SerializedObject(laserPointer);
        serializedPointer.FindProperty("controllerNode").enumValueIndex = (int)handNode;
        serializedPointer.FindProperty("laserMaterial").objectReferenceValue = laserMaterial;
        serializedPointer.FindProperty("lineRenderer").objectReferenceValue = lineRenderer;
        serializedPointer.ApplyModifiedProperties();
        
        // オブジェクトを選択状態にする
        Selection.activeGameObject = laserObject;
        
        Debug.Log($"Created {handName} laser pointer successfully!");
    }
    
    private Material FindOrCreateLaserMaterial()
    {
        // 既存のレーザーマテリアルを検索
        string[] guids = AssetDatabase.FindAssets("LaserMaterial t:Material");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        
        // 新しいマテリアルを作成
        return CreateLaserMaterial();
    }
    
    private Material CreateLaserMaterial()
    {
        // Unlit/Colorシェーダーを使用したマテリアルを作成
        Material laserMaterial = new Material(Shader.Find("Unlit/Color"));
        laserMaterial.color = Color.red;
        laserMaterial.name = "LaserMaterial";
        
        // マテリアルを保存
        string folderPath = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        string assetPath = $"{folderPath}/LaserMaterial.mat";
        AssetDatabase.CreateAsset(laserMaterial, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Created laser material at {assetPath}");
        return laserMaterial;
    }
}

#if UNITY_EDITOR
/// <summary>
/// VRLaserPointerのカスタムインスペクター
/// </summary>
[CustomEditor(typeof(VRLaserPointer))]
public class VRLaserPointerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GUILayout.Space(10);
        
        VRLaserPointer laserPointer = (VRLaserPointer)target;
        
        if (Application.isPlaying)
        {
            GUILayout.Label($"Laser Active: {laserPointer.IsLaserActive}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Toggle Laser"))
            {
                if (laserPointer.IsLaserActive)
                    laserPointer.DisableLaser();
                else
                    laserPointer.EnableLaser();
            }
        }
        else
        {
            if (GUILayout.Button("Auto Setup Components"))
            {
                SetupComponents(laserPointer);
            }
        }
    }
    
    private void SetupComponents(VRLaserPointer laserPointer)
    {
        GameObject go = laserPointer.gameObject;
        
        // LineRendererを追加/取得
        LineRenderer lineRenderer = go.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = go.AddComponent<LineRenderer>();
        }
        
        // XRRayInteractorを追加/取得
        XRRayInteractor rayInteractor = go.GetComponent<XRRayInteractor>();
        if (rayInteractor == null)
        {
            rayInteractor = go.AddComponent<XRRayInteractor>();
            
            // XR Interaction Managerを設定
            XRInteractionManager interactionManager = FindObjectOfType<XRInteractionManager>();
            if (interactionManager != null)
            {
                rayInteractor.interactionManager = interactionManager;
            }
        }
        
        Debug.Log("VRLaserPointer components setup completed!");
    }
}
#endif