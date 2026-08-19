using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion;

public class NetworkCampfire : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnFireStateChanged))]
    public NetworkBool isLit { get; set; } = false;

    [Header("시각 효과")]
    [SerializeField] private GameObject fxCampfire;

    [Header("Resources 내 SO 저장 경로")]
    [SerializeField] private string soFolderPath = "Items/";

    [Header("요리 설정")]
    public float cookTime = 3f;

    [Header("휴식 설정 (스태미나 회복)")]
    [Tooltip("모닥불 중심으로부터의 회복 반경 (미터)")]
    public float healRadius = 3.5f;
    public float restTickInterval = 2.0f;
    public int staminaRestoreAmount = 5;

    [Header("플레이어 레이어 (선택 사항, 기본 Everything)")]
    public LayerMask playerLayer = ~0;

    private Coroutine restCoroutine;


    private void Awake()
    {
        if (fxCampfire != null)
        {
            fxCampfire.SetActive(false);
        }
    }

    public override void Spawned()
    {
        UpdateFireState();
    }

    private void OnFireStateChanged()
    {
        UpdateFireState();
    }

    public void Ignite()
    {
        RPC_SetFireState(true);
    }

    public void Extinguish()
    {
        RPC_SetFireState(false);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetFireState(bool lit)
    {
        isLit = lit;
        UpdateFireCoroutine();
    }

    private void UpdateFireState()
    {
        if (fxCampfire != null)
        {
            fxCampfire.SetActive(isLit);
        }

        UpdateFireCoroutine();
    }

    private void UpdateFireCoroutine()
    {
        if (isLit && HasStateAuthority)
        {
            if (restCoroutine == null)
            {
                restCoroutine = StartCoroutine(ActiveZoneScanRoutine());
            }
        }
        else
        {
            if (restCoroutine != null)
            {
                StopCoroutine(restCoroutine);
                restCoroutine = null;
            }
        }
    }

    private IEnumerator ActiveZoneScanRoutine()
    {
        while (isLit)
        {
            yield return new WaitForSeconds(restTickInterval);

            // 모닥불 반경 healRadius 내의 모든 콜라이더 검색
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, healRadius, playerLayer);
            
            HashSet<NetworkPlayerStats> detectedPlayers = new HashSet<NetworkPlayerStats>();

            foreach (var col in hitColliders)
            {
                NetworkPlayerStats stats = col.GetComponentInParent<NetworkPlayerStats>();
                if (stats != null && stats.Object != null && stats.Object.IsValid)
                {
                    detectedPlayers.Add(stats);
                }
            }

            // 반경 안의 플레이어에게만 스태미나 지급
            if (detectedPlayers.Count > 0)
            {
                foreach (var player in detectedPlayers)
                {
                    player.RestoreStamina(staminaRestoreAmount);
                }
            }
        }

        restCoroutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 횃불 감지
        bool isTorch = other.gameObject.name.Contains("Torch") || 
                       other.gameObject.CompareTag("Torch") ||
                       (other.transform.parent != null && (other.transform.parent.name.Contains("Torch") || other.transform.parent.CompareTag("Torch")));

        if (isTorch && !isLit)
        {
            Debug.Log("🔥 [Campfire] 횃불 감지! 점화합니다.");
            Ignite();
            return;
        }

        // 2. 요리 로직 (호스트)
        if (isLit && HasStateAuthority)
        {
            NetworkObject foodNetworkObj = other.GetComponentInParent<NetworkObject>();
            if (foodNetworkObj == null) return;

            string cleanPrefabName = foodNetworkObj.gameObject.name.Replace("(Clone)", "").Trim();
            Item itemData = Resources.Load<Item>($"{soFolderPath}{cleanPrefabName}");

            if (itemData != null && itemData.GetItemType() == ItemType.RawFood)
            {
                StartCoroutine(CookFoodNetwork(foodNetworkObj, itemData));
            }
        }
    }

    private IEnumerator CookFoodNetwork(NetworkObject rawFoodNetworkObj, Item rawFoodData)
    {
        float timer = 0f;
        while (timer < cookTime)
        {
            timer += Time.deltaTime;
            if (rawFoodNetworkObj == null || !rawFoodNetworkObj.IsValid) yield break;
            yield return null;
        }

        if (rawFoodNetworkObj != null && rawFoodNetworkObj.IsValid)
        {
            Vector3 spawnPos = rawFoodNetworkObj.transform.position;
            Quaternion spawnRot = rawFoodNetworkObj.transform.rotation;

            Item cookedItemData = rawFoodData.GetCookedVersion();
            if (cookedItemData != null && cookedItemData.GetPrefab3D() != null)
            {
                Runner.Spawn(cookedItemData.GetPrefab3D(), spawnPos, spawnRot);
            }

            Runner.Despawn(rawFoodNetworkObj);
        }
    }
}