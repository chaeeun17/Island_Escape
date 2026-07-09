using UnityEngine;

public enum FloatState
{
    Ready,      // 낚싯대에 붙어있음
    Flying,     // 날아가는 중
    Floating,   // 물 위에 떠있음
    Reeling     // 회수 중
}

public class FishingCaster : MonoBehaviour
{
    public Transform floatHolder;
    public GameObject fishingFloat;
    public Transform rodTip;
    public Rigidbody floatRb;

    [Header("Line")]
    public float maxLineLength = 15f;
    public float reelSpeed = 2f;

    [Header("Tension")]
    public float tensionStrength = 40f;

    public bool IsCasting { get; private set; }

    private FloatState state = FloatState.Ready;

    public void Cast(Vector3 velocity)
    {
        fishingFloat.transform.SetParent(null);
        state = FloatState.Flying;

        floatRb.isKinematic = false;
        floatRb.linearVelocity = velocity;
    }

    void FixedUpdate()
    {
        if (Vector3.Distance(rodTip.position, fishingFloat.transform.position) > maxLineLength)
        {
            ResetFloat();
        }
    }

    public void OnHitWater(Vector3 waterPoint)
    {
        state = FloatState.Floating;

        floatRb.linearVelocity = Vector3.zero;
        floatRb.angularVelocity = Vector3.zero;

        floatRb.isKinematic = true;

        floatRb.position = waterPoint;
    }

    public void ResetFloat()
    {
        fishingFloat.transform.SetParent(floatHolder);

        fishingFloat.transform.localPosition = Vector3.zero;
        fishingFloat.transform.localRotation = Quaternion.identity;

        state = FloatState.Ready;

        floatRb.position = rodTip.position;
        floatRb.rotation = rodTip.rotation;

        floatRb.linearVelocity = Vector3.zero;
        floatRb.angularVelocity = Vector3.zero;

        floatRb.isKinematic = true;
    }
}