public interface IInteractable
{
    public string GetInteractText(); // UI에 표시할 텍스트 (예: "사과 먹기", "도끼 줍기")
    public void Interact();          // A 버튼을 눌렀을 때 실행할 실제 기능
}