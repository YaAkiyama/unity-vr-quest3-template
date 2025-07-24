using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VRコントローラー用レーザーポインターシステム
/// Meta Quest 3コントローラーのトリガー入力でレーザーのOn/Off制御
/// </summary>
public class VRLaserPointer : MonoBehaviour
{
    [Header("レーザー設定")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float laserMaxDistance = 10f;
    [SerializeField] private LayerMask raycastLayerMask = -1;
    
    [Header("ビジュアル設定")]
    [SerializeField] private Color laserColor = Color.red;
    [SerializeField] private float laserWidth = 0.02f;
    [SerializeField] private Material laserMaterial;
    
    [Header("コントローラー設定")]
    [SerializeField] private XRNode controllerNode = XRNode.RightHand;
    [SerializeField] private InputDeviceCharacteristics deviceCharacteristics = 
        InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice;
    
    // プライベート変数
    private InputDevice targetDevice;
    private bool isLaserActive = false;
    private RaycastHit raycastHit;
    private XRRayInteractor rayInteractor;
    
    // レーザーの状態管理
    private bool previousTriggerState = false;
    
    void Start()
    {
        InitializeLaserPointer();
        GetInputDevice();
    }
    
    void Update()
    {
        if (!targetDevice.isValid)
        {
            GetInputDevice();
            return;
        }
        
        HandleTriggerInput();
        
        if (isLaserActive)
        {
            UpdateLaserVisual();
        }
    }
    
    /// <summary>
    /// レーザーポインターの初期化
    /// </summary>
    private void InitializeLaserPointer()
    {
        // LineRendererコンポーネントを取得または追加
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }
        
        // LineRendererの基本設定
        lineRenderer.material = laserMaterial;
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = laserColor;
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false;
        
        // XR Ray Interactorを取得
        rayInteractor = GetComponent<XRRayInteractor>();
        
        Debug.Log($"VRLaserPointer initialized for {controllerNode}");
    }
    
    /// <summary>
    /// 入力デバイスの取得
    /// </summary>
    private void GetInputDevice()
    {
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(controllerNode, inputDevices);
        
        foreach (var device in inputDevices)
        {
            if (device.characteristics.HasFlag(deviceCharacteristics))
            {
                targetDevice = device;
                Debug.Log($"Found input device: {device.name} for {controllerNode}");
                break;
            }
        }
    }
    
    /// <summary>
    /// トリガー入力の処理
    /// </summary>
    private void HandleTriggerInput()
    {
        bool triggerValue = false;
        
        // トリガーボタンの状態を取得
        if (targetDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerValue))
        {
            // トリガーが押された瞬間を検出（トグル動作）
            if (triggerValue && !previousTriggerState)
            {
                ToggleLaser();
            }
            
            previousTriggerState = triggerValue;
        }
    }
    
    /// <summary>
    /// レーザーのOn/Off切り替え
    /// </summary>
    private void ToggleLaser()
    {
        isLaserActive = !isLaserActive;
        lineRenderer.enabled = isLaserActive;
        
        // XR Ray Interactorの有効/無効も連動
        if (rayInteractor != null)
        {
            rayInteractor.enabled = isLaserActive;
        }
        
        Debug.Log($"Laser {(isLaserActive ? "ON" : "OFF")} for {controllerNode}");
    }
    
    /// <summary>
    /// レーザービジュアルの更新
    /// </summary>
    private void UpdateLaserVisual()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition;
        
        // レイキャストを実行
        if (Physics.Raycast(transform.position, transform.forward, out raycastHit, laserMaxDistance, raycastLayerMask))
        {
            endPosition = raycastHit.point;
        }
        else
        {
            endPosition = startPosition + transform.forward * laserMaxDistance;
        }
        
        // LineRendererの位置を更新
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
    }
    
    /// <summary>
    /// レーザーの強制ON
    /// </summary>
    public void EnableLaser()
    {
        isLaserActive = true;
        lineRenderer.enabled = true;
        
        if (rayInteractor != null)
        {
            rayInteractor.enabled = true;
        }
    }
    
    /// <summary>
    /// レーザーの強制OFF
    /// </summary>
    public void DisableLaser()
    {
        isLaserActive = false;
        lineRenderer.enabled = false;
        
        if (rayInteractor != null)
        {
            rayInteractor.enabled = false;
        }
    }
    
    /// <summary>
    /// レーザーの現在の状態を取得
    /// </summary>
    public bool IsLaserActive => isLaserActive;
    
    /// <summary>
    /// 現在のレイキャストヒット情報を取得
    /// </summary>
    public RaycastHit GetCurrentHit() => raycastHit;
}