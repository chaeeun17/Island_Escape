using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion;

public class NetworkCampfire : NetworkBehaviour
{
    [Header("네트워크 상태")]
    [Networked, OnChangedRender(nameof(OnFireStateChanged))]
    public NetworkBool isLit { get; set; } = false;

    [Networked, OnChangedRender(nameof(OnBurntStateChanged))]
    public NetworkBool isBurntOut { get; set; } = false;

    [Header("시각 효과")]
    [SerializeField] private GameObject fxCampfire;

    [Header("장작 외형 오브젝트 교체")]
    [Tooltip("기본/점화 전후 일반 나무 메쉬 오브젝트")]
    [SerializeField] private GameObject normalWoodObject;
    [Tooltip("다 타고 난 뒤 표시될 검은 숯/탄 나무 오브젝트")]
    [SerializeField] private GameObject burntWoodObject;

    [Header("모닥불 지속 시간 (초)")]
    [Tooltip("불이 켜진 후 꺼질 때까지의 시간")]
    [SerializeField] private float fireDuration = 10f; // 테스트용 10초

    [Header("Resources 내 SO 저장 경로")]
    [SerializeField] private string soFolderPath = "Items/";

    [Header("요리 설정")]
    public float cookTime = 3f;

    [Header("휴식 설정 (스태미나 회복)")]
    public float healRadius = 1.5f;
    public float restTickInterval = 2.0f;
    public int staminaRestoreAmount = 5;

    [Header("플레이어 레이어")]
    public LayerMask playerLayer = ~0;

    private Coroutine restCoroutine;
    private Coroutine burnOutCoroutine;

    private void Awake()
    {
        if (fxCampfire != null) fxCampfire.SetActive(false);
        if (normalWoodObject != null) normalWoodObject.SetActive(true);
        if (burntWoodObject != null) burntWoodObject.SetActive(false);
    }

    public override void Spawned()
    {
        UpdateFireState();
        UpdateVisuals();
        CheckBurnTimerOnState();
    }

    // 🌟 [핵심] isLit 값이 변경되면(인스펙터, RPC, 횃불 등 모든 경로) 무조건 실행됨
    private void OnFireStateChanged()
    {
        UpdateFireState();
        CheckBurnTimerOnState();
    }

    private void OnBurntStateChanged()
    {
        UpdateVisuals();
    }

    // 🌟 불 켜짐 상태에 따라 타이머 코루틴을 자동으로 시작/정지
    private void CheckBurnTimerOnState()
    {
        if (isLit && !isBurntOut)
        {
            if (burnOutCoroutine != null) StopCoroutine(burnOutCoroutine);
            burnOutCoroutine = StartCoroutine(BurnOutRoutine());
            Debug.Log($"🔥 [Campfire] 불이 켜졌습니다! {fireDuration}초 뒤 자동으로 꺼집니다.");
        }
        else
        {
            if (burnOutCoroutine != null)
            {
                StopCoroutine(burnOutCoroutine);
                burnOutCoroutine = null;
            }
        }
    }

    // 🌟 인스펙터 컴포넌트 우클릭 메뉴로 테스트 점화 가능
    [ContextMenu("🔥 테스트: 불 켜기 (Ignite)")]
    public void Ignite()
    {
        if (isBurntOut)
        {
            Debug.LogWarning("⚠️ [Campfire] 이미 다 타버린 모닥불입니다.");
            return;
        }

        RPC_SetFireState(true);
    }

    [ContextMenu("💧 테스트: 불 끄기 (Extinguish)")]
    public void Extinguish()
    {
        RPC_SetFireState(false);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetFireState(bool lit)
    {
        isLit = lit;
        // OnFireStateChanged가 호출되면서 자동 처리됨
    }

    // 🌟 지속 시간 타이머 코루틴
    private IEnumerator BurnOutRoutine()
    {
        yield return new WaitForSeconds(fireDuration);

        Debug.Log("🪵 [Campfire] 시간이 다 되어 불이 꺼지고 탄 나무로 교체됩니다!");
        
        // 상태 변경
        if (HasStateAuthority || Runner == null)
        {
            isLit = false;
            isBurntOut = true;
        }
        else
        {
            // 클라이언트인 경우 상태 갱신 요청
            RPC_SetBurntOut();
        }

        UpdateFireState();
        UpdateVisuals();

        burnOutCoroutine = null;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetBurntOut()
    {
        isLit = false;
        isBurntOut = true;
    }

    private void UpdateFireState()
    {
        if (fxCampfire != null)
        {
            fxCampfire.SetActive(isLit);

            // 🌟 fxCampfire 하위 ParticleSystem들이 playOnAwake=false로 되어 있어
            //    SetActive(true)만으로는 재생되지 않는다. 명시적으로 Play()/Stop()을 호출해야 한다.
            //    루트에서 Play()/Stop()을 호출하면 기본값(withChildren: true)으로 자식 파티클까지 함께 처리된다.
            var fireParticles = fxCampfire.GetComponent<ParticleSystem>();
            if (fireParticles != null)
            {
                if (isLit) fireParticles.Play();
                else fireParticles.Stop();
            }
        }

        UpdateFireCoroutine();
    }

    private void UpdateVisuals()
    {
        bool burntState = false;

        if (Object != null && Object.IsValid)
        {
            try
            {
                burntState = isBurntOut;
            }
            catch (System.InvalidOperationException)
            {
                burntState = false;
            }
        }

        if (normalWoodObject != null)
        {
            normalWoodObject.SetActive(!burntState);
        }

        if (burntWoodObject != null)
        {
            burntWoodObject.SetActive(burntState);
        }
    }

    private void UpdateFireCoroutine()
    {
        if (isLit && (HasStateAuthority || Runner == null))
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
        bool isTorch = other.gameObject.name.Contains("Torch") || 
                       other.gameObject.CompareTag("Torch") ||
                       (other.transform.parent != null && (other.transform.parent.name.Contains("Torch") || other.transform.parent.CompareTag("Torch")));

        if (isTorch && !isLit && !isBurntOut)
        {
            Debug.Log("🔥 [Campfire] 횃불 감지! 점화합니다.");
            Ignite();
            return;
        }

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

    private void OnDrawGizmos()
    {
        bool currentLitState = false;

        if (Runner != null && Runner.IsRunning && Object != null && Object.IsValid)
        {
            try
            {
                currentLitState = isLit;
            }
            catch (System.InvalidOperationException)
            {
                currentLitState = false;
            }
        }

        Gizmos.color = currentLitState 
            ? new Color(1f, 0.5f, 0f, 0.15f) 
            : new Color(0.2f, 0.8f, 1f, 0.1f);
        Gizmos.DrawSphere(transform.position, healRadius);

        Gizmos.color = currentLitState ? Color.yellow : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}