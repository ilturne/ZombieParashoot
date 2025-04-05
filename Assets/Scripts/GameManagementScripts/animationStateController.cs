using UnityEngine;

public class AnimationStateController : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Get horizontal and vertical axis values (set to "0" if no input)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Determine if the character should be running
        bool isRunning = Mathf.Abs(horizontal) > 0 || Mathf.Abs(vertical) > 0;
        animator.SetBool("isRunning", isRunning);
    }
}
