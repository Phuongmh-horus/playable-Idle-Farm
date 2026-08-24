using UnityEngine;

public class LookAtPosition : MonoBehaviour
{
    [SerializeField] private Transform target;
    // Start is called before the first frame update
    void Start()
    {
        if (target != null)
        {
            transform.LookAt(target);
        }
    }

}
