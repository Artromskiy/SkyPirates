using UnityEngine;

public class EditorGameObject : MonoBehaviour
{
    private void Awake()
    {
        gameObject.SetActive(Application.isEditor);
    }
}
