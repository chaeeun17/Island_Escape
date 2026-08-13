using UnityEngine;
using System.Collections;

public class Campfire : MonoBehaviour
{
    [Header("상태")]
    public bool isLit = false;

    [Header("시각 효과")]
    public ParticleSystem fireParticles;
    public Light fireLight;

    [Header("요리 설정")]
    public float cookTime = 3f;

    [Header("휴식 설정 (체력 회복)")]
    public float restTickInterval = 2.0f; // 몇 초마다 체력을 회복할지
    public int staminaRestoreAmount = 5;  // 한 번에 회복할 체력량

    // 휴식 시스템 관리를 위한 변수
    private bool isPlayerInZone = false;
    private Coroutine restCoroutine;

    private void Start()
    {
        UpdateFireState();
    }

    public void Ignite()
    {
        isLit = true;
        UpdateFireState();

        // 불을 켰을 때 플레이어가 이미 주변에 있다면 즉시 휴식 시작
        if (isPlayerInZone && restCoroutine == null)
        {
            restCoroutine = StartCoroutine(RestRoutine());
        }
    }

    public void Extinguish()
    {
        isLit = false;
        UpdateFireState();

        if (restCoroutine != null)
        {
            StopCoroutine(restCoroutine);
            restCoroutine = null;
        }
    }

    private void UpdateFireState()
    {
        if (fireParticles != null)
        {
            if (isLit) fireParticles.Play();
            else fireParticles.Stop();
        }
        if (fireLight != null) fireLight.enabled = isLit;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 불 켜기 로직
        if (other.CompareTag("Torch") && !isLit)
        {
            Ignite();
            return;
        }

        // 2. 플레이어 접근 시 휴식 영역 진입
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            Debug.Log("🔥 플레이어가 모닥불 주변에 접근했습니다. (휴식 가능)");

            // 불이 켜져 있다면 체력 회복 코루틴 시작
            if (isLit && restCoroutine == null)
            {
                restCoroutine = StartCoroutine(RestRoutine());
            }
        }

        // 3. 요리 로직 
        if (isLit)
        {
            WorldItem worldItem = other.GetComponent<WorldItem>();

            if (worldItem != null && worldItem.itemData != null)
            {
                if (worldItem.itemData.GetItemType() == ItemType.RawFood)
                {
                    StartCoroutine(CookFood(other.gameObject, worldItem.itemData));
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 4. 플레이어가 휴식 영역을 벗어났을 때
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            Debug.Log("💨 플레이어가 모닥불 주변을 벗어났습니다. (휴식 중단)");

            // 진행 중인 회복 코루틴이 있다면 정지
            if (restCoroutine != null)
            {
                StopCoroutine(restCoroutine);
                restCoroutine = null;
            }
        }
    }

    // 휴식(체력 회복) 코루틴
    private IEnumerator RestRoutine()
    {
        // 불이 켜져 있고 플레이어가 영역 안에 있는 동안 반복 실행
        while (isLit && isPlayerInZone)
        {
            // 정해진 시간(restTickInterval)만큼 대기
            yield return new WaitForSeconds(restTickInterval);

            // PlayerStats 싱글톤을 통해 체력 회복
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.RestoreStamina(staminaRestoreAmount);
                // Debug.Log($"[휴식 중] 체력 {staminaRestoreAmount} 회복됨."); 
            }
        }

        restCoroutine = null; // 루프가 끝나면 코루틴 초기화
    }

    private IEnumerator CookFood(GameObject rawFoodObject, Item rawFoodData)
    {
        Debug.Log($"{rawFoodData.getItemName()} 요리 시작...");

        yield return new WaitForSeconds(cookTime);

        if (rawFoodObject != null)
        {
            Vector3 spawnPos = rawFoodObject.transform.position;
            Item cookedItemData = rawFoodData.GetCookedVersion();

            if (cookedItemData != null && cookedItemData.GetPrefab3D() != null)
            {
                Instantiate(cookedItemData.GetPrefab3D(), spawnPos, Quaternion.identity);
                Debug.Log("요리 완성!");
            }
            else
            {
                Debug.LogWarning("결과물 데이터나 3D 프리팹이 등록되지 않았습니다.");
            }

            Destroy(rawFoodObject);
        }
    }
}