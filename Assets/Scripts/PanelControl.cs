using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class PanelControl : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private Transform TargetPanel;

    private Vector2 StartPoint;
    private Vector2 EndPoint;

    private void Awake()
    {
        if(TargetPanel == null)
        {
            TargetPanel = transform.parent;
        }
    }

    //드래그 시작 위치 지정
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        StartPoint = TargetPanel.position;
        EndPoint = eventData.position;
    }


    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        TargetPanel.position = StartPoint + (eventData.position - EndPoint);
    }

    
}
